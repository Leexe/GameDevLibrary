using UnityEngine;

public struct BoidData
{
	public Vector3 Position;
	public Vector3 Velocity;
}

public class BoidSpawnerCompute : MonoBehaviour
{
	#region Fields

	[Header("References")]
	[SerializeField]
	private ComputeShader _boidComputeShader;

	[Header("Rendering")]
	[SerializeField]
	private Mesh _boidMesh;

	[SerializeField]
	private Material _boidMaterial;

	[Header("Spawn")]
	[SerializeField]
	[Min(1)]
	private int _boidCount = 60;

	[SerializeField]
	private Vector3 _spawnBoundaries = new(14f, 6f, 14f);

	[Header("Movement")]
	[SerializeField]
	[Min(0.1f)]
	private float _maxSpeed = 7f;

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

	private ComputeBuffer _boidBuffer;
	private ComputeBuffer _argsBuffer;
	private int _kernelIndex;

	// Cached Named Properties

	private static readonly int BoidsBuffer = Shader.PropertyToID("boidsBuffer");
	private static readonly int NumBoids = Shader.PropertyToID("numBoids");
	private static readonly int MaxSpeed = Shader.PropertyToID("maxSpeed");
	private static readonly int SeparationDistanceSqr = Shader.PropertyToID("separationDistanceSqr");
	private static readonly int AlignmentDistanceSqr = Shader.PropertyToID("alignmentDistanceSqr");
	private static readonly int CohesionDistanceSqr = Shader.PropertyToID("cohesionDistanceSqr");
	private static readonly int SeparationWeight = Shader.PropertyToID("separationWeight");
	private static readonly int AlignmentWeight = Shader.PropertyToID("alignmentWeight");
	private static readonly int CohesionWeight = Shader.PropertyToID("cohesionWeight");
	private static readonly int DeltaTime = Shader.PropertyToID("deltaTime");

	#endregion

	#region Methods

	private void Start()
	{
		// Initalize Array
		var boidsArray = new BoidData[_boidCount];
		for (int i = 0; i < _boidCount; i++)
		{
			boidsArray[i] = new BoidData
			{
				Position = transform.position + GetRandomSpawnOffset(),
				Velocity = Random.onUnitSphere,
			};
		}

		// Create Compute Buffer
		// Float = 4 Bytes, Float3 = 12 Bytes, 2 * Float3 = 24 Bytes
		_boidBuffer = new ComputeBuffer(_boidCount, 24);

		// Send Data to GPU
		_boidBuffer.SetData(boidsArray);

		// Hook Up Static Data
		_kernelIndex = _boidComputeShader.FindKernel("UpdateBoids");

		_boidComputeShader.SetBuffer(_kernelIndex, BoidsBuffer, _boidBuffer);
		_boidComputeShader.SetInt(NumBoids, _boidCount);
		_boidComputeShader.SetFloat(MaxSpeed, _maxSpeed);

		_boidComputeShader.SetFloat(SeparationDistanceSqr, _seperationDistance * _seperationDistance);
		_boidComputeShader.SetFloat(AlignmentDistanceSqr, _alignmentDistance * _alignmentDistance);
		_boidComputeShader.SetFloat(CohesionDistanceSqr, _cohesionDistance * _cohesionDistance);

		_boidComputeShader.SetFloat(SeparationWeight, _seperationWeight);
		_boidComputeShader.SetFloat(AlignmentWeight, _alignmentWeight);
		_boidComputeShader.SetFloat(CohesionWeight, _cohesionWeight);

		// Set Up Args Buffer
		uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
		args[0] = _boidMesh.GetIndexCount(0); // Indicies per mesh
		args[1] = (uint)_boidCount; // How many meshes to draw
		args[2] = _boidMesh.GetIndexStart(0); // Where does the mesh start
		args[3] = _boidMesh.GetBaseVertex(0); // Starting point of vertex array
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
			new Bounds(Vector3.zero, Vector3.one * 1000f), // Ensure they don't get culled
			_argsBuffer
		);
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

	#endregion
}
