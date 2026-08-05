using KinematicCharacterController;
using Movement;
using UnityEngine;

public partial class MyCharacterController
{
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
					currentVelocity.y * _wallRunInitialVertVelocityMult,
					currentVelocity.z
				);
				_applyInitialVertDrag = false;
			}

			// Check if the player is on stable ground i.e. not on a slope or in the air
			if (_motor.GroundingStatus.IsStableOnGround)
			{
				// Calculate max movement vector
				var inputRight = Vector3.Cross(_inputVector, _motor.CharacterUp);
				Vector3 reorientedInputVector =
					Vector3.Cross(_motor.GroundingStatus.GroundNormal, inputRight).normalized * _inputVector.magnitude;
				Vector3 targetVelocityVector = reorientedInputVector * _baseMovespeed;

				if (MovementState == MovementStates.Sliding)
				{
					targetVelocityVector = HandleSliding(currentVelocity, targetVelocityVector, deltaTime);
					currentVelocity = Vector3.Lerp(
						currentVelocity,
						targetVelocityVector * _movementMult,
						1 - Mathf.Exp(-_movementAcceleration * deltaTime)
					);
				}
				else
				{
					// If the player is inputting
					if (_inputVector != Vector3.zero)
					{
						Vector3 turnVector = currentVelocity;
						if (!_toggleMomentumBasedMovement)
						{
							turnVector = Quaternion.FromToRotation(currentVelocity, targetVelocityVector) * currentVelocity;
						}

						// If player is moving faster than standard sprint speed, drag them down slowly to preserve momentum
						if (CurrentHorVelocity.magnitude >= _sprintSpeedMult * _baseMovespeed)
						{
							currentVelocity = Vector3.Lerp(
								turnVector,
								targetVelocityVector * _movementMult,
								!_toggleMomentumBasedMovement 
									? _movementDeceleration * deltaTime 
									: 1 - Mathf.Exp(-_movementDeceleration * deltaTime)
							);
						}
						// Otherwise, use standard acceleration
						else
						{
							currentVelocity = Vector3.Lerp(
								turnVector,
								targetVelocityVector * _movementMult,
								1 - Mathf.Exp(-_movementAcceleration * deltaTime)
							);
						}
					}
					// If not inputting, stop moving
					else
					{
						currentVelocity = Vector3.Lerp(
							currentVelocity,
							Vector3.zero,
							1 - Mathf.Exp(-_movementDecelerationToStop * deltaTime)
						);
					}
				}
			}
			// If the player is in the air or on a slope
			else
			{
				// Add air movement if the player is inputting in the air
				if (_inputVector.sqrMagnitude > 0f)
				{
					Vector3 wishDir = _inputVector.normalized;
					float wishSpeed = _baseMovespeed * _airBaseSpeedMult;

					// Preventing climbing on unstable ground while in the air
					if (_motor.GroundingStatus.FoundAnyGround)
					{
						Vector3 perpendicularObstructionNormal = Vector3
							.Cross(
								Vector3.Cross(_motor.CharacterUp, _motor.GroundingStatus.GroundNormal),
								_motor.CharacterUp
							)
							.normalized;
						wishDir = Vector3.ProjectOnPlane(wishDir, perpendicularObstructionNormal).normalized;
					}

					// If the player is tangent to a wall, lock their movement against the wall
					if (_tangentialMovementOnWall)
					{
						wishDir = ClosestWallForward;
						wishSpeed = _wallRunVelocityExitThreshold - 1f;
					}

					float accel = _movementAcceleration;
					if (RestrictAirRotation)
					{
						accel *= _airRotationRestrictionMult;
					}

					var currentHorizontal = Vector3.ProjectOnPlane(currentVelocity, _motor.CharacterUp);
					float previousHorSpeed = currentHorizontal.magnitude;

					currentVelocity = Accelerate(currentVelocity, wishDir, wishSpeed, accel, deltaTime);

					if (!_allowAirStrafing)
					{
						var newHorizontal = Vector3.ProjectOnPlane(currentVelocity, _motor.CharacterUp);
						float cap = Mathf.Max(previousHorSpeed, wishSpeed);
						if (newHorizontal.magnitude > cap)
						{
							newHorizontal = newHorizontal.normalized * cap;
							currentVelocity = newHorizontal + Vector3.Project(currentVelocity, _motor.CharacterUp);
						}
					}
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

		// Set local variable _currentVelocity to currentVelocity
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
			float retention = _landingSlowDownMult;

			// If they landed and are instantly jumping again, don't slow them down
			if (_timeSinceJumpRequested <= _jumpBuffer)
			{
				OnBunnyHop?.Invoke();
				retention = _bunnyHopSlowDownMult;
			}

			// Multiply the horizontal velocity by the retention amount.
			MultHorVelocityMagnitude(retention);
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
}
