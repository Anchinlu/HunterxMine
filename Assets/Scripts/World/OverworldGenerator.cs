using MineCraftUnity.Core;
using MineCraftUnity.WorldGen;

namespace MineCraftUnity.World
{
    /// <summary>
    /// MC ref: net.minecraft.world.level.levelgen.NoiseBasedChunkGenerator
    /// fillFromNoise + buildSurface using MC 26.2 density router.
    /// </summary>
    public sealed class OverworldGenerator : IChunkGenerator
    {
        private readonly RandomState _randomState;

        public OverworldGenerator(int seed)
        {
            _randomState = OverworldNoiseBootstrap.CreateRandomState(seed);
        }

        public RandomState RandomState => _randomState;

        public int SampleSurfaceHeight(int worldX, int worldZ) =>
            NoiseBasedChunkFiller.SampleSurfaceY(_randomState, worldX, worldZ);

        public void GenerateChunk(Level level, Chunk chunk, System.Collections.Generic.HashSet<ChunkPos> changedChunks) =>
            NoiseBasedChunkFiller.FillChunk(level, chunk, _randomState, changedChunks);
    }
}
