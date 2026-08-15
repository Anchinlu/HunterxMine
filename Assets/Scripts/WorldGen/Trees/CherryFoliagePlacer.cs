using System;
using MineCraftUnity.Core;
using MineCraftUnity.World;

namespace MineCraftUnity.WorldGen.Trees
{
    public class CherryFoliagePlacer : FoliagePlacer
    {
        private readonly float _cornerHoleChance;
        private readonly float _hangingLeavesChance;
        private readonly float _hangingLeavesExtensionChance;

        public CherryFoliagePlacer(int radius, int offset, float cornerHoleChance, float hangingLeavesChance, float hangingLeavesExtensionChance) 
            : base(radius, offset)
        {
            _cornerHoleChance = cornerHoleChance;
            _hangingLeavesChance = hangingLeavesChance;
            _hangingLeavesExtensionChance = hangingLeavesExtensionChance;
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
            int i = radius + attachment.RadiusOffset - 1;
            
            PlaceRow(data, buffer, random, pos, i - 2, foliageHeight - 3, attachment.DoubleTrunk, config);
            PlaceRow(data, buffer, random, pos, i - 1, foliageHeight - 2, attachment.DoubleTrunk, config);
            PlaceRow(data, buffer, random, pos, i, foliageHeight - 1, attachment.DoubleTrunk, config);
            PlaceRow(data, buffer, random, pos, i - 1, foliageHeight, attachment.DoubleTrunk, config);
            PlaceRow(data, buffer, random, pos, i - 2, foliageHeight + 1, attachment.DoubleTrunk, config);
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
                        var leafPos = pos.Offset(ax, yo, az);
                        PlaceFoliageBlock(data, buffer, random, leafPos, config);

                        if (random.NextDouble() < _hangingLeavesChance)
                        {
                            PlaceFoliageBlock(data, buffer, random, leafPos.Offset(0, -1, 0), config);
                            if (random.NextDouble() < _hangingLeavesExtensionChance)
                            {
                                PlaceFoliageBlock(data, buffer, random, leafPos.Offset(0, -2, 0), config);
                            }
                        }
                    }
                }
            }
        }

        public override int FoliageHeight(Random random, int treeHeight, TreeConfiguration config)
        {
            return 5;
        }

        protected override bool ShouldSkipLocation(Random random, int localX, int localY, int localZ, int radius, bool giantTrunk)
        {
            if (localX == radius && localZ == radius && radius > 0)
            {
                return true;
            }
            
            bool bl = localX == radius && localZ == radius - 1;
            bool bl2 = localX == radius - 1 && localZ == radius;
            return (bl || bl2) && radius > 1 && random.NextDouble() < _cornerHoleChance;
        }
    }
}


