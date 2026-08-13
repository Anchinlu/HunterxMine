using MineCraftUnity.Player;
using MineCraftUnity.Rendering;
using UnityEditor;
using UnityEngine;

namespace MineCraftUnity.Editor
{
    public static class PlayerSetupMenu
    {
        private const string PlayerName = "Player";

        [MenuItem("MineCraft/Setup/Create Player")]
        public static void CreatePlayer()
        {
            var player = GameObject.Find(PlayerName);
            if (player == null)
            {
                player = CreatePlayerObject();
            }

            PositionPlayer(player.transform);
            Selection.activeGameObject = player;
            Debug.Log("[MineCraft] Player ready. WASD + mouse, F = fly, Space/Ctrl = up/down while flying, Esc = unlock cursor.");
        }

        private static GameObject CreatePlayerObject()
        {
            var playerGo = new GameObject(PlayerName);
            var controller = playerGo.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.3f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.skinWidth = 0.08f;
            controller.stepOffset = 0.35f;
            controller.minMoveDistance = 0f;

            var player = playerGo.AddComponent<PlayerController>();

            var cameraRoot = new GameObject("CameraRoot").transform;
            cameraRoot.SetParent(playerGo.transform, false);
            cameraRoot.localPosition = new Vector3(0f, 1.62f, 0f);
            player.SetCameraRoot(cameraRoot);

            var camera = Camera.main;
            if (camera != null)
            {
                camera.transform.SetParent(cameraRoot, false);
                camera.transform.localPosition = Vector3.zero;
                camera.transform.localRotation = Quaternion.identity;
                camera.fieldOfView = 70f;
                camera.farClipPlane = 512f;
                camera.clearFlags = CameraClearFlags.Skybox;
                if (camera.GetComponent<UnderwaterCameraEffect>() == null)
                {
                    camera.gameObject.AddComponent<UnderwaterCameraEffect>();
                }
            }
            else
            {
                var cameraGo = new GameObject("Main Camera");
                cameraGo.tag = "MainCamera";
                cameraGo.transform.SetParent(cameraRoot, false);
                camera = cameraGo.AddComponent<Camera>();
                cameraGo.AddComponent<AudioListener>();
                cameraGo.AddComponent<UnderwaterCameraEffect>();
                camera.fieldOfView = 70f;
            }

            return playerGo;
        }

        private static void PositionPlayer(Transform player)
        {
            var manager = Object.FindFirstObjectByType<ChunkManager>();
            if (manager != null)
            {
                var worldX = Mathf.FloorToInt(player.position.x);
                var worldZ = Mathf.FloorToInt(player.position.z);
                var surfaceY = manager.SampleSurfaceHeight(worldX, worldZ);
                player.position = new Vector3(worldX + 0.5f, surfaceY + 2f, worldZ + 0.5f);
            }
            else
            {
                player.position = new Vector3(0.5f, 80f, -2.5f);
            }

            player.rotation = Quaternion.Euler(0f, 0f, 0f);
        }
    }
}
