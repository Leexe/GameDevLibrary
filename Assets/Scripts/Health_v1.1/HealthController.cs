using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

public class HealthController : MonoBehaviour, IDamageable
{
	#region Settings

	[TabGroup("Details", "General")]
	[Tooltip("How much health the player has")]
	[SerializeField]
	[MinValue(0)]
	private float _maxHealth = 100f;

	[TabGroup("Details", "General")]
	[Tooltip("Enable broadcasting health events via ScriptableObject channel")]
	[SerializeField]
	[ToggleLeft]
	private bool _enableEventChannel;

	[TabGroup("Details", "General")]
	[Tooltip("Event channel to raise events")]
	[SerializeField]
	[ShowIf("_enableEventChannel")]
	[Indent]
	private HealthEventChannelSO _channel;

	[TabGroup("Details", "Regeneration")]
	[Tooltip("Allows the player to regenerate health over time")]
	[SerializeField]
	[ToggleLeft]
	private bool _toggleRegeneration;

	[TabGroup("Details", "Regeneration")]
	[Tooltip("How much health the player heals per second")]
	[ShowIf("_toggleRegeneration")]
	[SerializeField]
	[MinValue(0)]
	[Indent]
	private float _baseRegen = 1f;

	[TabGroup("Details", "Regeneration")]
	[Tooltip("How long after being hit does regeneration start")]
	[ShowIf("_toggleRegeneration")]
	[SerializeField]
	[MinValue(0)]
	[SuffixLabel("seconds")]
	[Indent]
	private float _regenerationDelay;

	[TabGroup("Details", "Invincibility")]
	[Tooltip("If true, the player will be invincible for a short time after taking damage")]
	[SerializeField]
	[ToggleLeft]
	private bool _toggleInvincibilityFrames;

	[TabGroup("Details", "Invincibility")]
	[Tooltip("If true, the player will be invincible for a short time after spawning")]
	[SerializeField]
	[ShowIf("_toggleInvincibilityFrames")]
	[Indent]
	private bool _spawnInvincibility;

	[TabGroup("Details", "Invincibility")]
	[Tooltip("How long the i-frames last after getting damaged")]
	[ShowIf("_toggleInvincibilityFrames")]
	[SerializeField]
	[MinValue(0)]
	[SuffixLabel("seconds")]
	[Indent]
	private float _invincibilityTimeAfterDamage = 1f;

	[TabGroup("Details", "Debug")]
	[Tooltip("When enabled, the entity cannot take damage")]
	[SerializeField]
	[ToggleLeft]
	private bool _godMode;

	#endregion

	#region State

	[field: TabGroup("Details", "Debug")]
	[field: SerializeField]
	public float Health { get; private set; }

	private float _timeSinceHurt;
	private float _regeneration;
	public bool IsDead { get; private set; }

	private bool CanTakeDamage
	{
		get
		{
			if (_godMode)
			{
				return false;
			}

			if (IsDead)
			{
				return false;
			}

			if (_toggleInvincibilityFrames && _timeSinceHurt <= _invincibilityTimeAfterDamage)
			{
				return false;
			}

			return true;
		}
	}

	private bool CanRegenerate
	{
		get
		{
			if (_godMode)
			{
				return false;
			}

			if (IsDead)
			{
				return false;
			}

			if (!_toggleRegeneration || IsFullHealth || _timeSinceHurt <= _regenerationDelay)
			{
				return false;
			}

			return true;
		}
	}

	#endregion

	#region Events

	// Events

	/// <summary>
	///     Event triggered when health is regenerated. Parameters: delta (amount regenerated), currentHealth, maxHealth
	/// </summary>
	[HideInInspector]
	public UnityEvent<float, float, float> OnRegen;

	/// <summary>
	///     Event triggered when entity is healed. Parameters: delta (amount healed), currentHealth, maxHealth
	/// </summary>
	[HideInInspector]
	public UnityEvent<float, float, float> OnHeal;

	/// <summary>
	///     Event triggered when entity takes damage. Parameters: delta (damage taken), currentHealth, maxHealth
	/// </summary>
	[HideInInspector]
	public UnityEvent<float, float, float> OnDamage;

	/// <summary>
	///     Event triggered when entity is revived. Parameters: currentHealth, maxHealth
	/// </summary>
	[HideInInspector]
	public UnityEvent<float, float> OnRevive;

	/// <summary>
	///     Event triggered when entity dies.
	/// </summary>
	[HideInInspector]
	public UnityEvent OnDeath;

	/// <summary>
	///     Event triggered when health changes. Parameters: delta (change amount), currentHealth, maxHealth
	/// </summary>
	[HideInInspector]
	public UnityEvent<float, float, float> OnHealthChanged;

	#endregion

	#region Properties

	// Getters
	public bool IsAlive => !IsDead;
	public bool IsFullHealth => Health >= _maxHealth;

	public float MaxHealth => _maxHealth;
	public float GetNormalizedHealth => _maxHealth > 0 ? Health / _maxHealth : 0;

	#endregion

	#region Initialization

	private void Awake()
	{
		Health = _maxHealth;
		IsDead = false;

		_regeneration = _toggleRegeneration ? _baseRegen : 0f;
		_timeSinceHurt = _toggleInvincibilityFrames && _spawnInvincibility ? 0f : float.MaxValue;
	}

	private void Start()
	{
		InitializeHealth();
	}

