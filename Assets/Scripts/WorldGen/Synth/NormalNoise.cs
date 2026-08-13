using MineCraftUnity.WorldGen.Noise;

namespace MineCraftUnity.WorldGen.Synth
{
    /// <summary>
    /// MC ref: net.minecraft.world.level.levelgen.synth.NormalNoise
    /// </summary>
    public sealed class NormalNoise
    {
        private const double InputFactor = 1.0181268882175227;

        private readonly double _valueFactor;
        private readonly PerlinNoise _first;
        private readonly PerlinNoise _second;
        private readonly double _maxValue;
        private readonly NoiseParameters _parameters;

        public static NormalNoise CreateLegacyNetherBiome(IRandomSource random, NoiseParameters parameters) =>
            new(random, parameters, useNewInitialization: false);

        public static NormalNoise Create(IRandomSource random, int firstOctave, params double[] amplitudes) =>
            Create(random, new NoiseParameters(firstOctave, amplitudes));

        public static NormalNoise Create(IRandomSource random, NoiseParameters parameters) =>
            new(random, parameters, useNewInitialization: true);

        private NormalNoise(IRandomSource random, NoiseParameters parameters, bool useNewInitialization)
        {
            int firstOctave = parameters.FirstOctave;
            double[] amplitudes = parameters.Amplitudes;
            _parameters = parameters;

            if (useNewInitialization)
            {
                _first = PerlinNoise.Create(random, firstOctave, amplitudes);
                _second = PerlinNoise.Create(random, firstOctave, amplitudes);
            }
            else
            {
                _first = PerlinNoise.CreateLegacyForLegacyNetherBiome(random, firstOctave, amplitudes);
                _second = PerlinNoise.CreateLegacyForLegacyNetherBiome(random, firstOctave, amplitudes);
            }

            int minOctave = int.MaxValue;
            int maxOctave = int.MinValue;
            for (var i = 0; i < amplitudes.Length; i++)
            {
                double amplitude = amplitudes[i];
                if (amplitude != 0.0)
                {
                    minOctave = System.Math.Min(minOctave, i);
                    maxOctave = System.Math.Max(maxOctave, i);
                }
            }

            _valueFactor = 0.16666666666666666 / ExpectedDeviation(maxOctave - minOctave);
            _maxValue = (_first.MaxValue() + _second.MaxValue()) * _valueFactor;
        }

        public double MaxValue() => _maxValue;

        private static double ExpectedDeviation(int octaveSpan) => 0.1 * (1.0 + 1.0 / (octaveSpan + 1));

        public double GetValue(double x, double y, double z)
        {
            double x2 = x * InputFactor;
            double y2 = y * InputFactor;
            double z2 = z * InputFactor;
            return (_first.GetValue(x, y, z) + _second.GetValue(x2, y2, z2)) * _valueFactor;
        }

        public NoiseParameters Parameters => _parameters;
    }
}
