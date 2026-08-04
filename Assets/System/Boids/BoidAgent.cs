using System.Collections.Generic;
using UnityEngine;

public class BoidAgent : MonoBehaviour
{
	#region Fields

	[Header("Movement")]
	[SerializeField]
	[Min(0.1f)]
	private float _maxSpeed = 7f;

	[SerializeField]
	[Min(0.1f)]
	private float _accelerationForce = 18f;

	[SerializeField]
	[Min(0.1f)]
	private float _rotationalSharpness = 10f;

	[Header("Behavior Distances")]
	[SerializeField]
	[Min(0.1f)]
	private float _seperationDistance = 1.5f;

	[SerializeField]
	[Min(0.1f)]
	private float _alignmentDistance = 3.5f;

	[SerializeField]
	[Min(0.1f)]
	private float _cohesionDistance = 4.5f;

	[Header("Behavior Weights")]
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

	[Header("Obstacle Avoidance")]
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

	[SerializeField]
	[Min(0.1f)]
	private float _floorClearance = 0.8f;

	private Rigidbody _rb;
	private readonly List<BoidAgent> _neighbors = new(32);

	private Vector3 _boundsCenter;
	private BoidSpawner _spawner;
	private float _boundsRadius = 25f;
	private float _innerBoundsRadius = 20f;
	private bool _useBounds = true;

	private float _seperationDistanceSqr;
	private float _alignmentDistanceSqr;
	private float _cohesionDistanceSqr;
	private float _neighborScanRadius;

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
		if (_rb.linearVelocity.sqrMagnitude < 0.01f)
		{
			_rb.linearVelocity = Random.onUnitSphere * _maxSpeed;
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
		_rb.linearVelocity = Vector3.ClampMagnitude(nextVelocity, _maxSpeed);
		if (_rb.linearVelocity.sqrMagnitude > 0.1f)
		{
			var lookDirection = Vector3.ProjectOnPlane(_rb.linearVelocity, Vector3.up);
			var targetRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
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
			finalSteering += (separationForce / separationCount) * _seperationWeight;
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
		float distance = offset.magnitude;

		float strength = Mathf.InverseLerp(_innerBoundsRadius, _boundsRadius, distance);
		return (_boundsCenter - Position).normalized * strength;
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

		// Floor Avoidance
		if (
			Physics.Raycast(
				position,
				Vector3.down,
				out RaycastHit floorHit,
				_floorClearance,
				_obstacleMask,
				QueryTriggerInteraction.Ignore
			)
		)
		{
			float floorStrength = Mathf.InverseLerp(_floorClearance, 0f, floorHit.distance);
			avoidance = Vector3.up * floorStrength;
		}

		return avoidance.normalized;
	}

	public void ConfigureBounds(Vector3 boundsCenter, float radius, float innerRadius, bool enabled = true)
	{
		_boundsCenter = boundsCenter;
		_boundsRadius = radius;
		_innerBoundsRadius = innerRadius;
		_useBounds = enabled;
	}

	public void ConfigureSpeed(float speed)
	{
		_maxSpeed = Mathf.Max(speed, 0.1f);
	}

	public void ConfigureBoidSpawnerReference(BoidSpawner spawner)
	{
		_spawner = spawner;
	}

	#endregion
}
