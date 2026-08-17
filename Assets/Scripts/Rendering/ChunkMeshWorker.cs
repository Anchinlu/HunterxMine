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
        public int PipelineGeneration { get; }

        public ChunkMeshResult(ChunkPos position, ChunkMeshData data, bool withCollision, int revision,
            int pipelineGeneration, bool success = true, bool wasSkipped = false, string errorMessage = null)
        {
            Position = position;
            Data = data;
            WithCollision = withCollision;
            Revision = revision;
            PipelineGeneration = pipelineGeneration;
            Success = success;
            WasSkipped = wasSkipped;
            ErrorMessage = errorMessage;
        }
    }

    /// <summary>
    /// Builds chunk mesh CPU data on worker threads; GPU upload stays on main thread.
    /// Semaphore is acquired BEFORE creating the snapshot to avoid wasted allocations
    /// when all workers are busy.
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
            Func<ChunkPos, bool> isStillNeeded,
            int pipelineGeneration)
        {
            // Acquire worker slot FIRST — avoid expensive snapshot copy when all workers are busy.
            if (!_parallelLimit.Wait(0))
            {
                return false;
            }

            ChunkMeshSnapshot snapshot = null;
            try
            {
                lock (worldLock)
                {
                    if (level.TryGetChunk(pos, out var chunk) && chunk.IsGenerated)
                    {
                        snapshot = new ChunkMeshSnapshot(level, chunk);
                    }
                }
            }
            catch
            {
                _parallelLimit.Release();
                throw;
            }

            if (snapshot == null)
            {
                // Chunk unavailable — release slot and report skip.
                _parallelLimit.Release();
                _completed.Enqueue(new ChunkMeshResult(pos, null, withCollision, 0, pipelineGeneration, true, true));
                return true;
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
                    _completed.Enqueue(new ChunkMeshResult(pos, data, withCollision, snapshotRevision, pipelineGeneration, success, wasSkipped, errorMessage));
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
