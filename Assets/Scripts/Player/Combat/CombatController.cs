using UnityEngine;
using UnityEngine.InputSystem;
using MineCraftUnity.UI;

namespace MineCraftUnity.Player.Combat
{
    [DisallowMultipleComponent]
    public class CombatController : MonoBehaviour
    {
        private AttackState _state = AttackState.CombatOff;
        private ConvertedAttackAnimation _activeDef;
        private string _queuedComboAttackId;

        private float _stateTime;
        private float _attackElapsed;
        
        public bool IsCombatMode => _state != AttackState.CombatOff;
        public bool IsAttacking => _state == AttackState.Windup || _state == AttackState.Active || _state == AttackState.Recovery;

        public float NormalizedTime => (IsAttacking && _activeDef != null && _activeDef.TotalDuration > 0f) ? Mathf.Clamp01(_attackElapsed / _activeDef.TotalDuration) : 0f;
        public float AttackElapsed => _attackElapsed;
        public int AttackIndex { get; private set; } // Kept for compatibility if needed

        public AttackState CurrentState => _state;
        public ConvertedAttackAnimation ActiveDefinition => _activeDef;
        public string QueuedComboAttackId => _queuedComboAttackId;

        // Temporary compatibility fields for Phase 2 animator (to be removed in Step 3)
        public float AttackArmPitch = 105f;
        public float AttackArmYaw = 8f;
        public float AttackArmRoll = 8f;
        public float AttackBodyPitch = 10f;
        public float AttackBodyForwardOffset = 0.03f;
        
        public float WindupTime => ActiveDefinition != null ? ActiveDefinition.HitWindowStart : 0.05f;
        public float StrikeTime => ActiveDefinition != null ? (ActiveDefinition.HitWindowEnd - ActiveDefinition.HitWindowStart) : 0.0833f;
        public float RecoveryTime => ActiveDefinition != null ? (ActiveDefinition.TotalDuration - ActiveDefinition.HitWindowEnd) : 0.3667f;

        private PlayerViewController _viewController;
        private PlayerController _playerController;
        
        private bool _hasEmittedHitEvent;

        private void Awake()
        {
            _viewController = GetComponent<PlayerViewController>();
            _playerController = GetComponent<PlayerController>();
        }

        private void Update()
        {
            if (Keyboard.current == null || Mouse.current == null) return;

            bool panelBusy = _viewController != null &&
                             (_viewController.IsStatusPanelInteracting || IsStatusPanelOpen());
            bool canInput = !ChatCommandOverlay.IsOpen && !panelBusy;

            if (canInput && Keyboard.current.rKey.wasPressedThisFrame)
            {
                if (_state == AttackState.CombatOff)
                {
                    _state = AttackState.CombatReady;
                    TryStartAttack("fist_auto1");
                }
                else if (_state == AttackState.CombatReady)
                {
                    _state = AttackState.CombatOff;
                    _activeDef = null;
                }
            }

            if (canInput && IsCombatMode && Mouse.current.leftButton.wasPressedThisFrame)
            {
                HandleAttackInput();
            }

            UpdateState();
        }

        private bool IsStatusPanelOpen()
        {
            return _viewController != null && _viewController.IsStatusPanelVisible;
        }

        private void HandleAttackInput()
        {
            if (_state == AttackState.CombatReady)
            {
                // Select attack based on precedence
                if (_playerController != null && _playerController.IsFlying)
                {
                    // Flying logic: maybe disabled or air slash
                    return; // Currently disabled in fly mode
                }
                else if (_playerController != null && !_playerController.IsGrounded)
                {
                    TryStartAttack("fist_airslash");
                }
                else if (_playerController != null && _playerController.IsGrounded && _playerController.CurrentVelocity.magnitude > 4f)
                {
                    TryStartAttack("fist_dash");
                }
                else
                {
                    TryStartAttack("fist_auto1");
                }
            }
            else if (IsAttacking && _activeDef != null)
            {
                // Check if in combo window
                if (_attackElapsed >= _activeDef.ComboWindowStart && _attackElapsed <= _activeDef.ComboWindowEnd)
                {
                    if (!string.IsNullOrEmpty(_activeDef.NextComboAttackId))
                    {
                        _queuedComboAttackId = _activeDef.NextComboAttackId;
                    }
                }
            }
        }

