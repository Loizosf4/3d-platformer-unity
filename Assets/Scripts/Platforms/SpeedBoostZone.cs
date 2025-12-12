using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class SpeedBoostZone : MonoBehaviour
{
    [Header("Speed Boost")]
    [SerializeField] private float speedMultiplier = 1.5f;
    [SerializeField] private float lingerDuration = 0.35f;

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

        // Refresh continuously; lingerDuration makes it persist briefly after leaving
        motor.ApplySpeedMultiplier(speedMultiplier, lingerDuration);
    }
}
