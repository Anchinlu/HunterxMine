using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MineCraftUnity.Core;
using MineCraftUnity.Rendering;

namespace MineCraftUnity.World
{
    public readonly struct ChunkGenerationResult
    {
        public ChunkPos Position { get; }
        public bool Success { get; }
        public bool WasSkipped { get; }
        public string ErrorMessage { get; }
        public HashSet<ChunkPos> ChangedChunks { get; }

        public ChunkGenerationResult(ChunkPos position, bool success, bool wasSkipped, string errorMessage, HashSet<ChunkPos> changedChunks)
        {
            Position = position;
            Success = success;
            WasSkipped = wasSkipped;
            ErrorMessage = errorMessage;
            ChangedChunks = changedChunks;
        }
    }

    /// <summary>
    /// Runs chunk block fill on a thread-pool worker. Unity APIs are not used here.
    /// </summary>
    public sealed class ChunkGenerationWorker : IDisposable
    {
        private readonly ConcurrentQueue<ChunkGenerationResult> _completed = new();
        private readonly SemaphoreSlim _parallelLimit;
        private int _disposed;

        public ChunkGenerationWorker(int maxParallel)
        {
            maxParallel = Math.Max(1, maxParallel);
            _parallelLimit = new SemaphoreSlim(maxParallel, maxParallel);
        }

        public bool TryDequeueCompleted(out ChunkGenerationResult result) => _completed.TryDequeue(out result);

        public bool TryStart(
            ChunkPos pos,
            Level level,
            IChunkGenerator generator,
            object worldLock,
            Func<ChunkPos, bool> isStillNeeded)
        {
            if (!_parallelLimit.Wait(0))
            {
                return false;
            }

            Task.Run(() =>
            {
                bool success = true;
                bool wasSkipped = false;
                string errorMessage = null;
                var changedChunks = new HashSet<ChunkPos>();

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
                                    generator.GenerateChunk(level, chunk, changedChunks);
                                    level.ApplyPendingDecorations(chunk, changedChunks);
                                }
                            }
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
                    _completed.Enqueue(new ChunkGenerationResult(pos, success, wasSkipped, errorMessage, changedChunks));
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
