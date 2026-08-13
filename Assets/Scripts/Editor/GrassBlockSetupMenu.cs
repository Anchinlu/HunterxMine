using MineCraftUnity.Rendering;
using UnityEditor;
using UnityEngine;

namespace MineCraftUnity.Editor
{
    public static class GrassBlockSetupMenu
    {
        private const string SceneObjectName = "GrassBlockPreview";
        private const string TextureRoot = "Assets/Minecraft/Blocks/grass_block/Textures";

        [MenuItem("MineCraft/Setup/Create Grass Block Preview")]
        public static void CreateGrassBlockPreview()
        {
            ConfigureAllTextures();

            var existing = GameObject.Find(SceneObjectName);
            if (existing != null)
            {
                Selection.activeGameObject = existing;
                existing.GetComponent<GrassBlockPreview>()?.Rebuild();
                return;
            }

            var go = new GameObject(SceneObjectName);
            go.AddComponent<MeshFilter>();
            go.AddComponent<MeshRenderer>();
            go.AddComponent<GrassBlockPreview>();
            go.transform.position = Vector3.zero;

            Selection.activeGameObject = go;
            Debug.Log("[MineCraft] Grass block preview created. Press Play or stay in Edit mode to view.");
        }

        [MenuItem("MineCraft/Setup/Configure Grass Block Textures")]
        public static void ConfigureAllTextures()
        {
            BlockPreviewSetupMenu.ConfigureAllBlockTextures();
        }
    }
}
