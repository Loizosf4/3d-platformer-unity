using UnityEngine;

/// <summary>
/// Static enemy that shoots hailstones at the player when within range.
/// Can be defeated by jumping on top of it.
/// </summary>
public class HailShooterEnemy : MonoBehaviour
{
    [Header("Detection")]
    [Tooltip("Radius within which the enemy will detect and shoot at the player")]
    [SerializeField] private float detectionRadius = 10f;
    
    [Tooltip("Height of the detection cylinder")]
    [SerializeField] private float detectionHeight = 20f;
    
    [Tooltip("Layer mask for player detection")]
    [SerializeField] private LayerMask playerLayer = -1;
    
    [Header("Shooting")]
    [Tooltip("Hailstone prefab to shoot")]
    [SerializeField] private GameObject hailstonePrefab;
    
    [Tooltip("Number of hailstones to shoot per burst")]
    [SerializeField, Range(1, 10)] private int hailstonesPerBurst = 3;
    
    [Tooltip("Time between shots (seconds)")]
    [SerializeField] private float fireRate = 2f;
    
    [Tooltip("Speed at which hailstones are launched")]
    [SerializeField] private float projectileSpeed = 15f;
    
    [Tooltip("Spread angle for multiple hailstones (degrees)")]
    [SerializeField] private float spreadAngle = 10f;
    
    [Tooltip("Point from which hailstones are spawned")]
    [SerializeField] private Transform shootPoint;
    
    [Header("Defeat")]
    [Tooltip("Tag to check for player stomp")]
    [SerializeField] private string playerTag = "Player";
    
    [Tooltip("Collider on top of enemy for stomp detection")]
    [SerializeField] private Collider stompCollider;
    
    [Header("Visual")]
    [Tooltip("Renderer to show/hide")]
    [SerializeField] private Renderer enemyRenderer;
    
    // Runtime
    private Transform _playerTransform;
    private float _nextFireTime;
    private bool _isActive = true;
    
    private void Start()
    {
        // Find player
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
        {
            _playerTransform = player.transform;
        }
        
        // Auto-find shoot point if not assigned
        if (shootPoint == null)
        {
            shootPoint = transform;
        }
        
        // Auto-find renderer if not assigned
        if (enemyRenderer == null)
        {
            enemyRenderer = GetComponentInChildren<Renderer>();
        }
        
        _nextFireTime = Time.time + fireRate;
    }
    
    private void Update()
    {
        if (!_isActive || hailstonePrefab == null)
        {
            Debug.LogWarning($"HailShooterEnemy: Not active or missing prefab. Active={_isActive}, Prefab={hailstonePrefab != null}");
            return;
        }
        
        // Auto-find player if lost
        if (_playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag(playerTag);
            if (player != null)
            {
                _playerTransform = player.transform;
                Debug.Log("HailShooterEnemy: Found player!");
            }
            else
            {
                return;
            }
        }
        
        // Check if player is within detection cylinder (radius + height)
        Vector3 enemyPos = transform.position;
        Vector3 playerPos = _playerTransform.position;
        
        // Calculate horizontal distance (XZ plane)
        float horizontalDistance = Vector3.Distance(
            new Vector3(enemyPos.x, 0, enemyPos.z),
            new Vector3(playerPos.x, 0, playerPos.z)
        );
        
        // Calculate vertical distance
        float verticalDistance = Mathf.Abs(playerPos.y - enemyPos.y);
        
        bool inRange = horizontalDistance <= detectionRadius && verticalDistance <= detectionHeight;
        
        Debug.Log($"HailShooterEnemy: hDist={horizontalDistance:F2}/{detectionRadius}, vDist={verticalDistance:F2}/{detectionHeight}, inRange={inRange}, canFire={Time.time >= _nextFireTime}");
        
        if (inRange && Time.time >= _nextFireTime)
        {
            ShootAtPlayer();
            _nextFireTime = Time.time + fireRate;
        }
    }
    
