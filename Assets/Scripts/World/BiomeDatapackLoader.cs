using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using MineCraftUnity.WorldGen;
using UnityEngine;

namespace MineCraftUnity.World
{
    /// <summary>
    /// MC ref: data/minecraft/worldgen/biome/*.json from minecraft-assets-26.2 datapack.
    /// </summary>
    public static class BiomeDatapackLoader
    {
        private static readonly ConcurrentDictionary<BiomeId, BiomeDefinition> Definitions = new();
        private static bool _loaded;
        private static readonly object _loadLock = new();

        public static bool IsLoaded => _loaded;

        public static void EnsureLoaded()
        {
            if (_loaded) return;

            lock (_loadLock)
            {
                if (_loaded) return;

                LoadFromDirectory(WorldGenDataPaths.BiomeDirectory);
                ApplyAliasDefaults();
                _loaded = true;
            }
        }

        public static bool TryGetDefinition(BiomeId id, out BiomeDefinition definition) =>
            Definitions.TryGetValue(id, out definition);

        public static IReadOnlyDictionary<BiomeId, BiomeDefinition> AllDefinitions => Definitions;

        private static void LoadFromDirectory(string directory)
        {
            Definitions.Clear();

            if (WorldGenDataPaths.UseResources)
            {
                LoadFromResources();
                return;
            }

            if (!Directory.Exists(directory))
            {
                Debug.LogWarning($"[BiomeDatapackLoader] Biome directory not found: {directory}");
                CreateFallbackDefinitions();
                return;
            }

            var loadedCount = 0;
            foreach (var file in Directory.EnumerateFiles(directory, "*.json"))
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                if (!BiomeJsonParser.TryParseIdFromFileName(fileName, out var id))
                {
                    continue;
                }

                try
                {
                    var json = File.ReadAllText(file);
                    Definitions[id] = BiomeJsonParser.Parse(id, json);
                    loadedCount++;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[BiomeDatapackLoader] Failed to load {file}: {ex.Message}");
                }
            }

            if (loadedCount == 0)
            {
                Debug.LogWarning("[BiomeDatapackLoader] No biome JSON loaded — using built-in fallbacks.");
                CreateFallbackDefinitions();
                return;
            }

            Debug.Log($"[BiomeDatapackLoader] Loaded {loadedCount} biomes from datapack.");
        }

        private static void LoadFromResources()
        {
            var assets = UnityEngine.Resources.LoadAll<UnityEngine.TextAsset>(
                $"{WorldGenDataPaths.ResourceRoot}/biome");

            if (assets == null || assets.Length == 0)
            {
                Debug.LogWarning("[BiomeDatapackLoader] No biome TextAssets found in Resources — using fallbacks.");
                CreateFallbackDefinitions();
                return;
            }

            var loadedCount = 0;
            foreach (var asset in assets)
            {
                if (!BiomeJsonParser.TryParseIdFromFileName(asset.name, out var id))
                {
                    continue;
                }

                try
                {
                    Definitions[id] = BiomeJsonParser.Parse(id, asset.text);
                    loadedCount++;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[BiomeDatapackLoader] Failed to parse {asset.name}: {ex.Message}");
                }
            }

            if (loadedCount == 0)
            {
                Debug.LogWarning("[BiomeDatapackLoader] Parsed 0 biome definitions from Resources — using fallbacks.");
                CreateFallbackDefinitions();
                return;
            }

            Debug.Log($"[BiomeDatapackLoader] Loaded {loadedCount} biomes from Resources.");
        }

        private static void ApplyAliasDefaults()
        {
            CopyDefinition(BiomeId.WarmOcean, BiomeId.DeepWarmOcean);
            CopyDefinition(BiomeId.Ocean, BiomeId.DeepOcean);
            CopyDefinition(BiomeId.FrozenOcean, BiomeId.DeepFrozenOcean);
            CopyDefinition(BiomeId.ColdOcean, BiomeId.DeepColdOcean);
            CopyDefinition(BiomeId.LukewarmOcean, BiomeId.DeepLukewarmOcean);
            CopyDefinition(BiomeId.Plains, BiomeId.SunflowerPlains);
            CopyDefinition(BiomeId.Plains, BiomeId.Meadow);
            CopyDefinition(BiomeId.Forest, BiomeId.FlowerForest);
            CopyDefinition(BiomeId.Forest, BiomeId.BirchForest);
            CopyDefinition(BiomeId.Forest, BiomeId.OldGrowthBirchForest);
            CopyDefinition(BiomeId.Forest, BiomeId.DarkForest);
            CopyDefinition(BiomeId.Forest, BiomeId.PaleGarden);
            CopyDefinition(BiomeId.Taiga, BiomeId.OldGrowthSpruceTaiga);
            CopyDefinition(BiomeId.Taiga, BiomeId.OldGrowthPineTaiga);
            CopyDefinition(BiomeId.SnowyPlains, BiomeId.IceSpikes);
            CopyDefinition(BiomeId.SnowyPlains, BiomeId.SnowySlopes);
            CopyDefinition(BiomeId.Beach, BiomeId.SnowyBeach);
            CopyDefinition(BiomeId.Savanna, BiomeId.SavannaPlateau);
            CopyDefinition(BiomeId.Savanna, BiomeId.WindsweptSavanna);
            CopyDefinition(BiomeId.Jungle, BiomeId.SparseJungle);
            CopyDefinition(BiomeId.Jungle, BiomeId.BambooJungle);
            CopyDefinition(BiomeId.Badlands, BiomeId.ErodedBadlands);
            CopyDefinition(BiomeId.Badlands, BiomeId.WoodedBadlands);
            CopyDefinition(BiomeId.WindsweptHills, BiomeId.WindsweptGravellyHills);
            CopyDefinition(BiomeId.WindsweptHills, BiomeId.WindsweptForest);
        }

        private static void CopyDefinition(BiomeId source, BiomeId target)
        {
            if (Definitions.ContainsKey(target) || !Definitions.TryGetValue(source, out var definition))
            {
                return;
            }

            Definitions[target] = new BiomeDefinition
            {
                Id = target,
                Temperature = definition.Temperature,
                Downfall = definition.Downfall,
                WaterColor = definition.WaterColor,
                GrassColor = definition.GrassColor,
                FoliageColor = definition.FoliageColor,
                GrassColorModifier = definition.GrassColorModifier,
                WaterFogColor = definition.WaterFogColor,
                SkyColor = definition.SkyColor,
                HasPrecipitation = definition.HasPrecipitation
            };
        }

        private static void CreateFallbackDefinitions()
        {
            foreach (BiomeId id in Enum.GetValues(typeof(BiomeId)))
            {
                if (id == BiomeId.Unknown)
                {
                    continue;
                }

                Definitions[id] = new BiomeDefinition { Id = id };
            }
        }
    }
}
