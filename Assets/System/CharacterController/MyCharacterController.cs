using System;
using KinematicCharacterController;
using Movement;
using PrimeTween;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

public partial class MyCharacterController : MonoBehaviour, ICharacterController
{
	#region References

	[Header("References")]
	[SerializeField]
	private KinematicCharacterMotor _motor;

	[SerializeField]
	private GameObject _meshRoot;

	#endregion

	#region Settings

	[TabGroup("Settings", "Movement")]
	[Header("Stable Movement")]
	[Tooltip("The player's base movespeed")]
	[SerializeField]
	private float _baseMovespeed = 8f;

	[TabGroup("Settings", "Movement")]
	[Tooltip("How fast the player accelerates while on stable ground")]
	[SerializeField]
	private float _stableAcceleration = 15f;

	[TabGroup("Settings", "Movement")]
	[Tooltip("How fast the player decelerate to a certain speed when inputting")]
	[SerializeField]
	private float _stableDeceleration = 1.2f;

	[TabGroup("Settings", "Movement")]
	[Tooltip("How fast the player decelerate while on stable ground and not inputting")]
	[SerializeField]
	private float _stableDecelerationToStop = 15f;

	[TabGroup("Settings", "Movement")]
	[Header("Sprinting")]
	[Tooltip("How fast the player moves while sprinting relative to their base movespeed")]
	[SerializeField]
	private float _sprintSpeedMult = 1f;

	[TabGroup("Settings", "Movement")]
	[Tooltip("How fast the player accelerates to sprint speed")]
	[SerializeField]
	private float _sprintAcceleration = 3f;

	[TabGroup("Settings", "Movement")]
	[Tooltip("Time it takes for a player to start sprinting after being above the running threshold")]
	[SerializeField]
	private float _timeTilSprint = 0.01f;

	[TabGroup("Settings", "Movement")]
	[Tooltip("Time it takes for the sprint to expire after being below the running threshold")]
	[SerializeField]
	private float _sprintExpireTime = 0.1f;

	[TabGroup("Settings", "Movement")]
	[Tooltip("Velocity threshold that the player has to surpass to start sprinting")]
	[SerializeField]
	private float _runningThreshold = 5f;

	[TabGroup("Settings", "Movement")]
	[Header("Air Movement")]
	[Tooltip("How fast the player moves while in the air relative to their base movespeed")]
	[SerializeField]
	private float _airBaseSpeedMult = 1f;

	[TabGroup("Settings", "Movement")]
	[Tooltip("How fast the player accelerates up to air base speed")]
	[SerializeField]
	private float _airAcceleration = 7f;

	[TabGroup("Settings", "Movement")]
	[Tooltip("How fast the player decelerates up to air base speed")]
	[SerializeField]
	private float _airDeceleration = 0.5f;

	[TabGroup("Settings", "Movement")]
	[Tooltip("How easily the player is able to change direction in the air")]
	[SerializeField]
	private float _airControlRate = 5f;

	[TabGroup("Settings", "Movement")]
	[Tooltip("Air drag")]
	[SerializeField]
	private float _drag = 0.01f;

	[TabGroup("Settings", "Sliding")]
	[Header("Sliding")]
	[Tooltip("Toggle the slide")]
	[SerializeField]
	private bool _slideToggle = true;

	[TabGroup("Settings", "Sliding")]
	[ShowIf("_slideToggle")]
	[Tooltip("How much the player's current speed is multiplied when they slide after landing on the ground")]
	[Range(0f, 1f)]
	[SerializeField]
	private float _slideConditionalSpeedMult = 0.05f;

	[TabGroup("Settings", "Sliding")]
	[ShowIf("_slideToggle")]
	[Tooltip("Upper limit to how much speed the slide can give")]
	[SerializeField]
	private float _slideForceMax = 2f;

	[TabGroup("Settings", "Sliding")]
	[ShowIf("_slideToggle")]
	[Tooltip("How long after landing on the ground, can the player get the slide speed mult")]
	[SerializeField]
	private float _slideSpeedMultBuffer = 0.1f;

	[TabGroup("Settings", "Sliding")]
	[ShowIf("_slideToggle")]
	[Tooltip("The max speed a slide can have when going down a hill relative to the base speed")]
	[SerializeField]
	private float _slideSlopeSpeedMult = 4f;

	[TabGroup("Settings", "Sliding")]
	[ShowIf("_slideToggle")]
	[Tooltip("How long the player is slide for after initiating a slide")]
	[SerializeField]
	private float _slideStunDuration = 0.6f;

