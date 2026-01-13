using Unity.Cinemachine;
using UnityEngine;

public class CameraManager : MonoSingleton<CameraManager>
{
	[Header("References")]
	public GameObject MainCameraGameObject;

	[SerializeField]
	private GameObject _cinemachine;

	[Header("FPS Camera Settings")]
	[SerializeField]
	private float _defaultCameraSensitivity = 1.125f;

	private CinemachineInputAxisController _cinemachineInputAxisController;
	public float CameraSensitivity { get; private set; }

	protected override void Awake()
	{
		base.Awake();

		CameraSensitivity = 1f;
		_cinemachineInputAxisController = _cinemachine.GetComponent<CinemachineInputAxisController>();
	}

	private void LockCamera()
	{
		foreach (
			InputAxisControllerBase<CinemachineInputAxisController.Reader>.Controller c in _cinemachineInputAxisController.Controllers
		)
		{
			if (c.Name == "Look X (Pan)")
			{
				c.Input.Gain = 0;
			}
			else if (c.Name == "Look Y (Tilt)")
			{
				c.Input.Gain = 0;
			}
		}
	}

	private void UnlockCamera()
	{
		foreach (
			InputAxisControllerBase<CinemachineInputAxisController.Reader>.Controller c in _cinemachineInputAxisController.Controllers
		)
		{
			if (c.Name == "Look X (Pan)")
			{
				c.Input.Gain = _defaultCameraSensitivity * CameraSensitivity;
			}
			else if (c.Name == "Look Y (Tilt)")
			{
				c.Input.Gain = -_defaultCameraSensitivity * CameraSensitivity;
			}
		}
	}

	public void ChangeSensitivity(float newSens)
	{
		foreach (
			InputAxisControllerBase<CinemachineInputAxisController.Reader>.Controller c in _cinemachineInputAxisController.Controllers
		)
		{
			if (c.Name == "Look X (Pan)")
			{
				c.Input.Gain = _defaultCameraSensitivity * newSens;
				CameraSensitivity = newSens;
			}
			else if (c.Name == "Look Y (Tilt)")
			{
				c.Input.Gain = -_defaultCameraSensitivity * newSens;
				CameraSensitivity = newSens;
			}
		}
	}
}
