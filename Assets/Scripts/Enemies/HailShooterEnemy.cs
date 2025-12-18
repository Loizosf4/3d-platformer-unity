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
    
    [Tooltip("Minimum Y velocity (negative = falling) required for stomp")]
    [SerializeField] private float stompVelocityThreshold = -2f;
    
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
        
        // Create a stomp detection collider on top
        CreateStompCollider();
        
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
        if (hailstonePrefab == null)
        {
            Debug.LogError("HailShooterEnemy: Cannot shoot - hailstonePrefab is null!");
            return;
        }
        
        // Aim at player's body (1 unit above their position) instead of their feet
        Vector3 targetPosition = _playerTransform.position + Vector3.up * 1f;
        Vector3 directionToPlayer = (targetPosition - shootPoint.position).normalized;
        
        Debug.Log($"HailShooterEnemy: SHOOTING {hailstonesPerBurst} hailstones at player. From {shootPoint.position} towards {targetPosition}");
        
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
            
            // Add small random offset to spawn position to prevent overlap
            Vector3 spawnOffset = Random.insideUnitSphere * 0.1f;
            spawnOffset.y = 0; // Keep on same height
            Vector3 spawnPosition = shootPoint.position + spawnOffset;
            
            // Spawn hailstone
            GameObject hailstone = Instantiate(hailstonePrefab, spawnPosition, Quaternion.identity);
            
            Debug.Log($"HailShooterEnemy: Spawned hailstone #{i} at {shootPoint.position}");
            
            // Get or add Rigidbody
            Rigidbody rb = hailstone.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = hailstone.AddComponent<Rigidbody>();
                rb.mass = 0.1f;
                Debug.Log("HailShooterEnemy: Added Rigidbody to hailstone");
            }
            
            // Apply velocity and enable gravity
            rb.velocity = spreadDirection * projectileSpeed;
            rb.useGravity = true;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            
            Debug.Log($"HailShooterEnemy: Rigidbody setup - velocity={rb.velocity}, gravity={rb.useGravity}, kinematic={rb.isKinematic}");
            
            // Ensure we have a SOLID collider for physics (hitting ground/walls)
            SphereCollider solidCol = hailstone.GetComponent<SphereCollider>();
            if (solidCol == null)
            {
                solidCol = hailstone.AddComponent<SphereCollider>();
                solidCol.radius = 0.15f;
                Debug.Log("HailShooterEnemy: Added SphereCollider to hailstone");
            }
            else
            {
                Debug.Log($"HailShooterEnemy: Found existing SphereCollider, isTrigger was: {solidCol.isTrigger}");
            }
            
            solidCol.isTrigger = false; // Solid collider for physics
            Debug.Log($"HailShooterEnemy: Set solidCol.isTrigger = false. Now is: {solidCol.isTrigger}");
            
            // Add a SECOND trigger collider on a child object for damage detection
            GameObject triggerChild = new GameObject("DamageTrigger");
            triggerChild.transform.SetParent(hailstone.transform);
            triggerChild.transform.localPosition = Vector3.zero;
            
            SphereCollider triggerCol = triggerChild.AddComponent<SphereCollider>();
            triggerCol.radius = 0.2f; // Slightly larger than solid collider
            triggerCol.isTrigger = true; // Trigger for damage detection
            
            Debug.Log($"HailShooterEnemy: Created child trigger collider on {triggerChild.name}");
            
            // Add damage components to the trigger child
            DamageOnTouch damageComp = triggerChild.AddComponent<DamageOnTouch>();
            DamageSource damageSource = triggerChild.AddComponent<DamageSource>();
            
            // Add script to destroy on collision with ground/walls (but not player)
            HailstoneDestroyer destroyer = hailstone.AddComponent<HailstoneDestroyer>();
            destroyer.playerTag = playerTag;
            
            // Ignore collision between hailstone and this shooter
            Collider shooterCollider = GetComponent<Collider>();
            if (shooterCollider != null)
            {
                Physics.IgnoreCollision(solidCol, shooterCollider);
                Debug.Log("HailShooterEnemy: Ignoring collision between hailstone and shooter");
            }
            
            Debug.Log($"HailShooterEnemy: Hailstone fully configured. Active={hailstone.activeSelf}, Name={hailstone.name}");
        }
        
        Debug.Log($"HailShooterEnemy: Fired {hailstonesPerBurst} hailstones at player");
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        if (!_isActive) return;
        
        // Check if player stomped on top
        if (collision.gameObject.CompareTag(playerTag))
        {
            Debug.Log($"HailShooterEnemy: Collision with player!");
            
            // Get player motor
            var motor = collision.gameObject.GetComponent<PlayerMotorCC>();
            if (motor == null)
                motor = collision.gameObject.GetComponentInParent<PlayerMotorCC>();
            
            if (motor != null)
            {
                // Check if player is falling and hit from above
                bool isFalling = motor.VerticalVelocity < stompVelocityThreshold;
                
                // Check contact normal - if pointing mostly upward, player hit from above
                bool hitFromAbove = false;
                foreach (ContactPoint contact in collision.contacts)
                {
                    if (contact.normal.y > 0.5f) // Normal pointing up means player hit from above
                    {
                        hitFromAbove = true;
                        Debug.Log($"HailShooterEnemy: Contact normal Y={contact.normal.y:F2}");
                        break;
                    }
                }
                
                Debug.Log($"HailShooterEnemy: isFalling={isFalling} (vel={motor.VerticalVelocity:F2}), hitFromAbove={hitFromAbove}");
                
                if (isFalling && hitFromAbove)
                {
                    Debug.Log("HailShooterEnemy: STOMPED! Defeating enemy.");
                    DefeatEnemy();
                    
                    // Give player a bounce
                    motor.AddUpwardVelocityThisFrame(10f);
                }
            }
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // This is just for debug logging now
        Debug.Log($"HailShooterEnemy: Trigger entered by {other.gameObject.name}, tag={other.tag}");
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
    
    private void CreateStompCollider()
    {
        // Create a child object for stomp detection
        GameObject stompDetector = new GameObject("StompDetector");
        stompDetector.transform.SetParent(transform);
        stompDetector.transform.localPosition = new Vector3(0, 0.6f, 0); // On top of cube
        stompDetector.layer = gameObject.layer;
        
        // Add a trigger collider for stomping (works with CharacterController)
        BoxCollider stompCol = stompDetector.AddComponent<BoxCollider>();
        stompCol.size = new Vector3(1.2f, 0.3f, 1.2f);
        stompCol.isTrigger = true; // MUST be trigger for CharacterController
        
        // Add a component to forward trigger events to parent
        StompDetector detector = stompDetector.AddComponent<StompDetector>();
        detector.enemy = this;
        
        Debug.Log("HailShooterEnemy: Created stomp detector on top");
    }
    
    public void OnPlayerStomp(Collider playerCollider)
    {
        if (!_isActive) return;
        
        Debug.Log("HailShooterEnemy: OnPlayerStomp called!");
        
        // Get player motor
        var motor = playerCollider.GetComponent<PlayerMotorCC>();
        if (motor == null)
            motor = playerCollider.GetComponentInParent<PlayerMotorCC>();
        
        if (motor != null)
        {
            // Check if player is falling
            bool isFalling = motor.VerticalVelocity < stompVelocityThreshold;
            
            Debug.Log($"HailShooterEnemy: isFalling={isFalling} (vel={motor.VerticalVelocity:F2})");
            
            if (isFalling)
            {
                Debug.Log("HailShooterEnemy: STOMPED! Defeating enemy.");
                DefeatEnemy();
                
                // Give player a bounce
                motor.AddUpwardVelocityThisFrame(10f);
            }
        }
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
