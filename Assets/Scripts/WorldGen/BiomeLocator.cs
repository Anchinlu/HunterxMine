using MineCraftUnity.Core;
using MineCraftUnity.World;

namespace MineCraftUnity.WorldGen
{
    /// <summary>Spiral climate search for a target overworld biome (MC /locate biome style).</summary>
    public static class BiomeLocator
    {
        public const int DefaultStepBlocks = 32;
        public const int DefaultMaxRadiusBlocks = 8192;

        public readonly struct LocateResult
        {
            public bool Found { get; }
            public int BlockX { get; }
            public int BlockZ { get; }
            public int DistanceBlocks { get; }

            public LocateResult(bool found, int blockX, int blockZ, int distanceBlocks)
            {
                Found = found;
                BlockX = blockX;
                BlockZ = blockZ;
                DistanceBlocks = distanceBlocks;
            }
        }

        public static LocateResult Locate(
            RandomState randomState,
            BiomeId target,
            int originBlockX,
            int originBlockZ,
            int maxRadiusBlocks = DefaultMaxRadiusBlocks,
            int stepBlocks = DefaultStepBlocks)
        {
            if (target == BiomeId.Unknown)
            {
                return default;
            }

            stepBlocks = System.Math.Max(4, stepBlocks);
            maxRadiusBlocks = System.Math.Max(stepBlocks, maxRadiusBlocks);

            if (MatchesBiome(randomState, target, originBlockX, originBlockZ))
            {
                return new LocateResult(true, originBlockX, originBlockZ, 0);
            }

            for (var radius = stepBlocks; radius <= maxRadiusBlocks; radius += stepBlocks)
            {
                if (TryRing(randomState, target, originBlockX, originBlockZ, radius, stepBlocks, out var foundX, out var foundZ))
                {
                    var dx = foundX - originBlockX;
                    var dz = foundZ - originBlockZ;
                    var distance = (int)System.Math.Round(System.Math.Sqrt(dx * dx + dz * dz));
                    return new LocateResult(true, foundX, foundZ, distance);
                }
            }

            return default;
        }

        private static bool TryRing(
            RandomState randomState,
            BiomeId target,
            int centerX,
            int centerZ,
            int radius,
            int stepBlocks,
            out int foundX,
            out int foundZ)
        {
            foundX = 0;
            foundZ = 0;

            var minX = centerX - radius;
            var maxX = centerX + radius;
            var minZ = centerZ - radius;
            var maxZ = centerZ + radius;

            for (var x = minX; x <= maxX; x += stepBlocks)
            {
                if (MatchesBiome(randomState, target, x, minZ))
                {
                    foundX = x;
                    foundZ = minZ;
                    return true;
                }

                if (minZ != maxZ && MatchesBiome(randomState, target, x, maxZ))
                {
                    foundX = x;
                    foundZ = maxZ;
                    return true;
                }
            }

            for (var z = minZ + stepBlocks; z < maxZ; z += stepBlocks)
            {
                if (MatchesBiome(randomState, target, minX, z))
                {
                    foundX = minX;
                    foundZ = z;
                    return true;
                }

                if (minX != maxX && MatchesBiome(randomState, target, maxX, z))
                {
                    foundX = maxX;
                    foundZ = z;
                    return true;
                }
            }

            return false;
        }

        private static bool MatchesBiome(RandomState randomState, BiomeId target, int blockX, int blockZ)
        {
            var sampleY = WorldConstants.SeaLevel;
            var climate = randomState.SampleClimate(blockX, sampleY, blockZ);
            return OverworldBiomeResolver.Resolve(climate) == target;
        }
    }
}
