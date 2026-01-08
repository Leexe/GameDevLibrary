using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

public class AudioManager : PersistentMonoSingleton<AudioManager>
{
	#region Fields

	private float _masterVolume = 1f;
	private float _musicVolume = 1f;
	private float _ambientVolume = 1f;
	private float _gameVolume = 1f;

	private Bus _masterBus;
	private Bus _musicBus;
	private Bus _ambientBus;
	private Bus _gameBus;

	private List<EventInstance> _eventInstances;

	private EventReference _currentMusicReference;

	// Music Instances
	private EventInstance _currentMusicTrack;
	private readonly Dictionary<FMOD.GUID, EventInstance> _musicTrackInstances = new();
	private readonly Dictionary<FMOD.GUID, EventInstance> _activeSnapshots = new();

	// Ambient Instances
	private EventInstance _currentAmbientTrack;
	private EventReference _currentAmbientReference;
	private readonly Dictionary<FMOD.GUID, EventInstance> _ambientTrackInstances = new();

	#endregion

	#region Lifecycle

	protected override void Awake()
	{
		base.Awake();

		_eventInstances = new List<EventInstance>();

		// Get Bus Names
		TryGetBus("bus:/", out _masterBus);
		TryGetBus("bus:/BGM", out _musicBus);
		TryGetBus("bus:/AMB", out _ambientBus);
		TryGetBus("bus:/SFX", out _gameBus);

		// Fetch audio preferences
		_masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
		_musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.7f);
		_ambientVolume = PlayerPrefs.GetFloat("AmbientVolume", 1f);
		_gameVolume = PlayerPrefs.GetFloat("GameVolume", 0.7f);

