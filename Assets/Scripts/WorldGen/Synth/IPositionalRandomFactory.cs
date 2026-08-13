namespace MineCraftUnity.WorldGen.Synth
{
    /// <summary>
    /// MC ref: net.minecraft.world.level.levelgen.PositionalRandomFactory
    /// </summary>
    public interface IPositionalRandomFactory
    {
        IRandomSource At(int x, int y, int z);

        IRandomSource FromHashOf(string name);

        IRandomSource FromSeed(long seed);
    }
}
