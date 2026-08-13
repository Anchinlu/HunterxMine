using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using MineCraftUnity.Core;
using MineCraftUnity.UI;
using MineCraftUnity.World;
using UnityEngine;

namespace MineCraftUnity.Rendering
{
    /// <summary>
    /// MC ref: net.minecraft.client.multiplayer.ClientChunkCache
    /// </summary>
    public sealed class ChunkManager : MonoBehaviour
    {
        [SerializeField] private int seed = 12345;
        [SerializeField] private int viewDistance = WorldConstants.DefaultViewDistance;
        [SerializeField] private int collisionDistance = WorldConstants.CollisionDistance;
        [SerializeField] private Transform followTarget;

        public int Seed => seed;
        public int ViewDistance => viewDistance;
        public int LoadedChunkCount => _renderers.Count;
        public int PendingGenerationCount => _generationList.Count + _generationInFlight.Count;
        public int PendingMeshCount => _meshList.Count + _meshInFlight.Count;
        public int GenerationInFlightCount => _generationInFlight.Count;
        public int MeshInFlightCount => _meshInFlight.Count;
        public int PendingCollisionCount => _collisionQueue.Count;
        public int PendingFluidTickCount => _fluidSimulator?.PendingTickCount ?? 0;
        public bool IsSpawning => _spawnAreaCoroutine != null;
        public bool IsSpawnAreaReady { get; private set; }
        public static string PipelineVersion => WorldConstants.PipelineVersion;

        private Level _level;
        private OverworldGenerator _generator;
        private readonly Dictionary<ChunkPos, ChunkRenderer> _renderers = new();
        private readonly HashSet<ChunkPos> _neededChunks = new();

        private readonly List<ChunkPos> _generationList = new();
        private readonly HashSet<ChunkPos> _generationScheduled = new();
        private readonly List<ChunkPos> _meshList = new();
        private readonly HashSet<ChunkPos> _meshScheduled = new();

        private ChunkPos _lastCenterChunk = new(int.MinValue, int.MinValue);
        private ChunkPos _lastCollisionCenter = new(int.MinValue, int.MinValue);
        private ChunkPos _priorityCenter = new(int.MinValue, int.MinValue);
        private float _chunkUpdateTimer;
        private Coroutine _spawnAreaCoroutine;
        private readonly HashSet<ChunkPos> _spawnChunks = new();

        private readonly object _worldLock = new();
        private ChunkGenerationWorker _generationWorker;
        private ChunkMeshWorker _meshWorker;
        private readonly HashSet<ChunkPos> _generationInFlight = new();
        private readonly HashSet<ChunkPos> _meshInFlight = new();
        private readonly Queue<ChunkPos> _collisionQueue = new();
        private readonly HashSet<ChunkPos> _collisionScheduled = new();
        private readonly Dictionary<ChunkPos, bool> _collisionDesired = new();
        private readonly Dictionary<ChunkPos, bool> _collisionApplied = new();
        private FluidSimulator _fluidSimulator;

        public Level Level
        {
            get
            {
                EnsureInitialized();
                return _level;
            }
        }

        public OverworldGenerator Generator
        {
            get
            {
                EnsureInitialized();
                return _generator;
            }
        }

        private void EnsureInitialized()
        {
            if (_level != null && _generator != null)
            {
                return;
            }

            _level = new Level(seed);
            _generator = new OverworldGenerator(seed);
            _fluidSimulator = new FluidSimulator(_level);
            WireLevelEvents();
            _generationWorker ??= new ChunkGenerationWorker(WorldConstants.MaxParallelChunkWorkers);
            _meshWorker ??= new ChunkMeshWorker(WorldConstants.MaxParallelChunkWorkers);
        }

        private void WireLevelEvents()
        {
            _level.BlockChanged += OnLevelBlockChanged;
            _level.FluidTickRequested += pos => _fluidSimulator?.ScheduleTick(pos);
        }

