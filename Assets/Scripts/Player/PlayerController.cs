using MineCraftUnity.Core;
using MineCraftUnity.Rendering;
using MineCraftUnity.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MineCraftUnity.Player
{
    /// <summary>
    /// MC ref: net.minecraft.client.player.LocalPlayer (movement + look)
    /// First-person CharacterController movement using Unity Input System.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 4.3f;
        [SerializeField] private float sprintSpeed = 5.6f;
        [SerializeField] private float jumpHeight = 1.25f;
        [SerializeField] private float gravity = -20f;

        [Header("Water")]
        [SerializeField] private float swimSpeed = 2.2f;
        [SerializeField] private float swimSprintSpeed = 3.4f;
        [SerializeField] private float waterGravity = -6f;
        [SerializeField] private float swimAscendSpeed = 3.5f;
        [SerializeField] private ChunkManager chunkManager;

        [Header("Fly")]
        [SerializeField] private float flySpeed = 8f;
        [SerializeField] private float flySprintMultiplier = 2f;

        [Header("Look")]
        [SerializeField] private float mouseSensitivity = 0.15f;
        [SerializeField] private float minPitch = -89f;
        [SerializeField] private float maxPitch = 89f;
        [SerializeField] private Transform cameraRoot;

        [Header("Cursor")]
        [SerializeField] private bool lockCursorOnStart = true;

        private CharacterController _controller;
        private float _verticalVelocity;
        private float _cameraPitch;
        private bool _isFlying;

        public Vector3 Position => transform.position;
        public bool IsFlying => _isFlying;
        public bool IsInWater { get; private set; }
        public bool IsHeadUnderwater { get; private set; }

        public bool IsGrounded => _controller != null && _controller.isGrounded;
        public Vector3 CurrentVelocity { get; private set; }
        public float HorizontalSpeed => new Vector3(CurrentVelocity.x, 0f, CurrentVelocity.z).magnitude;
        public bool IsSprinting { get; private set; }

        public float WalkSpeed => walkSpeed;
        public float SprintSpeed => sprintSpeed;

        public Vector2 MoveInput { get; private set; }
        public bool IsMoving => MoveInput.sqrMagnitude > 0.001f;
        public bool IsMovingBackward => MoveInput.y < -0.1f;
        public bool IsStrafing => Mathf.Abs(MoveInput.x) > 0.1f;

        public float CameraPitch => _cameraPitch;
        public float YawVelocity { get; private set; }

        public Vector3 LocalMoveDirection
        {
            get
            {
                Vector3 worldDirection = CurrentVelocity;
                worldDirection.y = 0f;
                return transform.InverseTransformDirection(worldDirection);
            }
        }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            
            if (GetComponent<MineCraftUnity.Player.Combat.CombatController>() == null)
            {
                gameObject.AddComponent<MineCraftUnity.Player.Combat.CombatController>();
            }

            ResolveCameraRoot();
            EnsureUnderwaterEffect();
            if (chunkManager == null)
            {
                chunkManager = FindFirstObjectByType<ChunkManager>();
            }
        }

        private void EnsureUnderwaterEffect()
        {
            if (cameraRoot == null)
            {
                return;
            }

            var camera = cameraRoot.GetComponentInChildren<Camera>();
            if (camera == null)
            {
                return;
            }

            if (camera.GetComponent<UnderwaterCameraEffect>() == null)
            {
                var effect = camera.gameObject.AddComponent<UnderwaterCameraEffect>();
                effect.enabled = true;
            }
        }

        private float _previousYaw;

        private void Start()
        {
            if (lockCursorOnStart)
            {
                LockCursor();
            }
            _previousYaw = transform.eulerAngles.y;
        }

        private void Update()
        {
            float currentYaw = transform.eulerAngles.y;
            YawVelocity = Mathf.DeltaAngle(_previousYaw, currentYaw) / Mathf.Max(Time.deltaTime, 0.001f);
            _previousYaw = currentYaw;

            if (_controller != null && !_controller.enabled)
            {
                MoveInput = Vector2.zero;
                CurrentVelocity = Vector3.zero;
                IsSprinting = false;
                return;
            }

            UpdateWaterState();

            if (Keyboard.current != null
                && Keyboard.current.escapeKey.wasPressedThisFrame
                && !MineCraftUnity.UI.ChatCommandOverlay.IsOpen)
            {
                UnlockCursor();
            }

            bool isInteractingWithPanel = false;
            if (TryGetComponent<PlayerViewController>(out var vc))
            {
                isInteractingWithPanel = vc.IsStatusPanelInteracting;
            }

            if (Mouse.current != null
                && Mouse.current.leftButton.wasPressedThisFrame
                && Cursor.lockState != CursorLockMode.Locked
                && !MineCraftUnity.UI.ChatCommandOverlay.IsOpen
                && !isInteractingWithPanel)
            {
                LockCursor();
            }

            if (Cursor.lockState != CursorLockMode.Locked)
            {
                if (!_isFlying)
                {
                    ApplyGravityOnly();
                }
                else
                {
                    MoveInput = Vector2.zero;
                    CurrentVelocity = Vector3.zero;
                    IsSprinting = false;
                }

                return;
            }

            HandleLook();
            TryToggleFly();

            if (_isFlying)
            {
                HandleFlyMove();
            }
            else if (IsInWater)
            {
                HandleSwimMove();
            }
            else
            {
                HandleMove();
            }
        }

        private void UpdateWaterState()
        {
            IsInWater = false;
            IsHeadUnderwater = false;
            if (chunkManager == null)
            {
                return;
            }

            var level = chunkManager.Level;
            var body = transform.position + Vector3.up * (_controller.height * 0.35f);
            var head = transform.position + Vector3.up * (_controller.height * 0.9f);

            IsInWater = level.IsWaterAt(
                Mathf.FloorToInt(body.x),
                Mathf.FloorToInt(body.y),
                Mathf.FloorToInt(body.z));

            IsHeadUnderwater = level.IsWaterAt(
                Mathf.FloorToInt(head.x),
                Mathf.FloorToInt(head.y),
                Mathf.FloorToInt(head.z));
        }

        private void TryToggleFly()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.fKey.wasPressedThisFrame)
            {
                return;
            }

            _isFlying = !_isFlying;
            _verticalVelocity = 0f;
        }

        public void SetCameraRoot(Transform root) => cameraRoot = root;

        public void SetChunkManager(ChunkManager manager) => chunkManager = manager;

        private void ResolveCameraRoot()
        {
            if (cameraRoot != null)
            {
                return;
            }

            var existing = transform.Find("CameraRoot");
            if (existing != null)
            {
                cameraRoot = existing;
            }
        }

        private void HandleLook()
        {
            var mouse = Mouse.current;
            if (mouse == null || cameraRoot == null)
            {
                return;
            }

            var delta = mouse.delta.ReadValue() * mouseSensitivity;
            transform.Rotate(Vector3.up, delta.x, Space.World);

            _cameraPitch = Mathf.Clamp(_cameraPitch - delta.y, minPitch, maxPitch);
            cameraRoot.localRotation = Quaternion.Euler(_cameraPitch, 0f, 0f);
        }

        private void HandleMove()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                ResetMovementState();
                return;
            }

            var input = ReadHorizontalInput(keyboard);
            MoveInput = input;

            if (input.sqrMagnitude < 0.001f)
            {
                ResetMovementState();
            }
            else
            {
                IsSprinting = keyboard.leftShiftKey.isPressed;
            }

            var speed = IsSprinting ? sprintSpeed : walkSpeed;
            GetHorizontalBasis(out var forward, out var right);

            var move = (forward * input.y + right * input.x) * speed;

            if (_controller.isGrounded)
            {
                if (_verticalVelocity < 0f)
                {
                    _verticalVelocity = -2f;
                }

                if (keyboard.spaceKey.wasPressedThisFrame)
                {
                    _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                }
            }

            _verticalVelocity += gravity * Time.deltaTime;
            move.y = _verticalVelocity;

            CurrentVelocity = move;

            _controller.Move(move * Time.deltaTime);
        }

        private void ResetMovementState()
        {
            MoveInput = Vector2.zero;
            IsSprinting = false;
            CurrentVelocity = new Vector3(0f, _verticalVelocity, 0f);
        }

        private void HandleSwimMove()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                ResetMovementState();
                return;
            }

            var input = ReadHorizontalInput(keyboard);
            MoveInput = input;
            
            var speed = keyboard.leftShiftKey.isPressed ? swimSprintSpeed : swimSpeed;
            GetHorizontalBasis(out var forward, out var right);

            var move = forward * input.y + right * input.x;
            if (move.sqrMagnitude > 1f)
            {
                move.Normalize();
            }

            move *= speed;

            if (keyboard.spaceKey.isPressed)
            {
                move.y += swimAscendSpeed;
            }
            else if (!IsHeadUnderwater)
            {
                _verticalVelocity = Mathf.Max(_verticalVelocity, -1f);
            }

            _verticalVelocity += waterGravity * Time.deltaTime;
            move.y += _verticalVelocity;

            CurrentVelocity = move;
            IsSprinting = keyboard.leftShiftKey.isPressed && input.sqrMagnitude > 0f;

            _controller.Move(move * Time.deltaTime);

            if (_controller.isGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = -1f;
            }
        }

        private void HandleFlyMove()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                ResetMovementState();
                return;
            }

            var input = ReadHorizontalInput(keyboard);
            MoveInput = input;
            
            var speed = keyboard.leftShiftKey.isPressed
                ? flySpeed * flySprintMultiplier
                : flySpeed;

            GetHorizontalBasis(out var forward, out var right);
            var move = forward * input.y + right * input.x;

            if (keyboard.spaceKey.isPressed)
            {
                move.y += 1f;
            }

            if (keyboard.leftCtrlKey.isPressed || keyboard.cKey.isPressed)
            {
                move.y -= 1f;
            }

            if (move.sqrMagnitude > 1f)
            {
                move.Normalize();
            }

            Vector3 finalMove = move * speed;
            CurrentVelocity = finalMove;
            IsSprinting = keyboard.leftShiftKey.isPressed && input.sqrMagnitude > 0f;

            _controller.Move(finalMove * Time.deltaTime);
        }

        private static Vector2 ReadHorizontalInput(Keyboard keyboard)
        {
            var input = Vector2.zero;
            if (keyboard.wKey.isPressed)
            {
                input.y += 1f;
            }

            if (keyboard.sKey.isPressed)
            {
                input.y -= 1f;
            }

            if (keyboard.aKey.isPressed)
            {
                input.x -= 1f;
            }

            if (keyboard.dKey.isPressed)
            {
                input.x += 1f;
            }

            if (input.sqrMagnitude > 1f)
            {
                input.Normalize();
            }

            return input;
        }

        private void GetHorizontalBasis(out Vector3 forward, out Vector3 right)
        {
            forward = transform.forward;
            forward.y = 0f;
            forward.Normalize();
            right = transform.right;
            right.y = 0f;
            right.Normalize();
        }

        private void ApplyGravityOnly()
        {
            if (_controller.isGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = -2f;
            }

            _verticalVelocity += gravity * Time.deltaTime;
            ResetMovementState();
            _controller.Move(new Vector3(0f, _verticalVelocity, 0f) * Time.deltaTime);
        }

        private static void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void TeleportToSurfaceWaitLoad(ChunkManager manager, int worldX, int worldZ)
        {
            StartCoroutine(TeleportCoroutine(manager, worldX, worldZ));
        }

        private System.Collections.IEnumerator TeleportCoroutine(ChunkManager manager, int worldX, int worldZ)
        {
            if (_controller != null) _controller.enabled = false;

            // 1. Move player to the target X/Z but very high up, so ChunkManager starts loading around this new position.
            transform.position = new Vector3(worldX + 0.5f, 255f, worldZ + 0.5f);
            _verticalVelocity = 0f;

            var chunkPos = new ChunkPos(Mathf.FloorToInt(worldX / 16f), Mathf.FloorToInt(worldZ / 16f));

            // 2. Wait indefinitely (or a very long time) for the chunk logic to generate.
            while (true)
            {
                if (manager.Level != null && manager.Level.TryGetChunk(chunkPos, out var chunk) && chunk.IsGenerated)
                    break;
                yield return null;
            }

            // 3. Now we have the chunk data, get the exact surface Y.
            if (!manager.TrySampleTopSolidY(worldX, worldZ, out var surfaceY))
            {
                surfaceY = manager.SampleSurfaceHeight(worldX, worldZ);
            }
            surfaceY = Mathf.Max(surfaceY, WorldConstants.SeaLevel - 4);

            // 4. Place player exactly above the surface.
            transform.position = new Vector3(worldX + 0.5f, surfaceY + 2f, worldZ + 0.5f);

            // 5. Wait for the mesh collider to be built below the player (max 2 seconds).
            float timeout = 2f;
            while (!Physics.Raycast(transform.position, Vector3.down, out _, 10f) && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

            if (_controller != null) _controller.enabled = true;
        }

        private static void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
