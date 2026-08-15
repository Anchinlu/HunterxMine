using System.Collections.Generic;
using MineCraftUnity.Core;
using MineCraftUnity.World;

namespace MineCraftUnity.WorldGen.Trees
{
    public class GiantTrunkPlacer : TrunkPlacer
    {
        public GiantTrunkPlacer(int baseHeight, int heightRandA, int heightRandB) 
            : base(baseHeight, heightRandA, heightRandB)
        {
        }

        public override List<FoliageAttachment> PlaceTrunk(
            ChunkGenerationData data,
            TreePlacementBuffer buffer,
            System.Random random,
            int treeHeight,
            BlockPos startPos,
            TreeConfiguration config)
        {
            var attachments = new List<FoliageAttachment>();

            for (int yo = 0; yo < treeHeight; yo++)
            {
                int yy = startPos.Y + yo;
                
                buffer.SetBlock(new BlockPos(startPos.X, yy, startPos.Z), config.TrunkProvider);
                buffer.SetBlock(new BlockPos(startPos.X + 1, yy, startPos.Z), config.TrunkProvider);
                buffer.SetBlock(new BlockPos(startPos.X, yy, startPos.Z + 1), config.TrunkProvider);
                buffer.SetBlock(new BlockPos(startPos.X + 1, yy, startPos.Z + 1), config.TrunkProvider);
            }

            attachments.Add(new FoliageAttachment(new BlockPos(startPos.X, startPos.Y + treeHeight, startPos.Z), 0, true));

            return attachments;
        }
    }
}

