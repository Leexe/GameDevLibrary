using System;
using System.Collections.Generic;
using System.Linq;
using Ink.Runtime;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Acts as the bridge between Ink narrative scripts and Unity gameplay systems.
/// </summary>
public class DialogueController : MonoBehaviour
{
	[FoldoutGroup("References")]
	[SerializeField]
	private TextAsset _inkJson;

	[FoldoutGroup("Transition Times")]
	[SerializeField]
	private float _characterFadeDuration = 0.5f;

	private DialogueEvents DialogueEvents => DialogueEvents.Instance;
	private Story _story;
	private bool _storyPlaying;
	private bool _typewriterPlaying;
	private int _choiceIndex = -1;
	private bool _needToDisplayChoices;
	private string _currentSpeakerName;

	private bool ChoicesAvailable => _story.currentChoices.Count > 0;

	private void Awake()
	{
		_story = new Story(_inkJson.text);
		InitializeTagHandlers();
	}

	private void OnEnable()
	{
		DialogueEvents.OnStartStory += StartStory;
		DialogueEvents.OnChoiceSelect += ContinueStory;
		DialogueEvents.OnTypewriterFinish += SetTypewriterInactive;
		DialogueEvents.OnTypewriterFinish += DisplayChoices;
		InputManager.Instance.OnContinueStoryPerformed.AddListener(ContinueStory);
	}

	private void OnDisable()
	{
		DialogueEvents.OnStartStory -= StartStory;
		DialogueEvents.OnChoiceSelect -= ContinueStory;
		DialogueEvents.OnTypewriterFinish -= SetTypewriterInactive;
		DialogueEvents.OnTypewriterFinish -= DisplayChoices;

		if (InputManager.Instance)
		{
			InputManager.Instance.OnContinueStoryPerformed.RemoveListener(ContinueStory);
		}
	}

	private void Start()
	{
		DialogueEvents.RemoveAllCharacters();
	}

	[FoldoutGroup("Debug")]
	[Button]
	[UsedImplicitly]
	private void StartTestingStory()
	{
		DialogueEvents.StartStory("Testing");
	}

	[FoldoutGroup("Debug")]
	[Button]
	[UsedImplicitly]
	private void StartAnimationTest()
	{
		DialogueEvents.StartStory("Testing_Animations");
	}

	[FoldoutGroup("Debug")]
	[Button]
	[UsedImplicitly]
	private void StartVoiceTest()
	{
		DialogueEvents.StartStory("Testing_Voices");
	}

	[FoldoutGroup("Debug")]
	[Button]
	[UsedImplicitly]
	private void StartTextAnimatorTest()
	{
		DialogueEvents.StartStory("Testing_TextAnimator");
	}

	#region Story Managers

	/// <summary>
	/// Starts the story from a specified Ink knot.
	/// </summary>
	private void StartStory(string knotName)
	{
		if (_storyPlaying)
		{
			// Restart the story
			_storyPlaying = false;
			_typewriterPlaying = false;
			_choiceIndex = -1;
			_currentSpeakerName = "";
			DialogueEvents.RemoveAllCharacters();
		}

		_storyPlaying = true;

		if (knotName == "")
		{
			Debug.LogError("[DialogueController] KnotName is Empty");
		}
		else
		{
			_story.ChoosePathString(knotName);
		}

		ContinueStory();
	}

	/// <summary>
	/// Advances the story to the next line or handles player interaction.
	/// </summary>
	private void ContinueStory()
	{
		// Check global blocking conditions (Pause, Backlog, etc.)
		if (!DialogueEvents.CanAdvanceStory)
		{
			return;
		}

		// Don't continue if story isn't currently playing
		if (!_storyPlaying)
		{
			return;
		}

		// If typewriter is playing, skip it instead of continuing
		if (_typewriterPlaying)
		{
			DialogueEvents.SkipTypewriter();
			return;
		}

		// If a choice was selected, proceed down that path
		if (ChoicesAvailable && _choiceIndex != -1)
		{
			_story.ChooseChoiceIndex(_choiceIndex);
			_choiceIndex = -1;
		}

		if (_story.canContinue)
		{
			_story.Continue();
			_typewriterPlaying = true;

			// Signal start of new dialogue line
			DialogueEvents.StartDialogue();

			// Parse any tags on this line
			if (_story.currentTags.Count > 0)
			{
				ParseTags(_story.currentTags);
			}

			DisplayDialogue();
		}
		else if (!ChoicesAvailable)
		{
			ExitStory();
		}
	}

	/// <summary>
	/// Continues the story after a choice is selected.
	/// </summary>
	/// <param name="choiceIndex">Index of the selected choice.</param>
	private void ContinueStory(int choiceIndex)
	{
		_choiceIndex = choiceIndex;
		ContinueStory();
	}

	/// <summary>
	/// Ends the current story and cleans up state.
	/// </summary>
	private void ExitStory()
	{
		_storyPlaying = false;
		_typewriterPlaying = false;

		DialogueEvents.EndStory();
	}

	#endregion

	#region Dialogue Display

