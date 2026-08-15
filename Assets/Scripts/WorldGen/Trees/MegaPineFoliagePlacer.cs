using System;
using MineCraftUnity.Core;
using MineCraftUnity.World;
using UnityEngine;

namespace MineCraftUnity.WorldGen.Trees
{
    public class MegaPineFoliagePlacer : FoliagePlacer
    {
        private readonly int _crownHeightMin;
        private readonly int _crownHeightMax;

        public MegaPineFoliagePlacer(int radius, int offset, int crownHeightMin, int crownHeightMax) 
            : base(radius, offset)
        {
            _crownHeightMin = crownHeightMin;
            _crownHeightMax = crownHeightMax;
        }

        public override void PlaceFoliage(
            Level level,
            TreePlacementBuffer buffer,
            System.Random random,
            TreeConfiguration config,
            int treeHeight,
            FoliageAttachment attachment,
            int foliageHeight,
            int radius)
        {
            BlockPos pos = attachment.Pos;
            int prevRadius = 0;

            for (int yy = pos.Y - foliageHeight + Offset; yy <= pos.Y + Offset; yy++)
            {
                int yo = pos.Y - yy;
                int smoothRadius = radius + attachment.RadiusOffset + Mathf.FloorToInt((float)yo / foliageHeight * 3.5f);
                int jaggedRadius;

                if (yo > 0 && smoothRadius == prevRadius && (yy & 1) == 0)
                {
                    jaggedRadius = smoothRadius + 1;
                }
                else
                {
                    jaggedRadius = smoothRadius;
                }

                int bound = attachment.DoubleTrunk ? jaggedRadius + 1 : jaggedRadius;
                for (int ax = -jaggedRadius; ax <= bound; ax++)
                {
                    for (int az = -jaggedRadius; az <= bound; az++)
                    {
                        if (!CheckSkip(random, ax, yo, az, jaggedRadius, attachment.DoubleTrunk))
                        {
                            PlaceFoliageBlock(level, buffer, random, new BlockPos(pos.X + ax, yy, pos.Z + az), config);
                        }
                    }
                }

                prevRadius = smoothRadius;
            }
        }

        public override int FoliageHeight(System.Random random, int treeHeight, TreeConfiguration config)
        {
            return random.Next(_crownHeightMin, _crownHeightMax + 1);
        }

        protected override bool ShouldSkipLocation(System.Random random, int localX, int localY, int localZ, int radius, bool giantTrunk)
        {
            if (giantTrunk)
            {
                return localX + localZ >= 7 ? true : localX * localX + localZ * localZ > radius * radius;
            }
            return localX * localX + localZ * localZ > radius * radius;
        }
    }
}
