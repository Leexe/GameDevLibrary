using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class GrassSpawnerCompute : MonoBehaviour
{
	public struct GrassData
	{
		public Vector3 Position; // 12 Bytes
		public float RotationY; // 4 Bytes
		public Vector2 Scale; // 8 Bytes
	}

	[Serializable]
	public class GrassVariant
	{
		public Mesh Mesh;
		public Material Material;

		[Min(0)]
		public int Weight = 1;

		internal ComputeBuffer ArgsBuffer;
		internal int Count;
	}

	#region Fields

	[Title("References")]
	[SerializeField]
	private ComputeShader _grassComputeShader;

	[Title("Rendering")]
	[SerializeField]
	private List<GrassVariant> _grassVariants = new();

	[Title("Spawn Data")]
	[SerializeField]
	[Min(0)]
	private int _grassCount = 1000;

	[SerializeField]
	private Vector2 _boundsSize2D = new(100, 100);

	[SerializeField]
	private float _spawnHeight;

	[SerializeField]
	private Vector2 _baseGrassSize = new(0.1f, 0.5f);

	[SerializeField]
	private Vector2 _grassSizeVariance = new(0.05f, 0.1f);

	private ComputeBuffer _grassBuffer;
	private Bounds _bounds;
	private int _kernelIndex;

	// Cached Named Properties

	private static readonly int NumGrass = Shader.PropertyToID("numGrass");
	private static readonly int BoundsSize = Shader.PropertyToID("boundsSize");
	private static readonly int BaseGrassSize = Shader.PropertyToID("baseGrassSize");
	private static readonly int GrassSizeVariance = Shader.PropertyToID("grassSizeVariance");
	private static readonly int GrassBuffer = Shader.PropertyToID("grassBuffer");
	private static readonly int SpawnHeight = Shader.PropertyToID("spawnHeight");
	private static readonly int BoundsCenter = Shader.PropertyToID("boundsCenter");

	#endregion

	#region Methods

	private void Start()
	{
		_kernelIndex = _grassComputeShader.FindKernel("InstantiateGrass");
		_bounds = new Bounds(Vector3.zero, Vector3.one * 1000f);

		_grassBuffer = new ComputeBuffer(_grassCount, 24);

		InitializeVariants();
		UpdateComputeShaderProperties();
		UpdateShaderProperties();

		int threadGroups = Mathf.CeilToInt(_grassCount / 64f);
		_grassComputeShader.Dispatch(_kernelIndex, threadGroups, 1, 1);
	}

	private void OnDestroy()
	{
		_grassBuffer?.Release();

		if (_grassVariants != null)
		{
			foreach (GrassVariant variant in _grassVariants)
			{
				variant.ArgsBuffer?.Release();
			}
		}
	}

	private void Update()
	{
		foreach (GrassVariant variant in _grassVariants)
		{
			Graphics.DrawMeshInstancedIndirect(variant.Mesh, 0, variant.Material, _bounds, variant.ArgsBuffer);
		}
	}

	private void InitializeVariants()
	{
		// Calculate variant counts based on weights
		int totalWeight = 0;
		foreach (GrassVariant variant in _grassVariants)
		{
			totalWeight += variant.Weight;
		}

		int grassAllocated = 0;
		for (int i = 0; i < _grassVariants.Count; i++)
		{
			GrassVariant variant = _grassVariants[i];
			if (i == _grassVariants.Count - 1)
			{
				variant.Count = _grassCount - grassAllocated;
			}
			else
			{
				variant.Count = Mathf.RoundToInt((float)variant.Weight / totalWeight * _grassCount);
				grassAllocated += variant.Count;
			}
		}

		// Set Up Args Buffers per variant
		foreach (GrassVariant variant in _grassVariants)
		{
			uint[] args =
			{
				variant.Mesh.GetIndexCount(0), // Indices per mesh
				(uint)variant.Count, // How many meshes to draw
				variant.Mesh.GetIndexStart(0), // Where does the mesh start
				variant.Mesh.GetBaseVertex(0), // Starting point of vertex array
				0,
			};
			variant.ArgsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
			variant.ArgsBuffer.SetData(args);
		}
	}

	private void OnValidate()
	{
		if (!Application.isPlaying || _grassBuffer == null || _grassComputeShader == null)
		{
			return;
		}

		UpdateComputeShaderProperties();

		int threadGroups = Mathf.CeilToInt(_grassBuffer.count / 64f);
		_grassComputeShader.Dispatch(_kernelIndex, threadGroups, 1, 1);
	}

	private void UpdateShaderProperties()
	{
		foreach (GrassVariant variant in _grassVariants)
		{
			variant.Material.SetBuffer(GrassBuffer, _grassBuffer);
		}
	}

	private void UpdateComputeShaderProperties()
	{
		_grassComputeShader.SetInt(NumGrass, _grassCount);
		_grassComputeShader.SetVector(BoundsCenter, transform.position);
		_grassComputeShader.SetVector(BoundsSize, new Vector4(_boundsSize2D.x, 0, _boundsSize2D.y, 0));
		_grassComputeShader.SetVector(BaseGrassSize, _baseGrassSize);
		_grassComputeShader.SetVector(GrassSizeVariance, _grassSizeVariance);
		_grassComputeShader.SetBuffer(_kernelIndex, GrassBuffer, _grassBuffer);
		_grassComputeShader.SetFloat(SpawnHeight, _spawnHeight);
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
		Vector3 center = transform.position + new Vector3(0f, _spawnHeight, 0f);
		var size = new Vector3(_boundsSize2D.x, 0.1f, _boundsSize2D.y);
		Gizmos.DrawWireCube(center, size);
	}

	#endregion
}
