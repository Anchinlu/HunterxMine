using MineCraftUnity.WorldGen.Density;

namespace MineCraftUnity.WorldGen
{
    /// <summary>
    /// MC ref: net.minecraft.world.level.levelgen.NoiseRouter
    /// </summary>
    public sealed class NoiseRouter
    {
        public IDensityFunction Barrier { get; set; }
        public IDensityFunction FluidLevelFloodedness { get; set; }
        public IDensityFunction FluidLevelSpread { get; set; }
        public IDensityFunction Lava { get; set; }
        public IDensityFunction Temperature { get; set; }
        public IDensityFunction Vegetation { get; set; }
        public IDensityFunction Continents { get; set; }
        public IDensityFunction Erosion { get; set; }
        public IDensityFunction Depth { get; set; }
        public IDensityFunction Ridges { get; set; }
        public IDensityFunction PreliminarySurfaceLevel { get; set; }
        public IDensityFunction SlopedCheese { get; set; }
        public IDensityFunction FinalDensity { get; set; }
    }
}
