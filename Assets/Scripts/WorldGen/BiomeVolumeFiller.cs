using MineCraftUnity.World;
using MineCraftUnity.Core;
using MineCraftUnity.WorldGen;
using MineCraftUnity.WorldGen.Density;

namespace MineCraftUnity.WorldGen
{
    /// <summary>
    /// MC ref: MultiNoiseBiomeSource — fill 4×4×4 quart biome grid for a chunk.
    /// </summary>
    public static class BiomeVolumeFiller
    {
        public static void FillChunkBiomes(ChunkGenerationData data, RandomState randomState)
        {
            var baseX = data.Position.GetMinBlockX();
            var baseZ = data.Position.GetMinBlockZ();

            for (var quartX = 0; quartX < WorldConstants.BiomeQuartCountXZ; quartX++)
            {
                for (var quartZ = 0; quartZ < WorldConstants.BiomeQuartCountXZ; quartZ++)
                {
                    var worldX = baseX + (quartX << 2) + 2;
                    var worldZ = baseZ + (quartZ << 2) + 2;
                    var columnCache = new DensityEvaluationCache(worldX, worldZ);

                    for (var quartY = 0; quartY < WorldConstants.BiomeQuartCountY; quartY++)
                    {
                        var blockY = WorldConstants.MinY + (quartY << 2) + 2;
                        var climate = randomState.SampleClimate(worldX, blockY, worldZ, columnCache);
                        data.SetQuartBiome(quartX, quartY, quartZ, OverworldBiomeResolver.Resolve(climate));
                    }
                }
            }
        }
    }
}

