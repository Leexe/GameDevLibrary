using System;
using System.Collections.Generic;
using Febucci.TextAnimatorForUnity;
using PrimeTween;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles the visual novel UI presentation layer.
/// Subscribes to DialogueEvents for decoupled communication with DialogueController.
/// </summary>
public class VisualNovelUI : MonoBehaviour
{
	[TitleGroup("References")]
	[FoldoutGroup("References/Game References")]
	[SerializeField]
	private TypewriterComponent _typewriter;

	[FoldoutGroup("References/Game References")]
	[SerializeField]
	private VisualNovelDictionarySO _visualNovelDictionary;

	[FoldoutGroup("References/Character Sprite References")]
	[SerializeField]
	private GameObject _characterPrefab;

	[FoldoutGroup("References/Character Sprite References")]
	[SerializeField]
	private Transform _leftSpriteTransform;

	[FoldoutGroup("References/Character Sprite References")]
	[SerializeField]
	private Transform _centerSpriteTransform;

	[FoldoutGroup("References/Character Sprite References")]
	[SerializeField]
	private Transform _rightSpriteTransform;

	[FoldoutGroup("References/Character Sprite References")]
	[SerializeField]
	private Transform _nonLayoutParent; // Parent for character sprites that are about to get deleted

	[FoldoutGroup("References/UI References")]
	[SerializeField]
	private CanvasGroup _canvasGroup;

	[FoldoutGroup("References/UI References")]
	[SerializeField]
	private GameObject _namePanel;

	[FoldoutGroup("References/UI References")]
	[SerializeField]
	private TextMeshProUGUI _nameText;

	[FoldoutGroup("References/UI References")]
	[SerializeField]
	private Image _backgroundImage;

	[TitleGroup("Default Values")]
	[FoldoutGroup("Default Values/Animation")]
	[SerializeField]
	private float _defaultFadeOutDuration = 1f;

	[FoldoutGroup("Default Values/Animation")]
	[SerializeField]
	private float _defaultShakeOffset = 20f;

	[FoldoutGroup("Default Values/Animation")]
	[SerializeField]
	private float _defaultAnimationDuration = 0.7f;

	[FoldoutGroup("Default Values/Animation")]
	[SerializeField]
	private float _slideOffset = 100f;

	[FoldoutGroup("Default Values/Animation")]
	[SerializeField]
	private Color _flashColor = Color.white;

	// Private Variables
	private Tween _canvasAlphaTween;
	private readonly Dictionary<string, VNCharacter> _activeCharacters = new();
	private Dictionary<string, Action<VNCharacter, string[]>> _animationHandlers;
	private DialogueEvents DialogueEvents => DialogueEvents.Instance;

	private void Awake()
	{
		InitializeAnimationHandlers();
	}

	private void InitializeAnimationHandlers()
	{
		_animationHandlers = new Dictionary<string, Action<VNCharacter, string[]>>(StringComparer.OrdinalIgnoreCase)
		{
			{
				"shake",
				(c, args) =>
					c.ShakeHorizontal(
						ParseFloat(args, 0, _defaultShakeOffset),
						ParseFloat(args, 1, _defaultAnimationDuration)
					)
			},
			{
				"shakehorizontal",
				(c, args) =>
					c.ShakeHorizontal(
						ParseFloat(args, 0, _defaultShakeOffset),
						ParseFloat(args, 1, _defaultAnimationDuration)
					)
			},
			{
				"shakevertical",
				(c, args) =>
					c.ShakeVertical(
						ParseFloat(args, 0, _defaultShakeOffset),
						ParseFloat(args, 1, _defaultAnimationDuration)
					)
			},
			{
				"hop",
				(c, args) => c.Hop(ParseFloat(args, 0, 100f), ParseFloat(args, 1, _defaultAnimationDuration / 3f))
			},
			{
				"reversehop",
				(c, args) => c.ReverseHop(ParseFloat(args, 0, 33f), ParseFloat(args, 1, _defaultAnimationDuration / 2f))
			},
			{
				"bounce",
				(c, args) => c.Bounce(ParseFloat(args, 0, 0.1f), ParseFloat(args, 1, _defaultAnimationDuration / 2f))
			},
			{ "flash", (c, args) => c.Flash(_flashColor, ParseFloat(args, 0, _defaultAnimationDuration / 3f)) },
			{
				"dodge",
				(c, args) => c.Dodge(ParseFloat(args, 0, 100f), ParseFloat(args, 1, _defaultAnimationDuration / 2f))
			},
			{
				"pop",
				(c, args) => c.Pop(ParseFloat(args, 0, 1.05f), ParseFloat(args, 1, _defaultAnimationDuration / 3f))
			},
		};
	}

