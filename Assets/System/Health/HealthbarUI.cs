using PrimeTween;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
	#region Enums

	private enum ReferenceType
	{
		EventChannel,
		DirectReference,
	}

	private enum HealthDataType
	{
		Image,
		Shader,
	}

	#endregion

	#region Settings

	[TabGroup("Settings", "References")]
	[SerializeField]
	private HealthDataType _healthDataType = HealthDataType.Shader;

	[TabGroup("Settings", "References")]
	[SerializeField]
	[ShowIf("@_healthDataType == HealthDataType.Shader")]
	private Renderer _healthBarRenderer;

	[TabGroup("Settings", "References")]
	[SerializeField]
	[ShowIf("@_healthDataType == HealthDataType.Image")]
	private Image _healthImage;

	[TabGroup("Settings", "References")]
	[SerializeField]
	[ShowIf("@_healthDataType == HealthDataType.Image")]
	private Image _tempHealthImage;

	[TabGroup("Settings", "Data Source")]
	[Tooltip("Use to toggle event channel mode and off for direct reference")]
	[SerializeField]
	private ReferenceType _referenceType = ReferenceType.EventChannel;

	[TabGroup("Settings", "Data Source")]
	[SerializeField]
	[ShowIf("@_referenceType == ReferenceType.EventChannel")]
	[Indent]
	private HealthEventChannelSO _channel;

	[TabGroup("Settings", "Data Source")]
	[SerializeField]
	[ShowIf("@_referenceType == ReferenceType.DirectReference")]
	[Indent]
	private HealthController _directController;

	[TabGroup("Settings", "Fade Out Health")]
	[SerializeField]
	[ToggleLeft]
	private bool _enableFadeOutHealth = true;

	[TabGroup("Settings", "Fade Out Health")]
	[SerializeField]
	[ShowIf("_enableFadeOutHealth")]
	[Indent]
	[Tooltip("How long the fade out takes")]
	[SuffixLabel("seconds")]
	private float _fadeDuration = 0.5f;

	[TabGroup("Settings", "Fade Out Health")]
	[SerializeField]
	[ShowIf("_enableFadeOutHealth")]
	[Indent]
	[Tooltip("How long it takes after damage for the fade out to start")]
	[SuffixLabel("seconds")]
	private float _fadeDelay = 1f;

	[TabGroup("Settings", "Shake Effect")]
	[SerializeField]
	[ToggleLeft]
	private bool _enableShakeOnDamage = true;

	[TabGroup("Settings", "Shake Effect")]
	[SerializeField]
	[ShowIf("_enableShakeOnDamage")]
	private Transform _healthBarTransform;

	[TabGroup("Settings", "Shake Effect")]
	[SerializeField]
	[ShowIf("_enableShakeOnDamage")]
	[Indent]
	[Tooltip("How far the UI shakes from its original position")]
	private Vector3 _shakeStrength = new(5f, 5f, 0f);

	[TabGroup("Settings", "Shake Effect")]
	[SerializeField]
	[ShowIf("_enableShakeOnDamage")]
	[Indent]
	[Tooltip("How long the shake lasts")]
	[SuffixLabel("seconds")]
	private float _shakeDuration = 0.3f;

	[TabGroup("Settings", "Shake Effect")]
	[SerializeField]
	[ShowIf("_enableShakeOnDamage")]
	[Indent]
	[Tooltip("How many times the UI oscillates during the shake")]
	private int _shakeFrequency = 10;

	[TabGroup("Settings", "Shake Effect")]
	[SerializeField]
	[ShowIf("_enableShakeOnDamage")]
	[Indent]
	[Tooltip("Easing for the shake")]
	private Ease _shakeEase = Ease.OutQuad;

	[TabGroup("Settings", "Misc")]
	[SerializeField]
	[MinMaxSlider(0f, 1f, true)]
	[Tooltip("The range of the health bar's fill amount that is visible")]
	private Vector2 _visibleFillRange = new(0f, 1f);

	#endregion

	#region State

	private Tween _shakeTween;
	private Sequence _fadeHealthSequence;
	private readonly int _healthPropertyName = Shader.PropertyToID("_Health");
	private readonly int _tempHealthPropertyName = Shader.PropertyToID("_TempHealth");
	private readonly int _tempHealthFadeOutPropertyName = Shader.PropertyToID("_FadeSecondaryHealth");
	private float _tempHealth = 1;

	#endregion

	#region Initialization

	private void OnValidate()
	{
		if (_referenceType == ReferenceType.EventChannel && _directController != null)
		{
			_directController = null;
		}

		if (_referenceType == ReferenceType.DirectReference && _channel != null)
		{
			_channel = null;
		}
	}

	private void Start()
	{
		// Initalize Health Bars
		if (_referenceType == ReferenceType.EventChannel)
		{
			UpdateHealthBar(1f, 1f);
			_tempHealth = 1;
		}
		else if (_referenceType == ReferenceType.DirectReference)
		{
			UpdateHealthBar(_directController.Health, _directController.MaxHealth);
			_tempHealth = _directController.GetNormalizedHealth;
		}
	}

	private void OnEnable()
	{
		if (_referenceType == ReferenceType.EventChannel)
		{
			_channel.OnHealthChanged += OnChannelHealthChanged;
			_channel.OnDeath += OnDeath;
		}
		else if (_referenceType == ReferenceType.DirectReference)
		{
			_directController.OnHealthChanged.AddListener(OnHealthChanged);
			_directController.OnDeath.AddListener(OnDeath);
		}
	}

	private void OnDisable()
	{
		if (_channel != null)
		{
			_channel.OnHealthChanged -= OnChannelHealthChanged;
		}

		if (_directController != null)
		{
			_directController.OnHealthChanged.RemoveListener(OnHealthChanged);
			_directController.OnDeath.RemoveListener(OnDeath);
		}
	}

	#endregion

	#region Event Listeners

	private void OnHealthChanged(float delta, float currentHealth, float maxHealth)
	{
		UpdateHealthBar(currentHealth, maxHealth, delta);

		if (delta < 0)
		{
			ShakeUI();
		}
	}

	private void OnChannelHealthChanged(float delta, float currentHealth, float maxHealth)
	{
		UpdateHealthBar(currentHealth, maxHealth, delta);

		if (delta < 0)
		{
			ShakeUI();
		}
	}

	private void OnDeath()
	{
		gameObject.SetActive(false);
	}

	private void UpdateHealthBar(float currentHealth, float maxHealth)
	{
		UpdateHealthBar(currentHealth, maxHealth, 0f);
	}

	private void UpdateHealthBar(float currentHealth, float maxHealth, float delta)
	{
		if (maxHealth > 0)
		{
			float normalizedHealth = currentHealth / maxHealth;
			float targetFill = Mathf.Lerp(_visibleFillRange.x, _visibleFillRange.y, normalizedHealth);

			if (_healthDataType == HealthDataType.Image)
			{
				_healthImage.fillAmount = targetFill;
			}
			else if (_healthDataType == HealthDataType.Shader)
			{
				_healthBarRenderer.material.SetFloat(_healthPropertyName, targetFill);
			}

			FadeOutHealth(delta, targetFill);
		}
	}

	private void FadeOutHealth(float delta, float targetFill)
	{
		if (!_enableFadeOutHealth)
		{
			return;
		}

		// Don't fade out health if the target fill is higher than the current temp health
		if (targetFill > _tempHealth)
		{
			_fadeHealthSequence.Stop();
			_tempHealth = targetFill;
			if (_healthDataType == HealthDataType.Image)
			{
				_tempHealthImage.fillAmount = targetFill;
			}
			else
			{
				_healthBarRenderer.material.SetFloat(_tempHealthPropertyName, targetFill);
			}
		}
		else
		{
			_fadeHealthSequence.Stop();

			if (_healthDataType == HealthDataType.Image)
			{
				_fadeHealthSequence = Sequence
					.Create()
					.Group(
						Tween.UIFillAmount(
							_tempHealthImage,
							_tempHealth,
							targetFill,
							_fadeDuration,
							startDelay: delta > 0 ? 0f : _fadeDelay // No delay for healing
						)
					)
					.Group(
						Tween.Custom(
							this,
							_tempHealth,
							targetFill,
							_fadeDuration,
							(target, val) => target._tempHealth = val,
							startDelay: delta > 0 ? 0f : _fadeDelay
						)
					);
			}
			else if (_healthDataType == HealthDataType.Shader)
			{
				// Initalize Values
				_healthBarRenderer.material.SetFloat(_tempHealthFadeOutPropertyName, 0);
				_healthBarRenderer.material.SetFloat(_tempHealthPropertyName, _tempHealth);

				// Tween
				_fadeHealthSequence = Sequence
					.Create()
					.Group(
						Tween.MaterialProperty(
							_healthBarRenderer.material,
							_tempHealthPropertyName,
							_tempHealth,
							targetFill,
							_fadeDuration,
							startDelay: delta > 0 ? 0f : _fadeDelay // No delay for healing
						)
					)
					.Group(
						Tween.MaterialProperty(
							_healthBarRenderer.material,
							_tempHealthFadeOutPropertyName,
							0,
							1,
							_fadeDuration,
							startDelay: delta > 0 ? 0f : _fadeDelay
						)
					)
					.Group(
						Tween.Custom(
							this,
							_tempHealth,
							targetFill,
							_fadeDuration,
							(target, val) => target._tempHealth = val,
							startDelay: delta > 0 ? 0f : _fadeDelay
						)
					);
			}
		}
	}

	private void ShakeUI()
	{
		if (!_enableShakeOnDamage)
		{
			return;
		}

		if (!_healthBarTransform)
		{
			Debug.LogWarning("Shake is enabled but no rect transform assigned");
			return;
		}

		_shakeTween.Stop();

		_shakeTween = Tween.ShakeLocalPosition(
			_healthBarTransform,
			_shakeStrength,
			_shakeDuration,
			_shakeFrequency,
			easeBetweenShakes: _shakeEase
		);
	}

	#endregion
}
