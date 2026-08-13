using System;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

namespace MineCraftUnity.World
{
    /// <summary>
    /// Minimal parser for MC biome JSON — extracts effects.colors and climate fields.
    /// </summary>
    public static class BiomeJsonParser
    {
        private static readonly Regex HexColorRegex = new(
            "\"(?<key>water_color|grass_color|foliage_color|dry_foliage_color)\"\\s*:\\s*\"(?<hex>#[0-9a-fA-F]{6})\"",
            RegexOptions.Compiled);

        private static readonly Regex GrassModifierRegex = new(
            "\"grass_color_modifier\"\\s*:\\s*\"(?<value>[a-z_]+)\"",
            RegexOptions.Compiled);

        private static readonly Regex FloatFieldRegex = new(
            "\"(?<key>temperature|downfall)\"\\s*:\\s*(?<value>-?[0-9]+(?:\\.[0-9]+)?)",
            RegexOptions.Compiled);

        private static readonly Regex WaterFogRegex = new(
            "\"minecraft:visual/water_fog_color\"\\s*:\\s*\"(?<hex>#[0-9a-fA-F]{6})\"",
            RegexOptions.Compiled);

        public static BiomeDefinition Parse(BiomeId id, string json)
        {
            var definition = new BiomeDefinition { Id = id, WaterColor = BiomeJsonParser.ParseHexColor("#3f76e4") };

            foreach (Match match in FloatFieldRegex.Matches(json))
            {
                var key = match.Groups["key"].Value;
                var value = float.Parse(match.Groups["value"].Value, CultureInfo.InvariantCulture);
                if (key == "temperature")
                {
                    definition = Clone(definition, temperature: value);
                }
                else if (key == "downfall")
                {
                    definition = Clone(definition, downfall: value);
                }
            }

            foreach (Match match in HexColorRegex.Matches(json))
            {
                var key = match.Groups["key"].Value;
                var color = ParseHexColor(match.Groups["hex"].Value);
                definition = key switch
                {
                    "water_color" => Clone(definition, waterColor: color),
                    "grass_color" => Clone(definition, grassColor: color),
                    "foliage_color" => Clone(definition, foliageColor: color),
                    _ => definition
                };
            }

            var modifierMatch = GrassModifierRegex.Match(json);
            if (modifierMatch.Success)
            {
                definition = Clone(definition, grassColorModifier: modifierMatch.Groups["value"].Value);
            }

            var fogMatch = WaterFogRegex.Match(json);
            if (fogMatch.Success)
            {
                definition = Clone(definition, waterFogColor: ParseHexColor(fogMatch.Groups["hex"].Value));
            }

            return definition;
        }

        public static bool TryParseIdFromFileName(string fileNameWithoutExtension, out BiomeId id)
        {
            id = BiomeId.Unknown;
            if (string.IsNullOrEmpty(fileNameWithoutExtension))
            {
                return false;
            }

            var pascal = SnakeCaseToPascal(fileNameWithoutExtension);
            if (!Enum.TryParse(pascal, ignoreCase: false, out id) || id == BiomeId.Unknown)
            {
                return false;
            }

            return true;
        }

        public static Color ParseHexColor(string hex)
        {
            if (string.IsNullOrEmpty(hex) || hex.Length < 7 || hex[0] != '#')
            {
                return Color.white;
            }

            var r = byte.Parse(hex.AsSpan(1, 2), NumberStyles.HexNumber);
            var g = byte.Parse(hex.AsSpan(3, 2), NumberStyles.HexNumber);
            var b = byte.Parse(hex.AsSpan(5, 2), NumberStyles.HexNumber);
            return new Color32(r, g, b, 255);
        }

        private static string SnakeCaseToPascal(string snake)
        {
            if (!snake.Contains('_'))
            {
                return char.ToUpperInvariant(snake[0]) + snake[1..];
            }

            var parts = snake.Split('_');
            var result = string.Empty;
            foreach (var part in parts)
            {
                if (part.Length == 0)
                {
                    continue;
                }

                result += char.ToUpperInvariant(part[0]) + part[1..];
            }

            return result;
        }

        private static BiomeDefinition Clone(
            BiomeDefinition source,
            float? temperature = null,
            float? downfall = null,
            Color? waterColor = null,
            Color? grassColor = null,
            Color? foliageColor = null,
            string grassColorModifier = null,
            Color? waterFogColor = null)
        {
            return new BiomeDefinition
            {
                Id = source.Id,
                Temperature = temperature ?? source.Temperature,
                Downfall = downfall ?? source.Downfall,
                WaterColor = waterColor ?? source.WaterColor,
                GrassColor = grassColor ?? source.GrassColor,
                FoliageColor = foliageColor ?? source.FoliageColor,
                GrassColorModifier = grassColorModifier ?? source.GrassColorModifier,
                WaterFogColor = waterFogColor ?? source.WaterFogColor
            };
        }
    }
}
