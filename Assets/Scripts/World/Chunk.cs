using System;
using MineCraftUnity.Blocks;
using MineCraftUnity.Core;

namespace MineCraftUnity.World
{
    /// <summary>
    /// MC ref: net.minecraft.world.level.chunk.LevelChunk
    /// </summary>
    public sealed class Chunk
    {
        public ChunkPos Position { get; }
        private readonly BlockId[] _blocks = new BlockId[WorldConstants.ChunkSize * WorldConstants.Height * WorldConstants.ChunkSize];
        public bool IsGenerated { get; private set; }
        public bool IsMeshDirty { get; set; } = true;

        public int MinFilledY { get; private set; } = WorldConstants.MaxY;
        public int MaxFilledY { get; private set; } = WorldConstants.MinY;

        public Chunk(ChunkPos position)
        {
            Position = position;
        }

        public void MarkGenerated() => IsGenerated = true;

        public BlockId GetBlock(int localX, int y, int localZ)
        {
            if (!IsInside(localX, y, localZ))
            {
                return BlockId.Air;
            }

            return _blocks[ToIndex(localX, y, localZ)];
        }

        public void SetBlock(int localX, int y, int localZ, BlockId id, bool markDirty = true)
        {
            if (!IsInside(localX, y, localZ))
            {
                return;
            }

            _blocks[ToIndex(localX, y, localZ)] = id;
            TrackBounds(y, id);

            if (markDirty)
            {
                IsMeshDirty = true;
            }
        }

        public void FinishBulkFill()
        {
            IsMeshDirty = true;
        }

        public bool HasBlocks => MinFilledY <= MaxFilledY;

        public static bool IsInside(int localX, int y, int localZ) =>
            localX is >= 0 and < WorldConstants.ChunkSize &&
            localZ is >= 0 and < WorldConstants.ChunkSize &&
            y is >= WorldConstants.MinY and <= WorldConstants.MaxY;

        public static int ToIndex(int localX, int y, int localZ)
        {
            var localY = y - WorldConstants.MinY;
            return localY * WorldConstants.ChunkSize * WorldConstants.ChunkSize + localZ * WorldConstants.ChunkSize + localX;
        }

        private void TrackBounds(int y, BlockId id)
        {
            if (id == BlockId.Air)
            {
                return;
            }

            MinFilledY = Math.Min(MinFilledY, y);
            MaxFilledY = Math.Max(MaxFilledY, y);
        }
    }
}
