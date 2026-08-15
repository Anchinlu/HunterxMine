using MineCraftUnity.Core;

namespace MineCraftUnity.WorldGen.Trees
{
    public struct FoliageAttachment
    {
        public BlockPos Pos { get; }
        public int RadiusOffset { get; }
        public bool DoubleTrunk { get; }

        public FoliageAttachment(BlockPos pos, int radiusOffset, bool doubleTrunk)
        {
            Pos = pos;
            RadiusOffset = radiusOffset;
            DoubleTrunk = doubleTrunk;
        }
    }
}
