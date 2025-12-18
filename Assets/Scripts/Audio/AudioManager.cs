using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Persistent singleton audio manager that handles all game audio.
/// Survives scene loads (DontDestroyOnLoad) and prevents duplicate instances.
/// Manages background music, one-shot SFX, and AudioMixer routing.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Mixer")]
    [Tooltip("Main AudioMixer asset with Music and SFX groups.")]
    public AudioMixer audioMixer; // Public so other audio components can access mixer groups

    [Header("Music")]
    [Tooltip("Background music clip to play on loop.")]
    [SerializeField] private AudioClip backgroundMusic;

    [Tooltip("Volume for background music (0-1).")]
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.5f;

    [Tooltip("If true, music starts playing immediately on Awake. Set to FALSE if using SceneMusicController.")]
    [SerializeField] private bool playMusicOnStart = false;

    [Header("SFX Settings")]
    [Tooltip("Default volume for one-shot SFX (0-1).")]
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;

    [Tooltip("Maximum number of simultaneous one-shot sounds.")]
    [SerializeField] private int maxSimultaneousSounds = 10;

    // Internal AudioSources
    private AudioSource _musicSource;
    private AudioSource[] _sfxPool;
    private int _nextSfxIndex;

    private void Awake()
    {
        // Singleton pattern with duplicate protection
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[AudioManager] Duplicate instance detected. Destroying {gameObject.name}.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeAudioSources();

        if (playMusicOnStart && backgroundMusic != null)
        {
            PlayMusic(backgroundMusic);
        }
    }

    private void OnDestroy()
    {
        // Cleanup singleton reference when destroyed
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Initialize music AudioSource and SFX pool.
    /// </summary>
    private void InitializeAudioSources()
    {
        // Create persistent music AudioSource
        _musicSource = gameObject.AddComponent<AudioSource>();
        _musicSource.loop = true;
        _musicSource.playOnAwake = false;
        _musicSource.volume = musicVolume;

        // Route to Music mixer group if available
        if (audioMixer != null)
        {
            var musicGroup = audioMixer.FindMatchingGroups("Music");
            if (musicGroup != null && musicGroup.Length > 0)
                _musicSource.outputAudioMixerGroup = musicGroup[0];
        }

        // Create SFX pool for one-shot sounds
        _sfxPool = new AudioSource[maxSimultaneousSounds];
        for (int i = 0; i < maxSimultaneousSounds; i++)
        {
            var sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            sfxSource.volume = sfxVolume;

            // Route to SFX mixer group if available
            if (audioMixer != null)
            {
                var sfxGroup = audioMixer.FindMatchingGroups("SFX");
                if (sfxGroup != null && sfxGroup.Length > 0)
                    sfxSource.outputAudioMixerGroup = sfxGroup[0];
            }

            _sfxPool[i] = sfxSource;
        }
    }

    #region Music Control

    /// <summary>
    /// Play background music. Replaces currently playing music.
    /// </summary>
    public void PlayMusic(AudioClip clip, float volume = -1f)
    {
        if (clip == null || _musicSource == null) return;

        _musicSource.clip = clip;
        _musicSource.volume = volume >= 0f ? volume : musicVolume;
        _musicSource.Play();
    }

    /// <summary>
    /// Stop background music.
    /// </summary>
    public void StopMusic()
    {
        if (_musicSource != null)
            _musicSource.Stop();
    }

    /// <summary>
    /// Pause background music.
    /// </summary>
    public void PauseMusic()
    {
        if (_musicSource != null)
            _musicSource.Pause();
    }

    /// <summary>
    /// Resume background music.
    /// </summary>
    public void ResumeMusic()
    {
        if (_musicSource != null)
            _musicSource.UnPause();
    }

    /// <summary>
    /// Set background music volume.
    /// </summary>
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (_musicSource != null)
            _musicSource.volume = musicVolume;
    }

    #endregion

    #region SFX Control

    /// <summary>
    /// Play a one-shot sound effect.
    /// Safe to call even if clip is null.
    /// </summary>
    public void PlaySFX(AudioClip clip, float volume = -1f)
    {
        if (clip == null) return;

        float finalVolume = volume >= 0f ? volume : sfxVolume;
        GetNextSfxSource().PlayOneShot(clip, finalVolume);
    }

    /// <summary>
    /// Play a one-shot sound effect at a specific world position.
    /// Creates a temporary AudioSource that destroys itself after playing.
    /// This ensures the sound persists even if the calling object is destroyed.
    /// </summary>
    public void PlayAtPosition(AudioClip clip, Vector3 position, float volume = -1f, float spatialBlend = 1f)
    {
        if (clip == null) return;

        // Create temporary GameObject for the sound
        GameObject tempGO = new GameObject($"TempAudio_{clip.name}");
        tempGO.transform.position = position;

        AudioSource tempSource = tempGO.AddComponent<AudioSource>();
        tempSource.clip = clip;
        tempSource.volume = volume >= 0f ? volume : sfxVolume;
        tempSource.spatialBlend = spatialBlend; // 0 = 2D, 1 = 3D
        tempSource.playOnAwake = false;

        // Route to SFX mixer group if available
        if (audioMixer != null)
        {
            var sfxGroup = audioMixer.FindMatchingGroups("SFX");
            if (sfxGroup != null && sfxGroup.Length > 0)
                tempSource.outputAudioMixerGroup = sfxGroup[0];
        }

        tempSource.Play();

        // Destroy after clip finishes playing
        Destroy(tempGO, clip.length + 0.1f);
    }

    /// <summary>
    /// Set default SFX volume for future one-shots.
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
    }

    /// <summary>
    /// Get the next available AudioSource from the pool (round-robin).
    /// </summary>
    private AudioSource GetNextSfxSource()
    {
        if (_sfxPool == null || _sfxPool.Length == 0)
        {
            Debug.LogError("[AudioManager] SFX pool is null or empty.");
            return null;
        }

        AudioSource source = _sfxPool[_nextSfxIndex];
        _nextSfxIndex = (_nextSfxIndex + 1) % _sfxPool.Length;
        return source;
    }

    #endregion

    #region Mixer Control (Optional)

    /// <summary>
    /// Set mixer group volume by name (e.g., "Music", "SFX").
    /// Volume should be in linear scale (0-1), will be converted to dB.
    /// </summary>
    public void SetMixerVolume(string groupName, float volume)
    {
        if (audioMixer == null) return;

        // Convert linear (0-1) to dB scale (-80 to 0)
        float dB = volume > 0f ? 20f * Mathf.Log10(volume) : -80f;
        audioMixer.SetFloat(groupName, dB);
    }

    #endregion
}
