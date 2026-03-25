using TMPro;
using UnityEngine;

public class DialogueChoiceBox : MonoBehaviour
{
	[SerializeField]
	private TextMeshProUGUI _choiceBoxText;

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
		AudioManager.Instance.PlayOneShot(FMODEvents.Instance.ButtonClick_Sfx);
		GameManager.Instance.Dialogue.OnChoiceSelect(_choiceIndex);
	}
}
