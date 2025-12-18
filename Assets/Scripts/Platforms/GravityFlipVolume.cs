using UnityEngine;

[RequireComponent(typeof(Collider))]
public class GravityFlipVolume : MonoBehaviour
{
    [Header("Gravity Settings")]
    [Tooltip("Gravity while inside the volume. Use +25 to pull upward if your normal gravity is -25.")]
    [SerializeField] private float gravityInside = +25f;

    [Tooltip("If true, leaving the volume restores the player's normal gravity.")]
    [SerializeField] private bool restoreOnExit = true;
    
    [Header("Flip Settings")]
    [Tooltip("If true, player will flip upside down when entering the volume")]
    [SerializeField] private bool flipPlayerUpsideDown = true;
    
    [Tooltip("How fast the player rotates when flipping (degrees per second)")]
    [SerializeField] private float flipRotationSpeed = 360f;

    [Header("Optional VFX")]
    [SerializeField] private ParticleSystem upParticles;

    [Header("Audio")]
    [Tooltip("Looping sound played while player is inside the volume.")]
    [SerializeField] private AudioClip ambientSound;
    [Tooltip("Volume for ambient sound (0-1).")]
    [SerializeField, Range(0f, 1f)] private float ambientVolume = 0.5f;
    [Tooltip("Spatial blend. 0 = 2D, 1 = 3D.")]
    [SerializeField, Range(0f, 1f)] private float spatialBlend = 1f;

    private AudioSource _audioSource;

    private void Awake()
    {
        if (ambientSound != null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.clip = ambientSound;
            _audioSource.loop = true;
            _audioSource.volume = ambientVolume;
            _audioSource.spatialBlend = spatialBlend;

            if (AudioManager.Instance != null && AudioManager.Instance.audioMixer != null)
            {
                var sfxGroup = AudioManager.Instance.audioMixer.FindMatchingGroups("SFX");
                if (sfxGroup != null && sfxGroup.Length > 0)
                    _audioSource.outputAudioMixerGroup = sfxGroup[0];
            }

            _audioSource.Play();
        }
    }

    private void Reset()
    {
        // Ensure collider is trigger
        var c = GetComponent<Collider>();
        c.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        var motor = other.GetComponent<PlayerMotorCC>();
        if (motor == null) return;

        motor.SetGravityOverride(gravityInside);
        
        // Tell the motor to flip the player upside down
        if (flipPlayerUpsideDown)
        {
            motor.SetGravityFlip(true, flipRotationSpeed);
        }

        if (upParticles != null) upParticles.Play(true);
    }

    private void OnTriggerExit(Collider other)
    {
        var motor = other.GetComponent<PlayerMotorCC>();
        if (motor == null) return;

        if (restoreOnExit)
        {
            motor.ClearGravityOverride();
            motor.ResetVerticalVelocity();
            
            // Return player to normal orientation
            if (flipPlayerUpsideDown)
            {
                motor.SetGravityFlip(false, flipRotationSpeed);
            }
        }

        if (upParticles != null) upParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }
}
