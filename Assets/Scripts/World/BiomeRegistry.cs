using MineCraftUnity.Blocks;
using UnityEngine;

namespace MineCraftUnity.World
{
    /// <summary>
    /// MC ref: biome JSON + SurfaceRuleData — colors from datapack only.
    /// </summary>
    public static class BiomeRegistry
    {
        private static readonly Color DefaultWaterColor = BiomeJsonParser.ParseHexColor("#3f76e4");
        private static readonly Color DefaultGrassColor = BiomeJsonParser.ParseHexColor("#91bd59");

        public static void EnsureLoaded() => BiomeDatapackLoader.EnsureLoaded();

        public static string GetDisplayName(BiomeId id) =>
            id switch
            {
                BiomeId.MushroomFields => "Mushroom Fields",
                BiomeId.DeepFrozenOcean => "Deep Frozen Ocean",
                BiomeId.DeepColdOcean => "Deep Cold Ocean",
                BiomeId.DeepOcean => "Deep Ocean",
                BiomeId.DeepLukewarmOcean => "Deep Lukewarm Ocean",
                BiomeId.DeepWarmOcean => "Deep Warm Ocean",
                BiomeId.FrozenOcean => "Frozen Ocean",
                BiomeId.ColdOcean => "Cold Ocean",
                BiomeId.Ocean => "Ocean",
                BiomeId.LukewarmOcean => "Lukewarm Ocean",
                BiomeId.WarmOcean => "Warm Ocean",
                BiomeId.StonyShore => "Stony Shore",
                BiomeId.Beach => "Beach",
                BiomeId.SnowyBeach => "Snowy Beach",
                BiomeId.Swamp => "Swamp",
                BiomeId.MangroveSwamp => "Mangrove Swamp",
                BiomeId.SnowyPlains => "Snowy Plains",
                BiomeId.IceSpikes => "Ice Spikes",
                BiomeId.SnowyTaiga => "Snowy Taiga",
                BiomeId.Plains => "Plains",
                BiomeId.SunflowerPlains => "Sunflower Plains",
                BiomeId.Forest => "Forest",
                BiomeId.FlowerForest => "Flower Forest",
                BiomeId.BirchForest => "Birch Forest",
                BiomeId.OldGrowthBirchForest => "Old Growth Birch Forest",
                BiomeId.DarkForest => "Dark Forest",
                BiomeId.Taiga => "Taiga",
                BiomeId.OldGrowthSpruceTaiga => "Old Growth Spruce Taiga",
                BiomeId.OldGrowthPineTaiga => "Old Growth Pine Taiga",
                BiomeId.Meadow => "Meadow",
                BiomeId.CherryGrove => "Cherry Grove",
                BiomeId.Savanna => "Savanna",
                BiomeId.SavannaPlateau => "Savanna Plateau",
                BiomeId.WindsweptSavanna => "Windswept Savanna",
                BiomeId.Jungle => "Jungle",
                BiomeId.SparseJungle => "Sparse Jungle",
                BiomeId.BambooJungle => "Bamboo Jungle",
                BiomeId.Desert => "Desert",
                BiomeId.Badlands => "Badlands",
                BiomeId.ErodedBadlands => "Eroded Badlands",
                BiomeId.WoodedBadlands => "Wooded Badlands",
                BiomeId.SnowySlopes => "Snowy Slopes",
                BiomeId.Grove => "Grove",
                BiomeId.JaggedPeaks => "Jagged Peaks",
                BiomeId.FrozenPeaks => "Frozen Peaks",
                BiomeId.StonyPeaks => "Stony Peaks",
                BiomeId.WindsweptHills => "Windswept Hills",
                BiomeId.WindsweptGravellyHills => "Windswept Gravelly Hills",
                BiomeId.WindsweptForest => "Windswept Forest",
                BiomeId.PaleGarden => "Pale Garden",
                BiomeId.River => "River",
                _ => "Unknown"
            };

        /// <summary>MC grass tint from JSON grass_color, modifier, or temperature colormap.</summary>
        public static Color GetGrassTint(BiomeId id)
        {
            EnsureLoaded();
            if (BiomeDatapackLoader.TryGetDefinition(id, out var def))
            {
                if (def.GrassColor.HasValue)
                {
                    return def.GrassColor.Value;
                }

                if (!string.IsNullOrEmpty(def.GrassColorModifier))
                {
                    return GetModifiedGrassColor(def.GrassColorModifier, def);
                }

                return ComputeGrassColorFromClimate(def.Temperature, def.Downfall);
            }

            return DefaultGrassColor;
        }