	[TabGroup("Settings", "Sliding")]
	[ShowIf("_slideToggle")]
	[Tooltip("Velocity threshold needed to enter a slide")]
	[SerializeField]
	private float _slideSpeedThreshold = 10f;

	[TabGroup("Settings", "Sliding")]
	[ShowIf("_slideToggle")]
	[Tooltip("Velocity threshold needed to exit a slide")]
	[SerializeField]
	private float _slideSpeedExitThreshold = 6f;

	[TabGroup("Settings", "Sliding")]
	[ShowIf("_slideToggle")]
	[Tooltip("How much the player is allowed to redirect their movement while sliding")]
	[SerializeField]
	private float _slidingRotationSmoothing = 2f;

	[TabGroup("Settings", "Sliding")]
	[ShowIf("_slideToggle")]
	[Tooltip("How smooth the slide slow down is")]
	[SerializeField]
	private float _slidingDragSmoothing = 3f;

	[TabGroup("Settings", "Sliding")]
	[ShowIf("_slideToggle")]
	[Tooltip("How fast the slide increases in speed down a slope")]
	[SerializeField]
	private float _slideBuildUpSmoothing = 1f;

	[TabGroup("Settings", "Sliding")]
	[ShowIf("_slideToggle")]
	[Tooltip("The percentage of velocity slowdown on the slide")]
	[SerializeField]
	private float _slideSlowDown = 0.85f;

	[TabGroup("Settings", "Crouch")]
	[Header("Crouching")]
	[Tooltip("Toggle to allow crouching")]
	[SerializeField]
	private bool _crouchToggle = true;

	[TabGroup("Settings", "Crouch")]
	[ShowIf("_crouchToggle")]
	[Tooltip("How fast the player moves while in crouching relative to their base movespeed")]
	[SerializeField]
	private float _crouchSpeedMult = 0.5f;

	[TabGroup("Settings", "Crouch")]
	[ShowIf("_crouchToggle")]
	[Tooltip("How long it takes for the player to crouch and uncrouch")]
	[SerializeField]
	private float _crouchTransitionTime = 0.2f;

	[TabGroup("Settings", "Crouch")]
	[ShowIf("_crouchToggle")]
	[Tooltip("The percentage of the player's height when crouching")]
	[Range(0f, 1f)]
	[SerializeField]
	private float _crouchCapsuleHeightMult = 0.5f;

	[TabGroup("Settings", "Crouch")]
	[ShowIf("_crouchToggle")]
	[Tooltip("The percentage of the player's YOffset when crouching")]
	[Range(0f, 1f)]
	[SerializeField]
	private float _crouchCapsuleYOffsetMult = 0.5f;

	[TabGroup("Settings", "Dash")]
	[Header("Dashes")]
	[Tooltip("How much velocity to increase at the start of a dash")]
	[SerializeField]
	private float _dashForce = 30f;

	[TabGroup("Settings", "Dash")]
	[Tooltip("How much velocity to decrease after a dash relative to dash force")]
	[Range(0f, 1f)]
	[SerializeField]
	private float _dashVelocityToDecreaseMult = 0.45f;

	[TabGroup("Settings", "Dash")]
	[Range(0f, 1f)]
	[Tooltip("How much the player's velocity is decreased after a dash")]
	[SerializeField]
	private float _dashEndVelocityMult = 0.65f;

	[TabGroup("Settings", "Dash")]
	[Tooltip("How long the player is stuck moving in a certain direction")]
	[SerializeField]
	private float _dashStun = 0.2f;

	[TabGroup("Settings", "Dash")]
	[Tooltip("The time allowed in between dashes")]
	[SerializeField]
	private float _dashCooldown = 0.5f;

	[TabGroup("Settings", "Dash")]
	[Tooltip("A buffer for the dash input")]
	[SerializeField]
	private float _dashBuffer = 0.2f;

	[TabGroup("Settings", "Dash")]
	[Tooltip("The amount of air dashes allowed while in the air")]
	[SerializeField]
	private int _airDashLimit = 2;

	[TabGroup("Settings", "Dash")]
	[Header("Downward Dash")]
	[Tooltip("How much velocity downwards during a downwards dash")]
	[SerializeField]
	private float _downwardsDashForce = 25f;

	[TabGroup("Settings", "Dash")]
	[Tooltip("How long the player is stuck moving in a certain direction")]
	[SerializeField]
	private float _downwardsDashStun = 0.8f;

