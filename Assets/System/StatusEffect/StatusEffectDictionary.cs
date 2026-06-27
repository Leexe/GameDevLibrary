using System.Collections.Generic;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using StatusEffects;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "StatusEffectDictionarySO", menuName = "Dictionaries/StatusEffectDictionarySO")]
public class StatusEffectDictionarySO : SerializedScriptableObject
{
	[FolderPath]
	[SerializeField]
	private string _pathToSODict;

	[OdinSerialize]
	[ReadOnly]
	public Dictionary<string, StatusEffectSO> SODict { get; private set; }

	/// <summary>
	///     Look up a StatusEffectSO by string ID.
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

#if UNITY_EDITOR
	[Button]
	[PropertyOrder(-1)]
	[Tooltip("Autogenerate SODict from SOs in the ScriptableObjects/Items folder")]
	[UsedImplicitly]
	private void GenerateSODict()
	{
		SODict = SODictionaryUtility.GenerateSODict<StatusEffectSO>(_pathToSODict);
		EditorUtility.SetDirty(this);
	}
#endif
}
