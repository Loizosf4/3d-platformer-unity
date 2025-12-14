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
    
    [Header("Pushback")]
    [Tooltip("Force to push player away from beam")]
    [SerializeField] private float pushbackForce = 15f;
    
    [Header("Detection")]
    [SerializeField] private string playerTag = "Player";
    
    private float _damageTimer = 0f;
    private const float DAMAGE_INTERVAL = 0.5f;

    private void Awake()
    {
        // Auto-find controller if not assigned
        if (controller == null)
            controller = GetComponentInParent<LightningWall>();
    }

    private void Update()
    {
        if (_damageTimer > 0f)
            _damageTimer -= Time.deltaTime;
    }

    private void OnTriggerStay(Collider other)
    {
        if (controller == null || !controller.IsActive) return;
        if (_damageTimer > 0f) return;
        if (!other.CompareTag(playerTag)) return;
        
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
        
        // Apply pushback force using beam surface normal
        var motor = other.GetComponentInParent<PlayerMotorCC>();
        if (motor != null)
        {
            // Determine which side of the beam the player is on (front +Z or back -Z in local space)
            Vector3 localPlayerPos = transform.InverseTransformPoint(other.transform.position);
            
            // Push in the direction of the beam's surface normal (Z-axis faces)
            Vector3 pushDirection;
            if (localPlayerPos.z > 0)
            {
                // Player is on the front side, push in +Z world direction
                pushDirection = transform.forward;
            }
            else
            {
                // Player is on the back side, push in -Z world direction
                pushDirection = -transform.forward;
            }
            
            // Add slight upward component
            pushDirection.y = 0.3f;
            pushDirection.Normalize();
            
            motor.AddDirectionalForce(pushDirection * pushbackForce);
        }
        
        // Start cooldown
        _damageTimer = DAMAGE_INTERVAL;
    }
}
