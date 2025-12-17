using UnityEngine;

/// <summary>
/// A platform that disappears after the player stands on it for a brief moment,
/// then reappears after a delay.
/// </summary>
public class DisappearingPlatform : MonoBehaviour
{
    [Header("Disappear Settings")]
    [Tooltip("Time the player must be on the platform before it starts to disappear")]
    [SerializeField] private float timeBeforeDisappear = 0.5f;
    
    [Tooltip("Time the platform stays disappeared before reappearing")]
    [SerializeField] private float disappearDuration = 2f;
    
    [Header("Visual Feedback")]
    [Tooltip("Make platform blink before disappearing")]
    [SerializeField] private bool blinkBeforeDisappear = true;
    
    [Tooltip("Blink speed when about to disappear")]
    [SerializeField] private float blinkSpeed = 10f;
    
    // Internal state
    private bool _playerOnPlatform = false;
    private float _timePlayerOnPlatform = 0f;
    private bool _isDisappeared = false;
    private float _disappearTimer = 0f;
    private bool _disappearanceTriggered = false; // Track if disappearance has been initiated
    
    // Components
    private Collider _collider;
    private MeshCollider _meshCollider;
    private MeshRenderer _renderer;
    private Material _material;
    private Color _originalColor;
    
    // Detection
    private Bounds _platformBounds;

    private void Start()
    {
        // Get components
        _collider = GetComponent<Collider>();
        _meshCollider = GetComponent<MeshCollider>();
        _renderer = GetComponent<MeshRenderer>();
        
        if (_renderer != null && _renderer.material != null)
        {
            // Create instance of material to avoid affecting other objects
            _material = _renderer.material;
            _originalColor = _material.color;
        }
        
        // Get platform bounds
        if (_collider != null)
        {
            _platformBounds = _collider.bounds;
        }
    }

    private void FixedUpdate()
    {
        if (_isDisappeared)
        {
            // Count down until reappear
            _disappearTimer += Time.fixedDeltaTime;
            
            if (_disappearTimer >= disappearDuration)
            {
                Reappear();
            }
            
            return;
        }
        
        // Update bounds
        if (_collider != null)
        {
            _platformBounds = _collider.bounds;
        }
        
        // Check if player is on platform
        bool playerDetected = CheckForPlayerOnPlatform();
        
        if (playerDetected)
        {
            if (!_playerOnPlatform)
            {
                // Player just landed on platform - trigger inevitable disappearance
                _playerOnPlatform = true;
                _timePlayerOnPlatform = 0f;
                _disappearanceTriggered = true;
            }
        }
        
        // Continue countdown even if player leaves once triggered
        if (_disappearanceTriggered)
        {
            _timePlayerOnPlatform += Time.fixedDeltaTime;
            
            // Visual feedback - blink when close to disappearing
            if (blinkBeforeDisappear && _material != null)
            {
                float blinkProgress = _timePlayerOnPlatform / timeBeforeDisappear;
                
                if (blinkProgress > 0.3f) // Start blinking at 30% of time
                {
                    float blink = Mathf.PingPong(Time.time * blinkSpeed, 1f);
                    _material.color = Color.Lerp(_originalColor, Color.clear, blink * blinkProgress);
                }
            }
            
            // Check if time to disappear
            if (_timePlayerOnPlatform >= timeBeforeDisappear)
            {
                Disappear();
            }
        }
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

    private void Disappear()
    {
        _isDisappeared = true;
        _disappearTimer = 0f;
        _playerOnPlatform = false;
        _timePlayerOnPlatform = 0f;
        _disappearanceTriggered = false; // Reset for next cycle
        
        // Disable collider so player falls through
        if (_collider != null)
        {
            _collider.enabled = false;
        }
        
        // Also disable mesh collider specifically if present
        if (_meshCollider != null)
        {
            _meshCollider.enabled = false;
        }
        
        // Hide visual
        if (_renderer != null)
        {
            _renderer.enabled = false;
        }
    }

    private void Reappear()
    {
        _isDisappeared = false;
        _disappearTimer = 0f;
        
        // Re-enable collider
        if (_collider != null)
        {
            _collider.enabled = true;
        }
        
        // Re-enable mesh collider specifically if present
        if (_meshCollider != null)
        {
            _meshCollider.enabled = true;
        }
        
        // Show visual
        if (_renderer != null)
        {
            _renderer.enabled = true;
        }
        
        // Restore original color
        if (_material != null)
        {
            _material.color = _originalColor;
        }
    }

    private void OnDrawGizmos()
    {
        // Draw a visual indicator of the platform state in Scene view
        if (_isDisappeared)
        {
            Gizmos.color = Color.red;
        }
        else if (_playerOnPlatform)
        {
            Gizmos.color = Color.yellow;
        }
        else
        {
            Gizmos.color = Color.green;
        }
        
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
    }

    /// <summary>
    /// Force the platform to disappear immediately
    /// </summary>
    public void ForceDisappear()
    {
        Disappear();
    }

    /// <summary>
    /// Force the platform to reappear immediately
    /// </summary>
    public void ForceReappear()
    {
        Reappear();
    }

    /// <summary>
    /// Reset the platform to its initial state
    /// </summary>
    public void ResetPlatform()
    {
        _isDisappeared = false;
        _playerOnPlatform = false;
        _timePlayerOnPlatform = 0f;
        _disappearTimer = 0f;
        _disappearanceTriggered = false;
        
        if (_collider != null) _collider.enabled = true;
        if (_meshCollider != null) _meshCollider.enabled = true;
        if (_renderer != null) _renderer.enabled = true;
        if (_material != null) _material.color = _originalColor;
    }
}
