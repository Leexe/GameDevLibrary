using Febucci.TextAnimatorForUnity;
using PrimeTween;
using Sirenix.OdinInspector;
using UnityEngine;

public class DialogueBox : MonoBehaviour
{
	[FoldoutGroup("References")]
	[SerializeField]
	private CanvasGroup _canvas;

	[FoldoutGroup("References")]
	[SerializeField]
	private TypewriterComponent _typewriter;

	[FoldoutGroup("Data")]
	[SerializeField]
	private string _characterID;

	[FoldoutGroup("Data")]
	[SerializeField]
	private Tween _canvasAlphaTween;

	[FoldoutGroup("Data")]
	[SerializeField]
	private float _fadeDuration = 0.25f;

	private void OnEnable()
	{
		DialogueEvents.Instance.OnDisplayDialogue += OnDisplayDialogue;
		DialogueEvents.Instance.OnTypewriterSkip += SkipTypewriter;
		_typewriter.onTextShowed.AddListener(DialogueEvents.Instance.TypewriterFinished);
	}

	private void OnDisable()
	{
		_typewriter.onTextShowed.RemoveListener(DialogueEvents.Instance.TypewriterFinished);
		if (DialogueEvents.Instance != null)
		{
			DialogueEvents.Instance.OnDisplayDialogue -= OnDisplayDialogue;
			DialogueEvents.Instance.OnTypewriterSkip -= SkipTypewriter;
		}
	}

	private void SkipTypewriter()
	{
		if (_canvas.alpha > 0)
		{
			_typewriter.SkipTypewriter();
		}
	}

	private void OnDisplayDialogue(string characterName, string text)
	{
		if (string.Equals(characterName, _characterID, System.StringComparison.OrdinalIgnoreCase))
		{
			OpenBox();
			_typewriter.ShowText(text);
		}
		else
		{
			CloseBox();
		}
	}

	private void CloseBox()
	{
		_canvasAlphaTween.Stop();
		_canvasAlphaTween = Tween.Custom(_canvas.alpha, 0, _fadeDuration, newVal => _canvas.alpha = newVal);
	}

	private void OpenBox()
	{
		_canvasAlphaTween.Stop();
		_canvasAlphaTween = Tween.Custom(_canvas.alpha, 1, _fadeDuration, newVal => _canvas.alpha = newVal);
	}
}