        private void TryStartAttack(string attackId)
        {
            var def = AttackAnimationLibrary.Get(attackId);
            if (def == null) return;

            _activeDef = def;
            _state = AttackState.Windup;
            _stateTime = 0f;
            _attackElapsed = 0f;
            _queuedComboAttackId = null;
            _hasEmittedHitEvent = false;
            
            if (attackId == "fist_auto1") AttackIndex = 1;
            else if (attackId == "fist_auto2") AttackIndex = 2;
            else if (attackId == "fist_auto3") AttackIndex = 3;
            else AttackIndex = 0;
        }

        private void UpdateState()
        {
            if (_state == AttackState.CombatOff || _state == AttackState.CombatReady) return;

            _stateTime += Time.deltaTime;
            _attackElapsed += Time.deltaTime;

            switch (_state)
            {
                case AttackState.Windup:
                    if (_attackElapsed >= _activeDef.HitWindowStart)
                    {
                        _state = AttackState.Active;
                    }
                    break;

                case AttackState.Active:
                    if (!_hasEmittedHitEvent)
                    {
                        PerformHitDetection();
                        _hasEmittedHitEvent = true;
                    }

                    if (_attackElapsed >= _activeDef.HitWindowEnd)
                    {
                        _state = AttackState.Recovery;
                    }
                    break;

                case AttackState.Recovery:
                    if (_attackElapsed >= _activeDef.TotalDuration)
                    {
                        if (!string.IsNullOrEmpty(_queuedComboAttackId))
                        {
                            TryStartAttack(_queuedComboAttackId);
                        }
                        else
                        {
                            _state = AttackState.Cooldown;
                            _stateTime = 0f;
                        }
                    }
                    break;

                case AttackState.Cooldown:
                    if (_stateTime >= 0.1f) // Short cooldown
                    {
                        _state = AttackState.CombatReady;
                        _stateTime = 0f;
                        _activeDef = null;
                    }
                    break;
            }
        }

        private void PerformHitDetection()
        {
            if (_viewController == null) return;

            var cam = GetComponentInChildren<Camera>();
            if (cam == null) return;

            float attackRange = 3f;
            float sphereRadius = 0.5f;

            Ray ray = new Ray(cam.transform.position, cam.transform.forward);
            
            // Cast a sphere forward
            if (Physics.SphereCast(ray, sphereRadius, out RaycastHit hitInfo, attackRange))
            {
                // Emit event
                AttackHitEvent hitEvent = new AttackHitEvent
                {
                    Attacker = gameObject,
                    Target = hitInfo.collider.gameObject,
                    HitPoint = hitInfo.point,
                    KnockbackDirection = hitInfo.normal,
                    Damage = 10f, // Base damage for now
                    AttackId = _activeDef.AttackId
                };

                Debug.Log($"[Combat] {hitEvent.AttackId} hit {hitEvent.Target.name} at {hitEvent.HitPoint}");
                
                // If target has IDamageable, call it (future integration)
                var damageable = hitInfo.collider.GetComponentInParent<IDamageable>();
                damageable?.TakeDamage(hitEvent);
            }
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, Screen.height / 2, 300, 200));
            GUILayout.Label($"Combat: {(_state == AttackState.CombatOff ? "OFF" : "ON")}");
            GUILayout.Label($"Attack: {_state}");
            GUILayout.Label($"AttackId: {(_activeDef != null ? _activeDef.AttackId : "NONE")}");
            GUILayout.Label($"AttackTime: {NormalizedTime:F2}");
            
            bool isHitWindow = (_activeDef != null) && (_attackElapsed >= _activeDef.HitWindowStart && _attackElapsed <= _activeDef.HitWindowEnd);
            GUILayout.Label($"HitWindow: {(isHitWindow ? "TRUE" : "FALSE")}");
            GUILayout.Label($"ComboQueued: {(string.IsNullOrEmpty(_queuedComboAttackId) ? "NONE" : _queuedComboAttackId)}");
            GUILayout.EndArea();
        }
    }
}
