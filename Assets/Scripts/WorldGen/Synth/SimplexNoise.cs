using System;

namespace MineCraftUnity.WorldGen.Synth
{
    /// <summary>
    /// MC ref: net.minecraft.world.level.levelgen.synth.SimplexNoise — gradient table for ImprovedNoise.
    /// </summary>
    public static class SimplexNoise
    {
        public static readonly int[][] Gradient =
        {
            new[] { 1, 1, 0 },
            new[] { -1, 1, 0 },
            new[] { 1, -1, 0 },
            new[] { -1, -1, 0 },
            new[] { 1, 0, 1 },
            new[] { -1, 0, 1 },
            new[] { 1, 0, -1 },
            new[] { -1, 0, -1 },
            new[] { 0, 1, 1 },
            new[] { 0, -1, 1 },
            new[] { 0, 1, -1 },
            new[] { 0, -1, -1 },
            new[] { 1, 1, 0 },
            new[] { 0, -1, 1 },
            new[] { -1, 1, 0 },
            new[] { 0, -1, -1 }
        };

        public static double Dot(int[] g, double x, double y, double z) => g[0] * x + g[1] * y + g[2] * z;
    }
}
