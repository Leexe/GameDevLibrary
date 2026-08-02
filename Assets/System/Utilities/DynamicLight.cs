using PrimeTween;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Light))]
public class DynamicLight : MonoBehaviour
{
	public enum FlickerMode
	{
		SineWave,
		Random,
		Perlin,
		Strobe,
		Pulse,
	}

	[Header("Flicker Mode")]
	[SerializeField]
	private FlickerMode _flickerMode = FlickerMode.Perlin;

	[Header("Intensity")]
	[SerializeField, Range(0f, 10f)]
	private float _intensityAmplitude = 1f;

	[SerializeField, Range(0.1f, 50f)]
	private float _intensitySpeed = 5f;

	[Header("Color")]
	[SerializeField]
	private bool _flickerColor;

	[SerializeField]
	[ShowIf("@_flickerColor")]
	private Color _alternateColor = new(1f, 0.5f, 0f);

	[SerializeField, Range(0f, 1f)]
	[ShowIf("@_flickerColor")]
	private float _colorBlendStrength = 0.3f;

	[Header("Range")]
	[SerializeField]
	private bool _flickerRange;

	[SerializeField, Range(0f, 20f)]
	[ShowIf("@_flickerRange")]
	private float _rangeAmplitude = 2f;

	[Header("Strobe Settings")]
	[SerializeField, Range(0.01f, 2f)]
	[ShowIf("@_flickerMode == FlickerMode.Strobe")]
	private float _strobeOnDuration = 0.05f;

	[SerializeField, Range(0.01f, 2f)]
	[ShowIf("@_flickerMode == FlickerMode.Strobe")]
	private float _strobeOffDuration = 0.1f;

	[Header("Advanced")]
	[SerializeField]
	private bool _randomizeOffsetOnStart = true;

	[SerializeField]
	[HideIf("@_randomizeOffsetOnStart")]
	private float _noiseOffset;

	[SerializeField, Range(1f, 20f)]
	[ShowIf("@_flickerMode == FlickerMode.Random || _flickerMode == FlickerMode.Perlin")]
	[Tooltip("Lower values provide smoother transitions, while higher values are more jumpy")]
	private float _smoothing = 5f;

	private Light _targetLight;
	private float _baseIntensity;
	private float _targetIntensity;
	private Color _baseColor;
	private Color _targetColor;

	private Sequence _lightSequence;
	private float _baseRange;
	private float _strobeTimer;
	private bool _strobeOn = true;

	private void Awake()
	{
		_targetLight = GetComponent<Light>();
	}

	private void Start()
	{
		_baseIntensity = _targetLight.intensity;
		_targetIntensity = _baseIntensity;
		_baseColor = _targetLight.color;
		_targetColor = _baseColor;
		_baseRange = _targetLight.range;

		if (_randomizeOffsetOnStart)
		{
			_noiseOffset = Random.Range(0f, 1000f);
		}
	}

	private void OnDisable()
	{
		_lightSequence.Stop();
	}

	private void Update()
	{
		HandleFlicker();
	}

	private void HandleFlicker()
	{
		float flickerValue = GetFlickerValue();

		float targetIntensity = _targetIntensity + (flickerValue * _intensityAmplitude);
		targetIntensity = Mathf.Max(targetIntensity, 0f);

		if (_flickerMode == FlickerMode.Random || _flickerMode == FlickerMode.Perlin)
		{
			_targetLight.intensity = Mathf.Lerp(_targetLight.intensity, targetIntensity, Time.deltaTime * _smoothing);
		}
		else
		{
			_targetLight.intensity = targetIntensity;
		}

		if (_flickerColor)
		{
			float colorT = Mathf.InverseLerp(-1f, 1f, flickerValue) * _colorBlendStrength;
			_targetLight.color = Color.Lerp(_targetColor, _alternateColor, colorT);
		}
		else if (_targetLight.color != _targetColor)
		{
			_targetLight.color = _targetColor;
		}

		if (_flickerRange)
		{
			float rangeValue = _baseRange + (flickerValue * _rangeAmplitude);
			_targetLight.range = Mathf.Max(rangeValue, 0.1f);
		}
	}