	private void InitializeHealth()
	{
		OnHealthChanged?.Invoke(0, Health, _maxHealth);
		_channel?.RaiseHealthChanged(0, Health, _maxHealth);
	}

	#endregion

	#region Logic

	private void Update()
	{
		HandleRegeneration(Time.deltaTime);
		_timeSinceHurt += Time.deltaTime;
	}

	private void HandleRegeneration(float deltaTime)
	{
		if (CanRegenerate)
		{
			float amount = _regeneration * deltaTime;
			Heal(amount);
			OnRegen?.Invoke(amount, Health, _maxHealth);
			_channel?.RaiseRegen(amount, Health, _maxHealth);
		}
	}

	#endregion

	#region Public API

	/// <summary>
	///     Kills the entity
	/// </summary>
	[TabGroup("Details", "Debug")]
	[Button("Kill")]
	public void Kill()
	{
		if (IsDead)
		{
			return;
		}

		float previousHealth = Health;
		Health = 0f;
		IsDead = true;

		OnDeath?.Invoke();
		OnHealthChanged?.Invoke(Health - previousHealth, Health, _maxHealth);
		_channel?.RaiseDeath();
		_channel?.RaiseHealthChanged(Health - previousHealth, Health, _maxHealth);
	}

	/// <summary>
	///     Heals the entity by the specified amount
	/// </summary>
	/// <param name="amount">Amount to heal.</param>
	[TabGroup("Details", "Debug")]
	[Button("Heal")]
	public void Heal(float amount)
	{
		if (IsDead)
		{
			return;
		}

		float previousHealth = Health;
		Health += amount;
		Health = Mathf.Min(Health, _maxHealth);

		float diff = Health - previousHealth;
		if (diff > 0)
		{
			OnHeal?.Invoke(diff, Health, _maxHealth);
			OnHealthChanged?.Invoke(diff, Health, _maxHealth);
			_channel?.RaiseHeal(diff, Health, _maxHealth);
			_channel?.RaiseHealthChanged(diff, Health, _maxHealth);
		}
	}

	/// <summary>
	///     Make the player take the given amount of damage and mark the player as dead when health is less than or equal to 0
	/// </summary>
	/// <param name="damage">Damage to take</param>
	[TabGroup("Details", "Debug")]
	[Button("Take Damage")]
	public void TakeDamage(float damage)
	{
		if (CanTakeDamage)
		{
			Health -= damage;
			_timeSinceHurt = 0f;

			OnDamage?.Invoke(damage, Health, _maxHealth);
			OnHealthChanged?.Invoke(-damage, Health, _maxHealth);
			_channel?.RaiseDamage(damage, Health, _maxHealth);
			_channel?.RaiseHealthChanged(-damage, Health, _maxHealth);

			// If the player is below a certain threshold of health, trigger death
			if (Health <= 0.01f)
			{
				Kill();
			}
		}
	}

	/// <summary>
	///     Revives the player and sets their health to the normalized health amount
	/// </summary>
	/// <param name="healthNormalized">
	///     A value from 0-1 that lerp between 0 and max health, setting the player's health to that
	///     amount.
	/// </param>
	[TabGroup("Details", "Debug")]
	[Button("Revive")]
	public void Revive(float healthNormalized = 1f)
	{
		IsDead = false;
		float previousHealth = Health;
		float healAmount = healthNormalized * _maxHealth;
		Health = Mathf.Clamp(healAmount, 0, _maxHealth);
		float diff = Health - previousHealth;

		OnRevive?.Invoke(Health, _maxHealth);
		OnHealthChanged?.Invoke(diff, Health, _maxHealth);
		_channel?.RaiseRevive(Health, _maxHealth);
		_channel?.RaiseHealthChanged(diff, Health, _maxHealth);
	}

	/// <summary>
	///     Changes the regeneration rate
	/// </summary>
	/// <param name="regenRate">Amount of healing per second</param>
	public void SetRegeneration(float regenRate)
	{
		_regeneration = regenRate;
	}

	/// <summary>
	///     Sets the player's health based on the normalized value
	/// </summary>
	/// <param name="healthNormalized">
	///     A value from 0-1 that lerp between 0 and max health, setting the player's health to that
	///     amount
	/// </param>
	public void SetHealth(float healthNormalized = 1f)
	{
		float previousHealth = Health;
		Health = Mathf.Clamp(_maxHealth * healthNormalized, 0, _maxHealth);
		float diff = Health - previousHealth;

		OnHealthChanged?.Invoke(diff, Health, _maxHealth);
		_channel?.RaiseHealthChanged(diff, Health, _maxHealth);
	}

	/// <summary>
	///     Sets the maximum health of the entity
	/// </summary>
	/// <param name="newMax">The new value for maximum health.</param>
	/// <param name="healToFull">If true, sets current health to current max health.</param>
	[TabGroup("Details", "Debug")]
	[Button("Set Max Health")]
	public void SetMaxHealth(float newMax, bool healToFull = false)
	{
		float previousHealth = Health;
		_maxHealth = newMax;
		if (healToFull)
		{
			Health = _maxHealth;
		}
		else
		{
			// Clamp current health if it exceeds new max
			if (Health > _maxHealth)
			{
				Health = _maxHealth;
			}
		}

		float diff = Health - previousHealth;
		OnHealthChanged?.Invoke(diff, Health, _maxHealth);
		_channel?.RaiseHealthChanged(diff, Health, _maxHealth);
	}

	#endregion
}
