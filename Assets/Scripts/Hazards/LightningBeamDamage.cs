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
        
        // Apply STRONG pushback force - push player away from wall center
        var motor = other.GetComponentInParent<PlayerMotorCC>();
        if (motor != null)
        {
            // Calculate direction from wall center to player
            Vector3 wallCenter = transform.position;
            Vector3 playerPos = other.transform.position;
            Vector3 pushDirection = (playerPos - wallCenter).normalized;
            
            // Keep only horizontal component (zero out Y)
            pushDirection.y = 0f;
            
            // If somehow the direction is too small (player directly above/below), use forward
            if (pushDirection.magnitude < 0.1f)
            {
                pushDirection = Vector3.forward;
            }
            else
            {
                pushDirection.Normalize();
            }
            
            // Add upward component
            pushDirection.y = 0.5f;
            pushDirection.Normalize();
            
            float force = controller.PushbackForce;
            Debug.Log($"PUSHBACK: wallCenter={wallCenter}, playerPos={playerPos}, pushDir={pushDirection}, force={force}");
            
            motor.AddDirectionalForce(pushDirection * force);
        }
        
        // Start cooldown
        _damageTimer = DAMAGE_INTERVAL;
    }
}
