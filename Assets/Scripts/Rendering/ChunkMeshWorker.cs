using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using MineCraftUnity.Core;
using MineCraftUnity.World;

namespace MineCraftUnity.Rendering
{
    public readonly struct ChunkMeshResult
    {
        public ChunkPos Position { get; }
        public ChunkMeshData Data { get; }
        public bool WithCollision { get; }

        public bool Success { get; }
        public bool WasSkipped { get; }
        public string ErrorMessage { get; }
        public int Revision { get; }

        public ChunkMeshResult(ChunkPos position, ChunkMeshData data, bool withCollision, int revision, bool success = true, bool wasSkipped = false, string errorMessage = null)
        {
            Position = position;
            Data = data;
            WithCollision = withCollision;
            Revision = revision;
            Success = success;
            WasSkipped = wasSkipped;
            ErrorMessage = errorMessage;
        }
    }

    /// <summary>
    /// Builds chunk mesh CPU data on worker threads; GPU upload stays on main thread.
    /// </summary>
    public sealed class ChunkMeshWorker : IDisposable
    {
        private readonly ConcurrentQueue<ChunkMeshResult> _completed = new();
        private readonly SemaphoreSlim _parallelLimit;
        private int _disposed;

        public ChunkMeshWorker(int maxParallel)
        {
            maxParallel = Math.Max(1, maxParallel);
            _parallelLimit = new SemaphoreSlim(maxParallel, maxParallel);
        }

        public bool TryDequeueCompleted(out ChunkMeshResult result) => _completed.TryDequeue(out result);

        public bool TryStart(
            ChunkPos pos,
            Level level,
            bool withCollision,
            object worldLock,
            Func<ChunkPos, bool> isStillNeeded)
        {
            ChunkMeshSnapshot snapshot = null;
            lock (worldLock)
            {
                if (level.TryGetChunk(pos, out var chunk) && chunk.IsGenerated)
                {
                    snapshot = new ChunkMeshSnapshot(level, chunk);
                }
            }

            if (snapshot == null)
            {
                _completed.Enqueue(new ChunkMeshResult(pos, null, withCollision, 0, true, true, null));
                return true; // Skipping gracefully if chunk doesn't exist
            }

            if (!_parallelLimit.Wait(0))
            {
                return false;
            }

            int snapshotRevision = snapshot.Revision;

            Task.Run(() =>
            {
                ChunkMeshData data = null;
                bool success = true;
                bool wasSkipped = false;
                string errorMessage = null;
                try
                {
                    if (isStillNeeded(pos))
                    {
                        using (ChunkProfilerMarkers.MeshBuildInto.Auto())
                        {
                            data = ChunkMeshBuilder.ComputeMeshData(snapshot);
                        }
                    }
                    else
                    {
                        wasSkipped = true;
                    }
                }
                catch (Exception ex)
                {
                    success = false;
                    errorMessage = ex.ToString();
                }
                finally
                {
                    _completed.Enqueue(new ChunkMeshResult(pos, data, withCollision, snapshotRevision, success, wasSkipped, errorMessage));
                    _parallelLimit.Release();
                }
            });

            return true;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            _parallelLimit.Dispose();
        }
    }
}
