using System.Collections.Generic;
using FMODUnity;
using Sirenix.OdinInspector;
using Stats;
using UnityEngine;
using UnityEngine.VFX;

namespace StatusEffects
{
	[CreateAssetMenu(fileName = "StatusEffectSO", menuName = "StatusEffects/StatusEffectSO", order = 0)]
	public class StatusEffectSO : SerializedScriptableObject
	{
		[field: TabGroup("General")]
		[field: Required]
		[field: Tooltip("Unique Identifier")]
		[field: SerializeField]
		public string Id { get; private set; }

		[field: TabGroup("General")]
		[field: SerializeField]
		[field: Tooltip("Display Name")]
		public string Name { get; private set; }

		[field: TabGroup("General")]
		[field: SerializeField]
		[field: Tooltip("Display Description")]
		[field: TextArea]
		public string Description { get; private set; }

		[field: TabGroup("General")]
		[field: SerializeField]
		[field: Tooltip("UI Prefab Representing this effect")]
		public GameObject IconPrefab { get; private set; }

		[field: Title("Duration")]
		[field: TabGroup("Timings")]
		[field: Tooltip("Should the status effect have infinite duration")]
		[field: SerializeField]
		public bool InfiniteDuration { get; private set; } = false;

		[field: TabGroup("Timings")]
		[field: Tooltip("How long the effect lasts when it is activated")]
		[field: Min(0)]
		[field: HideIf("InfiniteDuration")]
		[field: SerializeField]
		public float ActiveDuration { get; private set; } = 1f;

		[field: TabGroup("Timings")]
		[field: HideIf("InfiniteDuration")]
		[field: Tooltip("Can the active duration be refreshed on repeated hits")]
		[field: SerializeField]
		public bool RefreshableDuration { get; private set; }

		[field: TabGroup("Timings")]
		[field: Title("Ticks")]
		[field: Tooltip("Whether or not to have a tick rate for certain effects")]
		[field: SerializeField]
		public bool EnableTickRate { get; private set; } = true;

		[field: TabGroup("Timings")]
		[field: Tooltip("At what interval to proc a certain effect")]
		[field: ShowIf("EnableTickRate")]
		[field: SerializeField]
		public float TickRate { get; private set; } = 0.2f;

		[field: TabGroup("Build Up")]
		[field: Title("Count")]
		[field: Min(1)]
		[field: Tooltip("How much of the effect can be active at once")]
		[field: SerializeField]
		public int MaxCount { get; private set; } = 1;

		[field: TabGroup("Build Up")]
		[field: Title("Build Up")]
		[field: Tooltip("How much of the build up is decreasing per second")]
		[field: Range(0, 3)]
		[field: SerializeField]
		public float BuildUpDecay { get; private set; } = 0.2f;

		[field: TabGroup("Build Up")]
		[field: Min(0)]
		[field: Tooltip("How long the build up bar should stay where it is after build up being applied")]
		[field: SerializeField]
		public float BuildUpFreeze { get; private set; } = 0.35f;

		[field: TabGroup("Effects")]
		[field: Title("Damage")]
		[field: Tooltip("Damage done when the effect is activated")]
		[field: SerializeField]
		public float DamageOnActivation { get; private set; }

		[field: TabGroup("Effects")]
		[field: Tooltip("Damage done when the effect is reactivated")]
		[field: SerializeField]
		public float DamageOnReactivation { get; private set; }

		[field: TabGroup("Effects")]
		[field: Tooltip("Affects done per tick when the effect is active")]
		[field: SerializeField]
		public float DamageOnTick { get; private set; }

		[field: TabGroup("Effects")]
		[field: Tooltip("Damage done when the effect expires")]
		[field: SerializeField]
		public float DamageOnExpire { get; private set; }

		[field: TabGroup("Effects")]
		[field: Tooltip("Damage done per hit when the effect is active")]
		[field: SerializeField]
		public float DamageOnHit { get; private set; }

		[field: TabGroup("Effects")]
		[field: Tooltip(
			"How much damage is multiplied for every stack on the enemy (1.1 = 10% damage increase per stack)"
		)]
		[field: ShowIf("@MaxCount > 1")]
		[field: SerializeField]
		public float StackDamageMult { get; private set; } = 1f;

