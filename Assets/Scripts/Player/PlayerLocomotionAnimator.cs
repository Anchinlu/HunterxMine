using UnityEngine;

namespace MineCraftUnity.Player
{
    /// <summary>
    /// Additive-layer procedural locomotion animator (FA+Player-v1.0 behavioral parity).
    /// All animation is original C#. No .jem/.jpm assets are shipped.
    ///
    /// Architecture:
    ///   PlayerController.Update → movement data
    ///   LateUpdate → snapshot → input drag → blend weights → additive layers → clamp → smooth → write
    ///
    /// Layers (additive, cộng dồn):
    ///   Idle × idleWeight
    /// + Walk × walkWeight
    /// + Sprint × sprintWeight  (additive on walk, only the difference)
    /// + Strafe × strafeWeight × walkWeight
    /// + TurnDrag × 1
    /// + Vertical × internal weights (jump/fall/land)
    /// + Swim × swimWeight  (mutually exclusive with Fly)
    /// + Fly × flyWeight
    /// + Head (always, clamped)
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerLocomotionAnimator : MonoBehaviour
    {
        // ==================== POSE STRUCT ====================

        /// <summary>
        /// Full-body pose: rotation (degrees) + position offset (Unity units).
        /// Layers return this; results are cumulatively added via AddScaled.
        /// </summary>
        public struct FullPose
        {
            public Vector3 BodyRot, HeadRot;
            public Vector3 LeftArmRot, RightArmRot;
            public Vector3 LeftLegRot, RightLegRot;

            public Vector3 BodyPos, HeadPos;
            public Vector3 LeftArmPos, RightArmPos;
            public Vector3 LeftLegPos, RightLegPos;

            public static FullPose Zero => default;

            /// <summary>Cộng dồn: pose += layer × weight.</summary>
            public void AddScaled(in FullPose layer, float weight)
            {
                if (weight < 0.001f) return;
                BodyRot     += layer.BodyRot * weight;
                HeadRot     += layer.HeadRot * weight;
                LeftArmRot  += layer.LeftArmRot * weight;
                RightArmRot += layer.RightArmRot * weight;
                LeftLegRot  += layer.LeftLegRot * weight;
                RightLegRot += layer.RightLegRot * weight;
                BodyPos     += layer.BodyPos * weight;
                HeadPos     += layer.HeadPos * weight;
                LeftArmPos  += layer.LeftArmPos * weight;
                RightArmPos += layer.RightArmPos * weight;
                LeftLegPos  += layer.LeftLegPos * weight;
                RightLegPos += layer.RightLegPos * weight;
            }
        }

        // ==================== CONSTANTS & HELPERS ====================

        /// <summary>1 Minecraft pixel → Unity units (1 block = 16 pixels = 1 unit).</summary>
        private const float PixelToUnit = 1f / 16f;
        private const float PI = Mathf.PI;

        /// <summary>Convert FA+ pixel value to Unity units.</summary>
        private static float Px(float pixels) => pixels * PixelToUnit;

        /// <summary>Frame-rate independent exponential smoothing.</summary>
        private static float ExpSmooth(float current, float target, float speed)
        {
            float t = 1f - Mathf.Exp(-speed * Time.deltaTime);
            return Mathf.Lerp(current, target, t);
        }

        // ==================== SERIALIZED PARAMETERS ====================

        [Header("Walk Layer")]
        [SerializeField] private float walkFrequency = 6.5f;
        [SerializeField] private float walkArmSwing = 50f;
        [SerializeField] private float walkArmYaw = 12f;
        [SerializeField] private float walkArmRoll = 4f;
        [SerializeField] private float walkLegSwing = 35f;
        [SerializeField] private float walkLegBase = 10f;
        [SerializeField] private float walkBodySway = 13f;
        [SerializeField] private float walkBodyLean = 7f;
        [SerializeField] private float walkBodyRoll = 3f;
        [SerializeField] private float walkBounceScale = 1f;
        [SerializeField] private float walkArmReachPx = 2f;
        [SerializeField] private float walkArmOutwardRoll = 12f;
        [SerializeField] private float walkArmOutwardPx = 0.5f;
        [SerializeField] private float walkLegLiftPx = 1.3f;
        [SerializeField] private float walkLegStepPx = 1.6f;
        [SerializeField] private float walkLegLeanCompZ = 0.15f;

        [Header("Backward Layer")]
        [SerializeField] private float backwardLeanAngle = 5f;
        [SerializeField] private float backwardArmSwing = 38f;
        [SerializeField] private float backwardLegSwing = 30f;

        [Header("Sprint Additive Layer")]
        [SerializeField] private float sprintExtraLean = 10f;
        [SerializeField] private float sprintExtraArmRoll = 6f;
        [SerializeField] private float sprintExtraArmSwing = 15f;
        [SerializeField] private float sprintExtraLegSwing = 10f;
        [SerializeField] private float sprintArmSwayPx = 0.2f; // Slight translation, be careful not to detach shoulders
        [SerializeField] private float sprintExtraBounce = 0.4f;
        [SerializeField] private float sprintLegCompZ = 0.2f;

        [Header("Strafe Layer")]
        [SerializeField] private float strafeBodyRoll = 10f;
        [SerializeField] private float strafeShoulderYaw = 8f;
        [SerializeField] private float strafeLegYaw = 15f;
        [SerializeField] private float strafeLateralPx = 0.25f;

        [Header("Turn Drag")]
        [SerializeField] private float turnDragFactor = 0.008f;
        [SerializeField] private float turnBodyRoll = 4f;
        [SerializeField] private float turnBodyYaw = 2f;
        [SerializeField] private float turnArmSway = 3f;
        [SerializeField] private float turnDragSmooth = 6f;

        [Header("Head")]
        [SerializeField] private float headPitchScale = 0.6f;
        [SerializeField] private float maxHeadPitch = 35f;
        [SerializeField] private float maxHeadYaw = 35f;
        [SerializeField] private float maxHeadRoll = 7f;
        [SerializeField] private float headDragSpeed = 8f;

        [Header("Input Drag")]
        [SerializeField] private float inputDragSpeed = 8f;

        [Header("Blend Speeds")]
        [SerializeField] private float blendUpSpeed = 6f;
        [SerializeField] private float blendDownSpeed = 4f;

        [Header("Smoothing")]
        [SerializeField] private float bodyRotSmooth = 12f;
        [SerializeField] private float headRotSmooth = 8f;
        [SerializeField] private float limbRotSmooth = 14f;
        [SerializeField] private float positionSmooth = 10f;

        [Header("Idle Layer (Placeholder)")]
        [SerializeField] private float idleBreathSpeed = 2f;
        [SerializeField] private float idleBreathAngle = 1.2f;

        [Header("Jump/Fall/Landing (Phase 3)")]
        [SerializeField] private float jumpPoseDuration = 0.2f;
        [SerializeField] private float landingDuration = 0.22f;
        [SerializeField] private float fallArmSpread = 90f;
        [SerializeField] private float fallFrequency = 4f;
        [SerializeField] private float landingLegBend = 12f;

        [Header("Swim")]
        [SerializeField] private float swimFrequency = 3f;

        [Header("Fly")]
        [SerializeField] private float flySwaySpeed = 2f;
        [SerializeField] private float flyLegPositionCompensation = 0.35f;

        // ==================== ENUM (kept for debug) ====================

        public enum LocomotionState
        {
            Idle, Walk, Sprint, Backward, Strafe,
            Jump, Fall, Landing, Swim, Fly
        }

        public LocomotionState CurrentState { get; private set; }

        // ==================== CHARACTER PANEL OVERRIDE ====================
        
        public bool IsCharacterPanelOpen { get; set; }
        public float CharacterPanelWeight { get; private set; }

        // ==================== PRIVATE STATE ====================

        private PlayerController _player;
        private MineCraftUnity.Player.Combat.CombatController _attackController;
        
        private Transform _rootCombatPivot;
        private Transform _upperBodyPivot;
        private Transform _leftArmPivot, _rightArmPivot;
        private Transform _leftLegPivot, _rightLegPivot;
        private Transform _head;
        private Transform _headPivot;
        private Transform _chestPivot;
        private Transform _leftElbowPivot, _rightElbowPivot;
        private Transform _leftKneePivot, _rightKneePivot;

        // Locomotion base poses (legacy)
        private Vector3 _baseLArmPos, _baseRArmPos;
        private Vector3 _baseLLegPos, _baseRLegPos;

        // Combat base poses
        private Vector3 _rootBaseLocalPosition;
        private Quaternion _rootBaseLocalRotation;
        private Vector3 _chestBaseLocalPosition;
        private Quaternion _chestBaseLocalRotation;
        private Vector3 _upperBodyBaseLocalPosition;
        private Quaternion _upperBodyBaseLocalRotation;
        private Vector3 _headPivotBaseLocalPosition;
        private Quaternion _headPivotBaseLocalRotation;
        private Vector3 _headBaseLocalPosition;
        private Quaternion _headBaseLocalRotation;
        private Vector3 _leftArmBaseLocalPosition;
        private Quaternion _leftArmBaseLocalRotation;
        private Vector3 _rightArmBaseLocalPosition;
        private Quaternion _rightArmBaseLocalRotation;
        private Vector3 _leftElbowBaseLocalPosition;
        private Quaternion _leftElbowBaseLocalRotation;
        private Vector3 _rightElbowBaseLocalPosition;
        private Quaternion _rightElbowBaseLocalRotation;
        private Vector3 _leftLegBaseLocalPosition;
        private Quaternion _leftLegBaseLocalRotation;
        private Vector3 _rightLegBaseLocalPosition;
        private Quaternion _rightLegBaseLocalRotation;
        private Vector3 _leftKneeBaseLocalPosition;
        private Quaternion _leftKneeBaseLocalRotation;
        private Vector3 _rightKneeBaseLocalPosition;
        private Quaternion _rightKneeBaseLocalRotation;

        // Phases (separate per system)
        private float _groundPhase;
        private float _swimPhase;
        private float _flyPhase;
        private float _breathPhase;
        private static float Saturate(float value)
        {
            return Mathf.Clamp01(value);
        }

        private static float Smooth01(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }
        // Input drag — smoothed values (creates inertia/weight feeling)
        private float _forwardDrag;
        private float _strafeDrag;
        private float _speedDrag;
        private float _verticalDrag;
        private float _smoothTurnAmount;

        // Blend weights (0→1, smooth transitions)
        private float _walkWeight;
        private float _sprintWeight;
        private float _idleWeight;
        private float _strafeWeight;
        private float _jumpWeight;
        private float _fallWeight;
        private float _landWeight;
        private float _treadWeight;
        private float _swimMoveWeight;
        private float _swimUpWeight;
        private float _backwardWeight;
        private float _flyWeight;
        private float _inAirWeight;

        // Vertical tracking
        private bool _wasGrounded;
        private bool _initializedGround;
        private float _jumpElapsed;
        private float _airElapsed;
        private float _landingElapsed;
        private float _landingImpactSpeed;

        // Head drag
        private float _headPitchDrag;

        private LocomotionState _prevLogState = (LocomotionState)(-1);

        // ==================== LIFECYCLE ====================

        private void Awake()
        {
            _player = GetComponentInParent<PlayerController>();
            _attackController = GetComponentInParent<MineCraftUnity.Player.Combat.CombatController>();
            ResolvePivots();
        }

        private void Start()
        {
            if (_player == null)
                _player = GetComponentInParent<PlayerController>();
            if (_player != null)
            {
                _wasGrounded = _player.IsGrounded;
                _initializedGround = true;
            }
            CacheBasePositions();
            // Random breathing offset per entity (FA+ uses random(id))
            _breathPhase = Random.value * PI * 2f;
        }

        private void ResolvePivots()
        {
            var rootCombat = transform.Find("RootCombatPivot");
            _rootCombatPivot = rootCombat;
            if (rootCombat != null)
            {
                _upperBodyPivot = rootCombat.Find("UpperBodyPivot");
                _leftLegPivot = rootCombat.Find("LeftThighPivot");
                _rightLegPivot = rootCombat.Find("RightThighPivot");
            }
            else
            {
                // Fallbacks for older rig versions
                _upperBodyPivot = transform.Find("UpperBodyPivot");
                _leftLegPivot = transform.Find("LeftLegPivot");
                if (_leftLegPivot == null) _leftLegPivot = transform.Find("LeftThighPivot");
                _rightLegPivot = transform.Find("RightLegPivot");
                if (_rightLegPivot == null) _rightLegPivot = transform.Find("RightThighPivot");
            }

            if (_upperBodyPivot != null)
            {
                _leftArmPivot = _upperBodyPivot.Find("LeftShoulderPivot");
                if (_leftArmPivot == null) _leftArmPivot = _upperBodyPivot.Find("LeftArmPivot");
                
                _rightArmPivot = _upperBodyPivot.Find("RightShoulderPivot");
                if (_rightArmPivot == null) _rightArmPivot = _upperBodyPivot.Find("RightArmPivot");
                
                _headPivot = _upperBodyPivot.Find("HeadPivot");
                _head = _headPivot != null ? _headPivot.Find("Head") : null;
                if (_head == null) _head = _upperBodyPivot.Find("Head"); // Fallback

                _chestPivot = _upperBodyPivot.Find("ChestPivot");
                
                if (_leftArmPivot != null) _leftElbowPivot = _leftArmPivot.Find("LeftElbowPivot");
                if (_rightArmPivot != null) _rightElbowPivot = _rightArmPivot.Find("RightElbowPivot");
            }
            else
            {
                _leftArmPivot = transform.Find("LeftArmPivot");
                _rightArmPivot = transform.Find("RightArmPivot");
                _head = transform.Find("Head");
            }
            
            if (_leftLegPivot != null) _leftKneePivot = _leftLegPivot.Find("LeftKneePivot");
            if (_rightLegPivot != null) _rightKneePivot = _rightLegPivot.Find("RightKneePivot");

            if (_upperBodyPivot == null || _leftArmPivot == null || _rightArmPivot == null || _leftLegPivot == null || _rightLegPivot == null)
            {
                Debug.LogError($"[PlayerLocomotionAnimator] Missing required pivots on {name}. Disabling locomotion animator.");
                this.enabled = false;
            }
        }

        private void CacheBasePositions()
        {
            _baseLArmPos = _leftArmPivot != null ? _leftArmPivot.localPosition : new Vector3(-0.375f, 1.5f, 0f);
            _baseRArmPos = _rightArmPivot != null ? _rightArmPivot.localPosition : new Vector3(0.375f, 1.5f, 0f);
            _baseLLegPos = _leftLegPivot != null ? _leftLegPivot.localPosition : new Vector3(-0.125f, 0.75f, 0f);
            _baseRLegPos = _rightLegPivot != null ? _rightLegPivot.localPosition : new Vector3(0.125f, 0.75f, 0f);

            if (_rootCombatPivot != null) { _rootBaseLocalPosition = _rootCombatPivot.localPosition; _rootBaseLocalRotation = _rootCombatPivot.localRotation; }
            if (_chestPivot != null) { _chestBaseLocalPosition = _chestPivot.localPosition; _chestBaseLocalRotation = _chestPivot.localRotation; }
            if (_upperBodyPivot != null) { _upperBodyBaseLocalPosition = _upperBodyPivot.localPosition; _upperBodyBaseLocalRotation = _upperBodyPivot.localRotation; }
            if (_headPivot != null)
            {
                _headPivotBaseLocalPosition = _headPivot.localPosition;
                _headPivotBaseLocalRotation = _headPivot.localRotation;
            }
            if (_head != null) { _headBaseLocalPosition = _head.localPosition; _headBaseLocalRotation = _head.localRotation; }
            
            if (_leftArmPivot != null) { _leftArmBaseLocalPosition = _leftArmPivot.localPosition; _leftArmBaseLocalRotation = _leftArmPivot.localRotation; }
            if (_rightArmPivot != null) { _rightArmBaseLocalPosition = _rightArmPivot.localPosition; _rightArmBaseLocalRotation = _rightArmPivot.localRotation; }
            if (_leftElbowPivot != null) { _leftElbowBaseLocalPosition = _leftElbowPivot.localPosition; _leftElbowBaseLocalRotation = _leftElbowPivot.localRotation; }
            if (_rightElbowPivot != null) { _rightElbowBaseLocalPosition = _rightElbowPivot.localPosition; _rightElbowBaseLocalRotation = _rightElbowPivot.localRotation; }
            
            if (_leftLegPivot != null) { _leftLegBaseLocalPosition = _leftLegPivot.localPosition; _leftLegBaseLocalRotation = _leftLegPivot.localRotation; }
            if (_rightLegPivot != null) { _rightLegBaseLocalPosition = _rightLegPivot.localPosition; _rightLegBaseLocalRotation = _rightLegPivot.localRotation; }
            if (_leftKneePivot != null) { _leftKneeBaseLocalPosition = _leftKneePivot.localPosition; _leftKneeBaseLocalRotation = _leftKneePivot.localRotation; }
            if (_rightKneePivot != null) { _rightKneeBaseLocalPosition = _rightKneePivot.localPosition; _rightKneeBaseLocalRotation = _rightKneePivot.localRotation; }
        }

        // ==================== MAIN LOOP ====================

        private void LateUpdate()
        {
            if (_player == null)
            {
                _player = GetComponentInParent<PlayerController>();
                if (_player == null) return;
            }
            if (_upperBodyPivot == null)
            {
                ResolvePivots();
                CacheBasePositions();
            }
            if (_attackController == null)
            {
                _attackController = GetComponentInParent<MineCraftUnity.Player.Combat.CombatController>();
            }

            // ── 1. Snapshot raw input ──
            float horizontalSpeed = _player.HorizontalSpeed;
            float verticalSpeed = _player.CurrentVelocity.y;
            bool isGrounded = _player.IsGrounded;
            bool isFlying = _player.IsFlying;
            bool isInWater = _player.IsInWater;
            bool isSprinting = _player.IsSprinting;
            Vector2 moveInput = _player.MoveInput;
            float yawVelocity = _player.YawVelocity;

            float forward = Mathf.Clamp(moveInput.y, -1f, 1f);
            float strafe = Mathf.Clamp(moveInput.x, -1f, 1f);
            // Unclamped speed ratio — allows >1 during sprint for natural scaling
            float rawSpeedRatio = horizontalSpeed / Mathf.Max(_player.WalkSpeed, 0.01f);

            // ── 2. Initialize ground tracking & vertical timers ──
            if (!_initializedGround)
            {
                _wasGrounded = isGrounded;
                _landingElapsed = landingDuration;
                _initializedGround = true;
                return;
            }

            bool justLanded = !_wasGrounded && isGrounded;
            
            if (_wasGrounded && !isGrounded && verticalSpeed > 0.1f)
            {
                _jumpElapsed = 0f;
                _airElapsed = 0f;
            }
            
            if (!isGrounded)
            {
                _jumpElapsed += Time.deltaTime;
                _airElapsed += Time.deltaTime;
            }
            
            if (justLanded)
            {
                _landingImpactSpeed = Mathf.Clamp01(Mathf.Abs(_verticalDrag) / 10f);
                _landingElapsed = 0f;
            }
            
            if (isGrounded)
            {
                if (_landingElapsed < landingDuration)
                    _landingElapsed += Time.deltaTime;
            }
            
            _wasGrounded = isGrounded;

            // ── 3. Update input drag (exponential smoothing — FPS independent) ──
            _forwardDrag = ExpSmooth(_forwardDrag, forward, inputDragSpeed);
            _strafeDrag = ExpSmooth(_strafeDrag, strafe, inputDragSpeed);
            _speedDrag = ExpSmooth(_speedDrag, rawSpeedRatio, inputDragSpeed);
            _verticalDrag = ExpSmooth(_verticalDrag, verticalSpeed, inputDragSpeed);

            float turnRaw = Mathf.Clamp(yawVelocity * turnDragFactor, -1f, 1f);
            _smoothTurnAmount = ExpSmooth(_smoothTurnAmount, turnRaw, turnDragSmooth);

            // ── 4. Determine dominant state (for debug log) ──
            LocomotionState newState;
            if (isFlying) newState = LocomotionState.Fly;
            else if (isInWater) newState = LocomotionState.Swim;
            else if (_landingElapsed < landingDuration && isGrounded) newState = LocomotionState.Landing;
            else if (!isGrounded && verticalSpeed > 0.1f) newState = LocomotionState.Jump;
            else if (!isGrounded) newState = LocomotionState.Fall;
            else if (isSprinting && forward > 0.1f) newState = LocomotionState.Sprint;
            else if (forward < -0.1f) newState = LocomotionState.Backward;
            else if (moveInput.sqrMagnitude > 0.001f)
                newState = Mathf.Abs(strafe) > 0.5f && Mathf.Abs(forward) < 0.1f
                    ? LocomotionState.Strafe : LocomotionState.Walk;
            else newState = LocomotionState.Idle;

            if (newState != _prevLogState)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[LocoAnim] State={newState} Grounded={isGrounded} Vertical={verticalSpeed:F2} Jump={_jumpWeight:F2} Fall={_fallWeight:F2} Land={_landWeight:F2}");
#endif
                _prevLogState = newState;
            }
            CurrentState = newState;

            // ── 5. Update blend weights ──
            UpdateBlendWeights(isGrounded, isFlying, isInWater, isSprinting, forward, strafe, horizontalSpeed);

            // ── 6. Update phases ──
            _groundPhase += walkFrequency * Mathf.Max(_speedDrag, 0f) * Time.deltaTime;
            _swimPhase += swimFrequency * Time.deltaTime;
            _flyPhase += flySwaySpeed * Time.deltaTime;
            _breathPhase += idleBreathSpeed * Time.deltaTime;

            // ── 7. Compute additive layers ──
            var pose = FullPose.Zero;

            pose.AddScaled(ComputeIdleLayer(), _idleWeight);
            
            float groundAirDamp = 1f - Mathf.Clamp01(_fallWeight);
            float backwardDamp = 1f - _backwardWeight;
            
            pose.AddScaled(ComputeWalkLayer(), _walkWeight * groundAirDamp * backwardDamp);
            pose.AddScaled(ComputeBackwardLayer(), _backwardWeight * groundAirDamp);
            
            pose.AddScaled(ComputeSprintAdditiveLayer(), _sprintWeight * groundAirDamp);
            
            pose.AddScaled(ComputeStrafeLayer(), _strafeWeight * groundAirDamp); // Dampened by fall
            pose.AddScaled(ComputeTurnDragLayer(), 1f);

            pose.AddScaled(ComputeJumpLayer(), _jumpWeight);
            pose.AddScaled(ComputeFallLayer(), _fallWeight);
            pose.AddScaled(ComputeLandingLayer(), _landWeight);

            // Swim and Fly: mutually exclusive — highest weight wins
            if (_flyWeight > 0.01f && _flyWeight >= _treadWeight && _flyWeight >= _swimMoveWeight)
            {
                pose.AddScaled(ComputeFlyLayer(), _flyWeight);
            }
            else
            {
                pose.AddScaled(ComputeTreadWaterLayer(), _treadWeight);
                pose.AddScaled(ComputeSwimMoveLayer(), _swimMoveWeight);
                pose.AddScaled(ComputeSwimUpLayer(), _swimUpWeight);
            }

            ComputeHeadLayer(ref pose);

            // ── 7. Clamp rotations ──
            ClampPose(ref pose);

            // ── 8. Smooth & Write Locomotion Pose ──
            WritePose(pose);

            // ── 9. Combat Animation Override (Relative Delta) ──
            if (_attackController != null && _attackController.IsAttacking && _attackController.ActiveDefinition != null)
            {
                ApplyCombatAnimation(_attackController.ActiveDefinition, _attackController.AttackElapsed);
            }

            // ── 10. Apply Character Panel Override (Left Arm) ──
            // UI override must happen last so combat doesn't overwrite it
            if (CharacterPanelWeight > 0.001f)
            {
                // Smooth sinusoidal micro motion
                float micro = Mathf.Sin(Time.time * 1.5f) * 2f;
                Vector3 panelPose = new Vector3(-55f, 35f, -20f + micro);
                
                if (_leftArmPivot != null)
                {
                    Quaternion targetRot = Quaternion.Euler(panelPose);
                    _leftArmPivot.localRotation = Quaternion.Slerp(_leftArmPivot.localRotation, targetRot, CharacterPanelWeight);
                }
            }
        }

