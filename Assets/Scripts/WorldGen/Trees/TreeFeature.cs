using System.Collections.Generic;
using MineCraftUnity.Core;
using MineCraftUnity.World;

namespace MineCraftUnity.WorldGen.Trees
{
    public class TreeFeature
    {
        public void Place(
            Level level,
            BlockPos pos,
            System.Random random,
            TreeConfiguration config,
            HashSet<ChunkPos> changedChunks)
        {
            var buffer = new TreePlacementBuffer();

            int treeHeight = config.TrunkPlacer.GetTreeHeight(random);
            
            // Place Trunk
            var attachments = config.TrunkPlacer.PlaceTrunk(level, buffer, random, treeHeight, pos, config);

            // Place Foliage
            int foliageHeight = config.FoliagePlacer.FoliageHeight(random, treeHeight, config);
            foreach (var attachment in attachments)
            {
                config.FoliagePlacer.PlaceFoliage(
                    level,
                    buffer,
                    random,
                    config,
                    treeHeight,
                    attachment,
                    foliageHeight,
                    config.FoliagePlacer.Radius);
            }

            // Two-phase commit
            if (buffer.Validate(level))
            {
                buffer.Commit(level, changedChunks);
            }
        }
    }
}