		[field: TabGroup("Effects")]
		[field: Title("Modifiers")]
		[field: Tooltip("Modifiers applied when the effect is first activated")]
		[field: SerializeField]
		public List<Modifier> Modifiers { get; private set; }

		[field: TabGroup("Effects")]
		[field: Tooltip("Modifiers applied when the effect is reactivated, on applied on first activation")]
		[field: SerializeField]
		public List<Modifier> RepeatedModifiers { get; private set; }

		[field: TabGroup("Effects")]
		[field: Title("Combos")]
		[field: Tooltip(
			"The key is the effect that combines with the current effect to produce the value status effect"
		)]
		[field: SerializeField]
		public Dictionary<StatusEffectSO, StatusEffectSO> PotentialCombosDict { get; private set; } = new();

		[field: TabGroup("Visuals")]
		[field: Title("Visual Effects")]
		[field: Tooltip("Determines how long the status end visual effect lasts")]
		[field: SerializeField]
		public float VfxEndDelay { get; private set; }

		[field: TabGroup("Visuals")]
		[field: Tooltip("The visual effect to play when the status effect is first activated")]
		[field: SerializeField]
		public VisualEffectAsset StatusTriggeredVfx { get; private set; }

		[field: TabGroup("Visuals")]
		[field: Tooltip("The visual effect to play when the status effect is reactivated")]
		[field: SerializeField]
		public VisualEffectAsset StatusReactivateVfx { get; private set; }

		[field: TabGroup("Visuals")]
		[field: Tooltip("The visual effect to play continuously while the status effect is active")]
		[field: SerializeField]
		public VisualEffectAsset StatusActivateVfx { get; private set; }

		[field: TabGroup("Visuals")]
		[field: Tooltip("The visual effect to play each time the status effect ticks (e.g. Damage over Time)")]
		[field: SerializeField]
		public VisualEffectAsset StatusDotVfx { get; private set; }

		[field: TabGroup("Visuals")]
		[field: Tooltip("The visual effect to play when the status effect ends or is cancelled")]
		[field: SerializeField]
		public VisualEffectAsset StatusEndVfx { get; private set; }

		[field: TabGroup("Sfx")]
		[field: Title("Sounds")]
		[field: Tooltip("The sound to play when the status effect is first activated")]
		[field: SerializeField]
		public EventReference StatusTriggeredSfx { get; private set; }

		[field: TabGroup("Sfx")]
		[field: Tooltip("The sound to plays when the status effect reactivates")]
		[field: SerializeField]
		public EventReference StatusReactivateSfx { get; private set; }

		[field: TabGroup("Sfx")]
		[field: Tooltip("The sound to plays continuously when the status effect is active")]
		[field: SerializeField]
		public EventReference StatusActiveSfx { get; private set; }

		[field: TabGroup("Sfx")]
		[field: Tooltip("The sound to play when the status effect is ticked")]
		[field: SerializeField]
		public EventReference StatusDotSfx { get; private set; }

		[field: TabGroup("Sfx")]
		[field: Tooltip("The sound to play when the status effect expires")]
		[field: SerializeField]
		public EventReference StatusEndSfx { get; private set; }

		/// <summary>
		/// Checks if this status effect can combine with the given effect to produce a combo result
		/// </summary>
		/// <param name="statusEffect">The other effect to check for a combo with</param>
		/// <returns>True if a valid combo exists between this effect and the given effect</returns>
		public bool CheckCombo(StatusEffectSO statusEffect)
		{
			return PotentialCombosDict.ContainsKey(statusEffect) && PotentialCombosDict[statusEffect] != null;
		}

		/// <summary>
		/// Returns the resulting status effect from combining this effect with the given effect
		/// </summary>
		/// <param name="statusEffectTarget">The other effect to combine with</param>
		/// <returns>The combo result, or null if no combo exists</returns>
		public StatusEffectSO GetCombo(StatusEffectSO statusEffectTarget)
		{
			return PotentialCombosDict.GetValueOrDefault(statusEffectTarget, null);
		}

		/// <summary>
		/// Creates a runtime instance of this status effect
		/// </summary>
		public virtual StatusEffect CreateInstance()
		{
			return new StatusEffect(this);
		}
	}
}
