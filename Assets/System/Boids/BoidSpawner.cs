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
	private float _flockBoundsRadius = 30f;

	[SerializeField]
	[Tooltip(
		"The normalized ratio of the flock bounds radius at which boids begin softly steering back toward the center."
	)]
	[Range(0f, 1f)]
	private float _flockBoundsInnerRatio = 0.85f;

	[Header("Debug")]
	[SerializeField]
	private bool _enableOnStart;

	private readonly List<BoidAgent> _spawnBoids = new();
	public Dictionary<int, BoidAgent> BoidColliderAgentMap = new();
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
		BoidColliderAgentMap.Clear();

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
		boidAgent.ConfigureBounds(transform.position, _flockBoundsRadius, _flockBoundsInnerRatio * _flockBoundsRadius);
		boidAgent.ConfigureSpeed(_baseSpeed + Random.Range(-_speedVariance, _speedVariance));

		// Add to Dictionary
		Rigidbody body = boidGameObject.GetComponent<Rigidbody>();
		BoidColliderAgentMap[body.GetInstanceID()] = boidAgent;

		return boidAgent;
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
		Gizmos.DrawWireSphere(transform.position, _flockBoundsRadius);

		Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
		Gizmos.DrawWireSphere(transform.position, _flockBoundsRadius * _flockBoundsInnerRatio);
	}

	#endregion
}
