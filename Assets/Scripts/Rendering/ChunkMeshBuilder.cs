using System;
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
        private static readonly Color32 WhiteVertex = Color.white;

        private static Color32 GetBlockFaceTint(BlockFace face) => BlockFaceLighting.GetShadeColor(face);

        private static readonly BlockFace[] AllFaces =
        {
            BlockFace.Down, BlockFace.Up, BlockFace.North, BlockFace.South, BlockFace.West, BlockFace.East
        };

        [ThreadStatic] private static MeshBuildBuffers _threadBuffers;

        private static MeshBuildBuffers Buffers => _threadBuffers ??= new MeshBuildBuffers();

        public static ChunkMeshData ComputeMeshData(Chunk chunk, Level level)
        {
            var data = new ChunkMeshData { Position = chunk.Position };

            if (!chunk.HasBlocks)
            {
                data.IsEmpty = true;
                data.SubmeshTriangles = CreateEmptySubmeshes();
                data.CollisionVertices = System.Array.Empty<Vector3>();
                data.CollisionTriangles = System.Array.Empty<int>();
                return data;
            }

            Buffers.Clear();
            var layerVertices = Buffers.LayerVertices;
            var layerUvs = Buffers.LayerUvs;
            var layerNormals = Buffers.LayerNormals;
            var layerColors = Buffers.LayerColors;
            var layerTriangles = Buffers.LayerTriangles;

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

                        if (BlockRegistry.IsFluid(blockId))
                        {
                            EmitWaterFaces(chunk, localX, y, localZ, origin, level,
                                layerVertices, layerUvs, layerNormals, layerColors, layerTriangles);
                        }
                        else
                        {
                            EmitBlockFaces(blockId, chunk, localX, y, localZ, origin, level,
                                layerVertices, layerUvs, layerNormals, layerColors, layerTriangles, Buffers);
                        }
                    }
                }
            }

            PackMeshData(data, layerVertices, layerUvs, layerNormals, layerColors, layerTriangles, Buffers);
            data.CollisionVertices = Buffers.CollisionVertices.Count > 0
                ? Buffers.CollisionVertices.ToArray()
                : System.Array.Empty<Vector3>();
            data.CollisionTriangles = Buffers.CollisionTriangles.Count > 0
                ? Buffers.CollisionTriangles.ToArray()
                : System.Array.Empty<int>();
            return data;
        }

        public static void ApplyMeshData(Mesh mesh, in ChunkMeshData data)
        {
            mesh.name = $"ChunkMesh_{data.Position}";
            mesh.indexFormat = IndexFormat.UInt32;
            mesh.Clear(false);

            if (data.IsEmpty)
            {
                mesh.subMeshCount = (int)ChunkMeshLayer.Count;
                for (var layer = 0; layer < (int)ChunkMeshLayer.Count; layer++)
                {
                    mesh.SetTriangles(System.Array.Empty<int>(), layer);
                }

                return;
            }

            mesh.SetVertices(data.Vertices);
            mesh.SetUVs(0, data.Uvs);
            mesh.SetNormals(data.Normals);
            if (data.Colors.Length == data.Vertices.Length && data.Colors.Length > 0)
            {
                mesh.SetColors(data.Colors);
            }
            mesh.subMeshCount = (int)ChunkMeshLayer.Count;

            for (var layer = 0; layer < (int)ChunkMeshLayer.Count; layer++)
            {
                mesh.SetTriangles(data.SubmeshTriangles[layer] ?? System.Array.Empty<int>(), layer);
            }

            mesh.RecalculateBounds();
        }

        public static void BuildInto(Mesh mesh, Chunk chunk, Level level)
        {
            using (ChunkProfilerMarkers.MeshBuildInto.Auto())
            {
                ApplyMeshData(mesh, ComputeMeshData(chunk, level));
            }
        }

        private sealed class MeshBuildBuffers
        {
            public readonly List<Vector3>[] LayerVertices = new List<Vector3>[(int)ChunkMeshLayer.Count];
            public readonly List<Vector2>[] LayerUvs = new List<Vector2>[(int)ChunkMeshLayer.Count];
            public readonly List<Vector3>[] LayerNormals = new List<Vector3>[(int)ChunkMeshLayer.Count];
            public readonly List<Color32>[] LayerColors = new List<Color32>[(int)ChunkMeshLayer.Count];
            public readonly List<int>[] LayerTriangles = new List<int>[(int)ChunkMeshLayer.Count];
            public readonly List<int>[] SubmeshTriangles = new List<int>[(int)ChunkMeshLayer.Count];
            public readonly List<Vector3> AllVertices = new(4096);
            public readonly List<Vector2> AllUvs = new(4096);
            public readonly List<Vector3> AllNormals = new(4096);
            public readonly List<Color32> AllColors = new(4096);
            public readonly List<Vector3> CollisionVertices = new(1024);
            public readonly List<int> CollisionTriangles = new(1536);

            public MeshBuildBuffers()
            {
                for (var i = 0; i < (int)ChunkMeshLayer.Count; i++)
                {
                    LayerVertices[i] = new List<Vector3>(256);
                    LayerUvs[i] = new List<Vector2>(256);
                    LayerNormals[i] = new List<Vector3>(256);
                    LayerColors[i] = new List<Color32>(256);
                    LayerTriangles[i] = new List<int>(384);
                    SubmeshTriangles[i] = new List<int>(384);
                }
            }

            public void Clear()
            {
                for (var i = 0; i < (int)ChunkMeshLayer.Count; i++)
                {
                    LayerVertices[i].Clear();
                    LayerUvs[i].Clear();
                    LayerNormals[i].Clear();
                    LayerColors[i].Clear();
                    LayerTriangles[i].Clear();
                }

                CollisionVertices.Clear();
                CollisionTriangles.Clear();
            }

            public void ClearCombine()
            {
                AllVertices.Clear();
                AllUvs.Clear();
                AllNormals.Clear();
                AllColors.Clear();
                for (var i = 0; i < (int)ChunkMeshLayer.Count; i++)
                {
                    SubmeshTriangles[i].Clear();
                }
            }
        }

        private static int[][] CreateEmptySubmeshes()
        {
            var submeshes = new int[(int)ChunkMeshLayer.Count][];
            for (var i = 0; i < submeshes.Length; i++)
            {
                submeshes[i] = System.Array.Empty<int>();
            }

            return submeshes;
        }

        private static void PackMeshData(
            ChunkMeshData data,
            List<Vector3>[] layerVertices,
            List<Vector2>[] layerUvs,
            List<Vector3>[] layerNormals,
            List<Color32>[] layerColors,
            List<int>[] layerTriangles,
            MeshBuildBuffers combineBuffers)
        {
            data.IsEmpty = false;
            combineBuffers.ClearCombine();

            var allVertices = combineBuffers.AllVertices;
            var allUvs = combineBuffers.AllUvs;
            var allNormals = combineBuffers.AllNormals;
            var allColors = combineBuffers.AllColors;
            var submeshTriangles = combineBuffers.SubmeshTriangles;
            var vertexOffset = 0;

            for (var layer = 0; layer < (int)ChunkMeshLayer.Count; layer++)
            {
                if (layerVertices[layer].Count == 0)
                {
                    continue;
                }

                allVertices.AddRange(layerVertices[layer]);
                allUvs.AddRange(layerUvs[layer]);
                allNormals.AddRange(layerNormals[layer]);
                allColors.AddRange(layerColors[layer]);

                var offset = vertexOffset;
                var tris = layerTriangles[layer];
                var targetTris = submeshTriangles[layer];
                for (var i = 0; i < tris.Count; i++)
                {
                    targetTris.Add(tris[i] + offset);
                }

                vertexOffset += layerVertices[layer].Count;
            }

            data.Vertices = allVertices.ToArray();
            data.Uvs = allUvs.ToArray();
            data.Normals = allNormals.ToArray();
            data.Colors = allColors.ToArray();
            data.SubmeshTriangles = new int[(int)ChunkMeshLayer.Count][];
            for (var layer = 0; layer < (int)ChunkMeshLayer.Count; layer++)
            {
                data.SubmeshTriangles[layer] = submeshTriangles[layer].ToArray();
            }
        }

        public static Mesh Build(Chunk chunk, Level level)
        {
            var mesh = new Mesh { name = $"ChunkMesh_{chunk.Position}" };
            BuildInto(mesh, chunk, level);
            return mesh;
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
            List<Color32>[] layerColors,
            List<int>[] layerTriangles,
            MeshBuildBuffers collisionBuffers)
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
                    EmitGrassFace(chunk, localX, y, localZ, face, origin,
                        layerVertices, layerUvs, layerNormals, layerColors, layerTriangles);
                    if (definition.IsSolid)
                    {
                        AddFace(face, origin, collisionBuffers.CollisionVertices, collisionBuffers.CollisionTriangles);
                    }
                }
                else
                {
                    var meshLayer = BlockIdToLayer(blockId);
                    AddFace(face, origin, GetBlockFaceTint(face), layerVertices[(int)meshLayer], layerUvs[(int)meshLayer],
                        layerNormals[(int)meshLayer], layerColors[(int)meshLayer], layerTriangles[(int)meshLayer]);
                    if (definition.IsSolid)
                    {
                        AddFace(face, origin, collisionBuffers.CollisionVertices, collisionBuffers.CollisionTriangles);
                    }
                }
            }
        }

        private static void EmitWaterFaces(
            Chunk chunk,
            int localX,
            int y,
            int localZ,
            Vector3 origin,
            Level level,
            List<Vector3>[] layerVertices,
            List<Vector2>[] layerUvs,
            List<Vector3>[] layerNormals,
            List<Color32>[] layerColors,
            List<int>[] layerTriangles)
        {
            var blockId = BlockId.Water;
            var fluidLevel = chunk.GetFluidLevel(localX, y, localZ);
            var surfaceHeight = FluidLevel.GetHeight01(fluidLevel);
            var layer = (int)ChunkMeshLayer.Water;

            for (var i = 0; i < AllFaces.Length; i++)
            {
                var face = AllFaces[i];
                if (!level.ShouldRenderFaceInChunk(chunk, localX, y, localZ, face, blockId))
                {
                    continue;
                }

                var faceTint = BlockFaceLighting.GetShadeColor(face);

                if (face == BlockFace.Up)
                {
                    AddFluidTopFace(origin, surfaceHeight, faceTint, layerVertices[layer], layerUvs[layer],
                        layerNormals[layer], layerColors[layer], layerTriangles[layer]);
                }
                else if (face == BlockFace.Down)
                {
                    AddFace(face, origin, faceTint, layerVertices[layer], layerUvs[layer],
                        layerNormals[layer], layerColors[layer], layerTriangles[layer]);
                }
                else
                {
                    AddFluidSideFace(face, origin, surfaceHeight, faceTint, layerVertices[layer], layerUvs[layer],
                        layerNormals[layer], layerColors[layer], layerTriangles[layer]);
                }
            }
        }

        private static void EmitGrassFace(
            Chunk chunk,
            int localX,
            int y,
            int localZ,
            BlockFace face,
            Vector3 origin,
            List<Vector3>[] layerVertices,
            List<Vector2>[] layerUvs,
            List<Vector3>[] layerNormals,
            List<Color32>[] layerColors,
            List<int>[] layerTriangles)
        {
            var grassTint = BlockFaceLighting.ApplyShade(
                (Color32)BiomeRegistry.GetGrassTint(chunk.GetBiome(localX, y, localZ)),
                face);

            switch (face)
            {
                case BlockFace.Up:
                    AddFace(face, origin, grassTint, layerVertices[(int)ChunkMeshLayer.GrassTop], layerUvs[(int)ChunkMeshLayer.GrassTop],
                        layerNormals[(int)ChunkMeshLayer.GrassTop], layerColors[(int)ChunkMeshLayer.GrassTop], layerTriangles[(int)ChunkMeshLayer.GrassTop]);
                    break;
                case BlockFace.Down:
                    AddFace(face, origin, GetBlockFaceTint(face), layerVertices[(int)ChunkMeshLayer.GrassBottom], layerUvs[(int)ChunkMeshLayer.GrassBottom],
                        layerNormals[(int)ChunkMeshLayer.GrassBottom], layerColors[(int)ChunkMeshLayer.GrassBottom], layerTriangles[(int)ChunkMeshLayer.GrassBottom]);
                    break;
                default:
                    AddFace(face, origin, GetBlockFaceTint(face), layerVertices[(int)ChunkMeshLayer.GrassSide], layerUvs[(int)ChunkMeshLayer.GrassSide],
                        layerNormals[(int)ChunkMeshLayer.GrassSide], layerColors[(int)ChunkMeshLayer.GrassSide], layerTriangles[(int)ChunkMeshLayer.GrassSide]);
                    AddFace(face, origin, grassTint, layerVertices[(int)ChunkMeshLayer.GrassOverlay], layerUvs[(int)ChunkMeshLayer.GrassOverlay],
                        layerNormals[(int)ChunkMeshLayer.GrassOverlay], layerColors[(int)ChunkMeshLayer.GrassOverlay], layerTriangles[(int)ChunkMeshLayer.GrassOverlay]);
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
            BlockId.Gravel => ChunkMeshLayer.Gravel,
            _ => ChunkMeshLayer.Stone
        };

        private static void AddFace(
            BlockFace face,
            Vector3 origin,
            List<Vector3> vertices,
            List<int> triangles)
        {
            var start = vertices.Count;
            GetFaceQuad(face, origin, out var v0, out var v1, out var v2, out var v3);
            var normal = GetOutwardNormal(face);

            vertices.Add(v0);
            vertices.Add(v1);
            vertices.Add(v2);
            vertices.Add(v3);
            AddQuadTriangles(triangles, start, v0, v1, v2, v3, normal);
        }

        private static void AddFace(
            BlockFace face,
            Vector3 origin,
            Color32 tint,
            List<Vector3> vertices,
            List<Vector2> uvs,
            List<Vector3> normals,
            List<Color32> colors,
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

            colors.Add(tint);
            colors.Add(tint);
            colors.Add(tint);
            colors.Add(tint);

            AddQuadTriangles(triangles, start, v0, v1, v2, v3, normal);
        }

        private static void AddFluidTopFace(
            Vector3 origin,
            float surfaceHeight,
            Color32 tint,
            List<Vector3> vertices,
            List<Vector2> uvs,
            List<Vector3> normals,
            List<Color32> colors,
            List<int> triangles)
        {
            var y = origin.y + surfaceHeight;
            var start = vertices.Count;
            var v0 = new Vector3(origin.x, y, origin.z + 1f);
            var v1 = new Vector3(origin.x + 1f, y, origin.z + 1f);
            var v2 = new Vector3(origin.x + 1f, y, origin.z);
            var v3 = new Vector3(origin.x, y, origin.z);
            var normal = Vector3.up;

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

            colors.Add(tint);
            colors.Add(tint);
            colors.Add(tint);
            colors.Add(tint);

            AddQuadTriangles(triangles, start, v0, v1, v2, v3, normal);
        }

        private static void AddFluidSideFace(
            BlockFace face,
            Vector3 origin,
            float surfaceHeight,
            Color32 tint,
            List<Vector3> vertices,
            List<Vector2> uvs,
            List<Vector3> normals,
            List<Color32> colors,
            List<int> triangles)
        {
            var start = vertices.Count;
            GetFluidSideQuad(face, origin, surfaceHeight, out var v0, out var v1, out var v2, out var v3);
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

            colors.Add(tint);
            colors.Add(tint);
            colors.Add(tint);
            colors.Add(tint);

            AddQuadTriangles(triangles, start, v0, v1, v2, v3, normal);
        }

        private static void AddQuadTriangles(
            List<int> triangles,
            int start,
            Vector3 v0,
            Vector3 v1,
            Vector3 v2,
            Vector3 v3,
            Vector3 normal)
        {
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

        private static void GetFluidSideQuad(
            BlockFace face,
            Vector3 origin,
            float surfaceHeight,
            out Vector3 v0,
            out Vector3 v1,
            out Vector3 v2,
            out Vector3 v3)
        {
            var topY = origin.y + surfaceHeight;
            switch (face)
            {
                case BlockFace.North:
                    v0 = new Vector3(origin.x, origin.y, origin.z);
                    v1 = new Vector3(origin.x + 1f, origin.y, origin.z);
                    v2 = new Vector3(origin.x + 1f, topY, origin.z);
                    v3 = new Vector3(origin.x, topY, origin.z);
                    break;
                case BlockFace.South:
                    v0 = new Vector3(origin.x + 1f, origin.y, origin.z + 1f);
                    v1 = new Vector3(origin.x, origin.y, origin.z + 1f);
                    v2 = new Vector3(origin.x, topY, origin.z + 1f);
                    v3 = new Vector3(origin.x + 1f, topY, origin.z + 1f);
                    break;
                case BlockFace.West:
                    v0 = new Vector3(origin.x, origin.y, origin.z + 1f);
                    v1 = new Vector3(origin.x, origin.y, origin.z);
                    v2 = new Vector3(origin.x, topY, origin.z);
                    v3 = new Vector3(origin.x, topY, origin.z + 1f);
                    break;
                case BlockFace.East:
                    v0 = new Vector3(origin.x + 1f, origin.y, origin.z);
                    v1 = new Vector3(origin.x + 1f, origin.y, origin.z + 1f);
                    v2 = new Vector3(origin.x + 1f, topY, origin.z + 1f);
                    v3 = new Vector3(origin.x + 1f, topY, origin.z);
                    break;
                default:
                    GetFaceQuad(face, origin, out v0, out v1, out v2, out v3);
                    break;
            }
        }

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
