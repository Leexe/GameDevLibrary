using System.Collections.Generic;
using FMODUnity;
using Sirenix.OdinInspector;
using StatusEffects;
using UnityEngine;

namespace StatusEffects
{
	public enum Effect
	{
		None,
		Burn,
		Poison,
		Brittle,
		Fracture,
		Doom,
		Madness,
		Darkness,
		Corruption,
	}
}

[CreateAssetMenu(fileName = "BaseStatusEffect", menuName = "StatusEffect/StatusEffectSO")]
public class StatusEffectSO : SerializedScriptableObject
{
	[field: TabGroup("Details", "General")]
	[field: Tooltip("The name of the effect")]
	[field: SerializeField]
	public Effect Type { get; private set; }

	[TabGroup("Details", "General")]
	[Tooltip("How long the effect lasts when it is activated")]
	[SerializeField]
	protected float _activeDuration = 1f;

	[TabGroup("Details", "General")]
	[Tooltip("How much of the effect can be active at once")]
	[SerializeField]
	protected int _maxCount = 1;

	[TabGroup("Details", "General")]
	[Header("Repeated Hit")]
	[Tooltip("Can the active duration be refreshed on repeated hits")]
	[SerializeField]
	protected bool _refreshableDuration;

	[TabGroup("Details", "Mechanics")]
	[Title("Ticks")]
	[Tooltip("Whether or not to have a tick rate for certain effects")]
	[SerializeField]
	protected bool _enableTickRate = true;

	[TabGroup("Details", "Mechanics")]
	[Tooltip("At what interval to proc a certain effect")]
	[ShowIf("_enableTickRate")]
	[SerializeField]
	protected float _tickRate = 0.2f;

	[TabGroup("Details", "Mechanics")]
	[Title("Damage")]
	[Tooltip("How much damage to deal per tick")]
	[SerializeField]
	protected float _damageOnActivation;

	[TabGroup("Details", "Mechanics")]
	[Tooltip("How much damage to deal per tick")]
	[ShowIf("_enableTickRate")]
	[SerializeField]
	protected float _damagePerTick;

	[TabGroup("Details", "Mechanics")]
	[Tooltip("How much damage to deal per tick")]
	[SerializeField]
	protected float _damageOnExpire;

	[TabGroup("Details", "Mechanics")]
	[Title("Build Up")]
	[Tooltip("How much of the build up is decreasing per second")]
	[Range(0, 3)]
	[SerializeField]
	protected float _buildUpDecay = 0.2f;

	[TabGroup("Details", "Mechanics")]
	[Tooltip("How long the build up bar should freeze after build up being applied")]
	[SerializeField]
	protected float _buildUpFreeze = 0.35f;

	[TabGroup("Details", "Combos")]
	[Tooltip("The key is the effect that combines with the current effect to produce the value status effect")]
	[SerializeField]
	protected Dictionary<Effect, Effect> PotentialCombosDict = new();

	[TabGroup("Details", "Presentation")]
	[Title("Visuals")]
	[Tooltip("The UI prefab to spawn when this effect is active")]
	[SerializeField]
	protected GameObject _iconPrefab;

	[TabGroup("Details", "Presentation")]
	[Title("Sounds")]
	[Tooltip("The sound to play when the status effect is first triggered")]
	[SerializeField]
	protected EventReference _statusTriggeredSfx;

	[TabGroup("Details", "Presentation")]
	[Tooltip("The sound to plays continously when the status effect is active")]
	[SerializeField]
	protected EventReference _statusActiveSfx;

	[TabGroup("Details", "Presentation")]
	[Tooltip("The sound to play when the status effect is ticked")]
	[SerializeField]
	protected EventReference _statusDOTSfx;

	[TabGroup("Details", "Presentation")]
	[Tooltip("The sound to play when the status effect expires")]
	[SerializeField]
	protected EventReference _statusEndSfx;

	// Public Properties
	public GameObject IconPrefab => _iconPrefab;
	public float ActiveDuration => _activeDuration;
	public int MaxCount => _maxCount;
	public bool EnableTickRate => _enableTickRate;
	public float TickRate => _tickRate;
	public float DamageOnActivation => _damageOnActivation;
	public float DamagePerTick => _damagePerTick;
	public float DamageOnExpire => _damageOnExpire;
	public float BuildUpDecay => _buildUpDecay;
	public float BuildUpFreeze => _buildUpFreeze;
	public bool RefreshableDuration => _refreshableDuration;

	public EventReference StatusTriggeredSfx => _statusTriggeredSfx;
	public EventReference StatusActiveSfx => _statusActiveSfx;
	public EventReference StatusDOTSfx => _statusDOTSfx;
	public EventReference StatusEndSfx => _statusEndSfx;

	public bool CheckCombo(Effect statusEffect)
	{
		return PotentialCombosDict.ContainsKey(statusEffect) && PotentialCombosDict[statusEffect] != Effect.None;
	}

	public Effect GetComboResult(Effect statusEffectTarget)
	{
		return PotentialCombosDict.GetValueOrDefault(statusEffectTarget, Effect.None);
	}
}
