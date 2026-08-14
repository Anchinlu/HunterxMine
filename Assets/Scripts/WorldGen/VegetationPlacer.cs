using MineCraftUnity.Blocks;
using MineCraftUnity.Core;
using MineCraftUnity.World;

namespace MineCraftUnity.WorldGen
{
    /// <summary>
    /// MC ref: placed features — short grass, fern, flowers, simple trees per biome.
    /// </summary>
    public static class VegetationPlacer
    {
        public static void DecorateChunk(World.Chunk chunk, RandomState randomState)
        {
            var baseX = chunk.Position.GetMinBlockX();
            var baseZ = chunk.Position.GetMinBlockZ();

            for (var localX = 0; localX < WorldConstants.ChunkSize; localX++)
            {
                for (var localZ = 0; localZ < WorldConstants.ChunkSize; localZ++)
                {
                    if (!TryFindPlantSurface(chunk, localX, localZ, out var surfaceY))
                    {
                        continue;
                    }

                    var worldX = baseX + localX;
                    var worldZ = baseZ + localZ;
                    var biome = chunk.GetBiome(localX, surfaceY, localZ);
                    if (!BiomeRegistry.SupportsSurfaceVegetation(biome))
                    {
                        continue;
                    }

                    if (chunk.GetBlock(localX, surfaceY, localZ) != BlockId.GrassBlock)
                    {
                        continue;
                    }

                    var plantY = surfaceY + 1;
                    if (chunk.GetBlock(localX, plantY, localZ) != BlockId.Air)
                    {
                        continue;
                    }

                    var roll = Hash01(randomState.Seed, worldX, plantY, worldZ);

                    if (TryPlaceTree(chunk, randomState, localX, surfaceY, localZ, biome, roll))
                    {
                        continue;
                    }

                    TryPlaceGroundCover(chunk, localX, plantY, localZ, biome, roll, worldX, worldZ, randomState.Seed);
                }
            }
        }

        private static bool TryFindPlantSurface(World.Chunk chunk, int localX, int localZ, out int surfaceY)
        {
            for (var y = chunk.MaxFilledY; y >= chunk.MinFilledY; y--)
            {
                var block = chunk.GetBlock(localX, y, localZ);
                if (block == BlockId.Air || BlockRegistry.IsFluid(block))
                {
                    continue;
                }

                surfaceY = y;
                return true;
            }

            surfaceY = 0;
            return false;
        }

        private static void TryPlaceGroundCover(
            World.Chunk chunk,
            int localX,
            int plantY,
            int localZ,
            BiomeId biome,
            float roll,
            int worldX,
            int worldZ,
            int seed)
        {
            if (BiomeRegistry.PrefersFern(biome))
            {
                if (roll < 0.18f)
                {
                    chunk.SetBlock(localX, plantY, localZ, BlockId.Fern, markDirty: false);
                }

                return;
            }

            var flowerRoll = Hash01(seed, worldX + 17, plantY, worldZ + 31);
            if (TryPlaceFlower(chunk, localX, plantY, localZ, biome, flowerRoll, seed, worldX, worldZ))
            {
                return;
            }

            var grassChance = biome switch
            {
                BiomeId.FlowerForest or BiomeId.Meadow => 0.12f,
                BiomeId.Plains or BiomeId.SunflowerPlains => 0.22f,
                BiomeId.Forest or BiomeId.BirchForest or BiomeId.Taiga => 0.28f,
                BiomeId.Jungle or BiomeId.SparseJungle or BiomeId.BambooJungle => 0.35f,
                _ => 0.18f
            };

            if (roll < grassChance)
            {
                chunk.SetBlock(localX, plantY, localZ, BlockId.ShortGrass, markDirty: false);
            }
        }

