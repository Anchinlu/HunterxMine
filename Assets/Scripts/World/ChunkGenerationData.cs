using MineCraftUnity.World;
using System.Collections.Generic;
using MineCraftUnity.Blocks;
using MineCraftUnity.Core;

namespace MineCraftUnity.World
{
    public sealed class ChunkGenerationData
    {
        public ChunkPos Position { get; }
        public BlockId[] Blocks { get; }
        public byte[] Metadata { get; }
        public byte[] FluidLevels { get; }
        public BiomeId[] QuartBiomes { get; }
        public List<PendingBlock> Decorations { get; }
        
        public int MinFilledY { get; private set; } = WorldConstants.MaxY;
        public int MaxFilledY { get; private set; } = WorldConstants.MinY;

        public ChunkGenerationData(ChunkPos position)
        {
            Position = position;
            int volume = WorldConstants.ChunkSize * WorldConstants.Height * WorldConstants.ChunkSize;
            Blocks = new BlockId[volume];
            Metadata = new byte[volume];
            FluidLevels = new byte[volume];
            QuartBiomes = new BiomeId[WorldConstants.BiomeQuartVolume];
            Decorations = new List<PendingBlock>();
        }

        public void SetBlock(int localX, int y, int localZ, BlockId id, byte metadata = 0)
        {
            if (!Chunk.IsInside(localX, y, localZ))
            {
                return;
            }

            var index = Chunk.ToIndex(localX, y, localZ);
            Blocks[index] = id;
            Metadata[index] = metadata;
            if (id == BlockId.Water)
            {
                FluidLevels[index] = FluidLevel.Source;
            }
            else
            {
                FluidLevels[index] = 0;
            }

            TrackBounds(y, id);
        }

        public void SetQuartBiome(int quartX, int quartY, int quartZ, BiomeId biome)
        {
            if (quartX is < 0 or >= WorldConstants.BiomeQuartCountXZ ||
                quartZ is < 0 or >= WorldConstants.BiomeQuartCountXZ ||
                quartY is < 0 or >= WorldConstants.BiomeQuartCountY)
            {
                return;
            }

            QuartBiomes[Chunk.ToQuartIndex(quartX, quartY, quartZ)] = biome;
        }

        public BlockId GetBlock(int localX, int y, int localZ)
        {
            if (!Chunk.IsInside(localX, y, localZ))
            {
                return BlockId.Air;
            }
            return Blocks[Chunk.ToIndex(localX, y, localZ)];
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
            return QuartBiomes[Chunk.ToQuartIndex(quartX, quartY, quartZ)];
        }

        public void AddDecoration(PendingBlock block)
        {
            Decorations.Add(block);
        }

        private void TrackBounds(int y, BlockId id)
        {
            if (id == BlockId.Air)
            {
                return;
            }

            if (y < MinFilledY) MinFilledY = y;
            if (y > MaxFilledY) MaxFilledY = y;
        }
    }
}

