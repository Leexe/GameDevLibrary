using PrimeTween;
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

	[Header("Focus Cameras")]
	[SerializeField]
	private CinemachineCamera _playerCamera;

	[SerializeField]
	private CinemachinePanTilt _panTilt;

	[SerializeField]
	private CinemachineInputAxisController _inputAxisController;

	[Header("Focus Settings")]
	[SerializeField]
	private float _focusTweenDuration = 0.5f;

	private Sequence _focusSequence;

	private CinemachineInputAxisController _cinemachineInputAxisController;
	public float CameraSensitivity { get; private set; }

	private void OnDisable()
	{
		ClearFocus();
	}

	protected override void OnInitialized()
	{
		base.OnInitialized();

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

	public void FocusOn(Transform target)
	{
		// Disable Camera Movement
		if (_inputAxisController != null)
			_inputAxisController.enabled = false;

		Vector3 direction = target.position - _playerCamera.transform.position;
		if (direction == Vector3.zero)
		{
			return;
		}

		var targetRotation = Quaternion.LookRotation(direction);
		Vector3 targetEuler = targetRotation.eulerAngles;

		float targetPan = targetEuler.y;
		float targetTilt = targetEuler.x;
		if (targetTilt > 180f)
		{
			targetTilt -= 360f;
		}

		// Calculate shortest path for pan
		float startPan = _panTilt.PanAxis.Value;
		float deltaPan = Mathf.DeltaAngle(startPan, targetPan);
		float finalTargetPan = startPan + deltaPan;

		_focusSequence.Stop();
		_focusSequence = Sequence.Create();

		_focusSequence.Group(
			Tween.Custom(
				startPan,
				finalTargetPan,
				_focusTweenDuration,
				onValueChange: newVal => _panTilt.PanAxis.Value = newVal,
				ease: Ease.InOutSine
			)
		);

		_focusSequence.Group(
			Tween.Custom(
				_panTilt.TiltAxis.Value,
				targetTilt,
				_focusTweenDuration,
				onValueChange: newVal => _panTilt.TiltAxis.Value = newVal,
				ease: Ease.InOutSine
			)
		);
	}

	public void ClearFocus()
	{
		_focusSequence.Stop();
		if (_inputAxisController != null)
			_inputAxisController.enabled = true;
	}
}
