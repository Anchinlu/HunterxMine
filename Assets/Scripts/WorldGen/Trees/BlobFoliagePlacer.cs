using System;
using MineCraftUnity.Core;
using MineCraftUnity.World;

namespace MineCraftUnity.WorldGen.Trees
{
    public class BlobFoliagePlacer : FoliagePlacer
    {
        private readonly int _height;

        public BlobFoliagePlacer(int radius, int offset, int height)
            : base(radius, offset)
        {
            _height = height;
        }

        public override int FoliageHeight(System.Random random, int treeHeight, TreeConfiguration config)
        {
            return _height;
        }

        public override void PlaceFoliage(
            ChunkGenerationData data,
            TreePlacementBuffer buffer,
            System.Random random,
            TreeConfiguration config,
            int treeHeight,
            FoliageAttachment attachment,
            int foliageHeight,
            int radius)
        {
            var center = attachment.Pos;
            for (var y = Offset; y >= Offset - foliageHeight; y--)
            {
                var layerRadius = radius + attachment.RadiusOffset;
                if (y >= Offset - 1)
                {
                    layerRadius -= 1; // Top 2 layers are smaller
                }

                if (layerRadius < 0) layerRadius = 0;

                var layerY = center.Y + y;

                int bound = attachment.DoubleTrunk ? layerRadius + 1 : layerRadius;
                for (int x = -layerRadius; x <= bound; x++)
                {
                    for (int z = -layerRadius; z <= bound; z++)
                    {
                        if (!CheckSkip(random, x, layerY - center.Y, z, layerRadius, attachment.DoubleTrunk))
                        {
                            var pos = new BlockPos(center.X + x, layerY, center.Z + z);
                            PlaceFoliageBlock(data, buffer, random, pos, config);
                        }
                    }
                }
            }
        }

        protected override bool ShouldSkipLocation(System.Random random, int localX, int localY, int localZ, int radius, bool giantTrunk)
        {
            // Skip corners to make a rounded blob.
            if (Math.Abs(localX) == radius && Math.Abs(localZ) == radius && radius > 0)
            {
                // Minecraft BlobFoliagePlacer skips corners on the top layer, 
                // and has a random chance to skip on other layers if radius is large.
                // For simplicity and matching MC look, we'll skip corners if radius > 0.
                if (localY == Offset || random.Next(2) == 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}


