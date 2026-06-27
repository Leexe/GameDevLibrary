using System.Collections.Generic;
using System.Linq;
using PrimeTween;
using Sirenix.OdinInspector;
using StatusEffects;
using UnityEngine;
using UnityEngine.Events;

public class StatusEffectController : MonoBehaviour
{
	[Header("References")]
	[SerializeField]
	private HealthController _healthController;

	[SerializeField]
	private StatsController _statsController;

	[Header("Status Effects")]
	[Tooltip("How often to check for status effect updates")]
	[SerializeField]
	private float _updateInterval = 0.1f;

	[Header("Status Combos")]
	[Tooltip("Delete the two combo status effects used to produce the result effect")]
	[SerializeField]
	private bool _deleteComboStatuses = true;

	[Tooltip("How long should the two combo status effects stay alive after combo")]
	[ShowIf("_deleteComboStatuses")]
	[SerializeField]
	private float _deleteComboStatusesDelay = 0.3f;

	// Events

	[HideInInspector]
	public UnityEvent<StatusEffect, float> OnStatusEffectApply; // Build Up Normalized

	[HideInInspector]
	public UnityEvent<StatusEffect, float> OnStatusEffectActivate; // Active Duration Normalized

	[HideInInspector]
	public UnityEvent<StatusEffect, float> OnStatusEffectUpdate; // Active Duration Normalized

	[HideInInspector]
	public UnityEvent<StatusEffect> OnStatusEffectTick;

	[HideInInspector]
	public UnityEvent<StatusEffect> OnStatusEffectEnd;

	private readonly List<StatusEffectSO> _effectsToRemove = new();

	// Private Variables

	private readonly Dictionary<StatusEffectSO, StatusEffect> _enabledEffects = new();
	private float _intervalTimer;
	private StatsState Stats => _statsController != null ? _statsController.Stats : null;

	#region Combo System

	// Checks for potential combos for one status effect
	private void CheckForCombos(StatusEffectSO statusEffectSO)
	{
		// If the current status effect is not enabled, return
		if (!_enabledEffects.TryGetValue(statusEffectSO, out StatusEffect statusEffectInstance))
		{
			return;
		}

		// Create a list of potential status effects that combo with the current status effect
		var potentialCombos = _enabledEffects
			.Where(statusTarget => statusEffectInstance.Data.CheckCombo(statusTarget.Key))
			.ToList();

		// If there are no combinations, return
		if (potentialCombos.Count == 0)
		{
			return;
		}

		StatusEffectSO statusEffectTarget = potentialCombos[0].Key; // Take the first potential combo from the list
		StatusEffectSO statusEffectResult = statusEffectInstance.Data.GetCombo(statusEffectTarget);

		if (_deleteComboStatuses)
		{
			Tween.Delay(_deleteComboStatusesDelay, () => CancelStatusEffect(statusEffectSO));
			Tween.Delay(_deleteComboStatusesDelay, () => CancelStatusEffect(statusEffectTarget));
		}

		ApplyStatusEffect(statusEffectResult, 1f, statusEffectInstance.Source);
	}

	#endregion

	#region Unity Methods

	private void OnEnable()
	{
		_healthController.OnDeath.AddListener(CancelAllStatusEffects);
	}

	private void OnDisable()
	{
		if (_healthController != null)
		{
			_healthController.OnDeath.RemoveListener(CancelAllStatusEffects);
		}
	}

	private void Update()
	{
		_intervalTimer += Time.deltaTime;
		if (_intervalTimer >= _updateInterval)
		{
			UpdateStatusEffects(_intervalTimer);
			_intervalTimer %= _updateInterval;
		}

		RemoveStatusEffect();
	}

	private void OnDestroy()
	{
		CancelAllStatusEffects();
	}

	#endregion

	#region Public API

