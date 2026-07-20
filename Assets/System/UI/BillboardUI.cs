using UnityEngine;

namespace StatusEffects.UI
{
	public class BillboardUI : MonoBehaviour
	{
		[Header("Options")]
		[SerializeField]
		private bool _lockX;

		[SerializeField]
		private bool _lockY;

		[SerializeField]
		private bool _lockZ;

		private Camera _mainCamera;
		private Transform _transform;
		private Transform _cameraTransform;

		private void Start()
		{
			_mainCamera = Camera.main;
			_transform = transform;

			if (_mainCamera != null)
			{
				_cameraTransform = _mainCamera.transform;
			}
		}

		private void LateUpdate()
		{
			if (_cameraTransform == null)
			{
				return;
			}

			Vector3 targetEuler = _cameraTransform.rotation.eulerAngles;
			Vector3 currentEuler = _transform.rotation.eulerAngles;

			targetEuler.x = _lockX ? currentEuler.x : targetEuler.x;
			targetEuler.y = _lockY ? currentEuler.y : targetEuler.y;
			targetEuler.z = _lockZ ? currentEuler.z : targetEuler.z;

			_transform.rotation = Quaternion.Euler(targetEuler);
		}
	}
}
