using UnityEngine;

/// <summary>
/// Handles interval-based hazard audio (e.g., hailstorm ON/OFF cycles).
/// Loops sound while hazard is active, stops when inactive.
/// Optional start/stop stinger sounds.
/// All methods are idempotent (safe to call repeatedly).
/// </summary>
public class HazardAudio : MonoBehaviour
{
    [Header("Loop Sound")]
    [Tooltip("Looping sound played while hazard is active.")]
    [SerializeField] private AudioClip loopClip;

    [Tooltip("Volume for loop sound (0-1).")]
    [SerializeField, Range(0f, 1f)] private float loopVolume = 0.7f;

    [Tooltip("Spatial blend for loop. 0 = 2D, 1 = 3D.")]
    [SerializeField, Range(0f, 1f)] private float loopSpatialBlend = 1f;

    [Tooltip("Max distance for 3D loop sound.")]
    [SerializeField] private float loopMaxDistance = 50f;

    [Header("Stinger Sounds (Optional)")]
    [Tooltip("One-shot sound played when hazard activates.")]
    [SerializeField] private AudioClip startStinger;

    [Tooltip("One-shot sound played when hazard deactivates.")]
    [SerializeField] private AudioClip stopStinger;

    [Tooltip("Volume for stinger sounds (0-1).")]
    [SerializeField, Range(0f, 1f)] private float stingerVolume = 0.8f;

    [Tooltip("Spatial blend for stingers. 0 = 2D, 1 = 3D.")]
    [SerializeField, Range(0f, 1f)] private float stingerSpatialBlend = 0.8f;

    [Header("Fade Settings (Optional)")]
    [Tooltip("If > 0, loop will fade in/out instead of abrupt start/stop.")]
    [SerializeField] private float fadeDuration = 0f;

    // Internal state
    private AudioSource _loopSource;
    private bool _isPlaying;
    private float _fadeTimer;
    private bool _isFading;
    private float _targetVolume;

    private void Awake()
    {
        // Create AudioSource for loop
        if (loopClip != null)
        {
            _loopSource = gameObject.AddComponent<AudioSource>();
            _loopSource.clip = loopClip;
            _loopSource.loop = true;
            _loopSource.playOnAwake = false; // Important: don't auto-play
            _loopSource.volume = 0f; // Start silent (will fade in if needed)
            _loopSource.spatialBlend = loopSpatialBlend;
            _loopSource.maxDistance = loopMaxDistance;

            // Route to SFX mixer group if AudioManager exists
            if (AudioManager.Instance != null && AudioManager.Instance.audioMixer != null)
            {
                var sfxGroup = AudioManager.Instance.audioMixer.FindMatchingGroups("SFX");
                if (sfxGroup != null && sfxGroup.Length > 0)
                    _loopSource.outputAudioMixerGroup = sfxGroup[0];
            }
        }
    }

    private void Update()
    {
        // Handle fading if active
        if (_isFading && _loopSource != null)
        {
            _fadeTimer += Time.deltaTime;
            float t = Mathf.Clamp01(_fadeTimer / fadeDuration);
            _loopSource.volume = Mathf.Lerp(_loopSource.volume, _targetVolume, t);

            if (t >= 1f)
            {
                _isFading = false;
                _loopSource.volume = _targetVolume;

                // Stop source if fading out to zero
                if (_targetVolume == 0f && _loopSource.isPlaying)
                {
                    _loopSource.Stop();
                }
            }
        }
    }

    /// <summary>
    /// Start hazard audio (loop + optional start stinger).
    /// Idempotent - safe to call multiple times.
    /// </summary>
    public void StartHazardAudio()
    {
        if (_isPlaying) return; // Already playing
        _isPlaying = true;

        // Play start stinger
        if (startStinger != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayAtPosition(
                startStinger,
                transform.position,
                stingerVolume,
                stingerSpatialBlend
            );
        }

        // Start loop
        if (_loopSource != null && loopClip != null)
        {
            if (!_loopSource.isPlaying)
            {
                _loopSource.Play();
            }

            // Fade in or instant
            if (fadeDuration > 0f)
            {
                _loopSource.volume = 0f;
                StartFade(loopVolume);
            }
            else
            {
                _loopSource.volume = loopVolume;
            }
        }
    }

    /// <summary>
    /// Stop hazard audio (loop + optional stop stinger).
    /// Idempotent - safe to call multiple times.
    /// </summary>
    public void StopHazardAudio()
    {
        if (!_isPlaying) return; // Already stopped
        _isPlaying = false;

        // Play stop stinger
        if (stopStinger != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayAtPosition(
                stopStinger,
                transform.position,
                stingerVolume,
                stingerSpatialBlend
            );
        }

        // Stop loop
        if (_loopSource != null)
        {
            // Fade out or instant
            if (fadeDuration > 0f)
            {
                StartFade(0f);
            }
            else
            {
                _loopSource.volume = 0f;
                _loopSource.Stop();
            }
        }
    }

    /// <summary>
    /// Start a fade to target volume.
    /// </summary>
    private void StartFade(float targetVolume)
    {
        _targetVolume = targetVolume;
        _fadeTimer = 0f;
        _isFading = true;
    }

    /// <summary>
    /// Immediately stop all hazard audio (no fade, no stinger).
    /// Useful for cleanup or emergency stops.
    /// </summary>
    public void ForceStop()
    {
        _isPlaying = false;
        _isFading = false;

        if (_loopSource != null)
        {
            _loopSource.Stop();
            _loopSource.volume = 0f;
        }
    }

    private void OnDestroy()
    {
        // Cleanup
        ForceStop();
    }

    /// <summary>
    /// Check if hazard audio is currently playing.
    /// </summary>
    public bool IsPlaying => _isPlaying;
}
