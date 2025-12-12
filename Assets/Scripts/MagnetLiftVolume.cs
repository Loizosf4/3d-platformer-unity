using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class MagnetLiftVolume : MonoBehaviour
{
    [Header("Lift")]
    [Tooltip("Upward velocity added per second while inside.")]
    [SerializeField] private float liftStrength = 12f;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;

        var rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    private void OnTriggerStay(Collider other)
    {
        var motor = other.GetComponentInParent<PlayerMotorCC>();
        if (motor == null) return;

        motor.AddUpwardVelocityThisFrame(liftStrength * Time.deltaTime);
    }
}
