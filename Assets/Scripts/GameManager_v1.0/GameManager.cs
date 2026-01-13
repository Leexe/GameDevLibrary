using UnityEngine;

public class GameManager : MonoSingleton<GameManager>
{
	// Events
	public readonly ShootingEvents ShootingEventsRef = new();

	public void Start()
	{
		LockCursor();
	}

	private void LockCursor()
	{
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
	}

	private void UnlockCursor()
	{
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;
	}
}
