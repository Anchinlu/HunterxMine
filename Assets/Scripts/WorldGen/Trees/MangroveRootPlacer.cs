using System.Collections.Generic;
using MineCraftUnity.Core;
using MineCraftUnity.World;

namespace MineCraftUnity.WorldGen.Trees
{
    public class MangroveRootPlacer : TrunkPlacer
    {
        public MangroveRootPlacer(int baseHeight, int heightRandA, int heightRandB) 
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

            int rootHeight = 2 + random.Next(3);
            
            int trunkStart = startPos.Y + rootHeight;

            for (int r = 0; r < 4; r++)
            {
                int ox = r == 0 ? 1 : r == 1 ? -1 : 0;
                int oz = r == 2 ? 1 : r == 3 ? -1 : 0;
                
                int currX = startPos.X + ox;
                int currZ = startPos.Z + oz;
                
                for (int y = trunkStart - 1; y >= startPos.Y - 2; y--)
                {
                    buffer.SetBlock(new BlockPos(currX, y, currZ), config.TrunkProvider);
                    if (y <= startPos.Y && random.Next(2) == 0)
                    {
                        break;
                    }
                    if (random.Next(3) == 0)
                    {
                        currX += ox;
                        currZ += oz;
                    }
                }
            }

            for (int yo = 0; yo < treeHeight; yo++)
            {
                int yy = trunkStart + yo;
                buffer.SetBlock(new BlockPos(startPos.X, yy, startPos.Z), config.TrunkProvider);
            }

            attachments.Add(new FoliageAttachment(new BlockPos(startPos.X, trunkStart + treeHeight, startPos.Z), 0, false));

            return attachments;
        }
    }
}
