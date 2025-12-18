using UnityEngine;

/// <summary>
/// Handles player audio (jump, dash, footsteps).
/// Subscribes to PlayerMotorCC events - does NOT modify player movement logic.
/// Uses step-based timing for footsteps (not OnControllerColliderHit).
/// </summary>
[RequireComponent(typeof(PlayerMotorCC))]
public class PlayerAudio : MonoBehaviour
{
    [Header("References")]
    [Tooltip("PlayerMotorCC reference. Auto-assigned if not set.")]
    [SerializeField] private PlayerMotorCC motor;

    [Header("Jump Sounds")]
    [Tooltip("Sound played when player jumps (normal jump).")]
    [SerializeField] private AudioClip jumpSound;

    [Tooltip("Optional: different sound for double jump.")]
    [SerializeField] private AudioClip doubleJumpSound;

    [Tooltip("Optional: different sound for wall jump.")]
    [SerializeField] private AudioClip wallJumpSound;

    [Tooltip("Volume for jump sounds (0-1).")]
    [SerializeField, Range(0f, 1f)] private float jumpVolume = 0.7f;

    [Header("Dash Sound")]
    [Tooltip("Sound played when player dashes.")]
    [SerializeField] private AudioClip dashSound;

    [Tooltip("Volume for dash sound (0-1).")]
    [SerializeField, Range(0f, 1f)] private float dashVolume = 0.8f;

    [Header("Footsteps")]
    [Tooltip("Array of footstep sounds (randomly selected).")]
    [SerializeField] private AudioClip[] footstepSounds;

    [Tooltip("Minimum horizontal speed to trigger footsteps.")]
    [SerializeField] private float minSpeedForFootsteps = 0.5f;

    [Tooltip("Time between footstep sounds (seconds).")]
    [SerializeField] private float stepInterval = 0.35f;

    [Tooltip("Volume for footstep sounds (0-1).")]
    [SerializeField, Range(0f, 1f)] private float footstepVolume = 0.4f;

    [Tooltip("If true, footsteps play. If false, they don't (useful for testing).")]
    [SerializeField] private bool enableFootsteps = true;

    // Internal state
    private float _stepTimer;
    private int _jumpCount; // Track jumps for double jump detection

    private void Awake()
    {
        if (motor == null)
            motor = GetComponent<PlayerMotorCC>();
    }

    private void OnEnable()
    {
        // Subscribe to PlayerMotorCC events
        if (motor != null)
        {
            motor.OnJump += HandleJump;
            motor.OnDash += HandleDash;
        }
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent memory leaks
        if (motor != null)
        {
            motor.OnJump -= HandleJump;
            motor.OnDash -= HandleDash;
        }
    }

    private void Update()
    {
        // Step-based footsteps (safe, deterministic)
        if (enableFootsteps && footstepSounds != null && footstepSounds.Length > 0)
        {
            UpdateFootsteps();
        }
    }

    /// <summary>
    /// Handle jump event from PlayerMotorCC.
    /// Plays appropriate jump sound based on jump type.
    /// </summary>
    private void HandleJump()
    {
        if (AudioManager.Instance == null) return;

        AudioClip clipToPlay = jumpSound;

        // Detect jump type based on motor state
        // If player is grounded, it's a normal jump (reset counter)
        if (motor.IsGrounded)
        {
            _jumpCount = 0;
            clipToPlay = jumpSound;
        }
        else
        {
            // In air - could be double jump or wall jump
            _jumpCount++;

            // Prioritize wall jump sound if available and touching wall
            // (You can check motor.IsTouchingWall if you expose it, or just use double jump sound)
            if (_jumpCount == 1 && doubleJumpSound != null)
            {
                clipToPlay = doubleJumpSound;
            }
            else if (wallJumpSound != null)
            {
                // Use wall jump sound for subsequent jumps (approximation)
                clipToPlay = wallJumpSound;
            }
        }

        if (clipToPlay != null)
        {
            AudioManager.Instance.PlaySFX(clipToPlay, jumpVolume);
        }
    }

    /// <summary>
    /// Handle dash event from PlayerMotorCC.
    /// </summary>
    private void HandleDash()
    {
        if (AudioManager.Instance == null) return;

        if (dashSound != null)
        {
            AudioManager.Instance.PlaySFX(dashSound, dashVolume);
        }
    }

    /// <summary>
    /// Step-based footstep logic.
    /// Plays footsteps when:
    /// - Player is grounded
    /// - Horizontal speed > threshold
    /// - Step interval timer expires
    /// </summary>
    private void UpdateFootsteps()
    {
        // Only play footsteps when grounded
        if (!motor.IsGrounded)
        {
            _stepTimer = 0f; // Reset timer when airborne
            return;
        }

        // Get horizontal velocity (ignoring Y axis)
        Vector3 velocity = motor.Velocity;
        float horizontalSpeed = new Vector2(velocity.x, velocity.z).magnitude;

        // Only play if moving fast enough
        if (horizontalSpeed < minSpeedForFootsteps)
        {
            _stepTimer = 0f; // Reset timer when stopped
            return;
        }

        // Accumulate time
        _stepTimer += Time.deltaTime;

        // Trigger footstep when interval expires
        if (_stepTimer >= stepInterval)
        {
            PlayFootstep();
            _stepTimer = 0f; // Reset for next step
        }
    }

    /// <summary>
    /// Play a random footstep sound.
    /// </summary>
    private void PlayFootstep()
    {
        if (AudioManager.Instance == null || footstepSounds.Length == 0) return;

        // Pick random footstep sound
        AudioClip clip = footstepSounds[Random.Range(0, footstepSounds.Length)];
        if (clip != null)
        {
            AudioManager.Instance.PlaySFX(clip, footstepVolume);
        }
    }
}
