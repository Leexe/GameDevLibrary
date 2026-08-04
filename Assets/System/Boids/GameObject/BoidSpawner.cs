using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class BoidSpawner : MonoBehaviour
{
	#region Fields

	[Header("Prefabs")]
	[SerializeField]
	private GameObject _boidPrefab;

	[Header("Spawn")]
	[SerializeField]
	[Min(1)]
	private int _boidCount = 60;

	[SerializeField]
	private Vector3 _spawnBoundaries = new(14f, 6f, 14f);

	[SerializeField]
	[Min(0.1f)]
	private float _boidScale = 0.5f;

	[Header("Movement")]
	[SerializeField]
	private float _baseSpeed = 7f;

	[SerializeField]
	private float _speedVariance = 1.5f;

	[SerializeField]
	private Vector3 _flockBoundsSize = new(60f, 30f, 60f);

	[SerializeField]
	[Tooltip(
		"The normalized ratio of the flock bounds size at which boids begin softly steering back toward the center."
	)]
	[Range(0f, 1f)]
	private float _flockBoundsInnerRatio = 0.85f;

	[Header("Grid")]
	[SerializeField]
	private float _cellSize = 4.5f;

	[Header("Debug")]
	[SerializeField]
	private bool _enableOnStart;

	private readonly List<BoidAgent> _spawnBoids = new();
	private readonly Dictionary<Vector3Int, List<BoidAgent>> _spatialGrid = new();
	private Transform _runtimeRoot;

	#endregion

	#region Methods

	private void Start()
	{
		if (_enableOnStart)
		{
			SpawnFlock();
		}
	}

	private void FixedUpdate()
	{
		foreach (List<BoidAgent> cell in _spatialGrid.Values)
		{
			cell.Clear();
		}

		for (int i = 0; i < _spawnBoids.Count; i++)
		{
			BoidAgent boid = _spawnBoids[i];

			Vector3 pos = boid.Position;
			var cellCoord = new Vector3Int(
				Mathf.FloorToInt(pos.x / _cellSize),
				Mathf.FloorToInt(pos.y / _cellSize),
				Mathf.FloorToInt(pos.z / _cellSize)
			);

			if (!_spatialGrid.TryGetValue(cellCoord, out List<BoidAgent> cellList))
			{
				cellList = new List<BoidAgent>();
				_spatialGrid[cellCoord] = cellList;
			}

			cellList.Add(boid);
		}
	}

	[Button]
	public void SpawnFlock()
	{
		ClearFlock();
		EnsureRuntimeRoot();

		for (int i = 0; i < _boidCount; i++)
		{
			BoidAgent boid = CreateBoid(i);
			_spawnBoids.Add(boid);
		}
	}

	[Button]
	public void ClearFlock()
	{
		for (int i = _spawnBoids.Count - 1; i >= 0; i--)
		{
			BoidAgent boid = _spawnBoids[i];
			if (boid)
			{
				Destroy(boid.gameObject);
			}
		}

		_spawnBoids.Clear();
		_spatialGrid.Clear();

		if (!_runtimeRoot)
		{
			return;
		}

		Destroy(_runtimeRoot.gameObject);

		_runtimeRoot = null;
	}

	private BoidAgent CreateBoid(int index)
	{
		// Spawn Boid
		Vector3 spawnPosition = transform.position + GetRandomSpawnOffset();
		GameObject boidGameObject = Instantiate(_boidPrefab, spawnPosition, Quaternion.identity, _runtimeRoot);
		boidGameObject.name = $"Boid_{index:000}";
		boidGameObject.transform.SetParent(_runtimeRoot.transform, false);
		boidGameObject.transform.localScale = Vector3.one * _boidScale;

		// Get Boid Agent
		BoidAgent boidAgent = boidGameObject.GetComponent<BoidAgent>();
		boidAgent.ConfigureBoidSpawnerReference(this);
		boidAgent.ConfigureBounds(transform.position, _flockBoundsSize, _flockBoundsInnerRatio);
		boidAgent.ConfigureSpeed(_baseSpeed + Random.Range(-_speedVariance, _speedVariance));

		return boidAgent;
	}

	public void GetNeighbors(BoidAgent agent, float scanRadius, List<BoidAgent> results)
	{
		results.Clear();
		float scanRadiusSqr = scanRadius * scanRadius;
		Vector3 pos = agent.Position;

		var centerCell = new Vector3Int(
			Mathf.FloorToInt(pos.x / _cellSize),
			Mathf.FloorToInt(pos.y / _cellSize),
			Mathf.FloorToInt(pos.z / _cellSize)
		);

		for (int x = -1; x <= 1; x++)
		{
			for (int y = -1; y <= 1; y++)
			{
				for (int z = -1; z <= 1; z++)
				{
					Vector3Int neighborCell = centerCell + new Vector3Int(x, y, z);

					if (_spatialGrid.TryGetValue(neighborCell, out List<BoidAgent> cellBoids))
					{
						for (int i = 0; i < cellBoids.Count; i++)
						{
							BoidAgent otherBoid = cellBoids[i];
							if (otherBoid == agent)
							{
								continue;
							}

							if ((otherBoid.Position - pos).sqrMagnitude <= scanRadiusSqr)
							{
								results.Add(otherBoid);
							}
						}
					}
				}
			}
		}
	}

	private Vector3 GetRandomSpawnOffset()
	{
		return new Vector3(
			Random.Range(-_spawnBoundaries.x, _spawnBoundaries.x),
			Random.Range(-_spawnBoundaries.y, _spawnBoundaries.y),
			Random.Range(-_spawnBoundaries.z, _spawnBoundaries.z)
		);
	}

	private void EnsureRuntimeRoot()
	{
		if (_runtimeRoot)
		{
			return;
		}

		var root = new GameObject("BoidsRuntime");
		root.transform.SetParent(transform, false);
		_runtimeRoot = root.transform;
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
		Gizmos.DrawWireCube(transform.position, _spawnBoundaries * 2f);

		Gizmos.color = new Color(0f, 0f, 1f, 0.5f);
		Gizmos.DrawWireCube(transform.position, _flockBoundsSize);

		Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
		Gizmos.DrawWireCube(transform.position, _flockBoundsSize * _flockBoundsInnerRatio);
	}

	#endregion
}
