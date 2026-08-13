namespace MineCraftUnity.Blocks
{
    /// <summary>
    /// MC ref: net.minecraft.world.level.block.Block
    /// </summary>
    public readonly struct BlockDefinition
    {
        public readonly BlockId Id;
        public readonly string Name;
        public readonly BlockRenderKind RenderKind;
        public readonly bool IsSolid;
        public readonly bool IsFluid;
        public readonly bool CullsSameBlockFaces;

        public BlockDefinition(BlockId id, string name, BlockRenderKind renderKind, bool isSolid, bool isFluid, bool cullsSameBlockFaces)
        {
            Id = id;
            Name = name;
            RenderKind = renderKind;
            IsSolid = isSolid;
            IsFluid = isFluid;
            CullsSameBlockFaces = cullsSameBlockFaces;
        }
    }
}
