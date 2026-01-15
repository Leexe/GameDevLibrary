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
	private HealthController _healthManager;

	[SerializeField]
	private StatusEffectDictionary _statusEffectDictionary;

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

	// Private Variables

	private readonly Dictionary<Effect, StatusEffect> _enabledEffects = new();
	private readonly Dictionary<Effect, StatusEffect> _statusEffectCachedDict = new();
	private float _intervalTimer;

	#region Public API

	// Apply status effects when called
	public void ApplyStatusEffect(Effect statusEffect, float buildUp = 1f)
	{
		if (statusEffect == Effect.None || !_healthManager.IsAlive)
		{
			return;
		}

		EnableStatusEffect(statusEffect);

		OnStatusEffectApply?.Invoke(
			_statusEffectCachedDict[statusEffect],
			_statusEffectCachedDict[statusEffect].GetBuildUpNormalized()
		);

		// Checks if the effect was activated this build up frame
		if (_statusEffectCachedDict[statusEffect].ApplyBuildUp(buildUp, _healthManager, gameObject))
		{
			OnStatusEffectActivate?.Invoke(
				_statusEffectCachedDict[statusEffect],
				_statusEffectCachedDict[statusEffect].GetRemainingDurationNormalized()
			);
			CheckForCombos(statusEffect);
		}
	}

	#endregion

	#region Combo System

	// Checks for potential combos for one status effect
	private void CheckForCombos(Effect statusEffect)
	{
		// If the current status effect is not enabled, return
		if (!_enabledEffects.TryGetValue(statusEffect, out StatusEffect statusEffectInstance))
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

		Effect statusEffectTarget = potentialCombos[0].Key; // Take the first potential combo from the list
		Effect statusEffectResult = statusEffectInstance.Data.GetComboResult(statusEffectTarget);
		if (_deleteComboStatuses)
		{
			Tween.Delay(_deleteComboStatusesDelay, () => CancelStatusEffect(statusEffect));
			Tween.Delay(_deleteComboStatusesDelay, () => CancelStatusEffect(statusEffectTarget));
		}

		ApplyStatusEffect(statusEffectResult);
	}

	#endregion

	#region Unity Methods

	private void Start()
	{
		_healthManager.OnDeath.AddListener(CancelAllStatusEffects);
	}

	private void Update()
	{
		_intervalTimer += Time.deltaTime;
		if (_intervalTimer >= _updateInterval)
		{
			UpdateStatusEffects(_intervalTimer);
			_intervalTimer %= _updateInterval;
		}
	}

	#endregion

	#region Internal Logic

	// Adds the status effect to the enable effects dictionary and caches it if needed
	private void EnableStatusEffect(Effect statusEffect)
	{
		if (!_enabledEffects.ContainsKey(statusEffect))
		{
			// Cache the status effect if It's not already cached
			if (!_statusEffectCachedDict.ContainsKey(statusEffect))
			{
				if (_statusEffectDictionary.EffectDictionary.TryGetValue(statusEffect, out StatusEffectSO value))
				{
					_statusEffectCachedDict[statusEffect] = new StatusEffect(value);
				}
				else
				{
					Debug.LogWarning($"Status Effect: {statusEffect.ToString()} Not Cached");
				}
			}

			_enabledEffects[statusEffect] = _statusEffectCachedDict[statusEffect];
		}
	}

	// Updates the states of the status effects
	private void UpdateStatusEffects(float deltaTime)
	{
		foreach (KeyValuePair<Effect, StatusEffect> statusEffectEntry in _enabledEffects)
		{
			StatusEffect statusEffect = statusEffectEntry.Value;
			bool statusEffectTicked = statusEffect.UpdateEffect(deltaTime, _healthManager, gameObject);
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
		KeyValuePair<Effect, StatusEffect>[] expiredStatusEffects = _enabledEffects
			.Where(statusEffect => !statusEffect.Value.IsApplied)
			.ToArray();
		foreach (KeyValuePair<Effect, StatusEffect> statusEffect in expiredStatusEffects)
		{
			RemoveStatusEffect(statusEffect.Key);
		}
	}

	// Removes the status effect from the enabled effects dictionary
	private void RemoveStatusEffect(Effect statusEffect)
	{
		OnStatusEffectEnd?.Invoke(_enabledEffects[statusEffect]);
		_enabledEffects.Remove(statusEffect);
	}

	// Cancels the status effect
	private void CancelStatusEffect(Effect statusEffect)
	{
		// If the current status effect is not enabled, return
		if (!_enabledEffects.TryGetValue(statusEffect, out StatusEffect statusEffectInstance))
		{
			return;
		}

		statusEffectInstance.CancelEffect();
		OnStatusEffectEnd?.Invoke(_enabledEffects[statusEffect]);
	}

	// Removes all status effects
	private void CancelAllStatusEffects()
	{
		KeyValuePair<Effect, StatusEffect>[] enabledStatusEffects = _enabledEffects.Where(_ => true).ToArray();
		foreach (KeyValuePair<Effect, StatusEffect> effect in enabledStatusEffects)
		{
			effect.Value.CancelEffect();
			OnStatusEffectEnd?.Invoke(_enabledEffects[effect.Key]);
		}
	}

	#endregion
}
