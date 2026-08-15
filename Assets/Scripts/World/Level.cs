using System;
using System.Collections.Generic;
using MineCraftUnity.Blocks;
using MineCraftUnity.Core;

namespace MineCraftUnity.World
{
    public readonly struct PendingBlock
    {
        public readonly BlockPos Pos;
        public readonly BlockId Id;
        public readonly byte Metadata;

        public PendingBlock(BlockPos pos, BlockId id, byte metadata = 0)
        {
            Pos = pos;
            Id = id;
            Metadata = metadata;
        }

        public bool Equals(PendingBlock other) => Pos.Equals(other.Pos) && Id == other.Id && Metadata == other.Metadata;
        public override bool Equals(object obj) => obj is PendingBlock other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Pos, Id, Metadata);
    }

    /// <summary>
    /// MC ref: net.minecraft.world.level.Level
    /// </summary>
    public sealed class Level
    {
        private readonly Dictionary<ChunkPos, Chunk> _chunks = new();
        private readonly Dictionary<ChunkPos, HashSet<PendingBlock>> _pendingDecorations = new();
        public int Seed { get; }

        public event Action<ChunkPos> BlockChanged;

        public Level(int seed)
        {
            Seed = seed;
        }

        public Chunk GetOrCreateChunk(ChunkPos pos)
        {
            if (_chunks.TryGetValue(pos, out var chunk))
            {
                return chunk;
            }

            chunk = new Chunk(pos);
            _chunks[pos] = chunk;
            return chunk;
        }

        public bool TryGetChunk(ChunkPos pos, out Chunk chunk) => _chunks.TryGetValue(pos, out chunk);

        public BiomeId GetBiome(int worldX, int worldZ) => GetBiome(worldX, WorldConstants.SeaLevel, worldZ);

        public BiomeId GetBiome(int worldX, int worldY, int worldZ)
        {
            var chunkX = FloorDiv(worldX, WorldConstants.ChunkSize);
            var chunkZ = FloorDiv(worldZ, WorldConstants.ChunkSize);
            if (!_chunks.TryGetValue(new ChunkPos(chunkX, chunkZ), out var chunk) || !chunk.IsGenerated)
            {
                return BiomeId.Unknown;
            }

            var localX = ModWorldCoord(worldX, WorldConstants.ChunkSize);
            var localZ = ModWorldCoord(worldZ, WorldConstants.ChunkSize);
            return chunk.GetBiome(localX, worldY, localZ);
        }

        public IEnumerable<Chunk> GetAllChunks() => _chunks.Values;

        public BlockId GetBlock(int worldX, int worldY, int worldZ)
        {
            if (worldY < WorldConstants.MinY || worldY > WorldConstants.MaxY)
            {
                return BlockId.Air;
            }

            var chunkX = FloorDiv(worldX, WorldConstants.ChunkSize);
            var chunkZ = FloorDiv(worldZ, WorldConstants.ChunkSize);
            if (!_chunks.TryGetValue(new ChunkPos(chunkX, chunkZ), out var chunk) || !chunk.IsGenerated)
            {
                return BlockId.Air;
            }

            var localX = ModWorldCoord(worldX, WorldConstants.ChunkSize);
            var localZ = ModWorldCoord(worldZ, WorldConstants.ChunkSize);
            return chunk.GetBlock(localX, worldY, localZ);
        }

        public BlockId GetBlock(BlockPos pos) => GetBlock(pos.X, pos.Y, pos.Z);

        public byte GetFluidLevel(int worldX, int worldY, int worldZ)
        {
            if (GetBlock(worldX, worldY, worldZ) != BlockId.Water)
            {
                return FluidLevel.Source;
            }

            if (worldY < WorldConstants.MinY || worldY > WorldConstants.MaxY)
            {
                return FluidLevel.Source;
            }

            var chunkX = FloorDiv(worldX, WorldConstants.ChunkSize);
            var chunkZ = FloorDiv(worldZ, WorldConstants.ChunkSize);
            if (!_chunks.TryGetValue(new ChunkPos(chunkX, chunkZ), out var chunk) || !chunk.IsGenerated)
            {
                return FluidLevel.Source;
            }

            var localX = ModWorldCoord(worldX, WorldConstants.ChunkSize);
            var localZ = ModWorldCoord(worldZ, WorldConstants.ChunkSize);
            return chunk.GetFluidLevel(localX, worldY, localZ);
        }

        public byte GetFluidLevel(BlockPos pos) => GetFluidLevel(pos.X, pos.Y, pos.Z);

        public bool SetBlock(int worldX, int worldY, int worldZ, BlockId id)
        {
            if (worldY < WorldConstants.MinY || worldY > WorldConstants.MaxY)
            {
                return false;
            }

            var chunkPos = new ChunkPos(
                FloorDiv(worldX, WorldConstants.ChunkSize),
                FloorDiv(worldZ, WorldConstants.ChunkSize));

            if (!_chunks.TryGetValue(chunkPos, out var chunk) || !chunk.IsGenerated)
            {
                return false;
            }

            var localX = ModWorldCoord(worldX, WorldConstants.ChunkSize);
            var localZ = ModWorldCoord(worldZ, WorldConstants.ChunkSize);
            chunk.SetBlock(localX, worldY, localZ, id);
            NotifyBlockChanged(chunkPos, worldX, worldY, worldZ);
            return true;
        }

        public void SetBlockDuringGeneration(int worldX, int worldY, int worldZ, BlockId id, byte metadata, HashSet<ChunkPos> changedChunks)
        {
            if (worldY < WorldConstants.MinY || worldY > WorldConstants.MaxY)
            {
                return;
            }

            var chunkPos = new ChunkPos(
                FloorDiv(worldX, WorldConstants.ChunkSize),
                FloorDiv(worldZ, WorldConstants.ChunkSize));

            if (!_chunks.TryGetValue(chunkPos, out var chunk) || !chunk.IsGenerated)
            {
                if (!_pendingDecorations.TryGetValue(chunkPos, out var pending))
                {
                    pending = new HashSet<PendingBlock>();
                    _pendingDecorations[chunkPos] = pending;
                }
                pending.Add(new PendingBlock(new BlockPos(worldX, worldY, worldZ), id, metadata));
                return;
            }

            var localX = ModWorldCoord(worldX, WorldConstants.ChunkSize);
            var localZ = ModWorldCoord(worldZ, WorldConstants.ChunkSize);
            
            var currentBlock = chunk.GetBlock(localX, worldY, localZ);
            if (currentBlock != BlockId.Air && !BlockRegistry.IsLeaves(currentBlock))
            {
                return;
            }
            
            chunk.SetBlock(localX, worldY, localZ, id, metadata);
            chunk.IsMeshDirty = true;
            changedChunks.Add(chunkPos);

            if (localX == 0) changedChunks.Add(new ChunkPos(chunkPos.X - 1, chunkPos.Z));
            else if (localX == WorldConstants.ChunkSize - 1) changedChunks.Add(new ChunkPos(chunkPos.X + 1, chunkPos.Z));

            if (localZ == 0) changedChunks.Add(new ChunkPos(chunkPos.X, chunkPos.Z - 1));
            else if (localZ == WorldConstants.ChunkSize - 1) changedChunks.Add(new ChunkPos(chunkPos.X, chunkPos.Z + 1));
        }

        public void QueueDecorationsAndApply(Chunk chunk, System.Collections.Generic.List<PendingBlock> decorations, HashSet<ChunkPos> changedChunks)
        {
            foreach (var block in decorations)
            {
                var targetChunkPos = new ChunkPos(FloorDiv(block.Pos.X, WorldConstants.ChunkSize), FloorDiv(block.Pos.Z, WorldConstants.ChunkSize));
                if (!_pendingDecorations.TryGetValue(targetChunkPos, out var pendingSet))
                {
                    pendingSet = new HashSet<PendingBlock>();
                    _pendingDecorations[targetChunkPos] = pendingSet;
                }
                pendingSet.Add(block);
            }

            if (!_pendingDecorations.TryGetValue(chunk.Position, out var pending))
            {
                return;
            }

            foreach (var pendingBlock in pending)
            {
                var localX = ModWorldCoord(pendingBlock.Pos.X, WorldConstants.ChunkSize);
                var localZ = ModWorldCoord(pendingBlock.Pos.Z, WorldConstants.ChunkSize);
                var worldY = pendingBlock.Pos.Y;
                
                var currentBlock = chunk.GetBlock(localX, worldY, localZ);
                if (currentBlock != BlockId.Air && !BlockRegistry.IsLeaves(currentBlock))
                {
                    continue;
                }
                
                chunk.SetBlock(localX, worldY, localZ, pendingBlock.Id, pendingBlock.Metadata);
                chunk.IsMeshDirty = true;
                changedChunks.Add(chunk.Position);
                
                if (localX == 0) changedChunks.Add(new ChunkPos(chunk.Position.X - 1, chunk.Position.Z));
                else if (localX == WorldConstants.ChunkSize - 1) changedChunks.Add(new ChunkPos(chunk.Position.X + 1, chunk.Position.Z));

                if (localZ == 0) changedChunks.Add(new ChunkPos(chunk.Position.X, chunk.Position.Z - 1));
                else if (localZ == WorldConstants.ChunkSize - 1) changedChunks.Add(new ChunkPos(chunk.Position.X, chunk.Position.Z + 1));
            }

            _pendingDecorations.Remove(chunk.Position);
        }

        public bool SetWater(BlockPos pos, byte level, bool scheduleTick = false)
        {
            if (pos.Y < WorldConstants.MinY || pos.Y > WorldConstants.MaxY)
            {
                return false;
            }

            var chunkPos = new ChunkPos(
                FloorDiv(pos.X, WorldConstants.ChunkSize),
                FloorDiv(pos.Z, WorldConstants.ChunkSize));

            if (!_chunks.TryGetValue(chunkPos, out var chunk) || !chunk.IsGenerated)
            {
                return false;
            }

            var localX = ModWorldCoord(pos.X, WorldConstants.ChunkSize);
            var localZ = ModWorldCoord(pos.Z, WorldConstants.ChunkSize);
            chunk.SetWater(localX, pos.Y, localZ, level);
            NotifyBlockChanged(chunkPos, pos.X, pos.Y, pos.Z);
            if (scheduleTick)
            {
                FluidTickRequested?.Invoke(pos);
            }

            return true;
        }

        public event Action<BlockPos> FluidTickRequested;

        public bool IsWaterAt(int worldX, int worldY, int worldZ) =>
            BlockRegistry.IsFluid(GetBlock(worldX, worldY, worldZ));

        public bool TrySampleTopSolidY(int worldX, int worldZ, out int topY)
        {
            topY = WorldConstants.MinY - 1;
            var chunkX = FloorDiv(worldX, WorldConstants.ChunkSize);
            var chunkZ = FloorDiv(worldZ, WorldConstants.ChunkSize);
            if (!_chunks.TryGetValue(new ChunkPos(chunkX, chunkZ), out var chunk) || !chunk.IsGenerated)
            {
                return false;
            }

            var localX = ModWorldCoord(worldX, WorldConstants.ChunkSize);
            var localZ = ModWorldCoord(worldZ, WorldConstants.ChunkSize);
            for (var y = WorldConstants.MaxY; y >= WorldConstants.MinY; y--)
            {
                var block = chunk.GetBlock(localX, y, localZ);
                if (block != BlockId.Air && !BlockRegistry.IsFluid(block))
                {
                    topY = y;
                    return true;
                }
            }

            return false;
        }

        public bool ShouldRenderFace(BlockPos pos, BlockFace face, BlockId currentId)
        {
            if (currentId == BlockId.Air)
            {
                return false;
            }

            var neighborId = GetBlock(pos.Offset(face));
            return ShouldRenderFaceAgainst(currentId, GetFluidLevel(pos), neighborId, GetFluidLevel(pos.Offset(face)), face);
        }

        public bool ShouldRenderFaceInChunk(
            Chunk chunk,
            int localX,
            int y,
            int localZ,
            BlockFace face,
            BlockId currentId)
        {
            if (currentId == BlockId.Air)
            {
                return false;
            }

            var currentLevel = chunk.GetFluidLevel(localX, y, localZ);
            var neighborLocalX = localX;
            var neighborY = y;
            var neighborLocalZ = localZ;

            switch (face)
            {
                case BlockFace.Up: neighborY++; break;
                case BlockFace.Down: neighborY--; break;
                case BlockFace.North: neighborLocalZ--; break;
                case BlockFace.South: neighborLocalZ++; break;
                case BlockFace.West: neighborLocalX--; break;
                case BlockFace.East: neighborLocalX++; break;
            }

            BlockId neighborId;
            byte neighborLevel;
            if (Chunk.IsInside(neighborLocalX, neighborY, neighborLocalZ))
            {
                neighborId = chunk.GetBlock(neighborLocalX, neighborY, neighborLocalZ);
                neighborLevel = chunk.GetFluidLevel(neighborLocalX, neighborY, neighborLocalZ);
            }
            else
            {
                var baseX = chunk.Position.GetMinBlockX();
                var baseZ = chunk.Position.GetMinBlockZ();
                var worldX = baseX + neighborLocalX;
                var worldZ = baseZ + neighborLocalZ;
                neighborId = GetBlock(worldX, neighborY, worldZ);
                neighborLevel = GetFluidLevel(worldX, neighborY, worldZ);
            }

            return ShouldRenderFaceAgainst(currentId, currentLevel, neighborId, neighborLevel, face);
        }

        public static bool ShouldRenderFaceAgainst(
            BlockId currentId,
            byte currentLevel,
            BlockId neighborId,
            byte neighborLevel,
            BlockFace face)
        {
            if (neighborId == BlockId.Air)
            {
                return true;
            }

            if (BlockRegistry.IsFluid(currentId) && BlockRegistry.IsFluid(neighborId))
            {
                if (face == BlockFace.Up)
                {
                    return FluidLevel.GetHeight01(currentLevel) > FluidLevel.GetHeight01(neighborLevel) + 0.001f;
                }

                if (face == BlockFace.Down)
                {
                    return FluidLevel.GetHeight01(neighborLevel) > FluidLevel.GetHeight01(currentLevel) + 0.001f;
                }

                return currentLevel != neighborLevel;
            }

            if (!BlockRegistry.IsOpaque(neighborId))
            {
                return currentId != neighborId;
            }

            if (neighborId == currentId)
            {
                return !BlockRegistry.CullsSameBlockFaces(currentId);
            }

            return true;
        }

        private void NotifyBlockChanged(ChunkPos chunkPos, int worldX, int worldY, int worldZ)
        {
            BlockChanged?.Invoke(chunkPos);
            var localX = ModWorldCoord(worldX, WorldConstants.ChunkSize);
            var localZ = ModWorldCoord(worldZ, WorldConstants.ChunkSize);
            if (localX == 0)
            {
                BlockChanged?.Invoke(new ChunkPos(chunkPos.X - 1, chunkPos.Z));
            }
            else if (localX == WorldConstants.ChunkSize - 1)
            {
                BlockChanged?.Invoke(new ChunkPos(chunkPos.X + 1, chunkPos.Z));
            }

            if (localZ == 0)
            {
                BlockChanged?.Invoke(new ChunkPos(chunkPos.X, chunkPos.Z - 1));
            }
            else if (localZ == WorldConstants.ChunkSize - 1)
            {
                BlockChanged?.Invoke(new ChunkPos(chunkPos.X, chunkPos.Z + 1));
            }
        }

        public static int ModWorldCoord(int coord, int size)
        {
            var mod = coord % size;
            return mod < 0 ? mod + size : mod;
        }

        public static int FloorDiv(int value, int divisor)
        {
            var div = value / divisor;
            var rem = value % divisor;
            if (rem != 0 && ((rem < 0) ^ (divisor < 0)))
            {
                div--;
            }

            return div;
        }
    }
}
