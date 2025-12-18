using UnityEngine;

/// <summary>
/// Handles collectible audio: ambient loop while alive, one-shot pickup sound.
/// On pickup:
/// - Stops loop immediately
/// - Plays pickup sound via AudioManager.PlayAtPosition (survives object destruction)
/// </summary>
[RequireComponent(typeof(Collectible))]
public class CollectibleAudio : MonoBehaviour
{
    [Header("Ambient Loop")]
    [Tooltip("Looping ambient sound while collectible exists (e.g., sparkle, hum).")]
    [SerializeField] private AudioClip ambientLoopClip;

    [Tooltip("Volume for ambient loop (0-1).")]
    [SerializeField, Range(0f, 1f)] private float ambientVolume = 0.3f;

    [Tooltip("If true, ambient loop starts on Awake. If false, manually start.")]
    [SerializeField] private bool playAmbientOnStart = true;

    [Tooltip("Spatial blend for ambient loop. 0 = 2D, 1 = 3D.")]
    [SerializeField, Range(0f, 1f)] private float ambientSpatialBlend = 1f;

    [Tooltip("Max distance for 3D ambient sound.")]
    [SerializeField] private float ambientMaxDistance = 15f;

    [Header("Pickup Sound")]
    [Tooltip("One-shot sound played on pickup.")]
    [SerializeField] private AudioClip pickupSound;

    [Tooltip("Volume for pickup sound (0-1).")]
    [SerializeField, Range(0f, 1f)] private float pickupVolume = 0.8f;

    [Tooltip("Spatial blend for pickup sound. 0 = 2D, 1 = 3D.")]
    [SerializeField, Range(0f, 1f)] private float pickupSpatialBlend = 0.5f;

    // Internal AudioSource for ambient loop
    private AudioSource _ambientSource;
    private Collectible _collectible;
    private bool _hasBeenCollected;

    private void Awake()
    {
        _collectible = GetComponent<Collectible>();

        // Create AudioSource for ambient loop
        if (ambientLoopClip != null)
        {
            _ambientSource = gameObject.AddComponent<AudioSource>();
            _ambientSource.clip = ambientLoopClip;
            _ambientSource.loop = true;
            _ambientSource.playOnAwake = false;
            _ambientSource.volume = ambientVolume;
            _ambientSource.spatialBlend = ambientSpatialBlend;
            _ambientSource.maxDistance = ambientMaxDistance;

            // Route to SFX mixer group if AudioManager exists
            if (AudioManager.Instance != null && AudioManager.Instance.audioMixer != null)
            {
                var sfxGroup = AudioManager.Instance.audioMixer.FindMatchingGroups("SFX");
                if (sfxGroup != null && sfxGroup.Length > 0)
                    _ambientSource.outputAudioMixerGroup = sfxGroup[0];
            }

            if (playAmbientOnStart)
            {
                _ambientSource.Play();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Hook into collectible pickup (before Collectible disables itself)
        if (_hasBeenCollected) return;
        if (!other.CompareTag("Player")) return;

        // Detect pickup is about to happen
        // We need to play pickup sound BEFORE object is destroyed
        OnPickup();
    }

    /// <summary>
    /// Called when collectible is picked up.
    /// Stops ambient loop and plays pickup sound.
    /// </summary>
    private void OnPickup()
    {
        if (_hasBeenCollected) return;
        _hasBeenCollected = true;

        // Stop ambient loop immediately
        if (_ambientSource != null && _ambientSource.isPlaying)
        {
            _ambientSource.Stop();
        }

        // Play pickup sound via AudioManager.PlayAtPosition
        // This ensures the sound persists even if this GameObject is destroyed
        if (pickupSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayAtPosition(
                pickupSound,
                transform.position,
                pickupVolume,
                pickupSpatialBlend
            );
        }
    }

    /// <summary>
    /// Manually start ambient loop (if not auto-started).
    /// </summary>
    public void StartAmbient()
    {
        if (_ambientSource != null && !_ambientSource.isPlaying)
        {
            _ambientSource.Play();
        }
    }

    /// <summary>
    /// Manually stop ambient loop.
    /// </summary>
    public void StopAmbient()
    {
        if (_ambientSource != null && _ambientSource.isPlaying)
        {
            _ambientSource.Stop();
        }
    }

    private void OnDestroy()
    {
        // Cleanup (though ambient should already be stopped on pickup)
        if (_ambientSource != null)
        {
            _ambientSource.Stop();
        }
    }
}
