using Sirenix.OdinInspector;
using UnityEngine;

public struct BoidData
{
	public Vector3 Position;
	public Vector3 Velocity;
	public Vector3 Forward;
	public Vector3 Up;
	public float MaxSpeed;
}

public class BoidSpawnerCompute : MonoBehaviour
{
	#region Fields

	[Title("References")]
	[SerializeField]
	private ComputeShader _boidComputeShader;

	[Title("Rendering")]
	[SerializeField]
	private Mesh _boidMesh;

	[SerializeField]
	private Material _boidMaterial;

	[Title("Spawn")]
	[SerializeField]
	[Min(1)]
	private int _boidCount = 60;

	[SerializeField]
	private Vector3 _spawnBoundaries = new(14f, 6f, 14f);

	[SerializeField]
	[Min(0.1f)]
	private float _boidScale = 0.5f;

	[Title("Movement")]
	[SerializeField]
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

	[Title("Boundaries")]
	[SerializeField]
	private Vector3 _flockBoundsSize = new(60f, 30f, 60f);

	[SerializeField]
	[Tooltip(
		"The normalized ratio of the flock bounds size at which boids begin softly steering back toward the center."
	)]
	[Range(0f, 1f)]
	private float _flockBoundsInnerRatio = 0.85f;

	private ComputeBuffer _boidBuffer;
	private ComputeBuffer _argsBuffer;
	private int _kernelIndex;

	// Cached Named Properties

	private static readonly int BoidsBuffer = Shader.PropertyToID("boidsBuffer");
	private static readonly int NumBoids = Shader.PropertyToID("numBoids");
	private static readonly int SeparationDistanceSqr = Shader.PropertyToID("separationDistanceSqr");
	private static readonly int AlignmentDistanceSqr = Shader.PropertyToID("alignmentDistanceSqr");
	private static readonly int CohesionDistanceSqr = Shader.PropertyToID("cohesionDistanceSqr");
	private static readonly int SeparationWeight = Shader.PropertyToID("separationWeight");
	private static readonly int AlignmentWeight = Shader.PropertyToID("alignmentWeight");
	private static readonly int CohesionWeight = Shader.PropertyToID("cohesionWeight");
	private static readonly int DeltaTime = Shader.PropertyToID("deltaTime");
	private static readonly int AccelerationForce = Shader.PropertyToID("accelerationForce");
	private static readonly int RotationalSharpness = Shader.PropertyToID("rotationalSharpness");
	private static readonly int BoundsSize = Shader.PropertyToID("boundsSize");
	private static readonly int BoundsInnerRatio = Shader.PropertyToID("boundsInnerRatio");
	private static readonly int BoundsWeight = Shader.PropertyToID("boundsWeight");
	private static readonly int BoundsCenter = Shader.PropertyToID("boundsCenter");
	private static readonly int BoidScale = Shader.PropertyToID("_BoidScale");
	private static readonly int BankMultiplier = Shader.PropertyToID("bankMultiplier");

	#endregion

	#region Methods

	private void Start()
	{
		// Initalize Array
		var boidsArray = new BoidData[_boidCount];
		for (int i = 0; i < _boidCount; i++)
		{
			Vector3 velocity = Random.onUnitSphere;
			boidsArray[i] = new BoidData
			{
				Position = transform.position + GetRandomSpawnOffset(),
				Velocity = velocity,
				Forward = velocity,
				Up = Vector3.up,
				MaxSpeed = Mathf.Max(0.1f, _baseSpeed + Random.Range(-_speedVariance, _speedVariance)),
			};
		}

		// Create Compute Buffer
		// Float = 4 Bytes, Float3 = 12 Bytes.
		// Position(12) + Velocity(12) + Forward(12) + Up(12) + MaxSpeed(4) = 52 Bytes
		_boidBuffer = new ComputeBuffer(_boidCount, 52);

		// Send Data to GPU
		_boidBuffer.SetData(boidsArray);

		// Hook Up Static Data
		_kernelIndex = _boidComputeShader.FindKernel("UpdateBoids");

		_boidComputeShader.SetBuffer(_kernelIndex, BoidsBuffer, _boidBuffer);
		_boidComputeShader.SetInt(NumBoids, _boidCount);

		UpdateComputeShaderProperties();

		// Set Up Args Buffer
		uint[] args =
		{
			_boidMesh.GetIndexCount(0), // Indices per mesh
			(uint)_boidCount, // How many meshes to draw
			_boidMesh.GetIndexStart(0), // Where does the mesh start
			_boidMesh.GetBaseVertex(0), // Starting point of vertex array
			0,
		};
		_argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
		_argsBuffer.SetData(args);
	}

	private void Update()
	{
		_boidComputeShader.SetFloat(DeltaTime, Time.deltaTime);
		int threadGroups = Mathf.CeilToInt(_boidCount / 64f);
		_boidComputeShader.Dispatch(_kernelIndex, threadGroups, 1, 1);

		_boidMaterial.SetBuffer(BoidsBuffer, _boidBuffer);
		Graphics.DrawMeshInstancedIndirect(
			_boidMesh,
			0,
			_boidMaterial,
			new Bounds(Vector3.zero, Vector3.one * 1000f),
			_argsBuffer
		);
	}

	private void OnValidate()
	{
		if (!Application.isPlaying)
		{
			return;
		}

		UpdateComputeShaderProperties();
	}

	private void UpdateComputeShaderProperties()
	{
		_boidComputeShader.SetFloat(AccelerationForce, _accelerationForce);
		_boidComputeShader.SetFloat(RotationalSharpness, _rotationalSharpness);
		_boidComputeShader.SetFloat(BankMultiplier, _bankMultiplier);

		_boidComputeShader.SetFloat(SeparationDistanceSqr, _seperationDistance * _seperationDistance);
		_boidComputeShader.SetFloat(AlignmentDistanceSqr, _alignmentDistance * _alignmentDistance);
		_boidComputeShader.SetFloat(CohesionDistanceSqr, _cohesionDistance * _cohesionDistance);

		_boidComputeShader.SetFloat(SeparationWeight, _seperationWeight);
		_boidComputeShader.SetFloat(AlignmentWeight, _alignmentWeight);
		_boidComputeShader.SetFloat(CohesionWeight, _cohesionWeight);

		_boidComputeShader.SetVector(BoundsCenter, transform.position);
		_boidComputeShader.SetVector(BoundsSize, _flockBoundsSize);
		_boidComputeShader.SetFloat(BoundsInnerRatio, _flockBoundsInnerRatio);
		_boidComputeShader.SetFloat(BoundsWeight, _boundsWeight);

		_boidMaterial.SetFloat(BoidScale, _boidScale);
	}

	private void OnDestroy()
	{
		if (_boidBuffer != null)
		{
			_boidBuffer.Release();
		}

		if (_argsBuffer != null)
		{
			_argsBuffer.Release();
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
