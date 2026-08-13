using System;
using System.Collections.Generic;
using System.Linq;

namespace MineCraftUnity.WorldGen.Synth
{
    /// <summary>
    /// MC ref: net.minecraft.world.level.levelgen.synth.PerlinNoise
    /// </summary>
    public sealed class PerlinNoise
    {
        private readonly ImprovedNoise[] _noiseLevels;
        private readonly int _firstOctave;
        private readonly double[] _amplitudes;
        private readonly double _lowestFreqValueFactor;
        private readonly double _lowestFreqInputFactor;
        private readonly double _maxValue;

        public static PerlinNoise CreateLegacyForBlendedNoise(IRandomSource random, IEnumerable<int> octaves) =>
            new(random, MakeAmplitudes(new SortedSet<int>(octaves)), useNewInitialization: false);

        public static PerlinNoise CreateLegacyForLegacyNetherBiome(IRandomSource random, int firstOctave, double[] amplitudes) =>
            new(random, firstOctave, amplitudes, useNewInitialization: false);

        public static PerlinNoise Create(IRandomSource random, IEnumerable<int> octaves) =>
            Create(random, octaves.ToList());

        public static PerlinNoise Create(IRandomSource random, IReadOnlyList<int> octaveSet) =>
            new(random, MakeAmplitudes(new SortedSet<int>(octaveSet)), useNewInitialization: true);

        public static PerlinNoise Create(IRandomSource random, int firstOctave, double firstAmplitude, params double[] amplitudes)
        {
            var amplitudeList = new List<double>(amplitudes);
            amplitudeList.Insert(0, firstAmplitude);
            return new PerlinNoise(random, firstOctave, amplitudeList.ToArray(), useNewInitialization: true);
        }

        public static PerlinNoise Create(IRandomSource random, int firstOctave, double[] amplitudes) =>
            new(random, firstOctave, amplitudes, useNewInitialization: true);

        private static (int FirstOctave, double[] Amplitudes) MakeAmplitudes(SortedSet<int> octaveSet)
        {
            if (octaveSet.Count == 0)
            {
                throw new ArgumentException("Need some octaves!");
            }

            int lowFreqOctaves = -octaveSet.Min;
            int highFreqOctaves = octaveSet.Max;
            int octaves = lowFreqOctaves + highFreqOctaves + 1;
            if (octaves < 1)
            {
                throw new ArgumentException("Total number of octaves needs to be >= 1");
            }

            var amplitudes = new double[octaves];
            foreach (int octave in octaveSet)
            {
                amplitudes[octave + lowFreqOctaves] = 1.0;
            }

            return (-lowFreqOctaves, amplitudes);
        }

        private PerlinNoise(IRandomSource random, (int FirstOctave, double[] Amplitudes) pair, bool useNewInitialization)
            : this(random, pair.FirstOctave, pair.Amplitudes, useNewInitialization)
        {
        }

        private PerlinNoise(IRandomSource random, int firstOctave, double[] amplitudes, bool useNewInitialization)
        {
            _firstOctave = firstOctave;
            _amplitudes = amplitudes;
            int octaves = _amplitudes.Length;
            int zeroOctaveIndex = -_firstOctave;
            _noiseLevels = new ImprovedNoise[octaves];

            if (useNewInitialization)
            {
                IPositionalRandomFactory positional = random.ForkPositional();
                for (var i = 0; i < octaves; i++)
                {
                    if (_amplitudes[i] != 0.0)
                    {
                        int octave = _firstOctave + i;
                        _noiseLevels[i] = new ImprovedNoise(positional.FromHashOf("octave_" + octave));
                    }
                }
            }
            else
            {
                var zeroOctave = new ImprovedNoise(random);
                if (zeroOctaveIndex >= 0 && zeroOctaveIndex < octaves)
                {
                    double zeroOctaveAmplitude = _amplitudes[zeroOctaveIndex];
                    if (zeroOctaveAmplitude != 0.0)
                    {
                        _noiseLevels[zeroOctaveIndex] = zeroOctave;
                    }
                }

                for (int ix = zeroOctaveIndex - 1; ix >= 0; ix--)
                {
                    if (ix < octaves)
                    {
                        double amplitude = _amplitudes[ix];
                        if (amplitude != 0.0)
                        {
                            _noiseLevels[ix] = new ImprovedNoise(random);
                        }
                        else
                        {
                            SkipOctave(random);
                        }
                    }
                    else
                    {
                        SkipOctave(random);
                    }
                }

                int nonNullCount = _noiseLevels.Count(n => n != null);
                int nonZeroAmplitudeCount = _amplitudes.Count(a => a != 0.0);
                if (nonNullCount != nonZeroAmplitudeCount)
                {
                    throw new InvalidOperationException("Failed to create correct number of noise levels for given non-zero amplitudes");
                }

                if (zeroOctaveIndex < octaves - 1)
                {
                    throw new ArgumentException("Positive octaves are temporarily disabled");
                }
            }

            _lowestFreqInputFactor = Math.Pow(2.0, -zeroOctaveIndex);
            _lowestFreqValueFactor = Math.Pow(2.0, octaves - 1) / (Math.Pow(2.0, octaves) - 1.0);
            _maxValue = EdgeValue(2.0);
        }

        public double MaxValue() => _maxValue;

        private static void SkipOctave(IRandomSource random) => random.ConsumeCount(262);

        public double GetValue(double x, double y, double z) => GetValue(x, y, z, 0.0, 0.0);

        public double GetValue(double x, double y, double z, double yScale, double yFudge)
        {
            double value = 0.0;
            double factor = _lowestFreqInputFactor;
            double valueFactor = _lowestFreqValueFactor;

            for (var i = 0; i < _noiseLevels.Length; i++)
            {
                ImprovedNoise noise = _noiseLevels[i];
                if (noise != null)
                {
                    double noiseVal = noise.Noise(
                        Wrap(x * factor),
                        Wrap(y * factor),
                        Wrap(z * factor),
                        yScale * factor,
                        yFudge * factor);
                    value += _amplitudes[i] * noiseVal * valueFactor;
                }

                factor *= 2.0;
                valueFactor /= 2.0;
            }

            return value;
        }

        public double MaxBrokenValue(double yScale) => EdgeValue(yScale + 2.0);

        private double EdgeValue(double noiseValue)
        {
            double value = 0.0;
            double valueFactor = _lowestFreqValueFactor;

            for (var i = 0; i < _noiseLevels.Length; i++)
            {
                ImprovedNoise noise = _noiseLevels[i];
                if (noise != null)
                {
                    value += _amplitudes[i] * noiseValue * valueFactor;
                }

                valueFactor /= 2.0;
            }

            return value;
        }

        public ImprovedNoise GetOctaveNoise(int i) => _noiseLevels[_noiseLevels.Length - 1 - i];

        public static double Wrap(double x) => x - Mth.LFloor(x / 3.3554432E7 + 0.5) * 3.3554432E7;

        public int FirstOctave => _firstOctave;

        public IReadOnlyList<double> Amplitudes => _amplitudes;
    }
}
