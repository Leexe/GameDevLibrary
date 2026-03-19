using System.Collections.Generic;
using FMODUnity;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "VoiceSO", menuName = "ScriptableObjects/VisualNovel/VoiceSO", order = 1)]
public class VoiceSO : SerializedScriptableObject
{
	[Tooltip("A list of sounds that the voice will be composed of")]
	public List<EventReference> Sounds;

	[Tooltip("How often to play the sfx, every n characters")]
	public int Frequency = 1;

	[Tooltip("The pitch range that a voice would be played at (0.5 = half pitch, 1 = normal, 2 = double)")]
	[MinMaxSlider(0.5f, 2f, ShowFields = true)]
	public Vector2 PitchVariation = new(0.9f, 1.1f);

	// Private Variables
	private int _characterCount;

	/// <summary>
	/// Plays a voice sample with random pitch variation.
	/// </summary>
	/// <param name="character">The character being displayed (used for frequency tracking).</param>
	public void PlayVoice(char character)
	{
		// Skip whitespace and punctuation for frequency counting
		if (char.IsWhiteSpace(character) || char.IsPunctuation(character))
		{
			return;
		}

		_characterCount++;

		// Only play on frequency interval
		if (_characterCount % Frequency != 0)
		{
			return;
		}

		if (Sounds == null || Sounds.Count == 0)
		{
			return;
		}

		// Get a scrambled hash of the character for deterministic mapping
		int charHash = GetCharHash(character);

		// Pick a sound from the list and play with mapped pitch
		EventReference soundRef = Sounds[charHash % Sounds.Count];
		float pitch = GetPitchFromMap(charHash);
		AudioManager.Instance.PlayOneShotWithPitch(soundRef, pitch);
	}

	/// <summary>
	/// Resets the character count (call when a new dialogue line starts).
	/// </summary>
	public void ResetCount()
	{
		_characterCount = 0;
	}

	/// <summary>
	/// Gets a scrambled hash from a character's Unicode value.
	/// </summary>
	/// <param name="character">The character to hash.</param>
	/// <returns>A scrambled hash value.</returns>
	private int GetCharHash(char character)
	{
		int charCode = character;
		return ((charCode * 31) + 17) ^ (charCode >> 2);
	}

	/// <summary>
	/// Gets a deterministic pitch value from the pitch variation range.
	/// </summary>
	/// <param name="charHash">The scrambled hash of the character.</param>
	/// <returns>A pitch value within the pitch variation range.</returns>
	private float GetPitchFromMap(int charHash)
	{
		float t = Mathf.Abs(charHash % 101) / 100f;
		return Mathf.Lerp(PitchVariation.x, PitchVariation.y, t);
	}
}