        // ==================== BLEND WEIGHTS ====================

        private void UpdateBlendWeights(bool grounded, bool flying, bool inWater,
            bool sprinting, float forward, float strafe, float hSpeed)
        {
            float up = blendUpSpeed * Time.deltaTime;
            float down = blendDownSpeed * Time.deltaTime;

            bool isMoving = hSpeed > 0.1f;
            bool onGround = grounded && !flying && !inWater;

            // Walk: moving on ground forward/sideways
            float walkTarget = (onGround && isMoving && forward >= -0.1f) ? 1f : 0f;
            _walkWeight = Mathf.MoveTowards(_walkWeight, walkTarget,
                walkTarget > _walkWeight ? up : down);

            // Backward: moving backward on ground
            float backwardTarget = (onGround && !flying && !inWater && forward < -0.1f) ? Mathf.Clamp01(-forward) : 0f;
            _backwardWeight = Mathf.MoveTowards(_backwardWeight, backwardTarget,
                backwardTarget > _backwardWeight ? up : down);

            // Sprint: additive on walk (only the difference)
            float sprintTarget = (sprinting && forward > 0.1f && onGround) ? 1f : 0f;
            _sprintWeight = Mathf.MoveTowards(_sprintWeight, sprintTarget,
                sprintTarget > _sprintWeight ? up : down);

            // Idle: not moving on ground
            float idleTarget = (onGround && !isMoving) ? 1f : 0f;
            _idleWeight = Mathf.MoveTowards(_idleWeight, idleTarget,
                idleTarget > _idleWeight ? up : down);

            // Strafe: absolute strafe input
            float strafeTarget = Mathf.Abs(strafe) > 0.3f ? Mathf.Abs(strafe) : 0f;
            _strafeWeight = Mathf.MoveTowards(_strafeWeight, strafeTarget, up);

            // In air
            float inAirTarget = (!grounded && !flying && !inWater) ? 1f : 0f;
            _inAirWeight = Mathf.MoveTowards(_inAirWeight, inAirTarget,
                inAirTarget > _inAirWeight ? up * 1.5f : down);

            // Jump
            float jumpTarget = (!grounded && _player.CurrentVelocity.y > 0.1f && !flying && !inWater) ? 1f : 0f;
            _jumpWeight = Mathf.MoveTowards(_jumpWeight, jumpTarget,
                jumpTarget > _jumpWeight ? up * 2f : down);

            // Fall
            float fallTarget = (!grounded && _player.CurrentVelocity.y <= 0.1f && !flying && !inWater) ? 1f : 0f;
            _fallWeight = Mathf.MoveTowards(_fallWeight, fallTarget,
                fallTarget > _fallWeight ? up : down);

            // Landing
            float landTarget = (grounded && _landingElapsed < landingDuration) ? 1f : 0f;
            _landWeight = Mathf.MoveTowards(_landWeight, landTarget, up * 2f);

            // Swim and Tread
            float waterSpeed = hSpeed;
            float verticalWaterSpeed = _player.CurrentVelocity.y;

            float treadTarget = (inWater && waterSpeed < 0.15f && Mathf.Abs(verticalWaterSpeed) < 0.15f) ? 1f : 0f;
            _treadWeight = Mathf.MoveTowards(_treadWeight, treadTarget,
                treadTarget > _treadWeight ? up : down);

            float swimTarget = (inWater && waterSpeed >= 0.15f) ? 1f : 0f;
            _swimMoveWeight = Mathf.MoveTowards(_swimMoveWeight, swimTarget,
                swimTarget > _swimMoveWeight ? up : down);

            float swimUpTarget = (inWater && verticalWaterSpeed > 0.15f) ? 1f : 0f;
            _swimUpWeight = Mathf.MoveTowards(_swimUpWeight, swimUpTarget,
                swimUpTarget > _swimUpWeight ? up : down);

            // Fly
            float flyTarget = flying ? 1f : 0f;
            _flyWeight = Mathf.MoveTowards(_flyWeight, flyTarget,
                flyTarget > _flyWeight ? up : down);

            // Character Panel Override
            float panelTarget = IsCharacterPanelOpen ? 1f : 0f;
            CharacterPanelWeight = Mathf.MoveTowards(CharacterPanelWeight, panelTarget, 
                (panelTarget > CharacterPanelWeight ? 5f : 6f) * Time.deltaTime);
        }

