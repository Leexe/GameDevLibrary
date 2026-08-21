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

	[SerializeField]
	private UnityEngine.Rendering.ShadowCastingMode _shadowMode = UnityEngine.Rendering.ShadowCastingMode.Off;

	[Title("Spawn Data")]
	[SerializeField]
	[Min(0)]
	private Vector2 _grassSpacing = new(1f, 1f);

	[SerializeField]
	private Vector2 _boundsSize2D = new(100, 100);

	[SerializeField]
	private float _spawnHeight;

	[SerializeField]
	private Vector2 _baseGrassSize = new(0.1f, 0.5f);

	[SerializeField]
	private Vector2 _grassSizeVariance = new(0.05f, 0.1f);

	[SerializeField]
	private float _randomOffset = 1f;

	private ComputeBuffer _visibleGrassBuffer;
	private ComputeBuffer _argsBuffer;
	private int _grassColumns;
	private int _grassRows;
	private int _grassNum;
	private Bounds _bounds;
	private int _cullKernel;

	// Cached Named Properties
	private static readonly int GrassColumns = Shader.PropertyToID("columns");
	private static readonly int GrassRows = Shader.PropertyToID("rows");
	private static readonly int GrassSpacing = Shader.PropertyToID("grassSpacing");
	private static readonly int BoundsSize = Shader.PropertyToID("boundsSize");
	private static readonly int BaseGrassSize = Shader.PropertyToID("baseGrassSize");
	private static readonly int GrassSizeVariance = Shader.PropertyToID("grassSizeVariance");
	private static readonly int GrassBuffer = Shader.PropertyToID("grassBuffer");
	private static readonly int VisibleGrassBuffer = Shader.PropertyToID("visibleGrassBuffer");
	private static readonly int SpawnHeight = Shader.PropertyToID("spawnHeight");
	private static readonly int BoundsCenter = Shader.PropertyToID("boundsCenter");
	private static readonly int MaxRenderDistance = Shader.PropertyToID("maxRenderDistance");
	private static readonly int RandomOffset = Shader.PropertyToID("randomOffset");
	private static readonly int CameraPosition = Shader.PropertyToID("cameraPosition");
	private static readonly int FrustumPlanes = Shader.PropertyToID("frustumPlanes");

	#endregion

	#region Methods

	private void Start()
	{
		_cullKernel = _grassComputeShader.FindKernel("CullGrass");
		_bounds = new Bounds(Vector3.zero, Vector3.one * 1000f);

		_grassColumns = Mathf.CeilToInt(_boundsSize2D.x / _grassSpacing.x);
		_grassRows = Mathf.CeilToInt(_boundsSize2D.y / _grassSpacing.y);
		_grassNum = _grassColumns * _grassRows;

		_visibleGrassBuffer = new ComputeBuffer(_grassNum, 24, ComputeBufferType.Append);

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
	}

	private void OnDestroy()
	{
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
		int threadGroups = Mathf.CeilToInt(_grassNum / 64f);
		_grassComputeShader.Dispatch(_cullKernel, threadGroups, 1, 1);
		ComputeBuffer.CopyCount(_visibleGrassBuffer, _argsBuffer, 4);

		// Render Out Grass
		Graphics.DrawMeshInstancedIndirect(_mesh, 0, _material, _bounds, _argsBuffer, 0, null, _shadowMode);
	}

	private void OnValidate()
	{
		if (!Application.isPlaying || _grassComputeShader == null || _visibleGrassBuffer == null)
		{
			return;
		}

		int newColumns = Mathf.CeilToInt(_boundsSize2D.x / _grassSpacing.x);
		int newRows = Mathf.CeilToInt(_boundsSize2D.y / _grassSpacing.y);
		int newGrassNum = newColumns * newRows;

		if (newGrassNum != _grassNum)
		{
			_grassColumns = newColumns;
			_grassRows = newRows;
			_grassNum = newGrassNum;
			_visibleGrassBuffer.Release();
			_visibleGrassBuffer = new ComputeBuffer(_grassNum, 24, ComputeBufferType.Append);
			UpdateShaderProperties();
		}

		UpdateComputeShaderProperties();
	}

	private void UpdateShaderProperties()
	{
		_material.SetBuffer(GrassBuffer, _visibleGrassBuffer);
	}

	private void UpdateComputeShaderProperties()
	{
		_grassComputeShader.SetInt(GrassColumns, _grassColumns);
		_grassComputeShader.SetInt(GrassRows, _grassRows);
		_grassComputeShader.SetVector(GrassSpacing, _grassSpacing);
		_grassComputeShader.SetVector(BoundsCenter, transform.position);
		_grassComputeShader.SetVector(BoundsSize, new Vector4(_boundsSize2D.x, 0, _boundsSize2D.y, 0));
		_grassComputeShader.SetVector(BaseGrassSize, _baseGrassSize);
		_grassComputeShader.SetVector(GrassSizeVariance, _grassSizeVariance);
		_grassComputeShader.SetFloat(SpawnHeight, _spawnHeight);
		_grassComputeShader.SetFloat(MaxRenderDistance, _maxRenderDistance);
		_grassComputeShader.SetFloat(RandomOffset, _randomOffset);
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