	private float ParseFloat(string[] args, int index, float defaultValue)
	{
		if (args != null && index < args.Length && float.TryParse(args[index], out float result))
		{
			return result;
		}

		return defaultValue;
	}

	private void OnEnable()
	{
		DialogueEvents.OnStartStory += EnableStoryPanel;
		DialogueEvents.OnDisplayDialogue += ChangeStoryText;
		DialogueEvents.OnCharacterUpdate += UpdateCharacter;
		DialogueEvents.OnCharacterAnimation += PlayAnimation;
		DialogueEvents.OnCharacterRemove += RemoveCharacter;
		DialogueEvents.OnNameUpdate += ChangeNameText;
		DialogueEvents.OnBackgroundUpdate += ChangeBackground;
		DialogueEvents.OnEndStory += DisableStoryPanel;
		DialogueEvents.OnEndStory += RemoveAllCharacters;
		DialogueEvents.OnAllCharacterRemove += RemoveAllCharacters;
		DialogueEvents.OnTypewriterSkip += SkipTypewriter;

		// Uncomment: Need to have this listener for the typewriter to follow the timescale correctly
		// GameManager.Instance.OnGamePaused.AddListener(PauseTypewriter);
		// GameManager.Instance.OnGameResume.AddListener(ResumeTypewriter);

		_typewriter.onTextShowed.AddListener(DialogueEvents.TypewriterFinished);
	}

	private void OnDisable()
	{
		DialogueEvents.OnStartStory -= EnableStoryPanel;
		DialogueEvents.OnDisplayDialogue -= ChangeStoryText;
		DialogueEvents.OnCharacterUpdate -= UpdateCharacter;
		DialogueEvents.OnCharacterAnimation -= PlayAnimation;
		DialogueEvents.OnCharacterRemove -= RemoveCharacter;
		DialogueEvents.OnNameUpdate -= ChangeNameText;
		DialogueEvents.OnBackgroundUpdate -= ChangeBackground;
		DialogueEvents.OnEndStory -= DisableStoryPanel;
		DialogueEvents.OnEndStory -= RemoveAllCharacters;
		DialogueEvents.OnAllCharacterRemove -= RemoveAllCharacters;
		DialogueEvents.OnTypewriterSkip -= SkipTypewriter;

		// Uncomment: Need to have this listener for the typewriter to follow the timescale correctly
		// if (GameManager.Instance)
		// {
		// 	GameManager.Instance.OnGamePaused.RemoveListener(PauseTypewriter);
		// 	GameManager.Instance.OnGameResume.RemoveListener(ResumeTypewriter);
		// }
	}

	#region UI Updates

	/// <summary>
	/// Updates the speaker name display.
	/// Shows the name panel if a name is provided, hides it otherwise.
	/// </summary>
	/// <param name="speakerName">Speaker name to display, or empty/null to hide.</param>
	private void ChangeNameText(string speakerName)
	{
		if (string.IsNullOrEmpty(speakerName))
		{
			_namePanel.SetActive(false);
		}
		else
		{
			_namePanel.SetActive(true);
			_nameText.text = speakerName;
		}
	}

	/// <summary>
	/// Displays a line of dialogue using the typewriter effect.
	/// </summary>
	/// <param name="characterName">The name of the character speaking.</param>
	/// <param name="line">The dialogue line to display.</param>
	private void ChangeStoryText(string characterName, string line)
	{
		ChangeNameText(characterName);
		_typewriter.ShowText(line);
	}

