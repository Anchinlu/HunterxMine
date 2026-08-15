using System.IO;
using MineCraftUnity.UI;
using UnityEditor;
using UnityEngine;

namespace MineCraftUnity.Editor
{
    public static class HudSetupWizard
    {
        [MenuItem("MineCraft/Setup/Import HUD Assets")]
        public static void ImportHudAssets()
        {
            var sourceDir = "Docs/tham khảo/minecraft-assets-26.2/minecraft-assets-26.2/assets/minecraft/textures/gui/sprites/hud";
            var destDir = "Assets/Minecraft/Resources/HUD";

            if (!Directory.Exists(sourceDir))
            {
                Debug.LogError($"Source directory not found: {sourceDir}");
                return;
            }

            if (!Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            string[] filesToCopy = {
                "crosshair.png",
                "hotbar.png",
                "hotbar_selection.png",
                "heart/container.png",
                "heart/full.png",
                "heart/half.png",
                "food_empty.png",
                "food_half.png",
                "food_full.png",
                "armor_empty.png",
                "armor_half.png",
                "armor_full.png",
                "experience_bar_background.png",
                "experience_bar_progress.png"
            };

            foreach (var file in filesToCopy)
            {
                var srcPath = Path.Combine(sourceDir, file);
                var destPath = Path.Combine(destDir, Path.GetFileName(file));

                if (File.Exists(srcPath))
                {
                    File.Copy(srcPath, destPath, true);
                }
                else
                {
                    Debug.LogWarning($"Missing source file: {srcPath}");
                }
            }

            AssetDatabase.Refresh();

            var libraryPath = "Assets/Minecraft/Resources/HUD/HudSpriteLibrary.asset";
            var library = AssetDatabase.LoadAssetAtPath<HudSpriteLibrary>(libraryPath);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<HudSpriteLibrary>();
                AssetDatabase.CreateAsset(library, libraryPath);
            }

            foreach (var file in filesToCopy)
            {
                var destPath = $"Assets/Minecraft/Resources/HUD/{Path.GetFileName(file)}";
                var importer = AssetImporter.GetAtPath(destPath) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.filterMode = FilterMode.Point;
                    importer.mipmapEnabled = false;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.wrapMode = TextureWrapMode.Clamp;
                    importer.SaveAndReimport();

                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(destPath);
                    if (sprite != null)
                    {
                        var filename = Path.GetFileName(file);
                        switch (filename)
                        {
                            case "crosshair.png": library.Crosshair = sprite; break;
                            case "hotbar.png": library.Hotbar = sprite; break;
                            case "hotbar_selection.png": library.HotbarSelection = sprite; break;
                            case "container.png": library.HeartContainer = sprite; break;
                            case "full.png": library.HeartFull = sprite; break;
                            case "half.png": library.HeartHalf = sprite; break;
                            case "food_empty.png": library.FoodEmpty = sprite; break;
                            case "food_half.png": library.FoodHalf = sprite; break;
                            case "food_full.png": library.FoodFull = sprite; break;
                            case "armor_empty.png": library.ArmorEmpty = sprite; break;
                            case "armor_half.png": library.ArmorHalf = sprite; break;
                            case "armor_full.png": library.ArmorFull = sprite; break;
                            case "experience_bar_background.png": library.ExperienceBarBackground = sprite; break;
                            case "experience_bar_progress.png": library.ExperienceBarProgress = sprite; break;
                        }
                    }
                }
            }

            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
            Debug.Log("HUD Assets imported and configured successfully.");
        }
    }
}
