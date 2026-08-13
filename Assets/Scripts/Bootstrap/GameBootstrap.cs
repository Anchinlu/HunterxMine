using MineCraftUnity.Core;
using MineCraftUnity.Rendering;
using MineCraftUnity.UI;
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
            var manager = FindFirstObjectByType<ChunkManager>();
            if (manager == null)
            {
                var worldGo = new GameObject("World");
                manager = worldGo.AddComponent<ChunkManager>();
            }

            manager.Configure(worldSeed, viewDistance);

            var player = GameObject.Find("Player");
            if (player != null)
            {
                manager.SetFollowTarget(player.transform);
                RepositionPlayerOnSurface(manager, player.transform);
            }

            RemoveLegacyGround();
            EnsureStatsOverlay(manager.gameObject);
        }

        private static void EnsureStatsOverlay(GameObject worldRoot)
        {
            if (worldRoot.GetComponent<GameStatsOverlay>() != null)
            {
                return;
            }

            worldRoot.AddComponent<GameStatsOverlay>();
        }

        private static void RepositionPlayerOnSurface(ChunkManager manager, Transform player)
        {
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
