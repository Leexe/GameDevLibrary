using UnityEngine;

/// <summary>
/// A singleton that overrides the current instance instead of destroying it.
/// </summary>
public abstract class MonoStaticInstance<T> : MonoBehaviour
	where T : MonoBehaviour
{
	public static T Instance { get; private set; }

	protected virtual void Awake()
	{
		Instance = this as T;
	}

	protected virtual void OnApplicationQuit()
	{
		Instance = null;
		Destroy(gameObject);
	}
}

/// <summary>
/// A standard singleton that destroys any new instances if one already exists.
/// </summary>
public abstract class MonoSingleton<T> : MonoStaticInstance<T>
	where T : MonoBehaviour
{
	protected override void Awake()
	{
		if (Instance != null)
		{
			Destroy(gameObject);
			return;
		}

		base.Awake();
	}
}

/// <summary>
/// A persistent singleton that survives scene loads.
/// </summary>
public abstract class PersistentMonoSingleton<T> : MonoSingleton<T>
	where T : MonoBehaviour
{
	protected override void Awake()
	{
		base.Awake();
		DontDestroyOnLoad(gameObject);
	}
}

/// <summary>
/// A standard C# Singleton.
/// </summary>
public abstract class Singleton<T>
	where T : class
{
	private static T _instance;

	// ReSharper disable once StaticMemberInGenericType
	// We want seperate locks for each singleton
	private static readonly object PadLock = new();

	public static T Instance
	{
		get
		{
			lock (PadLock)
			{
				_instance ??= (T)System.Activator.CreateInstance(typeof(T), true);

				return _instance;
			}
		}
	}
}
