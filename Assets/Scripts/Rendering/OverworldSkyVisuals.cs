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
            public readonly Vector3 SunDirection;
            public readonly Vector3 MoonDirection;
            public readonly float SunDiscSize;
            public readonly float MoonDiscSize;
            public readonly float StarBrightness;
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
                Vector3 sunDirection,
                Vector3 moonDirection,
                float sunDiscSize,
                float moonDiscSize,
                float starBrightness,
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
                SunDirection = sunDirection;
                MoonDirection = moonDirection;
                SunDiscSize = sunDiscSize;
                MoonDiscSize = moonDiscSize;
                StarBrightness = starBrightness;
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

        private static readonly Color DaySkyTop = new(0.47f, 0.65f, 1f, 1f);
        private static readonly Color DaySkyHorizon = new(0.72f, 0.85f, 1f, 1f);
        private static readonly Color NightSkyTop = new(0.02f, 0.03f, 0.08f, 1f);
        private static readonly Color NightSkyHorizon = new(0.04f, 0.05f, 0.12f, 1f);
        private static readonly Color DayFog = new(0.75f, 0.88f, 1f, 1f);
        private static readonly Color NightFog = new(0.02f, 0.03f, 0.08f, 1f);

        public static Snapshot Evaluate(float dayFraction)
        {
            var sunHeight = Mathf.Cos((dayFraction - 0.25f) * Mathf.PI * 2f);
            var dayAmount = SmoothDayWeight(sunHeight);
            var nightAmount = 1f - dayAmount;

            var azimuth = dayFraction * Mathf.PI * 2f;
            var horizontal = Mathf.Sin(azimuth);
            var depth = Mathf.Cos(azimuth);
            var sunDirection = new Vector3(horizontal, Mathf.Max(sunHeight, -0.15f), depth).normalized;
            var moonDirection = (-sunDirection).normalized;

            var skyTop = Color.Lerp(NightSkyTop, DaySkyTop, dayAmount);
            var skyHorizon = Color.Lerp(NightSkyHorizon, DaySkyHorizon, dayAmount);

            var sunriseWarmth = Mathf.Clamp01(1f - Mathf.Abs(sunHeight) * 6f) * (1f - dayAmount * 0.35f);
            if (sunriseWarmth > 0.001f)
            {
                var warm = new Color(1f, 0.55f, 0.25f, 1f);
                skyHorizon = Color.Lerp(skyHorizon, warm, sunriseWarmth * 0.65f);
            }

            var fogColor = Color.Lerp(NightFog, DayFog, dayAmount);
            fogColor = Color.Lerp(fogColor, skyHorizon, 0.35f);

            var ambientSky = Color.Lerp(NightSkyHorizon * 0.55f, DaySkyTop * 0.85f, dayAmount);
            var ambientEquator = Color.Lerp(NightSkyHorizon * 0.35f, DaySkyHorizon * 0.75f, dayAmount);
            var ambientGround = ambientEquator * 0.45f;

            var skyLightFactor = Mathf.Lerp(0.27f, 1f, dayAmount);
            var sunLightIntensity = Mathf.Lerp(0.08f, 1.15f, SmoothStep01(Mathf.Clamp01(sunHeight)));
            var sunLightColor = Color.Lerp(new Color(0.55f, 0.62f, 0.9f, 1f), Color.white, dayAmount);

            var sunVisible = sunHeight > -0.05f;
            var sunDiscColor = sunVisible
                ? Color.Lerp(new Color(1f, 0.92f, 0.72f, 1f), Color.white, dayAmount)
                : Color.clear;
            var moonVisible = sunHeight < 0.15f;
            var moonDiscColor = moonVisible
                ? Color.Lerp(new Color(0.82f, 0.86f, 0.95f, 1f), Color.clear, dayAmount)
                : Color.clear;

            var starBrightness = Mathf.Clamp01((nightAmount - 0.15f) / 0.85f) * 0.5f;

            var sunLightRotation = Quaternion.LookRotation(-sunDirection, Vector3.up);

            return new Snapshot(
                skyTop,
                skyHorizon,
                sunDiscColor,
                moonDiscColor,
                sunDirection,
                moonDirection,
                sunDiscSize: 0.018f,
                moonDiscSize: 0.014f,
                starBrightness,
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

        private static float SmoothDayWeight(float sunHeight)
        {
            return SmoothStep01(Mathf.InverseLerp(-0.25f, 0.35f, sunHeight));
        }

        private static float SmoothStep01(float t) => t * t * (3f - 2f * t);
    }
}
