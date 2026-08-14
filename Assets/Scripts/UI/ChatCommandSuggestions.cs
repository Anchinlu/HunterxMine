using System;
using System.Collections.Generic;
using MineCraftUnity.World;

namespace MineCraftUnity.UI
{
    /// <summary>MC-style command autocomplete for chat input starting with /.</summary>
    internal static class ChatCommandSuggestions
    {
        private static readonly string[] AllCommands = BuildAllCommands();

        public static IReadOnlyList<string> GetMatches(string rawInput)
        {
            if (string.IsNullOrEmpty(rawInput) || rawInput[0] != '/')
            {
                return Array.Empty<string>();
            }

            if (string.Equals(rawInput, "/", StringComparison.Ordinal))
            {
                return new[] { "/help", "/time", "/weather", "/locate", "/tp" };
            }

            var biomeMatches = GetBiomeCommandMatches(rawInput);
            if (biomeMatches.Count > 0)
            {
                return biomeMatches;
            }

            var matches = new List<string>(8);
            foreach (var command in AllCommands)
            {
                if (command.StartsWith(rawInput, StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(command, rawInput, StringComparison.OrdinalIgnoreCase))
                {
                    matches.Add(command);
                }
            }

            matches.Sort(static (a, b) =>
            {
                var len = a.Length.CompareTo(b.Length);
                return len != 0 ? len : string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
            });

            if (matches.Count > 8)
            {
                return matches.GetRange(0, 8);
            }

            return matches;
        }

        private static string[] BuildAllCommands()
        {
            var commands = new List<string>
            {
                "/help",
                "/time",
                "/time query",
                "/time set",
                "/weather",
                "/weather clear",
                "/weather rain",
                "/weather thunder",
                "/locate",
                "/locate biome",
                "/tp",
                "/tp biome"
            };

            foreach (var biomeName in BiomeNameParser.GetSuggestions(string.Empty, 12))
            {
                commands.Add($"/locate biome {biomeName}");
                commands.Add($"/tp biome {biomeName}");
            }

            var values = new[]
            {
                "day", "sunrise", "noon", "sunset", "night", "midnight",
                "0", "1000", "6000", "12000", "13000", "18000"
            };

            foreach (var value in values)
            {
                commands.Add($"/time set {value}");
            }

            return commands.ToArray();
        }

        private static List<string> GetBiomeCommandMatches(string rawInput)
        {
            var matches = new List<string>(8);
            AppendBiomeMatches(matches, rawInput, "/locate biome ");
            AppendBiomeMatches(matches, rawInput, "/tp biome ");
            return matches;
        }

        private static void AppendBiomeMatches(List<string> matches, string rawInput, string commandPrefix)
        {
            if (!rawInput.StartsWith(commandPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var typedBiome = rawInput[commandPrefix.Length..];
            foreach (var biomeName in BiomeNameParser.GetSuggestions(typedBiome, MaxDynamicBiomeSuggestions))
            {
                var suggestion = $"{commandPrefix}{biomeName}";
                if (suggestion.StartsWith(rawInput, StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(suggestion, rawInput, StringComparison.OrdinalIgnoreCase))
                {
                    matches.Add(suggestion);
                }
            }
        }

        private const int MaxDynamicBiomeSuggestions = 8;
    }
}
