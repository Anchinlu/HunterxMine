using System;
using System.Collections.Generic;
using MineCraftUnity.Core;
using MineCraftUnity.World;
using UnityEngine;

namespace MineCraftUnity.WorldGen.Trees
{
    public class MegaJungleTrunkPlacer : GiantTrunkPlacer
    {
        public MegaJungleTrunkPlacer(int baseHeight, int heightRandA, int heightRandB) 
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
            var attachments = base.PlaceTrunk(data, buffer, random, treeHeight, startPos, config);

            for (int branchHeight = treeHeight - 2 - random.Next(4); branchHeight > treeHeight / 2; branchHeight -= 2 + random.Next(4))
            {
                float angle = (float)(random.NextDouble() * Math.PI * 2.0);
                int bx = 0;
                int bz = 0;

                for (int b = 0; b < 5; b++)
                {
                    bx = (int)(1.5f + Mathf.Cos(angle) * b);
                    bz = (int)(1.5f + Mathf.Sin(angle) * b);
                    var pos = new BlockPos(startPos.X + bx, startPos.Y + branchHeight - 3 + b / 2, startPos.Z + bz);
                    buffer.SetBlock(pos, config.TrunkProvider);
                }

                attachments.Add(new FoliageAttachment(new BlockPos(startPos.X + bx, startPos.Y + branchHeight, startPos.Z + bz), -2, false));
            }

            return attachments;
        }
    }
}

