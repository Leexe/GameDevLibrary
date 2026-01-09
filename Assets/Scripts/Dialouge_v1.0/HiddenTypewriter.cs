using Febucci.TextAnimatorForUnity;
using UnityEngine;

public class HiddenTypewriter : MonoBehaviour
{
	[SerializeField]
	private TypewriterComponent _hiddenTypewriter;

	private DialogueEvents DialogueEvents => DialogueEvents.Instance;

	private void OnEnable()
	{
		DialogueEvents.OnDisplayDialogue += ChangeStoryText;

		// Uncomment: Need to have this listener for the typewriter to follow the timescale correctly
		// GameManager.Instance.OnGamePaused.AddListener(PauseTypewriter);
		// GameManager.Instance.OnGameResume.AddListener(ResumeTypewriter);
	}

	private void OnDisable()
	{
		if (DialogueEvents != null)
		{
			DialogueEvents.OnDisplayDialogue -= ChangeStoryText;
		}

		// if (GameManager.Instance != null)
		// {
		// 	GameManager.Instance.OnGamePaused.RemoveListener(PauseTypewriter);
		// 	GameManager.Instance.OnGameResume.RemoveListener(ResumeTypewriter);
		// }
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
