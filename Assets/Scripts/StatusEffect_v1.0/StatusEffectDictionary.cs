using System.Collections.Generic;
using UnityEngine;

namespace StatusEffects
{
	[CreateAssetMenu(fileName = "StatusEffectDictionary", menuName = "StatusEffect/StatusEffectDictionary")]
	public class StatusEffectDictionary : ScriptableObject
	{
		[Tooltip("List of all status effects in the game.")]
		[SerializeField]
		private List<StatusEffectSO> _statusEffects;

		private Dictionary<Effect, StatusEffectSO> _effectDictionary;

		public Dictionary<Effect, StatusEffectSO> EffectDictionary
		{
			get
			{
				if (_effectDictionary == null)
				{
					_effectDictionary = new Dictionary<Effect, StatusEffectSO>();
					foreach (StatusEffectSO effect in _statusEffects)
					{
						if (effect != null && !_effectDictionary.ContainsKey(effect.Type))
						{
							_effectDictionary.Add(effect.Type, effect);
						}
					}
				}

				return _effectDictionary;
			}
		}
	}
}
