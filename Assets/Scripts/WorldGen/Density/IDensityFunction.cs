namespace MineCraftUnity.WorldGen.Density
{
    /// <summary>
    /// MC ref: net.minecraft.world.level.levelgen.DensityFunction
    /// </summary>
    public interface IDensityFunction
    {
        double Compute(in DensityContext context);
        double MinValue { get; }
        double MaxValue { get; }
    }

    /// <summary>
    /// MC ref: DensityFunction.FunctionContext / SinglePointContext
    /// </summary>
    public struct DensityContext
    {
        public int BlockX;
        public int BlockY;
        public int BlockZ;
    }
}
