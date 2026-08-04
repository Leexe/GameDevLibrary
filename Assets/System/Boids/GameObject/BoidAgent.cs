using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class BoidAgent : MonoBehaviour
{
	#region Fields

	[Title("Movement")]
	[SerializeField]
	[Min(0.1f)]
	private float _baseSpeed = 7f;

	[SerializeField]
	private float _speedVariance = 1.5f;

	[SerializeField]
	[Min(0.1f)]
	private float _accelerationForce = 18f;

	[SerializeField]
	[Min(0.1f)]
	private float _rotationalSharpness = 10f;

	[SerializeField]
	[Min(0f)]
	private float _bankMultiplier = 1.5f;

	[Title("Behavior Distances")]
	[SerializeField]
	[Min(0.1f)]
	private float _seperationDistance = 1.5f;

	[SerializeField]
	[Min(0.1f)]
	private float _alignmentDistance = 3.5f;

	[SerializeField]
	[Min(0.1f)]
	private float _cohesionDistance = 4.5f;

	[Title("Behavior Weights")]
	[SerializeField]
	[Min(0f)]
	private float _seperationWeight = 1.6f;

	[SerializeField]
	[Min(0f)]
	private float _alignmentWeight = 1f;

	[SerializeField]
	[Min(0f)]
	private float _cohesionWeight = 1.2f;

	[SerializeField]
	[Min(0f)]
	private float _boundsWeight = 2.5f;

	[SerializeField]
	[Min(0f)]
	private float _obstacleWeight = 3f;

	[Title("Obstacle Avoidance")]
	[SerializeField]
	private bool _avoidObstacles = true;

	[SerializeField]
	private LayerMask _obstacleMask = ~0;

	[SerializeField]
	[Min(0.1f)]
	private float _obstacleProbeRadius = 0.45f;

	[SerializeField]
	[Min(0.5f)]
	private float _obstacleLookAhead = 3.5f;

	private Rigidbody _rb;
	private readonly List<BoidAgent> _neighbors = new(32);

	private Vector3 _boundsCenter;
	private BoidSpawner _spawner;
	private Vector3 _boundsSize = new(50f, 50f, 50f);
	private float _boundsInnerRatio = 0.85f;
	private bool _useBounds = true;

	private float _seperationDistanceSqr;
	private float _alignmentDistanceSqr;
	private float _cohesionDistanceSqr;
	private float _neighborScanRadius;
	private float _currentMaxSpeed;

	#endregion

	#region Methods

	public Vector3 Velocity => _rb ? _rb.linearVelocity : Vector3.zero;
	public Vector3 Position { get; private set; }
	public Vector3 Forward { get; private set; }

	protected void Awake()
	{
		_rb = GetComponent<Rigidbody>();

		_seperationDistanceSqr = _seperationDistance * _seperationDistance;
		_alignmentDistanceSqr = _alignmentDistance * _alignmentDistance;
		_cohesionDistanceSqr = _cohesionDistance * _cohesionDistance;
		_neighborScanRadius = Mathf.Max(_seperationDistance, Mathf.Max(_alignmentDistance, _cohesionDistance));
	}

	protected void Start()
	{
		Position = transform.position;
		Forward = transform.forward;
		
		_currentMaxSpeed = Mathf.Max(0.1f, _baseSpeed + Random.Range(-_speedVariance, _speedVariance));
		
		if (_rb.linearVelocity.sqrMagnitude < 0.01f)
		{
			_rb.linearVelocity = Random.onUnitSphere * _currentMaxSpeed;
		}
	}

	protected void FixedUpdate()
	{
		Position = transform.position;
		Forward = transform.forward;

		FindNeighbors();

		Vector3 steering =
			ComputeSocialSteering()
			+ (ComputeBoundsSteer() * _boundsWeight)
			+ (ComputeObstacleAvoidance() * _obstacleWeight);

		if (steering.magnitude < 0.0001f)
		{
			steering = Forward;
		}

		Vector3 acceleration = steering.normalized * _accelerationForce;
		Vector3 nextVelocity = _rb.linearVelocity + (acceleration * Time.fixedDeltaTime);
		_rb.linearVelocity = Vector3.ClampMagnitude(nextVelocity, _currentMaxSpeed);
		if (_rb.linearVelocity.sqrMagnitude > 0.1f)
		{
			Vector3 velocityDir = _rb.linearVelocity.normalized;
			Quaternion targetRotation = Quaternion.LookRotation(velocityDir, Vector3.up);

			Vector3 localAcceleration = transform.InverseTransformDirection(acceleration);
			float rollAngle = -localAcceleration.x * _bankMultiplier;

			targetRotation *= Quaternion.Euler(0, 0, rollAngle);

			transform.rotation = Quaternion.Slerp(
				transform.rotation,
				targetRotation,
				Time.fixedDeltaTime * _rotationalSharpness
			);
		}
	}

	protected void FindNeighbors()
	{
		_neighbors.Clear();
		_spawner.GetNeighbors(this, _neighborScanRadius, _neighbors);
	}

	protected Vector3 ComputeSocialSteering()
	{
		Vector3 separationForce = Vector3.zero;
		Vector3 averageVelocity = Vector3.zero;
		Vector3 center = Vector3.zero;

		int separationCount = 0;
		int alignmentCount = 0;
		int cohesionCount = 0;

		Vector3 position = Position;

		for (int i = 0; i < _neighbors.Count; i++)
		{
			BoidAgent neighbor = _neighbors[i];
			Vector3 otherPosition = neighbor.Position;
			Vector3 toOther = position - otherPosition;
			float sqrDistance = toOther.sqrMagnitude;

			// Separation
			if (sqrDistance > 0.0001f && sqrDistance <= _seperationDistanceSqr)
			{
				separationForce += toOther / sqrDistance;
				separationCount++;
			}

			// Alignment
			if (sqrDistance <= _alignmentDistanceSqr)
			{
				averageVelocity += neighbor.Velocity;
				alignmentCount++;
			}

			// Cohesion
			if (sqrDistance <= _cohesionDistanceSqr)
			{
				center += otherPosition;
				cohesionCount++;
			}
		}

		Vector3 finalSteering = Vector3.zero;

		if (separationCount > 0)
		{
			finalSteering += separationForce / separationCount * _seperationWeight;
		}

		if (alignmentCount > 0)
		{
			finalSteering += (averageVelocity / alignmentCount).normalized * _alignmentWeight;
		}
		else
		{
			finalSteering += Forward * _alignmentWeight;
		}

		if (cohesionCount > 0)
		{
			finalSteering += ((center / cohesionCount) - position).normalized * _cohesionWeight;
		}

		return finalSteering;
	}

	protected Vector3 ComputeBoundsSteer()
	{
		if (!_useBounds)
		{
			return Vector3.zero;
		}

		Vector3 offset = Position - _boundsCenter;
		Vector3 extents = _boundsSize * 0.5f;
		Vector3 innerExtents = extents * _boundsInnerRatio;

		Vector3 steer = Vector3.zero;

		if (Mathf.Abs(offset.x) > innerExtents.x)
		{
			float strength = Mathf.InverseLerp(innerExtents.x, extents.x, Mathf.Abs(offset.x));
			steer.x = -Mathf.Sign(offset.x) * strength;
		}

		if (Mathf.Abs(offset.y) > innerExtents.y)
		{
			float strength = Mathf.InverseLerp(innerExtents.y, extents.y, Mathf.Abs(offset.y));
			steer.y = -Mathf.Sign(offset.y) * strength;
		}

		if (Mathf.Abs(offset.z) > innerExtents.z)
		{
			float strength = Mathf.InverseLerp(innerExtents.z, extents.z, Mathf.Abs(offset.z));
			steer.z = -Mathf.Sign(offset.z) * strength;
		}

		return steer;
	}

	protected Vector3 ComputeObstacleAvoidance()
	{
		if (!_avoidObstacles)
		{
			return Vector3.zero;
		}

		Vector3 velocity = _rb.linearVelocity;
		if (velocity.sqrMagnitude < 0.0001f)
		{
			return Vector3.zero;
		}

		Vector3 direction = velocity.normalized;
		Vector3 position = Position;
		Vector3 avoidance = Vector3.zero;

		// Look Avoidance
		if (
			Physics.SphereCast(
				position,
				_obstacleProbeRadius,
				direction,
				out RaycastHit lookHit,
				_obstacleLookAhead,
				_obstacleMask,
				QueryTriggerInteraction.Ignore
			)
		)
		{
			Vector3 awayFromHit = Vector3.Reflect(direction, lookHit.normal).normalized;
			avoidance += awayFromHit;
		}

		return avoidance.normalized;
	}

	public void ConfigureBounds(Vector3 boundsCenter, Vector3 size, float innerRatio, bool enabled = true)
	{
		_boundsCenter = boundsCenter;
		_boundsSize = size;
		_boundsInnerRatio = innerRatio;
		_useBounds = enabled;
	}

	public void ConfigureBoidSpawnerReference(BoidSpawner spawner)
	{
		_spawner = spawner;
	}

	#endregion
}
