using System.Collections.Generic;
using MineCraftUnity.Core;
using MineCraftUnity.World;

namespace MineCraftUnity.WorldGen.Trees
{
    public class StraightTrunkPlacer : TrunkPlacer
    {
        public StraightTrunkPlacer(int baseHeight, int heightRandA, int heightRandB)
            : base(baseHeight, heightRandA, heightRandB)
        {
        }

        public override List<FoliageAttachment> PlaceTrunk(
            Level level,
            TreePlacementBuffer buffer,
            System.Random random,
            int treeHeight,
            BlockPos startPos,
            TreeConfiguration config)
        {
            for (int i = 0; i < treeHeight; i++)
            {
                buffer.SetBlock(new BlockPos(startPos.X, startPos.Y + i, startPos.Z), config.TrunkProvider);
            }

            return new List<FoliageAttachment>
            {
                new FoliageAttachment(new BlockPos(startPos.X, startPos.Y + treeHeight, startPos.Z), 0, false)
            };
        }
    }
}