        private void OnLevelBlockChanged(ChunkPos chunkPos)
        {
            if (!_neededChunks.Contains(chunkPos))
            {
                return;
            }

            ScheduleMesh(chunkPos);
        }

        private void Awake()
        {
            BiomeRegistry.EnsureLoaded();
            EnsureInitialized();
            BlockMaterialLibrary.EnsureInitialized();
        }

        private void Start()
        {
            if (followTarget == null)
            {
                var player = GameObject.Find("Player");
                if (player != null)
                {
                    followTarget = player.transform;
                }
            }

            _chunkUpdateTimer = 0f;
            UpdateChunkSet(force: true);

            if (followTarget != null)
            {
                BeginSpawnAreaLoad();
            }
        }

        private void BeginSpawnAreaLoad()
        {
            if (_spawnAreaCoroutine != null)
            {
                StopCoroutine(_spawnAreaCoroutine);
            }

            _spawnAreaCoroutine = StartCoroutine(EnsureSpawnAreaReadyCoroutine());
        }

        private IEnumerator EnsureSpawnAreaReadyCoroutine()
        {
            if (!Application.isPlaying)
            {
                yield break;
            }

            IsSpawnAreaReady = false;
            EnsureInitialized();
            ResetAsyncPipelineState();

            var center = GetCenterChunkPos();
            _priorityCenter = center;
            var radius = Mathf.Max(collisionDistance, WorldConstants.SpawnPriorityRadius);

            _spawnChunks.Clear();
            for (var dx = -radius; dx <= radius; dx++)
            {
                for (var dz = -radius; dz <= radius; dz++)
                {
                    var pos = new ChunkPos(center.X + dx, center.Z + dz);
                    _neededChunks.Add(pos);
                    _spawnChunks.Add(pos);
                    ScheduleGeneration(pos);
                }
            }

            SortPendingWork(center);
            var total = _spawnChunks.Count;

            WorldLoadingOverlay.Show("Generating world…");
            WorldLoadingOverlay.SetProgress(0f);

            while (!AreAllSpawnChunksReady())
            {
                var ready = CountReadySpawnChunks();
                WorldLoadingOverlay.SetProgress(total > 0 ? (float)ready / total : 1f);
                yield return null;
            }

            WorldLoadingOverlay.SetProgress(1f);
            UpdateCollisionStatesIfNeeded(center, force: true);
            WorldLoadingOverlay.Hide();
            IsSpawnAreaReady = true;
            _spawnChunks.Clear();
            _spawnAreaCoroutine = null;
            UpdateChunkSet(force: true);
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            _chunkUpdateTimer -= Time.deltaTime;
            if (_chunkUpdateTimer <= 0f)
            {
                _chunkUpdateTimer = WorldConstants.ChunkUpdateInterval;
                UpdateChunkSet(force: false);
            }

            ProcessQueues();
            ProcessFluidTicks();
        }

        private void ProcessFluidTicks()
        {
            if (_fluidSimulator == null || !IsSpawnAreaReady)
            {
                return;
            }

            _fluidSimulator.ProcessTicks(WorldConstants.MaxFluidTicksPerFrame);
        }

        public void Configure(int newSeed, int newViewDistance, Transform target = null)
        {
            viewDistance = newViewDistance;
            if (target != null)
            {
                followTarget = target;
            }

            if (_level == null || seed != newSeed)
            {
                seed = newSeed;
                if (_level != null)
                {
                    SetSeed(newSeed);
                }
            }
        }

        public void SetSeed(int newSeed)
        {
            seed = newSeed;
            IsSpawnAreaReady = false;
            if (!Application.isPlaying)
            {
                _level = null;
                _generator = null;
                EnsureInitialized();
                return;
            }

            ClearWorld();
            _level = new Level(seed);
            _generator = new OverworldGenerator(seed);
            _fluidSimulator = new FluidSimulator(_level);
            WireLevelEvents();
            UpdateChunkSet(force: true);
            if (followTarget != null)
            {
                BeginSpawnAreaLoad();
            }
        }

