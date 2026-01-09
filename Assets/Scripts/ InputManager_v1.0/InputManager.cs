using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class InputManager : PersistentMonoSingleton<InputManager>
{
	public InputActionAsset InputActions;

	private InputAction _movementAction;
	private InputAction _movement2Action;
	private InputAction _kickAction;
	private InputAction _punchAction;
	private InputAction _grabAction;
	private InputAction _continueStoryAction;
	private InputAction _backlogAction;
	private InputAction _escapeAction;

	private InputActionMap _playerActionMap;
	private InputActionMap _uiActionMap;
	private InputActionMap _visualNovelActionMap;

	private const string PlayerActionMap = "Player";
	private const string UIActionMap = "UI";
	private const string VisualNovelActionMap = "VisualNovel";

	[HideInInspector]
	public UnityEvent<Vector2> OnMovement;

	[HideInInspector]
	public UnityEvent<Vector2> OnMovement2;

	[HideInInspector]
	public UnityEvent OnPunchPerformed;

	[HideInInspector]
	public UnityEvent OnKickPerformed;

	[HideInInspector]
	public UnityEvent OnGrabPerformed;

	[HideInInspector]
	public UnityEvent OnContinueStoryPerformed;

	[HideInInspector]
	public UnityEvent OnEscapePerformed;

	[HideInInspector]
	public UnityEvent OnBacklogPerformed;

	[HideInInspector]
	public UnityEvent OnAnyInputPerformed;

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

	private void OnDestroy()
	{
		if (_movementAction != null)
		{
			_movementAction.performed -= OnMovementPerformed;
			_movementAction.canceled -= OnMovementCanceled;
		}

		if (_movement2Action != null)
		{
			_movement2Action.performed -= OnMovementPerformed;
			_movement2Action.canceled -= OnMovementCanceled;
		}
	}

	private void Update()
	{
		UpdateInputs();
		CheckAnyInput();
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

		_movement2Action = InputActions.FindAction("Movement2");
		_movement2Action.performed += OnMovementPerformed;
		_movement2Action.canceled += OnMovementCanceled;

		_punchAction = InputActions.FindAction("Punch");
		_kickAction = InputActions.FindAction("Kick");
		_grabAction = InputActions.FindAction("Grab");

		_continueStoryAction = InputActions.FindAction("ContinueStory");
		_escapeAction = InputActions.FindAction("Escape");
		_backlogAction = InputActions.FindAction("Backlog");
	}

	private void UpdateInputs()
	{
		AddEventToAction(_kickAction, ref OnKickPerformed);
		AddEventToAction(_punchAction, ref OnPunchPerformed);
		AddEventToAction(_grabAction, ref OnGrabPerformed);

		AddEventToAction(_continueStoryAction, ref OnContinueStoryPerformed);
		AddEventToAction(_escapeAction, ref OnEscapePerformed);
		AddEventToAction(_backlogAction, ref OnBacklogPerformed);
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
	/// Checks if the input action is currently held down and invokes the UnityEvent.
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
	/// Checks if the input action was released this frame and invokes the UnityEvent.
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
