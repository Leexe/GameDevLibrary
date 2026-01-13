using System;
using KinematicCharacterController;
using PrimeTween;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

public enum MovementStates
{
	Stable = 0,
	Sprinting = 1,
	Sliding = 2,
	InAir = 3,
	GroundDashing = 4,
	AirDashing = 5,
	Crouching = 6,
	WallRunning = 7,
	GroundJump = 8,
	AirJump = 9,
	WallJump = 10,
	DownwardsDash = 11,
}

public class MyCharacterController : MonoBehaviour, ICharacterController
{
	#region References

	[Header("References")]
	[SerializeField]
	private KinematicCharacterMotor _motor;

	[SerializeField]
	private GameObject _meshRoot;

	[SerializeField]
	private StaminaSystem _staminaSystem;

	#endregion

	#region Settings

	[Header("Stable Movement")]
	[Tooltip("The player's base movespeed")]
	[SerializeField]
	private float _baseMovespeed = 8f;

	[Tooltip("How fast the player accelerates while on stable ground")]
	[SerializeField]
	private float _stableAcceleration = 15f;

	[Tooltip("How fast the player decelerate to a certain speed when inputting")]
	[SerializeField]
	private float _stableDeceleration = 1.2f;

	[Tooltip("How fast the player decelerate while on stable ground and not inputting")]
	[SerializeField]
	private float _stableDecelerationToStop = 15f;

	[Header("Sprinting")]
	[Tooltip("How fast the player moves while sprinting relative to their base movespeed")]
	[SerializeField]
	private float _sprintSpeedMult = 1f;

	[Tooltip("How fast the player accelerates to sprint speed")]
	[SerializeField]
	private float _sprintAcceleration = 3f;

	[Tooltip("Time it takes for a player to start sprinting after being above the running treshold")]
	[SerializeField]
	private float _timeTilSprint = 0.01f;

	[Tooltip("Time it takes for the sprint to expire after being below the running treshold")]
	[SerializeField]
	private float _sprintExpireTime = 0.1f;

	[Tooltip("Velocity threshold that the player has to surpass to start sprinting")]
	[SerializeField]
	private float _runningTreshold = 5f;

	[Header("Sliding")]
	[Tooltip("How much the player's current speed is multiplied when they slide after landing on the ground")]
	[Range(0f, 1f)]
	[SerializeField]
	private float _slideConditionalSpeedMult = 0.05f;

	[Tooltip("Upper limit to how much speed the slide can give")]
	[SerializeField]
	private float _slideForceMax = 2f;

	[Tooltip("How long after landing on the ground, can the player get the slide speed mult")]
	[SerializeField]
	private float _slideSpeedMultBuffer = 0.1f;

	[Tooltip("The max speed a slide can have when going down a hill relatiev to the base speed")]
	[SerializeField]
	private float _slideSlopeSpeedMult = 4f;

	[Tooltip("How long the player is slide for after initiating a slide")]
	[SerializeField]
	private float _slideStunDuration = 0.6f;

	[Tooltip("Velocity threshold needed to enter a slide")]
	[SerializeField]
	private float _slideSpeedThreshold = 10f;

	[Tooltip("Velocity threshold needed to exit a slide")]
	[SerializeField]
	private float _slideSpeedExitThreshold = 6f;

	[Tooltip("How much the player is allowed to redirect their movement while sliding")]
	[SerializeField]
	private float _slidingRotationSmoothing = 2f;

	[Tooltip("How smooth the slide slow down is")]
	[SerializeField]
	private float _slidingDragSmoothing = 3f;

	[Tooltip("How fast the slide increases in speed down a slope")]
	[SerializeField]
	private float _slideBuildUpSmoothing = 1f;

	[Tooltip("The percentage of velocity slowdown on the slide")]
	[SerializeField]
	private float _slideSlowDown = 0.85f;

	[Header("Air Movement")]
	[Tooltip("How fast the player moves while in the air relative to their base movespeed")]
	[SerializeField]
	private float _airBaseSpeedMult = 1f;

	[Tooltip("How fast the player accelerates up to air base speed")]
	[SerializeField]
	private float _airAcceleration = 7f;

	[Tooltip("How fast the player decelerates up to air base speed")]
	[SerializeField]
	private float _airDeceleration = 0.5f;

	[Tooltip("How easily the player is able to change direction in the air")]
	[SerializeField]
	private float _airControlRate = 5f;

	[Tooltip("Air drag")]
	[SerializeField]
	private float _drag = 0.01f;

	[Header("Dashes")]
	[Tooltip("How much velocity to increase at the start of a dash")]
	[SerializeField]
	private float _dashForce = 30f;

	[Tooltip("How much velocity to decrease after a dash relative to dash force")]
	[Range(0f, 1f)]
	[SerializeField]
	private float _dashVelocityToDecreaseMult = 0.45f;

	[Range(0f, 1f)]
	[Tooltip("How much the player's velocity is decreased after a dash")]
	[SerializeField]
	private float _dashEndVelocityMult = 0.65f;

	[Tooltip("How long the player is stuck moving in a certain direction")]
	[SerializeField]
	private float _dashStun = 0.2f;

	[Tooltip("The time allowed in between dashes")]
	[SerializeField]
	private float _dashCooldown = 0.5f;

	[Tooltip("A buffer for the dash input")]
	[SerializeField]
	private float _dashBuffer = 0.2f;

	[Tooltip("The amount of air dashes allowed while in the air")]
	[SerializeField]
	private int _airDashLimit = 2;

	[Header("Downward Dash")]
	[Tooltip("How much velocity downwards during a downwards dash")]
	[SerializeField]
	private float _downwardsDashForce = 25f;

	[Tooltip("How long the player is stuck moving in a certain direction")]
	[SerializeField]
	private float _downwardsDashStun = 0.8f;

	[Tooltip("A buffer for the downwards dash input")]
	[SerializeField]
	private float _downwardsDashBuffer = 0.5f;

	[Tooltip("The amount of air dashes allowed while in the air")]
	[SerializeField]
	private int _downwardsDashLimit = 4;

	[Header("Jumps")]
	[Tooltip("Allow jumps on steep slopes")]
	[SerializeField]
	private bool _allowJumpWhileSliding = true;

	[Tooltip("How high the player jumps")]
	[SerializeField]
	private float _jumpForce = 10f;

	[Tooltip(
		"How much the player's horizontal velocity is slowed down after landing, 0 to max slow down and 1 to have no slowdown"
	)]
	[Range(0f, 1f)]
	[SerializeField]
	private float _landingSlowDownMult = 0.85f;

	[Tooltip("How high the player jumps in the air")]
	[SerializeField]
	private float _airJumpForce = 14f;

	[Tooltip("A buffer for the jump input")]
	[SerializeField]
	private float _jumpBuffer = 0.3f;

