using System;
using KinematicCharacterController;
using Movement;
using PrimeTween;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

public partial class MyCharacterController
{
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
