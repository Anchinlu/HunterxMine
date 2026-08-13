using System.Collections.Generic;
using MineCraftUnity.Core;
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
        public int PendingGenerationCount => _generationList.Count;
        public int PendingMeshCount => _meshList.Count;
        public bool IsSpawnAreaReady { get; private set; }

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
        }

        private void Awake()
        {
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
                EnsureSpawnAreaReady();
            }
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
            UpdateChunkSet(force: true);
            if (followTarget != null)
            {
                EnsureSpawnAreaReady();
            }
        }

        public void SetFollowTarget(Transform target)
        {
            followTarget = target;
            IsSpawnAreaReady = false;
            if (Application.isPlaying)
            {
                UpdateChunkSet(force: true);
                EnsureSpawnAreaReady();
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
        /// Immediately generates and meshes the spawn chunk neighborhood so the player has ground on first frame.
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
                    GenerateAndMeshImmediate(pos, enableCollision: true);
                }
            }

            SortPendingWork(center);
            UpdateCollisionStatesIfNeeded(center, force: true);
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

            if (!force)
            {
                UpdateCollisionStatesIfNeeded(center);
            }
        }

        private void ProcessQueues()
        {
            var center = _priorityCenter;
            if (center.X == int.MinValue)
            {
                center = GetCenterChunkPos();
            }

            var generated = 0;
            while (_generationList.Count > 0 && generated < WorldConstants.MaxChunkGenerationsPerFrame)
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

                _generator.GenerateChunk(_level, chunk);
                MarkAdjacentMeshesDirty(pos);
                ScheduleMesh(pos);
                generated++;
            }

            var meshed = 0;
            while (_meshList.Count > 0 && meshed < WorldConstants.MaxChunkMeshesPerFrame)
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
                EnsureRenderer(chunk, withCollision);
                meshed++;
            }

            if (_generationList.Count == 0 && _meshList.Count == 0)
            {
                UpdateCollisionStatesIfNeeded(GetCenterChunkPos());
            }
        }

        private void GenerateAndMeshImmediate(ChunkPos pos, bool enableCollision)
        {
            var chunk = _level.GetOrCreateChunk(pos);
            if (!chunk.IsGenerated)
            {
                _generator.GenerateChunk(_level, chunk);
                MarkAdjacentMeshesDirty(pos);
            }

            UnscheduleGeneration(pos);
            EnsureRenderer(chunk, enableCollision);
            UnscheduleMesh(pos);
        }

        private void EnsureRenderer(Chunk chunk, bool withCollision)
        {
            if (_renderers.TryGetValue(chunk.Position, out var existing))
            {
                if (chunk.IsMeshDirty)
                {
                    existing.Rebuild(chunk, _level, withCollision);
                }
                else
                {
                    existing.SetCollisionEnabled(withCollision);
                }

                return;
            }

            var go = new GameObject();
            go.transform.SetParent(transform, false);
            var renderer = go.AddComponent<ChunkRenderer>();
            renderer.Initialize(chunk.Position);
            renderer.Rebuild(chunk, _level, withCollision);
            _renderers[chunk.Position] = renderer;
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
                pair.Value.SetCollisionEnabled(enableCollision);
            }
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
            _lastCenterChunk = new ChunkPos(int.MinValue, int.MinValue);
            _lastCollisionCenter = new ChunkPos(int.MinValue, int.MinValue);
            _priorityCenter = new ChunkPos(int.MinValue, int.MinValue);
        }

        private void OnDestroy()
        {
            ClearWorld();
        }
    }
}
