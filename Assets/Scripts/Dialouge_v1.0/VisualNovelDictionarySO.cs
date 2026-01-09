using System.Collections.Generic;
using FMODUnity;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(
	fileName = "VisualNovelDictionary",
	menuName = "ScriptableObjects/VisualNovel/VisualNovelDictionary",
	order = 1
)]
public class VisualNovelDictionarySO : SerializedScriptableObject
{
	public readonly Dictionary<string, EventReference> MusicMap;
	public readonly Dictionary<string, EventReference> SFXMap;
	public readonly Dictionary<string, EventReference> AmbienceMap;
	public Dictionary<string, Sprite> CharacterSpriteMap;
	public Dictionary<string, Sprite> BackgroundSpriteMap;
	public Dictionary<string, VoiceSO> VoicesMap;

	public VoiceSO GetVoice(string characterName)
	{
		if (VoicesMap.TryGetValue(characterName.ToLower(), out VoiceSO voice))
		{
			return voice;
		}
		Debug.LogError($"Voice not found for character: {characterName}");
		return null;
	}

	public bool TryGetVoice(string characterName, out VoiceSO voice)
	{
		if (VoicesMap.TryGetValue(characterName.ToLower(), out voice))
		{
			return true;
		}
		Debug.LogWarning($"Voice not found for character: {characterName}");
		return false;
	}
}
