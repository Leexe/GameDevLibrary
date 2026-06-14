using System;
using System.Collections.Generic;
using Stats;
using UnityEngine;

/* How to add new stats (they have to be floats):
   1. Go to the Stats namespace and add your new stat to the StatType enum.
   2. Go to the PlayerStats Scriptable Object and Generate a new Dictionary
   3. Change the initial values of the base stats
*/

public class StatsState
{
	private readonly Dictionary<StatType, float> _baseStatsMap;

	private readonly Dictionary<StatType, float> _finalStats = new();

	/// <summary>
	/// Event invoked when a stat's value changes.
	/// </summary>
	public event Action<StatChangedEventArgs> OnStatChanged;

	/// <summary>
	/// Gets the final calculated value of a stat after all modifiers are applied.
	/// </summary>
	/// <param name="statType">The stat type to retrieve.</param>
	/// <returns>The final stat value with all modifiers applied.</returns>
	public float GetFinalStat(StatType statType) => _finalStats.GetValueOrDefault(statType, 0f);

	// Stores stat changes aka modifiers
	private readonly List<Modifier> _modifiersList = new();

	// Constructor
	public StatsState(IReadOnlyDictionary<StatType, float> baseStatsMap)
	{
		_baseStatsMap = new Dictionary<StatType, float>(baseStatsMap);
		UpdateAllStats();
	}

	/// <summary>
	/// Adds a flat value modifier to a stat.
	/// </summary>
	/// <param name="statType">The stat to modify.</param>
	/// <param name="flatAdd">The flat value to add (e.g., +5 speed).</param>
	/// <returns>The created Modifier, which can be passed to RemoveModifier.</returns>
	public Modifier AddFlat(StatType statType, float flatAdd)
	{
		return AddModifier(statType, flatAdd: flatAdd);
	}

	/// <summary>
	/// Adds a percentage increase modifier to a stat.
	/// </summary>
	/// <param name="statType">The stat to modify.</param>
	/// <param name="percentIncreaseAdd">The percentage increase as a decimal (e.g., 0.2 for +20%).</param>
	/// <returns>The created Modifier, which can be passed to RemoveModifier.</returns>
	public Modifier PercentIncrease(StatType statType, float percentIncreaseAdd)
	{
		return AddModifier(statType, percentIncreaseAdd: percentIncreaseAdd);
	}

	/// <summary>
	/// Adds a base multiplier modifier to a stat.
	/// </summary>
	/// <param name="statType">The stat to modify.</param>
	/// <param name="modifierMult">The multiplier to apply to base (e.g., 2 for double).</param>
	/// <returns>The created Modifier, which can be passed to RemoveModifier.</returns>
	public Modifier BaseMult(StatType statType, float modifierMult)
	{
		return AddModifier(statType, modifierMult: modifierMult);
	}

	/// <summary>
	/// Adds a final multiplier modifier to a stat (applied after all other modifiers).
	/// </summary>
	/// <param name="statType">The stat to modify.</param>
	/// <param name="finalMult">The final multiplier to apply (e.g., 0.5 for half).</param>
	/// <returns>The created Modifier, which can be passed to RemoveModifier.</returns>
	public Modifier FinalMult(StatType statType, float finalMult)
	{
		return AddModifier(statType, finalMult: finalMult);
	}

	/// <summary>
	/// Adds a modifier to a stat.
	/// </summary>
	/// <param name="statType">The stat to modify.</param>
	/// <param name="flatAdd">Flat value to add (e.g., +5 speed).</param>
	/// <param name="percentIncreaseAdd">Additive percentage increase (e.g., 0.2 for +20%).</param>
	/// <param name="modifierMult">Multiplicative modifier (e.g., 2 for double).</param>
	/// <param name="finalMult">Multiplier applied after everything (e.g., 2 for double) </param>
	/// <returns>The created Modifier, which can be passed to RemoveModifier.</returns>
	public Modifier AddModifier(
		StatType statType,
		float flatAdd = 0f,
		float percentIncreaseAdd = 0f,
		float modifierMult = 1f,
		float finalMult = 1f
	)
	{
		var newModifier = new Modifier
		{
			StatName = statType,
			FlatAdd = flatAdd,
			PercentIncreaseAdd = percentIncreaseAdd,
			ModifierMult = modifierMult,
			FinalMult = finalMult,
		};
		_modifiersList.Add(newModifier);
		UpdateStat(statType);
		return newModifier;
	}

