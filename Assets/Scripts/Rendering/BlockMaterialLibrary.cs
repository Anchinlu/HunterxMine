using MineCraftUnity.Blocks;
using UnityEngine;

namespace MineCraftUnity.Rendering
{
    /// <summary>
    /// Shared block materials for chunk rendering. MC ref: block model + biome tint on grass.
    /// </summary>
    public static class BlockMaterialLibrary
    {
        private const string TextureRoot = "Assets/Minecraft/Blocks";
        private static readonly Color WaterMaterialTint = new(0.25f, 0.45f, 0.85f, 0.75f);

        private static Material[] _materials;
        private static bool _initialized;

        public static Material GetMaterial(ChunkMeshLayer layer)
        {
            EnsureInitialized();
            return _materials[(int)layer];
        }

        public static Material[] GetAllMaterials()
        {
            EnsureInitialized();
            return _materials;
        }

        public static void EnsureInitialized()
        {
            DayNightController.EnsureDefaultSkyLight();

            if (_initialized)
            {
                return;
            }

            _materials = new Material[(int)ChunkMeshLayer.Count];
            _materials[(int)ChunkMeshLayer.Stone] = CreateCubeMaterial("stone/Textures/stone.png");
            _materials[(int)ChunkMeshLayer.Dirt] = CreateCubeMaterial("dirt/Textures/dirt.png");
            _materials[(int)ChunkMeshLayer.Sand] = CreateCubeMaterial("sand/Textures/sand.png");
            _materials[(int)ChunkMeshLayer.Water] = CreateWaterMaterial("water/Textures/water_still.png");
            _materials[(int)ChunkMeshLayer.Bedrock] = CreateCubeMaterial("bedrock/Textures/bedrock.png");
            _materials[(int)ChunkMeshLayer.GrassTop] = CreateTintMaterial("grass_block/Textures/grass_block_top.png", Color.white);
            _materials[(int)ChunkMeshLayer.GrassBottom] = CreateCubeMaterial("grass_block/Textures/dirt.png");
            _materials[(int)ChunkMeshLayer.GrassSide] = CreateCubeMaterial("grass_block/Textures/grass_block_side.png");
            _materials[(int)ChunkMeshLayer.GrassOverlay] = CreateOverlayMaterial("grass_block/Textures/grass_block_side_overlay.png", Color.white);
            _materials[(int)ChunkMeshLayer.Gravel] = CreateCubeMaterial("gravel/Textures/gravel.png");
            _materials[(int)ChunkMeshLayer.ShortGrass] = CreateCutoutMaterial("short_grass/Textures/short_grass.png", Color.white);
            _materials[(int)ChunkMeshLayer.Fern] = CreateCutoutMaterial("fern/Textures/fern.png", Color.white);
            _materials[(int)ChunkMeshLayer.Dandelion] = CreateCutoutMaterial("dandelion/Textures/dandelion.png", Color.white);
            _materials[(int)ChunkMeshLayer.Poppy] = CreateCutoutMaterial("poppy/Textures/poppy.png", Color.white);
            _materials[(int)ChunkMeshLayer.OakLeaves] = CreateCutoutMaterial("oak_leaves/Textures/oak_leaves.png", Color.white);
            _materials[(int)ChunkMeshLayer.BirchLeaves] = CreateCutoutMaterial("birch_leaves/Textures/birch_leaves.png", Color.white);
            _materials[(int)ChunkMeshLayer.SpruceLeaves] = CreateCutoutMaterial("spruce_leaves/Textures/spruce_leaves.png", Color.white);
            _materials[(int)ChunkMeshLayer.OakLog] = CreateCubeMaterial("oak_log/Textures/oak_log.png");
            _materials[(int)ChunkMeshLayer.BirchLog] = CreateCubeMaterial("birch_log/Textures/birch_log.png");
            _materials[(int)ChunkMeshLayer.SpruceLog] = CreateCubeMaterial("spruce_log/Textures/spruce_log.png");
            _materials[(int)ChunkMeshLayer.JungleLog] = CreateCubeMaterial("jungle_log/Textures/jungle_log.png");
            _materials[(int)ChunkMeshLayer.AcaciaLog] = CreateCubeMaterial("acacia_log/Textures/acacia_log.png");
            _materials[(int)ChunkMeshLayer.DarkOakLog] = CreateCubeMaterial("dark_oak_log/Textures/dark_oak_log.png");
            _materials[(int)ChunkMeshLayer.CherryLog] = CreateCubeMaterial("cherry_log/Textures/cherry_log.png");
            _materials[(int)ChunkMeshLayer.MangroveLog] = CreateCubeMaterial("mangrove_log/Textures/mangrove_log.png");
            _materials[(int)ChunkMeshLayer.PaleOakLog] = CreateCubeMaterial("pale_oak_log/Textures/pale_oak_log.png");
            _materials[(int)ChunkMeshLayer.JungleLeaves] = CreateCutoutMaterial("jungle_leaves/Textures/jungle_leaves.png", Color.white);
            _materials[(int)ChunkMeshLayer.AcaciaLeaves] = CreateCutoutMaterial("acacia_leaves/Textures/acacia_leaves.png", Color.white);
            _materials[(int)ChunkMeshLayer.DarkOakLeaves] = CreateCutoutMaterial("dark_oak_leaves/Textures/dark_oak_leaves.png", Color.white);
            _materials[(int)ChunkMeshLayer.CherryLeaves] = CreateCutoutMaterial("cherry_leaves/Textures/cherry_leaves.png", Color.white);
            _materials[(int)ChunkMeshLayer.MangroveLeaves] = CreateCutoutMaterial("mangrove_leaves/Textures/mangrove_leaves.png", Color.white);
            _materials[(int)ChunkMeshLayer.PaleOakLeaves] = CreateCutoutMaterial("pale_oak_leaves/Textures/pale_oak_leaves.png", Color.white);
            _materials[(int)ChunkMeshLayer.Snow] = CreateCubeMaterial("snow/Textures/snow.png");
            _materials[(int)ChunkMeshLayer.GrassSnowSide] = CreateCubeMaterial("grass_block/Textures/grass_block_snow.png");
            _initialized = true;
        }

