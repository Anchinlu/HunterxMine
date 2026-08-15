using System;
using MineCraftUnity.Blocks;
using MineCraftUnity.Core;
using MineCraftUnity.Rendering;
using MineCraftUnity.World;
using MineCraftUnity.WorldGen.Density;

namespace MineCraftUnity.WorldGen
{
    /// <summary>
    /// MC ref: NoiseBasedChunkGenerator.doFill — sloped cheese > 0 = stone, then surface rules.
    /// Uses terrain density (sloped cheese) for fill — faster and stable; cave carvers come later.
    /// </summary>
    public static class NoiseBasedChunkFiller
    {
        public static ChunkGenerationData ComputeChunkData(ChunkPos pos, RandomState randomState)
        {
            var data = new ChunkGenerationData(pos);
            using (ChunkProfilerMarkers.FillChunk.Auto())
            {
                var baseX = data.Position.GetMinBlockX();
                var baseZ = data.Position.GetMinBlockZ();

                BiomeVolumeFiller.FillChunkBiomes(data, randomState);

                for (var localX = 0; localX < WorldConstants.ChunkSize; localX++)
                {
                    for (var localZ = 0; localZ < WorldConstants.ChunkSize; localZ++)
                    {
                        var worldX = baseX + localX;
                        var worldZ = baseZ + localZ;
                        var columnCache = new DensityEvaluationCache(worldX, worldZ);
                        FillAndSurfaceColumn(data, randomState, columnCache, localX, localZ, worldX, worldZ);
                    }
                }

                VegetationPlacer.DecorateChunk(data);
            }
            return data;
        }

        private static void FillAndSurfaceColumn(
            ChunkGenerationData data,
            RandomState randomState,
            DensityEvaluationCache columnCache,
            int localX,
            int localZ,
            int worldX,
            int worldZ)
        {
            var surfaceHint = SampleSurfaceHint(randomState, worldX, worldZ, columnCache);
            var scanTop = Math.Max(surfaceHint, WorldConstants.SeaLevel);
            scanTop = Math.Min(scanTop + WorldConstants.SurfaceOverhangMargin, WorldConstants.MaxY);

            var topSolid = WorldConstants.MinY - 1;
            var bedrockTop = WorldConstants.MinY + WorldConstants.BedrockLayers - 1;
            var consecutiveAirAboveSolid = 0;

            for (var y = WorldConstants.MinY; y <= scanTop; y++)
            {
                var density = randomState.SampleTerrainDensity(worldX, y, worldZ, columnCache);
                if (density > 0.0)
                {
                    data.SetBlock(localX, y, localZ, BlockId.Stone);
                    topSolid = y;
                    consecutiveAirAboveSolid = 0;
                    continue;
                }

                if (topSolid > bedrockTop)
                {
                    consecutiveAirAboveSolid++;
                    if (consecutiveAirAboveSolid >= WorldConstants.SurfaceOverhangMargin)
                    {
                        break;
                    }
                }

                if (y <= WorldConstants.SeaLevel && y > bedrockTop)
                {
                    data.SetBlock(localX, y, localZ, BlockId.Water);
                }
            }

            ApplySurfaceForColumn(data, randomState, columnCache, localX, localZ, worldX, worldZ, topSolid);
        }

        private static void ApplySurfaceForColumn(
            ChunkGenerationData data,
            RandomState randomState,
            DensityEvaluationCache columnCache,
            int localX,
            int localZ,
            int worldX,
            int worldZ,
            int topSolid)
        {
            var bedrockTop = WorldConstants.MinY + WorldConstants.BedrockLayers - 1;
            for (var y = WorldConstants.MinY; y <= bedrockTop; y++)
            {
                data.SetBlock(localX, y, localZ, BlockId.Bedrock);
            }

            for (var y = bedrockTop + 1; y <= WorldConstants.SeaLevel; y++)
            {
                if (data.GetBlock(localX, y, localZ) == BlockId.Air)
                {
                    data.SetBlock(localX, y, localZ, BlockId.Water);
                }
            }

            if (topSolid <= bedrockTop)
            {
                return;
            }

            var biome = data.GetBiome(localX, topSolid, localZ);
            var surfaceContext = new SurfaceRuleApplier.ColumnContext(
                worldX, worldZ, topSolid, biome);
            var surfaceStart = SurfaceRuleApplier.GetSurfaceLayerStart(surfaceContext, topSolid, bedrockTop);

            for (var y = surfaceStart; y <= topSolid; y++)
            {
                var block = SurfaceRuleApplier.GetBlockForColumn(y, surfaceContext);
                if (block == BlockId.Air || block == BlockId.Stone)
                {
                    continue;
                }

                data.SetBlock(localX, y, localZ, block);
            }
        }

        public static int SampleSurfaceY(RandomState randomState, int worldX, int worldZ)
        {
            var columnCache = new DensityEvaluationCache(worldX, worldZ);
            var hint = SampleSurfaceHint(randomState, worldX, worldZ, columnCache);
            if (hint >= WorldConstants.SeaLevel - 8)
            {
                return hint;
            }

            for (var y = WorldConstants.MaxY; y >= WorldConstants.MinY; y--)
            {
                if (randomState.SampleTerrainDensity(worldX, y, worldZ, columnCache) > 0.0)
                {
                    return y;
                }
            }

            return WorldConstants.SeaLevel;
        }

        private static int SampleSurfaceHint(RandomState randomState, int worldX, int worldZ, DensityEvaluationCache columnCache)
        {
            columnCache.BeginSample();
            var ctx = new DensityContext
            {
                BlockX = worldX,
                BlockY = 0,
                BlockZ = worldZ,
                Cache = columnCache
            };
            var preliminary = randomState.Router.PreliminarySurfaceLevel.Compute(in ctx);
            if (!double.IsNaN(preliminary) && !double.IsInfinity(preliminary))
            {
                return (int)Math.Clamp(Math.Floor(preliminary), WorldConstants.MinY, WorldConstants.MaxY);
            }

            return WorldConstants.MinY - 1;
        }
    }
}