	/// <summary>
	/// Fades in the story panel when dialogue begins.
	/// </summary>
	/// <param name="knotName">Name of the Ink knot being started (unused for UI).</param>
	private void EnableStoryPanel(string knotName)
	{
		float fadeDuration = 1f;
		_canvasAlphaTween.Stop();
		_canvasAlphaTween = Tween.Custom(_canvasGroup.alpha, 1, fadeDuration, newVal => _canvasGroup.alpha = newVal);
		_canvasGroup.interactable = true;
		_canvasGroup.blocksRaycasts = true;
	}

	/// <summary>
	/// Fades out the story panel when dialogue ends.
	/// </summary>
	private void DisableStoryPanel()
	{
		float fadeDuration = 1f;
		_canvasAlphaTween.Stop();
		_canvasAlphaTween = Tween.Custom(_canvasGroup.alpha, 0, fadeDuration, newVal => _canvasGroup.alpha = newVal);
		_canvasGroup.interactable = false;
		_canvasGroup.blocksRaycasts = false;
	}

	#endregion

	#region Typewriter

	/// <summary>
	/// Skips the current typewriter animation to show full text immediately.
	/// </summary>
	private void SkipTypewriter()
	{
		_typewriter.SkipTypewriter();
	}

	/// <summary>
	/// Pauses the typewriter animation. Called when game is paused.
	/// </summary>
	private void PauseTypewriter()
	{
		_typewriter.SetTypewriterSpeed(0f);
	}

	/// <summary>
	/// Resumes the typewriter animation. Called when game is unpaused.
	/// </summary>
	private void ResumeTypewriter()
	{
		_typewriter.SetTypewriterSpeed(1f);
	}

	#endregion

	#region Background Sprite

	/// <summary>
	/// Updates the background image.
	/// </summary>
	/// <param name="backgroundKey">The key for the background sprite.</param>
	private void ChangeBackground(string backgroundKey)
	{
		if (string.IsNullOrEmpty(backgroundKey))
		{
			return;
		}

		if (_visualNovelDictionary.BackgroundSpriteMap.TryGetValue(backgroundKey, out Sprite bgSprite))
		{
			_backgroundImage.sprite = bgSprite;
		}
		else
		{
			Debug.LogWarning($"[VisualNovelUI] Background sprite not found for key: {backgroundKey}");
		}
	}

	#endregion

	#region Character Sprite

	/// <summary>
	/// Updates a character's state, including spawning, moving, or changing sprite.
	/// </summary>
	/// <param name="characterName">Name of the character.</param>
	/// <param name="position">Target screen position (left, right, center).</param>
	/// <param name="spriteKey">Key for the new sprite, or null/empty to keep current.</param>
	/// <param name="fadeDuration">Duration for fade transitions.</param>
	private void UpdateCharacter(string characterName, CharacterPosition position, string spriteKey, float fadeDuration)
	{
		Transform targetParent = GetPositionTransform(position);

		// Snapshot positions of existing characters in target group before layout changes
		Dictionary<VNCharacter, Vector3> positionSnapshots = SnapshotLayoutGroupPositions(targetParent);

		// This character has not been created yet, instantiate it
		if (!_activeCharacters.TryGetValue(characterName, out VNCharacter character))
		{
			character = Instantiate(_characterPrefab, targetParent).GetComponent<VNCharacter>();
			if (character == null)
			{
				Debug.LogWarning("[VisualNovelUI] Character Prefab Doesn't Have VNCharacter");
				return;
			}

			character.ChangeObjectName(characterName);
			_activeCharacters[characterName] = character;

			// Initialize alpha to 0 for fade-in
			character.SetTransparency(0f);

			// Tween characters to their new positions
			positionSnapshots[character] = character.GetPosition();
			AnimateLayoutGroupCharacters(positionSnapshots, fadeDuration);

			// Slide character from the side
			float slideOffset = CharacterPosition.Right == position ? _slideOffset : -_slideOffset;
			character.SlideIn(slideOffset, fadeDuration);
			character.FadeIn(fadeDuration);
		}
		else
		{
			// Move existing character if parent changed
			if (GetLayoutGroupParent(character) != targetParent)
			{
				// Snapshot the source group before moving
				Transform sourceParent = GetLayoutGroupParent(character);
				Dictionary<VNCharacter, Vector3> sourceSnapshots = SnapshotLayoutGroupPositions(sourceParent);

				// Save old position and move to new parent
				Vector3 oldPosition = character.GetPosition();
				character.SetParent(targetParent);

				// Add moving character to target group snapshots with old position
				positionSnapshots[character] = oldPosition;

				// Remove moving character from source snapshots
				sourceSnapshots.Remove(character);

				// Animate both groups
				AnimateLayoutGroupCharacters(positionSnapshots, fadeDuration);
				AnimateLayoutGroupCharacters(sourceSnapshots, fadeDuration);
			}
		}

		// Handle flipping based on position
		if (position == CharacterPosition.Right)
		{
			character.transform.localScale = new Vector3(
				-Mathf.Abs(character.transform.localScale.x),
				character.transform.localScale.y,
				character.transform.localScale.z
			);
		}
		else if (position == CharacterPosition.Left)
		{
			character.transform.localScale = new Vector3(
				Mathf.Abs(character.transform.localScale.x),
				character.transform.localScale.y,
				character.transform.localScale.z
			);
		}

		// Update Sprite if key provided
		if (!string.IsNullOrEmpty(spriteKey))
		{
			if (_visualNovelDictionary.CharacterSpriteMap.TryGetValue(spriteKey, out Sprite newSprite))
			{
				character.SwitchSprite(newSprite);
			}
			else
			{
				Debug.LogWarning($"[VisualNovelUI] Character sprite not found for key: {spriteKey}");
			}
		}
	}

