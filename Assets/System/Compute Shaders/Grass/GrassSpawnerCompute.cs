using System;
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

	#region Fields

	[Title("References")]
	[SerializeField]
	private ComputeShader _grassComputeShader;

	[Title("Rendering")]
	[SerializeField]
	private Mesh _mesh;

	[SerializeField]
	private Material _material;

	[SerializeField]
	private float _maxRenderDistance = 100;

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
	private ComputeBuffer _visibleGrassBuffer;
	private ComputeBuffer _argsBuffer;
	private Bounds _bounds;
	private int _initKernel;
	private int _cullKernel;

	// Cached Named Properties
	private static readonly int NumGrass = Shader.PropertyToID("numGrass");
	private static readonly int BoundsSize = Shader.PropertyToID("boundsSize");
	private static readonly int BaseGrassSize = Shader.PropertyToID("baseGrassSize");
	private static readonly int GrassSizeVariance = Shader.PropertyToID("grassSizeVariance");
	private static readonly int GrassBuffer = Shader.PropertyToID("grassBuffer");
	private static readonly int VisibleGrassBuffer = Shader.PropertyToID("visibleGrassBuffer");
	private static readonly int SpawnHeight = Shader.PropertyToID("spawnHeight");
	private static readonly int BoundsCenter = Shader.PropertyToID("boundsCenter");
	private static readonly int MaxRenderDistance = Shader.PropertyToID("maxRenderDistance");
	private static readonly int CameraPosition = Shader.PropertyToID("cameraPosition");
	private static readonly int FrustumPlanes = Shader.PropertyToID("frustumPlanes");

	#endregion

	#region Methods

	private void Start()
	{
		_initKernel = _grassComputeShader.FindKernel("InstantiateGrass");
		_cullKernel = _grassComputeShader.FindKernel("CullGrass");
		_bounds = new Bounds(Vector3.zero, Vector3.one * 1000f);

		_grassBuffer = new ComputeBuffer(_grassCount, 24);
		_visibleGrassBuffer = new ComputeBuffer(_grassCount, 24, ComputeBufferType.Append);

		uint[] args = new uint[5]
		{
			_mesh.GetIndexCount(0), // Indices per mesh
			0, // How many meshes to draw
			_mesh.GetIndexStart(0), // Where does the mesh start
			_mesh.GetBaseVertex(0), // Starting point of vertex array
			0,
		};
		_argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
		_argsBuffer.SetData(args);

		UpdateComputeShaderProperties();
		UpdateShaderProperties();

		int threadGroups = Mathf.CeilToInt(_grassCount / 64f);
		_grassComputeShader.Dispatch(_initKernel, threadGroups, 1, 1);
	}

	private void OnDestroy()
	{
		_grassBuffer?.Release();
		_visibleGrassBuffer?.Release();
		_argsBuffer?.Release();
	}

	private void Update()
	{
		// Update Camera Properties
		_grassComputeShader.SetVector(CameraPosition, Camera.main.transform.position);
		_grassComputeShader.SetVectorArray(FrustumPlanes, CameraManager.Instance.FrustumPlanesVec4);

		// Cull Grass
		_visibleGrassBuffer.SetCounterValue(0);
		int threadGroups = Mathf.CeilToInt(_grassCount / 64f);
		_grassComputeShader.Dispatch(_cullKernel, threadGroups, 1, 1);
		ComputeBuffer.CopyCount(_visibleGrassBuffer, _argsBuffer, 4);

		// Render Out Grass
		Graphics.DrawMeshInstancedIndirect(_mesh, 0, _material, _bounds, _argsBuffer);
	}

	private void OnValidate()
	{
		if (!Application.isPlaying || _grassBuffer == null || _grassComputeShader == null)
		{
			return;
		}

		UpdateComputeShaderProperties();

		int threadGroups = Mathf.CeilToInt(_grassBuffer.count / 64f);
		_grassComputeShader.Dispatch(_initKernel, threadGroups, 1, 1);
	}

	private void UpdateShaderProperties()
	{
		_material.SetBuffer(GrassBuffer, _visibleGrassBuffer);
	}

	private void UpdateComputeShaderProperties()
	{
		_grassComputeShader.SetInt(NumGrass, _grassCount);
		_grassComputeShader.SetVector(BoundsCenter, transform.position);
		_grassComputeShader.SetVector(BoundsSize, new Vector4(_boundsSize2D.x, 0, _boundsSize2D.y, 0));
		_grassComputeShader.SetVector(BaseGrassSize, _baseGrassSize);
		_grassComputeShader.SetVector(GrassSizeVariance, _grassSizeVariance);
		_grassComputeShader.SetFloat(SpawnHeight, _spawnHeight);
		_grassComputeShader.SetFloat(MaxRenderDistance, _maxRenderDistance);

		_grassComputeShader.SetBuffer(_initKernel, GrassBuffer, _grassBuffer);
		
		_grassComputeShader.SetBuffer(_cullKernel, GrassBuffer, _grassBuffer);
		_grassComputeShader.SetBuffer(_cullKernel, VisibleGrassBuffer, _visibleGrassBuffer);
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
		Vector3 center = transform.position + new Vector3(0f, _spawnHeight, 0f);
		Vector3 size = new Vector3(_boundsSize2D.x, 0.1f, _boundsSize2D.y);
		Gizmos.DrawWireCube(center, size);
	}

	#endregion
}
