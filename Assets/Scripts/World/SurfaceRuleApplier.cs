using MineCraftUnity.Blocks;
using MineCraftUnity.Core;

namespace MineCraftUnity.World
{
    /// <summary>
    /// MC ref: SurfaceRules in NoiseBasedChunkGenerator — grass/dirt/stone/sand/water layering.
    /// </summary>
    public static class SurfaceRuleApplier
    {
        public static BlockId GetBlockForColumn(int worldY, int surfaceHeight, bool useSandSurface)
        {
            if (worldY <= WorldConstants.MinY + WorldConstants.BedrockLayers - 1)
            {
                return BlockId.Bedrock;
            }

            if (worldY > surfaceHeight)
            {
                return worldY <= WorldConstants.SeaLevel ? BlockId.Water : BlockId.Air;
            }

            if (worldY == surfaceHeight)
            {
                if (surfaceHeight <= WorldConstants.SeaLevel)
                {
                    return useSandSurface ? BlockId.Sand : BlockId.Dirt;
                }

                return BlockId.GrassBlock;
            }

            if (worldY > surfaceHeight - WorldConstants.DirtDepth)
            {
                return useSandSurface && surfaceHeight <= WorldConstants.SeaLevel + 2
                    ? BlockId.Sand
                    : BlockId.Dirt;
            }

            return BlockId.Stone;
        }

        public static bool ShouldUseSandSurface(int surfaceHeight, float continental)
        {
            return surfaceHeight <= WorldConstants.SeaLevel + 2 && continental < 0.42f;
        }
    }
}
