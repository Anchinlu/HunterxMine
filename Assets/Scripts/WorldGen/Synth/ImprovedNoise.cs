using System;

namespace MineCraftUnity.WorldGen.Synth
{
    /// <summary>
    /// MC ref: net.minecraft.world.level.levelgen.synth.ImprovedNoise
    /// </summary>
    public sealed class ImprovedNoise
    {
        private const float ShiftUpEpsilon = 1.0E-7F;

        private readonly byte[] _p;
        public readonly double Xo;
        public readonly double Yo;
        public readonly double Zo;

        public ImprovedNoise(IRandomSource random)
        {
            Xo = random.NextDouble() * 256.0;
            Yo = random.NextDouble() * 256.0;
            Zo = random.NextDouble() * 256.0;
            _p = new byte[256];

            for (var i = 0; i < 256; i++)
            {
                _p[i] = (byte)i;
            }

            for (var i = 0; i < 256; i++)
            {
                int offset = random.NextInt(256 - i);
                byte tmp = _p[i];
                _p[i] = _p[i + offset];
                _p[i + offset] = tmp;
            }
        }

        public double Noise(double x, double y, double z) => Noise(x, y, z, 0.0, 0.0);

        public double Noise(double x, double y, double z, double yScale, double yFudge)
        {
            x += Xo;
            y += Yo;
            z += Zo;
            int xf = Mth.Floor(x);
            int yf = Mth.Floor(y);
            int zf = Mth.Floor(z);
            double xr = x - xf;
            double yr = y - yf;
            double zr = z - zf;
            double yrFudge;
            if (yScale != 0.0)
            {
                double fudgeLimit = yFudge >= 0.0 && yFudge < yr ? yFudge : yr;
                yrFudge = Mth.Floor(fudgeLimit / yScale + ShiftUpEpsilon) * yScale;
            }
            else
            {
                yrFudge = 0.0;
            }

            return SampleAndLerp(xf, yf, zf, xr, yr - yrFudge, zr, yr);
        }

        public double NoiseWithDerivative(double x, double y, double z, double[] derivativeOut)
        {
            x += Xo;
            y += Yo;
            z += Zo;
            int xf = Mth.Floor(x);
            int yf = Mth.Floor(y);
            int zf = Mth.Floor(z);
            double xr = x - xf;
            double yr = y - yf;
            double zr = z - zf;
            return SampleWithDerivative(xf, yf, zf, xr, yr, zr, derivativeOut);
        }

        private static double GradDot(int hash, double x, double y, double z) =>
            SimplexNoise.Dot(SimplexNoise.Gradient[hash & 15], x, y, z);

        private int P(int x) => _p[x & 0xFF];

        private double SampleAndLerp(int x, int y, int z, double xr, double yr, double zr, double yrOriginal)
        {
            int x0 = P(x);
            int x1 = P(x + 1);
            int xy00 = P(x0 + y);
            int xy01 = P(x0 + y + 1);
            int xy10 = P(x1 + y);
            int xy11 = P(x1 + y + 1);
            double d000 = GradDot(P(xy00 + z), xr, yr, zr);
            double d100 = GradDot(P(xy10 + z), xr - 1.0, yr, zr);
            double d010 = GradDot(P(xy01 + z), xr, yr - 1.0, zr);
            double d110 = GradDot(P(xy11 + z), xr - 1.0, yr - 1.0, zr);
            double d001 = GradDot(P(xy00 + z + 1), xr, yr, zr - 1.0);
            double d101 = GradDot(P(xy10 + z + 1), xr - 1.0, yr, zr - 1.0);
            double d011 = GradDot(P(xy01 + z + 1), xr, yr - 1.0, zr - 1.0);
            double d111 = GradDot(P(xy11 + z + 1), xr - 1.0, yr - 1.0, zr - 1.0);
            double xAlpha = Mth.Smoothstep(xr);
            double yAlpha = Mth.Smoothstep(yrOriginal);
            double zAlpha = Mth.Smoothstep(zr);
            return Mth.Lerp3(xAlpha, yAlpha, zAlpha, d000, d100, d010, d110, d001, d101, d011, d111);
        }

