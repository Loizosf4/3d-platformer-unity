using UnityEngine;

public class AbilityProgress : MonoBehaviour
{
    private PlayerMotorCC _motor;

    private void Awake()
    {
        _motor = GetComponent<PlayerMotorCC>();
        ApplyFromPlayerStats();
    }

    private void OnEnable()
    {
        // Re-apply when player spawns/enables
        ApplyFromPlayerStats();
    }

    public bool HasDoubleJump => PlayerStats.Instance != null && PlayerStats.Instance.HasDoubleJump;
    public bool HasDash => PlayerStats.Instance != null && PlayerStats.Instance.HasDash;
    public bool HasWallJump => PlayerStats.Instance != null && PlayerStats.Instance.HasWallJump;

    // These are still called by your pickups (so you don't have to rewrite pickup code)
    public void UnlockDoubleJump()
    {
        PlayerStats.Instance?.UnlockDoubleJump();
        ApplyFromPlayerStats();
    }

    public void UnlockDash()
    {
        PlayerStats.Instance?.UnlockDash();
        ApplyFromPlayerStats();
    }

    public void UnlockWallJump()
    {
        PlayerStats.Instance?.UnlockWallJump();
        ApplyFromPlayerStats();
    }

    private void ApplyFromPlayerStats()
    {
        if (_motor == null)
        {
            Debug.LogError("[AbilityProgress] PlayerMotorCC missing on player.");
            return;
        }

        if (PlayerStats.Instance == null)
        {
            Debug.LogWarning("[AbilityProgress] PlayerStats.Instance is null (Bootstrapper not spawned yet?)");
            return;
        }

        // Apply to motor based on persistent stats
        if (PlayerStats.Instance.HasDoubleJump) _motor.UnlockDoubleJump();
        if (PlayerStats.Instance.HasDash) _motor.UnlockDash();
        if (PlayerStats.Instance.HasWallJump) _motor.UnlockWallJump();
    }
}
