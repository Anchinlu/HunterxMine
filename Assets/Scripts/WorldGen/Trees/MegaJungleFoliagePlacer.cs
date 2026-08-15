using System;
using MineCraftUnity.Core;
using MineCraftUnity.World;

namespace MineCraftUnity.WorldGen.Trees
{
    public class MegaJungleFoliagePlacer : FoliagePlacer
    {
        private readonly int _height;

        public MegaJungleFoliagePlacer(int radius, int offset, int height) 
            : base(radius, offset)
        {
            _height = height;
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
            int limit = attachment.DoubleTrunk ? foliageHeight : 1 + random.Next(2);

            for (int yo = Offset; yo >= Offset - limit; yo--)
            {
                int layerRadius = radius + attachment.RadiusOffset + 1 - yo;

                int bound = attachment.DoubleTrunk ? layerRadius + 1 : layerRadius;
                for (int ax = -layerRadius; ax <= bound; ax++)
                {
                    for (int az = -layerRadius; az <= bound; az++)
                    {
                        if (!CheckSkip(random, ax, yo, az, layerRadius, attachment.DoubleTrunk))
                        {
                            PlaceFoliageBlock(level, buffer, random, new BlockPos(pos.X + ax, pos.Y + yo, pos.Z + az), config);
                        }
                    }
                }
            }
        }

        public override int FoliageHeight(Random random, int treeHeight, TreeConfiguration config)
        {
            return _height;
        }

        protected override bool ShouldSkipLocation(Random random, int localX, int localY, int localZ, int radius, bool giantTrunk)
        {
            if (giantTrunk)
            {
                if (localX + localZ >= 7) return true;
            }
            return localX * localX + localZ * localZ > radius * radius;
        }
    }
}
