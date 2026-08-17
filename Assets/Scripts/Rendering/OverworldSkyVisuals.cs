using MineCraftUnity.World;
using UnityEngine;

namespace MineCraftUnity.Rendering
{
    /// <summary>
    /// MC ref: Timelines.OVERWORLD_DAY + dimension overworld sky/fog/light curves (simplified).
    /// </summary>
    public static class OverworldSkyVisuals
    {
        public readonly struct Snapshot
        {
            public readonly Color SkyTop;
            public readonly Color SkyHorizon;
            public readonly Color SunDiscColor;
            public readonly Color MoonDiscColor;
            public readonly Color SunriseSunsetColor;
            public readonly float RainBrightness;
            public readonly MoonPhase MoonPhase;
            public readonly bool ShowSunDisc;
            public readonly bool ShowMoonDisc;
            public readonly float SunAngleRadians;
            public readonly float MoonAngleRadians;
            public readonly Vector3 SunDirection;
            public readonly Vector3 MoonDirection;
            public readonly float SunDiscSize;
            public readonly float MoonDiscSize;
            public readonly float StarBrightness;
            public readonly float StarAngleRadians;
            public readonly Color FogColor;
            public readonly float FogStart;
            public readonly float FogEnd;
            public readonly Color AmbientSky;
            public readonly Color AmbientEquator;
            public readonly Color AmbientGround;
            public readonly Color SunLightColor;
            public readonly float SunLightIntensity;
            public readonly float SkyLightFactor;
            public readonly Quaternion SunLightRotation;

            public Snapshot(
                Color skyTop,
                Color skyHorizon,
                Color sunDiscColor,
                Color moonDiscColor,
                Color sunriseSunsetColor,
                float rainBrightness,
                MoonPhase moonPhase,
                bool showSunDisc,
                bool showMoonDisc,
                float sunAngleRadians,
                float moonAngleRadians,
                Vector3 sunDirection,
                Vector3 moonDirection,
                float sunDiscSize,
                float moonDiscSize,
                float starBrightness,
                float starAngleRadians,
                Color fogColor,
                float fogStart,
                float fogEnd,
                Color ambientSky,
                Color ambientEquator,
                Color ambientGround,
                Color sunLightColor,
                float sunLightIntensity,
                float skyLightFactor,
                Quaternion sunLightRotation)
            {
                SkyTop = skyTop;
                SkyHorizon = skyHorizon;
                SunDiscColor = sunDiscColor;
                MoonDiscColor = moonDiscColor;
                SunriseSunsetColor = sunriseSunsetColor;
                RainBrightness = rainBrightness;
                MoonPhase = moonPhase;
                ShowSunDisc = showSunDisc;
                ShowMoonDisc = showMoonDisc;
                SunAngleRadians = sunAngleRadians;
                MoonAngleRadians = moonAngleRadians;
                SunDirection = sunDirection;
                MoonDirection = moonDirection;
                SunDiscSize = sunDiscSize;
                MoonDiscSize = moonDiscSize;
                StarBrightness = starBrightness;
                StarAngleRadians = starAngleRadians;
                FogColor = fogColor;
                FogStart = fogStart;
                FogEnd = fogEnd;
                AmbientSky = ambientSky;
                AmbientEquator = ambientEquator;
                AmbientGround = ambientGround;
                SunLightColor = sunLightColor;
                SunLightIntensity = sunLightIntensity;
                SkyLightFactor = skyLightFactor;
                SunLightRotation = sunLightRotation;
            }
        }

        /// <summary>MC ref: Timelines.OVERWORLD_DAY SUN_ANGLE — 0 at noon (tick 6000).</summary>
        public static float EvaluateSunAngleRadians(float dayFraction)
        {
            return (dayFraction - 0.25f) * Mathf.PI * 2f;
        }

        /// <summary>MC ref: MOON_ANGLE offset 180° from sun.</summary>
        public static float EvaluateMoonAngleRadians(float dayFraction)
        {
            return EvaluateSunAngleRadians(dayFraction) + Mathf.PI;
        }

        /// <summary>World-space celestial direction. Tilted 30 degrees South to satisfy East-South-West orbit.</summary>
        public static Vector3 DirectionFromCelestialAngle(float angleRadians)
        {
            float tilt = 30f * Mathf.Deg2Rad;
            float x = -Mathf.Sin(angleRadians);
            float y = Mathf.Cos(angleRadians) * Mathf.Cos(tilt);
            float z = Mathf.Cos(angleRadians) * Mathf.Sin(tilt);
            return new Vector3(x, y, z).normalized;
        }

        public static Snapshot Evaluate(
            float dayFraction,
            long dayTime = 0,
            float rainLevel = 0f,
            float thunderLevel = 0f,
            BiomeId biome = BiomeId.Unknown)
        {
            return EvaluateInternal(dayFraction, dayTime, rainLevel, thunderLevel, biome);
        }

        public static Snapshot Evaluate(float dayFraction, long dayTime, float rainLevel, float thunderLevel) =>
            EvaluateInternal(dayFraction, dayTime, rainLevel, thunderLevel, BiomeId.Unknown);

