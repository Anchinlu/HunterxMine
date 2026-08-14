using System;
using System.Collections.Generic;

namespace MineCraftUnity.World
{
    /// <summary>Parse MC-style biome names (plains, flower_forest, minecraft:desert, …).</summary>
    public static class BiomeNameParser
    {
        private static readonly Dictionary<string, BiomeId> Aliases = BuildAliases();

        public static bool TryParse(string rawName, out BiomeId biomeId, out string error)
        {
            biomeId = BiomeId.Unknown;
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(rawName))
            {
                error = "Biome name is required.";
                return false;
            }

            var key = NormalizeKey(rawName);
            if (Aliases.TryGetValue(key, out biomeId))
            {
                return true;
            }

            error = $"Unknown biome: {rawName.Trim()}. Try plains, forest, desert, jungle, taiga, swamp, badlands, cherry_grove, …";
            return false;
        }

        public static IReadOnlyList<string> GetSuggestions(string prefix, int maxCount)
        {
            if (maxCount <= 0)
            {
                return Array.Empty<string>();
            }

            var normalizedPrefix = NormalizeKey(prefix);
            var matches = new List<string>(maxCount);

            foreach (var pair in Aliases)
            {
                if (pair.Value == BiomeId.Unknown)
                {
                    continue;
                }

                if (!pair.Key.StartsWith(normalizedPrefix, StringComparison.Ordinal))
                {
                    continue;
                }

                var commandName = ToCommandName(pair.Value);
                if (!matches.Contains(commandName))
                {
                    matches.Add(commandName);
                }
            }

            matches.Sort(StringComparer.OrdinalIgnoreCase);
            if (matches.Count > maxCount)
            {
                return matches.GetRange(0, maxCount);
            }

            return matches;
        }

        public static string ToCommandName(BiomeId id) =>
            id switch
            {
                BiomeId.Unknown => "unknown",
                _ => ToSnakeCase(id.ToString())
            };

        private static Dictionary<string, BiomeId> BuildAliases()
        {
            var aliases = new Dictionary<string, BiomeId>(StringComparer.Ordinal);

            foreach (BiomeId id in Enum.GetValues(typeof(BiomeId)))
            {
                if (id == BiomeId.Unknown)
                {
                    continue;
                }

                RegisterAlias(aliases, id, id.ToString());
                RegisterAlias(aliases, id, ToSnakeCase(id.ToString()));
                RegisterAlias(aliases, id, BiomeRegistry.GetDisplayName(id));
            }

            RegisterAlias(aliases, BiomeId.OldGrowthBirchForest, "tall_birch_forest");
            RegisterAlias(aliases, BiomeId.OldGrowthSpruceTaiga, "giant_tree_taiga");
            RegisterAlias(aliases, BiomeId.OldGrowthPineTaiga, "giant_spruce_taiga");
            RegisterAlias(aliases, BiomeId.WoodedBadlands, "wooded_badlands_plateau");
            RegisterAlias(aliases, BiomeId.SparseJungle, "jungle_edge");

            return aliases;
        }

        private static void RegisterAlias(Dictionary<string, BiomeId> aliases, BiomeId id, string name)
        {
            var key = NormalizeKey(name);
            if (key.Length == 0)
            {
                return;
            }

            aliases[key] = id;
        }

        private static string NormalizeKey(string name)
        {
            var trimmed = name.Trim();
            const string prefix = "minecraft:";
            if (trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                trimmed = trimmed[prefix.Length..];
            }

            return trimmed
                .Replace("_", string.Empty, StringComparison.Ordinal)
                .Replace(" ", string.Empty, StringComparison.Ordinal)
                .ToLowerInvariant();
        }

        private static string ToSnakeCase(string pascalCase)
        {
            if (string.IsNullOrEmpty(pascalCase))
            {
                return string.Empty;
            }

            var chars = new List<char>(pascalCase.Length + 8);
            for (var i = 0; i < pascalCase.Length; i++)
            {
                var c = pascalCase[i];
                if (char.IsUpper(c) && i > 0)
                {
                    chars.Add('_');
                }

                chars.Add(char.ToLowerInvariant(c));
            }

            return new string(chars.ToArray());
        }
    }
}
