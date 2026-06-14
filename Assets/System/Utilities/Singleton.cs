using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Similar to a Singleton except it will override the current instance instead of destroying it
/// </summary>
public abstract class MonoStaticInstance<T> : SerializedMonoBehaviour
	where T : MonoBehaviour
{
	public static T Instance { get; private set; }

	protected virtual void Awake() => Instance = this as T;

	protected virtual void OnApplicationQuit()
	{
		Instance = null;
		Destroy(gameObject);
	}
}

/// <summary>
/// A Singleton that will destroy any existing instances and replace it with a new instance
/// </summary>
public abstract class MonoSingleton<T> : MonoStaticInstance<T>
	where T : MonoBehaviour
{
	protected sealed override void Awake()
	{
		if (Instance != null)
		{
			Destroy(gameObject);
			return;
		}
		base.Awake();
		OnInitialized();
	}

	/// <summary>
	/// Called only on the real singleton instance, never on duplicates
	/// Override this instead of Awake() in subclasses
	/// </summary>
	protected virtual void OnInitialized() { }

	/// <summary>
	/// True only for the real singleton instance, not for duplicates being destroyed
	/// </summary>
	protected bool IsActiveInstance => Instance != null && Instance == this;
}

/// <summary>
/// A Singleton that does not get destroyed on scene loads
/// </summary>
public abstract class PersistentMonoSingleton<T> : MonoSingleton<T>
	where T : MonoBehaviour
{
	protected override void OnInitialized()
	{
		transform.SetParent(null);
		DontDestroyOnLoad(gameObject);
	}
}
