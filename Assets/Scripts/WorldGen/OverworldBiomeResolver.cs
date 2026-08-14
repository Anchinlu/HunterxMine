using MineCraftUnity.World;

namespace MineCraftUnity.WorldGen
{
    /// <summary>
    /// MC ref: OverworldBiomeBuilder — simplified decision tree using the same parameter thresholds and biome tables.
    /// </summary>
    public static class OverworldBiomeResolver
    {
        private static readonly BiomeId[] DeepOceans =
        {
            BiomeId.DeepFrozenOcean,
            BiomeId.DeepColdOcean,
            BiomeId.DeepOcean,
            BiomeId.DeepLukewarmOcean,
            BiomeId.DeepWarmOcean
        };

        private static readonly BiomeId[] Oceans =
        {
            BiomeId.FrozenOcean,
            BiomeId.ColdOcean,
            BiomeId.Ocean,
            BiomeId.LukewarmOcean,
            BiomeId.WarmOcean
        };

        private static readonly BiomeId[][] MiddleBiomes =
        {
            new[] { BiomeId.SnowyPlains, BiomeId.SnowyPlains, BiomeId.SnowyPlains, BiomeId.SnowyTaiga, BiomeId.Taiga },
            new[] { BiomeId.Plains, BiomeId.Plains, BiomeId.Forest, BiomeId.Taiga, BiomeId.OldGrowthSpruceTaiga },
            new[] { BiomeId.FlowerForest, BiomeId.Plains, BiomeId.Forest, BiomeId.BirchForest, BiomeId.DarkForest },
            new[] { BiomeId.Savanna, BiomeId.Savanna, BiomeId.Forest, BiomeId.Jungle, BiomeId.Jungle },
            new[] { BiomeId.Desert, BiomeId.Desert, BiomeId.Desert, BiomeId.Desert, BiomeId.Desert }
        };

        private static readonly BiomeId?[][] MiddleBiomeVariants =
        {
            new BiomeId?[] { BiomeId.IceSpikes, null, BiomeId.SnowyTaiga, null, null },
            new BiomeId?[] { null, null, null, null, BiomeId.OldGrowthPineTaiga },
            new BiomeId?[] { BiomeId.SunflowerPlains, null, null, BiomeId.OldGrowthBirchForest, null },
            new BiomeId?[] { null, null, BiomeId.Plains, BiomeId.SparseJungle, BiomeId.BambooJungle },
            new BiomeId?[] { null, null, null, null, null }
        };

        private static readonly BiomeId[][] PlateauBiomes =
        {
            new[] { BiomeId.SnowyPlains, BiomeId.SnowyPlains, BiomeId.SnowyPlains, BiomeId.SnowyTaiga, BiomeId.SnowyTaiga },
            new[] { BiomeId.Meadow, BiomeId.Meadow, BiomeId.Forest, BiomeId.Taiga, BiomeId.OldGrowthSpruceTaiga },
            new[] { BiomeId.Meadow, BiomeId.Meadow, BiomeId.Meadow, BiomeId.Meadow, BiomeId.PaleGarden },
            new[] { BiomeId.SavannaPlateau, BiomeId.SavannaPlateau, BiomeId.Forest, BiomeId.Forest, BiomeId.Jungle },
            new[] { BiomeId.Badlands, BiomeId.Badlands, BiomeId.Badlands, BiomeId.WoodedBadlands, BiomeId.WoodedBadlands }
        };

        private static readonly BiomeId?[][] PlateauBiomeVariants =
        {
            new BiomeId?[] { BiomeId.IceSpikes, null, null, null, null },
            new BiomeId?[] { BiomeId.CherryGrove, null, BiomeId.Meadow, BiomeId.Meadow, BiomeId.OldGrowthPineTaiga },
            new BiomeId?[] { BiomeId.CherryGrove, BiomeId.CherryGrove, BiomeId.Forest, BiomeId.BirchForest, null },
            new BiomeId?[] { null, null, null, null, null },
            new BiomeId?[] { BiomeId.ErodedBadlands, BiomeId.ErodedBadlands, null, null, null }
        };

        private static readonly BiomeId?[][] ShatteredBiomes =
        {
            new BiomeId?[]
            {
                BiomeId.WindsweptGravellyHills, BiomeId.WindsweptGravellyHills, BiomeId.WindsweptHills, BiomeId.WindsweptForest,
                BiomeId.WindsweptForest
            },
            new BiomeId?[]
            {
                BiomeId.WindsweptGravellyHills, BiomeId.WindsweptGravellyHills, BiomeId.WindsweptHills, BiomeId.WindsweptForest,
                BiomeId.WindsweptForest
            },
            new BiomeId?[]
            {
                BiomeId.WindsweptHills, BiomeId.WindsweptHills, BiomeId.WindsweptHills, BiomeId.WindsweptForest,
                BiomeId.WindsweptForest
            },
            new BiomeId?[] { null, null, null, null, null },
            new BiomeId?[] { null, null, null, null, null }
        };

