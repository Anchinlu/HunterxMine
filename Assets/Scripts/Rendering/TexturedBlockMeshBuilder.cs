using System.Collections.Generic;
using MineCraftUnity.Blocks;
using UnityEngine;

namespace MineCraftUnity.Rendering
{
    /// <summary>
    /// Builds a 1m cube mesh with MC-style per-face UVs (0..1 full tile per face).
    /// </summary>
    public static class TexturedBlockMeshBuilder
    {
        /// <summary>Single submesh cube — all faces share one texture (stone, dirt, sand, …).</summary>
        public static Mesh BuildCube()
        {
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var normals = new List<Vector3>();
            var triangles = new List<int>();

            AddFace(BlockFace.Up, vertices, uvs, normals, triangles);
            AddFace(BlockFace.Down, vertices, uvs, normals, triangles);
            AddFace(BlockFace.North, vertices, uvs, normals, triangles);
            AddFace(BlockFace.South, vertices, uvs, normals, triangles);
            AddFace(BlockFace.East, vertices, uvs, normals, triangles);
            AddFace(BlockFace.West, vertices, uvs, normals, triangles);

            var mesh = new Mesh { name = "BlockCube" };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Submesh 0 = top, 1 = bottom, 2 = side base, 3 = side overlay (MC element 2).</summary>
        public static Mesh BuildGrassBlockSubmeshes()
        {
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var normals = new List<Vector3>();
            var topTris = new List<int>();
            var bottomTris = new List<int>();
            var sideTris = new List<int>();
            var sideOverlayTris = new List<int>();

            AddFace(BlockFace.Up, vertices, uvs, normals, topTris);
            AddFace(BlockFace.Down, vertices, uvs, normals, bottomTris);
            AddFace(BlockFace.North, vertices, uvs, normals, sideTris);
            AddFace(BlockFace.South, vertices, uvs, normals, sideTris);
            AddFace(BlockFace.East, vertices, uvs, normals, sideTris);
            AddFace(BlockFace.West, vertices, uvs, normals, sideTris);
            AddFace(BlockFace.North, vertices, uvs, normals, sideOverlayTris);
            AddFace(BlockFace.South, vertices, uvs, normals, sideOverlayTris);
            AddFace(BlockFace.East, vertices, uvs, normals, sideOverlayTris);
            AddFace(BlockFace.West, vertices, uvs, normals, sideOverlayTris);

            var mesh = new Mesh { name = "GrassBlockCube" };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetNormals(normals);
            mesh.subMeshCount = 4;
            mesh.SetTriangles(topTris, 0);
            mesh.SetTriangles(bottomTris, 1);
            mesh.SetTriangles(sideTris, 2);
            mesh.SetTriangles(sideOverlayTris, 3);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddFace(
            BlockFace face,
            List<Vector3> vertices,
            List<Vector2> uvs,
            List<Vector3> normals,
            List<int> triangles)
        {
            var start = vertices.Count;
            GetFaceQuad(face, out var v0, out var v1, out var v2, out var v3);
            var normal = GetOutwardNormal(face);

            vertices.Add(v0);
            vertices.Add(v1);
            vertices.Add(v2);
            vertices.Add(v3);

            uvs.Add(new Vector2(0f, 0f));
            uvs.Add(new Vector2(1f, 0f));
            uvs.Add(new Vector2(1f, 1f));
            uvs.Add(new Vector2(0f, 1f));

            normals.Add(normal);
            normals.Add(normal);
            normals.Add(normal);
            normals.Add(normal);

            if (Vector3.Dot(Vector3.Cross(v1 - v0, v2 - v0), normal) >= 0f)
            {
                triangles.Add(start);
                triangles.Add(start + 1);
                triangles.Add(start + 2);
                triangles.Add(start);
                triangles.Add(start + 2);
                triangles.Add(start + 3);
            }
            else
            {
                triangles.Add(start);
                triangles.Add(start + 2);
                triangles.Add(start + 1);
                triangles.Add(start);
                triangles.Add(start + 3);
                triangles.Add(start + 2);
            }
        }

        private static Vector3 GetOutwardNormal(BlockFace face) => face switch
        {
            BlockFace.North => Vector3.back,
            BlockFace.South => Vector3.forward,
            BlockFace.West => Vector3.left,
            BlockFace.East => Vector3.right,
            BlockFace.Up => Vector3.up,
            _ => Vector3.down
        };

        private static void GetFaceQuad(BlockFace face, out Vector3 v0, out Vector3 v1, out Vector3 v2, out Vector3 v3)
        {
            switch (face)
            {
                case BlockFace.North:
                    v0 = new Vector3(0f, 0f, 0f); v1 = new Vector3(1f, 0f, 0f);
                    v2 = new Vector3(1f, 1f, 0f); v3 = new Vector3(0f, 1f, 0f);
                    break;
                case BlockFace.South:
                    v0 = new Vector3(1f, 0f, 1f); v1 = new Vector3(0f, 0f, 1f);
                    v2 = new Vector3(0f, 1f, 1f); v3 = new Vector3(1f, 1f, 1f);
                    break;
                case BlockFace.West:
                    v0 = new Vector3(0f, 0f, 1f); v1 = new Vector3(0f, 0f, 0f);
                    v2 = new Vector3(0f, 1f, 0f); v3 = new Vector3(0f, 1f, 1f);
                    break;
                case BlockFace.East:
                    v0 = new Vector3(1f, 0f, 0f); v1 = new Vector3(1f, 0f, 1f);
                    v2 = new Vector3(1f, 1f, 1f); v3 = new Vector3(1f, 1f, 0f);
                    break;
                case BlockFace.Up:
                    v0 = new Vector3(0f, 1f, 1f); v1 = new Vector3(1f, 1f, 1f);
                    v2 = new Vector3(1f, 1f, 0f); v3 = new Vector3(0f, 1f, 0f);
                    break;
                default:
                    v0 = new Vector3(0f, 0f, 0f); v1 = new Vector3(1f, 0f, 0f);
                    v2 = new Vector3(1f, 0f, 1f); v3 = new Vector3(0f, 0f, 1f);
                    break;
            }
        }
    }
}
