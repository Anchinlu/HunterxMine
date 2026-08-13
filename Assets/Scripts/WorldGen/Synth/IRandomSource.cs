namespace MineCraftUnity.WorldGen.Synth
{
    /// <summary>
    /// MC ref: net.minecraft.util.RandomSource — subset used by worldgen noise.
    /// </summary>
    public interface IRandomSource
    {
        IRandomSource Fork();

        IPositionalRandomFactory ForkPositional();

        int NextInt();

        int NextInt(int bound);

        long NextLong();

        bool NextBoolean();

        float NextFloat();

        double NextDouble();

        void ConsumeCount(int rounds);
    }
}
