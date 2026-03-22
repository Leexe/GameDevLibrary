using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using FMOD;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

public class AudioManager : PersistentMonoSingleton<AudioManager>
{
	#region Fields

	public enum AudioBusType
	{
		Master,
		Music,
		Ambience,
		Game,
	}

	// A small non-zero value to prevent FMOD from virtualizing events when volume is zero.
	// This ensures the audio visualizer keeps receiving spectrum data.
	private const float MinVcaVolume = 0.0001f;

	[SerializeField]
	private bool _initVisualization = true;

	private float _masterVolume = 1f;
	private float _musicVolume = 0.7f;
	private float _ambientVolume = 0.7f;
	private float _gameVolume = 0.7f;

	private Bus _masterBus;
	private Bus _musicBus;
	private Bus _ambientBus;
	private Bus _gameBus;

	private VCA _masterVca;
	private VCA _musicVca;
	private VCA _ambientVca;
	private VCA _gameVca;

	private List<EventInstance> _eventInstances;
	private EventReference _currentMusicReference;
	private int _channelsInitialized;

	// Music Instances
	private EventInstance _musicTrack;
	private readonly Dictionary<GUID, EventInstance> _musicTrackInstances = new();
	private readonly Dictionary<GUID, EventInstance> _activeSnapshots = new();

	// Ambient Instances
	private EventInstance _ambientTrack;
	private EventReference _ambientReference;
	private readonly Dictionary<GUID, EventInstance> _ambientTrackInstances = new();

	// Audio Visualization
	private readonly Dictionary<AudioBusType, DSP> _fftDsps = new();
	private readonly Dictionary<AudioBusType, ChannelGroup> _channelGroups = new();
	private int _fftWindowSize;
	private float[] _spectrumData;
	private bool _spectrumUpdatedThisFrame;
	private bool _startedVisualization;

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

		// Get VCAs
		TryGetVca("vca:/Master", out _masterVca);
		TryGetVca("vca:/BGM", out _musicVca);
		TryGetVca("vca:/AMB", out _ambientVca);
		TryGetVca("vca:/SFX", out _gameVca);

