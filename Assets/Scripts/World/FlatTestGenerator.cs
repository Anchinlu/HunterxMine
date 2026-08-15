using System.Collections.Generic;
using MineCraftUnity.Blocks;
using MineCraftUnity.Core;

namespace MineCraftUnity.World
{
    public class FlatTestGenerator : IChunkGenerator
    {
        public void GenerateChunk(Level level, Chunk chunk, HashSet<ChunkPos> changedChunks)
        {
            for (int qx = 0; qx < WorldConstants.BiomeQuartCountXZ; qx++)
            {
                for (int qy = 0; qy < WorldConstants.BiomeQuartCountY; qy++)
                {
                    for (int qz = 0; qz < WorldConstants.BiomeQuartCountXZ; qz++)
                    {
                        chunk.SetQuartBiome(qx, qy, qz, BiomeId.Plains);
                    }
                }
            }

            for (int x = 0; x < WorldConstants.ChunkSize; x++)
            {
                for (int z = 0; z < WorldConstants.ChunkSize; z++)
                {
                    for (int y = 59; y <= 63; y++)
                    {
                        if (y == 59)
                        {
                            chunk.SetBlock(x, y, z, BlockId.Bedrock);
                        }
                        else if (y >= 60 && y <= 62)
                        {
                            chunk.SetBlock(x, y, z, BlockId.Dirt);
                        }
                        else if (y == 63)
                        {
                            chunk.SetBlock(x, y, z, BlockId.GrassBlock);
                        }
                    }
                }
            }
            chunk.MarkGenerated();
        }

        public int SampleSurfaceHeight(int globalX, int globalZ)
        {
            return 63;
        }

        public BiomeId GetBiome(int globalX, int globalZ)
        {
            return BiomeId.Plains;
        }
    }
}
