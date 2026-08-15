using System.Collections.Generic;
using System;
using MineCraftUnity.Core;
using MineCraftUnity.World;

namespace MineCraftUnity.WorldGen.Trees
{
    public abstract class FoliagePlacer
    {
        public int Radius { get; }
        public int Offset { get; }

        protected FoliagePlacer(int radius, int offset)
        {
            Radius = radius;
            Offset = offset;
        }

        public abstract void PlaceFoliage(
            Level level,
            TreePlacementBuffer buffer,
            System.Random random,
            TreeConfiguration config,
            int treeHeight,
            FoliageAttachment attachment,
            int foliageHeight,
            int radius);

        public abstract int FoliageHeight(System.Random random, int treeHeight, TreeConfiguration config);

        protected abstract bool ShouldSkipLocation(System.Random random, int localX, int localY, int localZ, int radius, bool giantTrunk);

        protected void PlaceFoliageBlock(
            Level level,
            TreePlacementBuffer buffer,
            System.Random random,
            BlockPos pos,
            TreeConfiguration config)
        {
            buffer.TryPlaceLeaf(level, pos, config.FoliageProvider);
        }

        protected bool CheckSkip(System.Random random, int ax, int yo, int az, int layerRadius, bool doubleTrunk)
        {
            int localX, localZ;
            if (doubleTrunk)
            {
                localX = Math.Min(Math.Abs(ax), Math.Abs(ax - 1));
                localZ = Math.Min(Math.Abs(az), Math.Abs(az - 1));
            }
            else
            {
                localX = Math.Abs(ax);
                localZ = Math.Abs(az);
            }
            return ShouldSkipLocation(random, localX, yo, localZ, layerRadius, doubleTrunk);
        }
    }
}
