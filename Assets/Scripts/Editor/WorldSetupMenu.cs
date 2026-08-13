using MineCraftUnity.Bootstrap;
using MineCraftUnity.Player;
using MineCraftUnity.Rendering;
using MineCraftUnity.UI;
using UnityEditor;
using UnityEngine;

namespace MineCraftUnity.Editor
{
    public static class WorldSetupMenu
    {
        [MenuItem("MineCraft/Setup/Create Overworld")]
        public static void CreateOverworld()
        {
            ConfigureBlockTextures();
            RemoveLegacyObjects();

            var world = GameObject.Find("World");
            if (world == null)
            {
                world = new GameObject("World");
            }

            var manager = world.GetComponent<ChunkManager>();
            if (manager == null)
            {
                manager = world.AddComponent<ChunkManager>();
            }

            var bootstrap = world.GetComponent<GameBootstrap>();
            if (bootstrap == null)
            {
                bootstrap = world.AddComponent<GameBootstrap>();
            }

            if (world.GetComponent<GameStatsOverlay>() == null)
            {
                world.AddComponent<GameStatsOverlay>();
            }

            if (world.GetComponent<PerformanceBaselineRecorder>() == null)
            {
                world.AddComponent<PerformanceBaselineRecorder>();
            }

            DayNightController.EnsureOnWorld(world);

            EnsurePlayer(manager);

            Selection.activeGameObject = world;
            Debug.Log("[MineCraft] Overworld created. Press Play — chunks generate around the player.");
        }

        [MenuItem("MineCraft/Setup/Configure Terrain Block Textures")]
        public static void ConfigureBlockTextures()
        {
            BlockPreviewSetupMenu.ConfigureAllBlockTextures();
        }

        private static void EnsurePlayer(ChunkManager manager)
        {
            PlayerSetupMenu.CreatePlayer();

            var player = GameObject.Find("Player");
            if (player != null)
            {
                manager.SetFollowTarget(player.transform);
            }
        }

        private static void RemoveLegacyObjects()
        {
            var ground = GameObject.Find("Ground");
            if (ground != null)
            {
                Object.DestroyImmediate(ground);
            }

            var preview = GameObject.Find("GrassBlockPreview");
            if (preview != null)
            {
                Object.DestroyImmediate(preview);
            }
        }
    }
}
