using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class LevelManager : PersistentMonoSingleton<LevelManager>
{
	public enum SceneNames
	{
		Loading,
		MainMenu,
		Game,
	}

	[SerializeField]
	private bool _debugMessages;

	// Events
	[HideInInspector]
	public UnityEvent OnGamePaused;

	[HideInInspector]
	public UnityEvent OnGameResume;

	[HideInInspector]
	public UnityEvent OnSceneReady;

	// Private Variables
	private AsyncOperation _asyncOperation;

	private void OnEnable()
	{
		SceneManager.sceneLoaded += SceneLoaded;
	}

	private void OnDisable()
	{
		SceneManager.sceneLoaded -= SceneLoaded;
	}

	private void SceneLoaded(Scene scene, LoadSceneMode mode)
	{
		RemoveDuplicateEventSystems(scene);

		if (scene.buildIndex == (int)SceneNames.Game)
		{
			HideCursor();
		}
		else
		{
			ShowCursor();
		}
	}

	/// <summary>
	/// Destroys duplicate EventSystems that are not in the current active scene
	/// </summary>
	private void RemoveDuplicateEventSystems(Scene currentScene)
	{
		EventSystem[] eventSystems = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
		foreach (EventSystem es in eventSystems)
		{
			if (es.gameObject.scene != currentScene && es.gameObject.scene.name != "DontDestroyOnLoad")
			{
				Destroy(es.gameObject);
			}
		}
	}

	/// <summary>
	/// Exits game by quitting the application
	/// </summary>
	public void ExitGame()
	{
		Application.Quit();
	}

	/// <summary>
	/// Shows the cursor and unlocks it
	/// </summary>
	public void ShowCursor()
	{
		Cursor.visible = true;
		Cursor.lockState = CursorLockMode.None;
	}

	/// <summary>
	/// Hides the cursor and locks it to the center of the screen
	/// </summary>
	public void HideCursor()
	{
		Cursor.visible = false;
		Cursor.lockState = CursorLockMode.Locked;
	}

	/// <summary>
	/// Loads up the scene asynchronously
	/// </summary>
	/// <param name="sceneName"></param>
	public void LoadSceneAsync(SceneNames sceneName)
	{
		StartCoroutine(LoadSceneAsyncEnumerator(sceneName));
	}

	/// <summary>
	/// Helper method for loading up a scene asynchronously
	/// </summary>
	/// <param name="sceneName">Scene Name To Load</param>
	private IEnumerator LoadSceneAsyncEnumerator(SceneNames sceneName)
	{
		// Start loading the scene asynchronously in the background
		_asyncOperation = SceneManager.LoadSceneAsync((int)sceneName, LoadSceneMode.Single);

		if (_asyncOperation != null)
		{
			// Prevent the scene from activating and displaying immediately
			_asyncOperation.allowSceneActivation = false;

			while (!_asyncOperation.isDone)
			{
				float progress = Mathf.Clamp01(_asyncOperation.progress / 0.9f);
				if (_debugMessages)
				{
					Debug.Log("Loading progress: " + (progress * 100) + "%");
				}

				if (_asyncOperation.progress >= 0.9f)
				{
					if (_debugMessages)
					{
						Debug.Log("Scene fully preloaded");
					}

					OnSceneReady?.Invoke();
					break;
				}

				yield return null;
			}
		}
	}

	public void UnloadSceneAsync(SceneNames sceneName)
	{
		if (SceneManager.GetSceneByBuildIndex((int)sceneName).isLoaded)
		{
			SceneManager.UnloadSceneAsync((int)sceneName);
		}
	}

	/// <summary>
	/// Activates the preloaded scene
	/// </summary>
	public void ActivatePreloadedScene()
	{
		if (_asyncOperation is { progress: >= 0.9f })
		{
			_asyncOperation.allowSceneActivation = true;
		}
	}

	/// <summary>
	/// Switches to a different scene
	/// </summary>
	/// <param name="sceneName">Scene Name To Switch To</param>
	public void SwitchScenes(SceneNames sceneName)
	{
		SceneManager.LoadScene((int)sceneName);
	}
}
