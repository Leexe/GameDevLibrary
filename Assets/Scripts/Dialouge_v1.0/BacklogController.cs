using System.Collections.Generic;
using PrimeTween;
using TMPro;
using UnityEngine;

public class BacklogController : MonoBehaviour
{
	[Header("References")]
	[SerializeField]
	private TextMeshProUGUI _text;

	[SerializeField]
	private CanvasGroup _canvas;

	[Header("Data")]
	[SerializeField]
	private int _maxBacklog = 50;

	[SerializeField]
	private float _tweenDuration = 0.25f;

	private readonly List<string> _backlog = new();
	private DialogueEvents DialogueEvents => DialogueEvents.Instance;
	private Tween _opacityTween;
	private bool _isOpen;

	public bool IsOpen => _isOpen;

	private void OnEnable()
	{
		DialogueEvents.OnDisplayDialogue += AddToBacklog;
		DialogueEvents.AddBlockingCondition(() => IsOpen);
		InputManager.Instance.OnBacklogPerformed.AddListener(ToggleBacklog);

		_canvas.alpha = 0f;
		_canvas.blocksRaycasts = false;
		ClearText();
	}

	private void OnDisable()
	{
		DialogueEvents.OnDisplayDialogue -= AddToBacklog;
		DialogueEvents.RemoveBlockingCondition(() => IsOpen);

		if (InputManager.Instance)
		{
			InputManager.Instance.OnBacklogPerformed.RemoveListener(ToggleBacklog);
		}
	}

	private void ToggleBacklog()
	{
		// Game is paused, don't allow backlog to be opened or closed
		if (Time.timeScale == 0f)
		{
			return;
		}

		if (_isOpen)
		{
			CloseBacklog();
			_isOpen = false;
		}
		else
		{
			OpenBacklog();
			_isOpen = true;
		}
	}

	private void OpenBacklog()
	{
		_opacityTween.Stop();
		_opacityTween = Tween.Custom(
			_canvas,
			0f,
			1f,
			_tweenDuration,
			(t, val) =>
			{
				t.alpha = val;
			}
		);
		_canvas.blocksRaycasts = true;
	}

	private void CloseBacklog()
	{
		_opacityTween.Stop();
		_opacityTween = Tween.Custom(
			_canvas,
			1f,
			0f,
			_tweenDuration,
			(t, val) =>
			{
				t.alpha = val;
			}
		);
		_canvas.blocksRaycasts = false;
	}

	private void AddToBacklog(string characterName, string backlog)
	{
		if (_backlog.Count >= _maxBacklog)
		{
			_backlog.RemoveAt(0);
		}

		string entry = string.IsNullOrEmpty(characterName) ? backlog : $"{characterName}: {backlog}";

		_backlog.Add(entry);
		UpdateText();
	}

	private void UpdateText()
	{
		_text.text = string.Join("\n\n", _backlog);
	}

	private void ClearText()
	{
		_backlog.Clear();
		UpdateText();
	}
}
