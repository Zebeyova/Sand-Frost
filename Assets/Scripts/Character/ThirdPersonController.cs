using Base;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Character
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInputs))]
#if ENABLE_INPUT_SYSTEM
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class ThirdPersonController : MonoBehaviour
    {
        public enum CharacterState
        {
            Idle,
            Walk,
            Run,
            Jump,
            Fall
        }

        private const float _threshold = 0.01f;

        [Header("Code Driven Animation")]
        [Tooltip("The Animator on the character model. No Animator Controller is required.")]
        public Animator CharacterAnimator;
        public AnimationClip IdleAnimation;
        public AnimationClip WalkAnimation;
        public AnimationClip RunAnimation;
        public AnimationClip JumpAnimation;
        [Tooltip("Optional. Without a fall clip, hold the jump animation's airborne pose.")]
        public AnimationClip FallAnimation;
        [Min(0f)] public float AnimationBlendDuration = 0.15f;
        [Range(0f, 1f)] public float JumpAirbornePose = 0.5f;

        [Header("Player")]
        [Tooltip("Move speed of the character in m/s")]
        public float MoveSpeed = 2.0f;

        [Tooltip("Sprint speed of the character in m/s")]
        public float SprintSpeed = 5.335f;

        [Tooltip("How fast the character turns to face movement direction")]
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;

        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;

        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        [Space(10)]
        [Tooltip("The height the player can jump")]
        public float JumpHeight = 1.2f;

        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
        public float JumpTimeout = 0.50f;

        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;

        [Tooltip("Useful for rough ground")] public float GroundedOffset = -0.14f;

        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.28f;

        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        [Header("Cinemachine")] public GameObject CinemachineCameraTarget;

        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 70.0f;

        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -30.0f;

        [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
        public float CameraAngleOverride;

        [Tooltip("For locking the camera position on all axis")]
        public bool LockCameraPosition;
        private ThirdPersonAnimationPlayer _animationPlayer;
        private float _cinemachineTargetPitch;

        // cinemachine
        private float _cinemachineTargetYaw;
        private CharacterController _controller;
        private float _fallTimeoutDelta;
        private PlayerInputs _input;
        private float _jumpTakeoffVelocity;

        // timeout deltatime
        private float _jumpTimeoutDelta;
        private GameObject _mainCamera;

#if ENABLE_INPUT_SYSTEM
        private PlayerInput _playerInput;
#endif
        private float _rotationVelocity;

        // player
        private float _speed;

        private bool _started;
        private float _targetRotation;
        private readonly float _terminalVelocity = 53.0f;
        private float _verticalVelocity;

        public CharacterState CurrentState { get; private set; } = CharacterState.Idle;

        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return _playerInput != null && _playerInput.currentControlScheme == "KeyboardMouse";
#else
				return false;
#endif
            }
        }

        private void OnEnable()
        {
            if (_started) InitializeAnimation();
        }

        private void Awake()
        {
            // get a reference to our main camera
            if (_mainCamera == null) _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        }

        private void Start()
        {
            if (CinemachineCameraTarget != null) _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;

            _controller = GetComponent<CharacterController>();
            _input = GetComponent<PlayerInputs>();
#if ENABLE_INPUT_SYSTEM
            _playerInput = GetComponent<PlayerInput>();
#else
			Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

            // reset our timeouts on start
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
            _started = true;
            InitializeAnimation();
        }

        private void Update()
        {
            GroundedCheck();
            JumpAndGravity();
            Move();
            GroundedCheck();
            UpdateCharacterState();

            float jumpProgress = _jumpTakeoffVelocity > 0f ? Mathf.Clamp01(1f - _verticalVelocity / _jumpTakeoffVelocity) : 1f;
            _animationPlayer?.Tick(Time.deltaTime, jumpProgress);
        }

        private void LateUpdate()
        {
            CameraRotation();
        }


        private void OnDisable()
        {
            _animationPlayer?.Dispose();
            _animationPlayer = null;
        }

        private void OnDrawGizmosSelected()
        {
            var transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            var transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            Gizmos.color = Grounded ? transparentGreen : transparentRed;

            // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
            Gizmos.DrawSphere(
                new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
                GroundedRadius);
        }

        private void InitializeAnimation()
        {
            if (CharacterAnimator == null)
                // Prefer the model's Animator over an obsolete Animator on the player root.
                foreach (Animator candidate in GetComponentsInChildren<Animator>())
                {
                    if (candidate.enabled)
                    {
                        CharacterAnimator = candidate;
                        if (candidate.gameObject != gameObject) break;
                    }
                }

            if (CharacterAnimator == null) return; // Capsule-only players can still move.
            if (IdleAnimation == null || WalkAnimation == null || RunAnimation == null || JumpAnimation == null)
            {
                Debug.LogWarning("[ThirdPersonController] Assign Idle, Walk, Run and Jump animation clips to enable code driven animation.", this);
                return;
            }

            _animationPlayer = new ThirdPersonAnimationPlayer(CharacterAnimator,
                IdleAnimation, WalkAnimation, RunAnimation, JumpAnimation, FallAnimation,
                AnimationBlendDuration, JumpAirbornePose);
            _animationPlayer.SetState(CurrentState);
        }

        private void UpdateCharacterState()
        {
            if (!Grounded)
            {
                if (_verticalVelocity > 0f)
                    CurrentState = CharacterState.Jump;
                else if (CurrentState == CharacterState.Jump || _fallTimeoutDelta <= 0f)
                    CurrentState = CharacterState.Fall;
                // Brief loss of contact on steps keeps the previous locomotion state.
            }
            else
            {
                bool wasAirborne = CurrentState == CharacterState.Jump || CurrentState == CharacterState.Fall;
                CurrentState = _speed < 0.05f
                    ? CharacterState.Idle
                    : _input.sprint
                        ? CharacterState.Run
                        : CharacterState.Walk;
                if (wasAirborne) PlayLandingAudio();
            }

            _animationPlayer?.SetState(CurrentState);
        }

        private void GroundedCheck()
        {
            // set sphere position, with offset
            var spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
                transform.position.z);
            // The probe may still touch the floor during takeoff; upward motion is airborne.
            Grounded = _verticalVelocity <= 0f && Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
                QueryTriggerInteraction.Ignore);
        }

        private void CameraRotation()
        {
            if (CinemachineCameraTarget == null) return;

            // if there is an input and camera position is not fixed
            if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
            {
                //Don't multiply mouse input by Time.deltaTime;
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier;
                _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier;
            }

            // clamp our rotations so our values are limited 360 degrees
            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            // Cinemachine will follow this target
            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
                _cinemachineTargetYaw, 0.0f);
        }

        private void Move()
        {
            // set target speed based on move speed, sprint speed and if sprint is pressed
            float inputMagnitude = _input.analogMovement ? Mathf.Clamp01(_input.move.magnitude) : 1f;
            float targetSpeed = (_input.sprint ? SprintSpeed : MoveSpeed) * inputMagnitude;

            // a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

            // note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is no input, set the target speed to 0
            if (_input.move == Vector2.zero) targetSpeed = 0.0f;

            // a reference to the players current horizontal velocity
            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

            float speedOffset = 0.1f;

            // accelerate or decelerate to target speed
            if (currentHorizontalSpeed < targetSpeed - speedOffset ||
                currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                // creates curved result rather than a linear one giving a more organic speed change
                // note T in Lerp is clamped, so we don't need to clamp our speed
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed,
                    Time.deltaTime * SpeedChangeRate);

                // round speed to 3 decimal places
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
                _speed = targetSpeed;

            // normalize input direction
            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

            // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is a move input rotate player when the player is moving
            if (_input.move != Vector2.zero)
            {
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                                  (_mainCamera != null ? _mainCamera.transform.eulerAngles.y : 0f);
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                    RotationSmoothTime);

                // rotate to face input direction relative to camera position
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }


            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            // move the player
            CollisionFlags collisions = _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) +
                                                         new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
            if ((collisions & CollisionFlags.Above) != 0 && _verticalVelocity > 0f)
                _verticalVelocity = 0f;
        }

        private void JumpAndGravity()
        {
            if (Grounded)
            {
                // reset the fall timeout timer
                _fallTimeoutDelta = FallTimeout;

                // stop our velocity dropping infinitely when grounded
                if (_verticalVelocity < 0.0f)
                    _verticalVelocity = -2f;

                // Jump
                if (_input.jump && _jumpTimeoutDelta <= 0.0f)
                {
                    // the square root of H * -2 * G = how much velocity needed to reach desired height
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                    _jumpTakeoffVelocity = _verticalVelocity;
                    _input.jump = false;
                    Grounded = false;
                    _jumpTimeoutDelta = JumpTimeout;
                }

                // jump timeout
                if (_jumpTimeoutDelta >= 0.0f)
                    _jumpTimeoutDelta -= Time.deltaTime;
            }
            else
            {
                // reset the jump timeout timer
                _jumpTimeoutDelta = JumpTimeout;

                // fall timeout
                if (_fallTimeoutDelta >= 0.0f)
                    _fallTimeoutDelta -= Time.deltaTime;
                // if we are not grounded, do not jump
                _input.jump = false;
            }

            // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
            _verticalVelocity = Mathf.Max(_verticalVelocity + Gravity * Time.deltaTime, -_terminalVelocity);
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
                if (FootstepAudioClips is { Length: > 0 })
                {
                    int index = Random.Range(0, FootstepAudioClips.Length);
                    AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center),
                        FootstepAudioVolume);
                }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
                PlayLandingAudio();
        }

        private void PlayLandingAudio()
        {
            if (LandingAudioClip)
                AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center),
                    FootstepAudioVolume);
        }
    }
}
