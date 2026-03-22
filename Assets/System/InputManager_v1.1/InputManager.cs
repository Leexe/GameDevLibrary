using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class InputManager : PersistentMonoSingleton<InputManager>
{
	// References
	public InputActionAsset InputActions;

	// Actions
	private InputAction _backlogAction;
	private InputAction _changeGun;
	private InputAction _continueStoryAction;
	private InputAction _crouchAction;
	private InputAction _dashAction;
	private InputAction _escapeAction;
	private InputAction _jumpAction;
	private InputAction _movementAction;
	private InputActionMap _playerActionMap;
	private InputAction _reloadAction;
	private InputAction _shootAction;
	private InputActionMap _uiActionMap;

	// Action Maps
	private const string PlayerActionMap = "Player";
	private const string UIActionMap = "UI";

	// Events
	[HideInInspector]
	public UnityEvent<Vector2> OnMovement;

	[HideInInspector]
	public UnityEvent OnJumpPerformed;

	[HideInInspector]
	public UnityEvent OnDashPerformed;

	[HideInInspector]
	public UnityEvent OnShootingPerformed;

	[HideInInspector]
	public UnityEvent OnShootingReleased;

	[HideInInspector]
	public UnityEvent OnReloadPerformed;

	[HideInInspector]
	public UnityEvent OnCrouchPerformed;

	[HideInInspector]
	public UnityEvent OnCrouchRelease;

	[HideInInspector]
	public UnityEvent OnChangeGun;

	[HideInInspector]
	public UnityEvent OnContinueStoryPerformed;

	[HideInInspector]
	public UnityEvent OnEscapePerformed;

	[HideInInspector]
	public UnityEvent OnBacklogPerformed;

	[HideInInspector]
	public UnityEvent OnAnyInputPerformed;

	/** Start Methods **/

	protected override void Awake()
	{
		base.Awake();
		EnablePlayerInput();
		EnableUIInput();
		SetupInputActions();
	}

	private void OnEnable()
	{
		EnablePlayerInput();
	}

	private void OnDisable()
	{
		DisablePlayerInput();
	}

	private void SetupInputActions()
	{
		_movementAction = InputActions.FindAction("Movement");
		_jumpAction = InputActions.FindAction("Jump");
		_dashAction = InputActions.FindAction("Dash");
		_shootAction = InputActions.FindAction("Shoot");
		_reloadAction = InputActions.FindAction("Reload");
		_crouchAction = InputActions.FindAction("Crouch");
		_changeGun = InputActions.FindAction("ChangeGun");
	}

	/** Update Methods **/

	private void Update()
	{
		UpdateInputs();
		CheckAnyInput();
	}

	private void UpdateInputs()
	{
		UpdateMovementVector(_movementAction, ref OnMovement);

		AddEventToAction(_continueStoryAction, ref OnContinueStoryPerformed);
		AddEventToAction(_escapeAction, ref OnEscapePerformed);
		AddEventToAction(_backlogAction, ref OnBacklogPerformed);

		AddEventToAction(_jumpAction, ref OnJumpPerformed);
		AddEventToAction(_dashAction, ref OnDashPerformed);
		AddEventToAction(_shootAction, ref OnShootingPerformed);
		AddEventToAction(_reloadAction, ref OnReloadPerformed);
		AddEventToAction(_crouchAction, ref OnCrouchPerformed);
		AddEventToAction(_changeGun, ref OnChangeGun);

		AddEventToActionRelease(_shootAction, ref OnShootingReleased);
		AddEventToActionRelease(_crouchAction, ref OnCrouchRelease);
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
	/// Updates a Vector3 variable depending on a movement input action
	/// </summary>
	/// <param name="inputAction">Input action was pressed</param>
	/// <param name="unityEvent">Unity Event To Trigger</param>
	private void UpdateMovementVector(InputAction inputAction, ref UnityEvent<Vector2> unityEvent)
	{
		Vector3 readVector = inputAction.ReadValue<Vector3>();
		unityEvent?.Invoke(new Vector2(readVector.x, readVector.z));
	}

	/// <summary>
	/// Checks every update if the input was pressed and calls the unity event
	/// </summary>
	/// <param name="inputAction">Input action was pressed</param>
	/// <param name="unityEvent">Unity Event To Trigger</param>
	private void AddEventToAction(InputAction inputAction, ref UnityEvent unityEvent)
	{
		if (inputAction.WasPressedThisFrame())
		{
			unityEvent?.Invoke();
		}
	}

	/// <summary>
	/// Checks every update if the input was held down and calls the unity event
	/// </summary>
	/// <param name="inputAction">Input action was pressed</param>
	/// <param name="unityEvent">Unity Event To Trigger</param>
	private void AddEventToActionHold(InputAction inputAction, ref UnityEvent unityEvent)
	{
		if (inputAction.IsPressed())
		{
			unityEvent?.Invoke();
		}
	}

	/// <summary>
	/// Checks every update if the input was released and calls the unity event
	/// </summary>
	/// <param name="inputAction">Input action was pressed</param>
	/// <param name="unityEvent">Unity Event To Trigger</param>
	private void AddEventToActionRelease(InputAction inputAction, ref UnityEvent unityEvent)
	{
		if (inputAction.WasReleasedThisFrame())
		{
			unityEvent?.Invoke();
		}
	}

	/// <summary>
	/// Enable Player Input
	/// </summary>
	public void EnablePlayerInput()
	{
		InputActions.FindActionMap(PlayerActionMap).Enable();
	}

	/// <summary>
	/// Disable Player Input
	/// </summary>
	public void DisablePlayerInput()
	{
		InputActions.FindActionMap(PlayerActionMap).Disable();
	}

	/// <summary>
	/// Enable UI Input
	/// </summary>
	public void EnableUIInput()
	{
		InputActions.FindActionMap(UIActionMap).Enable();
	}

	/// <summary>
	/// Disable UI Input
	/// </summary>
	public void DisableUIInput()
	{
		InputActions.FindActionMap(UIActionMap).Disable();
	}
}