	[Tooltip("Time it takes to jump again")]
	[SerializeField]
	private float _jumpCooldown = 0.05f;

	[Tooltip("Time after leaving stable ground where the player can still jump")]
	[SerializeField]
	private float _coyoteTime = 0.15f;

	[Tooltip("Time at the apex of a player's jump where gravity is reduced")]
	[SerializeField]
	private float _jumpHangInterval = 0.3f;

	[Tooltip("The number of jumps the player has")]
	[SerializeField]
	private int _maxJumps = 2;

	[Header("Wall Running")]
	[Tooltip("Disable or Enable Wall Running")]
	[SerializeField]
	private bool _allowWallRun = true;

	[Tooltip("How much of the player's current horizontal velocity is increased after entering a wall run")]
	[Range(0f, 2f)]
	[ShowIf("_allowWallRun")]
	[SerializeField]
	private float _wallRunInitalHorVelocityMult = 0.5f;

	[Tooltip("How much of the player's current vertical velocity is decreased after entering a wall run")]
	[Range(0f, 1f)]
	[ShowIf("_allowWallRun")]
	[SerializeField]
	private float _wallRunInitalVertVelocityMult = 0.5f;

	[Tooltip("Limits how much of the player's current velocity is increased after entering a wall run")]
	[ShowIf("_allowWallRun")]
	[SerializeField]
	private float _wallRunInitalVelocityMax = 6f;

	[Tooltip("How fast the player has to be to start wall running")]
	[ShowIf("_allowWallRun")]
	[SerializeField]
	private float _wallRunVelocityThreshold = 11f;

	[Tooltip("How slow the player is before they are kicked out of a wall run")]
	[ShowIf("_allowWallRun")]
	[SerializeField]
	private float _wallRunVelocityExitThreshold = 6f;

	[Tooltip("How far up the ground the player has to be to wall run")]
	[ShowIf("_allowWallRun")]
	[SerializeField]
	private float _wallRunHeightThreshold = 2f;

	[Tooltip("How much velocity is taken away over time when the player wall runs")]
	[ShowIf("_allowWallRun")]
	[SerializeField]
	private float _wallRunDragSmoothing = 0.5f;

	[Tooltip("Gravity while wall running")]
	[ShowIf("_allowWallRun")]
	[SerializeField]
	private Vector3 _wallRunGravity = new(0, -5f, 0);

	[Header("Wall Jumps")]
	[Tooltip("Disable or Enable Wall Jumping")]
	[SerializeField]
	private bool _allowWallJump = true;

	[Tooltip("The direction of the new velocity vector along the normal of the wall")]
	[Range(0f, 1f)]
	[ShowIf("_allowWallJump")]
	[SerializeField]
	private float _wallJumpForceDirectionNormal = 0.5f;

	[Tooltip("The direction of the new velocity vector along the wall's forward direction")]
	[Range(0f, 1f)]
	[ShowIf("_allowWallJump")]
	[SerializeField]
	private float _wallJumpForceDirectionForwards = 0.5f;

	[Tooltip("The force of the new velocity vector upwards")]
	[ShowIf("_allowWallJump")]
	[SerializeField]
	private float _wallJumpForceUpwards = 0.5f;

	[Tooltip("How much force is added to the wall jump")]
	[ShowIf("_allowWallJump")]
	[SerializeField]
	private float _wallJumpAdditionalForce = 2f;

	[Tooltip("How long it takes for the player to be able to wall jump again")]
	[ShowIf("_allowWallJump")]
	[SerializeField]
	private float _wallJumpCooldown = 0.4f;

	[Tooltip("How long the restricted air rotation is after a wall jump")]
	[ShowIf("_allowWallJump")]
	[SerializeField]
	private float _restrictAirRotationTime = 1f;

	[Tooltip("How strong the restricted air movement is after a wall jump")]
	[Range(0f, 1f)]
	[ShowIf("_allowWallJump")]
	[SerializeField]
	private float _airRotationMult = 1f;

	[Header("Crounching")]
	[Tooltip("How fast the player moves while in crouching relative to their base movespeed")]
	[SerializeField]
	private float _crouchSpeedMult = 0.5f;

	[Tooltip("How long it takes for the player to crouch and uncrouch")]
	[SerializeField]
	private float _crouchTransitionTime = 0.2f;

	[Tooltip("The percentage of the player's height when crouching")]
	[Range(0f, 1f)]
	[SerializeField]
	private float _crouchCapsuleHeightMult = 0.5f;

	[Tooltip("The percentage of the player's YOffset when crouching")]
	[Range(0f, 1f)]
	[SerializeField]
	private float _crouchCapsuleYOffsetMult = 0.5f;

	[Header("Gravity")]
	[Tooltip("The player's base gravity")]
	[SerializeField]
	private Vector3 _baseGravity = new(0f, -30f, 0f);

	[Tooltip("Gravity while falling")]
	[SerializeField]
	private float _gravityFallingMult = 1.15f;

	[Tooltip("Gravity while at the apex of a player's jump")]
	[SerializeField]
	private float _gravityJumpHangMult = 0.9f;

	[Header("Capsule")]
	[SerializeField]
	private float _defaultCapsuleRadius = 0.5f;

	[SerializeField]
	private float _defaultCapsuleHeight = 2f;

	[SerializeField]
	private float _defaultCapsuleYOffset = 1f;

	[Header("Misc")]
	[Tooltip("How close to a wall a player has to be to start wall running")]
	[SerializeField]
	private float _wallCheckDistance = 0.5f;

	[Tooltip("Layer masks that indicates what layers the character collider to ignore")]
	[SerializeField]
	private LayerMask _ignoredLayers;

	[Tooltip("Ground Layers")]
	[SerializeField]
	private LayerMask _groundLayers;

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

	#endregion

	#region Internal Data

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
	private bool _tangentalMovementOnWall;
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