	[TabGroup("Settings", "Dash")]
	[Tooltip("A buffer for the downwards dash input")]
	[SerializeField]
	private float _downwardsDashBuffer = 0.5f;

	[TabGroup("Settings", "Dash")]
	[Tooltip("The amount of air dashes allowed while in the air")]
	[SerializeField]
	private int _downwardsDashLimit = 4;

	[TabGroup("Settings", "Jump")]
	[Header("Jumps")]
	[Tooltip("Toggle Jumping")]
	[SerializeField]
	private bool _toggleJump = true;

	[TabGroup("Settings", "Jump")]
	[ShowIf("_toggleJump")]
	[Tooltip("Allow jumps on steep slopes")]
	[SerializeField]
	private bool _allowJumpWhileSliding = true;

	[TabGroup("Settings", "Jump")]
	[ShowIf("_toggleJump")]
	[Tooltip("How high the player jumps")]
	[SerializeField]
	private float _jumpForce = 10f;

	[TabGroup("Settings", "Jump")]
	[ShowIf("_toggleJump")]
	[Tooltip(
		"How much the player's horizontal velocity is slowed down after landing, 0 to max slow down and 1 to have no slowdown"
	)]
	[Range(0f, 1f)]
	[SerializeField]
	private float _landingSlowDownMult = 0.85f;

	[TabGroup("Settings", "Jump")]
	[ShowIf("_toggleJump")]
	[Tooltip("How high the player jumps in the air")]
	[SerializeField]
	private float _airJumpForce = 14f;

	[TabGroup("Settings", "Jump")]
	[ShowIf("_toggleJump")]
	[Tooltip("A buffer for the jump input")]
	[SerializeField]
	private float _jumpBuffer = 0.3f;

	[TabGroup("Settings", "Jump")]
	[ShowIf("_toggleJump")]
	[Tooltip("Time it takes to jump again")]
	[SerializeField]
	private float _jumpCooldown = 0.05f;

	[TabGroup("Settings", "Jump")]
	[ShowIf("_toggleJump")]
	[Tooltip("Time after leaving stable ground where the player can still jump")]
	[SerializeField]
	private float _coyoteTime = 0.15f;

	[TabGroup("Settings", "Jump")]
	[ShowIf("_toggleJump")]
	[Tooltip("Time at the apex of a player's jump where gravity is reduced")]
	[SerializeField]
	private float _jumpHangInterval = 0.3f;

	[TabGroup("Settings", "Jump")]
	[ShowIf("_toggleJump")]
	[Tooltip("The number of jumps the player has")]
	[SerializeField]
	private int _maxJumps = 2;

	[TabGroup("Settings", "Wall")]
	[Header("General")]
	[Tooltip("How close to a wall a player has to be to start wall running")]
	[SerializeField]
	private float _wallCheckDistance = 0.5f;

	[TabGroup("Settings", "Wall")]
	[Header("Wall Running")]
	[Tooltip("Disable or Enable Wall Running")]
	[SerializeField]
	private bool _allowWallRun = true;

	[TabGroup("Settings", "Wall")]
	[Tooltip("How much of the player's current horizontal velocity is increased after entering a wall run")]
	[Range(0f, 2f)]
	[ShowIf("_allowWallRun")]
	[SerializeField]
	private float _wallRunInitialHorVelocityMult = 0.5f;

	[TabGroup("Settings", "Wall")]
	[Tooltip("How much of the player's current vertical velocity is decreased after entering a wall run")]
	[Range(0f, 1f)]
	[ShowIf("_allowWallRun")]
	[SerializeField]
	private float _wallRunInitialVertVelocityMult = 0.5f;

	[TabGroup("Settings", "Wall")]
	[Tooltip("Limits how much of the player's current velocity is increased after entering a wall run")]
	[ShowIf("_allowWallRun")]
	[SerializeField]
	private float _wallRunInitialVelocityMax = 6f;

	[TabGroup("Settings", "Wall")]
	[Tooltip("How fast the player has to be to start wall running")]
	[ShowIf("_allowWallRun")]
	[SerializeField]
	private float _wallRunVelocityThreshold = 11f;

	[TabGroup("Settings", "Wall")]
	[Tooltip("How slow the player is before they are kicked out of a wall run")]
	[ShowIf("_allowWallRun")]
	[SerializeField]
	private float _wallRunVelocityExitThreshold = 6f;

	[TabGroup("Settings", "Wall")]
	[Tooltip("How far up the ground the player has to be to wall run")]
	[ShowIf("_allowWallRun")]
	[SerializeField]
	private float _wallRunHeightThreshold = 2f;