		// Fetch audio preferences
		_masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
		_musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.7f);
		_ambientVolume = PlayerPrefs.GetFloat("AmbientVolume", 1f);
		_gameVolume = PlayerPrefs.GetFloat("GameVolume", 0.7f);

		// Apply audio preferences
		foreach (AudioBusType type in Enum.GetValues(typeof(AudioBusType)))
		{
			SetVolume(type, GetVolume(type));
		}
	}

	private void Start()
	{
		// Initialize Audio Visualization
		if (_initVisualization)
		{
			InitializeVisualization();
		}
	}

	private void OnDestroy()
	{
		CleanUp();
		SaveAudioPref();
	}

	private void Update()
	{
		if (IsVisualizationReady)
		{
			_spectrumUpdatedThisFrame = false;
		}
	}

	/// <summary>
	/// Attempts to get an FMOD bus by path, logging a warning if not found
	/// </summary>
	public bool TryGetBus(string busPath, out Bus bus)
	{
		try
		{
			bus = RuntimeManager.GetBus(busPath);
			return bus.isValid();
		}
		catch (Exception e)
		{
			Debug.LogWarning($"[AudioManager] Could not find bus '{busPath}': {e.Message}");
			bus = default;
			return false;
		}
	}

	/// <summary>
	/// Attempts to get an FMOD VCA by path, logging a warning if not found
	/// </summary>
	public bool TryGetVca(string vcaPath, out VCA vca)
	{
		try
		{
			vca = RuntimeManager.GetVCA(vcaPath);
			return vca.isValid();
		}
		catch (Exception e)
		{
			Debug.LogWarning($"[AudioManager] Could not find VCA '{vcaPath}': {e.Message}");
			vca = default;
			return false;
		}
	}

	/// <summary>
	/// Cleans up sound events and event emitters
	/// </summary>
	private void CleanUp()
	{
		CleanUpVisualization();

		foreach (EventInstance eventInstance in _eventInstances)
		{
			eventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
			eventInstance.release();
		}

		_musicTrackInstances.Clear();
		_ambientTrackInstances.Clear();
	}

	#endregion

	#region Music Control

	/// <summary>
	/// Play the primary music track
	/// </summary>
	public void ResumeMusic()
	{
		PlayInstance(_musicTrack);
	}

	/// <summary>
	/// Pause the primary music track
	/// </summary>
	public void PauseMusic()
	{
		PauseInstance(_musicTrack);
	}

	/// <summary>
	/// Stop the primary music track
	/// </summary>
	public void StopMusic(bool fadeOut = true)
	{
		StopInstance(_musicTrack, fadeOut);
	}

	/// <summary>
	/// Switches the music track
	/// </summary>
	public void SwitchMusic(EventReference music, bool playOnSwitch = true)
	{
		SwitchTrack(music, ref _currentMusicReference, ref _musicTrack, _musicTrackInstances);
		if (playOnSwitch)
		{
			ResumeMusic();
		}
	}

	#endregion

	#region Ambient Control

	/// <summary>
	/// Play the primary ambient track
	/// </summary>
	public void ResumeAmbience()
	{
		PlayInstance(_ambientTrack);
	}

	/// <summary>
	/// Pause the primary ambient track
	/// </summary>
	public void PauseAmbience()
	{
		PauseInstance(_ambientTrack);
	}

	/// <summary>
	/// Stop the primary ambient track
	/// </summary>
	public void StopAmbience(bool fadeOut = true)
	{
		StopInstance(_ambientTrack, fadeOut);
	}

	/// <summary>
	/// Switches the ambient track
	/// </summary>
	public void SwitchAmbience(EventReference ambience, bool playOnSwitch = true)
	{
		SwitchTrack(ambience, ref _ambientReference, ref _ambientTrack, _ambientTrackInstances);
		if (playOnSwitch)
		{
			ResumeAmbience();
		}
	}

	/// <summary>
	/// Generic helper to switch tracks for music or ambience.
	/// </summary>
	private void SwitchTrack(
		EventReference newTrack,
		ref EventReference currentReference,
		ref EventInstance currentInstance,
		Dictionary<GUID, EventInstance> instanceCache
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
	/// Plays a sound effect with a specific volume
	/// </summary>
	/// <param name="sound">The FMOD event reference of the sound</param>
	/// <param name="volume">Volume from 0 to 1</param>
	public void PlayOneShot(EventReference sound, float volume)
	{
		EventInstance instance = RuntimeManager.CreateInstance(sound);
		instance.setVolume(volume);
		instance.start();
		instance.release();
	}

	/// <summary>
	/// Plays a sound effect at some position in the world with a specific volume
	/// </summary>
	/// <param name="sound">The FMOD event reference of the sound</param>
	/// <param name="pos">Some position in the world</param>
	/// <param name="volume">Volume from 0 to 1</param>
	public void PlayOneShot(EventReference sound, Vector3 pos, float volume)
	{
		EventInstance instance = RuntimeManager.CreateInstance(sound);
		instance.set3DAttributes(pos.To3DAttributes());
		instance.setVolume(volume);
		instance.start();
		instance.release();
	}

	/// <summary>
	/// Plays a sound effect with a custom pitch
	/// </summary>
	/// <param name="sound">The FMOD event reference of the sound</param>
	/// <param name="pitch">Pitch multiplier (0.5 = half, 1.0 = normal, 2.0 = double)</param>
	/// <param name="volume">How loud to play the sound (0-1)</param>
	public void PlayOneShotWithPitch(EventReference sound, float pitch = 1f, float volume = 1f)
	{
		EventInstance instance = RuntimeManager.CreateInstance(sound);
		instance.setPitch(pitch);
		instance.setVolume(volume);
		instance.start();
		instance.release();
	}

	/// <summary>
	/// Plays a sound effect with a random pitch within a range (fire-and-forget)
	/// </summary>
	/// <param name="sound">The FMOD event reference of the sound</param>
	/// <param name="minPitch">Minimum pitch multiplier</param>
	/// <param name="maxPitch">Maximum pitch multiplier</param>
	/// <param name="volume">How loud to play the sound (0-1)</param>
	public void PlayOneShotRandomPitch(EventReference sound, float minPitch, float maxPitch, float volume = 1f)
	{
		float randomPitch = Random.Range(minPitch, maxPitch);
		PlayOneShotWithPitch(sound, randomPitch, volume);
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
	/// <param name="snapshot">The snapshot reference</param>
	/// <param name="isActive">Whether the snapshot is active or not</param>
	public void SetSnapshotState(EventReference snapshot, bool isActive)
	{
		if (!_activeSnapshots.ContainsKey(snapshot.Guid))
		{
			EventInstance instance = CreateInstance(snapshot);
			_activeSnapshots[snapshot.Guid] = instance;
		}

		EventInstance activeSnapshot = _activeSnapshots[snapshot.Guid];
		if (isActive)
		{
			activeSnapshot.start();
		}
		else
		{
			activeSnapshot.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
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

	public float GetVolume(AudioBusType type)
	{
		return type switch
		{
			AudioBusType.Master => _masterVolume,
			AudioBusType.Music => _musicVolume,
			AudioBusType.Ambience => _ambientVolume,
			AudioBusType.Game => _gameVolume,
			_ => 0f,
		};
	}

	public void SetVolume(AudioBusType type, float volume)
	{
		float clampedVolume = Mathf.Clamp(volume, 0f, 1f);
		clampedVolume = _startedVisualization ? Mathf.Max(Mathf.Clamp(volume, 0f, 1f), MinVcaVolume) : clampedVolume;

		switch (type)
		{
			case AudioBusType.Master:
				_masterVolume = clampedVolume;
				if (_masterVca.isValid())
				{
					_masterVca.setVolume(clampedVolume);
				}
				break;
			case AudioBusType.Music:
				_musicVolume = clampedVolume;
				if (_musicVca.isValid())
				{
					_musicVca.setVolume(clampedVolume);
				}
				break;
			case AudioBusType.Ambience:
				_ambientVolume = clampedVolume;
				if (_ambientVca.isValid())
				{
					_ambientVca.setVolume(clampedVolume);
				}
				break;
			case AudioBusType.Game:
				_gameVolume = clampedVolume;
				if (_gameVca.isValid())
				{
					_gameVca.setVolume(clampedVolume);
				}
				break;
		}
	}

	#endregion

	#region Audio Visualization

	public bool IsVisualizationReady => _channelsInitialized == 4;

	/// <summary>
	/// Creates the Fast Fourier Transform (FFT) Digital Signal Processors (DSPs) and attaches them to the corresponding channel groups (Master, Music, Ambience, Game)
	/// </summary>
	/// <param name="windowSize">FFT window size (power of 2). Larger = more frequency detail, more latency.</param>
	public void InitializeVisualization(int windowSize = 1024)
	{
		if (_startedVisualization)
		{
			Debug.LogWarning("Visualization already initialized.");
			return;
		}

		_fftWindowSize = windowSize;

		// Set each volume level to be non-zero to prevent FMOD from optimizing the bus out
		foreach (AudioBusType busType in Enum.GetValues(typeof(AudioBusType)))
		{
			StartCoroutine(InitializeDSPForBus(busType));
			SetVolume(busType, GetVolume(busType));
		}

		_spectrumData = new float[windowSize];
		_startedVisualization = true;
	}

	private IEnumerator InitializeDSPForBus(AudioBusType busType)
	{
		Bus bus = GetBusByType(busType);
		if (!bus.isValid())
		{
			Debug.LogWarning($"Cannot initialize DSP for {busType}: Bus invalid.");
			yield break;
		}

		// Lock Bus
		RESULT result = bus.lockChannelGroup();
		if (result != RESULT.OK)
		{
			Debug.Log($"Failed to lock {busType}: {result}");
		}

		// Flush commands to ensure the lock command is being called
		RuntimeManager.StudioSystem.flushCommands();

		// Get the channel group
		ChannelGroup channelGroup;
		while (bus.getChannelGroup(out channelGroup) != RESULT.OK)
		{
			yield return null;
		}

		// Create the FFT DSP
		result = RuntimeManager.CoreSystem.createDSPByType(DSP_TYPE.FFT, out DSP fftDsp);
		if (result != RESULT.OK)
		{
			Debug.LogError($"[AudioManager] Failed to create FFT DSP for {busType}: {result}");
			yield break;
		}

		// Set parameters for the FFT DSP
		fftDsp.setParameterInt((int)DSP_FFT.WINDOWSIZE, _fftWindowSize);
		fftDsp.setParameterInt((int)DSP_FFT.WINDOW, (int)DSP_FFT_WINDOW_TYPE.HANNING);
		fftDsp.setMeteringEnabled(false, true);

		// Attach DSP to the channel group
		result = channelGroup.addDSP(CHANNELCONTROL_DSP_INDEX.TAIL, fftDsp);
		if (result != RESULT.OK)
		{
			Debug.LogError($"[AudioManager] Failed to add FFT DSP to {busType} channel group: {result}");
			fftDsp.release();
			yield break;
		}

		_fftDsps[busType] = fftDsp;
		_channelGroups[busType] = channelGroup;
		_channelsInitialized++;
	}

	/// <summary>
	/// Removes and releases all FFT DSPs
	/// </summary>
	public void CleanUpVisualization()
	{
		if (!IsVisualizationReady)
		{
			return;
		}

		foreach ((AudioBusType busType, DSP fftDsp) in _fftDsps)
		{
			Bus bus = GetBusByType(busType);

			// Unlock the bus
			bus.unlockChannelGroup();

			// From the DSP attached to the channel
			if (_channelGroups.TryGetValue(busType, out ChannelGroup channelGroup) && channelGroup.hasHandle())
			{
				channelGroup.removeDSP(fftDsp);
			}

			// Release the DSP
			fftDsp.release();
		}

		// If volumes are set to 0, FMOD will optimize the bus out of the audio graph
		foreach (AudioBusType type in Enum.GetValues(typeof(AudioBusType)))
		{
			SetVolume(type, GetVolume(type));
		}

		_fftDsps.Clear();
		_channelGroups.Clear();
		_startedVisualization = false;
		_channelsInitialized = 0;
	}

	/// <summary>
	/// Gets the frequency peaks for the given number of buckets and spectrum size
	/// </summary>
	/// <param name="bucketSizes">An array of bucket sizes, calculated by <see cref="GetBucketSizes"/></param>
	/// <param name="numBuckets">The number of buckets</param>
	/// <param name="frequencyPeaks">An array of frequency peaks</param>
	/// <param name="audioBusType">The audio bus type (Master, Music, Ambience, Game)</param>
	/// <param name="scaleFactor">The scale factor that every frequency peak will be multiplied by</param>
	/// <param name="minPeak">The minimum peak</param>
	/// <param name="higherFrequencyBoost">The frequency boost to the higher frequencies, a small value (~0.05)</param>
	/// <param name="spectrumCutoff">The cutoff frequency for the spectrum data, cuts off the high frequencies</param>
	public bool GetFrequencyPeaks(
		int numBuckets,
		ref float[] frequencyPeaks,
		ref int[] bucketSizes,
		AudioBusType audioBusType = AudioBusType.Master,
		float scaleFactor = 5f,
		float minPeak = 0f,
		float higherFrequencyBoost = 0.05f,
		float spectrumCutoff = 0.75f
	)
	{
		float[] spectrumData = GetSpectrumData(audioBusType);
		if (spectrumData == null)
		{
			return false;
		}

		if (bucketSizes == null || bucketSizes.Length != numBuckets + 1)
		{
			bucketSizes = GetBucketSizes(numBuckets, (int)(GetSpectrumLength() * spectrumCutoff));
		}

		if (frequencyPeaks == null || frequencyPeaks.Length != numBuckets)
		{
			frequencyPeaks = new float[numBuckets];
		}

		for (int i = 0; i < numBuckets; i++)
		{
			int startIndex = bucketSizes[i];
			int endIndex = bucketSizes[i + 1];
			float peakValue = 0;

			for (int j = startIndex; j <= endIndex && j < spectrumData.Length; j++)
			{
				if (spectrumData[j] > peakValue)
				{
					peakValue = spectrumData[j];
				}
			}

			// Audio falls off at higher frequencies, so we boost higher buckets
			float frequencyBoost = 1.0f + (startIndex * higherFrequencyBoost);
			float compressedPeak = Mathf.Sqrt(peakValue);

			frequencyPeaks[i] = (compressedPeak * frequencyBoost * scaleFactor) + minPeak;
		}

		return true;
	}

	/// <summary>
	/// Gets the bucket sizes for the given number of buckets and spectrum size
	/// </summary>
	/// <param name="numBuckets">The number of buckets</param>
	/// <param name="spectrumSize">The spectrum size</param>
	private int[] GetBucketSizes(int numBuckets, int spectrumSize)
	{
		// Remove DC component (index 0)
		spectrumSize--;
		int[] bucketSizes = new int[numBuckets + 1];
		bucketSizes[0] = 1;

		float exponent = 2.0f;

		for (int i = 1; i < numBuckets + 1; i++)
		{
			float t = (float)i / numBuckets;
			bucketSizes[i] = (int)(spectrumSize * Mathf.Pow(t, exponent)) + 1;
		}

		return bucketSizes;
	}

	/// <summary>
	/// Returns the FFT spectrum data for the given bus and channel
	/// Call this every frame from the visualizer
	/// </summary>
	/// <param name="busType">The audio bus to visualize (Master, Music, Ambience, Game)</param>
	/// <param name="channel">Audio channel index (0 = left, 1 = right)</param>
	/// <returns>Array of spectrum bins, or null if not ready or no data available</returns>
	public float[] GetSpectrumData(AudioBusType busType = AudioBusType.Master, int channel = 0)
	{
		if (!IsVisualizationReady)
		{
			return null;
		}

		if (_spectrumUpdatedThisFrame)
		{
			return _spectrumData;
		}

		// Get the FFT DSP for the given bus type
		if (!_fftDsps.TryGetValue(busType, out DSP fftDsp) || !fftDsp.hasHandle())
		{
			return null;
		}

		// Get the spectrum data from the DSP, returns a pointer to the data in unmanagedData
		RESULT result = fftDsp.getParameterData((int)DSP_FFT.SPECTRUMDATA, out IntPtr unmanagedData, out uint _);

		if (result != RESULT.OK || unmanagedData == IntPtr.Zero)
		{
			return null;
		}

		// Go to the address from unmanagedData and create the data structure
		DSP_PARAMETER_FFT fftData = Marshal.PtrToStructure<DSP_PARAMETER_FFT>(unmanagedData);

		if (fftData.numchannels == 0)
		{
			return null;
		}

		int ch = Mathf.Clamp(channel, 0, fftData.numchannels - 1);
		_spectrumData = fftData.spectrum[ch];
		_spectrumUpdatedThisFrame = true;
		return _spectrumData;
	}

	/// <summary>
	/// Returns the number of spectrum bins for the current FFT window size
	/// </summary>
	public int GetSpectrumLength()
	{
		if (!IsVisualizationReady)
		{
			return 0;
		}

		return (_fftWindowSize / 2) + 1;
	}

	/// <summary>
	/// Returns the current Root Mean Square (RMS) or average loudness level for a bus
	/// </summary>
	/// <param name="busType">The AudioBusType to meter</param>
	/// <returns>Normalized RMS value (0-1), or 0 if unavailable</returns>
	public float GetRms(AudioBusType busType)
	{
		if (!IsVisualizationReady)
		{
			Debug.LogWarning("Visualization has not been initialized");
			return 0f;
		}

		Bus bus = GetBusByType(busType);

		// Get the channel group of bus
		RESULT result = bus.getChannelGroup(out ChannelGroup channelGroup);
		if (result != RESULT.OK || !channelGroup.hasHandle())
		{
			return 0f;
		}

		// Get the head DSP of the channel group
		result = channelGroup.getDSP(CHANNELCONTROL_DSP_INDEX.HEAD, out DSP headDsp);
		if (result != RESULT.OK)
		{
			return 0f;
		}

		// Get the metering info from the head DSP
		result = headDsp.getMeteringInfo(IntPtr.Zero, out DSP_METERING_INFO outputMetering);
		if (result != RESULT.OK || outputMetering.numchannels == 0)
		{
			return 0f;
		}

		// Calculate the Root Mean Square value
		float sum = 0f;
		for (int i = 0; i < outputMetering.numchannels; i++)
		{
			sum += outputMetering.rmslevel[i];
		}

		return sum / outputMetering.numchannels;
	}

	/// <summary>
	/// Returns the current peak level or maximum loudness level for a bus
	/// </summary>
	/// <param name="busType">The AudioBusType to meter</param>
	/// <returns>Normalized peak value (0-1), or 0 if unavailable</returns>
	public float GetPeakLevel(AudioBusType busType)
	{
		if (!IsVisualizationReady)
		{
			Debug.LogWarning("Visualization has not been initialized");
			return 0f;
		}

		Bus bus = GetBusByType(busType);
		RESULT result = bus.getChannelGroup(out ChannelGroup channelGroup);
		if (result != RESULT.OK || !channelGroup.hasHandle())
		{
			return 0f;
		}

		// Get the head DSP of the channel group
		result = channelGroup.getDSP(CHANNELCONTROL_DSP_INDEX.HEAD, out DSP headDsp);
		if (result != RESULT.OK)
		{
			return 0f;
		}

		// Get the metering info from the head DSP
		result = headDsp.getMeteringInfo(IntPtr.Zero, out DSP_METERING_INFO outputMetering);
		if (result != RESULT.OK || outputMetering.numchannels == 0)
		{
			return 0f;
		}

		// Calculate the peak level
		float peak = 0f;
		for (int i = 0; i < outputMetering.numchannels; i++)
		{
			if (outputMetering.peaklevel[i] > peak)
			{
				peak = outputMetering.peaklevel[i];
			}
		}

		return peak;
	}

	/// <summary>
	/// Returns the bus by type
	/// </summary>
	/// <param name="type">The AudioBusType</param>
	/// <returns>The bus</returns>
	public Bus GetBusByType(AudioBusType type)
	{
		return type switch
		{
			AudioBusType.Master => _masterBus,
			AudioBusType.Music => _musicBus,
			AudioBusType.Ambience => _ambientBus,
			AudioBusType.Game => _gameBus,
			_ => _masterBus,
		};
	}

	#endregion
}
