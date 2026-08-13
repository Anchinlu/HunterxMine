using MineCraftUnity.Blocks;
using UnityEngine;

namespace MineCraftUnity.Rendering
{
    /// <summary>
    /// Preview one grass block cube with vanilla textures from minecraft-assets-26.2.
    /// MC ref: assets/minecraft/models/block/grass_block.json
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class GrassBlockPreview : MonoBehaviour
    {
        private const string TextureRoot = "Assets/Minecraft/Blocks/grass_block/Textures";

        [SerializeField] private GrassBlockTextureSet textureSet;

        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private Material[] _materials;

        private void OnEnable() => Rebuild();

        private void OnValidate() => Rebuild();

        private void OnDestroy() => CleanupMaterials();

        public void Rebuild()
        {
            EnsureComponents();
            EnsureTextureSet();
            if (textureSet == null || textureSet.Top == null || textureSet.Bottom == null ||
                textureSet.Side == null || textureSet.SideOverlay == null)
            {
                return;
            }

            ConfigureTextureImportSettings();
            CleanupMaterials();

            _meshFilter.sharedMesh = TexturedBlockMeshBuilder.BuildGrassBlockSubmeshes();
            _materials = BuildMaterials();
            _meshRenderer.sharedMaterials = _materials;
            EnsureCollider();
        }

        private Material[] BuildMaterials() =>
            new[]
            {
                CreateTintMaterial(textureSet.Top, textureSet.GrassTint),
                CreateTintMaterial(textureSet.Bottom, Color.white),
                CreateTintMaterial(textureSet.Side, Color.white),
                CreateSideOverlayMaterial(textureSet.SideOverlay, textureSet.GrassTint)
            };

        private Material CreateTintMaterial(Texture2D texture, Color tint)
        {
            var shader = Shader.Find("MineCraft/BlockUnlit")
                ?? Shader.Find("Universal Render Pipeline/Unlit");
            var mat = new Material(shader) { name = $"Mat_{texture.name}" };
            SetTexture(mat, texture);
            SetColor(mat, tint);
            return mat;
        }

        private static Material CreateSideOverlayMaterial(Texture2D overlay, Color grassTint)
        {
            var shader = Shader.Find("MineCraft/GrassSideOverlay")
                ?? Shader.Find("Universal Render Pipeline/Unlit");
            var mat = new Material(shader) { name = "Mat_GrassSideOverlay" };
            if (mat.HasProperty("_OverlayMap"))
            {
                mat.SetTexture("_OverlayMap", overlay);
                mat.SetColor("_GrassTint", grassTint);
            }
            else
            {
                mat.mainTexture = overlay;
                mat.color = grassTint;
            }

            return mat;
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

        private void EnsureCollider()
        {
            var collider = GetComponent<MeshCollider>();
            if (collider == null)
            {
                collider = gameObject.AddComponent<MeshCollider>();
            }

            collider.sharedMesh = _meshFilter.sharedMesh;
        }

        private void EnsureComponents()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
        }

        private void EnsureTextureSet()
        {
            if (textureSet != null && textureSet.Top != null && textureSet.SideOverlay != null)
            {
                return;
            }

#if UNITY_EDITOR
            textureSet = ScriptableObject.CreateInstance<GrassBlockTextureSet>();
            textureSet.Top = LoadEditorTexture("grass_block_top.png");
            textureSet.Bottom = LoadEditorTexture("dirt.png");
            textureSet.Side = LoadEditorTexture("grass_block_side.png");
            textureSet.SideOverlay = LoadEditorTexture("grass_block_side_overlay.png");
#endif
        }

        private void ConfigureTextureImportSettings()
        {
#if UNITY_EDITOR
            var changedAny = false;
            foreach (var file in new[] { "grass_block_top.png", "grass_block_side.png", "grass_block_side_overlay.png", "dirt.png" })
            {
                var path = $"{TextureRoot}/{file}";
                var importer = UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.TextureImporter;
                if (importer == null)
                {
                    continue;
                }

                var changed = false;
                if (importer.filterMode != FilterMode.Point)
                {
                    importer.filterMode = FilterMode.Point;
                    changed = true;
                }

                if (importer.mipmapEnabled)
                {
                    importer.mipmapEnabled = false;
                    changed = true;
                }

                if (importer.textureCompression != UnityEditor.TextureImporterCompression.Uncompressed)
                {
                    importer.textureCompression = UnityEditor.TextureImporterCompression.Uncompressed;
                    changed = true;
                }

                if (file.Contains("overlay") && importer.alphaIsTransparency)
                {
                    importer.alphaIsTransparency = false;
                    changed = true;
                }

                if (changed)
                {
                    importer.SaveAndReimport();
                    changedAny = true;
                }
            }

            if (changedAny && textureSet != null)
            {
                textureSet.Top = LoadEditorTexture("grass_block_top.png");
                textureSet.Bottom = LoadEditorTexture("dirt.png");
                textureSet.Side = LoadEditorTexture("grass_block_side.png");
                textureSet.SideOverlay = LoadEditorTexture("grass_block_side_overlay.png");
            }
#endif
        }

        private void CleanupMaterials()
        {
            if (_materials == null)
            {
                return;
            }

            foreach (var mat in _materials)
            {
                if (mat == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(mat);
                }
                else
                {
                    DestroyImmediate(mat);
                }
            }

            _materials = null;
        }

#if UNITY_EDITOR
        private static Texture2D LoadEditorTexture(string fileName)
        {
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>($"{TextureRoot}/{fileName}");
        }

        [ContextMenu("Configure Texture Import (Point Filter)")]
        private void ConfigureTextureImport()
        {
            ConfigureTextureImportSettings();
            Rebuild();
            Debug.Log("[GrassBlockPreview] Textures configured and block rebuilt.");
        }
#endif
    }
}
