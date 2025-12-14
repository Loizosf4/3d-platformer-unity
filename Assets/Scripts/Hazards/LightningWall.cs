using UnityEngine;

/// <summary>
/// Lightning obstacle that cycles on/off, damaging the player when active.
/// Connects two wall endpoints with visual lightning effect.
/// </summary>
public class LightningWall : MonoBehaviour
{
    [Header("Wall Endpoints")]
    [Tooltip("Distance between the two walls (automatically positions walls)")]
    [SerializeField] private float wallDistance = 5f;
    
    [Tooltip("Height of the lightning beam")]
    [SerializeField] private float beamHeight = 3f;
    
    [Tooltip("First wall/endpoint transform")]
    [SerializeField] private Transform wallA;
    
    [Tooltip("Second wall/endpoint transform")]
    [SerializeField] private Transform wallB;
    
    [Tooltip("Lightning beam transform (automatically scaled)")]
    [SerializeField] private Transform lightningBeam;
    
    [Header("Timing")]
    [Tooltip("How long the lightning stays ON (seconds). Set to 0 for always on.")]
    [SerializeField] private float onDuration = 2f;
    
    [Tooltip("How long the lightning stays OFF (seconds). Set to 0 for always on.")]
    [SerializeField] private float offDuration = 2f;
    
    [Tooltip("If true, lightning starts in the ON state")]
    [SerializeField] private bool startActive = true;
    
    [Header("Damage")]
    [Tooltip("Damage dealt per hit")]
    [SerializeField] private int damageAmount = 1;
    
    [Header("Visual Feedback")]
    [Tooltip("Lightning visual renderer (line renderer or sprite)")]
    [SerializeField] private Renderer lightningVisual;
    
    [Tooltip("Particle system for lightning effect")]
    [SerializeField] private ParticleSystem lightningParticles;
    
    [Tooltip("Collider that damages player when lightning is active")]
    [SerializeField] private Collider damageCollider;
    
    [Tooltip("Solid collider that blocks player movement when lightning is active")]
    [SerializeField] private Collider blockingCollider;
    
    [Tooltip("Color when lightning is active")]
    [SerializeField] private Color activeColor = new Color(0.5f, 0.8f, 1f, 1f);
    
    [Tooltip("Color when lightning is inactive")]
    [SerializeField] private Color inactiveColor = new Color(0.2f, 0.2f, 0.3f, 0.3f);
    
    // State
    private bool _isActive;
    private float _timer;
    private bool _alwaysOn;
    private Material _lightningMaterial;

    private void Start()
    {
        // Position walls and scale beam based on distance
        UpdateWallPositions();
        
        // Check if always on mode
        _alwaysOn = (onDuration <= 0f && offDuration <= 0f) || offDuration <= 0f;
        
        _isActive = startActive || _alwaysOn;
        _timer = _isActive ? onDuration : offDuration;
        
        // Get material for color changes
        if (lightningVisual != null && lightningVisual.material != null)
        {
            _lightningMaterial = lightningVisual.material;
        }
        
        // Auto-find components if not assigned
        if (damageCollider == null)
            damageCollider = GetComponentInChildren<Collider>();
        
        if (lightningParticles == null)
            lightningParticles = GetComponentInChildren<ParticleSystem>();
        
        if (lightningBeam == null && lightningVisual != null)
            lightningBeam = lightningVisual.transform;
        
        // Set initial state
        UpdateVisuals();
    }

    private void OnValidate()
    {
        // Update wall positions in editor when distance changes
        if (Application.isPlaying) return;
        UpdateWallPositions();
    }

    private void UpdateWallPositions()
    {
        if (wallA != null)
        {
            wallA.localPosition = new Vector3(-wallDistance / 2f, 0f, 0f);
        }
        
        if (wallB != null)
        {
            wallB.localPosition = new Vector3(wallDistance / 2f, 0f, 0f);
        }
        
        if (lightningBeam != null)
        {
            lightningBeam.localPosition = Vector3.zero;
            Vector3 scale = lightningBeam.localScale;
            scale.x = wallDistance;
            scale.y = beamHeight;
            lightningBeam.localScale = scale;
        }
    }

    private void Update()
    {
        // Always on mode - no cycling
        if (_alwaysOn)
        {
            _isActive = true;
            UpdateVisuals();
            return;
        }
        
        // Cycle timing
        _timer -= Time.deltaTime;
        
        if (_timer <= 0f)
        {
            // Switch state
            _isActive = !_isActive;
            
            // Reset timer
            _timer = _isActive ? onDuration : offDuration;
            
            // Update visuals
            UpdateVisuals();
        }
    }

    private void UpdateVisuals()
    {
        // Enable/disable visual elements
        if (lightningVisual != null)
        {
            lightningVisual.enabled = _isActive;
            
            // Change color
            if (_lightningMaterial != null)
            {
                _lightningMaterial.color = _isActive ? activeColor : inactiveColor;
            }
        }
        
        // Particle system
        if (lightningParticles != null)
        {
            if (_isActive && !lightningParticles.isPlaying)
                lightningParticles.Play();
            else if (!_isActive && lightningParticles.isPlaying)
                lightningParticles.Stop();
        }
        
        // Damage collider
        if (damageCollider != null)
        {
            damageCollider.enabled = _isActive;
        }
        
        // Blocking collider
        if (blockingCollider != null)
        {
            blockingCollider.enabled = _isActive;
        }
    }

    /// <summary>
    /// Public properties for LightningBeamDamage script
    /// </summary>
    public bool IsActive => _isActive;
    public int DamageAmount => damageAmount;

    public Vector3 GetClosestWallPosition(Vector3 playerPos)
    {
        if (wallA == null && wallB == null)
            return transform.position;
        
        if (wallA == null)
            return wallB.position;
        
        if (wallB == null)
            return wallA.position;
        
        // Return closest wall
        float distA = Vector3.Distance(playerPos, wallA.position);
        float distB = Vector3.Distance(playerPos, wallB.position);
        
        return distA < distB ? wallA.position : wallB.position;
    }

    private void OnDrawGizmos()
    {
        if (wallA == null || wallB == null) return;
        
        // Draw line between walls
        Gizmos.color = _isActive ? Color.cyan : Color.gray;
        Gizmos.DrawLine(wallA.position, wallB.position);
        
        // Draw wall indicators
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(wallA.position, 0.2f);
        Gizmos.DrawWireSphere(wallB.position, 0.2f);
    }
    
    /// <summary>
    /// Force the lightning to a specific state (useful for scripted events)
    /// </summary>
    public void SetActive(bool active)
    {
        _isActive = active;
        UpdateVisuals();
    }
    
    /// <summary>
    /// Reset the cycle timer
    /// </summary>
    public void ResetTimer()
    {
        _timer = _isActive ? onDuration : offDuration;
    }
}
