using PrimeTween;
using Sirenix.OdinInspector;
using UnityEngine;

public class SwayingUI : MonoBehaviour
{
	/* Position Shake */
	private enum PositionTweenType
	{
		None,
		Shake,
		Smooth,
	}

	[FoldoutGroup("Position Shake")]
	[SerializeField]
	private PositionTweenType _positionTweenType = PositionTweenType.Shake;

	[FoldoutGroup("Position Shake")]
	[SerializeField]
	[ShowIf("@_positionTweenType != PositionTweenType.None")]
	private Vector3 _positionStrength = new(5f, 5f, 0f);

	[FoldoutGroup("Position Shake")]
	[SerializeField]
	[ShowIf("@_positionTweenType == PositionTweenType.Shake")]
	[MinValue(0.01f)]
	private float _positionFrequency = 0.5f;

	[FoldoutGroup("Position Shake")]
	[SerializeField]
	[ShowIf("@_positionTweenType != PositionTweenType.None")]
	private Ease _positionEase = Ease.Linear;

	[FoldoutGroup("Position Shake")]
	[SerializeField]
	[ShowIf("@_positionTweenType != PositionTweenType.None")]
	private float _positionDuration = 5f;

	/* Rotation Tween */

	[FoldoutGroup("Rotation Tween")]
	[SerializeField]
	private bool _rotationTweenEnable = true;

	[FoldoutGroup("Rotation Tween")]
	[SerializeField]
	[ShowIf("@_rotationTweenEnable")]
	[Tooltip("How far the rotation will go in degrees, following a pendulum motion")]
	private Vector3 _rotationStrength = new(0f, 0f, 5f);

	[FoldoutGroup("Rotation Tween")]
	[SerializeField]
	[ShowIf("@_rotationTweenEnable")]
	private Ease _rotationEase = Ease.InOutSine;

	[FoldoutGroup("Rotation Tween")]
	[SerializeField]
	[ShowIf("@_rotationTweenEnable")]
	[Tooltip("Randomize the starting point of the rotation animation")]
	private bool _rotationRandomStart;

	[FoldoutGroup("Rotation Tween")]
	[SerializeField]
	[ShowIf("@_rotationTweenEnable")]
	private float _rotationDuration = 5f;

	/* Scale Tween */

	[FoldoutGroup("Scale Tween")]
	[SerializeField]
	private bool _scaleTweenEnable = true;

	[FoldoutGroup("Scale Tween")]
	[SerializeField]
	[ShowIf("@_scaleTweenEnable")]
	[Tooltip("How strong the scale tween is (1.5 means scale to 150% then down to ~67%)")]
	[Range(1f, 5f)]
	private float _scaleStrength = 1.1f;

	[FoldoutGroup("Scale Tween")]
	[SerializeField]
	[ShowIf("@_scaleTweenEnable")]
	private Ease _scaleEase = Ease.InOutSine;

	[FoldoutGroup("Scale Tween")]
	[SerializeField]
	[ShowIf("@_scaleTweenEnable")]
	[Tooltip("Randomize the starting point of the scale animation")]
	private bool _scaleRandomStart;

	[FoldoutGroup("Scale Tween")]
	[SerializeField]
	[ShowIf("@_scaleTweenEnable")]
	private float _scaleDuration = 5f;

	/* General Settings */

	[FoldoutGroup("General Settings")]
	[SerializeField]
	private bool _activateOnStart = true;

	[FoldoutGroup("General Settings")]
	[SerializeField]
	private bool _useUnscaledTime;

	private Sequence _positionTween;
	private Sequence _rotationTween;
	private Sequence _scaleTween;

	private RectTransform _rectTransform;
	private Vector2 _initialRectPosition;
	private Vector2 _endPosition;
	private Vector3 _initialLocalPosition;
	private Vector3 _initialRotation;
	private Vector3 _initialScale;

	private void Awake()
	{
		_rectTransform = GetComponent<RectTransform>();
	}