        /// <summary>MC effects.water_color RGB (fog / future use — water mesh uses material tint).</summary>
        public static Color GetWaterColor(BiomeId id)
        {
            EnsureLoaded();
            if (BiomeDatapackLoader.TryGetDefinition(id, out var def))
            {
                return def.WaterColor;
            }

            return DefaultWaterColor;
        }

        /// <summary>MC attributes minecraft:visual/water_fog_color, or derived from water_color.</summary>
        public static Color GetWaterFogColor(BiomeId id)
        {
            EnsureLoaded();
            if (BiomeDatapackLoader.TryGetDefinition(id, out var def))
            {
                if (def.WaterFogColor.HasValue)
                {
                    return def.WaterFogColor.Value;
                }

                var water = def.WaterColor;
                return new Color(water.r * 0.35f, water.g * 0.35f, water.b * 0.35f, 1f);
            }

            return new Color(0.05f, 0.14f, 0.32f, 1f);
        }

        public static bool UsesWarmOceanFloor(BiomeId id) =>
            id is BiomeId.WarmOcean or BiomeId.LukewarmOcean or BiomeId.DeepLukewarmOcean or BiomeId.DeepWarmOcean;

        public static bool IsSandyLand(BiomeId id) =>
            id is BiomeId.Beach or BiomeId.SnowyBeach or BiomeId.Desert or BiomeId.Badlands or BiomeId.ErodedBadlands or BiomeId.WoodedBadlands;

        public static bool IsGravelLand(BiomeId id) =>
            id is BiomeId.WindsweptGravellyHills or BiomeId.WindsweptHills;

        public static bool IsStoneShore(BiomeId id) => id == BiomeId.StonyShore;

        public static BlockId GetTopSurfaceBlock(BiomeId biome, bool isUnderwater, bool isShallowUnderwater)
        {
            // MC SurfaceRuleData STONY_SHORE — default STONE (gravel only in narrow noise band).
            if (IsStoneShore(biome))
            {
                return BlockId.Stone;
            }

            if (isUnderwater)
            {
                return UsesWarmOceanFloor(biome) ? BlockId.Sand : BlockId.Gravel;
            }

            if (IsSandyLand(biome))
            {
                return BlockId.Sand;
            }

            if (IsGravelLand(biome))
            {
                return BlockId.Gravel;
            }

            return BlockId.GrassBlock;
        }

        public static BlockId GetSubsurfaceBlock(BiomeId biome, bool isUnderwater, bool isShallowUnderwater)
        {
            if (IsStoneShore(biome))
            {
                return BlockId.Stone;
            }

            if (!isUnderwater)
            {
                return BlockId.Dirt;
            }

            if (!isShallowUnderwater)
            {
                return BlockId.Stone;
            }

            return UsesWarmOceanFloor(biome) ? BlockId.Sand : BlockId.Dirt;
        }

        private static Color GetModifiedGrassColor(string modifier, BiomeDefinition def) =>
            modifier switch
            {
                "swamp" => def.FoliageColor ?? BiomeJsonParser.ParseHexColor("#6a7039"),
                "dark_forest" => BiomeJsonParser.ParseHexColor("#507a32"),
                "badlands" => BiomeJsonParser.ParseHexColor("#90814d"),
                "cherry_grove" => BiomeJsonParser.ParseHexColor("#b7db87"),
                _ => ComputeGrassColorFromClimate(def.Temperature, def.Downfall)
            };

        /// <summary>Simplified MC grass colormap from temperature + downfall.</summary>
        private static Color ComputeGrassColorFromClimate(float temperature, float downfall)
        {
            var temp = Mathf.Clamp01((temperature + 1f) * 0.5f);
            var rain = Mathf.Clamp01(downfall);
            var baseColor = Color.Lerp(
                BiomeJsonParser.ParseHexColor("#8eb971"),
                BiomeJsonParser.ParseHexColor("#bfba40"),
                temp);
            return Color.Lerp(baseColor, BiomeJsonParser.ParseHexColor("#bfb755"), 1f - rain);
        }
    }
}
