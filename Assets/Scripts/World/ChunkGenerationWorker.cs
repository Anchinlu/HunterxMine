using MineCraftUnity.World;
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
        public ChunkGenerationData Data { get; }
        public bool Success { get; }
        public bool WasSkipped { get; }
        public string ErrorMessage { get; }

        public ChunkGenerationResult(ChunkPos position, ChunkGenerationData data, bool success, bool wasSkipped, string errorMessage)
        {
            Position = position;
            Data = data;
            Success = success;
            WasSkipped = wasSkipped;
            ErrorMessage = errorMessage;
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
                ChunkGenerationData data = null;
                bool success = true;
                bool wasSkipped = false;
                string errorMessage = null;

                try
                {
                    if (isStillNeeded(pos))
                    {
                        using (ChunkProfilerMarkers.GenerateChunk.Auto())
                        {
                            data = generator.ComputeChunkData(pos);
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
                    _completed.Enqueue(new ChunkGenerationResult(pos, data, success, wasSkipped, errorMessage));
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

