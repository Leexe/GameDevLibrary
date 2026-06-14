using System.Collections.Generic;
using Sirenix.OdinInspector;
using StatusEffects;
using UnityEngine;

namespace StatusEffects
{
	public class StatusEffectIconUICache
	{
		public GameObject GameObjectRef;
		public StatusEffectIconUI StatusEffectIconUIRef;

		public StatusEffectIconUICache(GameObject gameObject, StatusEffectIconUI statusEffectIconUI)
		{
			GameObjectRef = gameObject;
			StatusEffectIconUIRef = statusEffectIconUI;
		}
	}
}

public class StatusEffectUI : SerializedMonoBehaviour
{
	[Header("References")]
	[SerializeField]
	private StatusEffectController _statusEffectManager;

	[SerializeField]
	private GameObject _gridParent;

	[SerializeField]
	private GameObject _defaultStatusEffectPrefab;

	private Dictionary<string, StatusEffectIconUICache> _statusUICached = new();

	private void Start()
	{
		// Remove all testing status effect UI's during start up
		foreach (Transform child in _gridParent.transform)
		{
			Destroy(child.gameObject);
		}
	}

	private void OnEnable()
	{
		_statusEffectManager.OnStatusEffectActivate.AddListener(UpdateStatusEffectUI_Activate);
		_statusEffectManager.OnStatusEffectApply.AddListener(UpdateStatusEffectUI_Apply);
		_statusEffectManager.OnStatusEffectUpdate.AddListener(UpdateStatusEffectUI_Update);
		_statusEffectManager.OnStatusEffectEnd.AddListener(DisableStatusEffectUI);
	}

	private void OnDisable()
	{
		_statusEffectManager?.OnStatusEffectActivate.RemoveListener(UpdateStatusEffectUI_Activate);
		_statusEffectManager?.OnStatusEffectApply.RemoveListener(UpdateStatusEffectUI_Apply);
		_statusEffectManager?.OnStatusEffectUpdate.RemoveListener(UpdateStatusEffectUI_Update);
		_statusEffectManager?.OnStatusEffectEnd.RemoveListener(DisableStatusEffectUI);
	}

	private void UpdateStatusEffectUI_Apply(StatusEffect statusEffect, float buildUpProgress)
	{
		string statusEffectId = statusEffect.Data.Id;

		// Check if the status effect is in the cached dictionary
		if (!_statusUICached.ContainsKey(statusEffectId))
		{
			GameObject statusEffectUIPrefab =
				statusEffect.Data.IconPrefab != null ? statusEffect.Data.IconPrefab : _defaultStatusEffectPrefab;
			GameObject statusEffectIconUI = Instantiate(
				statusEffectUIPrefab,
				_gridParent.transform.position,
				_gridParent.transform.rotation,
				_gridParent.transform
			);
			_statusUICached[statusEffectId] = new StatusEffectIconUICache(
				statusEffectIconUI,
				statusEffectIconUI.GetComponent<StatusEffectIconUI>()
			);
		}

		if (!statusEffect.IsActive)
		{
			_statusUICached[statusEffectId].GameObjectRef.SetActive(true);
			UpdateStatusEffectIcon(_statusUICached[statusEffectId].StatusEffectIconUIRef, statusEffect);
		}
	}

	private void UpdateStatusEffectUI_Update(StatusEffect statusEffect, float normalizedProgress)
	{
		string statusEffectId = statusEffect.Data.Id;
		UpdateStatusEffectIcon(_statusUICached[statusEffectId].StatusEffectIconUIRef, statusEffect);
	}

	private void UpdateStatusEffectUI_Activate(StatusEffect statusEffect, float normalizedProgress)
	{
		string statusEffectId = statusEffect.Data.Id;
		UpdateStatusEffectIcon(_statusUICached[statusEffectId].StatusEffectIconUIRef, statusEffect);
		_statusUICached[statusEffectId].StatusEffectIconUIRef.ActivatedStatusEffectUI();
	}

	private void DisableStatusEffectUI(StatusEffect statusEffect)
	{
		_statusUICached[statusEffect.Data.Id].GameObjectRef.SetActive(false);
	}

	private void UpdateStatusEffectIcon(StatusEffectIconUI iconRef, StatusEffect statusEffect)
	{
		iconRef.UpdateBuildUpUI(statusEffect.GetBuildUpNormalized());
		iconRef.UpdateActiveDurationUI(statusEffect.GetRemainingDurationNormalized(), statusEffect.IsActive);
		iconRef.UpdateCountText(statusEffect.Count, statusEffect.Data.MaxCount > 1);
	}
}
