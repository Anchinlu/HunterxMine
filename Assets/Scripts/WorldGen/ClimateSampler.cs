using MineCraftUnity.WorldGen.Density;

namespace MineCraftUnity.WorldGen
{
    /// <summary>
    /// MC ref: MultiNoiseBiomeSource + Climate.Sampler — sample climate dimensions from the noise router.
    /// </summary>
    public static class ClimateSampler
    {
        public static ClimateSample Sample(RandomState randomState, int blockX, int blockY, int blockZ)
        {
            var ctx = new DensityContext { BlockX = blockX, BlockY = blockY, BlockZ = blockZ };
            return Sample(randomState, in ctx);
        }

        public static ClimateSample Sample(
            RandomState randomState,
            int blockX,
            int blockY,
            int blockZ,
            DensityEvaluationCache columnCache)
        {
            columnCache.BeginSample();
            var ctx = new DensityContext
            {
                BlockX = blockX,
                BlockY = blockY,
                BlockZ = blockZ,
                Cache = columnCache
            };
            return Sample(randomState, in ctx);
        }

        public static ClimateSample Sample(RandomState randomState, in DensityContext context)
        {
            var router = randomState.Router;
            return new ClimateSample(
                (float)router.Temperature.Compute(in context),
                (float)router.Vegetation.Compute(in context),
                (float)router.Continents.Compute(in context),
                (float)router.Erosion.Compute(in context),
                (float)router.Depth.Compute(in context),
                (float)router.Ridges.Compute(in context));
        }
    }
}
