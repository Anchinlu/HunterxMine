using System;
using MineCraftUnity.WorldGen.Synth;

namespace MineCraftUnity.WorldGen.Density
{
    /// <summary>
    /// MC ref: net.minecraft.data.worldgen.TerrainProvider
    /// </summary>
    public static class TerrainProvider
    {
        private static float Identity(float value) => value;

        private static float AmplifiedOffset(float offset) => offset < 0.0F ? offset : offset * 2.0F;

        private static float AmplifiedFactor(float factor) => 1.25F - 6.25F / (factor + 5.0F);

        private static float AmplifiedJaggedness(float jaggedness) => jaggedness * 2.0F;

        public static ICubicSpline<TContext> OverworldOffset<TContext>(
            IBoundedFloatFunction<TContext> continents,
            IBoundedFloatFunction<TContext> erosion,
            IBoundedFloatFunction<TContext> ridges,
            bool amplified)
        {
            Func<float, float> offsetTransformer = amplified ? AmplifiedOffset : Identity;
            ICubicSpline<TContext> beachSpline = BuildErosionOffsetSpline(
                erosion, ridges, -0.15F, 0.0F, 0.0F, 0.1F, 0.0F, -0.03F, false, false, offsetTransformer);
            ICubicSpline<TContext> lowSpline = BuildErosionOffsetSpline(
                erosion, ridges, -0.1F, 0.03F, 0.1F, 0.1F, 0.01F, -0.03F, false, false, offsetTransformer);
            ICubicSpline<TContext> midSpline = BuildErosionOffsetSpline(
                erosion, ridges, -0.1F, 0.03F, 0.1F, 0.7F, 0.01F, -0.03F, true, true, offsetTransformer);
            ICubicSpline<TContext> highSpline = BuildErosionOffsetSpline(
                erosion, ridges, -0.05F, 0.03F, 0.1F, 1.0F, 0.01F, 0.01F, true, true, offsetTransformer);

            return CubicSpline.CreateBuilder(continents, offsetTransformer)
                .AddPoint(-1.1F, 0.044F)
                .AddPoint(-1.02F, -0.2222F)
                .AddPoint(-0.51F, -0.2222F)
                .AddPoint(-0.44F, -0.12F)
                .AddPoint(-0.18F, -0.12F)
                .AddPoint(-0.16F, beachSpline)
                .AddPoint(-0.15F, beachSpline)
                .AddPoint(-0.1F, lowSpline)
                .AddPoint(0.25F, midSpline)
                .AddPoint(1.0F, highSpline)
                .Build();
        }

        public static ICubicSpline<TContext> OverworldFactor<TContext>(
            IBoundedFloatFunction<TContext> continents,
            IBoundedFloatFunction<TContext> erosion,
            IBoundedFloatFunction<TContext> weirdness,
            IBoundedFloatFunction<TContext> ridges,
            bool amplified)
        {
            Func<float, float> factorTransformer = amplified ? AmplifiedFactor : Identity;
            return CubicSpline.CreateBuilder(continents, Identity)
                .AddPoint(-0.19F, 3.95F)
                .AddPoint(-0.15F, GetErosionFactor(erosion, weirdness, ridges, 6.25F, true, Identity))
                .AddPoint(-0.1F, GetErosionFactor(erosion, weirdness, ridges, 5.47F, true, factorTransformer))
                .AddPoint(0.03F, GetErosionFactor(erosion, weirdness, ridges, 5.08F, true, factorTransformer))
                .AddPoint(0.06F, GetErosionFactor(erosion, weirdness, ridges, 4.69F, false, factorTransformer))
                .Build();
        }

