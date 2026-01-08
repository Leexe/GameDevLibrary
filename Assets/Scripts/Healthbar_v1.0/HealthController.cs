using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

public class HealthController : MonoBehaviour, IDamageable
{
	#region Settings

	[FoldoutGroup("Health")]
	[Tooltip("How much health the player has")]
	[SerializeField]
	[MinValue(0)]
	private float _maxHealth = 100f;

	[FoldoutGroup("Regeneration")]
	[Tooltip("Allows the player to regenerate health over time")]
	[SerializeField]
	[ToggleLeft]
	private bool _toggleRegeneration;

	[FoldoutGroup("Regeneration")]
	[Tooltip("How much health the player heals per second")]
	[ShowIf("_toggleRegeneration")]
	[SerializeField]
	[MinValue(0)]
	[Indent]
	private float _baseRegen = 1f;

	[FoldoutGroup("Regeneration")]
	[Tooltip("How long after being hit does regeneration start")]
	[ShowIf("_toggleRegeneration")]
	[SerializeField]
	[MinValue(0)]
	[SuffixLabel("seconds")]
	[Indent]
	private float _regenerationDelay;

	[FoldoutGroup("Invincibility")]
	[Tooltip("If true, the player will be invincible for a short time after taking damage")]
	[SerializeField]
	[ToggleLeft]
	private bool _toggleInvincibilityFrames;

	[FoldoutGroup("Invincibility")]
	[Tooltip("If true, the player will be invincible for a short time after spawning")]
	[SerializeField]
	[ShowIf("_toggleInvincibilityFrames")]
	[Indent]
	private bool _spawnInvincibility;

	[FoldoutGroup("Invincibility")]
	[Tooltip("How long the i-frames last after getting damaged")]
	[ShowIf("_toggleInvincibilityFrames")]
	[SerializeField]
	[MinValue(0)]
	[SuffixLabel("seconds")]
	[Indent]
	private float _invincibilityTimeAfterDamage = 1f;

	[FoldoutGroup("Event Channel")]
	[Tooltip("Enable broadcasting health events via ScriptableObject channel")]
	[SerializeField]
	[ToggleLeft]
	private bool _enableChannel;

	[FoldoutGroup("Event Channel")]
	[Tooltip("Event channel to raise events")]
	[SerializeField]
	[ShowIf("_enableChannel")]
	[Indent]
	private HealthEventChannelSO _channel;

	[FoldoutGroup("Debug")]
	[Tooltip("When enabled, the entity cannot take damage")]
	[SerializeField]
	[ToggleLeft]
	private bool _godMode;

	#endregion

	#region State

	private float _timeSinceHurt;
	private float _regeneration;
	private float _health;
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
	/// Event triggered when health is initialized. Parameters: currentHealth, maxHealth
	/// </summary>
	[HideInInspector]
	public UnityEvent<float, float> OnInitiateHealth;

	/// <summary>
	/// Event triggered when health is regenerated. Parameters: delta (amount regenerated), currentHealth, maxHealth
	/// </summary>
	[HideInInspector]
	public UnityEvent<float, float, float> OnRegen;

	/// <summary>
	/// Event triggered when entity is healed. Parameters: delta (amount healed), currentHealth, maxHealth
	/// </summary>
	[HideInInspector]
	public UnityEvent<float, float, float> OnHeal;

	/// <summary>
	/// Event triggered when entity takes damage. Parameters: delta (damage taken), currentHealth, maxHealth
	/// </summary>
	[HideInInspector]
	public UnityEvent<float, float, float> OnDamage;

	/// <summary>
	/// Event triggered when entity is revived. Parameters: currentHealth, maxHealth
	/// </summary>
	[HideInInspector]
	public UnityEvent<float, float> OnRevive;

	/// <summary>
	/// Event triggered when entity dies.
	/// </summary>
	[HideInInspector]
	public UnityEvent OnDeath;

	/// <summary>
	/// Event triggered when health changes. Parameters: delta (change amount), currentHealth, maxHealth
	/// </summary>
	[HideInInspector]
	public UnityEvent<float, float, float> OnHealthChanged;

	#endregion

	#region Properties

	// Getters
	public bool IsAlive => !IsDead;
	public bool IsFullHealth => _health >= _maxHealth;
	public float Health => _health;
	public float MaxHealth => _maxHealth;
	public float GetNormalizedHealth => _maxHealth > 0 ? _health / _maxHealth : 0;

	#endregion

	#region Initialization

	private void Start()
	{
		InitializeHealth();
	}

