using System;

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

        /// <summary>Max ms spent generating chunks per frame (time-budget queue).</summary>
        public const float GenerationBudgetMs = 4f;

        /// <summary>Max ms spent meshing chunks per frame (time-budget queue).</summary>
        public const float MeshBudgetMs = 4f;

        /// <summary>Safety cap on chunk gen count per frame (in addition to time budget).</summary>
        public const int MaxChunkGenerationsPerFrame = 1;

        /// <summary>Safety cap on chunk mesh count per frame (in addition to time budget).</summary>
        public const int MaxChunkMeshesPerFrame = 2;

        /// <summary>Higher mesh-apply cap while spawn overlay is active (GPU upload only).</summary>
        public const int MaxSpawnChunkMeshesPerFrame = 3;

        /// <summary>Extra blocks scanned above surface hint for overhangs (Phase 2 tuning).</summary>
        public const int SurfaceOverhangMargin = 20;

        /// <summary>Parallel chunk workers for generation.</summary>
        public static int MaxGenerationWorkers => Math.Clamp(Environment.ProcessorCount / 4, 3, 4);

        /// <summary>Parallel chunk workers for mesh compute.</summary>
        public static int MaxMeshWorkers => Math.Clamp(Environment.ProcessorCount / 4, 3, 4);

        /// <summary>Max physics mesh cooks per frame (MeshCollider.sharedMesh is very expensive).</summary>
        public const int MaxCollisionUpdatesPerFrame = 1;

        /// <summary>Max ms spent on deferred collision updates per frame.</summary>
        public const float CollisionBudgetMs = 4f;

        /// <summary>Debug tag for F3 overlay — bump when pipeline behavior changes.</summary>
        public const string PipelineVersion = "P12";

        /// <summary>MC biome resolution — one biome cell per 4×4×4 blocks.</summary>
        public const int BiomeQuartSize = 4;
        public const int BiomeQuartCountXZ = ChunkSize / BiomeQuartSize;
        public const int BiomeQuartCountY = Height / BiomeQuartSize;
        public const int BiomeQuartVolume = BiomeQuartCountXZ * BiomeQuartCountXZ * BiomeQuartCountY;

        /// <summary>How often to check if the player moved to a new chunk.</summary>
        public const float ChunkUpdateInterval = 0.35f;

        /// <summary>Max water flow ticks processed per frame.</summary>
        public const int MaxFluidTicksPerFrame = 8;
    }
}