        public static ICubicSpline<TContext> OverworldJaggedness<TContext>(
            IBoundedFloatFunction<TContext> continents,
            IBoundedFloatFunction<TContext> erosion,
            IBoundedFloatFunction<TContext> weirdness,
            IBoundedFloatFunction<TContext> ridges,
            bool amplified)
        {
            Func<float, float> jaggednessTransformer = amplified ? AmplifiedJaggedness : Identity;
            return CubicSpline.CreateBuilder(continents, jaggednessTransformer)
                .AddPoint(-0.11F, 0.0F)
                .AddPoint(0.03F, BuildErosionJaggednessSpline(erosion, weirdness, ridges, 1.0F, 0.5F, 0.0F, 0.0F, jaggednessTransformer))
                .AddPoint(0.65F, BuildErosionJaggednessSpline(erosion, weirdness, ridges, 1.0F, 1.0F, 1.0F, 0.0F, jaggednessTransformer))
                .Build();
        }

        public static float PeaksAndValleys(float weirdness) =>
            -(Math.Abs(Math.Abs(weirdness) - 0.6666667F) - 0.33333334F) * 3.0F;

        private static ICubicSpline<TContext> BuildErosionJaggednessSpline<TContext>(
            IBoundedFloatFunction<TContext> erosion,
            IBoundedFloatFunction<TContext> weirdness,
            IBoundedFloatFunction<TContext> ridges,
            float jaggednessFactorAtPeakRidgeAndErosionIndex0,
            float jaggednessFactorAtPeakRidgeAndErosionIndex1,
            float jaggednessFactorAtHighRidgeAndErosionIndex0,
            float jaggednessFactorAtHighRidgeAndErosionIndex1,
            Func<float, float> jaggednessTransformer)
        {
            ICubicSpline<TContext> ridgeJaggednessSplineAtErosion0 = BuildRidgeJaggednessSpline(
                weirdness, ridges, jaggednessFactorAtPeakRidgeAndErosionIndex0, jaggednessFactorAtHighRidgeAndErosionIndex0, jaggednessTransformer);
            ICubicSpline<TContext> ridgeJaggednessSplineAtErosion1 = BuildRidgeJaggednessSpline(
                weirdness, ridges, jaggednessFactorAtPeakRidgeAndErosionIndex1, jaggednessFactorAtHighRidgeAndErosionIndex1, jaggednessTransformer);
            return CubicSpline.CreateBuilder(erosion, jaggednessTransformer)
                .AddPoint(-1.0F, ridgeJaggednessSplineAtErosion0)
                .AddPoint(-0.78F, ridgeJaggednessSplineAtErosion1)
                .AddPoint(-0.5775F, ridgeJaggednessSplineAtErosion1)
                .AddPoint(-0.375F, 0.0F)
                .Build();
        }

        private static ICubicSpline<TContext> BuildRidgeJaggednessSpline<TContext>(
            IBoundedFloatFunction<TContext> weirdness,
            IBoundedFloatFunction<TContext> ridges,
            float jaggednessFactorAtPeakRidge,
            float jaggednessFactorAtHighRidge,
            Func<float, float> jaggednessTransformer)
        {
            float highSliceStart = PeaksAndValleys(0.4F);
            float highSliceEnd = PeaksAndValleys(0.56666666F);
            float highSliceMiddle = (highSliceStart + highSliceEnd) / 2.0F;
            CubicSpline.Builder<TContext> ridgeSpline = CubicSpline.CreateBuilder(ridges, jaggednessTransformer);
            ridgeSpline.AddPoint(highSliceStart, 0.0F);
            if (jaggednessFactorAtHighRidge > 0.0F)
            {
                ridgeSpline.AddPoint(highSliceMiddle, BuildWeirdnessJaggednessSpline(weirdness, jaggednessFactorAtHighRidge, jaggednessTransformer));
            }
            else
            {
                ridgeSpline.AddPoint(highSliceMiddle, 0.0F);
            }

            if (jaggednessFactorAtPeakRidge > 0.0F)
            {
                ridgeSpline.AddPoint(1.0F, BuildWeirdnessJaggednessSpline(weirdness, jaggednessFactorAtPeakRidge, jaggednessTransformer));
            }
            else
            {
                ridgeSpline.AddPoint(1.0F, 0.0F);
            }

            return ridgeSpline.Build();
        }

