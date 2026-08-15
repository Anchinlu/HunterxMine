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
        private readonly byte[] _metadata = new byte[WorldConstants.ChunkSize * WorldConstants.Height * WorldConstants.ChunkSize];
        private readonly byte[] _fluidLevels = new byte[WorldConstants.ChunkSize * WorldConstants.Height * WorldConstants.ChunkSize];
        private readonly BiomeId[] _quartBiomes = new BiomeId[WorldConstants.BiomeQuartVolume];
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

        public byte GetFluidLevel(int localX, int y, int localZ)
        {
            if (!IsInside(localX, y, localZ))
            {
                return FluidLevel.Source;
            }

            return _fluidLevels[ToIndex(localX, y, localZ)];
        }

        public byte GetMetadata(int localX, int y, int localZ)
        {
            if (!IsInside(localX, y, localZ))
            {
                return 0;
            }

            return _metadata[ToIndex(localX, y, localZ)];
        }

        public void SetBlock(int localX, int y, int localZ, BlockId id, byte metadata = 0, bool markDirty = true)
        {
            if (!IsInside(localX, y, localZ))
            {
                return;
            }

            var index = ToIndex(localX, y, localZ);
            _blocks[index] = id;
            _metadata[index] = metadata;
            if (id == BlockId.Water)
            {
                _fluidLevels[index] = FluidLevel.Source;
            }
            else
            {
                _fluidLevels[index] = 0;
            }

            TrackBounds(y, id);

            if (markDirty)
            {
                IsMeshDirty = true;
            }
        }

        public void SetFluidLevel(int localX, int y, int localZ, byte level, bool markDirty = true)
        {
            if (!IsInside(localX, y, localZ))
            {
                return;
            }

            _fluidLevels[ToIndex(localX, y, localZ)] = FluidLevel.ClampFlow(level);
            if (markDirty)
            {
                IsMeshDirty = true;
            }
        }

        public void SetWater(int localX, int y, int localZ, byte level, bool markDirty = true)
        {
            if (!IsInside(localX, y, localZ))
            {
                return;
            }

            var index = ToIndex(localX, y, localZ);
            _blocks[index] = BlockId.Water;
            _fluidLevels[index] = FluidLevel.ClampFlow(level);
            TrackBounds(y, BlockId.Water);

            if (markDirty)
            {
                IsMeshDirty = true;
            }
        }

        public void FinishBulkFill()
        {
            IsMeshDirty = true;
        }

        public BiomeId GetBiome(int localX, int y, int localZ)
        {
            if (localX is < 0 or >= WorldConstants.ChunkSize ||
                localZ is < 0 or >= WorldConstants.ChunkSize ||
                y is < WorldConstants.MinY or > WorldConstants.MaxY)
            {
                return BiomeId.Unknown;
            }

            var quartX = localX >> 2;
            var quartZ = localZ >> 2;
            var quartY = (y - WorldConstants.MinY) >> 2;
            return _quartBiomes[ToQuartIndex(quartX, quartY, quartZ)];
        }

        public void SetQuartBiome(int quartX, int quartY, int quartZ, BiomeId biome)
        {
            if (quartX is < 0 or >= WorldConstants.BiomeQuartCountXZ ||
                quartZ is < 0 or >= WorldConstants.BiomeQuartCountXZ ||
                quartY is < 0 or >= WorldConstants.BiomeQuartCountY)
            {
                return;
            }

            _quartBiomes[ToQuartIndex(quartX, quartY, quartZ)] = biome;
        }

        public static int ToQuartIndex(int quartX, int quartY, int quartZ) =>
            quartY * WorldConstants.BiomeQuartCountXZ * WorldConstants.BiomeQuartCountXZ +
            quartZ * WorldConstants.BiomeQuartCountXZ +
            quartX;

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