	/// <summary>
	///     Applies status effect to the target
	/// </summary>
	/// <param name="statusEffectSO">The status effect to apply</param>
	/// <param name="buildUp">The amount of build up to apply, status effect triggers when it reaches 1</param>
	public void ApplyStatusEffect(StatusEffectSO statusEffectSO, float buildUp = 1f, GameObject source = null)
	{
		if (statusEffectSO == null || !_healthController.IsAlive)
		{
			return;
		}

		EnableStatusEffect(statusEffectSO);

		StatusEffect statusEffectInstance = _enabledEffects[statusEffectSO];

		OnStatusEffectApply?.Invoke(statusEffectInstance, statusEffectInstance.GetBuildUpNormalized());

		if (statusEffectInstance.ApplyBuildUp(buildUp, _healthController, Stats, source, gameObject))
		{
			OnStatusEffectActivate?.Invoke(statusEffectInstance, statusEffectInstance.GetRemainingDurationNormalized());
			CheckForCombos(statusEffectSO);
		}
	}

	/// <summary>
	///     Cancels a status effect on the target
	/// </summary>
	/// <param name="statusEffectSO">The status effect to cancel</param>
	public void CancelStatusEffect(StatusEffectSO statusEffectSO)
	{
		// If the current status effect is not enabled, return
		if (!_enabledEffects.TryGetValue(statusEffectSO, out StatusEffect statusEffectInstance))
		{
			return;
		}

		statusEffectInstance.CancelEffect(Stats);
		OnStatusEffectEnd?.Invoke(statusEffectInstance);
		_effectsToRemove.Add(statusEffectSO);
	}

	/// <summary>
	///     Cancels all status effects on the target
	/// </summary>
	public void CancelAllStatusEffects()
	{
		KeyValuePair<StatusEffectSO, StatusEffect>[] enabledStatusEffects = _enabledEffects.ToArray();
		foreach (KeyValuePair<StatusEffectSO, StatusEffect> effect in enabledStatusEffects)
		{
			effect.Value.CancelEffect(Stats);
			OnStatusEffectEnd?.Invoke(effect.Value);
		}

		_enabledEffects.Clear();
	}

	#endregion

	#region Internal Logic

	// Adds the status effect to the enable effects dictionary and caches it if needed
	private void EnableStatusEffect(StatusEffectSO statusEffectSO)
	{
		if (!_enabledEffects.ContainsKey(statusEffectSO))
		{
			_enabledEffects[statusEffectSO] = statusEffectSO.CreateInstance();
		}
	}

	// Updates the states of the status effects
	private void UpdateStatusEffects(float deltaTime)
	{
		foreach (KeyValuePair<StatusEffectSO, StatusEffect> statusEffectEntry in _enabledEffects)
		{
			StatusEffect statusEffect = statusEffectEntry.Value;
			bool statusEffectTicked = statusEffect.UpdateEffect(deltaTime, _healthController, Stats, gameObject);

			if (statusEffect.IsActive)
			{
				OnStatusEffectUpdate?.Invoke(statusEffect, statusEffect.GetRemainingDurationNormalized());

				// Check if the status effect has ticked
				if (statusEffectTicked)
				{
					OnStatusEffectTick?.Invoke(statusEffect);
				}
			}
			else
			{
				OnStatusEffectUpdate?.Invoke(statusEffect, statusEffect.GetBuildUpNormalized());
			}
		}

		// If the status effect is not active and has no build up, remove it
		KeyValuePair<StatusEffectSO, StatusEffect>[] expiredStatusEffects = _enabledEffects
			.Where(statusEffect => !statusEffect.Value.IsApplied)
			.ToArray();

		foreach (KeyValuePair<StatusEffectSO, StatusEffect> statusEffect in expiredStatusEffects)
		{
			RemoveStatusEffect(statusEffect.Key);
		}
	}

	// Removes the status effect from the enabled effects dictionary
	private void RemoveStatusEffect(StatusEffectSO statusEffectSO)
	{
		OnStatusEffectEnd?.Invoke(_enabledEffects[statusEffectSO]);
		_enabledEffects.Remove(statusEffectSO);
	}

	private void RemoveStatusEffect()
	{
		foreach (StatusEffectSO statusEffectSO in _effectsToRemove)
		{
			RemoveStatusEffect(statusEffectSO);
		}
	}

	#endregion
}
