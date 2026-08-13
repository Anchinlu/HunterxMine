using System.Collections.Generic;

namespace MineCraftUnity.Blocks
{
    /// <summary>
    /// MC ref: net.minecraft.world.level.block.Blocks / BuiltInRegistries.BLOCK
    /// </summary>
    public static class BlockRegistry
    {
        private static readonly Dictionary<BlockId, BlockDefinition> Definitions = new()
        {
            [BlockId.Air] = new BlockDefinition(BlockId.Air, "air", BlockRenderKind.None, false, false),
            [BlockId.GrassBlock] = new BlockDefinition(BlockId.GrassBlock, "grass_block", BlockRenderKind.GrassBlock, true, true),
            [BlockId.Dirt] = new BlockDefinition(BlockId.Dirt, "dirt", BlockRenderKind.Cube, true, true),
            [BlockId.Stone] = new BlockDefinition(BlockId.Stone, "stone", BlockRenderKind.Cube, true, true),
            [BlockId.Sand] = new BlockDefinition(BlockId.Sand, "sand", BlockRenderKind.Cube, true, true),
            [BlockId.Water] = new BlockDefinition(BlockId.Water, "water", BlockRenderKind.Cube, false, false),
            [BlockId.Bedrock] = new BlockDefinition(BlockId.Bedrock, "bedrock", BlockRenderKind.Cube, true, true)
        };

        public static BlockDefinition Get(BlockId id) => Definitions[id];

        public static bool IsSolid(BlockId id) => Definitions[id].IsSolid;

        public static bool CullsSameBlockFaces(BlockId id) => Definitions[id].CullsSameBlockFaces;
    }
}
