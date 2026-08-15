using MineCraftUnity.World;
using System;
using MineCraftUnity.Blocks;
using MineCraftUnity.Core;

namespace MineCraftUnity.World
{
    public sealed class ChunkMeshSnapshot
    {
        public ChunkPos Position { get; }
        public int Revision { get; }
        
        private readonly BlockId[] _centerBlocks;
        private readonly byte[] _centerMetadata;
        private readonly byte[] _centerFluid;
        private readonly BiomeId[] _centerBiomes;

        private readonly BlockId[] _zPlusBlocks;
        private readonly byte[] _zPlusMetadata;
        private readonly BlockId[] _zMinusBlocks;
        private readonly byte[] _zMinusMetadata;
        private readonly BlockId[] _xPlusBlocks;
        private readonly byte[] _xPlusMetadata;
        private readonly BlockId[] _xMinusBlocks;
        private readonly byte[] _xMinusMetadata;
        
        private readonly byte[] _zPlusFluid;
        private readonly byte[] _zMinusFluid;
        private readonly byte[] _xPlusFluid;
        private readonly byte[] _xMinusFluid;

        public bool IsEmpty { get; }

        public ChunkMeshSnapshot(Level level, Chunk chunk)
        {
            Position = chunk.Position;
            Revision = chunk.Revision;
            if (!chunk.HasBlocks)
            {
                IsEmpty = true;
                return;
            }

            IsEmpty = false;
            var volume = WorldConstants.ChunkSize * WorldConstants.Height * WorldConstants.ChunkSize;
            
            _centerBlocks = new BlockId[volume];
            _centerMetadata = new byte[volume];
            _centerFluid = new byte[volume];
            _centerBiomes = new BiomeId[WorldConstants.BiomeQuartVolume];
            
            CopyChunkData(chunk, _centerBlocks, _centerMetadata, _centerFluid, _centerBiomes);

            int sliceVolume = WorldConstants.ChunkSize * WorldConstants.Height;

            if (level.TryGetChunk(new ChunkPos(Position.X, Position.Z + 1), out var zPlus) && zPlus.IsGenerated)
            {
                _zPlusBlocks = new BlockId[sliceVolume];
                _zPlusMetadata = new byte[sliceVolume];
                _zPlusFluid = new byte[sliceVolume];
                zPlus.CopyZSliceToArrays(0, _zPlusBlocks, _zPlusMetadata, _zPlusFluid);
            }

            if (level.TryGetChunk(new ChunkPos(Position.X, Position.Z - 1), out var zMinus) && zMinus.IsGenerated)
            {
                _zMinusBlocks = new BlockId[sliceVolume];
                _zMinusMetadata = new byte[sliceVolume];
                _zMinusFluid = new byte[sliceVolume];
                zMinus.CopyZSliceToArrays(WorldConstants.ChunkSize - 1, _zMinusBlocks, _zMinusMetadata, _zMinusFluid);
            }

            if (level.TryGetChunk(new ChunkPos(Position.X + 1, Position.Z), out var xPlus) && xPlus.IsGenerated)
            {
                _xPlusBlocks = new BlockId[sliceVolume];
                _xPlusMetadata = new byte[sliceVolume];
                _xPlusFluid = new byte[sliceVolume];
                xPlus.CopyXSliceToArrays(0, _xPlusBlocks, _xPlusMetadata, _xPlusFluid);
            }

            if (level.TryGetChunk(new ChunkPos(Position.X - 1, Position.Z), out var xMinus) && xMinus.IsGenerated)
            {
                _xMinusBlocks = new BlockId[sliceVolume];
                _xMinusMetadata = new byte[sliceVolume];
                _xMinusFluid = new byte[sliceVolume];
                xMinus.CopyXSliceToArrays(WorldConstants.ChunkSize - 1, _xMinusBlocks, _xMinusMetadata, _xMinusFluid);
            }
        }

        private void CopyChunkData(Chunk chunk, BlockId[] blocks, byte[] metadata, byte[] fluid, BiomeId[] biomes)
        {
            chunk.CopyToArrays(blocks, metadata, fluid, biomes);
        }

        public BlockId GetBlock(int worldX, int worldY, int worldZ)
        {
            if (worldY < WorldConstants.MinY || worldY > WorldConstants.MaxY) return BlockId.Air;

            var chunkX = Level.FloorDiv(worldX, WorldConstants.ChunkSize);
            var chunkZ = Level.FloorDiv(worldZ, WorldConstants.ChunkSize);

            var localX = Level.ModWorldCoord(worldX, WorldConstants.ChunkSize);
            var localZ = Level.ModWorldCoord(worldZ, WorldConstants.ChunkSize);

            if (chunkX == Position.X && chunkZ == Position.Z)
                return _centerBlocks[Chunk.ToIndex(localX, worldY, localZ)];

            int yIdx = worldY - WorldConstants.MinY;

            if (chunkX == Position.X && chunkZ == Position.Z + 1 && _zPlusBlocks != null)
                return _zPlusBlocks[yIdx * WorldConstants.ChunkSize + localX];

            if (chunkX == Position.X && chunkZ == Position.Z - 1 && _zMinusBlocks != null)
                return _zMinusBlocks[yIdx * WorldConstants.ChunkSize + localX];

            if (chunkX == Position.X + 1 && chunkZ == Position.Z && _xPlusBlocks != null)
                return _xPlusBlocks[yIdx * WorldConstants.ChunkSize + localZ];

            if (chunkX == Position.X - 1 && chunkZ == Position.Z && _xMinusBlocks != null)
                return _xMinusBlocks[yIdx * WorldConstants.ChunkSize + localZ];

            return BlockId.Air;
        }

