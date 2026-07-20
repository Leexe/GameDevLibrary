using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : PersistentMonoSingleton<InputManager>
{
	// Action Maps
	private const string PlayerActionMap = "Player";
	private const string DialogueActionMap = "Dialogue";
	private const string UIActionMap = "UI";

	// References
	[Tooltip("The Input Action Asset containing all player and UI actions.")]
	public InputActionAsset InputActions;

	// Events
	public event Action<Vector2> OnMovement;
	public event Action OnZoomPerformed;
	public event Action OnJumpPerformed;
	public event Action OnDashPerformed;
	public event Action OnShootingPerformed;
	public event Action OnShootingReleased;
	public event Action OnReloadPerformed;
	public event Action OnCrouchPerformed;
	public event Action OnCrouchRelease;
	public event Action OnChangeGun;
	public event Action OnInteractPerformed;
	public event Action OnContinueStoryPerformed;
	public event Action OnEscapePerformed;
	public event Action OnBacklogPerformed;
	public event Action OnAnyInputPerformed;

	private InputAction _movementAction;
	private List<ActionBinding> _bindings = new List<ActionBinding>();

	/** Start Methods **/
	protected override void OnInitialized()
	{
		base.OnInitialized();

		if (InputActions == null)
		{
			Debug.LogError("InputManager: InputActions asset is not assigned!");
			return;
		}

		SetupInputActions();
		EnableUIInput();
	}

	private void OnEnable()
	{
		if (InputActions != null)
		{
			EnablePlayerInput();
			EnableVisualNovelInput();
			SubscribeEvents();
		}
	}

	private void OnDisable()
	{
		if (IsActiveInstance && InputActions != null)
		{
			DisablePlayerInput();
			DisableVisualNovelInput();
		}
		UnsubscribeEvents();
	}

	private void SetupInputActions()
	{
		_bindings.Clear();
		_movementAction = InputActions.FindAction("Movement");

		BindAction("ContinueStory", performed: () => OnContinueStoryPerformed?.Invoke());
		BindAction("Escape", performed: () => OnEscapePerformed?.Invoke());
		BindAction("Backlog", performed: () => OnBacklogPerformed?.Invoke());
		BindAction("Zoom", performed: () => OnZoomPerformed?.Invoke());
		BindAction("Jump", performed: () => OnJumpPerformed?.Invoke());
		BindAction("Dash", performed: () => OnDashPerformed?.Invoke());
		BindAction("Interact", performed: () => OnInteractPerformed?.Invoke());
		BindAction(
			"Shoot",
			performed: () => OnShootingPerformed?.Invoke(),
			canceled: () => OnShootingReleased?.Invoke()
		);
		BindAction("Reload", performed: () => OnReloadPerformed?.Invoke());
		BindAction("Crouch", performed: () => OnCrouchPerformed?.Invoke(), canceled: () => OnCrouchRelease?.Invoke());
		BindAction("ChangeGun", performed: () => OnChangeGun?.Invoke());
	}

	private void SubscribeEvents()
	{
		foreach (ActionBinding binding in _bindings)
		{
			binding.Subscribe();
		}
	}

	private void UnsubscribeEvents()
	{
		foreach (ActionBinding binding in _bindings)
		{
			binding.Unsubscribe();
		}
	}

	/** Update Methods **/
	private void Update()
	{
		if (!InputActions)
		{
			return;
		}

		UpdateContinuousInputs();
		CheckAnyInput();
	}

	private void UpdateContinuousInputs()
	{
		if (_movementAction != null)
		{
			Vector3 readVector = _movementAction.ReadValue<Vector3>();
			OnMovement?.Invoke(new Vector2(readVector.x, readVector.z));
		}
	}

	/// <summary>
	/// Checks for any input and invokes the event
	/// </summary>
	private void CheckAnyInput()
	{
		if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
		{
			OnAnyInputPerformed?.Invoke();
			return;
		}

		if (
			Mouse.current != null
			&& (
				Mouse.current.leftButton.wasPressedThisFrame
				|| Mouse.current.rightButton.wasPressedThisFrame
				|| Mouse.current.middleButton.wasPressedThisFrame
			)
		)
		{
			OnAnyInputPerformed?.Invoke();
		}
	}

	/// <summary>
	/// Enable Player Input
	/// </summary>
	public void EnablePlayerInput()
	{
		InputActions?.FindActionMap(PlayerActionMap)?.Enable();
	}

	/// <summary>
	/// Disable Player Input
	/// </summary>
	public void DisablePlayerInput()
	{
		InputActions?.FindActionMap(PlayerActionMap)?.Disable();
	}

	/// <summary>
	/// Enable UI Input
	/// </summary>
	public void EnableUIInput()
	{
		InputActions?.FindActionMap(UIActionMap)?.Enable();
	}

	/// <summary>
	/// Disable UI Input
	/// </summary>
	public void DisableUIInput()
	{
		InputActions?.FindActionMap(UIActionMap)?.Disable();
	}

	/// <summary>
	///     Enable all Visual Novel specific inputs.
	/// </summary>
	public void EnableVisualNovelInput()
	{
		InputActions?.FindActionMap(DialogueActionMap)?.Enable();
	}

	/// <summary>
	///     Disable all Visual Novel specific inputs.
	/// </summary>
	public void DisableVisualNovelInput()
	{
		InputActions?.FindActionMap(DialogueActionMap)?.Disable();
	}

	/** Action Binding System **/
	private class ActionBinding
	{
		private readonly Action _performedEvent;
		private readonly Action _canceledEvent;
		private readonly InputAction _action;

		public ActionBinding(InputAction action, Action performed, Action canceled)
		{
			_action = action;
			_performedEvent = performed;
			_canceledEvent = canceled;
		}

		public void Subscribe()
		{
			_action.performed += OnPerformed;
			_action.canceled += OnCanceled;
		}

		public void Unsubscribe()
		{
			_action.performed -= OnPerformed;
			_action.canceled -= OnCanceled;
		}

		private void OnPerformed(InputAction.CallbackContext context) => _performedEvent?.Invoke();

		private void OnCanceled(InputAction.CallbackContext context) => _canceledEvent?.Invoke();
	}

	private void BindAction(string actionName, Action performed = null, Action canceled = null)
	{
		InputAction action = InputActions.FindAction(actionName);
		if (action != null)
		{
			_bindings.Add(new ActionBinding(action, performed, canceled));
		}
		else
		{
			Debug.LogWarning($"InputManager: Could not find action '{actionName}'");
		}
	}
}