	private void InitializeHealth()
	{
		_health = _maxHealth;
		IsDead = false;

		_regeneration = _toggleRegeneration ? _baseRegen : 0f;
		_timeSinceHurt = _toggleInvincibilityFrames && _spawnInvincibility ? 0f : float.MaxValue;

		OnInitiateHealth?.Invoke(_health, _maxHealth);
		OnHealthChanged?.Invoke(0, _health, _maxHealth);
		_channel?.RaiseInitiateHealth(_health, _maxHealth);
		_channel?.RaiseHealthChanged(0, _health, _maxHealth);
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
			OnRegen?.Invoke(amount, _health, _maxHealth);
			_channel?.RaiseRegen(amount, _health, _maxHealth);
		}
	}

	#endregion

	#region Public API

	/// <summary>
	/// Kills the entity
	/// </summary>
	[FoldoutGroup("Debug")]
	[Button("Kill")]
	public void Kill()
	{
		if (IsDead)
		{
			return;
		}

		float previousHealth = _health;
		_health = 0f;
		IsDead = true;

		OnDeath?.Invoke();
		OnHealthChanged?.Invoke(_health - previousHealth, _health, _maxHealth);
		_channel?.RaiseDeath();
		_channel?.RaiseHealthChanged(_health - previousHealth, _health, _maxHealth);
	}

	/// <summary>
	/// Heals the entity by the specified amount
	/// </summary>
	/// <param name="amount">Amount to heal.</param>
	[FoldoutGroup("Debug")]
	[Button("Heal")]
	public void Heal(float amount)
	{
		if (IsDead)
		{
			return;
		}

		float previousHealth = _health;
		_health += amount;
		_health = Mathf.Min(_health, _maxHealth);

		float diff = _health - previousHealth;
		if (diff > 0)
		{
			OnHeal?.Invoke(diff, _health, _maxHealth);
			OnHealthChanged?.Invoke(diff, _health, _maxHealth);
			_channel?.RaiseHeal(diff, _health, _maxHealth);
			_channel?.RaiseHealthChanged(diff, _health, _maxHealth);
		}
	}

	/// <summary>
	/// Make the player take the given amount of damage and mark the player as dead when health is less than or equal to 0
	/// </summary>
	/// <param name="damage">Damage to take</param>
	[FoldoutGroup("Debug")]
	[Button("Take Damage")]
	public void TakeDamage(float damage)
	{
		if (CanTakeDamage)
		{
			_health -= damage;
			_timeSinceHurt = 0f;

			OnDamage?.Invoke(damage, _health, _maxHealth);
			OnHealthChanged?.Invoke(-damage, _health, _maxHealth);
			_channel?.RaiseDamage(damage, _health, _maxHealth);
			_channel?.RaiseHealthChanged(-damage, _health, _maxHealth);

			// If the player is below a certain threshold of health, trigger death
			if (_health <= 0.01f)
			{
				Kill();
			}
		}
	}

	/// <summary>
	/// Revives the player and sets their health to the normalized health amount
	/// </summary>
	/// <param name="healthNormalized">A value from 0-1 that lerp between 0 and max health, setting the player's health to that amount.</param>
	[FoldoutGroup("Debug")]
	[Button("Revive")]
	public void Revive(float healthNormalized = 1f)
	{
		IsDead = false;
		float previousHealth = _health;
		float healAmount = healthNormalized * _maxHealth;
		_health = Mathf.Clamp(healAmount, 0, _maxHealth);
		float diff = _health - previousHealth;

		OnRevive?.Invoke(_health, _maxHealth);
		OnHealthChanged?.Invoke(diff, _health, _maxHealth);
		_channel?.RaiseRevive(_health, _maxHealth);
		_channel?.RaiseHealthChanged(diff, _health, _maxHealth);
	}

	/// <summary>
	/// Changes the regeneration rate
	/// </summary>
	/// <param name="regenRate">Amount of healing per second</param>
	public void SetRegeneration(float regenRate)
	{
		_regeneration = regenRate;
	}

	/// <summary>
	/// Sets the player's health based on the normalized value
	/// </summary>
	/// <param name="healthNormalized">A value from 0-1 that lerp between 0 and max health, setting the player's health to that amount</param>
	public void SetHealth(float healthNormalized = 1f)
	{
		float previousHealth = _health;
		_health = Mathf.Clamp(_maxHealth * healthNormalized, 0, _maxHealth);
		float diff = _health - previousHealth;

		OnHealthChanged?.Invoke(diff, _health, _maxHealth);
		_channel?.RaiseHealthChanged(diff, _health, _maxHealth);
	}

	/// <summary>
	/// Sets the maximum health of the entity
	/// </summary>
	/// <param name="newMax">The new value for maximum health.</param>
	/// <param name="healToFull">If true, sets current health to current max health.</param>
	[FoldoutGroup("Debug")]
	[Button("Set Max Health")]
	public void SetMaxHealth(float newMax, bool healToFull = false)
	{
		float previousHealth = _health;
		_maxHealth = newMax;
		if (healToFull)
		{
			_health = _maxHealth;
		}
		else
		{
			// Clamp current health if it exceeds new max
			if (_health > _maxHealth)
			{
				_health = _maxHealth;
			}
		}

		float diff = _health - previousHealth;
		OnHealthChanged?.Invoke(diff, _health, _maxHealth);
		OnInitiateHealth?.Invoke(_health, _maxHealth);
		_channel?.RaiseHealthChanged(diff, _health, _maxHealth);
		_channel?.RaiseInitiateHealth(_health, _maxHealth);
	}

	#endregion
}