	[TabGroup("Settings", "Wall")]
	[Tooltip("How much velocity is taken away over time when the player wall runs")]
	[ShowIf("_allowWallRun")]
	[SerializeField]
	private float _wallRunDragSmoothing = 0.5f;

	[TabGroup("Settings", "Wall")]
	[Tooltip("Gravity while wall running")]
	[ShowIf("_allowWallRun")]
	[SerializeField]
	private Vector3 _wallRunGravity = new(0, -5f, 0);

	[TabGroup("Settings", "Wall")]
	[Header("Wall Jumps")]
	[Tooltip("Disable or Enable Wall Jumping")]
	[SerializeField]
	private bool _allowWallJump = true;

	[TabGroup("Settings", "Wall")]
	[Tooltip("The direction of the new velocity vector along the normal of the wall")]
	[Range(0f, 1f)]
	[ShowIf("_allowWallJump")]
	[SerializeField]
	private float _wallJumpForceDirectionNormal = 0.5f;

	[TabGroup("Settings", "Wall")]
	[Tooltip("The direction of the new velocity vector along the wall's forward direction")]
	[Range(0f, 1f)]
	[ShowIf("_allowWallJump")]
	[SerializeField]
	private float _wallJumpForceDirectionForwards = 0.5f;

	[TabGroup("Settings", "Wall")]
	[Tooltip("The force of the new velocity vector upwards")]
	[ShowIf("_allowWallJump")]
	[SerializeField]
	private float _wallJumpForceUpwards = 0.5f;

	[TabGroup("Settings", "Wall")]
	[Tooltip("How much force is added to the wall jump")]
	[ShowIf("_allowWallJump")]
	[SerializeField]
	private float _wallJumpAdditionalForce = 2f;

	[TabGroup("Settings", "Wall")]
	[Tooltip("How long it takes for the player to be able to wall jump again")]
	[ShowIf("_allowWallJump")]
	[SerializeField]
	private float _wallJumpCooldown = 0.4f;

	[TabGroup("Settings", "Wall")]
	[Tooltip("How long the restricted air rotation is after a wall jump")]
	[ShowIf("_allowWallJump")]
	[SerializeField]
	private float _restrictAirRotationTime = 1f;

	[TabGroup("Settings", "Wall")]
	[Tooltip("How strong the restricted air movement is after a wall jump")]
	[Range(0f, 1f)]
	[ShowIf("_allowWallJump")]
	[SerializeField]
	private float _airRotationMult = 1f;

	[TabGroup("Settings", "Stamina")]
	[Header("Stamina")]
	[Tooltip("The max amount of stamina the player has")]
	[SerializeField]
	private int _maxStaminaCharges = 3;

	[TabGroup("Settings", "Stamina")]
	[Tooltip("The base recharge rate of a single stamina charge")]
	[SuffixLabel("seconds", Overlay = true)]
	[SerializeField]
	private float _staminaBaseRechargeRate = 2f;

	[TabGroup("Settings", "Physics")]
	[Header("Gravity")]
	[Tooltip("The player's base gravity")]
	[SerializeField]
	private Vector3 _baseGravity = new(0f, -30f, 0f);

	[TabGroup("Settings", "Physics")]
	[Tooltip("Gravity while falling")]
	[SerializeField]
	private float _gravityFallingMult = 1.15f;

	[TabGroup("Settings", "Physics")]
	[Tooltip("Gravity while at the apex of a player's jump")]
	[SerializeField]
	private float _gravityJumpHangMult = 0.9f;

	[TabGroup("Settings", "Physics")]
	[Header("Capsule")]
	[SerializeField]
	private float _defaultCapsuleRadius = 0.5f;

	[TabGroup("Settings", "Physics")]
	[SerializeField]
	private float _defaultCapsuleHeight = 2f;

	[TabGroup("Settings", "Physics")]
	[SerializeField]
	private float _defaultCapsuleYOffset = 1f;

	[TabGroup("Settings", "Layers")]
	[Tooltip("Layer masks that indicates what layers the character collider to ignore")]
	[SerializeField]
	private LayerMask _ignoredLayers;

	[TabGroup("Settings", "Layers")]
	[Tooltip("Ground Layers")]
	[SerializeField]
	private LayerMask _groundLayers;

	[TabGroup("Settings", "Layers")]
	[Tooltip("Wall Layers")]
	[SerializeField]
	private LayerMask _wallLayers;

	#endregion

	#region Events

