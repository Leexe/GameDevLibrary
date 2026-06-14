using System.Collections.Generic;
using Sirenix.OdinInspector;
using Stats;
using UnityEngine;

public class StatsController : SerializedMonoBehaviour
{
	[Tooltip("Base stats for this entity")]
	[SerializeField]
	private Dictionary<StatType, float> _baseStats = new();

	public StatsState Stats { get; private set; }

	private void Awake()
	{
		Stats = new StatsState(_baseStats);
	}
}