        // ==================== WALK LAYER (FA+ inspired — Phase 2) ====================

        private FullPose ComputeWalkLayer()
        {
            var p = FullPose.Zero;
            float phase = _groundPhase;
            // Unclamp speed so sprint scales naturally beyond 1.0
            float sqrtSpeed = Mathf.Sqrt(Mathf.Max(_speedDrag, 0f));

            // ─── Body ───
            // FA+ mvmnt_bodyrx: (2*cos(ls*2) + 7*forwd_drag) * sqrt(speed)
            p.BodyRot.x = (2f * Mathf.Cos(phase * 2f) + walkBodyLean * _forwardDrag) * sqrtSpeed;
            // FA+ mvmnt_bodyry: 13*cos(ls) * sqrt(speed) — strong shoulder sway!
            p.BodyRot.y = walkBodySway * Mathf.Cos(phase) * sqrtSpeed;
            // FA+ mvmnt_bodyrz: -3*sin(ls) * sqrt(speed)
            p.BodyRot.z = -walkBodyRoll * Mathf.Sin(phase) * sqrtSpeed;

            // Body bounce — FA+ non-linear: (sin(π/4+ls*2 - cos(π/4+ls*2)/6) + 0.8) * 0.8
            float bouncePhase = PI / 4f + phase * 2f;
            float bouncePixels = (Mathf.Sin(bouncePhase - Mathf.Cos(bouncePhase) / 6f) + 0.8f) * 0.8f;
            p.BodyPos.y = -Px(bouncePixels * walkBounceScale) * sqrtSpeed;

            // Body lateral sway — FA+ mvmnt_bodytx
            p.BodyPos.x = Px(-Mathf.Cos(phase) / 2.6f * _forwardDrag) * sqrtSpeed;

            // ─── Arms ───
            // FA+ uses pi/7 phase offset for natural arm/leg overlap
            float armPhase = PI / 7f + phase;
            float armCos = Mathf.Cos(armPhase);

            // Arm swing X (contralateral: right arm forward with left leg)
            // FA+ rarm_rx: 60*cos(pi/7+ls) * sqrt(speed)
            p.RightArmRot.x =  walkArmSwing * armCos * sqrtSpeed;
            p.LeftArmRot.x  = -walkArmSwing * armCos * sqrtSpeed;

            // Arm yaw Y — FA+ simplified: (18*cos(phase) - 10*(0.8+cos(-1.3+phase)))/2
            float armYawExpr = (walkArmYaw * Mathf.Cos(phase)
                - 8f * (0.8f + Mathf.Cos(-1.3f + phase))) / 2f * sqrtSpeed;
            p.RightArmRot.y =  armYawExpr;
            p.LeftArmRot.y  = -armYawExpr;

            // Arm roll Z — outward: left +Z, right -Z on this rig
            float armRollExpr = (walkArmOutwardRoll + walkArmRoll * Mathf.Cos(PI / 5f + phase)) * sqrtSpeed;
            p.RightArmRot.z = -armRollExpr;
            p.LeftArmRot.z  =  armRollExpr;

            // Arm translation Z & X
            p.RightArmPos.z =  Px(walkArmReachPx * Mathf.Cos(phase - Mathf.Sin(phase) / 3f * 0.3f)) * sqrtSpeed;
            p.LeftArmPos.z  = -Px(walkArmReachPx * Mathf.Cos(phase + Mathf.Sin(phase) / 3f * 0.3f)) * sqrtSpeed;

            // Push shoulders slightly outward
            p.RightArmPos.x =  Px(walkArmOutwardPx) * sqrtSpeed;
            p.LeftArmPos.x  = -Px(walkArmOutwardPx) * sqrtSpeed;

            // ─── Legs ───
            // Non-linear phase offset for natural step asymmetry — FA+: cos(ls)/2.5
            float cosPhase = Mathf.Cos(phase);
            float rLegPhase = phase + cosPhase / 2.5f;
            float lLegPhase = phase - cosPhase / 2.5f;

            // Leg swing X — FA+: 13 - 40*cos(phase + cos(phase)/2.5)
            p.RightLegRot.x = (walkLegBase - walkLegSwing * Mathf.Cos(rLegPhase)) * sqrtSpeed;
            p.LeftLegRot.x  = (walkLegBase + walkLegSwing * Mathf.Cos(lLegPhase)) * sqrtSpeed;

            // Leg lift Y — leg lifts when swinging forward
            float rLegForward = Mathf.Max(0f, -Mathf.Cos(rLegPhase));
            float lLegForward = Mathf.Max(0f,  Mathf.Cos(lLegPhase));
            p.RightLegPos.y = Px(rLegForward * walkLegLiftPx) * sqrtSpeed;
            p.LeftLegPos.y  = Px(lLegForward * walkLegLiftPx) * sqrtSpeed;

            // Leg step Z — derive from rotation for guaranteed consistency
            float maxLeg = walkLegBase + walkLegSwing;
            float walkCompZ = p.BodyRot.x * walkLegLeanCompZ * PixelToUnit; // compensate for lean
            p.RightLegPos.z = Px(walkLegStepPx * (p.RightLegRot.x / maxLeg)) + walkCompZ;
            p.LeftLegPos.z  = Px(walkLegStepPx * (p.LeftLegRot.x / maxLeg)) + walkCompZ;

            return p;
        }

