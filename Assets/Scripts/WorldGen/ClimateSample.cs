namespace MineCraftUnity.WorldGen
{
    /// <summary>
    /// MC ref: Climate.Sampler sample at a block position (temperature, humidity, continentalness, erosion, depth, weirdness).
    /// </summary>
    public readonly struct ClimateSample
    {
        public float Temperature { get; }
        public float Humidity { get; }
        public float Continental { get; }
        public float Erosion { get; }
        public float Depth { get; }
        public float Weirdness { get; }

        public ClimateSample(
            float temperature,
            float humidity,
            float continental,
            float erosion,
            float depth,
            float weirdness)
        {
            Temperature = temperature;
            Humidity = humidity;
            Continental = continental;
            Erosion = erosion;
            Depth = depth;
            Weirdness = weirdness;
        }
    }
}
