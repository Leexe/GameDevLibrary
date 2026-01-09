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
	public readonly Dictionary<string, Sprite> CharacterSpriteMap;
	public readonly Dictionary<string, Sprite> BackgroundSpriteMap;
	public readonly Dictionary<string, VoiceSO> VoicesMap;
}