        // ==================== SPRINT ADDITIVE LAYER ====================
        // Only the DIFFERENCE from walk. Cộng thêm, không thay thế.

        private FullPose ComputeSprintAdditiveLayer()
        {
            var p = FullPose.Zero;
            float phase = _groundPhase;
            // Unclamped speed to apply proper sprint scaling
            float sqrtSpeed = Mathf.Sqrt(Mathf.Max(_speedDrag, 0f));

            // Extra forward lean
            p.BodyRot.x = sprintExtraLean * _forwardDrag;

            // Extra arm roll (outward)
            p.RightArmRot.z = -sprintExtraArmRoll * sqrtSpeed;
            p.LeftArmRot.z  =  sprintExtraArmRoll * sqrtSpeed;

            // Extra arm swing amplitude
            float armCos = Mathf.Cos(PI / 7f + phase);
            p.RightArmRot.x =  sprintExtraArmSwing * armCos * sqrtSpeed;
            p.LeftArmRot.x  = -sprintExtraArmSwing * armCos * sqrtSpeed;

            // Extra leg swing amplitude
            float cosPhase = Mathf.Cos(phase);
            float rLegPhase = phase + cosPhase / 2.5f;
            float lLegPhase = phase - cosPhase / 2.5f;
            p.RightLegRot.x = -sprintExtraLegSwing * Mathf.Cos(rLegPhase) * sqrtSpeed;
            p.LeftLegRot.x  =  sprintExtraLegSwing * Mathf.Cos(lLegPhase) * sqrtSpeed;

            // Extra arm lateral sway — FA+: (0.5+cos(ls*2))/4 * sprint
            float armTx = Px((0.5f + Mathf.Cos(_groundPhase * 2f)) / 4f * sprintArmSwayPx) * sqrtSpeed;
            p.LeftArmPos.x  = -armTx;
            p.RightArmPos.x =  armTx;

            // Extra bounce (stronger impact)
            float bounce = Mathf.Abs(Mathf.Sin(_groundPhase * 2f));
            p.BodyPos.y = -Px(bounce * sprintExtraBounce);

            // Extra leg compensation for sprint lean
            float sprintCompZ = p.BodyRot.x * sprintLegCompZ * PixelToUnit;
            p.LeftLegPos.z  = sprintCompZ;
            p.RightLegPos.z = sprintCompZ;

            return p;
        }

        // ==================== STRAFE LAYER ====================

        private FullPose ComputeStrafeLayer()
        {
            var p = FullPose.Zero;
            float sd = _strafeDrag;

            // Body roll toward strafe direction
            p.BodyRot.z = -sd * strafeBodyRoll;
            // Shoulder yaw
            p.BodyRot.y = sd * strafeShoulderYaw;

            // Leg yaw during strafe — FA+ has 20° per leg
            p.RightLegRot.y =  Mathf.Max(0f,  sd) * strafeLegYaw;
            p.LeftLegRot.y  = -Mathf.Max(0f, -sd) * strafeLegYaw;

            // Lateral body offset
            p.BodyPos.x = Px(sd * strafeLateralPx);

            return p;
        }

