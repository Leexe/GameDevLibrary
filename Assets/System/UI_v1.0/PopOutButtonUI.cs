using FMODUnity;
using PrimeTween;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;

public class PopOutButtonUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	[Title("Scale Settings")]
	[Tooltip("Scale multiplier when hovering over the button")]
	[SerializeField]
	private float _maxSize = 1.1f;

	[Tooltip("Duration of the scale up/down animation in seconds")]
	[SerializeField]
	private float _tweenDuration = 0.2f;

	[Tooltip("Easing function for the scale animation")]
	[SerializeField]
	private Ease _ease = Ease.OutBack;

	[Tooltip("If enabled, the button will pulse between two sizes after scaling up")]
	[Title("Oscillation Settings")]
	[SerializeField]
	private bool _enableOscillation = false;

	[Tooltip("Minimum scale during oscillation, should be less than max size")]
	[SerializeField]
	[ShowIf("_enableOscillation")]
	private float _minOscillateSize = 1.05f;

	[Tooltip("Duration of one oscillation cycle in seconds")]
	[SerializeField]
	[ShowIf("_enableOscillation")]
	private float _oscillateDuration = 1.5f;

	[Tooltip("If enabled, animations will ignore Time.timeScale")]
	[Title("Sound")]
	[SerializeField]
	private EventReference _hoverSound;

	[Tooltip("If enabled, animations will ignore Time.timeScale")]
	[Title("General Settings")]
	[SerializeField]
	private bool _useUnscaledTime;

	private Tween _scaleTween;
	private Sequence _oscillateSequence;
	private Vector3 _originalScale;

	private void Start()
	{
		_originalScale = transform.localScale;
	}

	private void OnDisable()
	{
		_scaleTween.Stop();
		_oscillateSequence.Stop();
		transform.localScale = _originalScale;
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		_scaleTween.Stop();
		_oscillateSequence.Stop();

		Vector3 maxScale = _originalScale * _maxSize;
		AudioManager.Instance.PlayOneShot(_hoverSound);

		_scaleTween = Tween
			.Scale(transform, maxScale, _tweenDuration, _ease, useUnscaledTime: _useUnscaledTime)
			.OnComplete(() =>
			{
				if (_enableOscillation)
				{
					StartOscillation();
				}
			});
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		_scaleTween.Stop();
		_oscillateSequence.Stop();

		_scaleTween = Tween.Scale(transform, _originalScale, _tweenDuration, _ease, useUnscaledTime: _useUnscaledTime);
	}

	private void StartOscillation()
	{
		_scaleTween.Stop();

		Vector3 maxScale = _originalScale * _maxSize;
		Vector3 minScale = _originalScale * _minOscillateSize;

		_oscillateSequence = Sequence
			.Create(-1, useUnscaledTime: _useUnscaledTime)
			.Chain(Tween.Scale(transform, minScale, _oscillateDuration / 2, Ease.InOutSine))
			.Chain(Tween.Scale(transform, maxScale, _oscillateDuration / 2, Ease.InOutSine));
	}
}
