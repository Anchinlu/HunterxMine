using System.Collections.Generic;
using MineCraftUnity.Blocks;
using MineCraftUnity.Core;
using MineCraftUnity.World;

namespace MineCraftUnity.WorldGen.Trees
{
    public class TreePlacementBuffer
    {
        private readonly Dictionary<BlockPos, PendingBlock> _blocks = new();

        public void SetBlock(BlockPos pos, BlockId id, byte metadata = 0)
        {
            _blocks[pos] = new PendingBlock(pos, id, metadata);
        }

        public bool TryPlaceLeaf(Level level, BlockPos pos, BlockId id, byte metadata = 0)
        {
            if (_blocks.TryGetValue(pos, out var existing) && BlockRegistry.IsLog(existing.Id))
            {
                return false;
            }

            var currentBlock = level.GetBlock(pos.X, pos.Y, pos.Z);
            if (currentBlock != BlockId.Air && !BlockRegistry.IsLeaves(currentBlock))
            {
                return false;
            }

            _blocks[pos] = new PendingBlock(pos, id, metadata);
            return true;
        }

        public bool Validate(Level level)
        {
            foreach (var kvp in _blocks)
            {
                var pos = kvp.Key;
                var currentBlock = level.GetBlock(pos.X, pos.Y, pos.Z);
                
                // Blocks that trees can replace
                if (currentBlock != BlockId.Air && 
                    !BlockRegistry.IsLeaves(currentBlock))
                {
                    // In MC, if it hits a solid block, it can't grow
                    return false;
                }
            }
            return true;
        }

        public void Commit(Level level, HashSet<ChunkPos> changedChunks)
        {
            foreach (var kvp in _blocks)
            {
                var pos = kvp.Key;
                level.SetBlockDuringGeneration(pos.X, pos.Y, pos.Z, kvp.Value.Id, kvp.Value.Metadata, changedChunks);
            }
        }
    }
}
