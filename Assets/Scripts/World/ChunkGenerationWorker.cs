using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using MineCraftUnity.Core;
using MineCraftUnity.Rendering;

namespace MineCraftUnity.World
{
    /// <summary>
    /// Runs chunk block fill on a thread-pool worker. Unity APIs are not used here.
    /// </summary>
    public sealed class ChunkGenerationWorker : IDisposable
    {
        private readonly ConcurrentQueue<ChunkPos> _completed = new();
        private readonly SemaphoreSlim _parallelLimit;
        private int _disposed;

        public ChunkGenerationWorker(int maxParallel)
        {
            maxParallel = Math.Max(1, maxParallel);
            _parallelLimit = new SemaphoreSlim(maxParallel, maxParallel);
        }

        public bool TryDequeueCompleted(out ChunkPos pos) => _completed.TryDequeue(out pos);

        public bool TryStart(
            ChunkPos pos,
            Level level,
            OverworldGenerator generator,
            object worldLock,
            Func<ChunkPos, bool> isStillNeeded)
        {
            if (!_parallelLimit.Wait(0))
            {
                return false;
            }

            Task.Run(() =>
            {
                try
                {
                    if (isStillNeeded(pos))
                    {
                        lock (worldLock)
                        {
                            var chunk = level.GetOrCreateChunk(pos);
                            if (!chunk.IsGenerated)
                            {
                                using (ChunkProfilerMarkers.GenerateChunk.Auto())
                                {
                                    generator.GenerateChunk(level, chunk);
                                }
                            }
                        }
                    }

                    _completed.Enqueue(pos);
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
