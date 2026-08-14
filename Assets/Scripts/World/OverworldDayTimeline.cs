using UnityEngine;

namespace MineCraftUnity.World
{
    /// <summary>
    /// MC ref: dimension_type/overworld.json + Timelines.OVERWORLD_DAY bootstrap.
    /// </summary>
    public static class OverworldDayTimeline
    {
        public const float DaySkyLightLevel = 15f;
        public const float NightSkyLightLevel = 4f;

        /// <summary>MC ref: overworld.json visual/sky_color.</summary>
        public static readonly Color DefaultSkyColor = Hex("#78a7ff");

        /// <summary>MC ref: overworld.json visual/fog_color.</summary>
        public static readonly Color DefaultFogColor = Hex("#c0d8ff");

        private static readonly Color NightFogStart = Hex("#0d0d17");
        private static readonly Color NightFogEnd = Hex("#171717");

        private static readonly (int tick, float value)[] SkyLightLevelMulTrack =
        {
            (133, 1f),
            (11867, 1f),
            (13670, NightSkyLightLevel / DaySkyLightLevel),
            (22330, NightSkyLightLevel / DaySkyLightLevel)
        };

        private static readonly (int tick, float value)[] SkyLightFactorTrack =
        {
            (730, 1f),
            (11270, 1f),
            (13140, 0.24f),
            (22860, 0.24f)
        };

        private static readonly (int tick, float value)[] SkyColorNightBlendTrack =
        {
            (133, 0f),
            (11867, 0f),
            (13670, 1f),
            (22330, 1f)
        };

        private static readonly (int tick, float value)[] StarBrightnessTrack =
        {
            (92, 0.037f),
            (627, 0f),
            (11373, 0f),
            (11732, 0.016f),
            (11959, 0.044f),
            (12399, 0.143f),
            (12729, 0.258f),
            (13228, 0.5f),
            (22772, 0.5f),
            (23032, 0.364f),
            (23356, 0.225f),
            (23758, 0.101f)
        };

        public static float SampleSkyLightLevel(long dayTime) =>
            DaySkyLightLevel * SampleFloat(SkyLightLevelMulTrack, dayTime);

        public static float SampleSkyLightFactor(long dayTime) =>
            SampleFloat(SkyLightFactorTrack, dayTime);

        public static float SampleStarBrightness(long dayTime) =>
            SampleFloat(StarBrightnessTrack, dayTime);

        /// <summary>MC ref: SKY_COLOR biome/dimension base × night RGB multiplier.</summary>
        public static Color SampleSkyColor(long dayTime)
        {
            var nightBlend = SampleFloat(SkyColorNightBlendTrack, dayTime);
            return Color.Lerp(DefaultSkyColor, Color.black, nightBlend);
        }

        /// <summary>MC ref: FOG_COLOR base × night fog multipliers.</summary>
        public static Color SampleFogColor(long dayTime)
        {
            var nightBlend = SampleFloat(SkyColorNightBlendTrack, dayTime);
            var nightFog = Color.Lerp(NightFogStart, NightFogEnd, 0.5f);
            return Color.Lerp(DefaultFogColor, nightFog, nightBlend);
        }

        /// <summary>MC ref: SUNRISE_SUNSET_COLOR track (warm/cool peaks at dawn/dusk).</summary>
        public static Color SampleSunriseSunsetColor(long dayTime)
        {
            var tick = NormalizeTick(dayTime);
            if (tick is >= 71 and <= 730 or >= 11270 and <= 11929)
            {
                return Hex("#FF8833");
            }

            if (tick is >= 11929 and <= 13252)
            {
                return Hex("#CC4488");
            }

            if (tick is >= 21807 and <= 23757)
            {
                return Hex("#FF8833");
            }

            return Clear;
        }

        private static readonly Color Clear = new(0f, 0f, 0f, 0f);

        private static float SampleFloat((int tick, float value)[] track, long dayTime)
        {
            var tick = NormalizeTick(dayTime);
            FindBracket(track, tick, out var a, out var b, out var t);
            return Mathf.Lerp(track[a].value, track[b].value, t);
        }

        private static int NormalizeTick(long dayTime)
        {
            var tick = (int)(dayTime % WorldTime.TicksPerDay);
            if (tick < 0)
            {
                tick += WorldTime.TicksPerDay;
            }

            return tick;
        }

        private static void FindBracket((int tick, float value)[] track, int tick, out int a, out int b, out float t)
        {
            if (tick <= track[0].tick)
            {
                a = track.Length - 1;
                b = 0;
                var span = (WorldTime.TicksPerDay - track[a].tick) + track[b].tick;
                t = span > 0 ? (tick + WorldTime.TicksPerDay - track[a].tick) / (float)span : 0f;
                return;
            }

            for (var i = 0; i < track.Length - 1; i++)
            {
                if (tick >= track[i].tick && tick <= track[i + 1].tick)
                {
                    a = i;
                    b = i + 1;
                    var range = track[b].tick - track[a].tick;
                    t = range > 0 ? (tick - track[a].tick) / (float)range : 0f;
                    return;
                }
            }

            a = track.Length - 1;
            b = 0;
            var wrap = (WorldTime.TicksPerDay - track[a].tick) + track[b].tick;
            t = wrap > 0 ? (tick - track[a].tick) / (float)wrap : 0f;
        }

        private static Color Hex(string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex, out var color))
            {
                color.a = 1f;
                return color;
            }

            return Color.magenta;
        }
    }
}