        private static Snapshot EvaluateInternal(
            float dayFraction,
            long dayTime,
            float rainLevel,
            float thunderLevel,
            BiomeId biome)
        {
            var moonPhase = MoonPhaseExtensions.FromDayTime(dayTime);
            var rainBrightness = Mathf.Clamp01(1f - rainLevel * 0.75f);
            var thunderDim = 1f - Mathf.Clamp01(thunderLevel) * 0.55f;
            var sunAngleRadians = EvaluateSunAngleRadians(dayFraction);
            var moonAngleRadians = EvaluateMoonAngleRadians(dayFraction);
            var sunHeight = Mathf.Cos(sunAngleRadians);
            var moonHeight = Mathf.Cos(moonAngleRadians);
            var dayAmount = Mathf.Clamp01(SmoothDayWeight(sunHeight));
            var nightAmount = 1f - dayAmount;

            var sunDirection = DirectionFromCelestialAngle(sunAngleRadians);
            var moonDirection = DirectionFromCelestialAngle(moonAngleRadians);

            var skyColor = OverworldDayTimeline.SampleSkyColor(dayTime);
            var fogColor = OverworldDayTimeline.SampleFogColor(dayTime);
            ApplyBiomeTint(ref skyColor, ref fogColor, biome, dayAmount, rainLevel);
            ApplyWeather(ref skyColor, ref fogColor, rainLevel, thunderDim);

            var ambientSky = skyColor * 0.85f;
            var ambientEquator = fogColor * 0.75f;
            var ambientGround = ambientEquator * 0.45f;

            var skyLightFactor = OverworldDayTimeline.SampleSkyLightFactor(dayTime) * thunderDim;
            var sunLightIntensity = Mathf.Lerp(0.08f, 1.15f, SmoothStep01(Mathf.Clamp01(sunHeight)))
                * rainBrightness * thunderDim;
            var sunLightColor = Color.Lerp(new Color(0.55f, 0.62f, 0.9f, 1f), Color.white, dayAmount);

            var showSunDisc = sunHeight > -0.05f;
            var sunDiscColor = showSunDisc
                ? Color.Lerp(new Color(1f, 0.92f, 0.72f, 1f), Color.white, dayAmount)
                : Color.clear;

            var showMoonDisc = moonHeight > 0.02f;
            var moonPhaseBrightness = EvaluateMoonPhaseBrightness(moonPhase);
            var moonDiscColor = showMoonDisc
                ? new Color(0.82f, 0.86f, 0.95f, Mathf.Clamp01((0.25f + nightAmount * 0.75f) * moonPhaseBrightness))
                : Color.clear;

            var starBrightness = OverworldDayTimeline.SampleStarBrightness(dayTime) * thunderDim;

            var sunriseSunsetColor = OverworldDayTimeline.SampleSunriseSunsetColor(dayTime);
            var horizonProximity = 1f - Mathf.Clamp01(Mathf.Abs(sunHeight) / 0.28f);
            sunriseSunsetColor.a *= horizonProximity * rainBrightness;

            var sunLightRotation = Quaternion.LookRotation(-sunDirection, Vector3.up);

            var starAngleRadians = sunAngleRadians;

            return new Snapshot(
                skyColor,
                fogColor,
                sunDiscColor,
                moonDiscColor,
                sunriseSunsetColor,
                rainBrightness,
                moonPhase,
                showSunDisc,
                showMoonDisc,
                sunAngleRadians,
                moonAngleRadians,
                sunDirection,
                moonDirection,
                sunDiscSize: 30f,
                moonDiscSize: 20f,
                starBrightness,
                starAngleRadians,
                fogColor,
                fogStart: 64f,
                fogEnd: 256f,
                ambientSky,
                ambientEquator,
                ambientGround,
                sunLightColor,
                sunLightIntensity,
                skyLightFactor,
                sunLightRotation);
        }

        /// <summary>MC ref: moon phase affects disc brightness (new moon nearly invisible).</summary>
        private static float EvaluateMoonPhaseBrightness(MoonPhase phase)
        {
            return phase switch
            {
                MoonPhase.NewMoon => 0.12f,
                MoonPhase.WaningCrescent or MoonPhase.WaxingCrescent => 0.45f,
                MoonPhase.FirstQuarter or MoonPhase.ThirdQuarter => 0.68f,
                _ => 1f
            };
        }

        /// <summary>MC ref: biome attributes visual/sky_color tints clear daytime sky/fog.</summary>
        private static void ApplyBiomeTint(
            ref Color skyColor,
            ref Color fogColor,
            BiomeId biome,
            float dayAmount,
            float rainLevel)
        {
            if (biome == BiomeId.Unknown)
            {
                return;
            }

            var biomeSky = BiomeRegistry.GetSkyColor(biome);
            if (ColorDistance(biomeSky, OverworldDayTimeline.DefaultSkyColor) < 0.001f)
            {
                return;
            }

            var blend = Mathf.Clamp01(dayAmount * (1f - rainLevel * 0.65f) * 0.4f);
            skyColor = Color.Lerp(skyColor, biomeSky, blend);
            fogColor = Color.Lerp(fogColor, biomeSky * 0.92f, blend * 0.65f);
        }

        private static float ColorDistance(Color a, Color b)
        {
            var dr = a.r - b.r;
            var dg = a.g - b.g;
            var db = a.b - b.b;
            return dr * dr + dg * dg + db * db;
        }

        /// <summary>MC ref: WeatherAttributes rain/thunder gray blend on sky/fog.</summary>
        private static void ApplyWeather(ref Color skyColor, ref Color fogColor, float rainLevel, float thunderDim)
        {
            if (rainLevel > 0.001f)
            {
                var rainGray = new Color(0.55f, 0.58f, 0.62f);
                skyColor = Color.Lerp(skyColor, rainGray, rainLevel * 0.45f);
                fogColor = Color.Lerp(fogColor, rainGray, rainLevel * 0.35f);
            }

            if (thunderDim < 0.999f)
            {
                skyColor *= thunderDim;
                fogColor *= thunderDim;
            }
        }

        private static float SmoothDayWeight(float sunHeight)
        {
            return SmoothStep01(Mathf.Clamp01(Mathf.InverseLerp(-0.25f, 0.35f, sunHeight)));
        }

        private static float SmoothStep01(float t) => t * t * (3f - 2f * t);
    }
}