        // ==================== TURN DRAG LAYER ====================

        private FullPose ComputeTurnDragLayer()
        {
            var p = FullPose.Zero;
            float t = _smoothTurnAmount;

            p.BodyRot.y = -t * turnBodyYaw;
            p.BodyRot.z = -t * turnBodyRoll;

            // Fly has its own arm turning logic.
            float groundArmWeight = 1f - _flyWeight;
            p.LeftArmRot.z  = -t * turnArmSway * groundArmWeight;
            p.RightArmRot.z =  t * turnArmSway * groundArmWeight;

            return p;
        }

        // ==================== IDLE LAYER ====================

        private FullPose ComputeIdleLayer()
        {
            var p = FullPose.Zero;
            float headYaw = _smoothTurnAmount;
            float breath = Mathf.Sin(_breathPhase);

            p.BodyRot.x += breath * idleBreathAngle;
            p.BodyRot.y += headYaw * 4f;

            p.LeftArmRot.y += headYaw * 3f;
            p.RightArmRot.y += headYaw * 3f;

            p.LeftArmRot.x += Mathf.Sin(_breathPhase + Mathf.PI) * 0.5f;
            p.RightArmRot.x += Mathf.Sin(_breathPhase) * 0.5f;
            
            return p;
        }

        // ==================== BACKWARD LAYER ====================

        private FullPose ComputeBackwardLayer()
        {
            var p = FullPose.Zero;
            float phase = -_groundPhase;
            float amplitude = Mathf.Sqrt(Mathf.Clamp01(_speedDrag)) * 0.78f;

            p.BodyRot.x = -backwardLeanAngle * Mathf.Abs(_forwardDrag);
            p.BodyRot.y = 8f * Mathf.Cos(phase) * amplitude;
            p.BodyRot.z = -2f * Mathf.Sin(phase) * amplitude;

            float arm = Mathf.Cos(Mathf.PI / 7f + phase) * backwardArmSwing * amplitude;
            p.RightArmRot.x = arm;
            p.LeftArmRot.x = -arm;

            float legPhase = phase + Mathf.Cos(phase) / 2.5f;
            p.RightLegRot.x = 8f - backwardLegSwing * Mathf.Cos(legPhase) * amplitude;
            p.LeftLegRot.x = 8f + backwardLegSwing * Mathf.Cos(phase - Mathf.Cos(phase) / 2.5f) * amplitude;

            p.RightArmPos.z = Px(1f * Mathf.Cos(phase));
            p.LeftArmPos.z = -Px(1f * Mathf.Cos(phase));

            p.BodyPos.z = Px(-1f * _forwardDrag);

            return p;
        }

        // ==================== JUMP LAYER (Phase 3) ====================

        private FullPose ComputeJumpLayer()
        {
            var p = FullPose.Zero;
            float jumpT = Saturate(_jumpElapsed / jumpPoseDuration);
            float jumpEnvelope = Mathf.Exp(-_jumpElapsed * Mathf.PI);
            float jumpWave = Mathf.Cos(jumpT * Mathf.PI);
            float speed = Mathf.Clamp01(_speedDrag);
            float movementScale = Mathf.Clamp01(0.35f + speed * 0.65f);

            // Body
            p.BodyRot.x += (-8f * jumpEnvelope + 3f * jumpWave) * movementScale;
            p.BodyRot.y += 4f * _strafeDrag * jumpEnvelope;
            p.BodyRot.z += -3f * _strafeDrag * jumpEnvelope;
            p.BodyPos.y += Px(2f) * jumpEnvelope;
            p.BodyPos.z += Px(-1f * _forwardDrag) * jumpEnvelope;
            
            // Arms
            float armWave = Mathf.Cos(jumpT * Mathf.PI);
            p.RightArmRot.x += (-20f + 12f * armWave - 15f * _forwardDrag * movementScale) * movementScale;
            p.LeftArmRot.x  += (-20f - 12f * armWave - 15f * _forwardDrag * movementScale) * movementScale;

            p.RightArmRot.y += -10f * _strafeDrag * jumpEnvelope;
            p.LeftArmRot.y  +=  10f * _strafeDrag * jumpEnvelope;

            p.RightArmRot.z +=  12f * jumpEnvelope;
            p.LeftArmRot.z  += -12f * jumpEnvelope;

            p.RightArmPos.y += Px(-0.5f) * jumpEnvelope;
            p.LeftArmPos.y  += Px(-0.5f) * jumpEnvelope;
            p.RightArmPos.z += Px(1.5f * _forwardDrag) * jumpEnvelope;
            p.LeftArmPos.z  += Px(1.5f * _forwardDrag) * jumpEnvelope;
            
            // Legs
            float rightJumpWave = Mathf.Sin(jumpT * Mathf.PI);
            float leftJumpWave = Mathf.Sin(jumpT * Mathf.PI + Mathf.PI * 0.35f);

            p.RightLegRot.x += ( 12f - 20f * rightJumpWave) * movementScale;
            p.LeftLegRot.x  += (-8f  + 20f * leftJumpWave) * movementScale;

            p.RightLegRot.z +=  5f * _strafeDrag * movementScale;
            p.LeftLegRot.z  += -5f * _strafeDrag * movementScale;

            p.RightLegPos.y += Px(1.5f * rightJumpWave);
            p.LeftLegPos.y  += Px(1.5f * leftJumpWave);
            p.RightLegPos.z += Px(-1.5f * _forwardDrag * rightJumpWave);
            p.LeftLegPos.z  += Px(-1.5f * _forwardDrag * leftJumpWave);

            return p;
        }

        // ==================== FALL LAYER (Phase 3) ====================

        private FullPose ComputeFallLayer()
        {
            var p = FullPose.Zero;
            float fallSpeed = Mathf.Max(0f, -_verticalDrag);
            float fallIntensity = Mathf.InverseLerp(0.5f, 10f, fallSpeed);
            fallIntensity = Smooth01(fallIntensity);

            float fallPhase = _airElapsed * fallFrequency;
            float limbSpeed = Mathf.Clamp01(_player.HorizontalSpeed / Mathf.Max(_player.SprintSpeed, 0.01f));
            
            // Body
            p.BodyRot.x += -10f * (0.2f + 0.8f * limbSpeed) * fallIntensity;
            p.BodyRot.y += 7f * Mathf.Sin(fallPhase) * fallIntensity;
            p.BodyRot.z += -3f * Mathf.Cos(fallPhase) * fallIntensity;
            p.BodyPos.y += Px(-0.5f * fallIntensity);
            p.BodyPos.z += Px(1.5f * _forwardDrag * fallIntensity);
            
            // Arms
            float armSway = Mathf.Sin(fallPhase);
            p.RightArmRot.x += (-5f + 10f * limbSpeed + 8f * armSway) * fallIntensity;
            p.LeftArmRot.x  += (-5f + 10f * limbSpeed - 8f * armSway) * fallIntensity;

            p.RightArmRot.y += ( 30f * limbSpeed - 10f * armSway) * fallIntensity;
            p.LeftArmRot.y  += (-30f * limbSpeed - 10f * armSway) * fallIntensity;

            p.RightArmRot.z += fallArmSpread * fallIntensity;
            p.LeftArmRot.z  -= (fallArmSpread + 10f) * fallIntensity;

            p.RightArmPos.x += -Px(1f) * fallIntensity;
            p.LeftArmPos.x  +=  Px(1f) * fallIntensity;
            p.RightArmPos.y +=  Px(1f) * fallIntensity;
            p.LeftArmPos.y  +=  Px(1f) * fallIntensity;
            p.RightArmPos.z += -Px(0.5f * fallIntensity);
            p.LeftArmPos.z  += -Px(0.5f * fallIntensity);
            
            // Legs
            float rFallPhase = Mathf.Sin(fallPhase);
            float lFallPhase = Mathf.Sin(fallPhase + Mathf.PI * 0.25f);
            
            p.RightLegRot.x += (8f - 20f * fallIntensity + 5f * rFallPhase) * fallIntensity;
            p.LeftLegRot.x  += (8f - 20f * fallIntensity + 5f * lFallPhase) * fallIntensity;

            p.RightLegRot.y += 7f * rFallPhase * fallIntensity;
            p.LeftLegRot.y  -= 7f * lFallPhase * fallIntensity;

            p.RightLegRot.z += 5f * fallIntensity;
            p.LeftLegRot.z  -= 5f * fallIntensity;

            p.RightLegPos.x +=  Px(0.8f * fallIntensity);
            p.LeftLegPos.x  += -Px(0.8f * fallIntensity);
            p.RightLegPos.y += Px(1f) * fallIntensity;
            p.LeftLegPos.y  += Px(1f) * fallIntensity;
            p.RightLegPos.z += Px(-1f) * fallIntensity;
            p.LeftLegPos.z  += Px(-1f) * fallIntensity;

            return p;
        }

        // ==================== LANDING LAYER (Phase 3) ====================