        public static void Reset()
        {
            if (_materials != null)
            {
                foreach (var mat in _materials)
                {
                    if (mat != null)
                    {
                        if (Application.isPlaying)
                        {
                            Object.Destroy(mat);
                        }
                        else
                        {
                            Object.DestroyImmediate(mat);
                        }
                    }
                }
            }

            _materials = null;
            _initialized = false;
        }

        private static Material CreateCubeMaterial(string relativePath)
        {
            return CreateTintMaterial(relativePath, Color.white);
        }

        private static Material CreateTintMaterial(string relativePath, Color tint)
        {
            var texture = LoadTexture(relativePath);
            var shader = Shader.Find("MineCraft/BlockUnlit") ?? Shader.Find("Universal Render Pipeline/Unlit");
            var mat = new Material(shader) { name = $"Mat_{System.IO.Path.GetFileNameWithoutExtension(relativePath)}" };
            SetTexture(mat, texture);
            SetColor(mat, Color.white);
            return mat;
        }

        private static Material CreateWaterMaterial(string relativePath)
        {
            var texture = LoadTexture(relativePath);
            var shader = Shader.Find("MineCraft/Water") ?? Shader.Find("Universal Render Pipeline/Unlit");
            var mat = new Material(shader) { name = "Mat_Water" };
            SetTexture(mat, texture);
            SetColor(mat, WaterMaterialTint);
            if (mat.HasProperty("_FrameCount"))
            {
                mat.SetFloat("_FrameCount", 32f);
            }

            if (mat.HasProperty("_FrameTime"))
            {
                mat.SetFloat("_FrameTime", 1f);
            }

            if (mat.HasProperty("_TickRate"))
            {
                mat.SetFloat("_TickRate", 20f);
            }

            mat.renderQueue = 3000;
            return mat;
        }

        private static Material CreateOverlayMaterial(string relativePath, Color grassTint)
        {
            var texture = LoadTexture(relativePath);
            var shader = Shader.Find("MineCraft/GrassSideOverlay") ?? Shader.Find("Universal Render Pipeline/Unlit");
            var mat = new Material(shader) { name = "Mat_GrassSideOverlay" };
            if (mat.HasProperty("_OverlayMap"))
            {
                mat.SetTexture("_OverlayMap", texture);
                mat.SetColor("_GrassTint", grassTint);
            }
            else
            {
                SetTexture(mat, texture);
                SetColor(mat, grassTint);
            }

            return mat;
        }

        private static Material CreateCutoutMaterial(string relativePath, Color tint)
        {
            var texture = LoadTexture(relativePath);
            var shader = Shader.Find("MineCraft/BlockCutout") ?? Shader.Find("Universal Render Pipeline/Unlit");
            var mat = new Material(shader) { name = $"Mat_{System.IO.Path.GetFileNameWithoutExtension(relativePath)}" };
            SetTexture(mat, texture);
            SetColor(mat, tint);
            if (mat.HasProperty("_Cutoff"))
            {
                mat.SetFloat("_Cutoff", 0.5f);
            }

            mat.renderQueue = 2450;
            return mat;
        }

        private static Texture2D LoadTexture(string relativePath)
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>($"{TextureRoot}/{relativePath}");
#else
            return Resources.Load<Texture2D>($"Blocks/{relativePath.Replace(".png", "")}");
#endif
        }

        private static void SetTexture(Material mat, Texture2D tex)
        {
            if (mat.HasProperty("_BaseMap"))
            {
                mat.SetTexture("_BaseMap", tex);
            }
            else
            {
                mat.mainTexture = tex;
            }
        }

        private static void SetColor(Material mat, Color color)
        {
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", color);
            }
            else
            {
                mat.color = color;
            }
        }
    }
}
