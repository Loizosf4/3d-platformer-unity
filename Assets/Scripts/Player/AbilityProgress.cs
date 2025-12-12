using UnityEngine;

public class AbilityProgress : MonoBehaviour
{
    [Header("Unlock State (Save Later)")]
    [SerializeField] private bool hasDoubleJump = false;
    [SerializeField] private bool hasDash = false;
    [SerializeField] private bool hasWallJump = false;

    private PlayerMotorCC _motor;

    private void Awake()
    {
        _motor = GetComponent<PlayerMotorCC>();
        ApplyToMotor();
    }

    public bool HasDoubleJump => hasDoubleJump;
    public bool HasDash => hasDash;
    public bool HasWallJump => hasWallJump;

    public void UnlockDoubleJump()
    {
        hasDoubleJump = true;
        ApplyToMotor();
    }

    public void UnlockDash()
    {
        hasDash = true;
        ApplyToMotor();
    }

    public void UnlockWallJump()
    {
        hasWallJump = true;
        ApplyToMotor();
    }

    private void ApplyToMotor()
    {
        if (_motor == null) return;

        // These methods will be added to the motor in the next step.
        if (hasDoubleJump) _motor.UnlockDoubleJump();
        if (hasDash) _motor.UnlockDash();
        if (hasWallJump) _motor.UnlockWallJump();
    }
}
