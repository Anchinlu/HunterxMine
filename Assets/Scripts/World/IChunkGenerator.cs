using MineCraftUnity.World;
using System.Collections.Generic;
using MineCraftUnity.Core;

namespace MineCraftUnity.World
{
    /// <summary>
    /// MC ref: net.minecraft.world.level.chunk.ChunkGenerator
    /// </summary>
    public interface IChunkGenerator
    {
        ChunkGenerationData ComputeChunkData(ChunkPos pos);
        int SampleSurfaceHeight(int worldX, int worldZ);
    }
}

