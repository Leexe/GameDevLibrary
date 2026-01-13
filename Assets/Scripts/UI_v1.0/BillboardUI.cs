using UnityEngine;

namespace StatusEffects.UI
{
	public class BillboardUI : MonoBehaviour
	{
		private Camera _mainCamera;

		private void Start()
		{
			_mainCamera = Camera.main;
		}

		private void LateUpdate()
		{
			if (_mainCamera == null)
			{
				_mainCamera = Camera.main;
				if (_mainCamera == null)
				{
					return;
				}
			}

			transform.rotation = _mainCamera.transform.rotation;
		}
	}
}