	// Events
	[HideInInspector]
	public UnityEvent OnGroundJump;

	[HideInInspector]
	public UnityEvent OnAirJump;

	[HideInInspector]
	public UnityEvent OnGroundDash;

	[HideInInspector]
	public UnityEvent OnGroundDashEnd;

	[HideInInspector]
	public UnityEvent OnAirDash;

	[HideInInspector]
	public UnityEvent OnAirDashEnd;

	[HideInInspector]
	public UnityEvent OnDownwardsDash;

	[HideInInspector]
	public UnityEvent OnLanding;

	[HideInInspector]
	public UnityEvent OnSlideStart;

	[HideInInspector]
	public UnityEvent OnSlideEnd;

	[HideInInspector]
	public UnityEvent<bool> OnWallRunStart;

	[HideInInspector]
	public UnityEvent OnWallRunEnd;

	[HideInInspector]
	public UnityEvent OnWallJump;

	[HideInInspector]
	public UnityEvent OnDashRefresh;

	[HideInInspector]
	public UnityEvent OnAirJumpsRefresh;

	[HideInInspector]
	public UnityEvent<float> OnStaminaRecharging; // normalizedStamina

	[HideInInspector]
	public UnityEvent OnStaminaRecharged;

	#endregion

	#region Private Variables

	private readonly bool _canJump = true;

	private readonly Collider[] _probedColliders = new Collider[8];
	private bool _applyInitialVertDrag;
	private RaycastHit _backWallHit;
	private bool _bouncePadTaken;
	private Transform _cameraTransform;

	// Capsule Size
	private float _capsuleHeight;
	private float _capsuleYOffset;
	private Tween _capsuleYOffsetTween;
	private bool _changedVelocityDirection;

	// Crouching
	private bool _crouchDown;
	private float _dashCooldownTimer;
	private float _dashEnterVelocity;
	private float _dashStunTimer;
	private float _distanceFromGround;
	private int _downwardsDashCount;
	private bool _dragEnabled = true;
	private RaycastHit _frontWallHit;
	private Vector3 _gravity;
	private bool _gravityEnabled = true;

	// Tweens
	private Tween _heightTween;
	private Vector3 _inputVector = Vector3.zero;
	private float _internalHorVelocityMult;
	private Vector3 _internalVelocityAdd = Vector3.zero;
	private bool _isCloseToBackWall;
	private bool _isCloseToFrontWall;
	private bool _isCloseToLeftWall;

	// Wall Check
	private bool _isCloseToRightWall;

	// Wall Jumping
	private bool _isCloseToWall;
	private float _jumpCooldownTimer;
	private bool _jumpedThisFrame;

	// Jumping
	private RaycastHit _leftWallHit;
	private bool _lockVerticalVelocity;

	// Look & Input Vectors
	private Vector3 _lookVector = new(1, 0, 0);
	private float _movementAcceleration = 15f;
	private float _movementDeceleration = 7f;
	private float _movementDecelerationToStop = 20f;

	// Misc.
	private float _movementMult = 1f;
	private Vector3 _newVelocityDirection = Vector3.zero;
	private bool _noMovementInput;
	private Vector3 _rawInputMovement;
	private float _restrictAirMovementTimer;
	private RaycastHit _rightWallHit;
	private float _slideSpeedMultTimer;

	// Sliding
	private float _slideStunTimer;
	private float _sprintExpireTimer;

	// Sprinting
	private float _sprintTimer;

	// Wall Running
	private bool _tangentialMovementOnWall;
	private float _targetCapsuleHeight;
	private float _targetCapsuleYOffset;

	// Dash
	private float _timeSinceDashRequested = 100f;

	// Downwards Dash
	private float _timeSinceDownwardsDashRequested = 100f;
	private float _timeSinceJumpAllowed;
	private float _timeSinceJumpRequested = 25f;
	private Tween _transformTween;
	private bool _ungroundPlayer;
	private float _wallJumpCooldownTimer = -0.1f;
	private bool _zeroVelocity;
	private bool _zeroVertVelocity;

	#endregion

	#region Properties

	private bool CanSprint => _sprintTimer >= _timeTilSprint;
	private bool RestrictAirRotation => _restrictAirMovementTimer > 0f;
	private Vector3 ClosestWallRightLeftNormal => _isCloseToRightWall ? _rightWallHit.normal : _leftWallHit.normal;
	private Vector3 ClosestWallFrontBackNormal => _isCloseToFrontWall ? _frontWallHit.normal : _backWallHit.normal;

