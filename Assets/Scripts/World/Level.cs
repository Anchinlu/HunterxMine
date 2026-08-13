using System.Collections.Generic;
using MineCraftUnity.Blocks;
using MineCraftUnity.Core;

namespace MineCraftUnity.World
{
    /// <summary>
    /// MC ref: net.minecraft.world.level.Level
    /// </summary>
    public sealed class Level
    {
        private readonly Dictionary<ChunkPos, Chunk> _chunks = new();
        public int Seed { get; }

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
                if (block != BlockId.Air && block != BlockId.Water)
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
            if (neighborId == BlockId.Air)
            {
                return true;
            }

            if (!BlockRegistry.IsSolid(neighborId))
            {
                return currentId != neighborId;
            }

            if (neighborId == currentId)
            {
                return !BlockRegistry.CullsSameBlockFaces(currentId);
            }

            return true;
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
            if (Chunk.IsInside(neighborLocalX, neighborY, neighborLocalZ))
            {
                neighborId = chunk.GetBlock(neighborLocalX, neighborY, neighborLocalZ);
            }
            else
            {
                var baseX = chunk.Position.GetMinBlockX();
                var baseZ = chunk.Position.GetMinBlockZ();
                neighborId = GetBlock(baseX + neighborLocalX, neighborY, baseZ + neighborLocalZ);
            }

            if (neighborId == BlockId.Air)
            {
                return true;
            }

            if (!BlockRegistry.IsSolid(neighborId))
            {
                return currentId != neighborId;
            }

            if (neighborId == currentId)
            {
                return !BlockRegistry.CullsSameBlockFaces(currentId);
            }

            return true;
        }

        private static int ModWorldCoord(int coord, int size)
        {
            var mod = coord % size;
            return mod < 0 ? mod + size : mod;
        }

        private static int FloorDiv(int value, int divisor)
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
