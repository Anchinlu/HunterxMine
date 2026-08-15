using System;
using MineCraftUnity.Core;
using MineCraftUnity.World;

namespace MineCraftUnity.WorldGen.Trees
{
    public class DarkOakFoliagePlacer : FoliagePlacer
    {
        public DarkOakFoliagePlacer(int radius, int offset) 
            : base(radius, offset)
        {
        }

        public override void PlaceFoliage(
            ChunkGenerationData data,
            TreePlacementBuffer buffer,
            Random random,
            TreeConfiguration config,
            int treeHeight,
            FoliageAttachment attachment,
            int foliageHeight,
            int radius)
        {
            BlockPos pos = attachment.Pos.Offset(0, Offset, 0);
            bool doubleTrunk = attachment.DoubleTrunk;

            if (doubleTrunk)
            {
                PlaceRow(data, buffer, random, pos, radius + 2, -1, doubleTrunk, config);
                PlaceRow(data, buffer, random, pos, radius + 3, 0, doubleTrunk, config);
                PlaceRow(data, buffer, random, pos, radius + 2, 1, doubleTrunk, config);
                if (random.Next(2) == 0)
                {
                    PlaceRow(data, buffer, random, pos, radius, 2, doubleTrunk, config);
                }
            }
            else
            {
                PlaceRow(data, buffer, random, pos, radius + 2, -1, doubleTrunk, config);
                PlaceRow(data, buffer, random, pos, radius + 1, 0, doubleTrunk, config);
            }
        }

        private void PlaceRow(
            ChunkGenerationData data, TreePlacementBuffer buffer, Random random, BlockPos pos, int layerRadius, int yo, bool doubleTrunk, TreeConfiguration config)
        {
            int bound = doubleTrunk ? layerRadius + 1 : layerRadius;
            for (int ax = -layerRadius; ax <= bound; ax++)
            {
                for (int az = -layerRadius; az <= bound; az++)
                {
                    if (!CheckSkip(random, ax, yo, az, layerRadius, doubleTrunk))
                    {
                        PlaceFoliageBlock(data, buffer, random, pos.Offset(ax, yo, az), config);
                    }
                }
            }
        }

        public override int FoliageHeight(Random random, int treeHeight, TreeConfiguration config)
        {
            return 4;
        }

        protected override bool ShouldSkipLocation(Random random, int localX, int localY, int localZ, int radius, bool giantTrunk)
        {
            if (localY == 0 && giantTrunk && 
                (localX == -radius || localX == radius) && 
                (localZ == -radius || localZ == radius))
            {
                return true;
            }
            return localX == radius && localZ == radius && radius > 0;
        }
    }
}


