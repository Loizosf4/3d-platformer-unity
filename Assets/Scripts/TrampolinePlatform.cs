using UnityEngine;

/// <summary>
/// A platform that acts like a trampoline, bouncing the player into the air.
/// Compresses when landed on and launches the player upward.
/// </summary>
public class TrampolinePlatform : MonoBehaviour
{
    [Header("Bounce Settings")]
    [Tooltip("How high the player will bounce (in meters)")]
    [SerializeField] private float bounceHeight = 10f;
    
    [Tooltip("Speed multiplier for the bounce (higher = faster launch)")]
    [SerializeField] private float bounceSpeed = 1.5f;
    
    [Header("Compression Settings")]
    [Tooltip("How much the platform compresses down when player lands (in meters)")]
    [SerializeField] private float compressionAmount = 0.3f;
    
    [Tooltip("How fast the platform compresses")]
    [SerializeField] private float compressionSpeed = 8f;
    
    [Tooltip("How fast the platform returns to normal after launch")]
    [SerializeField] private float returnSpeed = 5f;
    
    [Header("Cooldown")]
    [Tooltip("Time before the trampoline can be used again after bouncing")]
    [SerializeField] private float cooldownTime = 0.3f;
    
    [Header("Visual Feedback")]
    [Tooltip("Color when ready to bounce")]
    [SerializeField] private Color readyColor = Color.green;
    
    [Tooltip("Color when compressed")]
    [SerializeField] private Color compressedColor = Color.yellow;
    
    [Tooltip("Color during cooldown")]
    [SerializeField] private Color cooldownColor = Color.red;

    // Internal state
    private Vector3 _originalPosition;
    private Vector3 _originalScale;
    private bool _isCompressed = false;
    private bool _playerOnPlatform = false;
    private bool _hasLaunched = false;
    private float _compressionProgress = 0f;
    private float _cooldownTimer = 0f;
    private bool _isOnCooldown = false;
    
    // Components
    private MeshRenderer _renderer;
    private Material _material;
    private Color _originalColor;
    
    // Detection
    private Bounds _platformBounds;

