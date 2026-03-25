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
	public Dictionary<string, EventReference> MusicMap = new();
	public Dictionary<string, EventReference> SFXMap = new();
	public Dictionary<string, EventReference> AmbienceMap = new();
	public Dictionary<string, Sprite> CharacterSpriteMap = new();
	public Dictionary<string, Sprite> BackgroundSpriteMap = new();
	public Dictionary<string, VoiceSO> VoicesMap = new();
}
