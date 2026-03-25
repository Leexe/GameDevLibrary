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
		_dialogueState.OnPause += PauseTypewriter;
		_dialogueState.OnUnpause += ResumeTypewriter;
	}

	private void OnDisable()
	{
		if (_dialogueState != null)
		{
			_dialogueState.OnDisplayDialogue -= ChangeStoryText;
			_dialogueState.OnPause -= PauseTypewriter;
			_dialogueState.OnUnpause -= ResumeTypewriter;
		}
	}

	private void ChangeStoryText(string characterName, string line)
	{
		_hiddenTypewriter.ShowText(line);
	}

	private void PauseTypewriter()
	{
		_hiddenTypewriter.SetTypewriterSpeed(0f);
	}

	private void ResumeTypewriter()
	{
		_hiddenTypewriter.SetTypewriterSpeed(1f);
	}
}