    private void ShootAtPlayer()
    {
        // Aim at player's body (1 unit above their position) instead of their feet
        Vector3 targetPosition = _playerTransform.position + Vector3.up * 1f;
        Vector3 directionToPlayer = (targetPosition - shootPoint.position).normalized;
        
        for (int i = 0; i < hailstonesPerBurst; i++)
        {
            // Calculate spread
            float angleOffset = 0f;
            if (hailstonesPerBurst > 1)
            {
                angleOffset = Mathf.Lerp(-spreadAngle, spreadAngle, (float)i / (hailstonesPerBurst - 1));
            }
            
            // Rotate direction by spread angle around up axis
            Quaternion spreadRotation = Quaternion.AngleAxis(angleOffset, Vector3.up);
            Vector3 spreadDirection = spreadRotation * directionToPlayer;
            
            // Spawn hailstone
            GameObject hailstone = Instantiate(hailstonePrefab, shootPoint.position, Quaternion.identity);
            
            // Apply velocity
            Rigidbody rb = hailstone.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = spreadDirection * projectileSpeed;
            }
            
            // Add damage component if not already present
            DamageOnTouch damageComp = hailstone.GetComponent<DamageOnTouch>();
            if (damageComp == null)
            {
                damageComp = hailstone.AddComponent<DamageOnTouch>();
            }
            
            // Add damage source if not present
            DamageSource damageSource = hailstone.GetComponent<DamageSource>();
            if (damageSource == null)
            {
                damageSource = hailstone.AddComponent<DamageSource>();
            }
            
            // Destroy hailstone after lifetime to prevent clutter
            Destroy(hailstone, 5f);
        }
        
        Debug.Log($"HailShooterEnemy: Fired {hailstonesPerBurst} hailstones at player");
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (!_isActive) return;
        
        // Check if player stomped on top
        if (other.CompareTag(playerTag))
        {
            // Get player motor
            var motor = other.GetComponent<PlayerMotorCC>();
            if (motor == null)
                motor = other.GetComponentInParent<PlayerMotorCC>();
            
            if (motor != null)
            {
                // Calculate positions
                Vector3 playerPos = motor.transform.position;
                Vector3 enemyPos = transform.position;
                
                // Get the bottom of the player (CharacterController center - height/2)
                CharacterController playerCC = other as CharacterController;
                float playerBottom = playerPos.y;
                if (playerCC != null)
                {
                    playerBottom = playerPos.y - (playerCC.height / 2f) + playerCC.center.y;
                }
                
                // Get the top of the enemy (using the stomp collider position)
                float enemyTop = enemyPos.y + 0.5f; // Stomp collider is at y: 0.55
                
                // Check if player is falling (negative vertical velocity) and their feet are near the top of enemy
                bool isFalling = motor.VerticalVelocity < 0;
                bool isAboveEnemy = playerBottom > enemyTop - 0.2f; // Small tolerance
                
                if (isFalling && isAboveEnemy)
                {
                    Debug.Log($"HailShooterEnemy: Player stomped on enemy! (playerBottom: {playerBottom:F2}, enemyTop: {enemyTop:F2}, vertVel: {motor.VerticalVelocity:F2})");
                    DefeatEnemy();
                    
                    // Give player a small bounce
                    motor.AddUpwardVelocityThisFrame(8f);
                }
            }
        }
    }
    
    private void DefeatEnemy()
    {
        _isActive = false;
        
        Debug.Log("HailShooterEnemy: Defeated by player stomp!");
        
        // Hide visuals
        if (enemyRenderer != null)
            enemyRenderer.enabled = false;
        
        // Disable colliders
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (var col in colliders)
        {
            col.enabled = false;
        }
        
        // Destroy after a short delay
        Destroy(gameObject, 0.5f);
    }
    
    private void OnDrawGizmosSelected()
    {
        // Draw detection cylinder
        Gizmos.color = Color.yellow;
        
        // Draw cylinder as stacked circles
        int segments = 32;
        Vector3 center = transform.position;
        
        // Draw top circle
        DrawCircle(center + Vector3.up * detectionHeight / 2f, detectionRadius, segments);
        
        // Draw bottom circle
        DrawCircle(center - Vector3.up * detectionHeight / 2f, detectionRadius, segments);
        
        // Draw vertical lines connecting them
        for (int i = 0; i < 4; i++)
        {
            float angle = i * 90f * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * detectionRadius;
            Vector3 top = center + Vector3.up * detectionHeight / 2f + offset;
            Vector3 bottom = center - Vector3.up * detectionHeight / 2f + offset;
            Gizmos.DrawLine(top, bottom);
        }
        
        // Draw shoot direction to player in play mode
        if (Application.isPlaying && _playerTransform != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(shootPoint != null ? shootPoint.position : transform.position, _playerTransform.position);
        }
    }
    
    private void DrawCircle(Vector3 center, float radius, int segments)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);
        
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }
}
