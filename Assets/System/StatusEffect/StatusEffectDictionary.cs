using System.Collections.Generic;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using StatusEffects;
using UnityEngine;

[CreateAssetMenu(fileName = "StatusEffectDictionarySO", menuName = "Dictionaries/StatusEffectDictionarySO")]
public class StatusEffectDictionarySO : SerializedScriptableObject
{
	[OdinSerialize, ReadOnly]
	public Dictionary<string, StatusEffectSO> SODict { get; private set; }

	/// <summary>
	/// Look up a StatusEffectSO by string ID.
	/// Primarily used to match StatusEffectInstances with their StatusEffectSOs when loading from a save file.
	/// </summary>
	public StatusEffectSO GetStatusEffectSOById(string id)
	{
		if (SODict != null && SODict.TryGetValue(id, out StatusEffectSO statusEffect))
		{
			return statusEffect;
		}

		Debug.LogError($"StatusEffect with ID '{id}' not found in dictionary.");
		return null;
	}
}