        private FullPose ComputeLandingLayer()
        {
            var p = FullPose.Zero;
            float landT = Saturate(_landingElapsed / landingDuration);
            float impactEnvelope = 1f - Smooth01(landT);
            float impact = impactEnvelope * _landingImpactSpeed;
            
            // Body
            float landWave = Mathf.Sin(-Mathf.PI / 6f + landT * 4f);
            p.BodyRot.x += 5f * landWave * impact;
            p.BodyRot.z += 3f * Mathf.Cos(-Mathf.PI / 4f + landT * Mathf.PI) * impact;
            p.BodyPos.y += -Px(1.5f) * impact;
            p.BodyPos.z += Px(0.5f) * impact;
            
            // Arms
            p.LeftArmRot.x  += -5f * impact;
            p.RightArmRot.x += -5f * impact;
            p.LeftArmRot.z  += -8f * impact;
            p.RightArmRot.z +=  8f * impact;
            p.LeftArmPos.y  += -Px(1f) * impact;
            p.RightArmPos.y += -Px(1f) * impact;

            // Legs
            p.LeftLegRot.x  += -landingLegBend * impact;
            p.RightLegRot.x += -landingLegBend * impact;
            p.LeftLegPos.y  += -Px(1f) * impact;
            p.RightLegPos.y += -Px(1f) * impact;
            p.LeftLegPos.z  += Px(0.5f) * impact;
            p.RightLegPos.z += Px(0.5f) * impact;

            return p;
        }

        // ==================== TREAD WATER LAYER ====================

        private FullPose ComputeTreadWaterLayer()
        {
            var p = FullPose.Zero;
            float phase = _swimPhase;
            float wave = Mathf.Sin(phase + Mathf.Sin(phase) / 6f);

            p.BodyRot.x = 3.5f * wave;

            p.LeftArmRot.x = -24f * Mathf.Cos(PI / 5f + phase);
            p.RightArmRot.x = 24f * Mathf.Cos(PI / 5f + phase);

            p.LeftArmRot.y = -12f * Mathf.Cos(-PI / 5f + phase);
            p.RightArmRot.y = 12f * Mathf.Cos(-PI / 5f + phase);

            p.LeftArmRot.z = -15f - 10f * Mathf.Sin(phase);
            p.RightArmRot.z = 15f + 10f * Mathf.Sin(phase);

            p.LeftLegRot.x = 10f * wave;
            p.RightLegRot.x = -10f * wave;

            p.BodyPos.y = Px(0.8f * Mathf.Sin(phase * 2f));

            return p;
        }

        // ==================== SWIM MOVE LAYER ====================

        private FullPose ComputeSwimMoveLayer()
        {
            var p = FullPose.Zero;
            float phase = _swimPhase;
            float wave = Mathf.Sin(phase);

            p.BodyRot.x = -45f;

            p.LeftArmRot.x = -70f * wave;
            p.RightArmRot.x = 70f * wave;

            p.LeftArmRot.y = 24f * Mathf.Cos(-PI / 4f + phase);
            p.RightArmRot.y = 24f * Mathf.Cos(-PI / 4f + phase);

            p.LeftArmRot.z = -25f * wave;
            p.RightArmRot.z = 25f * wave;

            p.LeftLegRot.x = 18f * wave;
            p.RightLegRot.x = -18f * wave;

            p.BodyPos.z = Px(-1f * Mathf.Abs(_forwardDrag));

            return p;
        }

        // ==================== SWIM UP LAYER ====================

        private FullPose ComputeSwimUpLayer()
        {
            var p = FullPose.Zero;
            float up = Mathf.Clamp01(_player.CurrentVelocity.y / 4f);

            p.BodyRot.x = -35f * up;
            p.LeftArmRot.x = -35f * up;
            p.RightArmRot.x = -35f * up;
            p.LeftArmPos.y = Px(1f * up);
            p.RightArmPos.y = Px(1f * up);

            return p;
        }

        // ==================== FLY LAYER ====================

        private FullPose ComputeFlyLayer()
        {
            var p = FullPose.Zero;
            float speed = Mathf.Clamp01(_speedDrag);
            float turn = Mathf.Clamp(_smoothTurnAmount * 2f, -1f, 1f);
            float phase = _flyPhase;

            p.BodyRot.x = (3f * Mathf.Cos(Mathf.PI / 4f + phase) * speed - 15f * speed * Mathf.Min(0f, _forwardDrag));
            p.BodyRot.z += -turn * 8f;
            p.BodyRot.y += turn * 4f;
            
            Vector3 flyBodyRot = p.BodyRot;

            p.LeftArmRot.x = -7f * Mathf.Sin(phase) * speed + 30f * speed * _forwardDrag;
            p.RightArmRot.x = -7f * Mathf.Cos(phase) * speed + 30f * speed * _forwardDrag;

            float outwardYaw = 10f + 6f * speed;
            float sharedTurnYaw = turn * 5f;

            // Keep a guaranteed left/right yaw separation smoothly.
            p.LeftArmRot.y = -outwardYaw + sharedTurnYaw;
            p.RightArmRot.y = outwardYaw + sharedTurnYaw;

            float outwardRoll = 10f + 12f * speed;
            float bankRoll = turn * 8f;

            // Fluid roll, avoid strict clamping here so it doesn't look stiff
            p.LeftArmRot.z = -outwardRoll - bankRoll;
            p.RightArmRot.z = outwardRoll - bankRoll;

            p.LeftLegRot.x = 5f + 8f * Mathf.Cos(phase) * speed;
            p.RightLegRot.x = 5f + 8f * Mathf.Sin(phase) * speed;

            p.LeftLegRot.y = -10f - 5f * speed + sharedTurnYaw;
            p.RightLegRot.y = 10f + 5f * speed + sharedTurnYaw;

            // Bank the legs along with the body so they don't stay vertical and look disconnected
            // Also add outward roll (V-shape) so feet separate but hips stay attached
            float outwardLegRoll = 2f + 5f * speed;
            p.LeftLegRot.z = -bankRoll - outwardLegRoll;
            p.RightLegRot.z = -bankRoll + outwardLegRoll;
            
            // Share body rotation with legs to keep them connected during flight banking
            p.LeftLegRot += flyBodyRot;
            p.RightLegRot += flyBodyRot;

            p.BodyPos.y = Px(0.7f * Mathf.Sin(phase));

            float lateral = Px(0.4f + 0.3f * speed);
            p.LeftArmPos.x -= lateral;
            p.RightArmPos.x += lateral;
            
            float forwardReach = Px(0.5f * Mathf.Clamp01(Mathf.Abs(_forwardDrag)));
            p.LeftArmPos.z += forwardReach;
            p.RightArmPos.z += forwardReach;
            
            // Rotational position compensation for legs so they don't swing off the waist
            Vector3 flyLegOffset = new Vector3(
                -flyBodyRot.z * flyLegPositionCompensation * (1f / 16f), // Px is 1/16
                0f,
                flyBodyRot.x * flyLegPositionCompensation * (1f / 16f));

            p.LeftLegPos += flyLegOffset;
            p.RightLegPos += flyLegOffset;

            return p;
        }

        // ==================== ATTACK LAYER (Phase 3) ====================

        private string _combatLogOnce = "";

        private static bool UsesProceduralGroundPunch(string attackId) =>
            attackId == "fist_auto1" || attackId == "fist_auto2" || attackId == "fist_auto3";

        private void ApplyCombatAnimation(MineCraftUnity.Player.Combat.ConvertedAttackAnimation def, float elapsed)
        {
            float total = Mathf.Max(def.TotalDuration, 0.001f);
            float env = ComputeAttackEnvelope(elapsed, total);
            float norm = Mathf.Clamp01(elapsed / total);

            ResetStableCombatPivots();

            // Fallback: nếu asset chưa có track converted, dùng procedural tạm
            if (def.Tracks == null || def.Tracks.Count == 0)
            {
                if (_combatLogOnce != def.AttackId)
                {
                    _combatLogOnce = def.AttackId;
                    Debug.Log($"[CombatAnim] '{def.AttackId}' → PROCEDURAL fallback (Tracks=0)");
                }
                if (UsesProceduralGroundPunch(def.AttackId))
                {
                    ApplyProceduralGroundPunch(def.AttackId, norm, env);
                }
                return;
            }

            if (_combatLogOnce != def.AttackId)
            {
                _combatLogOnce = def.AttackId;
                Debug.Log($"[CombatAnim] '{def.AttackId}' → CONVERTED TRACKS (Tracks={def.Tracks.Count}, Duration={def.TotalDuration:F4})");
            }

            // Dùng dữ liệu converted tracks cho tất cả các đòn
            // Bước 1: Shoulder + Elbow (cánh tay)
            ApplyJointAnimation(def, elapsed, "LeftShoulderPivot", _leftArmPivot, env);
            ApplyJointAnimation(def, elapsed, "LeftElbowPivot", _leftElbowPivot, env);
            ApplyJointAnimation(def, elapsed, "RightShoulderPivot", _rightArmPivot, env);
            ApplyJointAnimation(def, elapsed, "RightElbowPivot", _rightElbowPivot, env);

            // Bước 2: Thigh + Knee (chân)
            ApplyJointAnimation(def, elapsed, "LeftThighPivot", _leftLegPivot, env);
            ApplyJointAnimation(def, elapsed, "LeftKneePivot", _leftKneePivot, env);
            ApplyJointAnimation(def, elapsed, "RightThighPivot", _rightLegPivot, env);
            ApplyJointAnimation(def, elapsed, "RightKneePivot", _rightKneePivot, env);

            // Bước 3: Torso, Chest, Head, Root
            // (Đã convert nhưng có thể cần disable tạm nếu bị lật ngược — bật từng cái để kiểm tra)
            ApplyJointAnimation(def, elapsed, "UpperBodyPivot", _upperBodyPivot, env);
            ApplyJointAnimation(def, elapsed, "ChestPivot", _chestPivot, env);
            ApplyJointAnimation(def, elapsed, "HeadPivot", _headPivot, env);
            // RootCombatPivot: giữ identity cho đòn đấm thường cho đến khi xác minh
            // ApplyJointAnimation(def, elapsed, "RootCombatPivot", _rootCombatPivot, env);
        }