        public void SetFollowTarget(Transform target)
        {
            if (followTarget == target && (IsSpawnAreaReady || _spawnAreaCoroutine != null))
            {
                return;
            }

            followTarget = target;
            IsSpawnAreaReady = false;
            if (Application.isPlaying)
            {
                UpdateChunkSet(force: true);
                BeginSpawnAreaLoad();
            }
        }

        public int SampleSurfaceHeight(int worldX, int worldZ)
        {
            EnsureInitialized();
            return _generator.SampleSurfaceHeight(worldX, worldZ);
        }

        public bool TrySampleTopSolidY(int worldX, int worldZ, out int topY)
        {
            EnsureInitialized();
            return _level.TrySampleTopSolidY(worldX, worldZ, out topY);
        }

        /// <summary>
        /// Synchronous spawn load (editor/tests). Prefer <see cref="BeginSpawnAreaLoad"/> at runtime.
        /// </summary>
        public void EnsureSpawnAreaReady()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            EnsureInitialized();
            var center = GetCenterChunkPos();
            _priorityCenter = center;
            var radius = Mathf.Max(collisionDistance, WorldConstants.SpawnPriorityRadius);

            for (var dx = -radius; dx <= radius; dx++)
            {
                for (var dz = -radius; dz <= radius; dz++)
                {
                    var pos = new ChunkPos(center.X + dx, center.Z + dz);
                    _neededChunks.Add(pos);
                    GenerateAndMeshImmediate(pos);
                }
            }

            SortPendingWork(center);
            UpdateCollisionStatesIfNeeded(center, force: true);
            ProcessCollisionQueue();
            IsSpawnAreaReady = true;
        }

        private void UpdateChunkSet(bool force)
        {
            EnsureInitialized();
            var center = GetCenterChunkPos();
            _priorityCenter = center;

            if (!force && center.Equals(_lastCenterChunk) &&
                _generationList.Count == 0 && _meshList.Count == 0)
            {
                UpdateCollisionStatesIfNeeded(center);
                return;
            }

            _lastCenterChunk = center;
            _neededChunks.Clear();

            for (var dx = -viewDistance; dx <= viewDistance; dx++)
            {
                for (var dz = -viewDistance; dz <= viewDistance; dz++)
                {
                    _neededChunks.Add(new ChunkPos(center.X + dx, center.Z + dz));
                }
            }

            var toRemove = new List<ChunkPos>();
            foreach (var pos in _renderers.Keys)
            {
                if (!_neededChunks.Contains(pos))
                {
                    toRemove.Add(pos);
                }
            }

            foreach (var pos in toRemove)
            {
                if (_renderers.TryGetValue(pos, out var renderer))
                {
                    Destroy(renderer.gameObject);
                    _renderers.Remove(pos);
                }
            }

            RemoveFromPending(toRemove);

            if (_spawnAreaCoroutine == null)
            {
                foreach (var pos in _neededChunks)
                {
                    var chunk = _level.GetOrCreateChunk(pos);
                    if (!chunk.IsGenerated)
                    {
                        ScheduleGeneration(pos);
                    }
                    else if (chunk.IsMeshDirty || !_renderers.ContainsKey(pos))
                    {
                        ScheduleMesh(pos);
                    }
                }

                SortPendingWork(center);
            }

            if (!force)
            {
                UpdateCollisionStatesIfNeeded(center);
            }
        }

        private void ProcessQueues()
        {
            using (ChunkProfilerMarkers.ProcessQueues.Auto())
            {
                if (IsSpawnAreaReady || _spawnAreaCoroutine != null)
                {
                    DrainAsyncGenerationResults();
                    DrainAsyncMeshResults();
                    TryScheduleAsyncGeneration();
                    TryScheduleAsyncMesh();
                }
                else if (_generationList.Count > 0)
                {
                    ProcessGenerationQueueSync();
                }
                else if (_meshList.Count > 0)
                {
                    ProcessMeshQueueSync();
                }

                ProcessCollisionQueue();
            }
        }

