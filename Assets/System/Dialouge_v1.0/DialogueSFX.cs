using Febucci.TextAnimatorForUnity;
using FMODUnity;
using Sirenix.OdinInspector;
using UnityEngine;

public class DialogueSFX : MonoBehaviour
{
	[FoldoutGroup("References")]
	[SerializeField]
	private TypewriterComponent _hiddenTypewriter;

	[FoldoutGroup("References")]
	[SerializeField]
	private VisualNovelDictionarySO _vnDictionary;

	// Private Variables
	private bool _charactersTalking;
	private VoiceSO _currentCharacterVoice;
	private DialogueEvents DialogueEvents => DialogueEvents.Instance;

	private void OnEnable()
	{
		_hiddenTypewriter.onCharacterVisible.AddListener(PlayVoice);
		DialogueEvents.OnStartDialogue += ResetVoiceState;
		DialogueEvents.OnDialogueVoice += SetSpeakingCharacter;
		DialogueEvents.OnPlaySFX += PlaySFX;
		DialogueEvents.OnPlayAmbience += PlayAmbience;
		DialogueEvents.OnPlayMusic += PlayMusic;
	}

	private void OnDisable()
	{
		_hiddenTypewriter.onCharacterVisible.RemoveListener(PlayVoice);
		DialogueEvents.OnStartDialogue -= ResetVoiceState;
		DialogueEvents.OnDialogueVoice -= SetSpeakingCharacter;
		DialogueEvents.OnPlaySFX -= PlaySFX;
		DialogueEvents.OnPlayAmbience -= PlayAmbience;
		DialogueEvents.OnPlayMusic -= PlayMusic;
	}

	/// <summary>
	/// Resets the voice state at the start of each new line.
	/// Called before tags are processed.
	/// </summary>
	private void ResetVoiceState()
	{
		if (_currentCharacterVoice)
		{
			_currentCharacterVoice.ResetCount();
		}

		_charactersTalking = false;
		_currentCharacterVoice = null;
	}

	/// <summary>
	/// Called when a #d_characterName tag is encountered.
	/// Enables voice playback for this line.
	/// </summary>
	private void SetSpeakingCharacter(string characterName)
	{
		_charactersTalking = true;
		if (_vnDictionary.VoicesMap.TryGetValue(characterName, out VoiceSO voice))
		{
			_currentCharacterVoice = voice;
		}
	}

	/// <summary>
	/// Plays a voice sample for the current character.
	/// </summary>
	private void PlayVoice(Febucci.TextAnimatorCore.Text.CharacterData characterData)
	{
		if (_charactersTalking && _currentCharacterVoice != null)
		{
			_currentCharacterVoice.PlayVoice(characterData.info.character);
		}
	}

	/// <summary>
	/// Plays a sound effect by key lookup.
	/// </summary>
	private void PlaySFX(string key)
	{
		PlayAudioFromDictionary(key, _vnDictionary.SFXMap, sfx => AudioManager.Instance.PlayOneShot(sfx), "SFX");
	}

	/// <summary>
	/// Plays/switches ambience by key lookup.
	/// </summary>
	private void PlayAmbience(string key)
	{
		PlayAudioFromDictionary(
			key,
			_vnDictionary.AmbienceMap,
			ambience => AudioManager.Instance.SwitchAmbienceTrack(ambience),
			"Ambience"
		);
	}

	/// <summary>
	/// Plays/switches music by key lookup.
	/// </summary>
	private void PlayMusic(string key)
	{
		PlayAudioFromDictionary(
			key,
			_vnDictionary.MusicMap,
			music => AudioManager.Instance.SwitchMusicTrack(music),
			"Music"
		);
	}

	/// <summary>
	/// Generic helper to look up audio in a dictionary and execute a play action.
	/// </summary>
	private void PlayAudioFromDictionary(
		string key,
		System.Collections.Generic.Dictionary<string, EventReference> audioMap,
		System.Action<EventReference> playAction,
		string debugContext
	)
	{
		if (audioMap.TryGetValue(key, out EventReference audioRef))
		{
			playAction(audioRef);
		}
		else
		{
			Debug.LogWarning($"VisualNovelSFX: {debugContext} key '{key}' not found in dictionary.");
		}
	}
}