	/// <summary>
	/// Plays an animation on a specific character.
	/// </summary>
	/// <param name="characterName">Name of the character.</param>
	/// <param name="animationID">Animation name (e.g. shake, hop).</param>
	/// <param name="args">Arguments for the animation.</param>
	private void PlayAnimation(string characterName, string animationID, string[] args)
	{
		if (!_activeCharacters.TryGetValue(characterName, out VNCharacter character))
		{
			Debug.LogWarning(
				$"[VisualNovelUI] Cannot play animation '{animationID}'. Character '{characterName}' not found."
			);
			return;
		}

		if (_animationHandlers.TryGetValue(animationID, out Action<VNCharacter, string[]> handler))
		{
			handler(character, args);
		}
		else
		{
			Debug.LogWarning($"[VisualNovelUI] Unknown animation '{animationID}' for character '{characterName}'.");
		}
	}

	#endregion

	#region Clean Up

	/// <summary>
	/// Removes a specific character from the screen.
	/// </summary>
	/// <param name="characterName">Name of the character to remove.</param>
	/// <param name="fadeDuration">Duration of fade out.</param>
	private void RemoveCharacter(string characterName, float fadeDuration)
	{
		if (_activeCharacters.TryGetValue(characterName, out VNCharacter character))
		{
			// Snapshot positions of characters in layout group
			Transform layoutGroup = GetLayoutGroupParent(character);
			Dictionary<VNCharacter, Vector3> snapshots = SnapshotLayoutGroupPositions(layoutGroup);

			// Move character to a new parent that doesn't have a layout group
			character.SetParent(_nonLayoutParent);
			character.transform.position = snapshots[character];
			snapshots.Remove(character);

			_activeCharacters.Remove(characterName);

			// Animate remaining characters
			AnimateLayoutGroupCharacters(snapshots, fadeDuration);

			// Slide out, fade out, and destroy
			float slideOffset = layoutGroup == _rightSpriteTransform ? _slideOffset : -_slideOffset;
			character.SlideOut(slideOffset, fadeDuration);
			character.FadeOutAndDestroy(fadeDuration);
		}
	}

