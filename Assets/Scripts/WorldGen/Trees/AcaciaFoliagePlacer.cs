using System;
using MineCraftUnity.Core;
using MineCraftUnity.World;

namespace MineCraftUnity.WorldGen.Trees
{
    public class AcaciaFoliagePlacer : FoliagePlacer
    {
        public AcaciaFoliagePlacer(int radius, int offset) 
            : base(radius, offset)
        {
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
            BlockPos pos = attachment.Pos.Offset(0, Offset, 0);
            
            PlaceRow(level, buffer, random, pos, radius + attachment.RadiusOffset, -1 - foliageHeight, attachment.DoubleTrunk, config);
            PlaceRow(level, buffer, random, pos, radius - 1, -foliageHeight, attachment.DoubleTrunk, config);
            PlaceRow(level, buffer, random, pos, radius + attachment.RadiusOffset - 1, 0, attachment.DoubleTrunk, config);
        }

        private void PlaceRow(
            Level level, TreePlacementBuffer buffer, Random random, BlockPos pos, int layerRadius, int yo, bool doubleTrunk, TreeConfiguration config)
        {
            int bound = doubleTrunk ? layerRadius + 1 : layerRadius;
            for (int ax = -layerRadius; ax <= bound; ax++)
            {
                for (int az = -layerRadius; az <= bound; az++)
                {
                    if (!CheckSkip(random, ax, yo, az, layerRadius, doubleTrunk))
                    {
                        PlaceFoliageBlock(level, buffer, random, pos.Offset(ax, yo, az), config);
                    }
                }
            }
        }

        public override int FoliageHeight(Random random, int treeHeight, TreeConfiguration config)
        {
            return 0;
        }

        protected override bool ShouldSkipLocation(Random random, int localX, int localY, int localZ, int radius, bool giantTrunk)
        {
            if (localY == 0)
            {
                return localX > 1 || localZ > 1 || (localX == 0 && localZ == 0);
            }
            return localX == radius && localZ == radius && radius > 0;
        }
    }
}
