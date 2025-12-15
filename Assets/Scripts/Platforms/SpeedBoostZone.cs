using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SpeedBoostZone : MonoBehaviour
{
    [Header("Speed Boost")]
    [SerializeField] private float speedMultiplier = 1.5f;
    [SerializeField] private float lingerDuration = 0.35f;

    private Collider _collider;

    private void Awake()
    {
        _collider = GetComponent<Collider>();
    }

    private void Update()
    {
        // Manually check for player using OverlapBox since triggers don't work well with CharacterController
        PlayerMotorCC motor = FindPlayerInZone();
        if (motor != null)
        {
            motor.ApplySpeedMultiplier(speedMultiplier, lingerDuration);
        }
    }

    private PlayerMotorCC FindPlayerInZone()
    {
        Bounds bounds = _collider.bounds;
        Collider[] hits = Physics.OverlapBox(bounds.center, bounds.extents, transform.rotation);
        
        foreach (var hit in hits)
        {
            // Only detect objects tagged as "Player" to avoid detecting projectiles like hail
            if (!hit.CompareTag("Player")) continue;
            
            var motor = hit.GetComponent<PlayerMotorCC>();
            if (motor != null)
            {
                return motor;
            }
        }
        return null;
    }

    private void OnDrawGizmos()
    {
        var col = GetComponent<Collider>();
        if (col == null) return;
        
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.3f);
        Gizmos.DrawCube(col.bounds.center, col.bounds.size);
    }
}
