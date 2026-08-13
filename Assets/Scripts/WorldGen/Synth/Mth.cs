using System;

namespace MineCraftUnity.WorldGen.Synth
{
    /// <summary>
    /// MC ref: net.minecraft.util.Mth — math helpers used by noise synthesis.
    /// </summary>
    public static class Mth
    {
        public static int Floor(double v) => (int)Math.Floor(v);

        public static long LFloor(double v) => (long)Math.Floor(v);

        public static double Lerp(double alpha, double p0, double p1) => p0 + alpha * (p1 - p0);

        public static double Lerp2(double alpha1, double alpha2, double x00, double x10, double x01, double x11)
        {
            return Lerp(alpha2, Lerp(alpha1, x00, x10), Lerp(alpha1, x01, x11));
        }

        public static double Lerp3(
            double alpha1,
            double alpha2,
            double alpha3,
            double x000,
            double x100,
            double x010,
            double x110,
            double x001,
            double x101,
            double x011,
            double x111)
        {
            return Lerp(alpha3, Lerp2(alpha1, alpha2, x000, x100, x010, x110), Lerp2(alpha1, alpha2, x001, x101, x011, x111));
        }

        public static double ClampedLerp(double factor, double min, double max)
        {
            if (factor < 0.0)
            {
                return min;
            }

            return factor > 1.0 ? max : Lerp(factor, min, max);
        }

        public static double Clamp(double value, double min, double max) => Math.Max(min, Math.Min(max, value));

        public static float Clamp(float value, float min, float max) => Math.Max(min, Math.Min(max, value));

        public static double ClampedMap(double value, double fromMin, double fromMax, double toMin, double toMax)
        {
            double t = Clamp((value - fromMin) / (fromMax - fromMin), 0.0, 1.0);
            return Lerp(t, toMin, toMax);
        }

        public static int BinarySearch(int min, int max, System.Func<int, bool> predicate)
        {
            var i = min;
            while (i < max)
            {
                if (predicate(i))
                {
                    return i;
                }

                i++;
            }

            return i;
        }

        public static double Smoothstep(double x) => x * x * x * (x * (x * 6.0 - 15.0) + 10.0);

        public static double SmoothstepDerivative(double x) => 30.0 * x * x * (x - 1.0) * (x - 1.0);

        /// <summary>MC ref: Mth.getSeed(int x, int y, int z)</summary>
        public static long GetSeed(int x, int y, int z)
        {
            unchecked
            {
                long seed = (long)(x * 3129871) ^ z * 116129781L ^ y;
                seed = seed * seed * 42317861L + seed * 11L;
                return seed >> 16;
            }
        }
    }
}
