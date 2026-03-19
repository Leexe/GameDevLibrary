using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class InputManager : PersistentMonoSingleton<InputManager>
{
	private const string PlayerActionMap = "Player";
	private const string UIActionMap = "UI";
	private const string VisualNovelActionMap = "VisualNovel";
	public InputActionAsset InputActions;

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
	private InputActionMap _visualNovelActionMap;

	protected override void Awake()
	{
		base.Awake();
		EnablePlayerInput();
		EnableUIInput();
		SetupInputActions();
	}

	private void Update()
	{
		UpdateInputs();
		CheckAnyInput();
	}

	private void OnEnable()
	{
		EnablePlayerInput();
	}

	private void OnDisable()
	{
		DisablePlayerInput();
	}

	private void OnDestroy()
	{
		if (_movementAction == null)
		{
			return;
		}

		_movementAction.performed -= OnMovementPerformed;
		_movementAction.canceled -= OnMovementCanceled;
	}

	#region Input Setup

	private void SetupInputActions()
	{
		_playerActionMap = InputActions.FindActionMap(PlayerActionMap);
		_uiActionMap = InputActions.FindActionMap(UIActionMap);
		_visualNovelActionMap = InputActions.FindActionMap(VisualNovelActionMap);

		_movementAction = InputActions.FindAction("Movement");
		_movementAction.performed += OnMovementPerformed;
		_movementAction.canceled += OnMovementCanceled;

		_jumpAction = InputActions.FindAction("Jump");
		_dashAction = InputActions.FindAction("Dash");
		_shootAction = InputActions.FindAction("Shoot");
		_reloadAction = InputActions.FindAction("Reload");
		_crouchAction = InputActions.FindAction("Crouch");
		_changeGun = InputActions.FindAction("ChangeGun");

		_continueStoryAction = InputActions.FindAction("ContinueStory");
		_escapeAction = InputActions.FindAction("Escape");
		_backlogAction = InputActions.FindAction("Backlog");
	}

	private void UpdateInputs()
	{
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

	#endregion

	#region Input Callbacks

	private void OnMovementPerformed(InputAction.CallbackContext context)
	{
		Vector3 readVector = context.ReadValue<Vector3>();
		OnMovement?.Invoke(new Vector2(readVector.x, readVector.z));
	}

	private void OnMovementCanceled(InputAction.CallbackContext context)
	{
		OnMovement?.Invoke(Vector2.zero);
	}

	#endregion

	#region Input Helpers

	private void AddEventToAction(InputAction inputAction, ref UnityEvent unityEvent)
	{
		if (inputAction.WasPressedThisFrame())
		{
			unityEvent?.Invoke();
		}
	}

	/// <summary>
	///     Checks if the input action is currently held down and invokes the UnityEvent.
	/// </summary>
	/// <param name="inputAction">The input action to check.</param>
	/// <param name="unityEvent">The UnityEvent to trigger.</param>
	private void AddEventToActionHold(InputAction inputAction, ref UnityEvent unityEvent)
	{
		if (inputAction.IsPressed())
		{
			unityEvent?.Invoke();
		}
	}

	/// <summary>
	///     Checks if the input action was released this frame and invokes the UnityEvent.
	/// </summary>
	/// <param name="inputAction">The input action to check.</param>
	/// <param name="unityEvent">The UnityEvent to trigger.</param>
	private void AddEventToActionRelease(InputAction inputAction, ref UnityEvent unityEvent)
	{
		if (inputAction.WasReleasedThisFrame())
		{
			unityEvent?.Invoke();
		}
	}

	#endregion

	#region Public Methods

	public void EnablePlayerInput()
	{
		_playerActionMap?.Enable();
	}

	public void DisablePlayerInput()
	{
		_playerActionMap?.Disable();
	}

	public void EnableUIInput()
	{
		_uiActionMap?.Enable();
	}

	public void DisableUIInput()
	{
		_uiActionMap?.Disable();
	}

	public void EnableVisualNovelInput()
	{
		_visualNovelActionMap?.Enable();
	}

	public void DisableVisualNovelInput()
	{
		_visualNovelActionMap?.Disable();
	}

	#endregion
}