	private float GetFlickerValue()
	{
		float t = (Time.time * _intensitySpeed) + _noiseOffset;

		switch (_flickerMode)
		{
			case FlickerMode.SineWave:
				return Mathf.Sin(t);

			case FlickerMode.Random:
				return Random.Range(-1f, 1f);

			case FlickerMode.Perlin:
				float noise = (Mathf.PerlinNoise(t, _noiseOffset + 100f) * 2f) - 1f;
				float noise2 = (Mathf.PerlinNoise(t * 2.7f, _noiseOffset + 200f) * 2f) - 1f;
				return (noise * 0.7f) + (noise2 * 0.3f);

			case FlickerMode.Strobe:
				return GetStrobeValue();

			case FlickerMode.Pulse:
				float saw = Mathf.Repeat(t, 1f);
				return (saw * 2f) - 1f;

			default:
				return 0f;
		}
	}

	private float GetStrobeValue()
	{
		_strobeTimer -= Time.deltaTime;

		if (_strobeTimer <= 0f)
		{
			_strobeOn = !_strobeOn;
			_strobeTimer = _strobeOn ? _strobeOnDuration : _strobeOffDuration;
		}

		return _strobeOn ? 1f : -1f;
	}

	// Public Methods

	public void SetFlickerMode(FlickerMode mode)
	{
		_flickerMode = mode;
	}

	public void SetBaseIntensity(float intensity)
	{
		_baseIntensity = intensity;
		_targetIntensity = intensity;
	}

	/// <summary>
	/// Tweens from the target intensity back to base after a duration
	/// </summary>
	/// <param name="flashIntensity">How bright the light flashes</param>
	/// <param name="duration">How long before the light returns to base</param>
	public void FlashIntensity(float flashIntensity, float duration)
	{
		_lightSequence.Stop();
		_lightSequence = Sequence.Create();
		_lightSequence.Chain(
			Tween.Custom(
				target: this,
				flashIntensity,
				_baseIntensity,
				duration,
				(target, val) => target._targetIntensity = val
			)
		);
	}

	/// <summary>
	/// Tweens from the target intensity/color back to base after a duration
	/// </summary>
	/// <param name="flashIntensity">How bright the light flashes</param>
	/// <param name="flashColor">The color to flash from</param>
	/// <param name="duration">How long before the light returns to base</param>
	public void FlashIntensity(float flashIntensity, Color flashColor, float duration)
	{
		_lightSequence.Stop();
		_lightSequence = Sequence.Create();
		_lightSequence.Group(
			Tween.Custom(
				target: this,
				flashIntensity,
				_baseIntensity,
				duration,
				(target, val) => target._targetIntensity = val
			)
		);
		_lightSequence.Group(
			Tween.Custom(target: this, flashColor, _baseColor, duration, (target, val) => target._targetColor = val)
		);
	}

	/// <summary>
	/// Tweens target intensity and color to target parameters
	/// </summary>
	/// <param name="targetIntensity">How bright the light is</param>
	/// <param name="targetColor">The target color</param>
	/// <param name="duration">How long the transition takes</param>
	public void TweenLights(float targetIntensity, Color targetColor, float duration)
	{
		_lightSequence.Stop();
		_lightSequence = Sequence.Create();
		_lightSequence.Group(
			Tween.Custom(
				target: this,
				_targetIntensity,
				targetIntensity,
				duration,
				(target, val) => target._targetIntensity = val
			)
		);
		_lightSequence.Group(
			Tween.Custom(target: this, _targetColor, targetColor, duration, (target, val) => target._targetColor = val)
		);
	}

	/// <summary>
	/// Resets the lights back to base intensity and color
	/// </summary>
	/// <param name="duration">How long the transition takes</param>
	public void ResetLights(float duration)
	{
		_lightSequence.Stop();
		_lightSequence = Sequence.Create();
		_lightSequence.Group(
			Tween.Custom(
				target: this,
				_targetIntensity,
				_baseIntensity,
				duration,
				(target, val) => target._targetIntensity = val
			)
		);
		_lightSequence.Group(
			Tween.Custom(target: this, _targetColor, _baseColor, duration, (target, val) => target._targetColor = val)
		);
	}

	public void KillLight()
	{
		enabled = false;
		_targetLight.intensity = 0f;
	}

	public void ReenableLight()
	{
		enabled = true;
		_targetIntensity = _baseIntensity;
	}
}