        private static bool TryPlaceFlower(
            World.Chunk chunk,
            int localX,
            int plantY,
            int localZ,
            BiomeId biome,
            float roll,
            int seed,
            int worldX,
            int worldZ)
        {
            var density = biome switch
            {
                BiomeId.FlowerForest => 0.32f,
                BiomeId.Meadow or BiomeId.SunflowerPlains => 0.14f,
                BiomeId.Plains or BiomeId.Forest => 0.04f,
                _ => 0f
            };

            if (roll >= density)
            {
                return false;
            }

            var speciesRoll = Hash01(seed, worldX + 41, plantY, worldZ + 59);
            var flower = speciesRoll switch
            {
                < 0.5f => BlockId.Dandelion,
                _ => BlockId.Poppy
            };
            chunk.SetBlock(localX, plantY, localZ, flower, markDirty: false);
            return true;
        }

        private static bool TryPlaceTree(
            World.Chunk chunk,
            RandomState randomState,
            int localX,
            int surfaceY,
            int localZ,
            BiomeId biome,
            float roll)
        {
            var treeChance = biome switch
            {
                BiomeId.Forest or BiomeId.FlowerForest => 0.025f,
                BiomeId.DarkForest => 0.04f,
                BiomeId.BirchForest or BiomeId.OldGrowthBirchForest => 0.028f,
                BiomeId.Taiga or BiomeId.OldGrowthSpruceTaiga or BiomeId.OldGrowthPineTaiga => 0.022f,
                BiomeId.Plains => 0.004f,
                BiomeId.Jungle or BiomeId.SparseJungle or BiomeId.BambooJungle => 0.03f,
                BiomeId.Savanna or BiomeId.SavannaPlateau or BiomeId.WindsweptSavanna => 0.02f,
                BiomeId.CherryGrove => 0.025f,
                BiomeId.MangroveSwamp => 0.035f,
                BiomeId.PaleGarden => 0.025f,
                _ => 0f
            };

            if (roll >= treeChance)
            {
                return false;
            }

            PlaceSimpleTree(chunk, localX, surfaceY + 1, localZ, biome, randomState.Seed);
            return true;
        }

        private static void PlaceSimpleTree(World.Chunk chunk, int localX, int baseY, int localZ, BiomeId biome, int seed)
        {
            var height = 4 + (Hash01(seed, localX, baseY, localZ) > 0.5f ? 1 : 0);
            var leaves = BiomeRegistry.GetLeavesBlock(biome);
            var log = BiomeRegistry.GetLogBlock(biome);

            for (var dy = 0; dy < height; dy++)
            {
                var y = baseY + dy;
                if (!World.Chunk.IsInside(localX, y, localZ))
                {
                    return;
                }

                if (chunk.GetBlock(localX, y, localZ) == BlockId.Air)
                {
                    chunk.SetBlock(localX, y, localZ, log, markDirty: false);
                }
            }

            var crownY = baseY + height;
            for (var dx = -2; dx <= 2; dx++)
            {
                for (var dz = -2; dz <= 2; dz++)
                {
                    for (var dy = -1; dy <= 2; dy++)
                    {
                        if (System.Math.Abs(dx) == 2 && System.Math.Abs(dz) == 2 && dy < 1)
                        {
                            continue;
                        }

                        var lx = localX + dx;
                        var ly = crownY + dy;
                        var lz = localZ + dz;
                        if (!World.Chunk.IsInside(lx, ly, lz))
                        {
                            continue;
                        }

                        if (chunk.GetBlock(lx, ly, lz) == BlockId.Air)
                        {
                            chunk.SetBlock(lx, ly, lz, leaves, markDirty: false);
                        }
                    }
                }
            }
        }

        private static float Hash01(int seed, int x, int y, int z)
        {
            unchecked
            {
                var h = seed;
                h = h * 31 + x * 734287;
                h = h * 31 + y * 912271;
                h = h * 31 + z * 438289;
                h ^= h >> 13;
                h *= 1274126177;
                return (h & 0x7FFFFFFF) / (float)int.MaxValue;
            }
        }
    }
}
