using System.Collections.Generic;
using MineCraftUnity.Core;
using MineCraftUnity.World;

namespace MineCraftUnity.WorldGen.Trees
{
    public abstract class TrunkPlacer
    {
        public int BaseHeight { get; }
        public int HeightRandA { get; }
        public int HeightRandB { get; }

        protected TrunkPlacer(int baseHeight, int heightRandA, int heightRandB)
        {
            BaseHeight = baseHeight;
            HeightRandA = heightRandA;
            HeightRandB = heightRandB;
        }

        public int GetTreeHeight(System.Random random)
        {
            return BaseHeight + random.Next(HeightRandA + 1) + random.Next(HeightRandB + 1);
        }

        public abstract List<FoliageAttachment> PlaceTrunk(
            Level level,
            TreePlacementBuffer buffer,
            System.Random random,
            int treeHeight,
            BlockPos startPos,
            TreeConfiguration config);
    }
}
