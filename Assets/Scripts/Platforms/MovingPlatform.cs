using UnityEngine;

/// <summary>
/// A platform that moves back and forth between two points.
/// Supports players with CharacterController by moving them along with the platform.
/// </summary>
public class MovingPlatform : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Direction of movement (normalized automatically)")]
    [SerializeField] private Vector3 moveDirection = Vector3.right;
    
    [Tooltip("Distance to travel in the specified direction")]
    [SerializeField] private float moveDistance = 5f;
    
    [Tooltip("Speed of platform movement")]
    [SerializeField] private float moveSpeed = 2f;
    
    [Header("Movement Type")]
    [Tooltip("Smooth easing movement (slower at endpoints) vs constant speed")]
    [SerializeField] private bool useSmoothMovement = true;
    
    [Header("Pause Settings")]
    [Tooltip("Time to wait at each endpoint before reversing")]
    [SerializeField] private float pauseTime = 0.5f;
    
    [Header("Gizmo Visualization")]
    [Tooltip("Show movement path in Scene view")]
    [SerializeField] private bool showDebugPath = true;
    
    [SerializeField] private Color gizmoColor = Color.yellow;

    // Internal state
    private Vector3 _startPosition;
    private Vector3 _endPosition;
    private float _progress = 0f;
    private bool _movingForward = true;
    private float _pauseTimer = 0f;
    private bool _isPaused = false;
    
    // Platform movement tracking for CharacterController
    private Vector3 _lastPosition;
    private Vector3 _platformVelocity;
    private Vector3 _platformMovement;
    
    // Track objects on the platform
    private System.Collections.Generic.HashSet<CharacterController> _controllersOnPlatform = new System.Collections.Generic.HashSet<CharacterController>();
    
    // Platform bounds for detection
    private Bounds _platformBounds;

    private void Start()
    {
        // Store initial position as start point
        _startPosition = transform.position;
        
        // Normalize direction and calculate end position
        moveDirection = moveDirection.normalized;
        _endPosition = _startPosition + (moveDirection * moveDistance);
        
        _lastPosition = transform.position;
        
        // Get platform bounds
        BoxCollider mainCollider = GetComponent<BoxCollider>();
        if (mainCollider != null)
        {
            _platformBounds = mainCollider.bounds;
        }
    }
    
    private void FixedUpdate()
    {
        // Handle pause at endpoints
        if (_isPaused)
        {
            _pauseTimer += Time.fixedDeltaTime;
            if (_pauseTimer >= pauseTime)
            {
                _isPaused = false;
                _pauseTimer = 0f;
                _movingForward = !_movingForward;
            }
            _platformVelocity = Vector3.zero;
            _platformMovement = Vector3.zero;
            return;
        }

        // Calculate movement
        float step = moveSpeed * Time.fixedDeltaTime;
        
        if (useSmoothMovement)
        {
            // Use smooth interpolation
            if (_movingForward)
            {
                _progress += step / moveDistance;
                if (_progress >= 1f)
                {
                    _progress = 1f;
                    _isPaused = true;
                }
            }
            else
            {
                _progress -= step / moveDistance;
                if (_progress <= 0f)
                {
                    _progress = 0f;
                    _isPaused = true;
                }
            }
            
            // Apply smooth easing
            float smoothProgress = Mathf.SmoothStep(0f, 1f, _progress);
            transform.position = Vector3.Lerp(_startPosition, _endPosition, smoothProgress);
        }
        else
        {
            // Constant speed movement
            if (_movingForward)
            {
                _progress += step / moveDistance;
                if (_progress >= 1f)
                {
                    _progress = 1f;
                    _isPaused = true;
                }
            }
            else
            {
                _progress -= step / moveDistance;
                if (_progress <= 0f)
                {
                    _progress = 0f;
                    _isPaused = true;
                }
            }
            
            transform.position = Vector3.Lerp(_startPosition, _endPosition, _progress);
        }
        
        // Calculate platform movement
        _platformMovement = transform.position - _lastPosition;
        _platformVelocity = _platformMovement / Time.fixedDeltaTime;
        _lastPosition = transform.position;
        
        // Update platform bounds
        BoxCollider mainCollider = GetComponent<BoxCollider>();
        if (mainCollider != null)
        {
            _platformBounds = mainCollider.bounds;
        }
        
        // Immediately move any objects on the platform
        MoveObjectsOnPlatform();
    }
    
    private void MoveObjectsOnPlatform()
    {
        if (_platformMovement.sqrMagnitude < 0.0001f) return;
        
        // Create an overlap box slightly above the platform
        Vector3 center = _platformBounds.center + Vector3.up * (_platformBounds.extents.y + 0.1f);
        Vector3 halfExtents = new Vector3(_platformBounds.extents.x, 0.5f, _platformBounds.extents.z);
        
        Collider[] colliders = Physics.OverlapBox(center, halfExtents, transform.rotation);
        
        foreach (Collider col in colliders)
        {
            CharacterController cc = col.GetComponent<CharacterController>();
            if (cc != null)
            {
                // Check if grounded on this platform using a raycast
                RaycastHit hit;
                Vector3 rayStart = cc.transform.position;
                
                if (Physics.Raycast(rayStart, Vector3.down, out hit, 2f))
                {
                    if (hit.collider != null && hit.collider.gameObject == gameObject)
                    {
                        cc.Move(_platformMovement);
                    }
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (!showDebugPath) return;

        // Calculate positions for gizmo
        Vector3 startPos = Application.isPlaying ? _startPosition : transform.position;
        Vector3 direction = moveDirection.normalized;
        Vector3 endPos = startPos + (direction * moveDistance);

        // Draw line showing movement path
        Gizmos.color = gizmoColor;
        Gizmos.DrawLine(startPos, endPos);
        
        // Draw spheres at endpoints
        Gizmos.DrawWireSphere(startPos, 0.3f);
        Gizmos.DrawWireSphere(endPos, 0.3f);
        
        // Draw arrow indicating direction
        Vector3 arrowMid = startPos + (direction * moveDistance * 0.5f);
        Vector3 perpendicular = Vector3.Cross(direction, Vector3.up).normalized;
        if (perpendicular == Vector3.zero) perpendicular = Vector3.Cross(direction, Vector3.right).normalized;
        
        Gizmos.DrawLine(endPos, endPos - direction * 0.5f + perpendicular * 0.25f);
        Gizmos.DrawLine(endPos, endPos - direction * 0.5f - perpendicular * 0.25f);
    }

    private void OnDrawGizmosSelected()
    {
        if (!showDebugPath) return;

        // Draw more detailed info when selected
        Vector3 startPos = Application.isPlaying ? _startPosition : transform.position;
        Vector3 direction = moveDirection.normalized;
        Vector3 endPos = startPos + (direction * moveDistance);

        // Draw distance markers
        Gizmos.color = Color.cyan;
        int markers = Mathf.Max(2, Mathf.CeilToInt(moveDistance / 2f));
        for (int i = 0; i <= markers; i++)
        {
            float t = i / (float)markers;
            Vector3 markerPos = Vector3.Lerp(startPos, endPos, t);
            Gizmos.DrawWireCube(markerPos, Vector3.one * 0.2f);
        }
    }

    /// <summary>
    /// Get the current velocity of the platform (useful for external scripts)
    /// </summary>
    public Vector3 GetPlatformVelocity()
    {
        return _platformVelocity;
    }

    /// <summary>
    /// Reset the platform to its starting position
    /// </summary>
    public void ResetPlatform()
    {
        _progress = 0f;
        _movingForward = true;
        _isPaused = false;
        _pauseTimer = 0f;
        transform.position = _startPosition;
        _lastPosition = _startPosition;
        _platformVelocity = Vector3.zero;
    }
}