	private void Start()
	{
		Canvas.ForceUpdateCanvases();

		_initialRectPosition = _rectTransform.anchoredPosition;
		_initialLocalPosition = transform.localPosition;
		_endPosition = _initialRectPosition + (Vector2)_positionStrength;
		_initialRotation = transform.localEulerAngles;
		_initialScale = transform.localScale;

		if (_activateOnStart)
		{
			StartTween();
		}
	}

	[FoldoutGroup("Debug")]
	[Button]
	public void StartTween()
	{
		StopTween();

		if (_positionTweenType == PositionTweenType.Shake)
		{
			_rectTransform.anchoredPosition = _initialRectPosition;
			transform.localPosition = _initialLocalPosition;

			_positionTween = Sequence
				.Create(-1, useUnscaledTime: _useUnscaledTime)
				.Chain(
					Tween.ShakeLocalPosition(
						transform,
						_positionStrength,
						_positionDuration,
						_positionFrequency,
						easeBetweenShakes: _positionEase,
						enableFalloff: false
					)
				);
		}

		if (_positionTweenType == PositionTweenType.Smooth)
		{
			_rectTransform.anchoredPosition = _initialRectPosition;

			_positionTween = Sequence
				.Create(-1, useUnscaledTime: _useUnscaledTime, cycleMode: Sequence.SequenceCycleMode.Yoyo)
				.Chain(Tween.UIAnchoredPosition(_rectTransform, _endPosition, _positionDuration, ease: _positionEase));
		}

		if (_rotationTweenEnable)
		{
			float halfDuration = _rotationDuration / 2f;
			Vector3 positiveRotation = _initialRotation + _rotationStrength;
			Vector3 negativeRotation = _initialRotation - _rotationStrength;

			transform.localRotation = Quaternion.Euler(negativeRotation);

			_rotationTween = Sequence
				.Create(-1, Sequence.SequenceCycleMode.Yoyo, useUnscaledTime: _useUnscaledTime)
				.Chain(Tween.LocalRotation(transform, positiveRotation, halfDuration, _rotationEase))
				.Chain(Tween.LocalRotation(transform, negativeRotation, halfDuration, _rotationEase));

			if (_rotationRandomStart)
			{
				_rotationTween.elapsedTime = Random.Range(0f, _rotationDuration);
			}
		}

		if (_scaleTweenEnable)
		{
			float halfDuration = _scaleDuration / 2f;
			Vector3 largeScale = _initialScale * _scaleStrength;
			Vector3 smallScale = _initialScale / _scaleStrength;

			transform.localScale = smallScale;

			_scaleTween = Sequence
				.Create(-1, Sequence.SequenceCycleMode.Yoyo, useUnscaledTime: _useUnscaledTime)
				.Chain(Tween.Scale(transform, largeScale, halfDuration, _scaleEase))
				.Chain(Tween.Scale(transform, smallScale, halfDuration, _scaleEase));

			if (_scaleRandomStart)
			{
				_scaleTween.elapsedTime = Random.Range(0f, _scaleDuration);
			}
		}
	}

	[FoldoutGroup("Debug")]
	[Button]
	public void StopTween()
	{
		_positionTween.Stop();
		_rotationTween.Stop();
		_scaleTween.Stop();
	}

	[FoldoutGroup("Debug")]
	[Button]
	public void TweenBackToDefault(float tweenDuration = 1f)
	{
		StopTween();

		if (_positionTweenType == PositionTweenType.Shake)
		{
			Tween.LocalPosition(transform, _initialLocalPosition, tweenDuration, _positionEase);
		}
		else if (_positionTweenType == PositionTweenType.Smooth)
		{
			Tween.UIAnchoredPosition(_rectTransform, _initialRectPosition, tweenDuration, _positionEase);
		}

		if (_rotationTweenEnable)
		{
			Tween.LocalRotation(transform, _initialRotation, tweenDuration, _rotationEase);
		}

		if (_scaleTweenEnable)
		{
			Tween.Scale(transform, _initialScale, tweenDuration, _scaleEase);
		}
	}

	private void OnDisable()
	{
		StopTween();
	}
}
