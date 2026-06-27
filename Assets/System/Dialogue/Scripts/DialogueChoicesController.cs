using System.Collections.Generic;
using UnityEngine;

public class DialogueChoicesController : MonoBehaviour
{
	[SerializeField]
	private DialogueController _dialogueController;

	[SerializeField]
	private GameObject _dialogueBoxPrefab;

	private readonly List<DialogueChoiceBox> _choiceBoxes = new();

	private void OnEnable()
	{
		_dialogueController.DialogueState.OnDisplayChoices += DisplayChoices;
		_dialogueController.DialogueState.OnChoiceSelect += HideChoices;
		_dialogueController.DialogueState.OnStartStory += HideChoices;
	}

	private void OnDisable()
	{
		if (_dialogueController)
		{
			_dialogueController.DialogueState.OnDisplayChoices -= DisplayChoices;
			_dialogueController.DialogueState.OnChoiceSelect -= HideChoices;
			_dialogueController.DialogueState.OnStartStory -= HideChoices;
		}
	}

	private void DisplayChoices(List<string> choices)
	{
		// Instantiate new choice boxes when needed
		int boxesNeeded = Mathf.Max(choices.Count - _choiceBoxes.Count, 0);
		for (int i = 0; i < boxesNeeded; i++)
		{
			_choiceBoxes.Add(Instantiate(_dialogueBoxPrefab, transform).GetComponent<DialogueChoiceBox>());
		}

		for (int i = 0; i < _choiceBoxes.Count; i++)
		{
			if (i < choices.Count)
			{
				_choiceBoxes[i].gameObject.SetActive(true);
				_choiceBoxes[i].SetText(choices[i]);
				_choiceBoxes[i].SetChoiceIndex(i);
			}
			else
			{
				_choiceBoxes[i].gameObject.SetActive(false);
			}
		}
	}

	private void HideChoices(string knotName)
	{
		HideChoices();
	}

	private void HideChoices(int choiceIndex = 0)
	{
		foreach (DialogueChoiceBox choiceBox in _choiceBoxes)
		{
			choiceBox.gameObject.SetActive(false);
		}
	}
}