        private static float ComputeAttackEnvelope(float elapsed, float total)
        {
            float fadeTime = 0.05f;
            float env = 1f;
            if (elapsed < fadeTime)
                env = elapsed / fadeTime;
            else if (elapsed > total - fadeTime)
                env = (total - elapsed) / fadeTime;
            return Smooth01(env);
        }

        private void ResetStableCombatPivots()
        {
            if (_rootCombatPivot != null)
            {
                _rootCombatPivot.localPosition = _rootBaseLocalPosition;
                _rootCombatPivot.localRotation = _rootBaseLocalRotation;
            }
            if (_chestPivot != null)
            {
                _chestPivot.localPosition = _chestBaseLocalPosition;
                _chestPivot.localRotation = _chestBaseLocalRotation;
            }
            if (_headPivot != null)
            {
                _headPivot.localPosition = _headPivotBaseLocalPosition;
                _headPivot.localRotation = _headPivotBaseLocalRotation;
            }
        }

        private void ApplyProceduralGroundPunch(string attackId, float norm, float env)
        {
            SampleProceduralGroundPunch(norm, attackId, out var shoulder, out var elbow, out var leftShoulder, out var bodyLean);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (norm <= 0.001f)
            {
                Debug.Assert(shoulder.sqrMagnitude < 0.001f && elbow.sqrMagnitude < 0.001f,
                    "[CombatAnim] First frame must be neutral pose.");
            }
#endif

            float blend = env;
            ApplyProceduralJoint(_rightArmPivot, _rightArmBaseLocalRotation, shoulder, blend);
            ApplyProceduralJoint(_rightElbowPivot, _rightElbowBaseLocalRotation, elbow, blend);
            ApplyProceduralJoint(_leftArmPivot, _leftArmBaseLocalRotation, leftShoulder, blend * 0.65f);

            if (_upperBodyPivot != null && bodyLean > 0.001f)
            {
                _upperBodyPivot.localRotation *= Quaternion.Euler(bodyLean * blend, 0f, 0f);
            }
        }

        private static void ApplyProceduralJoint(Transform pivot, Quaternion baseRot, Vector3 eulerOffset, float blend)
        {
            if (pivot == null || blend < 0.001f) return;
            var target = baseRot * Quaternion.Euler(eulerOffset);
            pivot.localRotation = Quaternion.Slerp(pivot.localRotation, target, blend);
        }

        /// <summary>
        /// Unity-space ground punch: wind-up -> extension -> hold -> recovery.
        /// Frame 0 is identity so the character stays in the standing pose.
        /// </summary>
        private static void SampleProceduralGroundPunch(
            float t, string attackId,
            out Vector3 shoulder, out Vector3 elbow, out Vector3 leftShoulder, out float bodyLean)
        {
            shoulder = Vector3.zero;
            elbow = Vector3.zero;
            leftShoulder = Vector3.zero;
            bodyLean = 0f;

            float yawBias = attackId switch
            {
                "fist_auto2" => -8f,
                "fist_auto3" => 10f,
                _ => 0f
            };

            if (t < 0.18f)
            {
                float w = t / 0.18f;
                shoulder = Vector3.Lerp(Vector3.zero, new Vector3(-28f, 6f + yawBias * 0.3f, -8f), w);
                elbow = Vector3.Lerp(Vector3.zero, new Vector3(58f, 0f, 0f), w);
                leftShoulder = Vector3.Lerp(Vector3.zero, new Vector3(0f, 0f, 10f), w);
            }
            else if (t < 0.42f)
            {
                float w = (t - 0.18f) / 0.24f;
                var windShoulder = new Vector3(-28f, 6f + yawBias * 0.3f, -8f);
                var strikeShoulder = new Vector3(92f, -10f + yawBias, 16f);
                shoulder = Vector3.Lerp(windShoulder, strikeShoulder, w);
                elbow = Vector3.Lerp(new Vector3(58f, 0f, 0f), new Vector3(8f, 0f, 0f), w);
                leftShoulder = Vector3.Lerp(new Vector3(0f, 0f, 10f), new Vector3(-12f, 4f, 8f), w);
                bodyLean = Mathf.Lerp(0f, 8f, w);
            }
            else if (t < 0.55f)
            {
                shoulder = new Vector3(92f, -10f + yawBias, 16f);
                elbow = new Vector3(8f, 0f, 0f);
                leftShoulder = new Vector3(-12f, 4f, 8f);
                bodyLean = 8f;
            }
            else
            {
                float w = (t - 0.55f) / 0.45f;
                shoulder = Vector3.Lerp(new Vector3(92f, -10f + yawBias, 16f), Vector3.zero, w);
                elbow = Vector3.Lerp(new Vector3(8f, 0f, 0f), Vector3.zero, w);
                leftShoulder = Vector3.Lerp(new Vector3(-12f, 4f, 8f), Vector3.zero, w);
                bodyLean = Mathf.Lerp(8f, 0f, w);
            }
        }

        private void GetBasePose(string jointName, out Vector3 pos, out Quaternion rot)
        {
            switch (jointName)
            {
                case "RootCombatPivot": pos = _rootBaseLocalPosition; rot = _rootBaseLocalRotation; break;
                case "UpperBodyPivot": pos = _upperBodyBaseLocalPosition; rot = _upperBodyBaseLocalRotation; break;
                case "ChestPivot": pos = _chestBaseLocalPosition; rot = _chestBaseLocalRotation; break;
                case "HeadPivot": pos = _headPivotBaseLocalPosition; rot = _headPivotBaseLocalRotation; break;
                case "LeftShoulderPivot": pos = _leftArmBaseLocalPosition; rot = _leftArmBaseLocalRotation; break;
                case "RightShoulderPivot": pos = _rightArmBaseLocalPosition; rot = _rightArmBaseLocalRotation; break;
                case "LeftElbowPivot": pos = _leftElbowBaseLocalPosition; rot = _leftElbowBaseLocalRotation; break;
                case "RightElbowPivot": pos = _rightElbowBaseLocalPosition; rot = _rightElbowBaseLocalRotation; break;
                case "LeftThighPivot": pos = _leftLegBaseLocalPosition; rot = _leftLegBaseLocalRotation; break;
                case "RightThighPivot": pos = _rightLegBaseLocalPosition; rot = _rightLegBaseLocalRotation; break;
                case "LeftKneePivot": pos = _leftKneeBaseLocalPosition; rot = _leftKneeBaseLocalRotation; break;
                case "RightKneePivot": pos = _rightKneeBaseLocalPosition; rot = _rightKneeBaseLocalRotation; break;
                default: pos = Vector3.zero; rot = Quaternion.identity; break;
            }
        }

        private void ApplyJointAnimation(MineCraftUnity.Player.Combat.ConvertedAttackAnimation def, float elapsed, string jointName, Transform target, float env)
        {
            if (target == null) return;
            var track = def.GetTrack(jointName);
            if (track == null || track.Times == null || track.Times.Length == 0) return;

            float[] times = track.Times;
            
            Vector3 finalDeltaP = Vector3.zero;
            Quaternion finalDeltaR = Quaternion.identity;

            if (elapsed <= times[0])
            {
                finalDeltaP = track.PositionDeltas[0];
                finalDeltaR = track.RotationDeltas[0];
            }
            else if (elapsed >= times[times.Length - 1])
            {
                finalDeltaP = track.PositionDeltas[times.Length - 1];
                finalDeltaR = track.RotationDeltas[times.Length - 1];
            }
            else
            {
                for (int i = 0; i < times.Length - 1; i++)
                {
                    if (elapsed >= times[i] && elapsed <= times[i + 1])
                    {
                        float t = (elapsed - times[i]) / (times[i + 1] - times[i]);
                        finalDeltaP = Vector3.Lerp(track.PositionDeltas[i], track.PositionDeltas[i + 1], t);
                        finalDeltaR = Quaternion.Slerp(track.RotationDeltas[i], track.RotationDeltas[i + 1], t);
                        break;
                    }
                }
            }

            GetBasePose(jointName, out var bPos, out var bRot);
            target.localPosition = Vector3.Lerp(target.localPosition, bPos + finalDeltaP, env);
            target.localRotation = Quaternion.Slerp(target.localRotation, bRot * finalDeltaR, env);
        }

        // ==================== HEAD LAYER ====================

