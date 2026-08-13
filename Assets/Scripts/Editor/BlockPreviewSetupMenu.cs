using MineCraftUnity.Blocks;
using MineCraftUnity.Rendering;
using UnityEditor;
using UnityEngine;

namespace MineCraftUnity.Editor
{
    /// <summary>
    /// Editor tools to configure block textures and spawn preview cubes in the scene.
    /// </summary>
    public static class BlockPreviewSetupMenu
    {
        private const string GalleryRootName = "BlockPreviewGallery";
        private const string TextureRoot = "Assets/Minecraft/Blocks";

        private static readonly (BlockId id, string label, float offsetX)[] GalleryBlocks =
        {
            (BlockId.Stone, "Preview_Stone", 0f),
            (BlockId.Dirt, "Preview_Dirt", 1.5f),
            (BlockId.Sand, "Preview_Sand", 3f),
            (BlockId.Water, "Preview_Water", 4.5f),
            (BlockId.Bedrock, "Preview_Bedrock", 6f),
            (BlockId.GrassBlock, "Preview_GrassBlock", 7.5f),
            (BlockId.Gravel, "Preview_Gravel", 9f)
        };

        [MenuItem("MineCraft/Setup/Configure All Block Textures")]
        public static void ConfigureAllBlockTextures()
        {
            var count = 0;
            foreach (var path in GetAllTexturePaths())
            {
                if (ConfigureTexture(path))
                {
                    count++;
                }
            }

            BlockMaterialLibrary.Reset();
            RebuildAllPreviews();
            Debug.Log($"[MineCraft] Configured {count} block textures (Point filter, uncompressed).");
        }

        [MenuItem("MineCraft/Setup/Create Terrain Block Previews")]
        public static void CreateTerrainBlockPreviews()
        {
            ConfigureAllBlockTextures();

            var gallery = GameObject.Find(GalleryRootName);
            if (gallery == null)
            {
                gallery = new GameObject(GalleryRootName);
                gallery.transform.position = Vector3.zero;
            }

            foreach (var (id, name, offsetX) in GalleryBlocks)
            {
                CreateOrUpdatePreview(gallery.transform, id, name, offsetX);
            }

            Selection.activeGameObject = gallery;
            Debug.Log("[MineCraft] Terrain block previews created. Each cube uses the same textures as the overworld.");
        }

        [MenuItem("MineCraft/Setup/Rebuild Block Previews")]
        public static void RebuildAllPreviews()
        {
            var gallery = GameObject.Find(GalleryRootName);
            if (gallery != null)
            {
                foreach (var preview in gallery.GetComponentsInChildren<CubeBlockPreview>(true))
                {
                    preview.Rebuild();
                }

                foreach (var grass in gallery.GetComponentsInChildren<GrassBlockPreview>(true))
                {
                    grass.Rebuild();
                }
            }

            var grassStandalone = GameObject.Find("GrassBlockPreview");
            grassStandalone?.GetComponent<GrassBlockPreview>()?.Rebuild();
        }

        internal static string[] GetAllTexturePaths()
        {
            return new[]
            {
                $"{TextureRoot}/stone/Textures/stone.png",
                $"{TextureRoot}/dirt/Textures/dirt.png",
                $"{TextureRoot}/sand/Textures/sand.png",
                $"{TextureRoot}/water/Textures/water_still.png",
                $"{TextureRoot}/bedrock/Textures/bedrock.png",
                $"{TextureRoot}/gravel/Textures/gravel.png",
                $"{TextureRoot}/water/Textures/water_flow.png",
                $"{TextureRoot}/grass_block/Textures/grass_block_top.png",
                $"{TextureRoot}/grass_block/Textures/grass_block_side.png",
                $"{TextureRoot}/grass_block/Textures/grass_block_side_overlay.png",
                $"{TextureRoot}/grass_block/Textures/dirt.png"
            };
        }

        internal static bool ConfigureTexture(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning($"Missing texture: {path}");
                return false;
            }

            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = false;
            importer.SaveAndReimport();
            return true;
        }

        private static void CreateOrUpdatePreview(Transform parent, BlockId id, string objectName, float offsetX)
        {
            var existing = parent.Find(objectName);
            if (existing != null)
            {
                if (id == BlockId.GrassBlock)
                {
                    existing.GetComponent<GrassBlockPreview>()?.Rebuild();
                }
                else
                {
                    var cube = existing.GetComponent<CubeBlockPreview>();
                    if (cube != null)
                    {
                        cube.BlockType = id;
                    }
                }

                existing.localPosition = new Vector3(offsetX, 0.5f, 0f);
                return;
            }

            var go = new GameObject(objectName);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(offsetX, 0.5f, 0f);
            go.AddComponent<MeshFilter>();
            go.AddComponent<MeshRenderer>();

            if (id == BlockId.GrassBlock)
            {
                go.AddComponent<GrassBlockPreview>().Rebuild();
                return;
            }

            var preview = go.AddComponent<CubeBlockPreview>();
            preview.BlockType = id;
        }
    }
}
