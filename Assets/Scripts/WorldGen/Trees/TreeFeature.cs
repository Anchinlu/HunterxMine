using System.Collections.Generic;
using MineCraftUnity.Core;
using MineCraftUnity.World;

namespace MineCraftUnity.WorldGen.Trees
{
    public class TreeFeature
    {
        public void Place(
            ChunkGenerationData data,
            BlockPos pos,
            System.Random random,
            TreeConfiguration config)
        {
            var buffer = new TreePlacementBuffer();

            int treeHeight = config.TrunkPlacer.GetTreeHeight(random);
            
            // Place Trunk
            var attachments = config.TrunkPlacer.PlaceTrunk(data, buffer, random, treeHeight, pos, config);

            // Place Foliage
            int foliageHeight = config.FoliagePlacer.FoliageHeight(random, treeHeight, config);
            foreach (var attachment in attachments)
            {
                config.FoliagePlacer.PlaceFoliage(
                    data,
                    buffer,
                    random,
                    config,
                    treeHeight,
                    attachment,
                    foliageHeight,
                    config.FoliagePlacer.Radius);
            }

            // Two-phase commit
            if (buffer.Validate(data))
            {
                buffer.Commit(data);
            }
        }
    }
}
