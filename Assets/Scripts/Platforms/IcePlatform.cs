using UnityEngine;

/// <summary>
/// A slippery ice platform that reduces player friction and control.
/// Player continues sliding after stopping input, and effect persists briefly after leaving.
/// </summary>
public class IcePlatform : MonoBehaviour
{
    [Header("Ice Properties")]
    [Tooltip("How slippery the ice is (0 = normal, 1 = very slippery)")]
    [Range(0f, 1f)]
    [SerializeField] private float slipperiness = 0.8f;
    
    [Tooltip("How long the icy effect lasts after leaving the platform (seconds)")]
    [SerializeField] private float icyFeetDuration = 1.5f;
    
    [Tooltip("Speed multiplier on ice (1.0 = normal speed, 1.5 = 50% faster)")]
    [SerializeField] private float speedMultiplier = 1.3f;
    
    [Header("Visual Feedback")]
    [Tooltip("Color tint when on ice")]
    [SerializeField] private Color iceColor = new Color(0.7f, 0.9f, 1f, 1f);
    
    // Detection
    private Bounds _platformBounds;
    private Collider _collider;
    private MeshRenderer _renderer;
    private Material _material;
    private Color _originalColor;

    private void Start()
    {
        // Get components
        _collider = GetComponent<Collider>();
        _renderer = GetComponent<MeshRenderer>();
        
        if (_renderer != null && _renderer.material != null)
        {
            _material = _renderer.material;
            _originalColor = _material.color;
            _material.color = iceColor;
        }
        
        // Get platform bounds
        if (_collider != null)
        {
            _platformBounds = _collider.bounds;
        }
    }

    private void FixedUpdate()
    {
        // Update bounds
        if (_collider != null)
        {
            _platformBounds = _collider.bounds;
        }
        
        // Check if player is on ice platform
        CheckForPlayerOnIce();
    }

    private void CheckForPlayerOnIce()
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
                        // Apply ice effect to player
                        PlayerMotorCC motor = cc.GetComponent<PlayerMotorCC>();
                        if (motor != null)
                        {
                            motor.ApplyIceEffect(slipperiness, icyFeetDuration, speedMultiplier);
                        }
                    }
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        // Draw ice platform indicator
        Gizmos.color = new Color(0.5f, 0.8f, 1f, 0.3f);
        
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.DrawCube(col.bounds.center, col.bounds.size);
        }
    }

    /// <summary>
    /// Set the slipperiness at runtime
    /// </summary>
    public void SetSlipperiness(float value)
    {
        slipperiness = Mathf.Clamp01(value);
    }

    /// <summary>
    /// Set the icy feet duration at runtime
    /// </summary>
    public void SetIcyFeetDuration(float duration)
    {
        icyFeetDuration = Mathf.Max(0f, duration);
    }
}
