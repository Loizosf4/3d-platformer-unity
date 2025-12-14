using UnityEngine;

/// <summary>
/// A tornado/vortex hazard that pulls the player toward its center and pushes them upward.
/// Acts like a real tornado - continuously pulls horizontally and lifts vertically until player reaches the top.
/// </summary>
public class TornadoVortex : MonoBehaviour
{
    [Header("Vortex Settings")]
    [Tooltip("Radius of the tornado's pull effect")]
    [SerializeField] private float pullRadius = 8f;
    
    [Tooltip("Height of the tornado (player is released when reaching this height)")]
    [SerializeField] private float tornadoHeight = 15f;
    
    [Tooltip("Horizontal force pulling player toward center (per second)")]
    [SerializeField] private float pullForce = 15f;
    
    [Tooltip("Upward force pushing player to the top (per second)")]
    [SerializeField] private float upwardForce = 20f;
    
    [Tooltip("Tangential force causing player to spin around the tornado (per second)")]
    [SerializeField] private float spinForce = 10f;
    
    [Tooltip("How quickly pull force increases as player gets closer to center (0 = constant, 1 = linear)")]
    [Range(0f, 2f)]
    [SerializeField] private float pullFalloff = 0.5f;
    
    [Tooltip("Height offset above tornado top where player is considered 'released'")]
    [SerializeField] private float releaseHeightOffset = 1f;
    
    [Header("Damage Settings")]
    [Tooltip("Whether to damage player when they enter the tornado")]
    [SerializeField] private bool damageOnEntry = false;
    
    [Tooltip("Damage dealt when player enters tornado (if damageOnEntry is true)")]
    [SerializeField] private int damageAmount = 1;
    
    [Header("Visual Settings")]
    [Tooltip("Visual representation of the tornado (will be rotated)")]
    [SerializeField] private Transform tornadoVisual;
    
    [Tooltip("Rotation speed of the tornado visual (degrees per second)")]
    [SerializeField] private float visualRotationSpeed = 180f;
    
    [Tooltip("Optional particle system for the vortex effect")]
    [SerializeField] private ParticleSystem vortexParticles;
    
    [Tooltip("Show pull radius gizmo in editor")]
    [SerializeField] private bool showGizmos = true;
    
    [Header("Audio")]
    [Tooltip("Sound played when player reaches the top")]
    [SerializeField] private AudioClip releaseSound;
    
    [Tooltip("Looping wind sound for the vortex")]
    [SerializeField] private AudioClip windLoopSound;
    
    // Runtime state
    private Transform playerTransform;
    private PlayerMotorCC playerMotor;
    private PlayerHealthController playerHealth;
    private PlayerStats playerStats;
    private AudioSource audioSource;
    
    private bool playerInRange = false;
    private bool hasPlayedReleaseSound = false;
    private bool hasSetGravityOverride = false;
    private bool hasDealtDamageThisEntry = false;
    
    private void Awake()
    {
        // Setup audio source for wind loop
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && windLoopSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        if (audioSource != null)
        {
            audioSource.clip = windLoopSound;
            audioSource.loop = true;
            audioSource.spatialBlend = 1f; // 3D sound
            audioSource.maxDistance = pullRadius * 2f;
            audioSource.volume = 0f; // Start silent, will fade in when player approaches
        }
    }
    
    private void Start()
    {
        // Find player references
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            playerMotor = player.GetComponent<PlayerMotorCC>();
            playerHealth = player.GetComponent<PlayerHealthController>();
            playerStats = PlayerStats.Instance;
        }
        
