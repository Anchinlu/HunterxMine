using System.Collections.Generic;
using MineCraftUnity.Core;

namespace MineCraftUnity.World
{
    /// <summary>
    /// MC ref: net.minecraft.world.level.chunk.ChunkGenerator
    /// </summary>
    public interface IChunkGenerator
    {
        void GenerateChunk(Level level, Chunk chunk, HashSet<ChunkPos> changedChunks);
        int SampleSurfaceHeight(int worldX, int worldZ);
    }
}
