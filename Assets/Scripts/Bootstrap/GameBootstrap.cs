using System.Collections;
using MineCraftUnity.Core;
using MineCraftUnity.Rendering;
using MineCraftUnity.UI;
using MineCraftUnity.World;
using MineCraftUnity.Player;
using UnityEngine;

namespace MineCraftUnity.Bootstrap
{
    /// <summary>
    /// MC ref: net.minecraft.client.main.Main — boots procedural overworld around the player.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private int worldSeed = 12345;
        [SerializeField] private int viewDistance = WorldConstants.DefaultViewDistance;

        private void Awake()
        {
            RemoveLegacyGround();

            var manager = FindFirstObjectByType<ChunkManager>();
            if (manager != null)
            {
                DayNightController.EnsureOnWorld(manager.gameObject, 6000);
            }
        }

        private void Start()
        {
            StartCoroutine(BootSequence());
        }

        private IEnumerator BootSequence()
        {
            var manager = FindFirstObjectByType<ChunkManager>();
            if (manager == null)
            {
                var worldGo = new GameObject("World");
                manager = worldGo.AddComponent<ChunkManager>();
            }

            manager.Configure(worldSeed, viewDistance);
            DayNightController.EnsureOnWorld(manager.gameObject, 6000);
            EnsureStatsOverlay(manager.gameObject);
            EnsureChatOverlay(manager.gameObject);
            EnsurePerformanceBaseline(manager.gameObject);

            var player = GameObject.Find("Player");
            if (player != null)
            {
                var playerController = player.GetComponent<PlayerController>();
                if (playerController != null)
                {
                    playerController.enabled = false;
                }

                manager.SetFollowTarget(player.transform);
                while (!manager.IsSpawnAreaReady)
                {
                    yield return null;
                }

                RepositionPlayerOnSurface(manager, player.transform);

                if (playerController != null)
                {
                    playerController.enabled = true;
                }
            }
        }

        private static void EnsureStatsOverlay(GameObject worldRoot)
        {
            if (worldRoot.GetComponent<GameStatsOverlay>() == null)
            {
                worldRoot.AddComponent<GameStatsOverlay>();
            }
        }

        private static void EnsureChatOverlay(GameObject worldRoot)
        {
            if (worldRoot.GetComponent<ChatCommandOverlay>() == null)
            {
                worldRoot.AddComponent<ChatCommandOverlay>();
            }
        }

        private static void EnsurePerformanceBaseline(GameObject worldRoot)
        {
            if (worldRoot.GetComponent<PerformanceBaselineRecorder>() == null)
            {
                worldRoot.AddComponent<PerformanceBaselineRecorder>();
            }
        }

        private static void RepositionPlayerOnSurface(ChunkManager manager, Transform player)
        {
            if (manager.WorldMode == WorldMode.FlatTest)
            {
                player.position = new Vector3(0.5f, 65f, 0.5f);
                return;
            }

            var worldX = Mathf.FloorToInt(player.position.x);
            var worldZ = Mathf.FloorToInt(player.position.z);

            if (!manager.TrySampleTopSolidY(worldX, worldZ, out var surfaceY))
            {
                surfaceY = manager.SampleSurfaceHeight(worldX, worldZ);
            }

            surfaceY = Mathf.Max(surfaceY, WorldConstants.SeaLevel - 4);
            player.position = new Vector3(worldX + 0.5f, surfaceY + 2f, worldZ + 0.5f);
        }

        private static void RemoveLegacyGround()
        {
            var ground = GameObject.Find("Ground");
            if (ground != null)
            {
                Destroy(ground);
            }

            var preview = GameObject.Find("GrassBlockPreview");
            if (preview != null)
            {
                Destroy(preview);
            }
        }
    }
}
