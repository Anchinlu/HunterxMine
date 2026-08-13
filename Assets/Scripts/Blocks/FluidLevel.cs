namespace MineCraftUnity.Blocks
{
    /// <summary>
    /// MC ref: LiquidBlock.LEVEL — 0 = source (8/9 block), 1–7 = flowing depth.
    /// </summary>
    public static class FluidLevel
    {
        public const byte Source = 0;
        public const byte MaxFlow = 7;

        public static bool IsSource(byte level) => level == Source;

        /// <summary>Rendered water surface height inside the block (0–1).</summary>
        public static float GetHeight01(byte level)
        {
            if (level == Source)
            {
                return 8f / 9f;
            }

            return (8f - level) / 9f;
        }

        public static byte ClampFlow(byte level)
        {
            if (level <= MaxFlow)
            {
                return level;
            }

            return MaxFlow;
        }
    }
}
