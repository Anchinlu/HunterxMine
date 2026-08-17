using System.Collections.Generic;
using UnityEngine;
using MineCraftUnity.Blocks;

namespace MineCraftUnity.Player
{
    public static class MinecraftSkinMeshBuilder
    {
        public static Mesh BuildHeadMesh()
        {
            return BuildPartMesh(0, 0, 8, 8, 8);
        }

        public static Mesh BuildBodyMesh()
        {
            return BuildPartMesh(16, 16, 8, 12, 4);
        }

        public static Mesh BuildArmMesh(bool isLeft, PlayerModelType modelType)
        {
            int w = (modelType == PlayerModelType.AlexSlim) ? 3 : 4;
            return isLeft ? BuildPartMesh(32, 48, w, 12, 4) : BuildPartMesh(40, 16, w, 12, 4);
        }

        public static Mesh BuildUpperArmMesh(bool isLeft, PlayerModelType modelType)
        {
            int w = (modelType == PlayerModelType.AlexSlim) ? 3 : 4;
            return isLeft ? BuildPartMesh(32, 48, w, 6, 4) : BuildPartMesh(40, 16, w, 6, 4);
        }

        public static Mesh BuildLowerArmMesh(bool isLeft, PlayerModelType modelType)
        {
            int w = (modelType == PlayerModelType.AlexSlim) ? 3 : 4;
            return isLeft ? BuildPartMesh(32, 54, w, 6, 4) : BuildPartMesh(40, 22, w, 6, 4);
        }

        public static Mesh BuildLegMesh(bool isLeft)
        {
            return isLeft ? BuildPartMesh(16, 48, 4, 12, 4) : BuildPartMesh(0, 16, 4, 12, 4);
        }

        public static Mesh BuildUpperLegMesh(bool isLeft)
        {
            return isLeft ? BuildPartMesh(16, 48, 4, 6, 4) : BuildPartMesh(0, 16, 4, 6, 4);
        }

        public static Mesh BuildLowerLegMesh(bool isLeft)
        {
            return isLeft ? BuildPartMesh(16, 54, 4, 6, 4) : BuildPartMesh(0, 22, 4, 6, 4);
        }