        public byte GetMetadata(int worldX, int worldY, int worldZ)
        {
            if (worldY < WorldConstants.MinY || worldY > WorldConstants.MaxY) return 0;

            var chunkX = Level.FloorDiv(worldX, WorldConstants.ChunkSize);
            var chunkZ = Level.FloorDiv(worldZ, WorldConstants.ChunkSize);

            var localX = Level.ModWorldCoord(worldX, WorldConstants.ChunkSize);
            var localZ = Level.ModWorldCoord(worldZ, WorldConstants.ChunkSize);

            if (chunkX == Position.X && chunkZ == Position.Z)
                return _centerMetadata[Chunk.ToIndex(localX, worldY, localZ)];

            int yIdx = worldY - WorldConstants.MinY;

            if (chunkX == Position.X && chunkZ == Position.Z + 1 && _zPlusMetadata != null)
                return _zPlusMetadata[yIdx * WorldConstants.ChunkSize + localX];

            if (chunkX == Position.X && chunkZ == Position.Z - 1 && _zMinusMetadata != null)
                return _zMinusMetadata[yIdx * WorldConstants.ChunkSize + localX];

            if (chunkX == Position.X + 1 && chunkZ == Position.Z && _xPlusMetadata != null)
                return _xPlusMetadata[yIdx * WorldConstants.ChunkSize + localZ];

            if (chunkX == Position.X - 1 && chunkZ == Position.Z && _xMinusMetadata != null)
                return _xMinusMetadata[yIdx * WorldConstants.ChunkSize + localZ];

            return 0;
        }

        public byte GetFluidLevel(int worldX, int worldY, int worldZ)
        {
            if (worldY < WorldConstants.MinY || worldY > WorldConstants.MaxY) return FluidLevel.Source;

            var chunkX = Level.FloorDiv(worldX, WorldConstants.ChunkSize);
            var chunkZ = Level.FloorDiv(worldZ, WorldConstants.ChunkSize);

            var localX = Level.ModWorldCoord(worldX, WorldConstants.ChunkSize);
            var localZ = Level.ModWorldCoord(worldZ, WorldConstants.ChunkSize);

            if (chunkX == Position.X && chunkZ == Position.Z)
                return _centerFluid[Chunk.ToIndex(localX, worldY, localZ)];

            int yIdx = worldY - WorldConstants.MinY;

            if (chunkX == Position.X && chunkZ == Position.Z + 1 && _zPlusFluid != null)
                return _zPlusFluid[yIdx * WorldConstants.ChunkSize + localX];

            if (chunkX == Position.X && chunkZ == Position.Z - 1 && _zMinusFluid != null)
                return _zMinusFluid[yIdx * WorldConstants.ChunkSize + localX];

            if (chunkX == Position.X + 1 && chunkZ == Position.Z && _xPlusFluid != null)
                return _xPlusFluid[yIdx * WorldConstants.ChunkSize + localZ];

            if (chunkX == Position.X - 1 && chunkZ == Position.Z && _xMinusFluid != null)
                return _xMinusFluid[yIdx * WorldConstants.ChunkSize + localZ];

            return FluidLevel.Source;
        }

        public BiomeId GetBiome(int localX, int y, int localZ)
        {
            if (localX is < 0 or >= WorldConstants.ChunkSize ||
                localZ is < 0 or >= WorldConstants.ChunkSize ||
                y is < WorldConstants.MinY or > WorldConstants.MaxY)
            {
                return BiomeId.Unknown;
            }

            var quartX = localX >> 2;
            var quartZ = localZ >> 2;
            var quartY = (y - WorldConstants.MinY) >> 2;
            return _centerBiomes[Chunk.ToQuartIndex(quartX, quartY, quartZ)];
        }
        
        public bool ShouldRenderFaceInChunk(int localX, int y, int localZ, BlockFace face, BlockId currentId)
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
            byte neighborLevel;
            if (Chunk.IsInside(neighborLocalX, neighborY, neighborLocalZ))
            {
                var index = Chunk.ToIndex(neighborLocalX, neighborY, neighborLocalZ);
                neighborId = _centerBlocks[index];
                neighborLevel = _centerFluid[index];
            }
            else
            {
                var baseX = Position.GetMinBlockX();
                var baseZ = Position.GetMinBlockZ();
                var worldX = baseX + neighborLocalX;
                var worldZ = baseZ + neighborLocalZ;
                neighborId = GetBlock(worldX, neighborY, worldZ);
                neighborLevel = GetFluidLevel(worldX, neighborY, worldZ);
            }

            var currentLevel = _centerFluid[Chunk.ToIndex(localX, y, localZ)];
            return Level.ShouldRenderFaceAgainst(currentId, currentLevel, neighborId, neighborLevel, face);
        }
    }
}

