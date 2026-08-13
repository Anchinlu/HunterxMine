namespace MineCraftUnity.Core
{
    /// <summary>
    /// MC ref: overworld.json noise settings + dimension_type/overworld.json.
    /// </summary>
    public static class WorldConstants
    {
        public const int ChunkSize = 16;
        public const int MinY = -64;
        public const int MaxY = 319;
        public const int Height = MaxY - MinY + 1;
        public const int SeaLevel = 63;
        public const int BedrockLayers = 5;
        public const int DirtDepth = 4;

        public const int DefaultViewDistance = 3;
        public const int CollisionDistance = 2;
        public const int SpawnPriorityRadius = 1;

        /// <summary>Spread chunk generation across frames (MC density is CPU-heavy).</summary>
        public const int MaxChunkGenerationsPerFrame = 1;

        /// <summary>Spread mesh building across frames (heaviest step).</summary>
        public const int MaxChunkMeshesPerFrame = 2;

        /// <summary>How often to check if the player moved to a new chunk.</summary>
        public const float ChunkUpdateInterval = 0.35f;
    }
}
