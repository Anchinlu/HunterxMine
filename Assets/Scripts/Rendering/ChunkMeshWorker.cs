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

        public ChunkMeshResult(ChunkPos position, ChunkMeshData data, bool withCollision)
        {
            Position = position;
            Data = data;
            WithCollision = withCollision;
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
            if (!_parallelLimit.Wait(0))
            {
                return false;
            }

            Task.Run(() =>
            {
                ChunkMeshData data = null;
                try
                {
                    if (isStillNeeded(pos))
                    {
                        Chunk chunk;
                        lock (worldLock)
                        {
                            if (level.TryGetChunk(pos, out chunk) && chunk.IsGenerated)
                            {
                                using (ChunkProfilerMarkers.MeshBuildInto.Auto())
                                {
                                    data = ChunkMeshBuilder.ComputeMeshData(chunk, level);
                                }
                            }
                        }
                    }

                    _completed.Enqueue(new ChunkMeshResult(pos, data, withCollision));
                }
                finally
                {
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