	/// <summary>
	/// Displays the current dialogue line, skipping blank lines.
	/// </summary>
	private void DisplayDialogue()
	{
		string storyLine = _story.currentText;

		// Skip blank lines
		while (IsLineBlank(storyLine) && _story.canContinue)
		{
			storyLine = _story.Continue();

			while (IsLineBlank(storyLine) && _story.canContinue)
			{
				storyLine = _story.Continue();
			}

			if (IsLineBlank(storyLine) && !_story.canContinue)
			{
				ExitStory();
			}
		}

		// Flag choices for display after typewriter finishes
		if (ChoicesAvailable)
		{
			_needToDisplayChoices = true;
		}

		DialogueEvents.DisplayDialogue(_currentSpeakerName, storyLine);
	}

	/// <summary>
	/// Displays choices if they are pending after typewriter completes.
	/// </summary>
	private void DisplayChoices()
	{
		if (_needToDisplayChoices)
		{
			DialogueEvents.DisplayChoices(_story.currentChoices);
		}
	}

	/// <summary>
	/// Marks the typewriter as inactive when animation completes.
	/// </summary>
	private void SetTypewriterInactive()
	{
		_typewriterPlaying = false;
	}

	/// <summary>
	/// Checks if a line of text is blank or only whitespace.
	/// </summary>
	/// <param name="line">The line to check.</param>
	/// <returns>True if the line is blank, false otherwise.</returns>
	private bool IsLineBlank(string line)
	{
		return line.Trim().Equals("") || line.Trim().Equals("\n");
	}

	#endregion

	#region Tag Parsing

	private Dictionary<string, Action<string[]>> _tagHandlers;

	/// <summary>
	/// Initializes the tag handler dictionary with all supported tag types.
	/// Called in Awake after story initialization.
	/// </summary>
	private void InitializeTagHandlers()
	{
		_tagHandlers = new Dictionary<string, Action<string[]>>
		{
			{ "ch", HandleCharacterTag },
			{ "chl", args => HandlePositionedCharacterTag(args, CharacterPosition.Left) },
			{ "chc", args => HandlePositionedCharacterTag(args, CharacterPosition.Center) },
			{ "chr", args => HandlePositionedCharacterTag(args, CharacterPosition.Right) },
			{ "an", HandleAnimationTag },
			{ "nm", HandleNameTag },
			{ "d", HandleDialogueVoiceTag },
			{ "bg", HandleBackgroundTag },
			{ "sx", HandleSFXTag },
			{ "ms", HandleMusicTag },
			{ "ab", HandleAmbienceTag },
		};
	}

	/// <summary>
	/// Parses a list of Ink tags and dispatches them to appropriate handlers.
	/// Tags use underscore-delimited format: "identifier_arg1_arg2"
	/// </summary>
	/// <param name="tags">List of tag strings from the current Ink line.</param>
	private void ParseTags(List<string> tags)
	{
		foreach (string inkTag in tags)
		{
			string[] parts = inkTag.Split('_');
			if (parts.Length < 1)
			{
				Debug.LogWarning($"[DialogueController] Empty tag encountered: {inkTag}");
				continue;
			}

			string identifier = parts[0].ToLower();
			string[] args = parts.Skip(1).ToArray();

			ProcessTag(identifier, args, inkTag);
		}
	}

	/// <summary>
	/// Dispatches a parsed tag to its registered handler.
	/// </summary>
	/// <param name="identifier">The tag type identifier (e.g., "ch", "nm").</param>
	/// <param name="args">Arguments following the identifier.</param>
	/// <param name="rawTag">Original tag string for error reporting.</param>
	private void ProcessTag(string identifier, string[] args, string rawTag)
	{
		if (_tagHandlers.TryGetValue(identifier, out Action<string[]> handler))
		{
			handler(args);
		}
		else
		{
			Debug.LogWarning($"[DialogueController] Unknown tag identifier: '{identifier}' in tag: '{rawTag}'");
		}
	}

	#endregion

	#region Tag Handlers

	/// <summary>
	/// Handles character sprite tags. Defaults to Center position.
	/// Format: #ch_Sera_happy_open → characterKey = "sera_happy_open"
	/// Format: #ch_Sera_clear → Removes character "sera"
	/// </summary>
	private void HandleCharacterTag(string[] args)
	{
		HandlePositionedCharacterTag(args, CharacterPosition.Center);
	}

