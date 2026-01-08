using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "HealthEventChannel", menuName = "Events/HealthEventChannel", order = 1)]
public class HealthEventChannelSO : ScriptableObject
{
	/// <summary>
	/// Event triggered when health is initialized. Parameters: currentHealth, maxHealth
	/// </summary>
	public UnityAction<float, float> OnInitiateHealth;

	public void RaiseInitiateHealth(float currentHealth, float maxHealth)
	{
		OnInitiateHealth?.Invoke(currentHealth, maxHealth);
	}

	/// <summary>
	/// Event triggered when health is regenerated. Parameters: delta (amount regenerated), currentHealth, maxHealth
	/// </summary>
	public UnityAction<float, float, float> OnRegen;

	public void RaiseRegen(float delta, float currentHealth, float maxHealth)
	{
		OnRegen?.Invoke(delta, currentHealth, maxHealth);
	}

	/// <summary>
	/// Event triggered when entity is healed. Parameters: delta (amount healed), currentHealth, maxHealth
	/// </summary>
	public UnityAction<float, float, float> OnHeal;

	public void RaiseHeal(float delta, float currentHealth, float maxHealth)
	{
		OnHeal?.Invoke(delta, currentHealth, maxHealth);
	}

	/// <summary>
	/// Event triggered when entity takes damage. Parameters: delta (damage taken), currentHealth, maxHealth
	/// </summary>
	public UnityAction<float, float, float> OnDamage;

	public void RaiseDamage(float delta, float currentHealth, float maxHealth)
	{
		OnDamage?.Invoke(delta, currentHealth, maxHealth);
	}

	/// <summary>
	/// Event triggered when entity is revived. Parameters: currentHealth, maxHealth
	/// </summary>
	public UnityAction<float, float> OnRevive;

	public void RaiseRevive(float currentHealth, float maxHealth)
	{
		OnRevive?.Invoke(currentHealth, maxHealth);
	}

	/// <summary>
	/// Event triggered when entity dies.
	/// </summary>
	public UnityAction OnDeath;

	public void RaiseDeath()
	{
		OnDeath?.Invoke();
	}

	/// <summary>
	/// Event triggered when health changes. Parameters: delta (change amount), currentHealth, maxHealth
	/// </summary>
	public UnityAction<float, float, float> OnHealthChanged;

	public void RaiseHealthChanged(float delta, float currentHealth, float maxHealth)
	{
		OnHealthChanged?.Invoke(delta, currentHealth, maxHealth);
	}
}
