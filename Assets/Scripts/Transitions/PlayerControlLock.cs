using UnityEngine;

public class PlayerControlLock : MonoBehaviour
{
    [SerializeField] private PlayerInputReader inputReader;
    [SerializeField] private PlayerMotorCC motor;

    private void Awake()
    {
        if (inputReader == null) inputReader = GetComponentInChildren<PlayerInputReader>(true);
        if (motor == null) motor = GetComponent<PlayerMotorCC>();
    }

    public void SetLocked(bool locked)
    {
        if (inputReader != null) inputReader.enabled = !locked;
        if (motor != null) motor.enabled = !locked;
    }
}
