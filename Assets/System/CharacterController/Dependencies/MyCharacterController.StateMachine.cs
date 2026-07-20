using System;
using KinematicCharacterController;
using Movement;
using PrimeTween;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

public partial class MyCharacterController
{
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
				_tangentialMovementOnWall = false;
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

				if (ConsumeStamina())
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

				if (ConsumeStamina())
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

				if (ConsumeStamina())
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
					_wallRunInitialHorVelocityMult * CurrentHorVelocity.magnitude,
					0f,
					_wallRunInitialVelocityMax
				);
				AddVelocity(wallRunVelocity * ClosestWallForward.normalized);
				// Reset air jumps
				ResetAirJumps();
				_dragEnabled = false;
				_applyInitialVertDrag = true;
				_gravityEnabled = false;
				_tangentialMovementOnWall = true;
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

				ConsumeStamina();
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
			if (
				_toggleJump
				&& GetJumps > 0
				&& _timeSinceJumpRequested <= _jumpBuffer
				&& _canJump
				&& _jumpCooldownTimer <= 0
			)
			{
				TransitionState(MovementStates.GroundJump);
			}
			// Slide, if the player is pressing the crouch button, and they are moving past a certain threshold or are moving down a slope
			else if (
				_crouchDown
				&& _slideToggle
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
			// Crouch, if the player presses the crouch button and are not fast enough to slide
			else if (_crouchDown && _crouchToggle)
			{
				TransitionState(MovementStates.Crouching);
			}
			// Ground Dash, if the player has stamina to consume, the player has pressed the dash button, are inputting in a direction, and not sliding/crouching/dashing
			else if (
				CanConsumeStamina()
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
				&& CanConsumeStamina()
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
				CanConsumeStamina()
				&& _timeSinceDashRequested < _dashBuffer
				&& GetAirDashes < _airDashLimit
				&& _dashCooldownTimer <= 0f
			)
			{
				TransitionState(MovementStates.AirDashing);
			}
			// Air Dash, if the player has stamina to consume, the player has pressed the dash button, and are inputting in a direction
			else if (
				CanConsumeStamina()
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
}