	public Modifier AddModifier(Modifier modifier)
	{
		return AddModifier(
			modifier.StatName,
			modifier.FlatAdd,
			modifier.PercentIncreaseAdd,
			modifier.ModifierMult,
			modifier.FinalMult
		);
	}

	/// <summary>
	/// Removes a previously added modifier.
	/// </summary>
	/// <param name="modifier">The modifier to remove (returned from AddModifier).</param>
	public void RemoveModifier(Modifier modifier)
	{
		StatType affectedStat = modifier.StatName;
		_modifiersList.Remove(modifier);
		UpdateStat(affectedStat);
	}

	/// <summary>
	/// Updates an existing modifier's values and recalculates the affected stat
	/// </summary>
	/// <param name="modifier">The modifier reference to update.</param>
	/// <param name="flatAdd">Flat value to add (e.g., +5 speed).</param>
	/// <param name="percentIncreaseAdd">Additive percentage increase (e.g., 0.2 for +20%).</param>
	/// <param name="modifierMult">Multiplicative modifier (e.g., 2 for double).</param>
	/// <param name="finalMult">Multiplier applied after everything (e.g., 2 for double) </param>
	public void ChangeModifier(
		Modifier modifier,
		float? flatAdd = null,
		float? percentIncreaseAdd = null,
		float? modifierMult = null,
		float? finalMult = null
	)
	{
		if (!_modifiersList.Contains(modifier))
		{
			return;
		}

		if (flatAdd.HasValue)
		{
			modifier.FlatAdd = flatAdd.Value;
		}
		if (percentIncreaseAdd.HasValue)
		{
			modifier.PercentIncreaseAdd = percentIncreaseAdd.Value;
		}
		if (modifierMult.HasValue)
		{
			modifier.ModifierMult = modifierMult.Value;
		}
		if (finalMult.HasValue)
		{
			modifier.FinalMult = finalMult.Value;
		}

		UpdateStat(modifier.StatName);
	}

	private void UpdateAllStats()
	{
		foreach (StatType statType in _baseStatsMap.Keys)
		{
			UpdateStat(statType);
		}
	}

	private void UpdateStat(StatType statType)
	{
		float flatAdd = 0;
		float percentIncrease = 0;
		float baseModifierMult = 1;
		float finalMult = 1;
		foreach (Modifier modifier in _modifiersList)
		{
			if (modifier.StatName == statType)
			{
				flatAdd += modifier.FlatAdd;
				percentIncrease += modifier.PercentIncreaseAdd * 0.01f;
				baseModifierMult *= modifier.ModifierMult;
				finalMult *= modifier.FinalMult;
			}
		}

		float baseValue = _baseStatsMap[statType];
		float oldValue = _finalStats.GetValueOrDefault(statType, baseValue);
		float newValue = ((baseValue * baseModifierMult) + flatAdd) * (1 + percentIncrease) * finalMult;
		_finalStats[statType] = newValue;

		if (!Mathf.Approximately(oldValue, newValue))
		{
			OnStatChanged?.Invoke(new StatChangedEventArgs(statType, newValue - oldValue, baseValue, newValue));
		}
		// For Testing
		// Debug.Log($"Update: {statType} = {GetFinalStat(statType)}");
	}
}

namespace Stats
{
	[Serializable]
	public enum StatType
	{
		Speed = 0,
		Health = 1,
		ReelSpeed = 2,
		RegenRate = 3,
		RegenDelay = 4,
		IFrameDuration = 5,
		CooldownReduction = 6,
		Firerate = 7,
		Duration = 8,
		Range = 9,
		ReelDamage = 10,
		SellValueMult = 11,
		MaxTrinketWeight = 12,
		EdgeCostReduction = 13,
	};

	/// <summary>
	/// Event arguments for stat change events.
	/// </summary>
	public readonly struct StatChangedEventArgs
	{
		public StatType StatType { get; }
		public float Delta { get; }
		public float BaseValue { get; }
		public float FinalValue { get; }

		public StatChangedEventArgs(StatType statType, float delta, float baseValue, float finalValue)
		{
			StatType = statType;
			Delta = delta;
			BaseValue = baseValue;
			FinalValue = finalValue;
		}
	}

	/// <summary>
	/// Represents a temporary stat modifier that can be added and removed.
	/// Formula: (base × ModifierMult + FlatAdd) × (1 + PercentIncreaseAdd) × FinalMult
	/// </summary>
	[Serializable]
	public class Modifier
	{
		public StatType StatName;
		public float FlatAdd;
		public float PercentIncreaseAdd;
		public float ModifierMult = 1f;
		public float FinalMult = 1f;
	}
}
