using System.Collections.Generic;
using MineCraftUnity.Core;
using MineCraftUnity.World;

namespace MineCraftUnity.WorldGen.Trees
{
    public class DarkOakTrunkPlacer : TrunkPlacer
    {
        public DarkOakTrunkPlacer(int baseHeight, int heightRandA, int heightRandB) 
            : base(baseHeight, heightRandA, heightRandB)
        {
        }

        public override List<FoliageAttachment> PlaceTrunk(
            Level level,
            TreePlacementBuffer buffer,
            System.Random random,
            int treeHeight,
            BlockPos startPos,
            TreeConfiguration config)
        {
            var attachments = new List<FoliageAttachment>();

            int dirX = random.Next(3) - 1;
            int dirZ = random.Next(3) - 1;
            if (dirX == 0 && dirZ == 0) dirX = 1;

            int leanHeight = treeHeight - random.Next(4);
            int leanSteps = 2 - random.Next(3);
            
            int startX = startPos.X;
            int startY = startPos.Y;
            int startZ = startPos.Z;
            
            int tx = startX;
            int tz = startZ;
            int endY = startY + treeHeight - 1;

            for (int yo = 0; yo < treeHeight; yo++)
            {
                if (yo >= leanHeight && leanSteps > 0)
                {
                    tx += dirX;
                    tz += dirZ;
                    leanSteps--;
                }

                int yy = startY + yo;
                
                buffer.SetBlock(new BlockPos(tx, yy, tz), config.TrunkProvider);
                buffer.SetBlock(new BlockPos(tx + 1, yy, tz), config.TrunkProvider);
                buffer.SetBlock(new BlockPos(tx, yy, tz + 1), config.TrunkProvider);
                buffer.SetBlock(new BlockPos(tx + 1, yy, tz + 1), config.TrunkProvider);
            }

            attachments.Add(new FoliageAttachment(new BlockPos(tx, endY, tz), 0, true));

            for (int q = -1; q <= 2; q++)
            {
                for (int r = -1; r <= 2; r++)
                {
                    if ((q < 0 || q > 1 || r < 0 || r > 1) && random.Next(3) == 0)
                    {
                        int branchLen = random.Next(3) + 2;
                        for (int t = 0; t < branchLen; t++)
                        {
                            buffer.SetBlock(new BlockPos(startX + q, endY - t - 1, startZ + r), config.TrunkProvider);
                        }

                        attachments.Add(new FoliageAttachment(new BlockPos(tx + q, endY, tz + r), 0, false));
                    }
                }
            }

            return attachments;
        }
    }
}
