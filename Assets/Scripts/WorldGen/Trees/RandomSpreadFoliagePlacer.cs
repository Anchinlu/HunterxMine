using System;
using MineCraftUnity.Core;
using MineCraftUnity.World;

namespace MineCraftUnity.WorldGen.Trees
{
    public class RandomSpreadFoliagePlacer : FoliagePlacer
    {
        private readonly int _foliageHeightMin;
        private readonly int _foliageHeightMax;

        public RandomSpreadFoliagePlacer(int radius, int offset, int foliageHeightMin, int foliageHeightMax) 
            : base(radius, offset)
        {
            _foliageHeightMin = foliageHeightMin;
            _foliageHeightMax = foliageHeightMax;
        }

        public override void PlaceFoliage(
            Level level,
            TreePlacementBuffer buffer,
            Random random,
            TreeConfiguration config,
            int treeHeight,
            FoliageAttachment attachment,
            int foliageHeight,
            int radius)
        {
            BlockPos pos = attachment.Pos;

            for (int i = 0; i < 40; i++)
            {
                int rx = random.Next(-radius, radius + (attachment.DoubleTrunk ? 2 : 1));
                int ry = random.Next(-foliageHeight, foliageHeight + 1);
                int rz = random.Next(-radius, radius + (attachment.DoubleTrunk ? 2 : 1));

                if (!CheckSkip(random, rx, ry, rz, radius, attachment.DoubleTrunk))
                {
                    PlaceFoliageBlock(level, buffer, random, pos.Offset(rx, ry, rz), config);
                }
            }
        }

        public override int FoliageHeight(Random random, int treeHeight, TreeConfiguration config)
        {
            return random.Next(_foliageHeightMin, _foliageHeightMax + 1);
        }

        protected override bool ShouldSkipLocation(Random random, int localX, int localY, int localZ, int radius, bool giantTrunk)
        {
            return localX * localX + localY * localY + localZ * localZ > radius * radius;
        }
    }
}
