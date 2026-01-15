using UnityEngine;
using UnityEngine.Events;

public class StaminaSystem : MonoBehaviour
{
	[Header("Stamina Data")]
	[SerializeField]
	private int _maxStaminaCharges = 3;

	[SerializeField]
	private float _staminaBaseRechargeRate = 2f;

	[HideInInspector]
	public UnityEvent<float> OnStaminaRecharging; // normalizedStamina

	[HideInInspector]
	public UnityEvent OnStaminaRecharged;

	private float _rechargeTimer;

	private float NormalizedStamina =>
		(GetStaminaCharges + (_rechargeTimer / _staminaBaseRechargeRate)) / _maxStaminaCharges;

	// Getters

	public float GetMaxStamina => _maxStaminaCharges;

	public int GetStaminaCharges { get; private set; }

	private void Start()
	{
		GetStaminaCharges = _maxStaminaCharges;
	}

	private void Update()
	{
		if (GetStaminaCharges < _maxStaminaCharges)
		{
			if (_rechargeTimer >= _staminaBaseRechargeRate)
			{
				GetStaminaCharges++;
				_rechargeTimer = 0f;
				OnStaminaRecharged?.Invoke();
			}

			_rechargeTimer += Time.deltaTime;
			OnStaminaRecharging?.Invoke(NormalizedStamina);
		}
		else
		{
			_rechargeTimer = 0f;
		}
	}

	private void OnEnable()
	{
		OnStaminaRecharging.AddListener(TriggerStaminaRechargeEvent);
	}

	private void OnDisable()
	{
		OnStaminaRecharging.RemoveListener(TriggerStaminaRechargeEvent);
	}

	// Adds stamina chrages
	public void AddStaminaCharges(int charges)
	{
		GetStaminaCharges += charges;
		if (GetStaminaCharges > _maxStaminaCharges)
		{
			GetStaminaCharges = _maxStaminaCharges;
		}

		OnStaminaRecharging?.Invoke(NormalizedStamina);
	}

	// Returns a boolean indicating if the stamina charge can be consumed, does not consume stamina charages
	public bool CanConsumeStamina()
	{
		if (GetStaminaCharges > 0)
		{
			return true;
		}

		return false;
	}

	// Returns a boolean indicating if the stamina charge can be consumed, does consume stamina charages
	public bool ConsumeStamina()
	{
		if (GetStaminaCharges > 0)
		{
			GetStaminaCharges--;
			OnStaminaRecharging?.Invoke(NormalizedStamina);
			return true;
		}

		Debug.LogWarning("Attempted to consume stamina but no stamina chrages");
		return false;
	}

	public void TriggerStaminaRechargeEvent(float normalizedStamina)
	{
		// GameManager.Instance.StaminaEventsRef.StaminaRecharging(_normalizedStamina);
	}
}