	private Vector3 ClosestWallNormal =>
		_isCloseToRightWall || _isCloseToLeftWall ? ClosestWallRightLeftNormal : ClosestWallFrontBackNormal;

	private Vector3 ClosestWallForward =>
		Vector3.Dot(CurrentHorVelocity, Vector3.Cross(ClosestWallNormal, _motor.CharacterUp)) > 0
			? Vector3.Cross(ClosestWallNormal, _motor.CharacterUp).normalized
			: -Vector3.Cross(ClosestWallNormal, _motor.CharacterUp).normalized;

	public MovementStates MovementState { get; private set; }

	// Getters && Setters
	public Vector3 CurrentVelocity { get; private set; }

	public Vector3 VelocityLastFrame { get; private set; }

	public int GetAirDashes { get; private set; }

	public int GetJumps { get; private set; }

	public Vector3 CurrentHorVelocity => Vector3.ProjectOnPlane(CurrentVelocity, _motor.CharacterUp);

	public bool IsOnASlope =>
		_motor.GroundingStatus.FoundAnyGround
		&& Vector3.Angle(_motor.CharacterUp, _motor.GroundingStatus.GroundNormal) > 0.1f;

	public bool IsDashing =>
		MovementState == MovementStates.GroundDashing || MovementState == MovementStates.AirDashing;

	public bool IsFalling => !IsGrounded && CurrentVelocity.y < -1f;

	public bool IsGrounded => _motor.GroundingStatus.FoundAnyGround;

	public bool MovingDownASlope =>
		_motor.GroundingStatus.FoundAnyGround
		&& Vector3.Angle(_motor.CharacterUp, _motor.GroundingStatus.GroundNormal) > 0.1f
		&& Vector3.ProjectOnPlane(CurrentVelocity, _motor.GroundingStatus.GroundNormal).y < -0.01f;

	#endregion

	#region Unity Methods

	private void Start()
	{
		_cameraTransform = GameObject.Find("Main Camera").GetComponent<Camera>().transform;
		_motor.CharacterController = this;
		MovementState = MovementStates.Stable;

		// Set capsule size
		_capsuleHeight = _defaultCapsuleHeight;
		_capsuleYOffset = _defaultCapsuleYOffset;
		_targetCapsuleHeight = _defaultCapsuleHeight;
		_targetCapsuleYOffset = _defaultCapsuleYOffset;

		_motor.SetCapsuleDimensions(_defaultCapsuleRadius, _defaultCapsuleHeight, _defaultCapsuleYOffset);

		// Stamina
		GetStaminaCharges = _maxStaminaCharges;
	}

	private void Update()
	{
		if (GetStaminaCharges < _maxStaminaCharges)
		{
			if (_rechargeTimer >= _staminaBaseRechargeRate)
			{
				GetStaminaCharges++;
				_rechargeTimer = 0f;
				OnStaminaRecharged?.Invoke();
			}

			_rechargeTimer += Time.deltaTime;
			OnStaminaRecharging?.Invoke(NormalizedStamina);
		}
		else
		{
			_rechargeTimer = 0f;
		}
	}

	private void OnEnable()
	{
		InputManager.Instance.OnJumpPerformed += JumpRequested;
		InputManager.Instance.OnDashPerformed += DashRequested;

		InputManager.Instance.OnMovement += OnMovementInput;
		InputManager.Instance.OnCrouchPerformed += OnCrouchStart;
		InputManager.Instance.OnCrouchRelease += OnCrouchRelease;

		OnStaminaRecharging.AddListener(TriggerStaminaRechargeEvent);
	}

	private void OnDisable()
	{
		if (InputManager.Instance != null)
		{
			InputManager.Instance.OnJumpPerformed -= JumpRequested;
			InputManager.Instance.OnDashPerformed -= DashRequested;

			InputManager.Instance.OnMovement -= OnMovementInput;
			InputManager.Instance.OnCrouchPerformed -= OnCrouchStart;
			InputManager.Instance.OnCrouchRelease -= OnCrouchRelease;
		}

		OnStaminaRecharging.RemoveListener(TriggerStaminaRechargeEvent);
	}

	private void OnDestroy()
	{
		// Clean up Tweens
		if (_heightTween.isAlive)
		{
			_heightTween.Complete();
		}

		if (_capsuleYOffsetTween.isAlive)
		{
			_capsuleYOffsetTween.Complete();
		}

		if (_transformTween.isAlive)
		{
			_transformTween.Complete();
		}
	}

	#endregion
}
