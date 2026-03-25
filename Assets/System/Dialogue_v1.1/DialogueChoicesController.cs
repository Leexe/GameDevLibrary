using System.Collections.Generic;
using Animancer;
using UnityEngine;

public class DialogueChoicesController : MonoBehaviour
{
	[SerializeField]
	private GameObject _dialogueBoxPrefab;

	private readonly List<DialogueChoiceBox> _choiceBoxes = new();

	private void OnEnable()
	{
		GameManager.Instance.Dialogue.OnDisplayChoices += DisplayChoices;
		GameManager.Instance.Dialogue.OnChoiceSelect += HideChoices;
		GameManager.Instance.Dialogue.OnStartStory += HideChoices;
	}

	private void OnDisable()
	{
		if (GameManager.Instance)
		{
			GameManager.Instance.Dialogue.OnDisplayChoices -= DisplayChoices;
			GameManager.Instance.Dialogue.OnChoiceSelect -= HideChoices;
			GameManager.Instance.Dialogue.OnStartStory -= HideChoices;
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
