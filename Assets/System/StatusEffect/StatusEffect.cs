using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
using Stats;
using UnityEngine;

namespace StatusEffects
{
	public class StatusEffect
	{
		public StatusEffectSO Data { get; }
		public int Count => (int)Mathf.Clamp(_currBuildUp, 0, Data.MaxCount);
		public bool IsActive => _currBuildUp >= 1f;
		public bool IsApplied => _currBuildUp > 0f;
		public GameObject Source { get; private set; }

		private float _buildUpFreezeTimer;
		private int _lastCount;
		private float _currBuildUp;
		private float _remainingDuration;
		private EventInstance _statusActiveSoundInstance;
		private float _tickTimer;
		private List<Modifier> _modifiersApplied = new();

		// Constructor
		public StatusEffect(StatusEffectSO data)
		{
			Data = data;
		}

		#region Update Methods

		/// <summary>
		///     Called when the effect build up is applied to a target
		/// </summary>
		/// <param name="buildUp">How much build up to apply</param>
		/// <param name="healthController">The target's health controller component</param>
		/// <param name="stats">The target's stats state</param>
		/// <param name="source">The source of the status effect</param>
		/// <param name="target">The target game object</param>
		/// <returns>True if status effect was activated from this build up frame</returns>
		public bool ApplyBuildUp(
			float buildUp,
			HealthController healthController,
			StatsState stats,
			GameObject source,
			GameObject target
		)
		{
			if (source != null)
			{
				Source = source;
			}
			bool wasActive = _currBuildUp >= 1f;
			_buildUpFreezeTimer = Data.BuildUpFreeze;
			_currBuildUp = Mathf.Clamp(_currBuildUp + buildUp, 0f, Data.MaxCount);

			// If the effect hasn't been activated before, activate it
			if (!wasActive && _currBuildUp >= 1f)
			{
				ActivateEffect(healthController, stats, target);
				return true;
			}

			// If the effect has been activated before, trigger repeated hit effects
			if (wasActive && _currBuildUp >= 1f)
			{
				RepeatedHit(healthController, stats, target);
			}

			return false;
		}

		/// <summary>
		///     Updates the effect state
		/// </summary>
		/// <param name="deltaTime">The time since the last frame</param>
		/// <param name="healthController">The target's health controller component</param>
		/// <param name="stats">The target's stats state</param>
		/// <param name="target">The target game object</param>
		/// <returns>Whether the tick was applied or not</returns>
		public bool UpdateEffect(
			float deltaTime,
			HealthController healthController,
			StatsState stats,
			GameObject target
		)
		{
			// If the effect is active decrease it's active duration and start applying ticks
			if (IsActive)
			{
				if (!Data.InfiniteDuration)
				{
					_remainingDuration -= deltaTime;

					if (_remainingDuration <= 0f)
					{
						EndEffect(healthController, stats, target);
						return false;
					}
				}

				_tickTimer += deltaTime;
				if (_tickTimer >= Data.TickRate && Data.EnableTickRate)
				{
					Tick(healthController, target);
					_tickTimer = 0f;
					return true;
				}
			}
			// If the effect is not active and there is a build up value, decrease it
			else if (_currBuildUp > 0)
			{
				// If the build up is not frozen, decrement it
				if (_buildUpFreezeTimer <= 0f)
				{
					_currBuildUp -= Data.BuildUpDecay * deltaTime;
					_currBuildUp = Mathf.Max(0f, _currBuildUp);
				}

				// Decrease the freeze timer
				if (_buildUpFreezeTimer > 0f)
				{
					_buildUpFreezeTimer -= deltaTime;
				}
			}

			return false;
		}

		#endregion

		#region Virutal Methods

		/// <summary>
		///     Called when the effect activates for the first time
		/// </summary>
		/// <param name="healthController">The target's health controller component</param>
		/// <param name="stats">The target's stats state</param>
		/// <param name="target">The target game object</param>
		protected virtual void ActivateEffect(HealthController healthController, StatsState stats, GameObject target)
		{
			_remainingDuration = Data.ActiveDuration;
			_lastCount = Count;

			// Sfx
			PlayStatusEffectSFX(Data.StatusTriggeredSfx, target);
			PlayStatusEffectSFXInstance(Data.StatusActiveSfx, target);

			// Damage
			float stackDamageMult = 1f;
			if (Count > 1)
			{
				stackDamageMult = ((Data.StackDamageMult - 1) * (Count - 1)) + 1;
			}
			healthController.TakeDamage(Data.DamageOnActivation * stackDamageMult);

			// Modifiers
			foreach (Modifier modifier in Data.Modifiers)
			{
				_modifiersApplied.Add(stats.AddModifier(modifier));
			}
		}