        private static ICubicSpline<TContext> BuildWeirdnessJaggednessSpline<TContext>(
            IBoundedFloatFunction<TContext> weirdness,
            float jaggednessFactor,
            Func<float, float> jaggednessTransformer)
        {
            float maxJaggednessAtNegativeWeirdness = 0.63F * jaggednessFactor;
            float maxJaggednessAtPositiveWeirdness = 0.3F * jaggednessFactor;
            return CubicSpline.CreateBuilder(weirdness, jaggednessTransformer)
                .AddPoint(-0.01F, maxJaggednessAtNegativeWeirdness)
                .AddPoint(0.01F, maxJaggednessAtPositiveWeirdness)
                .Build();
        }

        private static ICubicSpline<TContext> GetErosionFactor<TContext>(
            IBoundedFloatFunction<TContext> erosion,
            IBoundedFloatFunction<TContext> weirdness,
            IBoundedFloatFunction<TContext> ridges,
            float baseValue,
            bool shatteredTerrain,
            Func<float, float> factorTransformer)
        {
            ICubicSpline<TContext> baseSpline = CubicSpline.CreateBuilder(weirdness, factorTransformer)
                .AddPoint(-0.2F, 6.3F)
                .AddPoint(0.2F, baseValue)
                .Build();
            CubicSpline.Builder<TContext> erosionPoints = CubicSpline.CreateBuilder(erosion, factorTransformer)
                .AddPoint(-0.6F, baseSpline)
                .AddPoint(-0.5F, CubicSpline.CreateBuilder(weirdness, factorTransformer).AddPoint(-0.05F, 6.3F).AddPoint(0.05F, 2.67F).Build())
                .AddPoint(-0.35F, baseSpline)
                .AddPoint(-0.25F, baseSpline)
                .AddPoint(-0.1F, CubicSpline.CreateBuilder(weirdness, factorTransformer).AddPoint(-0.05F, 2.67F).AddPoint(0.05F, 6.3F).Build())
                .AddPoint(0.03F, baseSpline);

            if (shatteredTerrain)
            {
                ICubicSpline<TContext> weirdnessShattered = CubicSpline.CreateBuilder(weirdness, factorTransformer)
                    .AddPoint(0.0F, baseValue)
                    .AddPoint(0.1F, 0.625F)
                    .Build();
                ICubicSpline<TContext> ridgesShattered = CubicSpline.CreateBuilder(ridges, factorTransformer)
                    .AddPoint(-0.9F, baseValue)
                    .AddPoint(-0.69F, weirdnessShattered)
                    .Build();
                erosionPoints.AddPoint(0.35F, baseValue).AddPoint(0.45F, ridgesShattered).AddPoint(0.55F, ridgesShattered).AddPoint(0.62F, baseValue);
            }
            else
            {
                ICubicSpline<TContext> extremeHillsTerrainFromMidSliceAndUp = CubicSpline.CreateBuilder(ridges, factorTransformer)
                    .AddPoint(-0.7F, baseSpline)
                    .AddPoint(-0.15F, 1.37F)
                    .Build();
                ICubicSpline<TContext> extra3dNoiseOnPeaksOnly = CubicSpline.CreateBuilder(ridges, factorTransformer)
                    .AddPoint(0.45F, baseSpline)
                    .AddPoint(0.7F, 1.56F)
                    .Build();
                erosionPoints.AddPoint(0.05F, extra3dNoiseOnPeaksOnly)
                    .AddPoint(0.4F, extra3dNoiseOnPeaksOnly)
                    .AddPoint(0.45F, extremeHillsTerrainFromMidSliceAndUp)
                    .AddPoint(0.55F, extremeHillsTerrainFromMidSliceAndUp)
                    .AddPoint(0.58F, baseValue);
            }

            return erosionPoints.Build();
        }

