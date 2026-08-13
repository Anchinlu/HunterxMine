using System.Collections.Generic;
using MineCraftUnity.Blocks;
using MineCraftUnity.Core;
using MineCraftUnity.World;
using UnityEngine;
using UnityEngine.Rendering;

namespace MineCraftUnity.Rendering
{
    /// <summary>
    /// MC ref: net.minecraft.client.renderer.block.ModelBlockRenderer — greedy face emission per chunk.
    /// </summary>
    public static class ChunkMeshBuilder
    {
        private static readonly BlockFace[] AllFaces =
        {
            BlockFace.Down, BlockFace.Up, BlockFace.North, BlockFace.South, BlockFace.West, BlockFace.East
        };

        public static Mesh Build(Chunk chunk, Level level)
        {
            var mesh = new Mesh { name = $"ChunkMesh_{chunk.Position}" };
            BuildInto(mesh, chunk, level);
            return mesh;
        }

        public static void BuildInto(Mesh mesh, Chunk chunk, Level level)
        {
            if (!chunk.HasBlocks)
            {
                ApplyEmptyMesh(mesh, chunk.Position);
                return;
            }

            var layerVertices = new List<Vector3>[(int)ChunkMeshLayer.Count];
            var layerUvs = new List<Vector2>[(int)ChunkMeshLayer.Count];
            var layerNormals = new List<Vector3>[(int)ChunkMeshLayer.Count];
            var layerTriangles = new List<int>[(int)ChunkMeshLayer.Count];

            for (var i = 0; i < (int)ChunkMeshLayer.Count; i++)
            {
                layerVertices[i] = new List<Vector3>(256);
                layerUvs[i] = new List<Vector2>(256);
                layerNormals[i] = new List<Vector3>(256);
                layerTriangles[i] = new List<int>(384);
            }

            var minY = chunk.MinFilledY;
            var maxY = chunk.MaxFilledY;

            for (var localX = 0; localX < WorldConstants.ChunkSize; localX++)
            {
                for (var y = minY; y <= maxY; y++)
                {
                    for (var localZ = 0; localZ < WorldConstants.ChunkSize; localZ++)
                    {
                        var blockId = chunk.GetBlock(localX, y, localZ);
                        if (blockId == BlockId.Air)
                        {
                            continue;
                        }

                        var origin = new Vector3(
                            chunk.Position.GetMinBlockX() + localX,
                            y,
                            chunk.Position.GetMinBlockZ() + localZ);

                        EmitBlockFaces(blockId, chunk, localX, y, localZ, origin, level,
                            layerVertices, layerUvs, layerNormals, layerTriangles);
                    }
                }
            }

            CombineLayersInto(mesh, chunk.Position, layerVertices, layerUvs, layerNormals, layerTriangles);
        }

        private static void ApplyEmptyMesh(Mesh mesh, ChunkPos position)
        {
            mesh.name = $"ChunkMesh_{position}";
            mesh.Clear();
            mesh.subMeshCount = (int)ChunkMeshLayer.Count;
            for (var layer = 0; layer < (int)ChunkMeshLayer.Count; layer++)
            {
                mesh.SetTriangles(System.Array.Empty<int>(), layer);
            }
        }

        private static void CombineLayersInto(
            Mesh mesh,
            ChunkPos position,
            List<Vector3>[] layerVertices,
            List<Vector2>[] layerUvs,
            List<Vector3>[] layerNormals,
            List<int>[] layerTriangles)
        {
            mesh.name = $"ChunkMesh_{position}";
            mesh.indexFormat = IndexFormat.UInt32;
            mesh.Clear(false);

            var allVertices = new List<Vector3>(4096);
            var allUvs = new List<Vector2>(4096);
            var allNormals = new List<Vector3>(4096);
            var submeshTriangles = new List<int>[(int)ChunkMeshLayer.Count];
            var vertexOffset = 0;

            for (var layer = 0; layer < (int)ChunkMeshLayer.Count; layer++)
            {
                submeshTriangles[layer] = new List<int>();
                if (layerVertices[layer].Count == 0)
                {
                    continue;
                }

                allVertices.AddRange(layerVertices[layer]);
                allUvs.AddRange(layerUvs[layer]);
                allNormals.AddRange(layerNormals[layer]);

                var offset = vertexOffset;
                var tris = layerTriangles[layer];
                for (var i = 0; i < tris.Count; i++)
                {
                    submeshTriangles[layer].Add(tris[i] + offset);
                }

                vertexOffset += layerVertices[layer].Count;
            }

            mesh.SetVertices(allVertices);
            mesh.SetUVs(0, allUvs);
            mesh.SetNormals(allNormals);
            mesh.subMeshCount = (int)ChunkMeshLayer.Count;

            for (var layer = 0; layer < (int)ChunkMeshLayer.Count; layer++)
            {
                mesh.SetTriangles(submeshTriangles[layer], layer);
            }

            mesh.RecalculateBounds();
        }