        private void ComputeHeadLayer(ref FullPose pose)
        {
            if (_player == null) return;

            // Head pitch (looking up/down) — smooth drag
            float rawPitch = _player.CameraPitch;
            _headPitchDrag = ExpSmooth(_headPitchDrag, rawPitch, headDragSpeed);
            float pitchDeg = Mathf.Clamp(_headPitchDrag * headPitchScale, -maxHeadPitch, maxHeadPitch);

            // Head yaw — reacts to body turning (uses turn amount, NOT CameraPitch)
            float yawDeg = Mathf.Clamp(-_smoothTurnAmount * 12f, -maxHeadYaw, maxHeadYaw);

            // Head roll — FA+: -7*clamp(headyaw_speed*2) + strafe contribution
            float rollDeg = Mathf.Clamp(
                -7f * Mathf.Clamp(_smoothTurnAmount * 2f, -1f, 1f) - _strafeDrag * 3f,
                -maxHeadRoll, maxHeadRoll);

            // Phase 3 Vertical Response
            pitchDeg += -4f * _jumpWeight;
            pitchDeg +=  3f * _fallWeight;
            pitchDeg +=  5f * _landWeight;
            pitchDeg +=  5f * _treadWeight;
            pitchDeg += -8f * _swimMoveWeight;
            pitchDeg += -12f * _flyWeight * Mathf.Abs(_forwardDrag);
            
            pose.HeadPos.y +=  Px(0.5f) * _jumpWeight;
            pose.HeadPos.y += -Px(0.5f) * _fallWeight;
            pose.HeadPos.y += -Px(1.5f) * _landWeight;

            // Head is set directly, not additive (it has its own clamping)
            pose.HeadRot = new Vector3(pitchDeg, yawDeg, rollDeg);
        }

        // ==================== CLAMP ====================

        private void ClampPose(ref FullPose pose)
        {
            // Body — prevent extreme combined rotations
            pose.BodyRot.x = Mathf.Clamp(pose.BodyRot.x, -60f, 60f);
            pose.BodyRot.y = Mathf.Clamp(pose.BodyRot.y, -40f, 40f);
            pose.BodyRot.z = Mathf.Clamp(pose.BodyRot.z, -30f, 30f);

            // Arms — generic limits
            pose.LeftArmRot.x  = Mathf.Clamp(pose.LeftArmRot.x,  -160f, 160f);
            pose.RightArmRot.x = Mathf.Clamp(pose.RightArmRot.x, -160f, 160f);
            pose.LeftArmRot.z  = Mathf.Clamp(pose.LeftArmRot.z,  -120f, 120f);
            pose.RightArmRot.z = Mathf.Clamp(pose.RightArmRot.z, -120f, 120f);
            
            // Soft anti-cross logic for flight (prevent entering chest, but don't hard lock)
            if (_flyWeight > 0.1f)
            {
                float limitZ = 2f * _flyWeight;
                pose.LeftArmRot.z = Mathf.Min(pose.LeftArmRot.z, -limitZ);
                pose.RightArmRot.z = Mathf.Max(pose.RightArmRot.z, limitZ);
                
                float limitY = 15f + 20f * (1f - _flyWeight); // allow more freedom when transitioning
                pose.LeftArmRot.y = Mathf.Clamp(pose.LeftArmRot.y, -45f, limitY);
                pose.RightArmRot.y = Mathf.Clamp(pose.RightArmRot.y, -limitY, 45f);
            }

            // Legs
            pose.LeftLegRot.x  = Mathf.Clamp(pose.LeftLegRot.x,  -90f, 90f);
            pose.RightLegRot.x = Mathf.Clamp(pose.RightLegRot.x, -90f, 90f);

            // Head already clamped in ComputeHeadLayer
        }

        // ==================== WRITE ====================

        private void WritePose(in FullPose pose)
        {
            ResetStableCombatPivots();

            float dt = Time.deltaTime;
            // FPS-independent smoothing rates
            float bodyRate = 1f - Mathf.Exp(-bodyRotSmooth * dt);
            float limbRate = 1f - Mathf.Exp(-limbRotSmooth * dt);
            float headRate = 1f - Mathf.Exp(-headRotSmooth * dt);
            float posRate  = 1f - Mathf.Exp(-positionSmooth * dt);

            // Body bounce/offset must also affect legs (they're NOT children of UpperBodyPivot)
            Vector3 legBodyOffset = new Vector3(0f, pose.BodyPos.y, pose.BodyPos.z);

            // UpperBodyPivot (body rotation + position)
            SmoothWrite(_upperBodyPivot, pose.BodyRot, pose.BodyPos, bodyRate, posRate);

            // Arms (children of UpperBodyPivot — inherit body transform, add own offset)
            SmoothWrite(_leftArmPivot, pose.LeftArmRot,
                _baseLArmPos + pose.LeftArmPos, limbRate, posRate);
            SmoothWrite(_rightArmPivot, pose.RightArmRot,
                _baseRArmPos + pose.RightArmPos, limbRate, posRate);

            // Legs (independent of UpperBodyPivot — need body offset to stay attached)
            SmoothWrite(_leftLegPivot, pose.LeftLegRot,
                _baseLLegPos + pose.LeftLegPos + legBodyOffset, limbRate, posRate);
            SmoothWrite(_rightLegPivot, pose.RightLegRot,
                _baseRLegPos + pose.RightLegPos + legBodyOffset, limbRate, posRate);

            // Head
            if (_head != null)
            {
                Quaternion targetRot = Quaternion.Euler(pose.HeadRot);
                _head.localRotation = Quaternion.Slerp(_head.localRotation, targetRot, headRate);
            }
            // Zero out auxiliary joints that Locomotion doesn't manage
            if (_chestPivot != null) _chestPivot.localRotation = Quaternion.identity;
            if (_leftElbowPivot != null) _leftElbowPivot.localRotation = Quaternion.identity;
            if (_rightElbowPivot != null) _rightElbowPivot.localRotation = Quaternion.identity;
            if (_leftKneePivot != null) _leftKneePivot.localRotation = Quaternion.identity;
            if (_rightKneePivot != null) _rightKneePivot.localRotation = Quaternion.identity;

            // Apply Combat Animation
            // (Removed duplicate call to ApplyCombatAnimation here as it is already called in LateUpdate)

            // Character Panel left arm override (protect panel view from combat and locomotion)
            if (CharacterPanelWeight > 0.001f && _leftArmPivot != null)
            {
                _leftArmPivot.localRotation = Quaternion.Slerp(_leftArmPivot.localRotation, Quaternion.Euler(-90f, 45f, 0f), CharacterPanelWeight);
                if (_leftElbowPivot != null)
                {
                    _leftElbowPivot.localRotation = Quaternion.Slerp(_leftElbowPivot.localRotation, Quaternion.identity, CharacterPanelWeight);
                }
            }
        }

        private static void SmoothWrite(Transform t, Vector3 rotDeg, Vector3 pos,
            float rotRate, float posRate)
        {
            if (t == null) return;
            t.localRotation = Quaternion.Slerp(t.localRotation, Quaternion.Euler(rotDeg), rotRate);
            t.localPosition = Vector3.Lerp(t.localPosition, pos, posRate);
        }

        // ==================== DEBUG ====================

#if UNITY_EDITOR
        private void OnGUI()
        {
            if (_player == null) return;
            if (!UnityEngine.InputSystem.Keyboard.current.f3Key.isPressed) return;

            GUILayout.BeginArea(new Rect(10, 10, 420, 420));
            var style = new GUIStyle(GUI.skin.label) { fontSize = 13 };

            GUILayout.Label($"State: {CurrentState}  Grounded: {_player.IsGrounded}", style);
            GUILayout.Label($"MoveInput: {_player.MoveInput}", style);
            GUILayout.Label($"HSpeed: {_player.HorizontalSpeed:F2}  VSpeed: {_player.CurrentVelocity.y:F2}  Sprint: {_player.IsSprinting}", style);
            GUILayout.Space(5);

            GUILayout.Label("── Blend Weights ──", style);
            GUILayout.Label($"idle={_idleWeight:F2}  walk={_walkWeight:F2}  back={_backwardWeight:F2}  sprint={_sprintWeight:F2}  strafe={_strafeWeight:F2}", style);
            GUILayout.Label($"jump={_jumpWeight:F2}  fall={_fallWeight:F2}  land={_landWeight:F2}", style);
            GUILayout.Label($"tread={_treadWeight:F2}  swim={_swimMoveWeight:F2}  swimUp={_swimUpWeight:F2}  fly={_flyWeight:F2}  inAir={_inAirWeight:F2}", style);
            GUILayout.Space(5);

            GUILayout.Label("── Phase 3 Air Timers ──", style);
            GUILayout.Label($"AirTime: {_airElapsed:F2}s  Jump: {_jumpElapsed:F2}s  Land: {_landingElapsed:F2}s", style);
            GUILayout.Label($"LandImpact: {_landingImpactSpeed:F2}", style);
            GUILayout.Space(5);

            GUILayout.Label("── Input Drag ──", style);
            GUILayout.Label($"fwd={_forwardDrag:F2}  strafe={_strafeDrag:F2}  speed={_speedDrag:F2}  vert={_verticalDrag:F2}", style);
            GUILayout.Label($"turn={_smoothTurnAmount:F3}", style);
            GUILayout.Space(5);

            GUILayout.Label($"Phase: {_groundPhase:F2}", style);
            GUILayout.Label($"Head: pitch={_headPitchDrag * headPitchScale:F1} yaw={-_smoothTurnAmount * 12f:F1}", style);
            GUILayout.EndArea();
        }
#endif
    }
}
