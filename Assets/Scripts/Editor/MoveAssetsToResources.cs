#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MineCraftUnity.Editor
{
    /// <summary>
    /// One-click menu to move runtime assets into Assets/Resources/ so they
    /// can be loaded via Resources.Load in standalone builds.
    ///
    /// ⚠️ Run this ONCE then delete this file — it is not needed after the move.
    ///
    /// Menu: MineCraft → Setup → Move Assets to Resources
    /// </summary>
    public static class MoveAssetsToResources
    {
        [MenuItem("MineCraft/Setup/Move Assets to Resources (Run Once)")]
        public static void Execute()
        {
            if (!EditorUtility.DisplayDialog(
                "Move Assets to Resources",
                "This will:\n\n" +
                "1. Move Assets/Minecraft/Blocks → Assets/Resources/Blocks\n" +
                "2. Move Assets/Minecraft/Environment/Celestial → Assets/Resources/Environment/Celestial\n" +
                "3. Copy Docs/.../worldgen/biome/*.json → Assets/Resources/WorldGen/biome/\n" +
                "4. Copy Docs/.../worldgen/noise/*.json → Assets/Resources/WorldGen/noise/\n\n" +
                "All GUIDs will be preserved. Continue?",
                "Yes, Move", "Cancel"))
            {
                return;
            }

            int moved = 0;
            int copied = 0;

            // ─── 1. Move block textures ───
            moved += MoveAssetFolder("Assets/Minecraft/Blocks", "Assets/Resources/Blocks");

            // ─── 2. Move celestial textures ───
            EnsureFolder("Assets/Resources/Environment");
            moved += MoveAssetFolder("Assets/Minecraft/Environment/Celestial", "Assets/Resources/Environment/Celestial");

            // ─── 3. Copy biome datapack JSONs ───
            copied += CopyDatapackJsons(
                "Docs/tham khảo/minecraft-assets-26.2/minecraft-assets-26.2/data/minecraft/worldgen/biome",
                "Assets/Resources/WorldGen/biome");

            // ─── 4. Copy noise datapack JSONs ───
            copied += CopyDatapackJsons(
                "Docs/tham khảo/minecraft-assets-26.2/minecraft-assets-26.2/data/minecraft/worldgen/noise",
                "Assets/Resources/WorldGen/noise");

            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Done",
                $"Moved {moved} asset(s).\nCopied {copied} datapack JSON(s) into Resources.\n\n" +
                "You can now delete this script (MoveAssetsToResources.cs).\n" +
                "Enter Play Mode to verify everything loads correctly.",
                "OK");

            Debug.Log($"[MoveAssetsToResources] Completed: {moved} moved, {copied} copied.");
        }

        /// <summary>
        /// Move an entire folder using AssetDatabase.MoveAsset (preserves GUIDs).
        /// </summary>
        private static int MoveAssetFolder(string source, string destination)
        {
            if (!AssetDatabase.IsValidFolder(source))
            {
                Debug.LogWarning($"[MoveAssets] Source folder not found, skipping: {source}");
                return 0;
            }

            if (AssetDatabase.IsValidFolder(destination))
            {
                Debug.LogWarning($"[MoveAssets] Destination already exists, skipping: {destination}");
                return 0;
            }

            // Ensure parent folder exists
            var destParent = Path.GetDirectoryName(destination)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(destParent))
            {
                EnsureFolder(destParent);
            }

            var result = AssetDatabase.MoveAsset(source, destination);
            if (string.IsNullOrEmpty(result))
            {
                Debug.Log($"[MoveAssets] ✅ Moved: {source} → {destination}");
                return 1;
            }

            Debug.LogError($"[MoveAssets] ❌ Failed to move {source} → {destination}: {result}");
            return 0;
        }

        /// <summary>
        /// Copy JSON files from a project-level directory into Assets/Resources/.
        /// Unity doesn't support .json as TextAsset directly from Resources.Load,
        /// so we copy with .json extension but Unity will recognize them as TextAsset.
        /// </summary>
        private static int CopyDatapackJsons(string relativeSourceDir, string destinationAssetPath)
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (projectRoot == null)
            {
                Debug.LogError("[MoveAssets] Could not determine project root.");
                return 0;
            }

            var sourceDir = Path.GetFullPath(Path.Combine(projectRoot, relativeSourceDir));
            if (!Directory.Exists(sourceDir))
            {
                Debug.LogWarning($"[MoveAssets] Datapack directory not found, skipping: {sourceDir}");
                return 0;
            }

            // Create destination inside Assets/
            var destFull = Path.GetFullPath(Path.Combine(projectRoot, destinationAssetPath));
            if (!Directory.Exists(destFull))
            {
                Directory.CreateDirectory(destFull);
            }

            int count = 0;
            foreach (var file in Directory.EnumerateFiles(sourceDir, "*.json"))
            {
                var fileName = Path.GetFileName(file);
                var destFile = Path.Combine(destFull, fileName);

                if (File.Exists(destFile))
                {
                    continue; // Already copied
                }

                File.Copy(file, destFile);
                count++;
            }

            if (count > 0)
            {
                Debug.Log($"[MoveAssets] ✅ Copied {count} JSON files → {destinationAssetPath}");
            }

            return count;
        }

        /// <summary>
        /// Recursively create a folder path in the AssetDatabase.
        /// </summary>
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            var folderName = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(folderName))
            {
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }
    }
}
#endif
