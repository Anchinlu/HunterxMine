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

        [Header("HUD")]
        [Tooltip("Kéo PlayerHud Prefab vào đây. Nếu để trống, sẽ tìm trong Scene hoặc tự tạo mới.")]
        [SerializeField] private PlayerHud hudPrefab;

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
            GameObject hudObj = null;

            if (player != null)
            {
                PlayerVisualBootstrap.EnsurePlayerVisual(player.transform);

                var playerController = player.GetComponent<PlayerController>();
                if (playerController != null)
                {
                    playerController.enabled = false;
                }

                var stats = player.GetComponent<MineCraftUnity.Player.PlayerStats>();
                if (stats == null) stats = player.AddComponent<MineCraftUnity.Player.PlayerStats>();
                
                var levelSys = player.GetComponent<MineCraftUnity.Player.PlayerLevelSystem>();
                if (levelSys == null) levelSys = player.AddComponent<MineCraftUnity.Player.PlayerLevelSystem>();

                var defaultClass = Resources.Load<MineCraftUnity.Player.CharacterClassDefinition>($"CharacterClasses/{stats.CurrentClass}");
                if (defaultClass != null)
                {
                    levelSys.Initialize(defaultClass);
                }
                else
                {
                    Debug.LogWarning($"[Bootstrap] Could not load default class definition for {stats.CurrentClass}");
                }
                
                var inventory = player.GetComponent<MineCraftUnity.Player.PlayerInventory>();
                if (inventory == null) inventory = player.AddComponent<MineCraftUnity.Player.PlayerInventory>();

                // Priority: 1) Prefab  2) Already in Scene  3) Auto-create
                PlayerHud hud = null;
                if (hudPrefab != null)
                {
                    hud = Instantiate(hudPrefab);
                    hud.name = "PlayerHud";
                    hudObj = hud.gameObject;
                }
                else
                {
                    hud = FindFirstObjectByType<PlayerHud>();
                    if (hud != null)
                    {
                        hudObj = hud.gameObject;
                    }
                    else
                    {
                        hudObj = new GameObject("PlayerHud");
                        hud = hudObj.AddComponent<PlayerHud>();
                    }
                }

                hudObj.SetActive(false); // Hide until world is ready
                var library = Resources.Load<MineCraftUnity.UI.HudSpriteLibrary>("HUD/HudSpriteLibrary");
                if (library != null)
                {
                    hud.Initialize(library, stats, inventory);
                }

                manager.SetFollowTarget(player.transform);
                while (!manager.IsSpawnAreaReady)
                {
                    yield return null;
                }

                RepositionPlayerOnSurface(manager, player.transform);

                var playerChunkPos = new ChunkPos(Mathf.FloorToInt(player.transform.position.x) >> 4, Mathf.FloorToInt(player.transform.position.z) >> 4);
                while (!manager.ForceApplyCollision(playerChunkPos))
                {
                    yield return null;
                }

                if (playerController != null)
                {
                    playerController.enabled = true;
                }

                if (hudObj != null)
                {
                    hudObj.SetActive(true);
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
