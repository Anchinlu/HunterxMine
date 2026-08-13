using System;

namespace MineCraftUnity.WorldGen.Noise
{
    /// <summary>
    /// MC ref: NormalNoise.NoiseParameters — firstOctave + amplitudes from worldgen/noise/*.json
    /// </summary>
    [Serializable]
    public sealed class NoiseParameters
    {
        public int FirstOctave { get; }
        public double[] Amplitudes { get; }

        public NoiseParameters(int firstOctave, double[] amplitudes)
        {
            FirstOctave = firstOctave;
            Amplitudes = amplitudes ?? Array.Empty<double>();
        }
    }
}
