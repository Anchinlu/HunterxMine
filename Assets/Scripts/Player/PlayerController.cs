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

        public Vector3 Position => transform.position;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            ResolveCameraRoot();
        }

        private void Start()
        {
            if (lockCursorOnStart)
            {
                LockCursor();
            }
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                UnlockCursor();
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
            {
                LockCursor();
            }

            if (Cursor.lockState != CursorLockMode.Locked)
            {
                ApplyGravityOnly();
                return;
            }

            HandleLook();
            HandleMove();
        }

        public void SetCameraRoot(Transform root) => cameraRoot = root;

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
                return;
            }

            var input = Vector2.zero;
            if (keyboard.wKey.isPressed) input.y += 1f;
            if (keyboard.sKey.isPressed) input.y -= 1f;
            if (keyboard.aKey.isPressed) input.x -= 1f;
            if (keyboard.dKey.isPressed) input.x += 1f;

            if (input.sqrMagnitude > 1f)
            {
                input.Normalize();
            }

            var speed = keyboard.leftShiftKey.isPressed ? sprintSpeed : walkSpeed;
            var forward = transform.forward;
            forward.y = 0f;
            forward.Normalize();
            var right = transform.right;
            right.y = 0f;
            right.Normalize();

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

            _controller.Move(move * Time.deltaTime);
        }

        private void ApplyGravityOnly()
        {
            if (_controller.isGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = -2f;
            }

            _verticalVelocity += gravity * Time.deltaTime;
            _controller.Move(new Vector3(0f, _verticalVelocity, 0f) * Time.deltaTime);
        }

        private static void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private static void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
