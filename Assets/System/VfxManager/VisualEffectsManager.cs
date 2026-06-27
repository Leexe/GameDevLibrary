using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

public class VisualEffectsManager : MonoSingleton<VisualEffectsManager>
{
	[VFXType(VFXTypeAttribute.Usage.GraphicsBuffer)]
	public struct BufferData
	{
		public Vector3 Position; // 12 Bytes
		public float Scale; // 4 Bytes
	}

	public class VfxData
	{
		public VisualEffect VfxGraph;
		public GraphicsBuffer Buffer;
		public List<Transform> Targets = new();
		public List<BufferData> DataArray = new();
	}

	[SerializeField]
	private int _initialCapacity = 64;

	[SerializeField]
	private GameObject _vfxPrefab;

	[SerializeField]
	private Dictionary<string, VisualEffect> _vfxGraphs;

	private ExposedProperty _bufferProperty = "SpawnPoints";
	private ExposedProperty _bufferCountProperty = "SpawnPointsCount";
	private Dictionary<string, VfxData> _vfxData = new();
	private const int BufferStride = 16;

	protected override void OnInitialized()
	{
		foreach (KeyValuePair<string, VisualEffect> kvp in _vfxGraphs)
		{
			_vfxData[kvp.Key] = new VfxData { VfxGraph = kvp.Value };
			EnsureBufferCapacity(
				ref _vfxData[kvp.Key].Buffer,
				_initialCapacity,
				BufferStride,
				_vfxData[kvp.Key].VfxGraph,
				_bufferProperty
			);

			// Disable Gameobjects
			_vfxData[kvp.Key].VfxGraph.gameObject.SetActive(false);
		}
	}

	private void LateUpdate()
	{
		foreach (KeyValuePair<string, VfxData> kvp in _vfxData)
		{
			VfxData vfxData = kvp.Value;

			vfxData.Targets.RemoveAll(t => t == null);

			if (vfxData.Targets.Count == 0)
			{
				continue;
			}

			// Ensure that buffer is big enough
			EnsureBufferCapacity(
				ref vfxData.Buffer,
				vfxData.Targets.Count,
				BufferStride,
				vfxData.VfxGraph,
				_bufferProperty
			);

			// Add data into buffer
			vfxData.DataArray.Clear();
			foreach (Transform t in vfxData.Targets)
			{
				vfxData.DataArray.Add(new BufferData { Position = t.position, Scale = t.localScale.x });
			}

			// Send data to GPU
			vfxData.Buffer.SetData(vfxData.DataArray);
			vfxData.VfxGraph.SetUInt(_bufferCountProperty, (uint)vfxData.DataArray.Count);
		}
	}

	private void OnDestroy()
	{
		ReleaseBuffers();
	}

	public void AddVfxTarget(string id, Transform target, VisualEffectAsset vfxAsset, float destroyAfterTime = -1)
	{
		// Create new vfxGraph Gameobject and vfxData if status is not recognized
		if (!_vfxData.ContainsKey(id))
		{
			_vfxGraphs[id] = Instantiate(_vfxPrefab, gameObject.transform).GetComponent<VisualEffect>();
			_vfxGraphs[id].visualEffectAsset = vfxAsset;
			_vfxData[id] = new VfxData { VfxGraph = _vfxGraphs[id] };
			EnsureBufferCapacity(
				ref _vfxData[id].Buffer,
				_initialCapacity,
				BufferStride,
				_vfxData[id].VfxGraph,
				_bufferProperty
			);
		}

		AddVfxTarget(id, target, destroyAfterTime);
	}

	public void AddVfxTarget(string id, Transform target, float destroyAfterTime = -1)
	{
		// Create new vfxGraph Gameobject and vfxData if status is not recognized
		if (!_vfxData.ContainsKey(id))
		{
			Debug.LogError($"AddVfxTarget: Vfx doesn't exist: {id}");
		}

		// Add target transform to list
		if (!_vfxData[id].Targets.Contains(target))
		{
			_vfxData[id].Targets.Add(target);
		}

		// Enable Gameobject if it was previously inactive
		if (!_vfxData[id].VfxGraph.isActiveAndEnabled)
		{
			_vfxData[id].VfxGraph.gameObject.SetActive(true);
		}

		if (destroyAfterTime > 0)
		{
			Tween.Delay(destroyAfterTime, () => RemoveVfxTarget(id, target));
		}
	}

	public void RemoveVfxTarget(string id, Transform target)
	{
		if (_vfxData.TryGetValue(id, out VfxData vfxData))
		{
			vfxData.Targets.Remove(target);
			vfxData.VfxGraph.SetUInt(_bufferCountProperty, (uint)vfxData.Targets.Count);
			if (vfxData.Targets.Count == 0)
			{
				vfxData.VfxGraph.gameObject.SetActive(false);
			}
		}
	}

	private void EnsureBufferCapacity(
		ref GraphicsBuffer buffer,
		int capacity,
		int stride,
		VisualEffect vfx,
		int vfxBufferProperty
	)
	{
		if (buffer == null || buffer.count < capacity)
		{
			buffer?.Release();
			int newCapacity = Mathf.Max(_initialCapacity, capacity * 2);
			buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, newCapacity, stride);
			vfx.SetGraphicsBuffer(vfxBufferProperty, buffer);
		}
	}

	private void ReleaseBuffers()
	{
		foreach (VfxData group in _vfxData.Values)
		{
			group.Buffer?.Release();
			group.Buffer = null;
		}
	}
}