        private static Mesh BuildPartMesh(int texX, int texY, int w, int h, int d)
        {
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var normals = new List<Vector3>();
            var triangles = new List<int>();

            // Minecraft UV unwrapping order per face:
            // Top:    (texX + d,     texY)         to (texX + d + w,     texY + d)
            // Bottom: (texX + d + w, texY)         to (texX + d + w + w, texY + d)
            // Right:  (texX,         texY + d)     to (texX + d,         texY + d + h) [East]
            // Front:  (texX + d,     texY + d)     to (texX + d + w,     texY + d + h) [South]
            // Left:   (texX + d + w, texY + d)     to (texX + d + w + d, texY + d + h) [West]
            // Back:   (texX + d + w + d, texY + d) to (texX + d + w + d + w, texY + d + h) [North]

            // 1. Up (Top)
            AddFace(BlockFace.Up, vertices, uvs, normals, triangles, 
                texX + d, texY, w, d);

            // 2. Down (Bottom)
            AddFace(BlockFace.Down, vertices, uvs, normals, triangles, 
                texX + d + w, texY, w, d);

            // 3. North (Back)
            AddFace(BlockFace.North, vertices, uvs, normals, triangles, 
                texX + d + w + d, texY + d, w, h);

            // 4. South (Front)
            AddFace(BlockFace.South, vertices, uvs, normals, triangles, 
                texX + d, texY + d, w, h);

            // 5. East (Right)
            AddFace(BlockFace.East, vertices, uvs, normals, triangles, 
                texX, texY + d, d, h);

            // 6. West (Left)
            AddFace(BlockFace.West, vertices, uvs, normals, triangles, 
                texX + d + w, texY + d, d, h);

            var mesh = new Mesh { name = $"SkinPart_{texX}_{texY}" };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddFace(
            BlockFace face,
            List<Vector3> vertices,
            List<Vector2> uvs,
            List<Vector3> normals,
            List<int> triangles,
            float x, float y, float w, float h)
        {
            var start = vertices.Count;
            GetFaceQuad(face, out var v0, out var v1, out var v2, out var v3);
            var normal = GetOutwardNormal(face);

            vertices.Add(v0);
            vertices.Add(v1);
            vertices.Add(v2);
            vertices.Add(v3);

            // Calculate UVs based on 64x64 skin size
            // U runs from left to right (0..1)
            // V runs from bottom to top (0..1)
            // Our helper coordinates (x, y) run from top-left (0..64)
            
            if (face == BlockFace.Up)
            {
                uvs.Add(GetUV(x, y + h));
                uvs.Add(GetUV(x + w, y + h));
                uvs.Add(GetUV(x + w, y));
                uvs.Add(GetUV(x, y));
            }
            else if (face == BlockFace.Down)
            {
                uvs.Add(GetUV(x, y));
                uvs.Add(GetUV(x + w, y));
                uvs.Add(GetUV(x + w, y + h));
                uvs.Add(GetUV(x, y + h));
            }
            else if (face == BlockFace.South || face == BlockFace.North)
            {
                uvs.Add(GetUV(x, y + h));
                uvs.Add(GetUV(x + w, y + h));
                uvs.Add(GetUV(x + w, y));
                uvs.Add(GetUV(x, y));
            }
            else // East or West
            {
                uvs.Add(GetUV(x + w, y + h));
                uvs.Add(GetUV(x, y + h));
                uvs.Add(GetUV(x, y));
                uvs.Add(GetUV(x + w, y));
            }

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

        private static Vector2 GetUV(float px, float py)
        {
            return new Vector2(px / 64f, (64f - py) / 64f);
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
                    v0 = new Vector3(-0.5f, -0.5f, -0.5f); v1 = new Vector3(0.5f, -0.5f, -0.5f);
                    v2 = new Vector3(0.5f, 0.5f, -0.5f); v3 = new Vector3(-0.5f, 0.5f, -0.5f);
                    break;
                case BlockFace.South:
                    v0 = new Vector3(0.5f, -0.5f, 0.5f); v1 = new Vector3(-0.5f, -0.5f, 0.5f);
                    v2 = new Vector3(-0.5f, 0.5f, 0.5f); v3 = new Vector3(0.5f, 0.5f, 0.5f);
                    break;
                case BlockFace.West:
                    v0 = new Vector3(-0.5f, -0.5f, 0.5f); v1 = new Vector3(-0.5f, -0.5f, -0.5f);
                    v2 = new Vector3(-0.5f, 0.5f, -0.5f); v3 = new Vector3(-0.5f, 0.5f, 0.5f);
                    break;
                case BlockFace.East:
                    v0 = new Vector3(0.5f, -0.5f, -0.5f); v1 = new Vector3(0.5f, -0.5f, 0.5f);
                    v2 = new Vector3(0.5f, 0.5f, 0.5f); v3 = new Vector3(0.5f, 0.5f, -0.5f);
                    break;
                case BlockFace.Up:
                    v0 = new Vector3(-0.5f, 0.5f, 0.5f); v1 = new Vector3(0.5f, 0.5f, 0.5f);
                    v2 = new Vector3(0.5f, 0.5f, -0.5f); v3 = new Vector3(-0.5f, 0.5f, -0.5f);
                    break;
                default:
                    v0 = new Vector3(-0.5f, -0.5f, -0.5f); v1 = new Vector3(0.5f, -0.5f, -0.5f);
                    v2 = new Vector3(0.5f, -0.5f, 0.5f); v3 = new Vector3(-0.5f, -0.5f, 0.5f);
                    break;
            }
        }
    }
}
