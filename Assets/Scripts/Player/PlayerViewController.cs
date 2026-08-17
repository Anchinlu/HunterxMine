using UnityEngine;
using UnityEngine.InputSystem;
using MineCraftUnity.UI;

namespace MineCraftUnity.Player
{
    /// <summary>
    /// Controls player camera view modes (First Person vs. Third Person)
    /// and handles camera collision to prevent clipping into terrain blocks.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerViewController : MonoBehaviour
    {
        [Header("View Settings")]
        [SerializeField] private bool startsFirstPerson = true;
        [SerializeField] private float thirdPersonDistance = 4f;
        [SerializeField] private float thirdPersonHeight = 0.15f;
        [SerializeField] private float minDistance = 0.8f;

        private Transform _cameraRoot;
        private Camera _camera;
        private PlayerVisualController _visualController;

        private bool _isFirstPerson;
        private bool _isStatusPanelInteracting;
        public bool IsStatusPanelInteracting => _isStatusPanelInteracting;
        public bool IsStatusPanelVisible => _statusPanel != null && _statusPanel.IsVisible;

        // Character Status Panel
        [Header("UI Prefabs")]
        [SerializeField] private GameObject characterStatusPanelPrefab;

        private CharacterStatusPanel _statusPanel;

        private void Start()
        {
            _isFirstPerson = startsFirstPerson;
            
            ResolveReferences();
            ApplyViewMode();
        }

        private void ResolveReferences()
        {
            // Find CameraRoot
            _cameraRoot = transform.Find("CameraRoot");
            if (_cameraRoot != null)
            {
                _camera = _cameraRoot.GetComponentInChildren<Camera>(true);
            }

            // Find PlayerVisualController
            var visualRoot = transform.Find(PlayerVisualBuilder.VisualRootName);
            if (visualRoot != null)
            {
                _visualController = visualRoot.GetComponent<PlayerVisualController>();
            }
        }

        private void Update()
        {
            if (Keyboard.current == null) return;

            // F5 or V to toggle view mode
            if (Keyboard.current.f5Key.wasPressedThisFrame || Keyboard.current.vKey.wasPressedThisFrame)
            {
                ToggleView();
            }

            // C to toggle character status panel (first-person only)
            if (Keyboard.current.cKey.wasPressedThisFrame && _isFirstPerson
                && !ChatCommandOverlay.IsOpen)
            {
                ToggleStatusPanel();
            }

            if (_statusPanel != null && _statusPanel.IsVisible && Mouse.current.middleButton.wasPressedThisFrame)
            {
                ToggleInteraction();
            }

            if (_isStatusPanelInteracting && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                ToggleInteraction();
            }
        }

        private void ToggleInteraction()
        {
            _isStatusPanelInteracting = !_isStatusPanelInteracting;
            if (_isStatusPanelInteracting)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
            else
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }

        private void LateUpdate()
        {
            if (_camera == null || _cameraRoot == null)
            {
                ResolveReferences();
                if (_camera == null || _cameraRoot == null) return;
            }

            if (_isFirstPerson)
            {
                // Reset camera to eye position in First Person
                _camera.transform.localPosition = Vector3.zero;
                _camera.transform.localRotation = Quaternion.identity;
            }
            else
            {
                // Handle Third Person camera with collision detection
                Vector3 eyePos = _cameraRoot.position;
                Vector3 targetLocalPos = new Vector3(0f, thirdPersonHeight, -thirdPersonDistance);
                Vector3 targetWorldPos = _cameraRoot.TransformPoint(targetLocalPos);

                Vector3 toCamera = targetWorldPos - eyePos;
                float maxDist = toCamera.magnitude;
                Vector3 dir = toCamera.normalized;

                // Adjust distance if there are block collisions
                float adjustedDist = GetAdjustedDistance(eyePos, dir, maxDist);

                // Set camera position and rotation in world space
                _camera.transform.position = eyePos + dir * adjustedDist;
                _camera.transform.rotation = _cameraRoot.rotation;
            }
        }

        private float GetAdjustedDistance(Vector3 eyePos, Vector3 direction, float maxDist)
        {
            var hits = Physics.RaycastAll(eyePos, direction, maxDist);
            float nearestDist = maxDist;

            foreach (var hit in hits)
            {
                // Ignore Player components
                if (hit.transform.root == transform.root)
                {
                    continue;
                }

                // Ignore trigger colliders
                if (hit.collider.isTrigger)
                {
                    continue;
                }

                if (hit.distance < nearestDist)
                {
                    nearestDist = hit.distance;
                }
            }

            // Push camera slightly forward from the hit point to prevent clipping
            return Mathf.Max(minDistance, nearestDist - 0.15f);
        }

        public void ToggleView()
        {
            _isFirstPerson = !_isFirstPerson;
            ApplyViewMode();
        }

        private void ApplyViewMode()
        {
            if (_visualController == null)
            {
                ResolveReferences();
            }

            if (_visualController != null)
            {
                _visualController.SetFirstPerson(_isFirstPerson);
            }
        }

        // ─── Character Status Panel ───

        private void EnsureStatusPanel()
        {
            if (_visualController == null) ResolveReferences();
            if (_visualController == null) return;

            var visualRoot = _visualController.transform;
            
            // 1. Find the socket using the explicit path
            Transform panelSocket = visualRoot.Find(
                "RootCombatPivot/UpperBodyPivot/LeftShoulderPivot/LeftElbowPivot/LeftHandSocket/CharacterStatusPanelSocket");

            if (panelSocket == null)
            {
                // Fallback for legacy scenes
                Debug.LogWarning("[PlayerViewController] Explicit panel socket path not found. Falling back to deep scan.");
                Transform handSocket = null;
                foreach (var t in visualRoot.GetComponentsInChildren<Transform>())
                {
                    if (t.name == "LeftHandSocket")
                    {
                        handSocket = t;
                        break;
                    }
                }
                
                if (handSocket == null)
                {
                    Debug.LogWarning("[PlayerViewController] LeftHandSocket not found. Cannot create Character Status Panel.");
                    return;
                }

                panelSocket = handSocket.Find("CharacterStatusPanelSocket");
                if (panelSocket == null)
                {
                    Debug.LogWarning("[PlayerViewController] CharacterStatusPanelSocket not found under LeftHandSocket. Creating dynamically.");
                    var socketGo = new GameObject("CharacterStatusPanelSocket");
                    socketGo.transform.SetParent(handSocket, false);
                    socketGo.transform.localPosition = Vector3.zero;
                    socketGo.transform.localRotation = Quaternion.identity;
                    panelSocket = socketGo.transform;
                }
            }

            // 2. Prevent duplicate panels
            Transform existingPanel = panelSocket.Find("CharacterStatusPanel");
            if (existingPanel != null)
            {
                _statusPanel = existingPanel.GetComponentInChildren<CharacterStatusPanel>(true);
                if (_statusPanel != null) return;
                
                // Exists but missing component, destroy and recreate
                Destroy(existingPanel.gameObject);
            }

            // 3. Instantiate the panel
            if (characterStatusPanelPrefab != null)
            {
                var panelGo = Instantiate(characterStatusPanelPrefab, panelSocket, false);
                panelGo.name = "CharacterStatusPanel";
                
                // Panel should be exactly at the socket with no extra offsets
                // (Intentionally removed setting localPosition and localRotation to zero to preserve prefab layout)
                
                _statusPanel = panelGo.GetComponent<CharacterStatusPanel>();
                if (_statusPanel == null)
                {
                    _statusPanel = panelGo.GetComponentInChildren<CharacterStatusPanel>(true);
                }

                if (_statusPanel == null)
                {
                    Debug.LogError("[PlayerViewController] CharacterStatusPanel not found in prefab hierarchy.");
                    Destroy(panelGo);
                    return;
                }
            }
            else
            {
                // Create panel object manually
                var panelGo = new GameObject("CharacterStatusPanel");
                panelGo.transform.SetParent(panelSocket, false);
                panelGo.transform.localPosition = Vector3.zero;
                panelGo.transform.localRotation = Quaternion.identity;

                var builder = panelGo.AddComponent<CharacterStatusPanelBuilder>();
                _statusPanel = panelGo.GetComponent<CharacterStatusPanel>();
                if (_statusPanel == null)
                    _statusPanel = panelGo.AddComponent<CharacterStatusPanel>();
            }

            // Initialize with player stats
            var stats = GetComponent<PlayerStats>();
            if (stats != null)
            {
                _statusPanel.Initialize(stats);
            }
        }

        private void ToggleStatusPanel()
        {
            EnsureStatusPanel();
            if (_statusPanel == null) return;

            _statusPanel.Toggle();

            if (!_statusPanel.IsVisible && _isStatusPanelInteracting)
            {
                ToggleInteraction();
            }

            // Toggle left-arm override in Locomotion Animator
            var loco = GetComponentInChildren<PlayerLocomotionAnimator>();
            if (loco != null)
            {
                loco.IsCharacterPanelOpen = _statusPanel.IsVisible;
            }
        }
    }
}