        public static ICubicSpline<TContext> BuildErosionOffsetSpline<TContext>(
            IBoundedFloatFunction<TContext> erosion,
            IBoundedFloatFunction<TContext> ridges,
            float lowValley,
            float hill,
            float tallHill,
            float mountainFactor,
            float plain,
            float swamp,
            bool includeExtremeHills,
            bool saddle,
            Func<float, float> offsetTransformer)
        {
            ICubicSpline<TContext> veryLowErosionMountains = BuildMountainRidgeSplineWithPoints(
                ridges, (float)Mth.Lerp(mountainFactor, 0.6, 1.5), saddle, offsetTransformer);
            ICubicSpline<TContext> lowErosionMountains = BuildMountainRidgeSplineWithPoints(
                ridges, (float)Mth.Lerp(mountainFactor, 0.6, 1.0), saddle, offsetTransformer);
            ICubicSpline<TContext> mountains = BuildMountainRidgeSplineWithPoints(ridges, mountainFactor, saddle, offsetTransformer);
            ICubicSpline<TContext> widePlateau = RidgeSpline(
                ridges,
                lowValley - 0.15F,
                0.5F * mountainFactor,
                0.5F * mountainFactor,
                0.5F * mountainFactor,
                0.6F * mountainFactor,
                0.5F,
                offsetTransformer);
            ICubicSpline<TContext> narrowPlateau = RidgeSpline(
                ridges, lowValley, plain * mountainFactor, hill * mountainFactor, 0.5F * mountainFactor, 0.6F * mountainFactor, 0.5F, offsetTransformer);
            ICubicSpline<TContext> plains = RidgeSpline(ridges, lowValley, plain, plain, hill, tallHill, 0.5F, offsetTransformer);
            ICubicSpline<TContext> plainsFarInland = RidgeSpline(ridges, lowValley, plain, plain, hill, tallHill, 0.5F, offsetTransformer);
            ICubicSpline<TContext> extremeHills = CubicSpline.CreateBuilder(ridges, offsetTransformer)
                .AddPoint(-1.0F, lowValley)
                .AddPoint(-0.4F, plains)
                .AddPoint(0.0F, tallHill + 0.07F)
                .Build();
            ICubicSpline<TContext> swamps = RidgeSpline(ridges, -0.02F, swamp, swamp, hill, tallHill, 0.0F, offsetTransformer);
            CubicSpline.Builder<TContext> builder = CubicSpline.CreateBuilder(erosion, offsetTransformer)
                .AddPoint(-0.85F, veryLowErosionMountains)
                .AddPoint(-0.7F, lowErosionMountains)
                .AddPoint(-0.4F, mountains)
                .AddPoint(-0.35F, widePlateau)
                .AddPoint(-0.1F, narrowPlateau)
                .AddPoint(0.2F, plains);
            if (includeExtremeHills)
            {
                builder.AddPoint(0.4F, plainsFarInland).AddPoint(0.45F, extremeHills).AddPoint(0.55F, extremeHills).AddPoint(0.58F, plainsFarInland);
            }

            builder.AddPoint(0.7F, swamps);
            return builder.Build();
        }

        private static ICubicSpline<TContext> RidgeSpline<TContext>(
            IBoundedFloatFunction<TContext> ridges,
            float valley,
            float low,
            float mid,
            float high,
            float peaks,
            float minValleySteepness,
            Func<float, float> offsetTransformer)
        {
            float d1 = Math.Max(0.5F * (low - valley), minValleySteepness);
            float d2 = 5.0F * (mid - low);
            return CubicSpline.CreateBuilder(ridges, offsetTransformer)
                .AddPoint(-1.0F, valley, d1)
                .AddPoint(-0.4F, low, Math.Min(d1, d2))
                .AddPoint(0.0F, mid, d2)
                .AddPoint(0.4F, high, 2.0F * (high - mid))
                .AddPoint(1.0F, peaks, 0.7F * (peaks - high))
                .Build();
        }

