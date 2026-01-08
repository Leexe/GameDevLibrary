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

	#endregion

	#region Settings

	[FoldoutGroup("References")]
	[SerializeField]
	private RectTransform _rectTransform;

	[FoldoutGroup("References")]
	[SerializeField]
	private Image _healthImage;

	[FoldoutGroup("References")]
	[SerializeField]
	private Image _tempHealthImage;

	[FoldoutGroup("Data Source")]
	[Tooltip("Use to toggle event channel mode and off for direct reference")]
	[SerializeField]
	private ReferenceType _referenceType = ReferenceType.EventChannel;

	[FoldoutGroup("Data Source")]
	[SerializeField]
	[ShowIf("@_referenceType == ReferenceType.EventChannel")]
	[Indent]
	private HealthEventChannelSO _channel;

	[FoldoutGroup("Data Source")]
	[SerializeField]
	[ShowIf("@_referenceType == ReferenceType.DirectReference")]
	[Indent]
	private HealthController _directController;

	[FoldoutGroup("Fade Out Health")]
	[SerializeField]
	[ToggleLeft]
	private bool _enableFadeOutHealth = true;

	[FoldoutGroup("Fade Out Health")]
	[SerializeField]
	[ShowIf("_enableFadeOutHealth")]
	[Indent]
	[Tooltip("How long the fade out takes")]
	[SuffixLabel("seconds")]
	private float _fadeDuration = 0.5f;

	[FoldoutGroup("Fade Out Health")]
	[SerializeField]
	[ShowIf("_enableFadeOutHealth")]
	[Indent]
	[Tooltip("How long it takes after damage for the fade out to start")]
	[SuffixLabel("seconds")]
	private float _fadeDelay = 1f;

	[FoldoutGroup("Shake Effect")]
	[SerializeField]
	[ToggleLeft]
	private bool _enableShakeOnDamage = true;

	[FoldoutGroup("Shake Effect")]
	[SerializeField]
	[ShowIf("_enableShakeOnDamage")]
	[Indent]
	[Tooltip("How far the UI shakes from its original position")]
	private Vector3 _shakeStrength = new(5f, 5f, 0f);

	[FoldoutGroup("Shake Effect")]
	[SerializeField]
	[ShowIf("_enableShakeOnDamage")]
	[Indent]
	[Tooltip("How long the shake lasts")]
	[SuffixLabel("seconds")]
	private float _shakeDuration = 0.3f;

	[FoldoutGroup("Shake Effect")]
	[SerializeField]
	[ShowIf("_enableShakeOnDamage")]
	[Indent]
	[Tooltip("How many times the UI oscillates during the shake")]
	private int _shakeFrequency = 10;

	[FoldoutGroup("Shake Effect")]
	[SerializeField]
	[ShowIf("_enableShakeOnDamage")]
	[Indent]
	[Tooltip("Easing for the shake")]
	private Ease _shakeEase = Ease.OutQuad;

	[FoldoutGroup("Misc")]
	[SerializeField]
	[MinMaxSlider(0f, 1f, true)]
	[Tooltip("The range of the health bar's fill amount that is visible")]
	private Vector2 _visibleFillRange = new Vector2(0f, 1f);

	#endregion

	#region State

	private Tween _shakeTween;
	private Tween _fadeHealthTween;

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

	private void OnEnable()
	{
		if (_channel != null)
		{
			_channel.OnHealthChanged += OnChannelHealthChanged;
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
			_directController = null;
		}
	}

	/// <summary>
	/// Initialize the health bar with a direct reference to a HealthController.
	/// </summary>
	public void Initialize(HealthController controller)
	{
		// Clean up previous subscription if any
		if (_directController != null)
		{
			_directController.OnHealthChanged.RemoveListener(OnHealthChanged);
			_directController.OnDeath.RemoveListener(OnDeath);
		}

		_directController = controller;

		if (_directController != null)
		{
			_directController.OnHealthChanged.AddListener(OnHealthChanged);
			_directController.OnDeath.AddListener(OnDeath);

			UpdateHealthBar(0f, _directController.Health, _directController.MaxHealth);
		}
	}

	#endregion

	#region Event Listeners

	private void OnHealthChanged(float delta, float currentHealth, float maxHealth)
	{
		UpdateHealthBar(delta, currentHealth, maxHealth);

		if (delta < 0)
		{
			ShakeUI();
		}
	}

	private void OnChannelHealthChanged(float delta, float currentHealth, float maxHealth)
	{
		UpdateHealthBar(delta, currentHealth, maxHealth);

		if (delta < 0)
		{
			ShakeUI();
		}
	}

	private void OnDeath()
	{
		gameObject.SetActive(false);
	}

	private void UpdateHealthBar(float delta, float currentHealth, float maxHealth)
	{
		if (maxHealth > 0)
		{
			float normalizedHealth = currentHealth / maxHealth;
			float targetFill = Mathf.Lerp(_visibleFillRange.x, _visibleFillRange.y, normalizedHealth);

			_healthImage.fillAmount = targetFill;
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
		if (targetFill > _tempHealthImage.fillAmount)
		{
			_fadeHealthTween.Stop();
			_tempHealthImage.fillAmount = targetFill;
		}
		else
		{
			_fadeHealthTween.Stop();

			_fadeHealthTween = Tween.Custom(
				target: this,
				startValue: _tempHealthImage.fillAmount,
				endValue: targetFill,
				duration: _fadeDuration,
				onValueChange: (target, val) => target._tempHealthImage.fillAmount = val,
				startDelay: delta > 0 ? 0f : _fadeDelay // No delay for healing
			);
		}
	}

	private void ShakeUI()
	{
		if (!_enableShakeOnDamage)
		{
			return;
		}
		else if (!_rectTransform)
		{
			Debug.LogWarning("Shake is enabled but no rect transform assigned");
			return;
		}

		_shakeTween.Stop();

		_shakeTween = Tween.ShakeLocalPosition(
			_rectTransform,
			_shakeStrength,
			_shakeDuration,
			_shakeFrequency,
			easeBetweenShakes: _shakeEase
		);
	}

	#endregion
}
