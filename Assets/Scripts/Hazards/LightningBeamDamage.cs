using UnityEngine;

/// <summary>
/// Damage trigger for the lightning beam. 
/// This must be on the same GameObject as the trigger collider.
/// </summary>
[RequireComponent(typeof(Collider))]
public class LightningBeamDamage : MonoBehaviour
{
    [Tooltip("Reference to the parent LightningWall controller")]
    [SerializeField] private LightningWall controller;
    
    [Header("Detection")]
    [SerializeField] private string playerTag = "Player";
    
    private float _damageTimer = 0f;
    private const float DAMAGE_INTERVAL = 0.5f;

    private void Awake()
    {
        // Auto-find controller if not assigned
        if (controller == null)
            controller = GetComponentInParent<LightningWall>();
            
        if (controller == null)
            Debug.LogError($"LightningBeamDamage on {gameObject.name}: Could not find LightningWall controller!");
            
        // Verify we have a trigger collider
        Collider col = GetComponent<Collider>();
        if (col == null)
            Debug.LogError($"LightningBeamDamage on {gameObject.name}: No collider found!");
        else if (!col.isTrigger)
            Debug.LogWarning($"LightningBeamDamage on {gameObject.name}: Collider is not a trigger!");
        else
            Debug.Log($"LightningBeamDamage on {gameObject.name}: Setup complete. Trigger collider found.");
    }

    private void Update()
    {
        if (_damageTimer > 0f)
            _damageTimer -= Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"LightningBeamDamage.OnTriggerEnter: {other.gameObject.name}, tag={other.tag}, controller={controller != null}, active={controller?.IsActive}");
        
        if (controller == null || !controller.IsActive) return;
        if (!other.CompareTag(playerTag)) return;
        
        Debug.Log($"Applying damage on ENTER to {other.gameObject.name}");
        ApplyDamage(other);
        
        // Apply immediate VERY strong pushback to prevent fast players from passing through
        // This replaces the need for a solid blocking collider
        ApplyPushback(other, controller.PushbackForce * 3.0f);
    }

    private void OnTriggerStay(Collider other)
    {
        if (controller == null || !controller.IsActive) return;
        Debug.Log($"Applying damage on STAY to {other.gameObject.name}");
        if (_damageTimer > 0f) return;
        if (!other.CompareTag(playerTag)) return;
        
        ApplyDamage(other);
    }
    
    private void ApplyDamage(Collider other)
    {
        
        var health = other.GetComponentInParent<PlayerHealthController>();
        if (health == null) return;
        
        // Don't apply damage or pushback if player is already dead/handling death
        if (health.IsInvincible) return;
        
        // Get player stats to check health
        var stats = PlayerStats.Instance;
        if (stats == null) return;
        
        // Store hearts before damage
        int heartsBeforeDamage = stats.CurrentHearts;
        
        // Apply damage
        Vector3 hitSource = controller.GetClosestWallPosition(other.transform.position);
        health.TryTakeDamage(hitSource, controller.DamageAmount);
        
        // If player died from this damage (hearts went to 0), don't apply pushback
        if (stats.CurrentHearts <= 0 || heartsBeforeDamage <= controller.DamageAmount) 
        {
            _damageTimer = DAMAGE_INTERVAL;
            return;
        }
        
        // Apply pushback with normal force
        ApplyPushback(other, controller.PushbackForce);
        
        // Start cooldown
        _damageTimer = DAMAGE_INTERVAL;
    }
    
    private void ApplyPushback(Collider other, float force)
    {
        var motor = other.GetComponentInParent<PlayerMotorCC>();
        if (motor == null) return;
        
        // The wall runs left-right (X-axis), so we need to push along Z-axis (perpendicular to wall)
        // Get player position and wall position
        Vector3 playerPos = other.transform.position;
        Vector3 wallPos = controller.transform.position;
        
        // Calculate which side of the wall the player is on (Z-axis direction)
        Vector3 wallToPlayer = playerPos - wallPos;
        float zDirection = Mathf.Sign(wallToPlayer.z);
        
        // If player is very close to wall center (within 0.1 units), default to pushing forward
        if (Mathf.Abs(wallToPlayer.z) < 0.1f)
        {
            zDirection = 1f; // Push forward by default
        }
        
        // Push direction is perpendicular to the wall (along Z-axis)
        Vector3 pushDirection = controller.transform.forward * zDirection;
        
        // Add upward component for better feel
        pushDirection.y = 0.5f;
        pushDirection.Normalize();
        
        Debug.Log($"PUSHBACK: playerPos={playerPos}, wallPos={wallPos}, zDir={zDirection}, pushDir={pushDirection}, force={force}");
        
        motor.AddDirectionalForce(pushDirection * force);
    }
}