        private void DrainAsyncGenerationResults()
        {
            while (_generationWorker.TryDequeueCompleted(out var pos))
            {
                _generationInFlight.Remove(pos);
                if (!_neededChunks.Contains(pos))
                {
                    continue;
                }

                if (!_level.TryGetChunk(pos, out var chunk) || !chunk.IsGenerated)
                {
                    continue;
                }

                _fluidSimulator?.ScheduleSpreadCandidatesForChunk(chunk);
                MarkAdjacentMeshesDirty(pos);
                ScheduleMesh(pos);
            }
        }

        private void DrainAsyncMeshResults()
        {
            var maxApply = _spawnAreaCoroutine != null
                ? WorldConstants.MaxSpawnChunkMeshesPerFrame
                : WorldConstants.MaxChunkMeshesPerFrame;

            var applied = 0;
            while (applied < maxApply &&
                   _meshWorker.TryDequeueCompleted(out var result))
            {
                _meshInFlight.Remove(result.Position);
                if (!_neededChunks.Contains(result.Position))
                {
                    continue;
                }

                ApplyMeshResult(result);
                UnscheduleMesh(result.Position);
                RequestCollisionState(result.Position, result.WithCollision);
                applied++;
            }

            if (_generationList.Count == 0 && _generationInFlight.Count == 0 &&
                _meshList.Count == 0 && _meshInFlight.Count == 0)
            {
                UpdateCollisionStatesIfNeeded(GetCenterChunkPos());
            }
        }

        private void TryScheduleAsyncGeneration()
        {
            if (_generationList.Count == 0)
            {
                return;
            }

            var center = _priorityCenter;
            if (center.X == int.MinValue)
            {
                center = GetCenterChunkPos();
            }

            SortPendingWork(center);

            for (var i = 0; i < _generationList.Count;)
            {
                var pos = _generationList[i];
                if (!_neededChunks.Contains(pos))
                {
                    _generationList.RemoveAt(i);
                    _generationScheduled.Remove(pos);
                    continue;
                }

                if (_generationWorker.TryStart(pos, _level, _generator, _worldLock, IsChunkStillNeeded))
                {
                    _generationInFlight.Add(pos);
                    _generationList.RemoveAt(i);
                    _generationScheduled.Remove(pos);
                    continue;
                }

                break;
            }
        }

        private void TryScheduleAsyncMesh()
        {
            if (_meshList.Count == 0)
            {
                return;
            }

            var center = _priorityCenter;
            if (center.X == int.MinValue)
            {
                center = GetCenterChunkPos();
            }

            SortPendingWork(center);
            var collisionCenter = GetCenterChunkPos();

            for (var i = 0; i < _meshList.Count;)
            {
                var pos = _meshList[i];
                if (!_neededChunks.Contains(pos))
                {
                    _meshList.RemoveAt(i);
                    _meshScheduled.Remove(pos);
                    continue;
                }

                if (!_level.TryGetChunk(pos, out var chunk) || !chunk.IsGenerated)
                {
                    _meshList.RemoveAt(i);
                    _meshScheduled.Remove(pos);
                    continue;
                }

                var withCollision = IsWithinDistance(pos, collisionCenter, collisionDistance);
                if (_meshWorker.TryStart(pos, _level, withCollision, _worldLock, IsChunkStillNeeded))
                {
                    _meshInFlight.Add(pos);
                    _meshList.RemoveAt(i);
                    _meshScheduled.Remove(pos);
                    continue;
                }

                break;
            }
        }

        private bool IsChunkStillNeeded(ChunkPos pos) => _neededChunks.Contains(pos);

