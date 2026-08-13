using Unity.Profiling;

namespace MineCraftUnity.Rendering
{
    /// <summary>
    /// Unity Profiler markers for chunk pipeline (Phase 0 baseline).
    /// </summary>
    public static class ChunkProfilerMarkers
    {
        public static readonly ProfilerMarker ProcessQueues = new("ChunkManager.ProcessQueues");
        public static readonly ProfilerMarker GenerateChunk = new("ChunkManager.GenerateChunk");
        public static readonly ProfilerMarker EnsureRenderer = new("ChunkManager.EnsureRenderer");
        public static readonly ProfilerMarker MeshBuildInto = new("ChunkMeshBuilder.BuildInto");
        public static readonly ProfilerMarker FillChunk = new("NoiseBasedChunkFiller.FillChunk");
    }
}