        public static BiomeId Resolve(in ClimateSample sample) =>
            Resolve(
                sample.Temperature,
                sample.Humidity,
                sample.Continental,
                sample.Erosion,
                sample.Weirdness);

        public static BiomeId Resolve(
            float temperature,
            float humidity,
            float continental,
            float erosion,
            float weirdness)
        {
            var temperatureIndex = TemperatureIndex(temperature);
            var humidityIndex = HumidityIndex(humidity);
            var weirdPositive = weirdness >= 0f;

            if (continental < -1.05f)
            {
                return BiomeId.MushroomFields;
            }

            if (continental < -0.455f)
            {
                return DeepOceans[temperatureIndex];
            }

            if (continental < -0.19f)
            {
                return Oceans[temperatureIndex];
            }

            if (continental < -0.11f)
            {
                return ResolveCoastBiome(temperatureIndex, humidityIndex, continental, erosion, weirdPositive);
            }

            return ResolveInlandBiome(temperatureIndex, humidityIndex, continental, erosion, weirdness, weirdPositive);
        }

        /// <summary>MC ref: TerrainProvider.PeaksAndValleys — river valleys sit on the high weirdness slice.</summary>
        private static bool IsRiverValley(float weirdness) =>
            System.Math.Abs(System.Math.Abs(weirdness) - 0.6666667f) < 0.12f;

        private static bool ShouldPickRiver(float continental, float erosion, float weirdness) =>
            continental >= -0.11f
            && continental < 0.03f
            && erosion > -0.78f
            && erosion <= -0.375f
            && IsRiverValley(weirdness);

        private static BiomeId ResolveCoastBiome(
            int temperatureIndex,
            int humidityIndex,
            float continental,
            float erosion,
            bool weirdPositive)
        {
            if (erosion < -0.375f)
            {
                return BiomeId.StonyShore;
            }

            if (erosion > 0.55f && continental > -0.11f + 0.03f)
            {
                if (temperatureIndex is >= 1 and <= 2)
                {
                    return BiomeId.Swamp;
                }

                if (temperatureIndex is >= 3 and <= 4)
                {
                    return BiomeId.MangroveSwamp;
                }
            }

            if (erosion >= 0.45f)
            {
                return PickShatteredCoastBiome(temperatureIndex, humidityIndex, weirdPositive);
            }

            return PickBeachBiome(temperatureIndex);
        }

        private static BiomeId ResolveInlandBiome(
            int temperatureIndex,
            int humidityIndex,
            float continental,
            float erosion,
            float weirdness,
            bool weirdPositive)
        {
            if (ShouldPickRiver(continental, erosion, weirdness))
            {
                return BiomeId.River;
            }

            if (erosion < -0.78f)
            {
                return PickPeakBiome(temperatureIndex, humidityIndex, weirdPositive);
            }

            if (erosion < -0.375f)
            {
                if (continental < 0.03f)
                {
                    return PickMiddleBiomeOrBadlandsIfHot(temperatureIndex, humidityIndex, weirdPositive);
                }

                return PickSlopeBiome(temperatureIndex, humidityIndex, weirdPositive);
            }

            if (erosion < -0.2225f)
            {
                if (continental >= 0.3f)
                {
                    return PickPlateauBiome(temperatureIndex, humidityIndex, weirdPositive);
                }

                return PickMiddleBiomeOrBadlandsIfHotOrSlopeIfCold(temperatureIndex, humidityIndex, weirdPositive);
            }

            if (erosion < 0.05f)
            {
                if (continental >= 0.3f && erosion >= -0.2225f)
                {
                    return PickPlateauBiome(temperatureIndex, humidityIndex, weirdPositive);
                }

                if (continental >= 0.03f && continental < 0.3f && erosion >= -0.2225f)
                {
                    return PickMiddleBiomeOrBadlandsIfHot(temperatureIndex, humidityIndex, weirdPositive);
                }

                return PickMiddleBiomeOrBadlandsIfHot(temperatureIndex, humidityIndex, weirdPositive);
            }

            if (erosion < 0.45f)
            {
                return PickMiddleBiomeOrBadlandsIfHot(temperatureIndex, humidityIndex, weirdPositive);
            }

            if (erosion < 0.55f)
            {
                if (continental < 0.03f)
                {
                    return PickShatteredCoastBiome(temperatureIndex, humidityIndex, weirdPositive);
                }

                return MaybePickWindsweptSavanna(temperatureIndex, humidityIndex, weirdPositive,
                    PickShatteredBiome(temperatureIndex, humidityIndex, weirdPositive));
            }

            return PickMiddleBiomeOrBadlandsIfHot(temperatureIndex, humidityIndex, weirdPositive);
        }