        private double SampleWithDerivative(int x, int y, int z, double xr, double yr, double zr, double[] derivativeOut)
        {
            int x0 = P(x);
            int x1 = P(x + 1);
            int xy00 = P(x0 + y);
            int xy01 = P(x0 + y + 1);
            int xy10 = P(x1 + y);
            int xy11 = P(x1 + y + 1);
            int p000 = P(xy00 + z);
            int p100 = P(xy10 + z);
            int p010 = P(xy01 + z);
            int p110 = P(xy11 + z);
            int p001 = P(xy00 + z + 1);
            int p101 = P(xy10 + z + 1);
            int p011 = P(xy01 + z + 1);
            int p111 = P(xy11 + z + 1);
            int[] g000 = SimplexNoise.Gradient[p000 & 15];
            int[] g100 = SimplexNoise.Gradient[p100 & 15];
            int[] g010 = SimplexNoise.Gradient[p010 & 15];
            int[] g110 = SimplexNoise.Gradient[p110 & 15];
            int[] g001 = SimplexNoise.Gradient[p001 & 15];
            int[] g101 = SimplexNoise.Gradient[p101 & 15];
            int[] g011 = SimplexNoise.Gradient[p011 & 15];
            int[] g111 = SimplexNoise.Gradient[p111 & 15];
            double d000 = SimplexNoise.Dot(g000, xr, yr, zr);
            double d100 = SimplexNoise.Dot(g100, xr - 1.0, yr, zr);
            double d010 = SimplexNoise.Dot(g010, xr, yr - 1.0, zr);
            double d110 = SimplexNoise.Dot(g110, xr - 1.0, yr - 1.0, zr);
            double d001 = SimplexNoise.Dot(g001, xr, yr, zr - 1.0);
            double d101 = SimplexNoise.Dot(g101, xr - 1.0, yr, zr - 1.0);
            double d011 = SimplexNoise.Dot(g011, xr, yr - 1.0, zr - 1.0);
            double d111 = SimplexNoise.Dot(g111, xr - 1.0, yr - 1.0, zr - 1.0);
            double xAlpha = Mth.Smoothstep(xr);
            double yAlpha = Mth.Smoothstep(yr);
            double zAlpha = Mth.Smoothstep(zr);
            double d1x = Mth.Lerp3(xAlpha, yAlpha, zAlpha, g000[0], g100[0], g010[0], g110[0], g001[0], g101[0], g011[0], g111[0]);
            double d1y = Mth.Lerp3(xAlpha, yAlpha, zAlpha, g000[1], g100[1], g010[1], g110[1], g001[1], g101[1], g011[1], g111[1]);
            double d1z = Mth.Lerp3(xAlpha, yAlpha, zAlpha, g000[2], g100[2], g010[2], g110[2], g001[2], g101[2], g011[2], g111[2]);
            double d2x = Mth.Lerp2(yAlpha, zAlpha, d100 - d000, d110 - d010, d101 - d001, d111 - d011);
            double d2y = Mth.Lerp2(zAlpha, xAlpha, d010 - d000, d011 - d001, d110 - d100, d111 - d101);
            double d2z = Mth.Lerp2(xAlpha, yAlpha, d001 - d000, d101 - d100, d011 - d010, d111 - d110);
            double xSd = Mth.SmoothstepDerivative(xr);
            double ySd = Mth.SmoothstepDerivative(yr);
            double zSd = Mth.SmoothstepDerivative(zr);
            derivativeOut[0] += d1x + xSd * d2x;
            derivativeOut[1] += d1y + ySd * d2y;
            derivativeOut[2] += d1z + zSd * d2z;
            return Mth.Lerp3(xAlpha, yAlpha, zAlpha, d000, d100, d010, d110, d001, d101, d011, d111);
        }
    }
}
