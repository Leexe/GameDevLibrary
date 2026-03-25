using Febucci.TextAnimatorForUnity;
using UnityEngine;

public class HiddenTypewriter : MonoBehaviour
{
	[SerializeField]
	private TypewriterComponent _hiddenTypewriter;

	private DialogueState _dialogueState;

	private void OnEnable()
	{
		_dialogueState = GameManager.Instance.Dialogue;
		_dialogueState.OnDisplayDialogue += ChangeStoryText;

		// Uncomment: Need to have this listener for the typewriter to follow the timescale correctly
		GameManager.Instance.OnGamePause.AddListener(PauseTypewriter);
		GameManager.Instance.OnGameUnpause.AddListener(ResumeTypewriter);
	}

	private void OnDisable()
	{
		if (_dialogueState != null)
		{
			_dialogueState.OnDisplayDialogue -= ChangeStoryText;
		}

		if (GameManager.Instance != null)
		{
			GameManager.Instance.OnGamePause.RemoveListener(PauseTypewriter);
			GameManager.Instance.OnGameUnpause.RemoveListener(ResumeTypewriter);
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