        private static ICubicSpline<TContext> BuildMountainRidgeSplineWithPoints<TContext>(
            IBoundedFloatFunction<TContext> ridges,
            float modulation,
            bool saddle,
            Func<float, float> offsetTransformer)
        {
            CubicSpline.Builder<TContext> build = CubicSpline.CreateBuilder(ridges, offsetTransformer);
            const float allowRiversBelow = -0.7F;
            float minPointContinentalness = MountainContinentalness(-1.0F, modulation, allowRiversBelow);
            float maxPointContinentalness = MountainContinentalness(1.0F, modulation, allowRiversBelow);
            float ridgeZeroPoint = CalculateMountainRidgeZeroContinentalnessPoint(modulation);
            if (-0.65F < ridgeZeroPoint && ridgeZeroPoint < 1.0F)
            {
                float afterRiverThresholdContinentalness = MountainContinentalness(-0.65F, modulation, allowRiversBelow);
                float beforeRiverThresholdContinentalness = MountainContinentalness(-0.75F, modulation, allowRiversBelow);
                float minPointDerivative = CalculateSlope(minPointContinentalness, beforeRiverThresholdContinentalness, -1.0F, -0.75F);
                build.AddPoint(-1.0F, minPointContinentalness, minPointDerivative);
                build.AddPoint(-0.75F, beforeRiverThresholdContinentalness);
                build.AddPoint(-0.65F, afterRiverThresholdContinentalness);
                float ridgeZeroPointContinentalness = MountainContinentalness(ridgeZeroPoint, modulation, allowRiversBelow);
                float maxPointDerivative = CalculateSlope(ridgeZeroPointContinentalness, maxPointContinentalness, ridgeZeroPoint, 1.0F);
                build.AddPoint(ridgeZeroPoint - 0.01F, ridgeZeroPointContinentalness);
                build.AddPoint(ridgeZeroPoint, ridgeZeroPointContinentalness, maxPointDerivative);
                build.AddPoint(1.0F, maxPointContinentalness, maxPointDerivative);
            }
            else
            {
                float simpleDerivative = CalculateSlope(minPointContinentalness, maxPointContinentalness, -1.0F, 1.0F);
                if (saddle)
                {
                    build.AddPoint(-1.0F, Math.Max(0.2F, minPointContinentalness));
                    build.AddPoint(0.0F, (float)Mth.Lerp(0.5F, minPointContinentalness, maxPointContinentalness), simpleDerivative);
                }
                else
                {
                    build.AddPoint(-1.0F, minPointContinentalness, simpleDerivative);
                }

                build.AddPoint(1.0F, maxPointContinentalness, simpleDerivative);
            }

            return build.Build();
        }

        private static float MountainContinentalness(float ridge, float modulation, float allowRiversBelow)
        {
            const float ridgeOffset = 1.17F;
            const float ridgeAmplitude = 0.46082947F;
            float ridgeSlope = 1.0F - (1.0F - modulation) * 0.5F;
            float ridgeIntersect = 0.5F * (1.0F - modulation);
            float adjustedRidgeHeight = (ridge + ridgeOffset) * ridgeAmplitude;
            float continentalness = adjustedRidgeHeight * ridgeSlope - ridgeIntersect;
            return ridge < allowRiversBelow ? Math.Max(continentalness, -0.2222F) : Math.Max(continentalness, 0.0F);
        }

        private static float CalculateMountainRidgeZeroContinentalnessPoint(float modulation)
        {
            const float ridgeOffset = 1.17F;
            const float ridgeAmplitude = 0.46082947F;
            float ridgeSlope = 1.0F - (1.0F - modulation) * 0.5F;
            float ridgeIntersect = 0.5F * (1.0F - modulation);
            return ridgeIntersect / (ridgeAmplitude * ridgeSlope) - ridgeOffset;
        }

        private static float CalculateSlope(float y1, float y2, float x1, float x2) => (y2 - y1) / (x2 - x1);
    }
}