		// Apply audio preferences
		SetMasterVolume(_masterVolume);
		SetMusicVolume(_musicVolume);
		SetAmbienceVolume(_ambientVolume);
		SetGameVolume(_gameVolume);
	}

	private void OnDestroy()
	{
		CleanUp();
		SaveAudioPref();
	}

	/// <summary>
	/// Attempts to get an FMOD bus by path, logging a warning if not found.
	/// </summary>
	public bool TryGetBus(string busPath, out Bus bus)
	{
		try
		{
			bus = RuntimeManager.GetBus(busPath);
			return bus.isValid();
		}
		catch (System.Exception e)
		{
			Debug.LogWarning($"[AudioManager] Could not find bus '{busPath}': {e.Message}");
			bus = default;
			return false;
		}
	}

	/// <summary>
	/// Cleans up sound events and event emitters
	/// </summary>
	private void CleanUp()
	{
		foreach (EventInstance eventInstance in _eventInstances)
		{
			eventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
			eventInstance.release();
		}

		_musicTrackInstances.Clear();
		_ambientTrackInstances.Clear();
	}

	#endregion

	#region Music Track Control

	/// <summary>
	/// Play the current music track
	/// </summary>
	public void ResumeCurrentMusicTrack()
	{
		PlayInstance(_currentMusicTrack);
	}

	/// <summary>
	/// Pause the current music track
	/// </summary>
	public void PauseCurrentMusicTrack()
	{
		PauseInstance(_currentMusicTrack);
	}

	/// <summary>
	/// Stop the current music track
	/// </summary>
	public void StopCurrentMusicTrack(bool fadeOut = true)
	{
		StopInstance(_currentMusicTrack, fadeOut);
	}

	/// <summary>
	/// Plays the music track
	/// </summary>
	public void SwitchMusicTrack(EventReference musicTrack, bool playOnSwitch = true)
	{
		SwitchTrack(musicTrack, ref _currentMusicReference, ref _currentMusicTrack, _musicTrackInstances);
		if (playOnSwitch)
		{
			ResumeCurrentMusicTrack();
		}
	}

	#endregion

	#region Ambient Track Control

	/// <summary>
	/// Play the current ambient track
	/// </summary>
	public void PlayCurrentAmbientTrack()
	{
		PlayInstance(_currentAmbientTrack);
	}

	/// <summary>
	/// Pause the current ambient track
	/// </summary>
	public void PauseCurrentAmbientTrack()
	{
		StopInstance(_currentAmbientTrack);
	}

	/// <summary>
	/// Stop the current ambient track
	/// </summary>
	public void StopCurrentAmbientTrack(bool fadeOut = true)
	{
		StopInstance(_currentAmbientTrack, fadeOut);
	}

	/// <summary>
	/// Switches the ambient track
	/// </summary>
	public void SwitchAmbienceTrack(EventReference ambienceTrack, bool playOnSwitch = true)
	{
		SwitchTrack(ambienceTrack, ref _currentAmbientReference, ref _currentAmbientTrack, _ambientTrackInstances);
		if (playOnSwitch)
		{
			ResumeCurrentMusicTrack();
		}
	}

	/// <summary>
	/// Generic helper to switch tracks for music or ambience.
	/// </summary>
	private void SwitchTrack(
		EventReference newTrack,
		ref EventReference currentReference,
		ref EventInstance currentInstance,
		Dictionary<FMOD.GUID, EventInstance> instanceCache
	)
	{
		if (newTrack.Guid == currentReference.Guid)
		{
			return; // Already playing this track
		}

		if (currentInstance.isValid())
		{
			currentInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
		}

		currentReference = newTrack;

		if (!instanceCache.ContainsKey(newTrack.Guid))
		{
			instanceCache[newTrack.Guid] = CreateInstance(newTrack);
		}

		currentInstance = instanceCache[newTrack.Guid];
	}

	#endregion

	#region One-Shots

	/// <summary>
	/// Plays a sound effect
	/// </summary>
	/// <param name="sound">The FMOD event reference of the sound</param>
	public void PlayOneShot(EventReference sound)
	{
		RuntimeManager.PlayOneShot(sound);
	}

	/// <summary>
	/// Plays a sound effect attached to a game object
	/// </summary>
	/// <param name="sound">The FMOD event reference of the sound</param>
	/// <param name="gameObjectRef">The game object to be attached to</param>
	public void PlayOneShot(EventReference sound, GameObject gameObjectRef)
	{
		RuntimeManager.PlayOneShotAttached(sound, gameObjectRef);
	}

	/// <summary>
	/// Plays a sound effect at some position in the world
	/// </summary>
	/// <param name="sound">The FMOD event reference of the sound</param>
	/// <param name="pos">Some position in the world</param>
	public void PlayOneShot(EventReference sound, Vector3 pos)
	{
		RuntimeManager.PlayOneShot(sound, pos);
	}

	/// <summary>
	/// Plays a sound effect with a custom pitch
	/// </summary>
	/// <param name="sound">The FMOD event reference of the sound</param>
	/// <param name="pitch">Pitch multiplier (0.5 = half, 1.0 = normal, 2.0 = double)</param>
	public void PlayOneShotWithPitch(EventReference sound, float pitch = 1f)
	{
		EventInstance instance = RuntimeManager.CreateInstance(sound);
		instance.setPitch(pitch);
		instance.start();
		instance.release();
	}

	/// <summary>
	/// Plays a sound effect with a random pitch within a range (fire-and-forget)
	/// </summary>
	/// <param name="sound">The FMOD event reference of the sound</param>
	/// <param name="minPitch">Minimum pitch multiplier</param>
	/// <param name="maxPitch">Maximum pitch multiplier</param>
	public void PlayOneShotWithPitch(EventReference sound, float minPitch, float maxPitch)
	{
		float randomPitch = Random.Range(minPitch, maxPitch);
		PlayOneShotWithPitch(sound, randomPitch);
	}

	#endregion

	#region Event Instance

	/// <summary>
	/// Creates an event instance of a sound, which allows a sound to be looped, or it's FMOD parameters to be modified
	/// </summary>
	/// <param name="sound">The FMOD event reference of the sound</param>
	/// <returns>Returns a cached event instance of the sound, that can be used to modify the sound at runtime</returns>
	public EventInstance CreateInstance(EventReference sound)
	{
		EventInstance eventInstance = RuntimeManager.CreateInstance(sound);
		eventInstance.setTimelinePosition(0);
		_eventInstances.Add(eventInstance);
		return eventInstance;
	}

	/// <summary>
	/// Creates an event instance of a sound and attaches it to a game object
	/// </summary>
	/// <param name="sound">The FMOD event reference of the sound</param>
	/// <param name="gameObjectRef">The game object to attach to</param>
	/// <returns>Returns a cached event instance of the sound, that can be used to modify the sound at runtime</returns>
	public EventInstance CreateInstance(EventReference sound, GameObject gameObjectRef)
	{
		EventInstance eventInstance = RuntimeManager.CreateInstance(sound);
		RuntimeManager.AttachInstanceToGameObject(eventInstance, gameObjectRef);
		_eventInstances.Add(eventInstance);
		return eventInstance;
	}

	/// <summary>
	/// Delete an event instance, pausing and freeing it from memory
	/// </summary>
	public void DeleteInstance(EventInstance eventInstance)
	{
		_eventInstances.Remove(eventInstance);
		eventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
		eventInstance.release();
	}

	/// <summary>
	/// Free up an event instance sound
	/// </summary>
	public void DestroyInstance(EventInstance instance)
	{
		instance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
		instance.release();
		_eventInstances.Remove(instance);
	}

	#endregion

	#region Instance Playback

	/// <summary>
	/// Play an event instance sound at the start of the instance
	/// </summary>
	public void PlayInstanceAtStart(EventInstance eventInstance)
	{
		eventInstance.setTimelinePosition(0);
		eventInstance.start();
	}

	/// <summary>
	/// Resume playing the event instance sound
	/// </summary>
	public void PlayInstance(EventInstance instance)
	{
		// If the track is playing, do nothing
		instance.getPlaybackState(out PLAYBACK_STATE state);
		if (state == PLAYBACK_STATE.PLAYING)
		{
			return;
		}

		// If the track is paused, unpause it
		instance.getPaused(out bool isPaused);
		if (isPaused)
		{
			instance.setPaused(false);
		}
		// If the track was never played, start it
		else
		{
			instance.start();
		}
	}

	/// <summary>
	/// Pauses an event instance sound
	/// </summary>
	/// <param name="instance">The event instance sound</param>
	public void PauseInstance(EventInstance instance)
	{
		instance.setPaused(true);
	}

	/// <summary>
	/// Stops an event instance sound
	/// </summary>
	/// <param name="instance">The event instance sound</param>
	/// <param name="allowFadeOut">Allow the sound to fade out or not</param>
	public void StopInstance(EventInstance instance, bool allowFadeOut = true)
	{
		instance.stop(allowFadeOut ? FMOD.Studio.STOP_MODE.ALLOWFADEOUT : FMOD.Studio.STOP_MODE.IMMEDIATE);
	}

	/// <summary>
	/// Returns a boolean indicating if an event instance sound is playing
	/// </summary>
	public bool InstanceIsPlaying(EventInstance instance)
	{
		instance.getPlaybackState(out PLAYBACK_STATE state);
		return state != PLAYBACK_STATE.STOPPED;
	}

	/// <summary>
	/// Sets the pitch of an event instance
	/// </summary>
	/// <param name="instance">The event instance to modify</param>
	/// <param name="pitch">Pitch multiplier (0.5 = half, 1.0 = normal, 2.0 = double)</param>
	public void SetInstancePitch(EventInstance instance, float pitch)
	{
		instance.setPitch(pitch);
	}

	#endregion

	#region Parameters

	/// <summary>
	/// Sets a global parameter by name
	/// </summary>
	/// <param name="parameterName">The name of the parameter</param>
	/// <param name="value">The value of the parameter</param>
	public void SetGlobalParameter(string parameterName, float value)
	{
		RuntimeManager.StudioSystem.setParameterByName(parameterName, value);
	}

	/// <summary>
	/// Sets a parameter of an event instance sound by name
	/// </summary>
	/// <param name="instance">The event instance sound</param>
	/// <param name="parameterName">The name of the parameter</param>
	/// <param name="value">The value of the parameter</param>
	public void SetInstanceParameter(EventInstance instance, string parameterName, float value)
	{
		instance.setParameterByName(parameterName, value);
	}

	/// <summary>
	/// Sets a parameter of an event instance sound by name
	/// </summary>
	/// <param name="instance">The event instance sound</param>
	/// <param name="parameterID">The parameter ID of the parameter</param>
	/// <param name="value">The value of the parameter</param>
	public void SetInstanceParameter(EventInstance instance, PARAMETER_ID parameterID, float value)
	{
		instance.setParameterByID(parameterID, value);
	}

	#endregion

	#region Snapshots

	/// <summary>
	/// Sets the state of a snapshot
	/// </summary>
	/// <param name="snapshotReference">The snapshot reference</param>
	/// <param name="isActive">Whether the snapshot is active or not</param>
	public void SetSnapshotState(EventReference snapshotReference, bool isActive)
	{
		if (!_activeSnapshots.ContainsKey(snapshotReference.Guid))
		{
			EventInstance snapshot = CreateInstance(snapshotReference);
			_activeSnapshots[snapshotReference.Guid] = snapshot;
		}

		EventInstance instance = _activeSnapshots[snapshotReference.Guid];
		if (isActive)
		{
			instance.start();
		}
		else
		{
			instance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
		}
	}

	#endregion

	#region Volume Control

	/// <summary>
	/// Saves audio level preferences to player prefs
	/// </summary>
	public void SaveAudioPref()
	{
		PlayerPrefs.SetFloat("MasterVolume", _masterVolume);
		PlayerPrefs.SetFloat("MusicVolume", _musicVolume);
		PlayerPrefs.SetFloat("AmbientVolume", _ambientVolume);
		PlayerPrefs.SetFloat("GameVolume", _gameVolume);
	}

	public float GetMasterVolume()
	{
		return _masterVolume;
	}

	public float GetMusicVolume()
	{
		return _musicVolume;
	}

	public float GetAmbienceVolume()
	{
		return _ambientVolume;
	}

	public float GetGameVolume()
	{
		return _gameVolume;
	}

	public void SetMasterVolume(float masterVolume)
	{
		_masterVolume = Mathf.Clamp(masterVolume, 0f, 1f);
		if (_masterBus.isValid())
		{
			_masterBus.setVolume(_masterVolume);
		}
	}

	public void SetMusicVolume(float musicVolume)
	{
		_musicVolume = Mathf.Clamp(musicVolume, 0f, 1f);
		if (_musicBus.isValid())
		{
			_musicBus.setVolume(_musicVolume);
		}
	}

	public void SetGameVolume(float gameVolume)
	{
		_gameVolume = Mathf.Clamp(gameVolume, 0f, 1f);
		if (_gameBus.isValid())
		{
			_gameBus.setVolume(_gameVolume);
		}
	}

	public void SetAmbienceVolume(float ambientVolume)
	{
		_ambientVolume = Mathf.Clamp(ambientVolume, 0f, 1f);
		if (_ambientBus.isValid())
		{
			_ambientBus.setVolume(_ambientVolume);
		}
	}

	#endregion
}
