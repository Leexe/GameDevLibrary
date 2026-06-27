using Febucci.TextAnimatorForUnity;
using UnityEngine;

public class HiddenTypewriter : MonoBehaviour
{
	[SerializeField]
	private DialogueController _dialogueController;

	[SerializeField]
	private TypewriterComponent _hiddenTypewriter;

	private DialogueState _dialogueState;

	private void OnEnable()
	{
		_dialogueState = _dialogueController.DialogueState;
		_dialogueState.OnDisplayDialogue += ChangeStoryText;
	}

	private void OnDisable()
	{
		if (_dialogueState != null)
		{
			_dialogueState.OnDisplayDialogue -= ChangeStoryText;
		}
	}

	private void ChangeStoryText(string characterName, string line)
	{
		_hiddenTypewriter.ShowText(line);
	}
}