	public bool IsFalling => CurrentVelocity.y < 0.1;

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
	}

	private void OnEnable()
	{
		InputManager.Instance.OnJumpPerformed.AddListener(JumpRequested);
		InputManager.Instance.OnDashPerformed.AddListener(DashRequested);

		InputManager.Instance.OnMovement.AddListener(OnMovementInput);
		InputManager.Instance.OnCrouchPerformed.AddListener(OnCrouchStart);
		InputManager.Instance.OnCrouchRelease.AddListener(OnCrouchRelease);
	}

	private void OnDisable()
	{
		InputManager.Instance?.OnJumpPerformed.RemoveListener(JumpRequested);
		InputManager.Instance?.OnDashPerformed.RemoveListener(DashRequested);

		InputManager.Instance?.OnMovement.RemoveListener(OnMovementInput);
		InputManager.Instance?.OnCrouchPerformed.RemoveListener(OnCrouchStart);
		InputManager.Instance?.OnCrouchRelease.RemoveListener(OnCrouchRelease);
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

	#region Movement

	public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
	{
		currentRotation = Quaternion.LookRotation(_lookVector, _motor.CharacterUp);
	}

	public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
	{
		// Handle movement
		{
			// Sets movement to zero
			if (_noMovementInput)
			{
				_inputVector = Vector3.zero;
			}

			// Slow down the vertical velocity after touching a wall
			if (_applyInitialVertDrag)
			{
				currentVelocity = new Vector3(
					currentVelocity.x,
					currentVelocity.y * _wallRunInitalVertVelocityMult,
					currentVelocity.z
				);
				_applyInitialVertDrag = false;
			}

			Vector3 targetVelocityVector = Vector3.zero;
			// Check if the player is on stable ground i.e. not on a slope or in the air
			if (_motor.GroundingStatus.IsStableOnGround)
			{
				// Calculate max movement vector
				var inputRight = Vector3.Cross(_inputVector, _motor.CharacterUp);
				Vector3 reorientedInputVector =
					Vector3.Cross(_motor.GroundingStatus.GroundNormal, inputRight).normalized * _inputVector.magnitude;
				targetVelocityVector = reorientedInputVector * _baseMovespeed;

				if (MovementState == MovementStates.Sliding)
				{
					targetVelocityVector = HandleSliding(currentVelocity, targetVelocityVector, deltaTime);
				}

				// If the player is inputting and is faster than the sprint movespeed, slow down their velocity slower than usual
				if (_inputVector != Vector3.zero && CurrentHorVelocity.magnitude >= _sprintSpeedMult * _baseMovespeed)
				{
					Vector3 turnVector =
						Quaternion.FromToRotation(currentVelocity, targetVelocityVector) * currentVelocity;
					currentVelocity = Vector3.Lerp(
						turnVector,
						targetVelocityVector * _movementMult,
						_movementDeceleration * deltaTime
					);
				}
				// Otherwise, speed up their movement
				else if (_inputVector != Vector3.zero)
				{
					Vector3 turnVector =
						Quaternion.FromToRotation(currentVelocity, targetVelocityVector) * currentVelocity;
					currentVelocity = Vector3.Lerp(
						turnVector,
						targetVelocityVector * _movementMult,
						1 - Mathf.Exp(-_movementAcceleration * deltaTime)
					);
				}
				// If not inputting and not sliding, apply deacceleration
				else
				{
					currentVelocity = Vector3.Lerp(
						currentVelocity,
						targetVelocityVector * _movementMult,
						1 - Mathf.Exp(-_movementDecelerationToStop * deltaTime)
					);
				}
			}
			// If the player is in the air or on a slope
			else
			{
				float airControlRate = _airControlRate;

				// Add air movement if the player is inputing in the air
				if (_inputVector.sqrMagnitude > 0f)
				{
					targetVelocityVector = _inputVector * _baseMovespeed * _airBaseSpeedMult;

					// Preventing climbing on unstable ground while in the air
					if (_motor.GroundingStatus.FoundAnyGround)
					{
						Vector3 perpendicularObstructionNormal = Vector3
							.Cross(
								Vector3.Cross(_motor.CharacterUp, _motor.GroundingStatus.GroundNormal),
								_motor.CharacterUp
							)
							.normalized;
						targetVelocityVector = Vector3.ProjectOnPlane(
							targetVelocityVector,
							perpendicularObstructionNormal
						);
					}

					// If the player is tangent to a wall, lock their movement against the wall
					if (_tangentalMovementOnWall)
					{
						targetVelocityVector = ClosestWallForward * (_wallRunVelocityExitThreshold - 1f);
					}

					// Restrict air rotation
					if (RestrictAirRotation)
					{
						airControlRate *= _airRotationMult;
					}
				}

				// Turn or Accelerate the Player
				var horizontalVelocity = Vector3.ProjectOnPlane(currentVelocity, _motor.CharacterUp);
				if (targetVelocityVector.magnitude < horizontalVelocity.magnitude)
				{
					// If any component of the target velocity vector is along the current horizontal velocity, remove it
					if (Vector3.Dot(targetVelocityVector, horizontalVelocity.normalized) > 0f)
					{
						targetVelocityVector =
							targetVelocityVector
							- (
								horizontalVelocity.normalized
								* Vector3.Dot(targetVelocityVector, horizontalVelocity.normalized)
							);
					}

					currentVelocity += targetVelocityVector * (deltaTime * airControlRate * 0.75f);
					// Decelerate Player
					var horizontalCurrentVector = Vector3.ProjectOnPlane(currentVelocity, _gravity);
					currentVelocity =
						Vector3.Lerp(
							horizontalCurrentVector,
							horizontalCurrentVector.normalized * targetVelocityVector.magnitude,
							_movementDeceleration * deltaTime
						) + Vector3.Project(currentVelocity, _gravity);
				}
				else
				{
					horizontalVelocity += targetVelocityVector * (deltaTime * _movementAcceleration);
					horizontalVelocity = Vector3.ClampMagnitude(horizontalVelocity, targetVelocityVector.magnitude);
					currentVelocity = new Vector3(horizontalVelocity.x, currentVelocity.y, horizontalVelocity.z);
				}

				// Add gravity to the gravity
				if (_gravityEnabled)
				{
					currentVelocity += HandleGravity(currentVelocity, deltaTime);
				}

				// Add drag to the player
				if (_dragEnabled)
				{
					currentVelocity *= 1f / (1f + (_drag * deltaTime));
				}
			}
		}

		// Handle Sprinting
		HandleSprinting(currentVelocity, deltaTime);

		// Set vertical velocity to 0
		if (_lockVerticalVelocity)
		{
			currentVelocity = Vector3.ProjectOnPlane(currentVelocity, _motor.CharacterUp);
		}

		// Set velocity to zero
		if (_zeroVelocity)
		{
			currentVelocity = Vector3.zero;
			_zeroVelocity = false;
		}

		// Set vertical velocity to zero
		if (_zeroVertVelocity)
		{
			currentVelocity.y = 0f;
			_zeroVertVelocity = false;
		}

		// Multiply velocity by some factor
		if (!Mathf.Approximately(_internalHorVelocityMult, 1))
		{
			currentVelocity = new Vector3(
				currentVelocity.x * _internalHorVelocityMult,
				currentVelocity.y,
				currentVelocity.z * _internalHorVelocityMult
			);
			_internalHorVelocityMult = 1;
		}

		// Handle redirecting the velocity
		if (_changedVelocityDirection)
		{
			currentVelocity = CurrentVelocity.magnitude * _newVelocityDirection.normalized;
			_changedVelocityDirection = false;
		}

		// Handle Additive Velocity
		if (_internalVelocityAdd.sqrMagnitude > 0f)
		{
			currentVelocity += _internalVelocityAdd;
			_internalVelocityAdd = Vector3.zero;
		}

		// Set local varaible _currentVelocity to currentVelocity
		CurrentVelocity = currentVelocity;
	}

	public void AfterCharacterUpdate(float deltaTime)
	{
		// Check if the player is grounded and reset their jumps if so
		if (_allowJumpWhileSliding ? _motor.GroundingStatus.FoundAnyGround : _motor.GroundingStatus.IsStableOnGround)
		{
			if (!_jumpedThisFrame)
			{
				GetJumps = _maxJumps;
				_timeSinceJumpAllowed = 0f;
			}
		}
		else
		{
			// Time kept for coyote time
			_timeSinceJumpAllowed += deltaTime;
		}

		if (_motor.GroundingStatus.IsStableOnGround && !_motor.LastGroundingStatus.IsStableOnGround)
		{
			// Player has landed on the ground, keep their horizontal velocity the same
			AddVelocity(
				(Vector3.ProjectOnPlane(VelocityLastFrame, _motor.CharacterUp) - CurrentHorVelocity)
					* _landingSlowDownMult
			);
			OnLanding?.Invoke();
		}
	}

	public void BeforeCharacterUpdate(float deltaTime)
	{
		// Get Player Inputs
		FeedPlayerInput();

		// Handle character states
		HandleStates();

		// Handle Slide Stun Duration
		HandleSlide(deltaTime);

		// Handle Dashing
		HandleDash(deltaTime);

		// Handle Wall Checking
		HandleCheckForWall();

		// Handle Capsule Dimensions
		HandleCapsuleSize();

		// Handle Ground Distance
		HandleDistanceOfGround();

		// Handle ungrounding the player
		HandleUngroundPlayer();

		// Handles the jump timers
		HandleJumpTimers(deltaTime);

		// Handles air restrict rotation time
		HandleRestrictAirRotation(deltaTime);

		// Handle bounce pad taken
		HandleBouncePadTaken();

		VelocityLastFrame = CurrentVelocity;
	}

	public bool IsColliderValidForCollisions(Collider coll)
	{
		if (((1 << coll.gameObject.layer) & _ignoredLayers) > 0)
		{
			return false;
		}

		return true;
	}

	public void OnDiscreteCollisionDetected(Collider hitCollider) { }

	public void OnGroundHit(
		Collider hitCollider,
		Vector3 hitNormal,
		Vector3 hitPoint,
		ref HitStabilityReport hitStabilityReport
	) { }

	public void OnMovementHit(
		Collider hitCollider,
		Vector3 hitNormal,
		Vector3 hitPoint,
		ref HitStabilityReport hitStabilityReport
	) { }

	public void PostGroundingUpdate(float deltaTime) { }

	public void ProcessHitStabilityReport(
		Collider hitCollider,
		Vector3 hitNormal,
		Vector3 hitPoint,
		Vector3 atCharacterPosition,
		Quaternion atCharacterRotation,
		ref HitStabilityReport hitStabilityReport
	) { }

	#endregion

	#region State Machine

	private void TransitionState(MovementStates newState)
	{
		// Don't transition if the states are the same
		if (newState == MovementState)
		{
			return;
		}

		if (ExitState(MovementState, newState))
		{
			EnterState(MovementState, newState);
			MovementState = newState;
		}
	}

	// Returns true if exit conditions can be met, else false
	private bool ExitState(MovementStates oldState, MovementStates newState)
	{
		switch (oldState)
		{
			case MovementStates.Stable:
			{
				break;
			}
			case MovementStates.Crouching:
			{
				if (!UncrouchPlayer())
				{
					return false;
				}

				break;
			}
			case MovementStates.Sliding:
			{
				// If the stun timer is active and the player is grounded, do not allow the slide state to change
				if (
					_slideStunTimer > 0f
					&& _motor.GroundingStatus.FoundAnyGround
					&& _timeSinceJumpRequested > _jumpBuffer
				)
				{
					return false;
				}

				// If the player can't uncrouch, transition to crouch state
				if (!CanUncrouch())
				{
					EnterState(MovementState, MovementStates.Crouching);
					MovementState = MovementStates.Crouching;
					OnSlideEnd?.Invoke();
					return false;
				}

				UncrouchPlayer();
				OnSlideEnd?.Invoke();
				break;
			}
			case MovementStates.GroundDashing:
			{
				if (_dashStunTimer > 0f)
				{
					return false;
				}

				// Calculate how much velocity to decrease after exiting a dash and only decrease the velocity if it does not send the player backwards
				float enterExitVelocityDiff = _dashEnterVelocity - CurrentHorVelocity.magnitude;
				if (CurrentHorVelocity.magnitude > _dashForce * _dashVelocityToDecreaseMult)
				{
					AddHorizontalVelocity(
						CurrentHorVelocity.normalized
							* ((-_dashForce * _dashVelocityToDecreaseMult) + enterExitVelocityDiff)
							* 0.5f
					); // Constant here to account for ground friction
				}

				MultHorVelocityMagnitude(_dashEndVelocityMult);
				_noMovementInput = false;
				_lockVerticalVelocity = false;
				OnGroundDashEnd?.Invoke();
				break;
			}
			case MovementStates.AirDashing:
			{
				if (_dashStunTimer > 0f && !_bouncePadTaken)
				{
					return false;
				}

				// Calculate how much velocity to decrease after exiting a dash and only decrease the velocity if it does not send the player backwards
				float enterExitVelocityDiff = _dashEnterVelocity - CurrentHorVelocity.magnitude;
				if (CurrentHorVelocity.magnitude > _dashForce * _dashVelocityToDecreaseMult)
				{
					AddHorizontalVelocity(
						CurrentHorVelocity.normalized
							* ((-_dashForce * _dashVelocityToDecreaseMult) + enterExitVelocityDiff)
					);
				}

				MultHorVelocityMagnitude(_dashEndVelocityMult);
				_noMovementInput = false;
				_lockVerticalVelocity = false;
				OnAirDashEnd?.Invoke();
				break;
			}
			case MovementStates.DownwardsDash:
			{
				if (_dashStunTimer > 0f && newState != MovementStates.InAir)
				{
					return false;
				}

				_noMovementInput = false;
				break;
			}
			case MovementStates.WallRunning:
			{
				// Reset movement parameters
				_movementMult = 1f;
				_movementAcceleration = _stableAcceleration;
				_movementDeceleration = _stableDeceleration;
				_movementDecelerationToStop = _stableDecelerationToStop;
				_gravity = _baseGravity;

				_dragEnabled = true;
				_gravityEnabled = true;
				_tangentalMovementOnWall = false;
				OnWallRunEnd?.Invoke();
				break;
			}
			case MovementStates.WallJump:
			{
				_restrictAirMovementTimer = _restrictAirRotationTime;
				break;
			}
		}

		return true;
	}

	private void EnterState(MovementStates oldState, MovementStates newState)
	{
		switch (newState)
		{
			case MovementStates.Stable:
			{
				_movementMult = 1f;
				_movementAcceleration = _stableAcceleration;
				_movementDeceleration = _stableDeceleration;
				_movementDecelerationToStop = _stableDecelerationToStop;
				_gravity = _baseGravity;
				_restrictAirMovementTimer = 0f;
				break;
			}
			case MovementStates.Crouching:
			{
				_movementMult = _crouchSpeedMult;
				_movementAcceleration = _stableAcceleration;
				_movementDeceleration = _stableDeceleration;
				_movementDecelerationToStop = _stableDecelerationToStop;
				_gravity = _baseGravity;
				// Enter crouching positon
				CrouchPlayer();
				// Stop sprinting if crouching
				StopSprint();
				_restrictAirMovementTimer = 0f;
				break;
			}
			case MovementStates.Sprinting:
			{
				_movementMult = _sprintSpeedMult;
				_movementAcceleration = _sprintAcceleration;
				_movementDeceleration = _stableDeceleration;
				_movementDecelerationToStop = _stableDecelerationToStop;
				_gravity = _baseGravity;
				_restrictAirMovementTimer = 0f;
				break;
			}
			case MovementStates.AirDashing:
			{
				_movementMult = 1f;
				_movementAcceleration = 0f;
				_movementDeceleration = 0f;
				_movementDecelerationToStop = 0f;
				_gravity = _baseGravity;

				// If the input vector is zero, the input vector is where the camera is facing
				if (_inputVector == Vector3.zero)
				{
					_inputVector = Vector3.ProjectOnPlane(_lookVector, _motor.CharacterUp);
				}

				if (_staminaSystem.ConsumeStamina())
				{
					_lockVerticalVelocity = true;
					_noMovementInput = true;
					_dashStunTimer = _dashStun;
					AddVelocityInPlayerInputDirection(_dashForce);
				}

				ZeroMovement();
				_dashEnterVelocity = CurrentHorVelocity.magnitude + _dashForce;
				_dashCooldownTimer = _dashCooldown;
				GetAirDashes++;
				_restrictAirMovementTimer = 0f;
				OnAirDash?.Invoke();
				break;
			}
			case MovementStates.GroundDashing:
			{
				_movementMult = 1f;
				_movementAcceleration = 0f;
				_movementDeceleration = 0f;
				_movementDecelerationToStop = 0f;
				_gravity = _baseGravity;

				// If the input vector is zero, the input vector is where the camera is facing
				if (_inputVector == Vector3.zero)
				{
					_inputVector = Vector3.ProjectOnPlane(_lookVector, _motor.CharacterUp);
				}

				if (_staminaSystem.ConsumeStamina())
				{
					_noMovementInput = true;
					_dashStunTimer = _dashStun;
					AddVelocityInPlayerInputDirection(_dashForce);
				}

				ZeroMovement();
				_dashEnterVelocity = CurrentHorVelocity.magnitude + _dashForce;
				_dashCooldownTimer = _dashCooldown;
				_restrictAirMovementTimer = 0f;
				OnGroundDash?.Invoke();
				break;
			}
			case MovementStates.DownwardsDash:
			{
				_movementMult = 1f;
				_movementAcceleration = 0f;
				_movementDeceleration = 0f;
				_movementDecelerationToStop = 0f;
				_gravity = _baseGravity;

				if (_staminaSystem.ConsumeStamina())
				{
					_noMovementInput = true;
					_dashStunTimer = _downwardsDashStun;
					AddVertVelocity(-_downwardsDashForce);
					if (CurrentVelocity.y > 0f)
					{
						_zeroVertVelocity = true;
					}
				}

				_dashCooldownTimer = _downwardsDashStun;
				_timeSinceDownwardsDashRequested = _downwardsDashBuffer + 1f;
				_downwardsDashCount++;
				OnDownwardsDash?.Invoke();
				break;
			}
			case MovementStates.Sliding:
			{
				_movementMult = _slideSlowDown;
				_movementAcceleration = _slidingDragSmoothing;
				_movementDeceleration = _slidingDragSmoothing;
				_movementDecelerationToStop = _slidingDragSmoothing;
				_gravity = _baseGravity;
				// Force the player to slide for some amount of time
				_slideStunTimer = _slideStunDuration;
				// Enter crouching positon
				CrouchPlayer();
				// Apply the slide force, if the player is about to land
				if (_slideSpeedMultTimer > 0f)
				{
					float speedToAdd = Mathf.Clamp(
						CurrentVelocity.magnitude * _slideConditionalSpeedMult,
						0f,
						_slideForceMax
					);
					AddVelocity(CurrentVelocity.normalized * speedToAdd);
				}
				else
				{
					AddVelocity(Vector3.zero);
				}

				_restrictAirMovementTimer = 0f;
				OnSlideStart?.Invoke();
				break;
			}
			case MovementStates.WallRunning:
			{
				_movementMult = 1f;
				_movementAcceleration = _wallRunDragSmoothing;
				_movementDeceleration = _wallRunDragSmoothing;
				_movementDecelerationToStop = _stableDecelerationToStop;
				_gravity = _wallRunGravity;
				// Add velocity along the wall forward
				float wallRunVelocity = Mathf.Clamp(
					_wallRunInitalHorVelocityMult * CurrentHorVelocity.magnitude,
					0f,
					_wallRunInitalVelocityMax
				);
				AddVelocity(wallRunVelocity * ClosestWallForward.normalized);
				// Reset air jumps
				ResetAirJumps();
				_dragEnabled = false;
				_applyInitialVertDrag = true;
				_gravityEnabled = false;
				_tangentalMovementOnWall = true;
				_restrictAirMovementTimer = 0f;
				OnWallRunStart?.Invoke(_isCloseToRightWall);
				break;
			}
			case MovementStates.GroundJump:
			{
				_movementMult = 1f;
				_movementAcceleration = _stableAcceleration;
				_movementDeceleration = _stableDeceleration;
				_movementDecelerationToStop = _stableDecelerationToStop;
				_gravity = _wallRunGravity;
				Vector3 jumpDirection = _motor.CharacterUp;
				if (_motor.GroundingStatus is { FoundAnyGround: true, IsStableOnGround: false })
				{
					jumpDirection = _motor.GroundingStatus.GroundNormal;
				}

				_motor.ForceUnground();

				AddVelocity(jumpDirection * _jumpForce);
				_timeSinceJumpRequested = _jumpBuffer + 1f;
				GetJumps--;
				_jumpCooldownTimer = _jumpCooldown;
				_jumpedThisFrame = true;
				_timeSinceJumpAllowed = _coyoteTime + 0.1f;
				OnGroundJump?.Invoke();
				break;
			}
			case MovementStates.AirJump:
			{
				_movementMult = 1f;
				_movementAcceleration = _stableAcceleration;
				_movementDeceleration = _stableDeceleration;
				_movementDecelerationToStop = _stableDecelerationToStop;

				_motor.ForceUnground();

				if (CurrentVelocity.y < _airJumpForce)
				{
					AddVelocity(_motor.CharacterUp * _airJumpForce);
					_zeroVertVelocity = true;
				}
				else
				{
					AddVelocity(_motor.CharacterUp * _airJumpForce / 2);
				}

				_staminaSystem.ConsumeStamina();
				_timeSinceJumpRequested = _jumpBuffer + 1f;
				GetJumps--;
				_jumpCooldownTimer = _jumpCooldown;
				_jumpedThisFrame = true;
				OnAirJump?.Invoke();
				break;
			}
			case MovementStates.WallJump:
			{
				_movementMult = 1f;
				_movementAcceleration = _stableAcceleration;
				_movementDeceleration = _stableDeceleration;
				_movementDecelerationToStop = _stableDecelerationToStop;

				_motor.ForceUnground();

				// Add velocity to jump
				Vector3 newHorVelocityDirection =
					(ClosestWallNormal.normalized * _wallJumpForceDirectionNormal)
					+ (ClosestWallForward * _wallJumpForceDirectionForwards);
				ChangeVelocityDirection(newHorVelocityDirection);
				AddVelocity(
					(_wallJumpAdditionalForce * newHorVelocityDirection.normalized)
						+ (_motor.CharacterUp * _wallJumpForceUpwards)
				);
				// Reset air jumps
				ResetAirJumps();

				_wallJumpCooldownTimer = _wallJumpCooldown;
				_timeSinceJumpRequested = _jumpBuffer + 1f;
				_jumpCooldownTimer = _jumpCooldown;
				_jumpedThisFrame = true;
				OnWallJump?.Invoke();
				break;
			}
			case MovementStates.InAir:
			{
				_movementMult = 1f;
				_movementAcceleration = _airAcceleration;
				_movementDeceleration = _airDeceleration;
				_movementDecelerationToStop = _stableDecelerationToStop;
				_gravity = _baseGravity;
				break;
			}
			default:
			{
				_movementMult = 1f;
				_movementAcceleration = _stableAcceleration;
				_movementDeceleration = _stableAcceleration;
				_movementDecelerationToStop = _stableDecelerationToStop;
				_gravity = _baseGravity;
				break;
			}
		}
	}

	private void HandleStates()
	{
		// Stable Grounded States
		if (_motor.GroundingStatus.IsStableOnGround)
		{
			// Ground Jump, if the player has jumps, requested the jumps, and has not jumped during the jump cooldown period
			if (GetJumps > 0 && _timeSinceJumpRequested <= _jumpBuffer && _canJump && _jumpCooldownTimer <= 0)
			{
				TransitionState(MovementStates.GroundJump);
			}
			// Slide, if the player is pressing the crouch button, and they are moving past a certain threshold or are moving down a slope
			else if (
				_crouchDown
				&& (
					(
						MovementState == MovementStates.Sliding
							? CurrentHorVelocity.magnitude >= _slideSpeedExitThreshold
							: CurrentHorVelocity.magnitude >= _slideSpeedThreshold
					) || MovingDownASlope
				)
			)
			{
				TransitionState(MovementStates.Sliding);
			}
			// Crouch, if the player presses the crouch button and are not fast enough
			else if (_crouchDown)
			{
				TransitionState(MovementStates.Crouching);
			}
			// Ground Dash, if the player has stamina to consume, the player has pressed the dash button, are inputting in a direction, and not sliding/crouching/dashing
			else if (
				_staminaSystem.CanConsumeStamina()
				&& _timeSinceDashRequested < _dashBuffer
				&& _dashCooldownTimer <= 0f
				&& (
					MovementState != MovementStates.Sliding
					|| MovementState != MovementStates.Crouching
					|| MovementState != MovementStates.GroundDashing
					|| MovementState != MovementStates.AirDashing
				)
			)
			{
				TransitionState(MovementStates.GroundDashing);
			}
			// Sprint, if the player can sprint
			else if (CanSprint)
			{
				TransitionState(MovementStates.Sprinting);
			}
			// Else, stable
			else
			{
				TransitionState(MovementStates.Stable);
			}
		}
		// Air States
		else
		{
			// Ground Jump, if the player is on a slope and the option _allowJumpWhileSliding is on or the player is jumping during coyote time
			if (
				GetJumps > 0
				&& _timeSinceJumpRequested <= _jumpBuffer
				&& _canJump
				&& (
					(_allowJumpWhileSliding && _motor.GroundingStatus.FoundAnyGround)
					|| _timeSinceJumpAllowed <= _coyoteTime
				)
			)
			{
				TransitionState(MovementStates.GroundJump);
			}
			// Wall Jump, if the player has not jumped during the cooldown and is close to a wall
			else if (
				_timeSinceJumpRequested <= _jumpBuffer
				&& _canJump
				&& _wallJumpCooldownTimer <= 0
				&& _allowWallJump
				&& _isCloseToWall
			)
			{
				TransitionState(MovementStates.WallJump);
			}
			// Air Jump, if the player has jumps, the player has not last jumped during the cooldown timer, and they can consume stamina
			else if (
				GetJumps > 0
				&& _timeSinceJumpRequested <= _jumpBuffer
				&& _canJump
				&& _jumpCooldownTimer <= 0
				&& (
					_allowJumpWhileSliding
						? !_motor.GroundingStatus.FoundAnyGround
						: !_motor.GroundingStatus.IsStableOnGround
				)
				&& _staminaSystem.CanConsumeStamina()
			)
			{
				TransitionState(MovementStates.AirJump);
			}
			// Wall Run, if the player is close to wall, they are not crouching, they are inputting, is falling, and are fast enough along the tangent of the wall
			else if (
				(_isCloseToLeftWall || _isCloseToRightWall)
				&& !_crouchDown
				&& _inputVector != Vector3.zero
				&& IsFalling
				&& _allowWallRun
				&& (_distanceFromGround >= _wallRunHeightThreshold || _distanceFromGround < 0)
				&& (
					MovementState == MovementStates.WallRunning
						? Vector3.Project(CurrentVelocity, ClosestWallForward).magnitude
							>= _wallRunVelocityExitThreshold
						: Vector3.Project(CurrentVelocity, ClosestWallForward).magnitude >= _wallRunVelocityThreshold
				)
			)
			{
				TransitionState(MovementStates.WallRunning);
			}
			// Air Dash, if the player has stamina to consume, the player has pressed the dash button, has air dashes left, and has not dashed recently
			else if (
				_staminaSystem.CanConsumeStamina()
				&& _timeSinceDashRequested < _dashBuffer
				&& GetAirDashes < _airDashLimit
				&& _dashCooldownTimer <= 0f
			)
			{
				TransitionState(MovementStates.AirDashing);
			}
			// Air Dash, if the player has stamina to consume, the player has pressed the dash button, and are inputting in a direction
			else if (
				_staminaSystem.CanConsumeStamina()
				&& _timeSinceDownwardsDashRequested < _downwardsDashBuffer
				&& _downwardsDashCount < _downwardsDashLimit
				&& _dashCooldownTimer <= 0f
			)
			{
				TransitionState(MovementStates.DownwardsDash);
			}
			else
			{
				TransitionState(MovementStates.InAir);
			}
		}
	}

	#endregion

	#region Private Methods

	// Crouches the player
	private void CrouchPlayer()
	{
		_heightTween = Tween.Custom(
			_capsuleHeight,
			_crouchCapsuleHeightMult * _defaultCapsuleHeight,
			_crouchTransitionTime,
			newVal => _capsuleHeight = newVal,
			Ease.InOutSine
		);
		_capsuleYOffsetTween = Tween.Custom(
			_capsuleYOffset,
			_crouchCapsuleYOffsetMult * _defaultCapsuleYOffset,
			_crouchTransitionTime,
			newVal => _capsuleYOffset = newVal,
			Ease.InOutSine
		);
		_transformTween = Tween.Custom(
			_meshRoot.transform.localScale,
			new Vector3(1f, 0.5f, 1f),
			_crouchTransitionTime,
			newVal => _meshRoot.transform.localScale = newVal,
			Ease.InOutSine
		);
	}

	// Returns true if the player can uncrouch otherwise false, uncrouches the player if possible
	private bool UncrouchPlayer()
	{
		// Detects a collider above the player, if there is one, don't uncrouch
		if (!CanUncrouch())
		{
			return false;
		}

		_heightTween = Tween.Custom(
			_capsuleHeight,
			_defaultCapsuleHeight,
			_crouchTransitionTime,
			newVal => _capsuleHeight = newVal,
			Ease.InOutSine
		);
		_capsuleYOffsetTween = Tween.Custom(
			_capsuleYOffset,
			_defaultCapsuleYOffset,
			_crouchTransitionTime,
			newVal => _capsuleYOffset = newVal,
			Ease.InOutSine
		);
		_transformTween = Tween.Custom(
			_meshRoot.transform.localScale,
			new Vector3(1f, 1f, 1f),
			_crouchTransitionTime,
			newVal => _meshRoot.transform.localScale = newVal,
			Ease.InOutSine
		);
		return true;
	}

	// Returns true if the player can uncrouch otherwise false, does not uncrouch the player if possible
	private bool CanUncrouch()
	{
		_motor.SetCapsuleDimensions(0.5f, 2f, 1f);
		if (_motor.CharacterCollisionsOverlap(_motor.TransientPosition, _motor.TransientRotation, _probedColliders) > 0)
		{
			_motor.SetCapsuleDimensions(0.5f, 1f, 0.5f);
			return false;
		}

		_motor.SetCapsuleDimensions(0.5f, 1f, 0.5f);
		return true;
	}

	// Stops the player's sprint state
	private void StopSprint()
	{
		_sprintTimer = 0f;
	}

	#endregion

	#region ICharacterController Implementations

	private Vector3 HandleGravity(Vector3 currentVelocity, float deltaTime)
	{
		float yVelocity = currentVelocity.y;
		Vector3 gravityToApply = _gravity * deltaTime;
		if (yVelocity < -_jumpHangInterval)
		{
			gravityToApply *= _gravityFallingMult;
		}
		else if (yVelocity < _jumpHangInterval)
		{
			gravityToApply *= _gravityJumpHangMult;
		}

		return gravityToApply;
	}

	private void HandleSprinting(Vector3 currentVelocity, float deltaTime)
	{
		// The player can start sprinting only if they are on the ground and above a certain velocity
		if (
			!CanSprint
			&& MovementState != MovementStates.Sliding
			&& _motor.GroundingStatus.IsStableOnGround
			&& Vector3.ProjectOnPlane(currentVelocity, _motor.CharacterUp).magnitude > _runningTreshold
		)
		{
			_sprintTimer += deltaTime;
		}
		// If the player is sprinting and above the speed threshold, keep sprinting
		else if (CanSprint && Vector3.ProjectOnPlane(currentVelocity, _motor.CharacterUp).magnitude > _runningTreshold)
		{
			_sprintExpireTimer = _sprintExpireTime;
		}
		// If the player is sprinting and below the speed threshold, start the sprint expire timer
		else if (CanSprint && _sprintExpireTimer > 0)
		{
			_sprintExpireTimer -= deltaTime;
		}
		// Reset sprinting
		else
		{
			_sprintExpireTimer = 0f;
			_sprintTimer = 0f;
		}
	}

	private Vector3 HandleSliding(Vector3 currentVelocity, Vector3 targetVelocityVector, float deltaTime)
	{
		// If the player is sliding down a slope, increase their velocity
		if (MovingDownASlope)
		{
			_movementMult = _slideSlopeSpeedMult;
			_movementAcceleration = _slideBuildUpSmoothing;
			_movementDecelerationToStop = _slideBuildUpSmoothing;
			if (_inputVector != Vector3.zero)
			{
				targetVelocityVector =
					Vector3
						.Lerp(
							currentVelocity,
							targetVelocityVector,
							1 - Mathf.Exp(-_slidingRotationSmoothing * deltaTime)
						)
						.normalized * targetVelocityVector.magnitude;
			}
			else
			{
				targetVelocityVector = currentVelocity.normalized * _baseMovespeed;
			}
		}
		// If the player is not sliding down a slope, decrease their velocity
		else
		{
			_movementMult = _slideSlowDown;
			_movementAcceleration = _slidingDragSmoothing;
			_movementDecelerationToStop = _slidingDragSmoothing;
			if (_inputVector != Vector3.zero)
			{
				targetVelocityVector =
					Vector3
						.Lerp(
							currentVelocity,
							targetVelocityVector,
							1 - Mathf.Exp(-_slidingRotationSmoothing * deltaTime)
						)
						.normalized * currentVelocity.magnitude;
			}
			else
			{
				targetVelocityVector = currentVelocity;
			}
		}

		return targetVelocityVector;
	}

	private void HandleSlide(float deltaTime)
	{
		if (!_motor.GroundingStatus.FoundAnyGround)
		{
			_slideSpeedMultTimer = _slideSpeedMultBuffer;
		}

		_slideSpeedMultTimer -= deltaTime;
		_slideStunTimer -= deltaTime;
	}

	private void HandleDash(float deltaTime)
	{
		// Handle dash stun duration
		if (_dashStunTimer > 0)
		{
			_dashStunTimer -= deltaTime;
		}

		_timeSinceDashRequested += deltaTime;
		_timeSinceDownwardsDashRequested += deltaTime;

		// Handle dash cooldown
		if (_dashCooldownTimer > 0f)
		{
			_dashCooldownTimer -= deltaTime;
		}

		// Reset air dashes and downward dashes when touching ground
		if (_motor.GroundingStatus.IsStableOnGround)
		{
			GetAirDashes = 0;
			_downwardsDashCount = 0;
			OnDashRefresh?.Invoke();
		}
	}

	private void HandleCheckForWall()
	{
		_isCloseToRightWall = Physics.Raycast(
			_motor.transform.position,
			_motor.CharacterRight,
			out _rightWallHit,
			_wallCheckDistance,
			_wallLayers
		);
		_isCloseToLeftWall = Physics.Raycast(
			_motor.transform.position,
			-_motor.CharacterRight,
			out _leftWallHit,
			_wallCheckDistance,
			_wallLayers
		);
		_isCloseToFrontWall = Physics.Raycast(
			_motor.transform.position,
			_motor.CharacterForward,
			out _frontWallHit,
			_wallCheckDistance,
			_wallLayers
		);
		_isCloseToBackWall = Physics.Raycast(
			_motor.transform.position,
			-_motor.CharacterForward,
			out _backWallHit,
			_wallCheckDistance,
			_wallLayers
		);
		_isCloseToWall = _isCloseToRightWall || _isCloseToLeftWall || _isCloseToFrontWall || _isCloseToBackWall;
	}

	private void HandleCapsuleSize()
	{
		if (
			Mathf.Approximately(_targetCapsuleHeight, _capsuleHeight)
			|| Mathf.Approximately(_targetCapsuleYOffset, _targetCapsuleYOffset)
		)
		{
			_motor.SetCapsuleDimensions(_defaultCapsuleRadius, _capsuleHeight, _capsuleYOffset);
		}
	}

	private void HandleDistanceOfGround()
	{
		if (Physics.Raycast(_motor.transform.position, -_motor.CharacterUp, out RaycastHit hit, 15f, _groundLayers))
		{
			_distanceFromGround = hit.distance;
		}
		else
		{
			_distanceFromGround = -1f;
		}
	}

	private void HandleJumpTimers(float deltaTime)
	{
		_timeSinceJumpRequested += deltaTime;
		_jumpedThisFrame = false;
		_jumpCooldownTimer -= deltaTime;
		_wallJumpCooldownTimer -= deltaTime;
	}

	private void HandleUngroundPlayer()
	{
		if (_ungroundPlayer)
		{
			_motor.ForceUnground();
			_ungroundPlayer = false;
		}
	}

	private void HandleRestrictAirRotation(float deltaTime)
	{
		if (RestrictAirRotation)
		{
			_restrictAirMovementTimer -= deltaTime;
		}
	}

	private void HandleBouncePadTaken()
	{
		_bouncePadTaken = false;
	}

	#endregion

	#region Input Methods

	/// <summary>
	///     Takes in the player inputs
	/// </summary>
	private void FeedPlayerInput()
	{
		// Find the vector that the camera is pointing on the character's horizontal plane
		Quaternion cameraRot = _cameraTransform.rotation;
		Vector3 cameraPlanarDirection = Vector3
			.ProjectOnPlane(cameraRot * Vector3.forward, _motor.CharacterUp)
			.normalized;
		if (cameraPlanarDirection.sqrMagnitude == 0f)
		{
			cameraPlanarDirection = Vector3.ProjectOnPlane(cameraRot * Vector3.up, _motor.CharacterUp).normalized;
		}

		// From camera's planar direction, find its rotation
		var cameraPlanarRotation = Quaternion.LookRotation(cameraPlanarDirection, _motor.CharacterUp);

		_inputVector = (cameraPlanarRotation * _rawInputMovement).normalized;
		_lookVector = cameraPlanarDirection;
	}

	private void JumpRequested()
	{
		_timeSinceJumpRequested = 0f;
	}

	private void DashRequested()
	{
		_timeSinceDashRequested = 0f;
	}

	private void ResetAirJumps()
	{
		GetJumps = Math.Clamp(_maxJumps - 1, 0, _maxJumps);
		OnAirJumpsRefresh?.Invoke();
	}

	private void OnMovementInput(Vector2 input)
	{
		_rawInputMovement = new Vector3(input.x, 0, input.y);
	}

	private void OnCrouchStart()
	{
		_crouchDown = true;
	}

	private void OnCrouchRelease()
	{
		_crouchDown = false;
	}

	#endregion

	#region Public Methods

	public void AddVelocity(Vector3 velocity)
	{
		_internalVelocityAdd += velocity;
	}

	public void MultHorVelocityMagnitude(float factor)
	{
		_internalHorVelocityMult = factor;
	}

	public void AddHorizontalVelocity(Vector3 velocity)
	{
		_internalVelocityAdd += Vector3.ProjectOnPlane(velocity, _motor.CharacterUp);
	}

	public void AddVelocityInPlayerInputDirection(float force)
	{
		_internalVelocityAdd += _inputVector.normalized * force;
	}

	public void AddVertVelocity(float force)
	{
		_ungroundPlayer = true;
		_internalVelocityAdd += _motor.CharacterUp.normalized * force;
	}

	public void ZeroAndAddVertVelocity(float force)
	{
		_ungroundPlayer = true;
		_zeroVertVelocity = true;
		_internalVelocityAdd += _motor.CharacterUp.normalized * force;
	}

	public void ChangeVelocityDirection(Vector3 direction)
	{
		_changedVelocityDirection = true;
		_newVelocityDirection = direction;
	}

	public void EnableMovement()
	{
		_zeroVelocity = false;
	}

	public void ZeroMovement()
	{
		_zeroVelocity = true;
	}

	public void EnableGravity()
	{
		_gravityEnabled = true;
	}

	public void ZeroGravity()
	{
		_gravityEnabled = false;
	}

	public void ResetJumps()
	{
		GetJumps = _maxJumps;
		OnAirJumpsRefresh?.Invoke();
	}

	public void ResetDashes()
	{
		GetAirDashes = 0;
		OnDashRefresh?.Invoke();
	}

	public void OnBouncePadTaken()
	{
		_bouncePadTaken = true;
	}

	#endregion
}
