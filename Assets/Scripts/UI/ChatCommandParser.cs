using System;
using System.Globalization;
using MineCraftUnity.Rendering;
using MineCraftUnity.World;
using UnityEngine;

namespace MineCraftUnity.UI
{
    /// <summary>MC-style chat commands (time set, …).</summary>
    internal static class ChatCommandParser
    {
        public static bool TryExecute(string rawInput, out string response)
        {
            response = string.Empty;
            if (string.IsNullOrWhiteSpace(rawInput))
            {
                response = "Empty command.";
                return false;
            }

            var input = rawInput.Trim();
            if (input.StartsWith('/'))
            {
                input = input[1..].TrimStart();
            }

            var parts = input.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                response = "Empty command.";
                return false;
            }

            if (string.Equals(parts[0], "time", StringComparison.OrdinalIgnoreCase))
            {
                return TryExecuteTime(parts, out response);
            }

            if (string.Equals(parts[0], "weather", StringComparison.OrdinalIgnoreCase))
            {
                return TryExecuteWeather(parts, out response);
            }

            if (string.Equals(parts[0], "locate", StringComparison.OrdinalIgnoreCase)
                || string.Equals(parts[0], "tp", StringComparison.OrdinalIgnoreCase))
            {
                return BiomeTeleportCommands.TryExecute(parts, out response);
            }

            if (string.Equals(parts[0], "help", StringComparison.OrdinalIgnoreCase))
            {
                response = "Commands: time set/query, weather clear/rain/thunder, locate/tp biome <name>, help";
                return true;
            }

            response = $"Unknown command: {parts[0]}. Type help.";
            return false;
        }

        private static bool TryExecuteWeather(string[] parts, out string response)
        {
            response = string.Empty;
            if (parts.Length < 2)
            {
                response = "Usage: weather clear | rain | thunder";
                return false;
            }

            var controller = DayNightController.Instance;
            if (controller == null)
            {
                response = "DayNightController not found.";
                return false;
            }

            switch (parts[1].ToLowerInvariant())
            {
                case "clear":
                    controller.SetWeatherClear();
                    response = "Weather set to clear.";
                    return true;
                case "rain":
                    controller.SetWeatherRain();
                    response = "Weather set to rain.";
                    return true;
                case "thunder":
                case "thunderstorm":
                    controller.SetWeatherThunder();
                    response = "Weather set to thunder.";
                    return true;
                default:
                    response = $"Unknown weather: {parts[1]}. Use clear, rain, or thunder.";
                    return false;
            }
        }

        private static bool TryExecuteTime(string[] parts, out string response)
        {
            response = string.Empty;
            if (parts.Length < 2)
            {
                response = "Usage: time set <tick|noon|day|night|midnight>  or  time query";
                return false;
            }

            if (string.Equals(parts[1], "query", StringComparison.OrdinalIgnoreCase))
            {
                return TryQueryTime(out response);
            }

            if (!string.Equals(parts[1], "set", StringComparison.OrdinalIgnoreCase))
            {
                response = "Usage: time set <tick|noon|day|night|midnight>  or  time query";
                return false;
            }

            if (parts.Length < 3)
            {
                response = "Usage: time set <tick|noon|day|night|midnight>";
                return false;
            }

            if (!TryParseTimeArgument(parts[2], out var ticks))
            {
                response = $"Invalid time: {parts[2]}. Use tick 0-23999 or noon/day/night/midnight/sunrise/sunset.";
                return false;
            }

            var controller = DayNightController.Instance;
            if (controller == null)
            {
                response = "DayNightController not found.";
                return false;
            }

            controller.SetDayTime(ticks);
            response = $"Set time to {WorldTime.FormatClock(ticks)} (tick {NormalizeTick(ticks)}).";
            return true;
        }

        private static bool TryQueryTime(out string response)
        {
            var controller = DayNightController.Instance;
            if (controller == null)
            {
                response = "DayNightController not found.";
                return false;
            }

            var dayTime = controller.WorldTime.DayTime;
            var weather = controller.IsThundering ? "thunder"
                : controller.RainLevel > 0.05f ? "rain" : "clear";
            response = $"Time is {WorldTime.FormatClock(dayTime)} (tick {NormalizeTick(dayTime)}, phase {controller.WorldTime.MoonPhase}, weather {weather}).";
            return true;
        }

        private static bool TryParseTimeArgument(string token, out long ticks)
        {
            ticks = 0;
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            if (long.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out ticks))
            {
                ticks = NormalizeTick(ticks);
                return true;
            }

            ticks = token.ToLowerInvariant() switch
            {
                "sunrise" or "morning" or "day" => 1000,
                "noon" or "midday" => 6000,
                "sunset" or "evening" => 12000,
                "night" => 13000,
                "midnight" => 18000,
                _ => -1
            };

            return ticks >= 0;
        }

        private static long NormalizeTick(long ticks)
        {
            var normalized = ticks % WorldTime.TicksPerDay;
            if (normalized < 0)
            {
                normalized += WorldTime.TicksPerDay;
            }

            return normalized;
        }
    }
}
