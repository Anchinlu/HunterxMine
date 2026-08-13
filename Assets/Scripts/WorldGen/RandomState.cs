using MineCraftUnity.WorldGen.Density;

namespace MineCraftUnity.WorldGen
{
    /// <summary>
    /// MC ref: net.minecraft.world.level.levelgen.RandomState — seeded noise + density router for one world.
    /// </summary>
    public sealed class RandomState
    {
        public int Seed { get; }
        public NoiseRouter Router { get; }

        public RandomState(int seed, NoiseRouter router)
        {
            Seed = seed;
            Router = router;
        }

        public double SampleTerrainDensity(int blockX, int blockY, int blockZ)
        {
            var ctx = new DensityContext { BlockX = blockX, BlockY = blockY, BlockZ = blockZ };
            return Router.SlopedCheese.Compute(in ctx);
        }

        public double SampleTerrainDensity(int blockX, int blockY, int blockZ, DensityEvaluationCache columnCache)
        {
            columnCache.BeginSample();
            var ctx = new DensityContext
            {
                BlockX = blockX,
                BlockY = blockY,
                BlockZ = blockZ,
                Cache = columnCache
            };
            return Router.SlopedCheese.Compute(in ctx);
        }

        public double SampleFinalDensity(int blockX, int blockY, int blockZ)
        {
            var ctx = new DensityContext { BlockX = blockX, BlockY = blockY, BlockZ = blockZ };
            return Router.FinalDensity.Compute(in ctx);
        }

        public ClimateSample SampleClimate(int blockX, int blockY, int blockZ) =>
            ClimateSampler.Sample(this, blockX, blockY, blockZ);

        public ClimateSample SampleClimate(
            int blockX,
            int blockY,
            int blockZ,
            DensityEvaluationCache columnCache) =>
            ClimateSampler.Sample(this, blockX, blockY, blockZ, columnCache);
    }
}
