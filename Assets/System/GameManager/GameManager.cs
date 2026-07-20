using System;
using PrimeTween;
using UnityEngine;

public class GameManager : MonoSingleton<GameManager>
{
	private static float _effectTimeScale = 1f; // temp effects

	public float RunTime;
	private Sequence _timeSlowSequence;

	// Events
	public readonly ShootingEvents ShootingEventsRef = new();

	[HideInInspector]
	public Action<bool> OnInteractableEnter;

	[HideInInspector]
	public Action OnInteractableExit;

	[HideInInspector]
	public Action<string> OnItemPickUp;

	public static float BaseTimeScale { get; private set; } = 1f;

	public static float SimulationTimeScale { get; private set; } = 1f;

	public static bool IsPaused { get; private set; }

	// Unity Functions

	private void Start()
	{
		LockCursor();
	}

	// Event Triggers

	public void TriggerOnInteractableEnter(bool isAfterDialogue = false)
	{
		OnInteractableEnter?.Invoke(isAfterDialogue);
	}

	public void TriggerOnInteractableExit()
	{
		OnInteractableExit?.Invoke();
	}

	public void TriggerOnItemPickUp(string itemId)
	{
		OnItemPickUp?.Invoke(itemId);
	}

	// Cursor

	public static void LockCursor()
	{
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
	}

	public static void UnlockCursor()
	{
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;
	}

	// Time

	public void TimeSlow(float targetTimeSlow, float duration)
	{
		_timeSlowSequence.Stop();
		_timeSlowSequence = Sequence
			.Create(useUnscaledTime: true)
			.Chain(
				Tween.Custom(
					this,
					_effectTimeScale,
					targetTimeSlow,
					duration / 4,
					(_, timeScale) =>
					{
						_effectTimeScale = timeScale;
						ApplyTime();
					},
					Ease.OutQuad
				)
			)
			.Chain(
				Tween.Custom(
					this,
					targetTimeSlow,
					1f,
					duration * 3 / 4,
					(_, timeScale) =>
					{
						_effectTimeScale = timeScale;
						ApplyTime();
					},
					Ease.InQuad
				)
			);
	}

	public void CancelTimeSlow()
	{
		if (_timeSlowSequence.isAlive)
		{
			_timeSlowSequence.Stop();
			_effectTimeScale = 1f;
			ApplyTime();
		}
	}

	public static void SetPaused(bool val)
	{
		IsPaused = val;
		ApplyTime();
	}

	public static void SetBaseTimeScale(float val)
	{
		BaseTimeScale = val;
		ApplyTime();
	}

	public static void SetSimulationTimeScale(float val)
	{
		SimulationTimeScale = val;
		ApplyTime();
	}

	private static void ApplyTime()
	{
		Time.timeScale = IsPaused ? 0f : BaseTimeScale * SimulationTimeScale * _effectTimeScale;
	}
}