        private static void EmitBlockFaces(
            BlockId blockId,
            Chunk chunk,
            int localX,
            int y,
            int localZ,
            Vector3 origin,
            Level level,
            List<Vector3>[] layerVertices,
            List<Vector2>[] layerUvs,
            List<Vector3>[] layerNormals,
            List<int>[] layerTriangles)
        {
            var definition = BlockRegistry.Get(blockId);
            if (definition.RenderKind == BlockRenderKind.None)
            {
                return;
            }

            for (var i = 0; i < AllFaces.Length; i++)
            {
                var face = AllFaces[i];
                if (!level.ShouldRenderFaceInChunk(chunk, localX, y, localZ, face, blockId))
                {
                    continue;
                }

                if (definition.RenderKind == BlockRenderKind.GrassBlock)
                {
                    EmitGrassFace(face, origin, layerVertices, layerUvs, layerNormals, layerTriangles);
                }
                else
                {
                    var layer = BlockIdToLayer(blockId);
                    AddFace(face, origin, layerVertices[(int)layer], layerUvs[(int)layer],
                        layerNormals[(int)layer], layerTriangles[(int)layer]);
                }
            }
        }

        private static void EmitGrassFace(
            BlockFace face,
            Vector3 origin,
            List<Vector3>[] layerVertices,
            List<Vector2>[] layerUvs,
            List<Vector3>[] layerNormals,
            List<int>[] layerTriangles)
        {
            switch (face)
            {
                case BlockFace.Up:
                    AddFace(face, origin, layerVertices[(int)ChunkMeshLayer.GrassTop], layerUvs[(int)ChunkMeshLayer.GrassTop],
                        layerNormals[(int)ChunkMeshLayer.GrassTop], layerTriangles[(int)ChunkMeshLayer.GrassTop]);
                    break;
                case BlockFace.Down:
                    AddFace(face, origin, layerVertices[(int)ChunkMeshLayer.GrassBottom], layerUvs[(int)ChunkMeshLayer.GrassBottom],
                        layerNormals[(int)ChunkMeshLayer.GrassBottom], layerTriangles[(int)ChunkMeshLayer.GrassBottom]);
                    break;
                default:
                    AddFace(face, origin, layerVertices[(int)ChunkMeshLayer.GrassSide], layerUvs[(int)ChunkMeshLayer.GrassSide],
                        layerNormals[(int)ChunkMeshLayer.GrassSide], layerTriangles[(int)ChunkMeshLayer.GrassSide]);
                    AddFace(face, origin, layerVertices[(int)ChunkMeshLayer.GrassOverlay], layerUvs[(int)ChunkMeshLayer.GrassOverlay],
                        layerNormals[(int)ChunkMeshLayer.GrassOverlay], layerTriangles[(int)ChunkMeshLayer.GrassOverlay]);
                    break;
            }
        }

        private static ChunkMeshLayer BlockIdToLayer(BlockId id) => id switch
        {
            BlockId.Stone => ChunkMeshLayer.Stone,
            BlockId.Dirt => ChunkMeshLayer.Dirt,
            BlockId.Sand => ChunkMeshLayer.Sand,
            BlockId.Water => ChunkMeshLayer.Water,
            BlockId.Bedrock => ChunkMeshLayer.Bedrock,
            _ => ChunkMeshLayer.Stone
        };

        private static void AddFace(
            BlockFace face,
            Vector3 origin,
            List<Vector3> vertices,
            List<Vector2> uvs,
            List<Vector3> normals,
            List<int> triangles)
        {
            var start = vertices.Count;
            GetFaceQuad(face, origin, out var v0, out var v1, out var v2, out var v3);
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

        private static void GetFaceQuad(BlockFace face, Vector3 origin, out Vector3 v0, out Vector3 v1, out Vector3 v2, out Vector3 v3)
        {
            switch (face)
            {
                case BlockFace.North:
                    v0 = origin + new Vector3(0f, 0f, 0f); v1 = origin + new Vector3(1f, 0f, 0f);
                    v2 = origin + new Vector3(1f, 1f, 0f); v3 = origin + new Vector3(0f, 1f, 0f);
                    break;
                case BlockFace.South:
                    v0 = origin + new Vector3(1f, 0f, 1f); v1 = origin + new Vector3(0f, 0f, 1f);
                    v2 = origin + new Vector3(0f, 1f, 1f); v3 = origin + new Vector3(1f, 1f, 1f);
                    break;
                case BlockFace.West:
                    v0 = origin + new Vector3(0f, 0f, 1f); v1 = origin + new Vector3(0f, 0f, 0f);
                    v2 = origin + new Vector3(0f, 1f, 0f); v3 = origin + new Vector3(0f, 1f, 1f);
                    break;
                case BlockFace.East:
                    v0 = origin + new Vector3(1f, 0f, 0f); v1 = origin + new Vector3(1f, 0f, 1f);
                    v2 = origin + new Vector3(1f, 1f, 1f); v3 = origin + new Vector3(1f, 1f, 0f);
                    break;
                case BlockFace.Up:
                    v0 = origin + new Vector3(0f, 1f, 1f); v1 = origin + new Vector3(1f, 1f, 1f);
                    v2 = origin + new Vector3(1f, 1f, 0f); v3 = origin + new Vector3(0f, 1f, 0f);
                    break;
                default:
                    v0 = origin + new Vector3(0f, 0f, 0f); v1 = origin + new Vector3(1f, 0f, 0f);
                    v2 = origin + new Vector3(1f, 0f, 1f); v3 = origin + new Vector3(0f, 0f, 1f);
                    break;
            }
        }
    }
}