		/// <summary>
		///     Called every tick depending on the tick rate during the active duration
		/// </summary>
		/// <param name="healthController">The target's health controller component</param>
		/// <param name="target">The target game object</param>
		protected virtual void Tick(HealthController healthController, GameObject target)
		{
			// On Tick Damage
			float stackDamageMult = 1f;
			if (Count > 1)
			{
				stackDamageMult = ((Data.StackDamageMult - 1) * (Count - 1)) + 1;
			}
			healthController.TakeDamage(Data.DamageOnTick * stackDamageMult);

			// Sfx
			PlayStatusEffectSFX(Data.StatusDotSfx, target);
		}

		/// <summary>
		///     Called when build up is added to an active status effect
		/// </summary>
		/// <param name="healthController">The target's health controller component</param>
		/// <param name="stats">The target's stats state</param>
		/// <param name="target">The target game object</param>
		protected virtual void RepeatedHit(HealthController healthController, StatsState stats, GameObject target)
		{
			if (Data.RefreshableDuration)
			{
				_remainingDuration = Data.ActiveDuration;
			}

			// On Hit Damage
			float stackDamageMult = 1f;
			if (Count > 1)
			{
				stackDamageMult = ((Data.StackDamageMult - 1) * (Count - 1)) + 1;
			}
			healthController.TakeDamage(Data.DamageOnHit * stackDamageMult);

			// New Count Damage
			if (_lastCount > 0 && _lastCount < Count)
			{
				// Deal Reactivation Damage
				healthController.TakeDamage(Data.DamageOnReactivation * stackDamageMult);
				_lastCount = Count;

				// Modifiers
				foreach (Modifier modifier in Data.RepeatedModifiers)
				{
					_modifiersApplied.Add(stats.AddModifier(modifier));
				}

				// Sfx
				PlayStatusEffectSFX(Data.StatusReactivateSfx, target);
			}
		}

		/// <summary>
		///     Called when an effect's active duration ends
		/// </summary>
		/// <param name="healthController">The target's health controller component</param>
		/// <param name="stats">The target's stats state</param>
		/// <param name="target">The target game object</param>
		protected virtual void EndEffect(HealthController healthController, StatsState stats, GameObject target)
		{
			// Deal Damage
			float stackDamageMult = 1f;
			if (Count > 1)
			{
				stackDamageMult = ((Data.StackDamageMult - 1) * (Count - 1)) + 1;
			}
			healthController.TakeDamage(Data.DamageOnExpire * stackDamageMult);

			// Deal With Sfx
			StopStatusEffectSfxInstance(Data.StatusActiveSfx);
			PlayStatusEffectSFX(Data.StatusEndSfx, target);

			// Reset Params
			_tickTimer = 0f;
			_buildUpFreezeTimer = 0f;
			_remainingDuration = 0f;
			_currBuildUp = 0f;
			_lastCount = 0;

			// Deal with modifiers
			foreach (Modifier modifier in _modifiersApplied)
			{
				stats.RemoveModifier(modifier);
			}
			_modifiersApplied.Clear();
		}

		/// <summary>
		///     Called when an effect is canceled early
		/// </summary>
		public virtual void CancelEffect(StatsState stats)
		{
			// Stop Sfx
			StopStatusEffectSfxInstance(Data.StatusActiveSfx);

			// Reset Params
			_tickTimer = 0f;
			_buildUpFreezeTimer = 0f;
			_remainingDuration = 0f;
			_currBuildUp = 0f;
			_lastCount = 0;

			// Deal with modifiers
			foreach (Modifier modifier in _modifiersApplied)
			{
				stats.RemoveModifier(modifier);
			}
			_modifiersApplied.Clear();
		}

		#endregion

		#region SFX

		private void PlayStatusEffectSFX(EventReference eventReference, GameObject target)
		{
			// Check if the sound effect was assigned
			if (!eventReference.IsNull)
			{
				AudioManager.Instance.PlayOneShot(eventReference, target);
			}
		}

		private void PlayStatusEffectSFXInstance(EventReference eventReference, GameObject target)
		{
			if (!eventReference.IsNull)
			{
				if (!_statusActiveSoundInstance.isValid())
				{
					_statusActiveSoundInstance = AudioManager.Instance.CreateInstance(eventReference, target);
				}

				AudioManager.Instance.PlayInstanceAtStart(_statusActiveSoundInstance);
			}
		}

		private void StopStatusEffectSfxInstance(EventReference eventReference)
		{
			if (!eventReference.IsNull)
			{
				AudioManager.Instance.StopInstance(_statusActiveSoundInstance);
				AudioManager.Instance.DestroyInstance(_statusActiveSoundInstance);
			}
		}

		#endregion

		#region Getters & Setters

		/// <summary>
		/// Gets the normalized build up value of the status effect
		/// </summary>
		public float GetBuildUpNormalized()
		{
			return Mathf.Clamp(_currBuildUp / 1f, 0f, 1f);
		}

		/// <summary>
		/// Returns the normalized duration of the status effect, infinite effects are always 1
		/// </summary>
		public float GetRemainingDurationNormalized()
		{
			if (Data.InfiniteDuration)
			{
				return 1f;
			}

			return Mathf.Clamp(_remainingDuration / Data.ActiveDuration, 0f, 1f);
		}

		#endregion
	}
}
