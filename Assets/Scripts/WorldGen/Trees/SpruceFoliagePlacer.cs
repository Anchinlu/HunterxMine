using System;
using MineCraftUnity.Core;
using MineCraftUnity.World;

namespace MineCraftUnity.WorldGen.Trees
{
    public class SpruceFoliagePlacer : FoliagePlacer
    {
        private readonly int _trunkHeightMin;
        private readonly int _trunkHeightMax;

        public SpruceFoliagePlacer(int radius, int offset, int trunkHeightMin, int trunkHeightMax) 
            : base(radius, offset)
        {
            _trunkHeightMin = trunkHeightMin;
            _trunkHeightMax = trunkHeightMax;
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
            int currentRadius = random.Next(2);
            int maxRadius = 1;
            int minRadius = 0;

            for (int yo = Offset; yo >= -foliageHeight; yo--)
            {
                int bound = attachment.DoubleTrunk ? currentRadius + 1 : currentRadius;
                for (int ax = -currentRadius; ax <= bound; ax++)
                {
                    for (int az = -currentRadius; az <= bound; az++)
                    {
                        if (!CheckSkip(random, ax, yo, az, currentRadius, attachment.DoubleTrunk))
                        {
                            PlaceFoliageBlock(level, buffer, random, pos.Offset(ax, yo, az), config);
                        }
                    }
                }

                if (currentRadius >= maxRadius)
                {
                    currentRadius = minRadius;
                    minRadius = 1;
                    maxRadius = Math.Min(maxRadius + 1, radius + attachment.RadiusOffset);
                }
                else
                {
                    currentRadius++;
                }
            }
        }

        public override int FoliageHeight(Random random, int treeHeight, TreeConfiguration config)
        {
            return Math.Max(4, treeHeight - random.Next(_trunkHeightMin, _trunkHeightMax + 1));
        }

        protected override bool ShouldSkipLocation(Random random, int localX, int localY, int localZ, int radius, bool giantTrunk)
        {
            return localX == radius && localZ == radius && radius > 0;
        }
    }
}
