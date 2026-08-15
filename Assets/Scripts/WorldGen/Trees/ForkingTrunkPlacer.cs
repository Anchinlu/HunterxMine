using System.Collections.Generic;
using MineCraftUnity.Core;
using MineCraftUnity.World;

namespace MineCraftUnity.WorldGen.Trees
{
    public class ForkingTrunkPlacer : TrunkPlacer
    {
        public ForkingTrunkPlacer(int baseHeight, int heightRandA, int heightRandB) 
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
            
            int leanDirX = random.Next(3) - 1; 
            int leanDirZ = random.Next(3) - 1;
            if (leanDirX == 0 && leanDirZ == 0) leanDirX = 1;

            int leanHeight = treeHeight - random.Next(4) - 1;
            int leanSteps = 3 - random.Next(3);
            
            int tx = startPos.X;
            int tz = startPos.Z;
            int? endY = null;

            for (int yo = 0; yo < treeHeight; yo++)
            {
                int yy = startPos.Y + yo;
                if (yo >= leanHeight && leanSteps > 0)
                {
                    tx += leanDirX;
                    tz += leanDirZ;
                    leanSteps--;
                }

                byte metadata = 0;
                if (yo >= leanHeight && leanDirX != 0 && leanDirZ == 0) metadata = 1; // X
                else if (yo >= leanHeight && leanDirZ != 0 && leanDirX == 0) metadata = 2; // Z

                buffer.SetBlock(new BlockPos(tx, yy, tz), config.TrunkProvider, metadata);
                endY = yy + 1;
            }

            if (endY.HasValue)
            {
                attachments.Add(new FoliageAttachment(new BlockPos(tx, endY.Value, tz), 1, false));
            }

            tx = startPos.X;
            tz = startPos.Z;
            
            int branchDirX = random.Next(3) - 1;
            int branchDirZ = random.Next(3) - 1;
            if (branchDirX == leanDirX && branchDirZ == leanDirZ) branchDirX = -branchDirX;
            if (branchDirX == 0 && branchDirZ == 0) branchDirZ = 1;

            int branchPos = leanHeight - random.Next(2) - 1;
            if (branchPos < 0) branchPos = 0;
            int branchSteps = 1 + random.Next(3);
            endY = null;

            for (int yo = branchPos; yo < treeHeight && branchSteps > 0; branchSteps--)
            {
                if (yo >= 1)
                {
                    int yy = startPos.Y + yo;
                    tx += branchDirX;
                    tz += branchDirZ;
                    
                    byte metadata = 0;
                    if (branchDirX != 0 && branchDirZ == 0) metadata = 1; // X
                    else if (branchDirZ != 0 && branchDirX == 0) metadata = 2; // Z

                    buffer.SetBlock(new BlockPos(tx, yy, tz), config.TrunkProvider, metadata);
                    endY = yy + 1;
                }
                yo++;
            }

            if (endY.HasValue)
            {
                attachments.Add(new FoliageAttachment(new BlockPos(tx, endY.Value, tz), 0, false));
            }

            return attachments;
        }
    }
}

