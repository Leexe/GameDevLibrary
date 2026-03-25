using FMOD.Studio;
using FMODUnity;
using UnityEngine;

namespace StatusEffects
{
	public class StatusEffect
	{
		private float _buildUpFreezeTimer;
		private float _currBuildUp;

		private float _remainingDuration;
		private EventInstance _statusActiveSoundInstance;
		private float _tickTimer;

		public StatusEffect(StatusEffectSO data)
		{
			Data = data;
		}

		public StatusEffectSO Data { get; }

		public int Count => (int)Mathf.Clamp(_currBuildUp, 0, Data.MaxCount);
		public bool IsActive => _currBuildUp >= 1f;
		public bool IsApplied => _currBuildUp > 0f;

		#region Update Methods

		/// <summary>
		///     Called when the effect build up is applied to a target,returns true when status effect was activated this build up
		///     frame
		/// </summary>
		/// <param name="buildUp">How much build up to apply</param>
		/// <param name="targetHealthManger">The target's health manager</param>
		/// <param name="target">The target game object</param>
		/// <returns></returns>
		public bool ApplyBuildUp(float buildUp, HealthController targetHealthManger, GameObject target)
		{
			bool wasActive = _currBuildUp >= 1f;
			_buildUpFreezeTimer = Data.BuildUpFreeze;
			_currBuildUp += buildUp;

			// If the effect hasn't been activated before, activate it
			if (!wasActive && _currBuildUp >= 1f)
			{
				ActivateEffect(targetHealthManger, target);
				return true;
			}
			// If the effect has been activated before, trigger repeated hit effects

			if (wasActive && _currBuildUp >= 1f)
			{
				RepeatedHit(targetHealthManger, target);
			}

			return false;
		}

		/// <summary>
		///     Updates the effect state
		/// </summary>
		/// <param name="deltaTime">The time since the last frame</param>
		/// <param name="targetHealthManger">The target's health manager</param>
		/// <param name="target">The target game object</param>
		/// <returns>Whether the tick was applied or not</returns>
		public bool UpdateEffect(float deltaTime, HealthController targetHealthManger, GameObject target)
		{
			// If the effect is active decrease it's active duration and start applying ticks
			if (IsActive)
			{
				_remainingDuration -= deltaTime;
				if (_remainingDuration <= 0f)
				{
					EndEffect(targetHealthManger, target);
				}

				_tickTimer += deltaTime;
				if (_tickTimer >= Data.TickRate && Data.EnableTickRate)
				{
					Tick(targetHealthManger, target);
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
		/// <param name="targetHealthManger">The target's health manager</param>
		/// <param name="target">The target game object</param>
		protected virtual void ActivateEffect(HealthController targetHealthManger, GameObject target)
		{
			_remainingDuration = Data.ActiveDuration;
			PlayStatusEffectSFX(Data.StatusTriggeredSfx, target);
			PlayStatusEffectSFXInstance(Data.StatusActiveSfx, target);
			targetHealthManger.TakeDamage(Data.DamageOnActivation);
		}

		/// <summary>
		///     Called every tick depending on the tick rate during the active duration
		/// </summary>
		/// <param name="targetHealthManger">The target's health manager</param>
		/// <param name="target">The target game object</param>
		protected virtual void Tick(HealthController targetHealthManger, GameObject target)
		{
			targetHealthManger.TakeDamage(Data.DamagePerTick);
			PlayStatusEffectSFX(Data.StatusDOTSfx, target);
		}

		/// <summary>
		///     Called when build up is added to an active status effect
		/// </summary>
		/// <param name="targetHealthManger">The target's health manager</param>
		/// <param name="target">The target game object</param>
		protected virtual void RepeatedHit(HealthController targetHealthManger, GameObject target)
		{
			if (Data.RefreshableDuration)
			{
				_remainingDuration = Data.ActiveDuration;
			}
		}

		/// <summary>
		///     Called when an effect's active duration ends
		/// </summary>
		/// <param name="targetHealthManger">The target's health manager</param>
		/// <param name="target">The target game object</param>
		protected virtual void EndEffect(HealthController targetHealthManger, GameObject target)
		{
			targetHealthManger.TakeDamage(Data.DamageOnExpire);
			StopStatusEffectSfxInstance(Data.StatusActiveSfx);
			PlayStatusEffectSFX(Data.StatusEndSfx, target);
			_tickTimer = 0f;
			_buildUpFreezeTimer = 0f;
			_remainingDuration = 0f;
			_currBuildUp = 0f;
		}

		/// <summary>
		///     Called when an effect is canceled early
		/// </summary>
		public virtual void CancelEffect()
		{
			StopStatusEffectSfxInstance(Data.StatusActiveSfx);
			_tickTimer = 0f;
			_buildUpFreezeTimer = 0f;
			_remainingDuration = 0f;
			_currBuildUp = 0f;
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
			}
		}

		#endregion

		#region Getters & Setters

		public float GetBuildUpNormalized()
		{
			return Mathf.Clamp(_currBuildUp / 1f, 0f, 1f);
		}

		public float GetRemainingDurationNormalized()
		{
			return Mathf.Clamp(_remainingDuration / Data.ActiveDuration, 0f, 1f);
		}

		#endregion
	}
}