        private static int TemperatureIndex(float temperature)
        {
            if (temperature < -0.45f)
            {
                return 0;
            }

            if (temperature < -0.15f)
            {
                return 1;
            }

            if (temperature < 0.2f)
            {
                return 2;
            }

            if (temperature < 0.55f)
            {
                return 3;
            }

            return 4;
        }

        private static int HumidityIndex(float humidity)
        {
            if (humidity < -0.35f)
            {
                return 0;
            }

            if (humidity < -0.1f)
            {
                return 1;
            }

            if (humidity < 0.1f)
            {
                return 2;
            }

            if (humidity < 0.3f)
            {
                return 3;
            }

            return 4;
        }

        private static BiomeId PickMiddleBiome(int temperatureIndex, int humidityIndex, bool weirdPositive)
        {
            if (!weirdPositive)
            {
                return MiddleBiomes[temperatureIndex][humidityIndex];
            }

            var variant = MiddleBiomeVariants[temperatureIndex][humidityIndex];
            return variant ?? MiddleBiomes[temperatureIndex][humidityIndex];
        }

        private static BiomeId PickMiddleBiomeOrBadlandsIfHot(int temperatureIndex, int humidityIndex, bool weirdPositive) =>
            temperatureIndex == 4
                ? PickBadlandsBiome(humidityIndex, weirdPositive)
                : PickMiddleBiome(temperatureIndex, humidityIndex, weirdPositive);

        private static BiomeId PickMiddleBiomeOrBadlandsIfHotOrSlopeIfCold(
            int temperatureIndex,
            int humidityIndex,
            bool weirdPositive) =>
            temperatureIndex == 0
                ? PickSlopeBiome(temperatureIndex, humidityIndex, weirdPositive)
                : PickMiddleBiomeOrBadlandsIfHot(temperatureIndex, humidityIndex, weirdPositive);

        private static BiomeId MaybePickWindsweptSavanna(
            int temperatureIndex,
            int humidityIndex,
            bool weirdPositive,
            BiomeId underlying) =>
            temperatureIndex > 1 && humidityIndex < 4 && weirdPositive
                ? BiomeId.WindsweptSavanna
                : underlying;

        private static BiomeId PickShatteredCoastBiome(int temperatureIndex, int humidityIndex, bool weirdPositive)
        {
            var beachOrMiddle = weirdPositive
                ? PickMiddleBiome(temperatureIndex, humidityIndex, weirdPositive)
                : PickBeachBiome(temperatureIndex);
            return MaybePickWindsweptSavanna(temperatureIndex, humidityIndex, weirdPositive, beachOrMiddle);
        }

        private static BiomeId PickBeachBiome(int temperatureIndex) =>
            temperatureIndex switch
            {
                0 => BiomeId.SnowyBeach,
                4 => BiomeId.Desert,
                _ => BiomeId.Beach
            };

        private static BiomeId PickBadlandsBiome(int humidityIndex, bool weirdPositive)
        {
            if (humidityIndex < 2)
            {
                return weirdPositive ? BiomeId.ErodedBadlands : BiomeId.Badlands;
            }

            return humidityIndex < 3 ? BiomeId.Badlands : BiomeId.WoodedBadlands;
        }

        private static BiomeId PickPlateauBiome(int temperatureIndex, int humidityIndex, bool weirdPositive)
        {
            if (weirdPositive)
            {
                var variant = PlateauBiomeVariants[temperatureIndex][humidityIndex];
                if (variant.HasValue)
                {
                    return variant.Value;
                }
            }

            return PlateauBiomes[temperatureIndex][humidityIndex];
        }

        private static BiomeId PickPeakBiome(int temperatureIndex, int humidityIndex, bool weirdPositive)
        {
            if (temperatureIndex <= 2)
            {
                return weirdPositive ? BiomeId.FrozenPeaks : BiomeId.JaggedPeaks;
            }

            return temperatureIndex == 3 ? BiomeId.StonyPeaks : PickBadlandsBiome(humidityIndex, weirdPositive);
        }

        private static BiomeId PickSlopeBiome(int temperatureIndex, int humidityIndex, bool weirdPositive) =>
            temperatureIndex >= 3
                ? PickPlateauBiome(temperatureIndex, humidityIndex, weirdPositive)
                : humidityIndex <= 1 ? BiomeId.SnowySlopes : BiomeId.Grove;

        private static BiomeId PickShatteredBiome(int temperatureIndex, int humidityIndex, bool weirdPositive)
        {
            var biome = ShatteredBiomes[temperatureIndex][humidityIndex];
            return biome ?? PickMiddleBiome(temperatureIndex, humidityIndex, weirdPositive);
        }
    }
}
