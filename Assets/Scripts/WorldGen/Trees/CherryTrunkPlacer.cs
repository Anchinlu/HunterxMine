using System;
using System.Collections.Generic;
using MineCraftUnity.Core;
using MineCraftUnity.World;

namespace MineCraftUnity.WorldGen.Trees
{
    public class CherryTrunkPlacer : TrunkPlacer
    {
        private readonly int _branchCountMin;
        private readonly int _branchCountMax;
        private readonly int _branchHorizontalLengthMin;
        private readonly int _branchHorizontalLengthMax;
        private readonly int _branchStartOffsetMin;
        private readonly int _branchStartOffsetMax;
        private readonly int _branchEndOffsetMin;
        private readonly int _branchEndOffsetMax;

        public CherryTrunkPlacer(
            int baseHeight, int heightRandA, int heightRandB,
            int branchCountMin, int branchCountMax,
            int branchHorizontalLengthMin, int branchHorizontalLengthMax,
            int branchStartOffsetMin, int branchStartOffsetMax,
            int branchEndOffsetMin, int branchEndOffsetMax) 
            : base(baseHeight, heightRandA, heightRandB)
        {
            _branchCountMin = branchCountMin;
            _branchCountMax = branchCountMax;
            _branchHorizontalLengthMin = branchHorizontalLengthMin;
            _branchHorizontalLengthMax = branchHorizontalLengthMax;
            _branchStartOffsetMin = branchStartOffsetMin;
            _branchStartOffsetMax = branchStartOffsetMax;
            _branchEndOffsetMin = branchEndOffsetMin;
            _branchEndOffsetMax = branchEndOffsetMax;
        }

        public override List<FoliageAttachment> PlaceTrunk(
            ChunkGenerationData data,
            TreePlacementBuffer buffer,
            Random random,
            int treeHeight,
            BlockPos startPos,
            TreeConfiguration config)
        {
            int firstBranchOffset = Math.Max(0, treeHeight - 1 + random.Next(_branchStartOffsetMin, _branchStartOffsetMax + 1));
            int secondBranchOffset = Math.Max(0, treeHeight - 1 + random.Next(_branchStartOffsetMin, _branchStartOffsetMax));
            if (secondBranchOffset >= firstBranchOffset)
            {
                secondBranchOffset++;
            }

            int branchCount = random.Next(_branchCountMin, _branchCountMax + 1);
            bool hasMiddleBranch = branchCount == 3;
            bool hasBothSideBranches = branchCount >= 2;

            int trunkHeight;
            if (hasMiddleBranch) trunkHeight = treeHeight;
            else if (hasBothSideBranches) trunkHeight = Math.Max(firstBranchOffset, secondBranchOffset) + 1;
            else trunkHeight = firstBranchOffset + 1;

            for (int y = 0; y < trunkHeight; y++)
            {
                buffer.SetBlock(new BlockPos(startPos.X, startPos.Y + y, startPos.Z), config.TrunkProvider);
            }

            var attachments = new List<FoliageAttachment>();
            if (hasMiddleBranch)
            {
                attachments.Add(new FoliageAttachment(new BlockPos(startPos.X, startPos.Y + trunkHeight, startPos.Z), 0, false));
            }

            int dirX = random.Next(3) - 1;
            int dirZ = random.Next(3) - 1;
            if (dirX == 0 && dirZ == 0) dirX = 1;

            byte sidewaysMeta = (dirX != 0 && dirZ == 0) ? (byte)1 : (byte)2;

            attachments.Add(GenerateBranch(
                buffer, random, treeHeight, startPos, config, sidewaysMeta, dirX, dirZ,
                firstBranchOffset, firstBranchOffset < trunkHeight - 1));

            if (hasBothSideBranches)
            {
                attachments.Add(GenerateBranch(
                    buffer, random, treeHeight, startPos, config, sidewaysMeta, -dirX, -dirZ,
                    secondBranchOffset, secondBranchOffset < trunkHeight - 1));
            }

            return attachments;
        }

        private FoliageAttachment GenerateBranch(
            TreePlacementBuffer buffer,
            Random random,
            int treeHeight,
            BlockPos origin,
            TreeConfiguration config,
            byte sidewaysMeta,
            int dirX, int dirZ,
            int offsetFromOrigin,
            bool middleContinuesUpwards)
        {
            int logX = origin.X;
            int logY = origin.Y + offsetFromOrigin;
            int logZ = origin.Z;

            int branchEndPosOffset = treeHeight - 1 + random.Next(_branchEndOffsetMin, _branchEndOffsetMax + 1);
            bool extendBranchAway = middleContinuesUpwards || branchEndPosOffset < offsetFromOrigin;
            int distanceToTrunk = random.Next(_branchHorizontalLengthMin, _branchHorizontalLengthMax + 1) + (extendBranchAway ? 1 : 0);
            
            int endX = origin.X + dirX * distanceToTrunk;
            int endY = origin.Y + branchEndPosOffset;
            int endZ = origin.Z + dirZ * distanceToTrunk;

            int stepsHorizontally = extendBranchAway ? 2 : 1;

            for (int i = 0; i < stepsHorizontally; i++)
            {
                logX += dirX;
                logZ += dirZ;
                buffer.SetBlock(new BlockPos(logX, logY, logZ), config.TrunkProvider, sidewaysMeta);
            }

            int vertDir = endY > logY ? 1 : -1;

            while (true)
            {
                int distance = Math.Abs(logX - endX) + Math.Abs(logY - endY) + Math.Abs(logZ - endZ);
                if (distance == 0)
                {
                    return new FoliageAttachment(new BlockPos(endX, endY + 1, endZ), 0, false);
                }

                float chance = (float)Math.Abs(endY - logY) / distance;
                bool growVertically = random.NextDouble() < chance;

                if (growVertically)
                {
                    logY += vertDir;
                }
                else
                {
                    logX += dirX;
                    logZ += dirZ;
                }

                buffer.SetBlock(new BlockPos(logX, logY, logZ), config.TrunkProvider, growVertically ? (byte)0 : sidewaysMeta);
            }
        }
    }
}

