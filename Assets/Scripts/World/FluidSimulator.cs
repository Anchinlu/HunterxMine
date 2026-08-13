using System;
using System.Collections.Generic;
using MineCraftUnity.Blocks;
using MineCraftUnity.Core;

namespace MineCraftUnity.World
{
    /// <summary>
    /// MC ref: FlowingFluid.spread — budgeted water flow ticks on worker-owned level data.
    /// </summary>
    public sealed class FluidSimulator
    {
        private readonly Level _level;
        private readonly Queue<BlockPos> _tickQueue = new();
        private readonly HashSet<BlockPos> _scheduled = new();

        public FluidSimulator(Level level)
        {
            _level = level;
        }

        public int PendingTickCount => _tickQueue.Count;

        public void ScheduleTick(BlockPos pos)
        {
            if (_scheduled.Add(pos))
            {
                _tickQueue.Enqueue(pos);
            }
        }

        public void ScheduleSpreadCandidatesForChunk(Chunk chunk)
        {
            var baseX = chunk.Position.GetMinBlockX();
            var baseZ = chunk.Position.GetMinBlockZ();

            for (var localX = 0; localX < WorldConstants.ChunkSize; localX++)
            {
                for (var localZ = 0; localZ < WorldConstants.ChunkSize; localZ++)
                {
                    for (var y = chunk.MinFilledY; y <= chunk.MaxFilledY; y++)
                    {
                        if (chunk.GetBlock(localX, y, localZ) != BlockId.Water)
                        {
                            continue;
                        }

                        var worldX = baseX + localX;
                        var worldZ = baseZ + localZ;
                        if (ShouldScheduleInitialTick(worldX, y, worldZ))
                        {
                            ScheduleTick(new BlockPos(worldX, y, worldZ));
                        }
                    }
                }
            }
        }

        public int ProcessTicks(int maxTicks)
        {
            var processed = 0;
            while (processed < maxTicks && _tickQueue.Count > 0)
            {
                var pos = _tickQueue.Dequeue();
                _scheduled.Remove(pos);
                if (_level.GetBlock(pos) != BlockId.Water)
                {
                    continue;
                }

                SpreadAt(pos);
                processed++;
            }

            return processed;
        }

        private bool ShouldScheduleInitialTick(int worldX, int y, int worldZ)
        {
            if (IsAirOrFluidPassable(_level.GetBlock(worldX, y - 1, worldZ)))
            {
                return true;
            }

            if (IsAirOrFluidPassable(_level.GetBlock(worldX + 1, y, worldZ)))
            {
                return true;
            }

            if (IsAirOrFluidPassable(_level.GetBlock(worldX - 1, y, worldZ)))
            {
                return true;
            }

            if (IsAirOrFluidPassable(_level.GetBlock(worldX, y, worldZ + 1)))
            {
                return true;
            }

            if (IsAirOrFluidPassable(_level.GetBlock(worldX, y, worldZ - 1)))
            {
                return true;
            }

            var fluidLevel = _level.GetFluidLevel(worldX, y, worldZ);
            if (fluidLevel != FluidLevel.Source)
            {
                return true;
            }

            return false;
        }

        private static bool IsAirOrFluidPassable(BlockId id) =>
            id == BlockId.Air || BlockRegistry.IsFluid(id);

        private void SpreadAt(BlockPos pos)
        {
            var level = _level.GetFluidLevel(pos);
            var below = pos.Offset(BlockFace.Down);
            var belowBlock = _level.GetBlock(below);

            if (belowBlock == BlockId.Air)
            {
                _level.SetWater(below, FluidLevel.Source, scheduleTick: true);
                ScheduleTick(pos);
                return;
            }

            if (BlockRegistry.IsFluid(belowBlock))
            {
                ScheduleTick(pos);
                return;
            }

            if (FluidLevel.IsSource(level))
            {
                TrySpreadHorizontal(pos, 1);
                return;
            }

            if (level >= FluidLevel.MaxFlow)
            {
                return;
            }

            TrySpreadHorizontal(pos, (byte)(level + 1));
        }

        private void TrySpreadHorizontal(BlockPos pos, byte newLevel)
        {
            TrySpreadTo(pos, BlockFace.North, newLevel);
            TrySpreadTo(pos, BlockFace.South, newLevel);
            TrySpreadTo(pos, BlockFace.West, newLevel);
            TrySpreadTo(pos, BlockFace.East, newLevel);
        }

        private void TrySpreadTo(BlockPos from, BlockFace face, byte newLevel)
        {
            var target = from.Offset(face);
            var targetBlock = _level.GetBlock(target);
            if (targetBlock == BlockId.Water)
            {
                if (_level.GetFluidLevel(target) <= newLevel)
                {
                    return;
                }
            }
            else if (targetBlock != BlockId.Air)
            {
                return;
            }

            var below = target.Offset(BlockFace.Down);
            var belowBlock = _level.GetBlock(below);
            if (belowBlock == BlockId.Air)
            {
                return;
            }

            if (BlockRegistry.IsFluid(belowBlock))
            {
                var belowLevel = _level.GetFluidLevel(below);
                if (!FluidLevel.IsSource(belowLevel) && belowLevel > newLevel)
                {
                    return;
                }
            }

            _level.SetWater(target, newLevel, scheduleTick: true);
        }
    }
}
