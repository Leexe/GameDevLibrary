using System;
using System.Collections.Generic;
using Febucci.TextAnimatorCore.Text;
using Febucci.TextAnimatorForUnity;
using FMODUnity;
using PrimeTween;
using Sirenix.OdinInspector;
using UnityEngine;

public class DialogueSFX : MonoBehaviour
{
	[FoldoutGroup("References")]
	[SerializeField]
	private DialogueController _dialogueController;

	[FoldoutGroup("References")]
	[SerializeField]
	private TypewriterComponent _hiddenTypewriter;

	[FoldoutGroup("References")]
	[SerializeField]
	private VisualNovelDictionarySO _vnDictionary;

	[FoldoutGroup("Voice Fade Out")]
	[SerializeField]
	private float _fadeDuration = 1f;

	// Private Variables
	private bool _charactersTalking;
	private VoiceSO _currentCharacterVoice;
	private DialogueState _dialogueState;
	private Tween _fadeOutTween;
	private float _volume = 1f;

	private void OnEnable()
	{
		_dialogueState = _dialogueController.DialogueState;
		_hiddenTypewriter.onCharacterVisible.AddListener(PlayVoice);
		_dialogueState.OnStartDialogue += ResetVoiceState;
		_dialogueState.OnEndStory += FadeOutVoice;
		_dialogueState.OnDialogueVoice += SetSpeakingCharacter;
		_dialogueState.OnPlaySFX += PlaySFX;
		_dialogueState.OnPlayAmbience += PlayAmbience;
		_dialogueState.OnPlayMusic += PlayMusic;
	}

	private void OnDisable()
	{
		_hiddenTypewriter.onCharacterVisible.RemoveListener(PlayVoice);
		_dialogueState.OnStartDialogue -= ResetVoiceState;
		_dialogueState.OnEndStory -= FadeOutVoice;
		_dialogueState.OnDialogueVoice -= SetSpeakingCharacter;
		_dialogueState.OnPlaySFX -= PlaySFX;
		_dialogueState.OnPlayAmbience -= PlayAmbience;
		_dialogueState.OnPlayMusic -= PlayMusic;

		_fadeOutTween.Stop();
	}

	/// <summary>
	///     Resets the voice state at the start of each new line.
	///     Called before tags are processed.
	/// </summary>
	private void ResetVoiceState()
	{
		if (_currentCharacterVoice)
		{
			_currentCharacterVoice.ResetCount();
		}

		_charactersTalking = false;
		_currentCharacterVoice = null;
		_fadeOutTween.Stop();
		_volume = 1f;
	}

	/// <summary>
	///     Fades out the voice volume.
	/// </summary>
	private void FadeOutVoice()
	{
		_fadeOutTween = Tween.Custom(this, 1, 0, _fadeDuration, (target, value) => target._volume = value);
	}

	/// <summary>
	///     Called when a #d_characterName tag is encountered.
	///     Enables voice playback for this line.
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
	///     Plays a voice sample for the current character.
	/// </summary>
	private void PlayVoice(CharacterData characterData)
	{
		if (_charactersTalking && _currentCharacterVoice != null)
		{
			_currentCharacterVoice.PlayVoice(characterData.info.character, _volume);
		}
	}

	/// <summary>
	///     Plays a sound effect by key lookup.
	/// </summary>
	private void PlaySFX(string key)
	{
		PlayAudioFromDictionary(key, _vnDictionary.SFXMap, sfx => AudioManager.Instance.PlayOneShot(sfx), "SFX");
	}

	/// <summary>
	///     Plays/switches ambience by key lookup.
	/// </summary>
	private void PlayAmbience(string key)
	{
		if (key == "none")
		{
			AudioManager.Instance.StopAmbience();
			return;
		}

		PlayAudioFromDictionary(
			key,
			_vnDictionary.AmbienceMap,
			ambience => AudioManager.Instance.SwitchAmbience(ambience),
			"Ambience"
		);
	}

	/// <summary>
	///     Plays/switches music by key lookup.
	/// </summary>
	private void PlayMusic(string key)
	{
		if (key == "none")
		{
			AudioManager.Instance.StopMusic();
			return;
		}

		PlayAudioFromDictionary(
			key,
			_vnDictionary.MusicMap,
			music => AudioManager.Instance.SwitchMusic(music),
			"Music"
		);
	}

	/// <summary>
	///     Generic helper to look up audio in a dictionary and execute a play action.
	/// </summary>
	private void PlayAudioFromDictionary(
		string key,
		Dictionary<string, EventReference> audioMap,
		Action<EventReference> playAction,
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
