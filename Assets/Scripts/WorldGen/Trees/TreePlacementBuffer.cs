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

        public bool TryPlaceLeaf(ChunkGenerationData data, BlockPos pos, BlockId id, byte metadata = 0)
        {
            if (_blocks.TryGetValue(pos, out var existing) && BlockRegistry.IsLog(existing.Id))
            {
                return false;
            }

            var currentBlock = data.GetBlock(pos.X - data.Position.GetMinBlockX(), pos.Y, pos.Z - data.Position.GetMinBlockZ());
            if (currentBlock != BlockId.Air && !BlockRegistry.IsLeaves(currentBlock))
            {
                return false;
            }

            _blocks[pos] = new PendingBlock(pos, id, metadata);
            return true;
        }

        public bool Validate(ChunkGenerationData data)
        {
            foreach (var kvp in _blocks)
            {
                var pos = kvp.Key;
                var currentBlock = data.GetBlock(pos.X - data.Position.GetMinBlockX(), pos.Y, pos.Z - data.Position.GetMinBlockZ());
                
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

        public void Commit(ChunkGenerationData data)
        {
            foreach (var kvp in _blocks)
            {
                data.AddDecoration(kvp.Value);
            }
        }
    }
}
