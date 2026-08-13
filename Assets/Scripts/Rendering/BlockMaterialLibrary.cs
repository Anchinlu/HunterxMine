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
        private static readonly Color PlainsGrassTint = new(145f / 255f, 189f / 255f, 89f / 255f, 1f);
        private static readonly Color WaterTint = new(0.25f, 0.45f, 0.85f, 0.75f);

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
            _materials[(int)ChunkMeshLayer.GrassTop] = CreateTintMaterial("grass_block/Textures/grass_block_top.png", PlainsGrassTint);
            _materials[(int)ChunkMeshLayer.GrassBottom] = CreateCubeMaterial("grass_block/Textures/dirt.png");
            _materials[(int)ChunkMeshLayer.GrassSide] = CreateCubeMaterial("grass_block/Textures/grass_block_side.png");
            _materials[(int)ChunkMeshLayer.GrassOverlay] = CreateOverlayMaterial("grass_block/Textures/grass_block_side_overlay.png", PlainsGrassTint);
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
            SetColor(mat, tint);
            return mat;
        }

        private static Material CreateWaterMaterial(string relativePath)
        {
            var texture = LoadTexture(relativePath);
            var shader = Shader.Find("MineCraft/BlockUnlit") ?? Shader.Find("Universal Render Pipeline/Unlit");
            var mat = new Material(shader) { name = "Mat_Water" };
            SetTexture(mat, texture);
            SetColor(mat, WaterTint);
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