	/// <summary>
	/// Cleans up all character sprites from the screen
	/// </summary>
	/// <param name="fadeDuration">How long it takes to fade out and delete all characters</param>
	private void RemoveAllCharacters(float fadeDuration)
	{
		var activeTransforms = new HashSet<Transform>();

		foreach (VNCharacter character in _activeCharacters.Values)
		{
			if (character != null)
			{
				activeTransforms.Add(character.transform);

				Transform parent = character.transform.parent;
				float slideOffset = parent == _rightSpriteTransform ? _slideOffset : -_slideOffset;
				character.SlideOut(slideOffset, fadeDuration);

				character.FadeOutAndDestroy(fadeDuration);
			}
		}

		_activeCharacters.Clear();

		// Helper to destroy any objects not tracked by the system
		void DestroyOrphans(Transform parent)
		{
			if (parent == null)
			{
				return;
			}

			for (int i = parent.childCount - 1; i >= 0; i--)
			{
				Transform child = parent.GetChild(i);
				if (!activeTransforms.Contains(child))
				{
					if (fadeDuration == 0)
					{
						DestroyImmediate(child.gameObject);
					}
					else
					{
						Destroy(child.gameObject);
					}
				}
			}
		}

		DestroyOrphans(_leftSpriteTransform);
		DestroyOrphans(_centerSpriteTransform);
		DestroyOrphans(_rightSpriteTransform);
	}

	/// <summary>
	/// Cleans up all character sprites from the screen using the default fade duration.
	/// </summary>
	private void RemoveAllCharacters()
	{
		RemoveAllCharacters(_defaultFadeOutDuration);
	}

	#endregion

	#region Position

	/// <summary>
	/// Captures the current positions of all characters in a layout group.
	/// </summary>
	/// <param name="layoutGroup">The layout group transforms snapshot.</param>
	/// <returns>Dictionary mapping characters to their current positions.</returns>
	private Dictionary<VNCharacter, Vector3> SnapshotLayoutGroupPositions(Transform layoutGroup)
	{
		var snapshots = new Dictionary<VNCharacter, Vector3>();
		foreach (KeyValuePair<string, VNCharacter> character in _activeCharacters)
		{
			if (character.Value != null && character.Value.transform.parent == layoutGroup)
			{
				snapshots[character.Value] = character.Value.GetPosition();
			}
		}

		return snapshots;
	}

	/// <summary>
	/// Animates all characters from their snap-shotted positions to their new layout positions.
	/// </summary>
	/// <param name="snapshots">Dictionary of characters and their old positions.</param>
	/// <param name="duration">Duration of the tween animation.</param>
	private void AnimateLayoutGroupCharacters(Dictionary<VNCharacter, Vector3> snapshots, float duration)
	{
		if (snapshots.Count == 0)
		{
			return;
		}

		// Get the layout group from the first character
		Transform layoutGroup = null;
		foreach (KeyValuePair<VNCharacter, Vector3> character in snapshots)
		{
			if (character.Key != null)
			{
				layoutGroup = character.Key.transform.parent;
				break;
			}
		}

		if (layoutGroup == null)
		{
			return;
		}

		// Rebuild layout once to get all target positions
		LayoutRebuilder.ForceRebuildLayoutImmediate(layoutGroup as RectTransform);

		// Capture target positions and start tweens
		foreach (KeyValuePair<VNCharacter, Vector3> character in snapshots)
		{
			if (character.Key != null)
			{
				Vector3 targetPosition = character.Key.transform.position;
				character.Key.TweenPositions(character.Value, targetPosition, duration);
			}
		}
	}

	/// <summary>
	/// Helper to get the Transform corresponding to a CharacterPosition enum.
	/// </summary>
	/// <param name="position">The position enum value.</param>
	/// <returns>The transform for that position.</returns>
	private Transform GetPositionTransform(CharacterPosition position)
	{
		return position switch
		{
			CharacterPosition.Left => _leftSpriteTransform,
			CharacterPosition.Right => _rightSpriteTransform,
			_ => _centerSpriteTransform,
		};
	}

	/// <summary>
	/// Get the layout group parent from a VNCharacter.
	/// </summary>
	/// <param name="character">The character to get the layout group parent from.</param>
	/// <returns>The layout group parent.</returns>
	private Transform GetLayoutGroupParent(VNCharacter character)
	{
		return character.transform.parent;
	}

	#endregion
}
