using System;
using System.Linq;
using MineCraftUnity.WorldGen.Density;

namespace MineCraftUnity.WorldGen.Synth
{
    /// <summary>
    /// MC ref: net.minecraft.world.level.levelgen.synth.BlendedNoise
    /// </summary>
    public sealed class BlendedNoise : IDensityFunction
    {
        private readonly PerlinNoise _minLimitNoise;
        private readonly PerlinNoise _maxLimitNoise;
        private readonly PerlinNoise _mainNoise;
        private readonly double _xzMultiplier;
        private readonly double _yMultiplier;
        private readonly double _xzFactor;
        private readonly double _yFactor;
        private readonly double _smearScaleMultiplier;
        private readonly double _maxValue;
        private readonly double _xzScale;
        private readonly double _yScale;

        public static BlendedNoise CreateUnseeded(
            double xzScale,
            double yScale,
            double xzFactor,
            double yFactor,
            double smearScaleMultiplier) =>
            new(new XoroshiroRandomSource(0L), xzScale, yScale, xzFactor, yFactor, smearScaleMultiplier);

        private BlendedNoise(
            PerlinNoise minLimitNoise,
            PerlinNoise maxLimitNoise,
            PerlinNoise mainNoise,
            double xzScale,
            double yScale,
            double xzFactor,
            double yFactor,
            double smearScaleMultiplier)
        {
            _minLimitNoise = minLimitNoise;
            _maxLimitNoise = maxLimitNoise;
            _mainNoise = mainNoise;
            _xzScale = xzScale;
            _yScale = yScale;
            _xzFactor = xzFactor;
            _yFactor = yFactor;
            _smearScaleMultiplier = smearScaleMultiplier;
            _xzMultiplier = 684.412 * _xzScale;
            _yMultiplier = 684.412 * _yScale;
            _maxValue = minLimitNoise.MaxBrokenValue(_yMultiplier);
        }

        public BlendedNoise(
            IRandomSource random,
            double xzScale,
            double yScale,
            double xzFactor,
            double yFactor,
            double smearScaleMultiplier)
            : this(
                PerlinNoise.CreateLegacyForBlendedNoise(random, Enumerable.Range(-15, 16)),
                PerlinNoise.CreateLegacyForBlendedNoise(random, Enumerable.Range(-15, 16)),
                PerlinNoise.CreateLegacyForBlendedNoise(random, Enumerable.Range(-7, 8)),
                xzScale,
                yScale,
                xzFactor,
                yFactor,
                smearScaleMultiplier)
        {
        }

        public BlendedNoise WithNewRandom(IRandomSource terrainRandom) =>
            new(terrainRandom, _xzScale, _yScale, _xzFactor, _yFactor, _smearScaleMultiplier);

        public double Compute(in DensityContext context)
        {
            double limitX = context.BlockX * _xzMultiplier;
            double limitY = context.BlockY * _yMultiplier;
            double limitZ = context.BlockZ * _xzMultiplier;
            double mainX = limitX / _xzFactor;
            double mainY = limitY / _yFactor;
            double mainZ = limitZ / _xzFactor;
            double limitSmear = _yMultiplier * _smearScaleMultiplier;
            double mainSmear = limitSmear / _yFactor;
            double blendMin = 0.0;
            double blendMax = 0.0;
            double mainNoiseValue = 0.0;
            double pow = 1.0;

            for (var i = 0; i < 8; i++)
            {
                ImprovedNoise noise = _mainNoise.GetOctaveNoise(i);
                if (noise != null)
                {
                    mainNoiseValue += noise.Noise(
                            PerlinNoise.Wrap(mainX * pow),
                            PerlinNoise.Wrap(mainY * pow),
                            PerlinNoise.Wrap(mainZ * pow),
                            mainSmear * pow,
                            mainY * pow)
                        / pow;
                }

                pow /= 2.0;
            }

            double factor = (mainNoiseValue / 10.0 + 1.0) / 2.0;
            bool isMax = factor >= 1.0;
            bool isMin = factor <= 0.0;
            pow = 1.0;

            for (var i = 0; i < 16; i++)
            {
                double wx = PerlinNoise.Wrap(limitX * pow);
                double wy = PerlinNoise.Wrap(limitY * pow);
                double wz = PerlinNoise.Wrap(limitZ * pow);
                double yScalePow = limitSmear * pow;
                if (!isMax)
                {
                    ImprovedNoise minNoise = _minLimitNoise.GetOctaveNoise(i);
                    if (minNoise != null)
                    {
                        blendMin += minNoise.Noise(wx, wy, wz, yScalePow, limitY * pow) / pow;
                    }
                }

                if (!isMin)
                {
                    ImprovedNoise maxNoise = _maxLimitNoise.GetOctaveNoise(i);
                    if (maxNoise != null)
                    {
                        blendMax += maxNoise.Noise(wx, wy, wz, yScalePow, limitY * pow) / pow;
                    }
                }

                pow /= 2.0;
            }

            return Mth.ClampedLerp(factor, blendMin / 512.0, blendMax / 512.0) / 128.0;
        }

        public double MinValue => -MaxValue;

        public double MaxValue => _maxValue;
    }
}