        private void ProcessGenerationQueueSync()
        {
            var generationStopwatch = Stopwatch.StartNew();
            var generated = 0;
            while (_generationList.Count > 0 &&
                   generated < WorldConstants.MaxChunkGenerationsPerFrame &&
                   generationStopwatch.Elapsed.TotalMilliseconds < WorldConstants.GenerationBudgetMs)
            {
                var pos = _generationList[0];
                _generationList.RemoveAt(0);
                _generationScheduled.Remove(pos);

                if (!_neededChunks.Contains(pos))
                {
                    continue;
                }

                var chunk = _level.GetOrCreateChunk(pos);
                if (chunk.IsGenerated)
                {
                    continue;
                }

                using (ChunkProfilerMarkers.GenerateChunk.Auto())
                {
                    _generator.GenerateChunk(_level, chunk);
                }

                _fluidSimulator?.ScheduleSpreadCandidatesForChunk(chunk);
                MarkAdjacentMeshesDirty(pos);
                ScheduleMesh(pos);
                generated++;
            }
        }

        private void ProcessMeshQueueSync()
        {
            var meshStopwatch = Stopwatch.StartNew();
            var meshed = 0;
            while (_meshList.Count > 0 &&
                   meshed < WorldConstants.MaxChunkMeshesPerFrame &&
                   meshStopwatch.Elapsed.TotalMilliseconds < WorldConstants.MeshBudgetMs)
            {
                var pos = _meshList[0];
                _meshList.RemoveAt(0);
                _meshScheduled.Remove(pos);

                if (!_neededChunks.Contains(pos))
                {
                    continue;
                }

                if (!_level.TryGetChunk(pos, out var chunk) || !chunk.IsGenerated)
                {
                    continue;
                }

                var withCollision = IsWithinDistance(pos, GetCenterChunkPos(), collisionDistance);
                EnsureRenderer(chunk);
                RequestCollisionState(pos, withCollision);
                meshed++;
            }

            if (_generationList.Count == 0 && _meshList.Count == 0)
            {
                UpdateCollisionStatesIfNeeded(GetCenterChunkPos());
            }
        }

        private void GenerateImmediate(ChunkPos pos)
        {
            var chunk = _level.GetOrCreateChunk(pos);
            if (!chunk.IsGenerated)
            {
                using (ChunkProfilerMarkers.GenerateChunk.Auto())
                {
                    _generator.GenerateChunk(_level, chunk);
                }

                MarkAdjacentMeshesDirty(pos);
            }

            UnscheduleGeneration(pos);
        }

        private void MeshImmediate(ChunkPos pos)
        {
            if (!_level.TryGetChunk(pos, out var chunk) || !chunk.IsGenerated)
            {
                return;
            }

            EnsureRenderer(chunk);
            UnscheduleMesh(pos);
        }

        private void GenerateAndMeshImmediate(ChunkPos pos)
        {
            GenerateImmediate(pos);
            MeshImmediate(pos);
        }

        private void ApplyMeshResult(ChunkMeshResult result)
        {
            if (result.Data == null)
            {
                return;
            }

            if (!_level.TryGetChunk(result.Position, out var chunk))
            {
                return;
            }

            using (ChunkProfilerMarkers.EnsureRenderer.Auto())
            {
                if (_renderers.TryGetValue(result.Position, out var existing))
                {
                    existing.ApplyMeshData(result.Data);
                }
                else
                {
                    var go = new GameObject();
                    go.transform.SetParent(transform, false);
                    var renderer = go.AddComponent<ChunkRenderer>();
                    renderer.Initialize(result.Position);
                    renderer.ApplyMeshData(result.Data);
                    _renderers[result.Position] = renderer;
                }

                chunk.IsMeshDirty = false;
            }
        }

        private void EnsureRenderer(Chunk chunk)
        {
            using (ChunkProfilerMarkers.EnsureRenderer.Auto())
            {
                if (_renderers.TryGetValue(chunk.Position, out var existing))
                {
                    if (chunk.IsMeshDirty)
                    {
                        existing.Rebuild(chunk, _level);
                    }

                    return;
                }

                var go = new GameObject();
                go.transform.SetParent(transform, false);
                var renderer = go.AddComponent<ChunkRenderer>();
                renderer.Initialize(chunk.Position);
                renderer.Rebuild(chunk, _level);
                _renderers[chunk.Position] = renderer;
            }
        }

