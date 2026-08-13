using MineCraftUnity.Core;
using UnityEngine;

namespace MineCraftUnity.Rendering
{
    /// <summary>
    /// CPU-side mesh payload produced off the main thread; applied via <see cref="ChunkMeshBuilder.ApplyMeshData"/>.
    /// </summary>
    public sealed class ChunkMeshData
    {
        public ChunkPos Position;
        public bool IsEmpty;
        public Vector3[] Vertices = System.Array.Empty<Vector3>();
        public Vector2[] Uvs = System.Array.Empty<Vector2>();
        public Vector3[] Normals = System.Array.Empty<Vector3>();
        public Color32[] Colors = System.Array.Empty<Color32>();
        public int[][] SubmeshTriangles;
        public Vector3[] CollisionVertices = System.Array.Empty<Vector3>();
        public int[] CollisionTriangles = System.Array.Empty<int>();
    }
}
