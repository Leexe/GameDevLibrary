using FMODUnity;
using TMPro;
using UnityEngine;

public class DialogueChoiceBox : MonoBehaviour
{
	[SerializeField]
	private DialogueController _dialogueController;

	[SerializeField]
	private TextMeshProUGUI _choiceBoxText;

	[Tooltip("Optional click sound effect. Leave empty for no sound.")]
	[SerializeField]
	private EventReference _clickSfx;

	private int _choiceIndex;

	public void SetText(string text)
	{
		_choiceBoxText.text = text;
	}

	public void SetChoiceIndex(int index)
	{
		_choiceIndex = index;
	}

	public void OnChoicePressed()
	{
		if (!_clickSfx.IsNull)
		{
			AudioManager.Instance.PlayOneShot(_clickSfx);
		}

		_dialogueController.DialogueState.UpdateChoiceSelected(_choiceIndex);
	}
}