        private void MarkAdjacentMeshesDirty(ChunkPos pos)
        {
            TryMarkDirty(new ChunkPos(pos.X + 1, pos.Z));
            TryMarkDirty(new ChunkPos(pos.X - 1, pos.Z));
            TryMarkDirty(new ChunkPos(pos.X, pos.Z + 1));
            TryMarkDirty(new ChunkPos(pos.X, pos.Z - 1));
        }

        private void TryMarkDirty(ChunkPos pos)
        {
            if (!_level.TryGetChunk(pos, out var neighbor) || !neighbor.IsGenerated)
            {
                return;
            }

            neighbor.IsMeshDirty = true;
            if (_neededChunks.Contains(pos))
            {
                ScheduleMesh(pos);
            }
        }

        private void ScheduleGeneration(ChunkPos pos)
        {
            if (_generationScheduled.Add(pos))
            {
                _generationList.Add(pos);
            }
        }

        private void ScheduleMesh(ChunkPos pos)
        {
            if (_meshScheduled.Add(pos))
            {
                _meshList.Add(pos);
            }
        }

        private void UnscheduleGeneration(ChunkPos pos)
        {
            if (!_generationScheduled.Remove(pos))
            {
                return;
            }

            _generationList.Remove(pos);
        }

        private void UnscheduleMesh(ChunkPos pos)
        {
            if (!_meshScheduled.Remove(pos))
            {
                return;
            }

            _meshList.Remove(pos);
        }

        private void SortPendingWork(ChunkPos center)
        {
            _generationList.Sort((a, b) => CompareChunkPriority(a, b, center));
            _meshList.Sort((a, b) => CompareChunkPriority(a, b, center));
        }

        private static int CompareChunkPriority(ChunkPos a, ChunkPos b, ChunkPos center)
        {
            var distA = ChunkDistanceSquared(a, center);
            var distB = ChunkDistanceSquared(b, center);
            if (distA != distB)
            {
                return distA.CompareTo(distB);
            }

            // Stable tie-break so ordering is deterministic.
            if (a.X != b.X)
            {
                return a.X.CompareTo(b.X);
            }

            return a.Z.CompareTo(b.Z);
        }

        private static int ChunkDistanceSquared(ChunkPos a, ChunkPos center)
        {
            var dx = a.X - center.X;
            var dz = a.Z - center.Z;
            return dx * dx + dz * dz;
        }

        private void UpdateCollisionStatesIfNeeded(ChunkPos center, bool force = false)
        {
            if (!force && center.Equals(_lastCollisionCenter))
            {
                return;
            }

            _lastCollisionCenter = center;
            foreach (var pair in _renderers)
            {
                var enableCollision = IsWithinDistance(pair.Key, center, collisionDistance);
                RequestCollisionState(pair.Key, enableCollision);
            }
        }

        private void RequestCollisionState(ChunkPos pos, bool enable)
        {
            _collisionDesired[pos] = enable;
            if (_collisionApplied.TryGetValue(pos, out var applied) && applied == enable)
            {
                return;
            }

            if (_collisionScheduled.Add(pos))
            {
                _collisionQueue.Enqueue(pos);
            }
        }

