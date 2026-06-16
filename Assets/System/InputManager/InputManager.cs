using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class InputManager : PersistentMonoSingleton<InputManager>
{
	// Action Maps
	private const string PlayerActionMap = "Player";
	private const string VisualNovelActionMap = "VisualNovel";
	private const string UIActionMap = "UI";

	// References
	[Tooltip("The Input Action Asset containing all player and UI actions.")]
	public InputActionAsset InputActions;

	// Events
	[Header("Continuous Events (Updated Every Frame)")]
	[HideInInspector]
	public UnityEvent<Vector2> OnMovement;

	[Header("Discrete Events (Fired on Press/Release)")]
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

	// Actions
	private InputAction _backlogAction;
	private InputAction _changeGun;
	private InputAction _continueStoryAction;
	private InputAction _crouchAction;
	private InputAction _dashAction;
	private InputAction _escapeAction;
	private InputAction _jumpAction;
	private InputAction _movementAction;
	private InputAction _reloadAction;
	private InputAction _shootAction;

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
		if (!IsActiveInstance || InputActions == null)
			return;

		DisablePlayerInput();
		DisableVisualNovelInput();
		UnsubscribeEvents();
	}

	private void SetupInputActions()
	{
		_continueStoryAction = InputActions.FindAction("ContinueStory");
		_escapeAction = InputActions.FindAction("Escape");
		_backlogAction = InputActions.FindAction("Backlog");
		_movementAction = InputActions.FindAction("Movement");
		_jumpAction = InputActions.FindAction("Jump");
		_dashAction = InputActions.FindAction("Dash");
		_shootAction = InputActions.FindAction("Shoot");
		_reloadAction = InputActions.FindAction("Reload");
		_crouchAction = InputActions.FindAction("Crouch");
		_changeGun = InputActions.FindAction("ChangeGun");
	}

	private void SubscribeEvents()
	{
		_continueStoryAction.performed += HandleContinueStory;
		_escapeAction.performed += HandleEscape;
		_backlogAction.performed += HandleBacklog;
		_jumpAction.performed += HandleJump;
		_dashAction.performed += HandleDash;
		_shootAction.performed += HandleShootPerformed;
		_shootAction.canceled += HandleShootReleased;
		_reloadAction.performed += HandleReload;
		_crouchAction.performed += HandleCrouchPerformed;
		_crouchAction.canceled += HandleCrouchReleased;
	}

	private void UnsubscribeEvents()
	{
		_continueStoryAction.performed -= HandleContinueStory;
		_escapeAction.performed -= HandleEscape;
		_backlogAction.performed -= HandleBacklog;
		_jumpAction.performed -= HandleJump;
		_dashAction.performed -= HandleDash;
		_shootAction.performed -= HandleShootPerformed;
		_shootAction.canceled -= HandleShootReleased;
		_reloadAction.performed -= HandleReload;
		_crouchAction.performed -= HandleCrouchPerformed;
		_crouchAction.canceled -= HandleCrouchReleased;
	}

	/** Update Methods **/
	private void Update()
	{
		if (InputActions == null)
			return;

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

	// Action Event Handlers (Using named methods avoids closure allocations)
	private void HandleContinueStory(InputAction.CallbackContext context) => OnContinueStoryPerformed?.Invoke();

	private void HandleEscape(InputAction.CallbackContext context) => OnEscapePerformed?.Invoke();

	private void HandleBacklog(InputAction.CallbackContext context) => OnBacklogPerformed?.Invoke();

	private void HandleJump(InputAction.CallbackContext context) => OnJumpPerformed?.Invoke();

	private void HandleDash(InputAction.CallbackContext context) => OnDashPerformed?.Invoke();

	private void HandleShootPerformed(InputAction.CallbackContext context) => OnShootingPerformed?.Invoke();

	private void HandleShootReleased(InputAction.CallbackContext context) => OnShootingReleased?.Invoke();

	private void HandleReload(InputAction.CallbackContext context) => OnReloadPerformed?.Invoke();

	private void HandleCrouchPerformed(InputAction.CallbackContext context) => OnCrouchPerformed?.Invoke();

	private void HandleCrouchReleased(InputAction.CallbackContext context) => OnCrouchRelease?.Invoke();

	private void HandleChangeGun(InputAction.CallbackContext context) => OnChangeGun?.Invoke();

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
	/// Enable VisualNovel Input
	/// </summary>
	public void EnableVisualNovelInput()
	{
		InputActions?.FindActionMap(VisualNovelActionMap)?.Enable();
	}

	/// <summary>
	/// Disable VisualNovel Input
	/// </summary>
	public void DisableVisualNovelInput()
	{
		InputActions?.FindActionMap(VisualNovelActionMap)?.Disable();
	}
}