	/// <summary>
	/// Handles positioned character tags.
	/// Format: #chl_Sera_happy → Position: Left, ID: Sera, Key: Sera_happy
	/// Format: #chl_Sera → Position: Left, ID: Sera, Key: null (Move only)
	/// Format: #chl_Samurai1:Samurai_happy → Position: Left, ID: Samurai1, Key: Samurai_happy (alias syntax)
	/// Format: #chl_Sera_clear → Remove Sera Sprite
	/// </summary>
	private void HandlePositionedCharacterTag(string[] args, CharacterPosition position)
	{
		if (args.Length == 0)
		{
			return;
		}

		string firstArg = args[0];
		string characterId;
		string spriteBase;

		if (firstArg.ToLower() == "clear")
		{
			DialogueEvents.RemoveAllCharacters(_characterFadeDuration);
			return;
		}

		// Check for alias syntax: "CharacterId:SpriteBase" (e.g., "Sera:Girl_Happy")
		// This allows multiple characters to share the same sprite asset
		if (firstArg.Contains(':'))
		{
			string[] aliasParts = firstArg.Split(':');
			characterId = aliasParts[0]; // Tracking ID (e.g., "Sera")
			spriteBase = aliasParts[1]; // Sprite lookup base (e.g., "Girl_Happy")
		}
		else
		{
			characterId = firstArg;
			spriteBase = firstArg;
		}

		// Strip animation suffix from characterId if present (e.g., "clown+pop" -> "clown")
		if (characterId.Contains('+'))
		{
			characterId = characterId.Split('+')[0];
		}

		// Check for clear command
		if (args.Length > 1 && args[1].ToLower() == "clear")
		{
			DialogueEvents.RemoveCharacter(characterId, _characterFadeDuration);
			return;
		}

		// Reconstruct sprite key
		string spriteKey = "";
		if (args.Length >= 1)
		{
			// Replace the first arg with spriteBase for sprite lookup
			string[] spriteArgs = new string[args.Length];
			spriteArgs[0] = spriteBase;
			for (int i = 1; i < args.Length; i++)
			{
				spriteArgs[i] = args[i];
			}

			spriteKey = string.Join("_", spriteArgs);
		}

		// Handle inline animation syntax (e.g. Sera_Happy+Shake)
		if (spriteKey.Contains('+'))
		{
			string[] splitParts = spriteKey.Split('+');
			string realSpriteKey = splitParts[0];
			string animationPart = splitParts[1];

			// Update the character with the real sprite key
			DialogueEvents.UpdateCharacter(characterId, position, realSpriteKey, _characterFadeDuration);

			// Parse animation part (e.g. "shake_10_0.5")
			string[] animParts = animationPart.Split('_');
			string animName = animParts[0];
			string[] animArgs = animParts.Skip(1).ToArray();

			DialogueEvents.UpdateAnimation(characterId, animName, animArgs);
		}
		else
		{
			DialogueEvents.UpdateCharacter(characterId, position, spriteKey, _characterFadeDuration);
		}
	}

	private void HandleAnimationTag(string[] args)
	{
		if (args.Length < 2)
		{
			Debug.LogWarning(
				$"[DialogueController] Animation tag requires at least CharacterID and AnimationName (e.g. an_Sera_Shake). Args passed: {string.Join(", ", args)}"
			);
			return;
		}

		string characterId = args[0];
		string animationName = args[1];
		string[] parameters = args.Skip(2).ToArray();

		DialogueEvents.UpdateAnimation(characterId, animationName, parameters);
	}

	/// <summary>
	/// Handles speaker name tags.
	/// Format: #nm_Sera → Shows "Sera", #nm_none → Hides name
	/// </summary>
	private void HandleNameTag(string[] args)
	{
		if (args.Length == 0 || args[0].ToLower() == "none")
		{
			_currentSpeakerName = "";
			DialogueEvents.UpdateName("");
			return;
		}

		string characterName = args[0];
		_currentSpeakerName = characterName;
		DialogueEvents.UpdateName(characterName);
	}

	/// <summary>
	/// Handles dialogue voice tags to indicate spoken dialogue lines.
	/// Format: #d_sera → Marks line as spoken dialogue by "sera"
	/// </summary>
	private void HandleDialogueVoiceTag(string[] args)
	{
		if (args.Length == 0)
		{
			Debug.LogWarning("[DialogueController] Dialogue voice tag requires character name (e.g. #d_sera)");
			return;
		}

		string characterName = args[0].ToLower();
		DialogueEvents.SetDialogueVoice(characterName);
	}

	/// <summary>
	/// Handles background change tags.
	/// Format: #bg_park → backgroundKey = "park"
	/// </summary>
	private void HandleBackgroundTag(string[] args)
	{
		string backgroundKey = string.Join("_", args);
		DialogueEvents.UpdateBackground(backgroundKey);
	}

	/// <summary>
	/// Handles sound effect tags.
	/// Format: #sx_door → sfxKey = "door"
	/// </summary>
	private void HandleSFXTag(string[] args)
	{
		string sfxKey = string.Join("_", args);
		DialogueEvents.PlaySFX(sfxKey);
	}

	/// <summary>
	/// Handles music change tags.
	/// Format: #ms_morning → musicKey = "morning"
	/// </summary>
	private void HandleMusicTag(string[] args)
	{
		string musicKey = string.Join("_", args);
		DialogueEvents.PlayMusic(musicKey);
	}

	/// <summary>
	/// Handles ambience change tags.
	/// Format: #ab_park → ambienceKey = "park"
	/// </summary>
	private void HandleAmbienceTag(string[] args)
	{
		string ambienceKey = string.Join("_", args);
		DialogueEvents.PlayAmbience(ambienceKey);
	}

	#endregion
}
