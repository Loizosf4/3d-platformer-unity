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
