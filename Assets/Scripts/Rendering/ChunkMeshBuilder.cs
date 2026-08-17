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
    /// MC ref: net.minecraft.client.renderer.block.ModelBlockRenderer — greedy face emission per snapshot.
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

        public static ChunkMeshData ComputeMeshData(ChunkMeshSnapshot snapshot)
        {
            var data = new ChunkMeshData { Position = snapshot.Position };

            if (snapshot.IsEmpty)
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

            // Use the snapshot's filled Y bounds with a 1-block margin for neighbor face culling.
            // If MinFilledY > MaxFilledY, the chunk is effectively empty.
            if (snapshot.MinFilledY > snapshot.MaxFilledY)
            {
                data.IsEmpty = true;
                data.SubmeshTriangles = CreateEmptySubmeshes();
                data.CollisionVertices = System.Array.Empty<Vector3>();
                data.CollisionTriangles = System.Array.Empty<int>();
                return data;
            }

            var minY = System.Math.Max(WorldConstants.MinY, snapshot.MinFilledY - 1);
            var maxY = System.Math.Min(WorldConstants.MaxY, snapshot.MaxFilledY + 1);

            for (var localX = 0; localX < WorldConstants.ChunkSize; localX++)
            {
                for (var y = minY; y <= maxY; y++)
                {
                    for (var localZ = 0; localZ < WorldConstants.ChunkSize; localZ++)
                    {
                        var blockId = snapshot.GetBlock(
                            snapshot.Position.GetMinBlockX() + localX, y, snapshot.Position.GetMinBlockZ() + localZ);
                        if (blockId == BlockId.Air)
                        {
                            continue;
                        }

                        var origin = new Vector3(
                            snapshot.Position.GetMinBlockX() + localX,
                            y,
                            snapshot.Position.GetMinBlockZ() + localZ);

                        if (BlockRegistry.IsFluid(blockId))
                        {
                            EmitWaterFaces(snapshot, localX, y, localZ, origin,
                                layerVertices, layerUvs, layerNormals, layerColors, layerTriangles);
                        }
                        else
                        {
                            var renderKind = BlockRegistry.Get(blockId).RenderKind;
                            if (renderKind == BlockRenderKind.Cross)
                            {
                                EmitCrossModel(blockId, snapshot, localX, y, localZ, origin,
                                    layerVertices, layerUvs, layerNormals, layerColors, layerTriangles);
                            }
                            else
                            {
                                EmitBlockFaces(blockId, snapshot, localX, y, localZ, origin,
                                    layerVertices, layerUvs, layerNormals, layerColors, layerTriangles, Buffers);
                            }
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

        public static void BuildInto(Mesh mesh, ChunkMeshSnapshot snapshot)
        {
            using (ChunkProfilerMarkers.MeshBuildInto.Auto())
            {
                ApplyMeshData(mesh, ComputeMeshData(snapshot));
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

        public static Mesh Build(ChunkMeshSnapshot snapshot)
        {
            var mesh = new Mesh { name = $"ChunkMesh_{snapshot.Position}" };
            BuildInto(mesh, snapshot);
            return mesh;
        }

        private static void EmitBlockFaces(
            BlockId blockId, ChunkMeshSnapshot snapshot,
            int localX,
            int y,
            int localZ,
            Vector3 origin,
            
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

            if (definition.RenderKind == BlockRenderKind.CutoutCube)
            {
                EmitCutoutCubeFaces(blockId, snapshot, localX, y, localZ, origin,
                    layerVertices, layerUvs, layerNormals, layerColors, layerTriangles, collisionBuffers);
                return;
            }

            for (var i = 0; i < AllFaces.Length; i++)
            {
                var face = AllFaces[i];
                if (!snapshot.ShouldRenderFaceInChunk(localX, y, localZ, face, blockId))
                {
                    continue;
                }

                if (definition.RenderKind == BlockRenderKind.GrassBlock)
                {
                    EmitGrassFace(snapshot, localX, y, localZ, face, origin,
                        layerVertices, layerUvs, layerNormals, layerColors, layerTriangles);
                    if (definition.IsSolid)
                    {
                        AddFace(face, origin, collisionBuffers.CollisionVertices, collisionBuffers.CollisionTriangles);
                    }
                }
                else
                {
                    var meshLayer = BlockIdToLayer(blockId);
                    byte metadata = BlockRegistry.IsLog(blockId) ? snapshot.GetMetadata(localX, y, localZ) : (byte)0;
                    AddFace(face, origin, GetBlockFaceTint(face), layerVertices[(int)meshLayer], layerUvs[(int)meshLayer],
                        layerNormals[(int)meshLayer], layerColors[(int)meshLayer], layerTriangles[(int)meshLayer], metadata);
                    if (definition.IsSolid)
                    {
                        AddFace(face, origin, collisionBuffers.CollisionVertices, collisionBuffers.CollisionTriangles);
                    }
                }
            }
        }

        private static void EmitCutoutCubeFaces(
            BlockId blockId, ChunkMeshSnapshot snapshot,
            int localX,
            int y,
            int localZ,
            Vector3 origin,
            
            List<Vector3>[] layerVertices,
            List<Vector2>[] layerUvs,
            List<Vector3>[] layerNormals,
            List<Color32>[] layerColors,
            List<int>[] layerTriangles,
            MeshBuildBuffers collisionBuffers)
        {
            var meshLayer = BlockIdToLayer(blockId);
            var foliageTint = GetFoliageTint(snapshot, localX, y, localZ);
            for (var fi = 0; fi < AllFaces.Length; fi++)
            {
                var cutoutFace = AllFaces[fi];
                if (!snapshot.ShouldRenderFaceInChunk(localX, y, localZ, cutoutFace, blockId))
                {
                    continue;
                }

                AddFace(cutoutFace, origin, MultiplyTint(foliageTint, GetBlockFaceTint(cutoutFace)),
                    layerVertices[(int)meshLayer], layerUvs[(int)meshLayer],
                    layerNormals[(int)meshLayer], layerColors[(int)meshLayer], layerTriangles[(int)meshLayer]);
            }

            if (BlockRegistry.IsSolid(blockId))
            {
                for (var cf = 0; cf < AllFaces.Length; cf++)
                {
                    AddFace(AllFaces[cf], origin, collisionBuffers.CollisionVertices, collisionBuffers.CollisionTriangles);
                }
            }
        }

        private static void EmitWaterFaces(
            ChunkMeshSnapshot snapshot,
            int localX,
            int y,
            int localZ,
            Vector3 origin,
            
            List<Vector3>[] layerVertices,
            List<Vector2>[] layerUvs,
            List<Vector3>[] layerNormals,
            List<Color32>[] layerColors,
            List<int>[] layerTriangles)
        {
            var blockId = BlockId.Water;
            var fluidLevel = snapshot.GetFluidLevel(localX, y, localZ);
            var surfaceHeight = FluidLevel.GetHeight01(fluidLevel);
            var layer = (int)ChunkMeshLayer.Water;

            for (var i = 0; i < AllFaces.Length; i++)
            {
                var face = AllFaces[i];
                if (!snapshot.ShouldRenderFaceInChunk(localX, y, localZ, face, blockId))
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
            ChunkMeshSnapshot snapshot,
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
            var biome = snapshot.GetBiome(localX, y, localZ);
            var isSnowy = BiomeRegistry.IsSnowyBiome(biome);
            var grassTint = BlockFaceLighting.ApplyShade(
                (Color32)BiomeRegistry.GetGrassTint(biome),
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
                    if (isSnowy)
                    {
                        AddFace(face, origin, GetBlockFaceTint(face), layerVertices[(int)ChunkMeshLayer.GrassSnowSide], layerUvs[(int)ChunkMeshLayer.GrassSnowSide],
                            layerNormals[(int)ChunkMeshLayer.GrassSnowSide], layerColors[(int)ChunkMeshLayer.GrassSnowSide], layerTriangles[(int)ChunkMeshLayer.GrassSnowSide]);
                    }
                    else
                    {
                        AddFace(face, origin, GetBlockFaceTint(face), layerVertices[(int)ChunkMeshLayer.GrassSide], layerUvs[(int)ChunkMeshLayer.GrassSide],
                            layerNormals[(int)ChunkMeshLayer.GrassSide], layerColors[(int)ChunkMeshLayer.GrassSide], layerTriangles[(int)ChunkMeshLayer.GrassSide]);
                        AddFace(face, origin, grassTint, layerVertices[(int)ChunkMeshLayer.GrassOverlay], layerUvs[(int)ChunkMeshLayer.GrassOverlay],
                            layerNormals[(int)ChunkMeshLayer.GrassOverlay], layerColors[(int)ChunkMeshLayer.GrassOverlay], layerTriangles[(int)ChunkMeshLayer.GrassOverlay]);
                    }
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
            BlockId.ShortGrass => ChunkMeshLayer.ShortGrass,
            BlockId.Fern => ChunkMeshLayer.Fern,
            BlockId.Dandelion => ChunkMeshLayer.Dandelion,
            BlockId.Poppy => ChunkMeshLayer.Poppy,
            BlockId.OakLeaves => ChunkMeshLayer.OakLeaves,
            BlockId.BirchLeaves => ChunkMeshLayer.BirchLeaves,
            BlockId.SpruceLeaves => ChunkMeshLayer.SpruceLeaves,
            BlockId.JungleLeaves => ChunkMeshLayer.JungleLeaves,
            BlockId.AcaciaLeaves => ChunkMeshLayer.AcaciaLeaves,
            BlockId.DarkOakLeaves => ChunkMeshLayer.DarkOakLeaves,
            BlockId.CherryLeaves => ChunkMeshLayer.CherryLeaves,
            BlockId.MangroveLeaves => ChunkMeshLayer.MangroveLeaves,
            BlockId.PaleOakLeaves => ChunkMeshLayer.PaleOakLeaves,
            BlockId.OakLog => ChunkMeshLayer.OakLog,
            BlockId.BirchLog => ChunkMeshLayer.BirchLog,
            BlockId.SpruceLog => ChunkMeshLayer.SpruceLog,
            BlockId.JungleLog => ChunkMeshLayer.JungleLog,
            BlockId.AcaciaLog => ChunkMeshLayer.AcaciaLog,
            BlockId.DarkOakLog => ChunkMeshLayer.DarkOakLog,
            BlockId.CherryLog => ChunkMeshLayer.CherryLog,
            BlockId.MangroveLog => ChunkMeshLayer.MangroveLog,
            BlockId.PaleOakLog => ChunkMeshLayer.PaleOakLog,
            BlockId.Snow => ChunkMeshLayer.Snow,
            _ => ChunkMeshLayer.Stone
        };

        private static Color32 GetGrassTint(ChunkMeshSnapshot snapshot, int localX, int y, int localZ)
        {
            var biome = snapshot.GetBiome(localX, y, localZ);
            return ToColor32(BiomeRegistry.GetGrassTint(biome));
        }

        private static Color32 GetFoliageTint(ChunkMeshSnapshot snapshot, int localX, int y, int localZ)
        {
            var biome = snapshot.GetBiome(localX, y, localZ);
            return ToColor32(BiomeRegistry.GetFoliageTint(biome));
        }

        private static Color32 ToColor32(Color color) =>
            new((byte)(color.r * 255f), (byte)(color.g * 255f), (byte)(color.b * 255f), 255);

        /// <summary>MC ref: two diagonal cross quads (tinted_plains_cross model).</summary>
        private static void EmitCrossModel(
            BlockId blockId, ChunkMeshSnapshot snapshot,
            int localX,
            int y,
            int localZ,
            Vector3 origin,
            List<Vector3>[] layerVertices,
            List<Vector2>[] layerUvs,
            List<Vector3>[] layerNormals,
            List<Color32>[] layerColors,
            List<int>[] layerTriangles)
        {
            var layer = (int)BlockIdToLayer(blockId);
            var tint = BlockRegistry.UsesGrassTint(blockId)
                ? GetGrassTint(snapshot, localX, y, localZ)
                : WhiteVertex;
            var shade = BlockFaceLighting.GetShadeColor(BlockFace.Up);
            tint = MultiplyTint(tint, shade);

            AddCrossQuad(origin,
                new Vector3(0f, 0f, 0f), new Vector3(1f, 0f, 1f), new Vector3(1f, 1f, 1f), new Vector3(0f, 1f, 0f),
                tint, layerVertices[layer], layerUvs[layer], layerNormals[layer], layerColors[layer], layerTriangles[layer]);
            AddCrossQuad(origin,
                new Vector3(1f, 0f, 0f), new Vector3(0f, 0f, 1f), new Vector3(0f, 1f, 1f), new Vector3(1f, 1f, 0f),
                tint, layerVertices[layer], layerUvs[layer], layerNormals[layer], layerColors[layer], layerTriangles[layer]);
        }

        private static Color32 MultiplyTint(Color32 tint, Color32 shade)
        {
            return new Color32(
                (byte)(tint.r * shade.r / 255),
                (byte)(tint.g * shade.g / 255),
                (byte)(tint.b * shade.b / 255),
                255);
        }

        private static void AddCrossQuad(
            Vector3 origin,
            Vector3 v0Local,
            Vector3 v1Local,
            Vector3 v2Local,
            Vector3 v3Local,
            Color32 tint,
            List<Vector3> vertices,
            List<Vector2> uvs,
            List<Vector3> normals,
            List<Color32> colors,
            List<int> triangles)
        {
            var start = vertices.Count;
            var v0 = origin + v0Local;
            var v1 = origin + v1Local;
            var v2 = origin + v2Local;
            var v3 = origin + v3Local;
            var edge1 = v1 - v0;
            var edge2 = v2 - v0;
            var normal = Vector3.Cross(edge1, edge2).normalized;

            vertices.Add(v0);
            vertices.Add(v1);
            vertices.Add(v2);
            vertices.Add(v3);
            uvs.Add(new Vector2(0f, 0f));
            uvs.Add(new Vector2(1f, 0f));
            uvs.Add(new Vector2(1f, 1f));
            uvs.Add(new Vector2(0f, 1f));
            for (var i = 0; i < 4; i++)
            {
                normals.Add(normal);
                colors.Add(tint);
            }

            AddQuadTriangles(triangles, start, v0, v1, v2, v3, normal);
        }

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
            List<int> triangles,
            byte metadata = 0)
        {
            var start = vertices.Count;
            GetFaceQuad(face, origin, out var v0, out var v1, out var v2, out var v3);
            var normal = GetOutwardNormal(face);

            vertices.Add(v0);
            vertices.Add(v1);
            vertices.Add(v2);
            vertices.Add(v3);

            if (metadata == 1 && (face == BlockFace.North || face == BlockFace.South || face == BlockFace.Up || face == BlockFace.Down))
            {
                uvs.Add(new Vector2(0f, 1f));
                uvs.Add(new Vector2(0f, 0f));
                uvs.Add(new Vector2(1f, 0f));
                uvs.Add(new Vector2(1f, 1f));
            }
            else if (metadata == 2 && (face == BlockFace.East || face == BlockFace.West))
            {
                uvs.Add(new Vector2(0f, 1f));
                uvs.Add(new Vector2(0f, 0f));
                uvs.Add(new Vector2(1f, 0f));
                uvs.Add(new Vector2(1f, 1f));
            }
            else
            {
                uvs.Add(new Vector2(0f, 0f));
                uvs.Add(new Vector2(1f, 0f));
                uvs.Add(new Vector2(1f, 1f));
                uvs.Add(new Vector2(0f, 1f));
            }

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