    private void Start()
    {
        // Store original transform
        _originalPosition = transform.position;
        _originalScale = transform.localScale;
        
        // Get components
        _renderer = GetComponent<MeshRenderer>();
        
        if (_renderer != null && _renderer.material != null)
        {
            // Create instance of material to avoid affecting other objects
            _material = _renderer.material;
            _originalColor = _material.color;
        }
        
        // Get platform bounds
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            _platformBounds = col.bounds;
        }
    }

    private void FixedUpdate()
    {
        // Update bounds
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            _platformBounds = col.bounds;
        }
        
        // Handle cooldown
        if (_isOnCooldown)
        {
            _cooldownTimer += Time.fixedDeltaTime;
            
            if (_cooldownTimer >= cooldownTime)
            {
                _isOnCooldown = false;
                _cooldownTimer = 0f;
                
                // Restore color
                if (_material != null)
                {
                    _material.color = readyColor;
                }
            }
            
            // Return to original position during cooldown
            if (_compressionProgress > 0f)
            {
                _compressionProgress -= Time.fixedDeltaTime * returnSpeed;
                _compressionProgress = Mathf.Max(0f, _compressionProgress);
                UpdateCompression();
            }
            
            return;
        }
        
        // Check if player is on platform
        bool playerDetected = CheckForPlayerOnPlatform();
        
        if (playerDetected && !_isOnCooldown)
        {
            if (!_playerOnPlatform)
            {
                // Player just landed on trampoline
                _playerOnPlatform = true;
                _isCompressed = true;
                _hasLaunched = false;
            }
            
            // Compress the platform
            if (_isCompressed && !_hasLaunched)
            {
                _compressionProgress += Time.fixedDeltaTime * compressionSpeed;
                
                if (_compressionProgress >= 1f)
                {
                    _compressionProgress = 1f;
                    
                    // Launch the player!
                    LaunchPlayer();
                }
                
                UpdateCompression();
                
                // Update color based on compression
                if (_material != null)
                {
                    _material.color = Color.Lerp(readyColor, compressedColor, _compressionProgress);
                }
            }
        }
        else
        {
            // Player left the platform or not detected
            if (_playerOnPlatform && _hasLaunched)
            {
                // Player was launched and left, start cooldown
                _playerOnPlatform = false;
                _isCompressed = false;
                _isOnCooldown = true;
                _cooldownTimer = 0f;
                
                if (_material != null)
                {
                    _material.color = cooldownColor;
                }
            }
            else if (_playerOnPlatform && !_hasLaunched)
            {
                // Player left before launch completed
                _playerOnPlatform = false;
                _isCompressed = false;
            }
            
            // Return to original position
            if (_compressionProgress > 0f && !_isOnCooldown)
            {
                _compressionProgress -= Time.fixedDeltaTime * returnSpeed;
                _compressionProgress = Mathf.Max(0f, _compressionProgress);
                UpdateCompression();
                
                if (_material != null && _compressionProgress == 0f)
                {
                    _material.color = readyColor;
                }
            }
        }
    }

    private void UpdateCompression()
    {
        // Move platform down
        float currentCompression = _compressionProgress * compressionAmount;
        transform.position = _originalPosition - Vector3.up * currentCompression;
        
        // Slightly squash the platform for visual effect
        float squash = 1f - (_compressionProgress * 0.2f);
        transform.localScale = new Vector3(_originalScale.x, _originalScale.y * squash, _originalScale.z);
    }

    private bool CheckForPlayerOnPlatform()
    {
        // Create an overlap box slightly above the platform
        Vector3 center = _platformBounds.center + Vector3.up * (_platformBounds.extents.y + 0.1f);
        Vector3 halfExtents = new Vector3(_platformBounds.extents.x, 0.5f, _platformBounds.extents.z);
        
        Collider[] colliders = Physics.OverlapBox(center, halfExtents, transform.rotation);
        
        foreach (Collider col in colliders)
        {
            CharacterController cc = col.GetComponent<CharacterController>();
            if (cc != null)
            {
                // Verify the controller is actually standing on this platform
                RaycastHit hit;
                Vector3 rayStart = cc.transform.position;
                
                if (Physics.Raycast(rayStart, Vector3.down, out hit, 2f))
                {
                    if (hit.collider != null && hit.collider.gameObject == gameObject)
                    {
                        return true;
                    }
                }
            }
        }
        
        return false;
    }

    private void LaunchPlayer()
    {
        _hasLaunched = true;
        
        // Find the player and launch them
        Vector3 center = _platformBounds.center + Vector3.up * (_platformBounds.extents.y + 0.1f);
        Vector3 halfExtents = new Vector3(_platformBounds.extents.x, 0.5f, _platformBounds.extents.z);
        
        Collider[] colliders = Physics.OverlapBox(center, halfExtents, transform.rotation);
        
        foreach (Collider col in colliders)
        {
            CharacterController cc = col.GetComponent<CharacterController>();
            if (cc != null)
            {
                // Check if player is on this platform
                RaycastHit hit;
                Vector3 rayStart = cc.transform.position;
                
                if (Physics.Raycast(rayStart, Vector3.down, out hit, 2f))
                {
                    if (hit.collider != null && hit.collider.gameObject == gameObject)
                    {
                        // Try to call the player motor to add upward velocity
                        PlayerMotorCC motor = cc.GetComponent<PlayerMotorCC>();
                        if (motor != null)
                        {
                            // Calculate launch velocity: v = sqrt(2 * g * h) * speed multiplier
                            float launchVelocity = Mathf.Sqrt(2f * 25f * bounceHeight) * bounceSpeed;
                            motor.AddUpwardVelocityThisFrame(launchVelocity);
                            
                            Debug.Log($"Trampoline launched player with velocity: {launchVelocity}");
                        }
                    }
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        // Draw a visual indicator showing bounce height
        Vector3 pos = Application.isPlaying ? _originalPosition : transform.position;
        
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(pos + Vector3.up * bounceHeight, 0.5f);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(pos, pos + Vector3.up * bounceHeight);
        
        // Draw compression indicator
        Gizmos.color = Color.red;
        Gizmos.DrawLine(pos, pos - Vector3.up * compressionAmount);
        Gizmos.DrawWireCube(pos - Vector3.up * compressionAmount * 0.5f, new Vector3(0.5f, compressionAmount, 0.5f));
    }

    /// <summary>
    /// Set the bounce height at runtime
    /// </summary>
    public void SetBounceHeight(float height)
    {
        bounceHeight = height;
    }

    /// <summary>
    /// Set the bounce speed multiplier at runtime
    /// </summary>
    public void SetBounceSpeed(float speed)
    {
        bounceSpeed = speed;
    }

    /// <summary>
    /// Reset the trampoline to its initial state
    /// </summary>
    public void ResetTrampoline()
    {
        _isCompressed = false;
        _playerOnPlatform = false;
        _hasLaunched = false;
        _compressionProgress = 0f;
        _isOnCooldown = false;
        _cooldownTimer = 0f;
        
        transform.position = _originalPosition;
        transform.localScale = _originalScale;
        
        if (_material != null)
        {
            _material.color = readyColor;
        }
    }
}
