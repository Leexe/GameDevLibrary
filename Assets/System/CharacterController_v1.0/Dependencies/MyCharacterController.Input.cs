using System;
using KinematicCharacterController;
using Movement;
using PrimeTween;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

public partial class MyCharacterController
{
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
}