        // Start wind sound if available
        if (audioSource != null && windLoopSound != null)
        {
            audioSource.Play();
        }
    }
    
    private void Update()
    {
        if (playerTransform == null) return;
        
        // Rotate visual
        if (tornadoVisual != null)
        {
            tornadoVisual.Rotate(Vector3.up, visualRotationSpeed * Time.deltaTime);
        }
        
        // Check if player is in range and below release height
        Vector3 toPlayer = playerTransform.position - transform.position;
        float horizontalDistance = new Vector3(toPlayer.x, 0, toPlayer.z).magnitude;
        float playerHeightAboveBase = playerTransform.position.y - transform.position.y;
        
        // Player is affected if within radius and hasn't been released at top yet
        bool wasInRange = playerInRange;
        playerInRange = horizontalDistance <= pullRadius && playerHeightAboveBase < (tornadoHeight + releaseHeightOffset);
        
        // Reset flags when player re-enters
        if (playerInRange && !wasInRange)
        {
            hasPlayedReleaseSound = false;
            hasSetGravityOverride = false;
            hasDealtDamageThisEntry = false;
        }
        
        // Update audio volume based on distance
        if (audioSource != null)
        {
            float volumeTarget = playerInRange ? 1f : Mathf.Clamp01(1f - (horizontalDistance - pullRadius) / pullRadius);
            audioSource.volume = Mathf.Lerp(audioSource.volume, volumeTarget, Time.deltaTime * 5f);
        }
        
        // Apply pull and lift effects
        if (playerInRange)
        {
            ApplyVortexEffects(horizontalDistance, toPlayer, playerHeightAboveBase);
        }
        else if (wasInRange)
        {
            // Player just left tornado (released at top)
            hasDealtDamageThisEntry = false;
            
            // Restore normal gravity
            if (hasSetGravityOverride && playerMotor != null)
            {
                playerMotor.ClearGravityOverride();
                hasSetGravityOverride = false;
            }
        }
    }
    
    private void ApplyVortexEffects(float distanceFromCenter, Vector3 toPlayer, float playerHeight)
    {
        if (playerMotor == null) return;
        
        // Override gravity to upward on first frame - this solves the ground friction issue
        if (!hasSetGravityOverride)
        {
            playerMotor.SetGravityOverride(upwardForce);
            hasSetGravityOverride = true;
        }
        
        // Calculate horizontal pull force toward center
        Vector3 pullDirection = -toPlayer.normalized; // Toward center
        pullDirection.y = 0; // Keep horizontal only
        
        float distanceFactor = 1f;
        if (pullFalloff > 0f)
        {
            // Increase force as player gets closer
            float normalizedDistance = distanceFromCenter / pullRadius; // 1 at edge, 0 at center
            distanceFactor = 1f + pullFalloff * (1f - normalizedDistance);
        }
        
        // Apply horizontal pull force
        Vector3 horizontalForce = pullDirection * pullForce * distanceFactor * Time.deltaTime;
        playerMotor.AddDirectionalForce(horizontalForce);
        
        // Apply tangential force to spin player around tornado
        Vector3 tangentDirection = Vector3.Cross(Vector3.up, pullDirection).normalized;
        Vector3 spinForceVec = tangentDirection * spinForce * Time.deltaTime;
        playerMotor.AddDirectionalForce(spinForceVec);
        
        // Apply damage once on entry if enabled (no pushback)
        if (damageOnEntry && !hasDealtDamageThisEntry && playerHealth != null)
        {
            playerHealth.TryTakeDamage(transform.position, damageAmount);
            hasDealtDamageThisEntry = true;
        }
        
        // Play sound effect when player reaches top (once per entry)
        if (playerHeight >= tornadoHeight && releaseSound != null && !hasPlayedReleaseSound)
        {
            AudioSource.PlayClipAtPoint(releaseSound, transform.position, 0.5f);
            hasPlayedReleaseSound = true;
        }
    }
    
    private void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        // Draw pull radius at base
        Gizmos.color = new Color(0.5f, 1f, 0.5f, 0.3f);
        DrawCircle(transform.position, pullRadius, 32);
        
        // Draw pull radius at top
        Gizmos.color = new Color(0.5f, 1f, 0.5f, 0.2f);
        DrawCircle(transform.position + Vector3.up * tornadoHeight, pullRadius, 32);
        
        // Draw release height
        Gizmos.color = Color.yellow;
        DrawCircle(transform.position + Vector3.up * (tornadoHeight + releaseHeightOffset), pullRadius, 16);
        
        // Draw tornado height indicator
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * tornadoHeight);
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
