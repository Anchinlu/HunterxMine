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
            [BlockId.Air] = new BlockDefinition(BlockId.Air, "air", BlockRenderKind.None, false, false, false),
            [BlockId.GrassBlock] = new BlockDefinition(BlockId.GrassBlock, "grass_block", BlockRenderKind.GrassBlock, true, false, true),
            [BlockId.Dirt] = new BlockDefinition(BlockId.Dirt, "dirt", BlockRenderKind.Cube, true, false, true),
            [BlockId.Stone] = new BlockDefinition(BlockId.Stone, "stone", BlockRenderKind.Cube, true, false, true),
            [BlockId.Sand] = new BlockDefinition(BlockId.Sand, "sand", BlockRenderKind.Cube, true, false, true),
            [BlockId.Water] = new BlockDefinition(BlockId.Water, "water", BlockRenderKind.Cube, false, true, true),
            [BlockId.Bedrock] = new BlockDefinition(BlockId.Bedrock, "bedrock", BlockRenderKind.Cube, true, false, true),
            [BlockId.Gravel] = new BlockDefinition(BlockId.Gravel, "gravel", BlockRenderKind.Cube, true, false, true),
            [BlockId.ShortGrass] = new BlockDefinition(BlockId.ShortGrass, "short_grass", BlockRenderKind.Cross, false, false, false),
            [BlockId.Fern] = new BlockDefinition(BlockId.Fern, "fern", BlockRenderKind.Cross, false, false, false),
            [BlockId.Dandelion] = new BlockDefinition(BlockId.Dandelion, "dandelion", BlockRenderKind.Cross, false, false, false),
            [BlockId.Poppy] = new BlockDefinition(BlockId.Poppy, "poppy", BlockRenderKind.Cross, false, false, false),
            [BlockId.OakLeaves] = new BlockDefinition(BlockId.OakLeaves, "oak_leaves", BlockRenderKind.CutoutCube, true, false, true),
            [BlockId.BirchLeaves] = new BlockDefinition(BlockId.BirchLeaves, "birch_leaves", BlockRenderKind.CutoutCube, true, false, true),
            [BlockId.SpruceLeaves] = new BlockDefinition(BlockId.SpruceLeaves, "spruce_leaves", BlockRenderKind.CutoutCube, true, false, true),
            [BlockId.JungleLeaves] = new BlockDefinition(BlockId.JungleLeaves, "jungle_leaves", BlockRenderKind.CutoutCube, true, false, true),
            [BlockId.AcaciaLeaves] = new BlockDefinition(BlockId.AcaciaLeaves, "acacia_leaves", BlockRenderKind.CutoutCube, true, false, true),
            [BlockId.DarkOakLeaves] = new BlockDefinition(BlockId.DarkOakLeaves, "dark_oak_leaves", BlockRenderKind.CutoutCube, true, false, true),
            [BlockId.CherryLeaves] = new BlockDefinition(BlockId.CherryLeaves, "cherry_leaves", BlockRenderKind.CutoutCube, true, false, true),
            [BlockId.MangroveLeaves] = new BlockDefinition(BlockId.MangroveLeaves, "mangrove_leaves", BlockRenderKind.CutoutCube, true, false, true),
            [BlockId.PaleOakLeaves] = new BlockDefinition(BlockId.PaleOakLeaves, "pale_oak_leaves", BlockRenderKind.CutoutCube, true, false, true),
            [BlockId.OakLog] = new BlockDefinition(BlockId.OakLog, "oak_log", BlockRenderKind.Cube, true, false, true),
            [BlockId.BirchLog] = new BlockDefinition(BlockId.BirchLog, "birch_log", BlockRenderKind.Cube, true, false, true),
            [BlockId.SpruceLog] = new BlockDefinition(BlockId.SpruceLog, "spruce_log", BlockRenderKind.Cube, true, false, true),
            [BlockId.JungleLog] = new BlockDefinition(BlockId.JungleLog, "jungle_log", BlockRenderKind.Cube, true, false, true),
            [BlockId.AcaciaLog] = new BlockDefinition(BlockId.AcaciaLog, "acacia_log", BlockRenderKind.Cube, true, false, true),
            [BlockId.DarkOakLog] = new BlockDefinition(BlockId.DarkOakLog, "dark_oak_log", BlockRenderKind.Cube, true, false, true),
            [BlockId.CherryLog] = new BlockDefinition(BlockId.CherryLog, "cherry_log", BlockRenderKind.Cube, true, false, true),
            [BlockId.MangroveLog] = new BlockDefinition(BlockId.MangroveLog, "mangrove_log", BlockRenderKind.Cube, true, false, true),
            [BlockId.PaleOakLog] = new BlockDefinition(BlockId.PaleOakLog, "pale_oak_log", BlockRenderKind.Cube, true, false, true)
        };

        public static BlockDefinition Get(BlockId id) => Definitions[id];

        public static bool IsSolid(BlockId id) => Definitions[id].IsSolid;

        public static bool IsFluid(BlockId id) => Definitions[id].IsFluid;

        public static bool CullsSameBlockFaces(BlockId id) => Definitions[id].CullsSameBlockFaces;

        public static bool IsPlant(BlockId id) =>
            id is BlockId.ShortGrass or BlockId.Fern or BlockId.Dandelion or BlockId.Poppy;

        public static bool IsLeaves(BlockId id) =>
            id is BlockId.OakLeaves or BlockId.BirchLeaves or BlockId.SpruceLeaves
            or BlockId.JungleLeaves or BlockId.AcaciaLeaves or BlockId.DarkOakLeaves
            or BlockId.CherryLeaves or BlockId.MangroveLeaves or BlockId.PaleOakLeaves;

        public static bool UsesGrassTint(BlockId id) => id is BlockId.ShortGrass or BlockId.Fern;
    }
}