        private void ProcessCollisionQueue()
        {
            if (_collisionQueue.Count == 0)
            {
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            var processed = 0;
            while (_collisionQueue.Count > 0 &&
                   processed < WorldConstants.MaxCollisionUpdatesPerFrame &&
                   stopwatch.Elapsed.TotalMilliseconds < WorldConstants.CollisionBudgetMs)
            {
                var pos = _collisionQueue.Dequeue();
                _collisionScheduled.Remove(pos);

                if (!_collisionDesired.TryGetValue(pos, out var enable))
                {
                    continue;
                }

                if (_collisionApplied.TryGetValue(pos, out var applied) && applied == enable)
                {
                    continue;
                }

                if (!_renderers.TryGetValue(pos, out var renderer))
                {
                    continue;
                }

                renderer.SetCollisionEnabled(enable);
                _collisionApplied[pos] = enable;
                processed++;
            }
        }

        private void ClearPendingQueues()
        {
            _generationList.Clear();
            _generationScheduled.Clear();
            _meshList.Clear();
            _meshScheduled.Clear();
        }

        private void ResetAsyncPipelineState()
        {
            ClearPendingQueues();
            _generationInFlight.Clear();
            _meshInFlight.Clear();

            if (_generationWorker != null)
            {
                while (_generationWorker.TryDequeueCompleted(out _))
                {
                }
            }

            if (_meshWorker != null)
            {
                while (_meshWorker.TryDequeueCompleted(out _))
                {
                }
            }
        }

        private bool IsSpawnChunkReady(ChunkPos pos)
        {
            if (!_level.TryGetChunk(pos, out var chunk) || !chunk.IsGenerated)
            {
                return false;
            }

            if (chunk.IsMeshDirty || !_renderers.ContainsKey(pos))
            {
                return false;
            }

            return true;
        }

        private int CountReadySpawnChunks()
        {
            var ready = 0;
            foreach (var pos in _spawnChunks)
            {
                if (IsSpawnChunkReady(pos))
                {
                    ready++;
                }
            }

            return ready;
        }

        private bool AreAllSpawnChunksReady()
        {
            foreach (var pos in _spawnChunks)
            {
                if (!IsSpawnChunkReady(pos))
                {
                    return false;
                }
            }

            return _spawnChunks.Count > 0;
        }

        private static bool IsWithinDistance(ChunkPos a, ChunkPos b, int distance)
        {
            return Mathf.Abs(a.X - b.X) <= distance && Mathf.Abs(a.Z - b.Z) <= distance;
        }

        private void RemoveFromPending(List<ChunkPos> removed)
        {
            if (removed.Count == 0)
            {
                return;
            }

            var removedSet = new HashSet<ChunkPos>(removed);
            for (var i = _generationList.Count - 1; i >= 0; i--)
            {
                if (removedSet.Contains(_generationList[i]))
                {
                    _generationScheduled.Remove(_generationList[i]);
                    _generationList.RemoveAt(i);
                }
            }

            for (var i = _meshList.Count - 1; i >= 0; i--)
            {
                if (removedSet.Contains(_meshList[i]))
                {
                    _meshScheduled.Remove(_meshList[i]);
                    _meshList.RemoveAt(i);
                }
            }
        }

        private ChunkPos GetCenterChunkPos()
        {
            if (followTarget == null)
            {
                return new ChunkPos(0, 0);
            }

            var pos = followTarget.position;
            return new ChunkPos(
                Mathf.FloorToInt(pos.x / WorldConstants.ChunkSize),
                Mathf.FloorToInt(pos.z / WorldConstants.ChunkSize));
        }

        private void ClearWorld()
        {
            _generationInFlight.Clear();
            _meshInFlight.Clear();
            if (_generationWorker != null)
            {
                while (_generationWorker.TryDequeueCompleted(out _))
                {
                }
            }

            if (_meshWorker != null)
            {
                while (_meshWorker.TryDequeueCompleted(out _))
                {
                }
            }

            foreach (var renderer in _renderers.Values)
            {
                if (renderer != null)
                {
                    Destroy(renderer.gameObject);
                }
            }

            _renderers.Clear();
            _neededChunks.Clear();
            _generationList.Clear();
            _generationScheduled.Clear();
            _meshList.Clear();
            _meshScheduled.Clear();
            _collisionQueue.Clear();
            _collisionScheduled.Clear();
            _collisionDesired.Clear();
            _collisionApplied.Clear();
            _lastCenterChunk = new ChunkPos(int.MinValue, int.MinValue);
            _lastCollisionCenter = new ChunkPos(int.MinValue, int.MinValue);
            _priorityCenter = new ChunkPos(int.MinValue, int.MinValue);
        }

        private void OnDestroy()
        {
            _generationWorker?.Dispose();
            _meshWorker?.Dispose();
            ClearWorld();
        }
    }
}
