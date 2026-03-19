using System;
using KinematicCharacterController;
using Movement;
using PrimeTween;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

public partial class MyCharacterController
{
	#region Movement Helpers

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
			&& Vector3.ProjectOnPlane(currentVelocity, _motor.CharacterUp).magnitude > _runningThreshold
		)
		{
			_sprintTimer += deltaTime;
		}
		// If the player is sprinting and above the speed threshold, keep sprinting
		else if (CanSprint && Vector3.ProjectOnPlane(currentVelocity, _motor.CharacterUp).magnitude > _runningThreshold)
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
}
