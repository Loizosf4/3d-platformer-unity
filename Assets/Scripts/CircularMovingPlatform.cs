using UnityEngine;

/// <summary>
/// A platform that rotates in a circular path around a specified axis.
/// Supports players with CharacterController by moving them along with the platform.
/// </summary>
public class CircularMovingPlatform : MonoBehaviour
{
    [Header("Rotation Settings")]
    [Tooltip("Rotate around X axis")]
    [SerializeField] private bool rotateAroundX = false;
    
    [Tooltip("Rotate around Y axis (horizontal circle)")]
    [SerializeField] private bool rotateAroundY = true;
    
    [Tooltip("Rotate around Z axis")]
    [SerializeField] private bool rotateAroundZ = false;
    
    [Header("Movement Settings")]
    [Tooltip("Radius of the circular path")]
    [SerializeField] private float radius = 5f;
    
    [Tooltip("Speed of rotation in degrees per second")]
    [SerializeField] private float rotationSpeed = 30f;
    
    [Tooltip("Start angle in degrees")]
    [SerializeField] private float startAngle = 0f;
    
    [Header("Gizmo Visualization")]
    [Tooltip("Show circular path in Scene view")]
    [SerializeField] private bool showDebugPath = true;
    
    [SerializeField] private Color gizmoColor = Color.cyan;
    
    [Tooltip("Number of segments to draw the circle (higher = smoother)")]
    [SerializeField] private int gizmoSegments = 64;

    // Internal state
    private Vector3 _centerPoint;
    private float _currentAngle;
    private Vector3 _lastPosition;
    private Vector3 _platformMovement;
    
    // Platform bounds for detection
    private Bounds _platformBounds;

    private void Start()
    {
        // Store the initial position as the center point of rotation
        _centerPoint = transform.position;
        _currentAngle = startAngle;
        
        // Position the platform at the starting angle
        UpdatePosition();
        
        _lastPosition = transform.position;
        
        // Get platform bounds
        BoxCollider mainCollider = GetComponent<BoxCollider>();
        if (mainCollider != null)
        {
            _platformBounds = mainCollider.bounds;
        }
        
        Debug.Log($"Circular platform initialized. Center: {_centerPoint}, Radius: {radius}");
    }

    private void FixedUpdate()
    {
        // Increment angle based on speed
        _currentAngle += rotationSpeed * Time.fixedDeltaTime;
        
        // Keep angle in 0-360 range
        if (_currentAngle >= 360f)
            _currentAngle -= 360f;
        else if (_currentAngle < 0f)
            _currentAngle += 360f;
        
        // Update platform position
        UpdatePosition();
        
        // Calculate platform movement
        _platformMovement = transform.position - _lastPosition;
        _lastPosition = transform.position;
        
        // Update platform bounds
        BoxCollider mainCollider = GetComponent<BoxCollider>();
        if (mainCollider != null)
        {
            _platformBounds = mainCollider.bounds;
        }
        
        // Move any objects on the platform
        MoveObjectsOnPlatform();
    }

    private void UpdatePosition()
    {
        float angleRad = _currentAngle * Mathf.Deg2Rad;
        Vector3 offset = Vector3.zero;
        
        // Calculate offset based on selected axes
        // For combined rotations, we create a position that satisfies both constraints
        
        if (rotateAroundX && rotateAroundY && !rotateAroundZ)
        {
            // Rotate in YZ plane (circle perpendicular to X axis) and XZ plane (perpendicular to Y)
            // This creates a diagonal circular path
            offset = new Vector3(
                Mathf.Cos(angleRad) * radius * 0.707f,
                Mathf.Sin(angleRad) * radius * 0.707f,
                Mathf.Cos(angleRad + Mathf.PI * 0.5f) * radius * 0.707f
            );
        }
        else if (rotateAroundX && rotateAroundZ && !rotateAroundY)
        {
            // Rotate in YZ and XY planes
            offset = new Vector3(
                Mathf.Cos(angleRad) * radius * 0.707f,
                Mathf.Sin(angleRad + Mathf.PI * 0.5f) * radius * 0.707f,
                Mathf.Sin(angleRad) * radius * 0.707f
            );
        }
        else if (rotateAroundY && rotateAroundZ && !rotateAroundX)
        {
            // Rotate in XZ and XY planes
            offset = new Vector3(
                Mathf.Cos(angleRad + Mathf.PI * 0.5f) * radius * 0.707f,
                Mathf.Sin(angleRad) * radius * 0.707f,
                Mathf.Cos(angleRad) * radius * 0.707f
            );
        }
        else if (rotateAroundX && !rotateAroundY && !rotateAroundZ)
        {
            // Rotate around X axis (circle in YZ plane)
            offset = new Vector3(
                0f,
                Mathf.Cos(angleRad) * radius,
                Mathf.Sin(angleRad) * radius
            );
        }
        else if (rotateAroundY && !rotateAroundX && !rotateAroundZ)
        {
            // Rotate around Y axis (circle in XZ plane) - most common
            offset = new Vector3(
                Mathf.Cos(angleRad) * radius,
                0f,
                Mathf.Sin(angleRad) * radius
            );
        }
        else if (rotateAroundZ && !rotateAroundX && !rotateAroundY)
        {
            // Rotate around Z axis (circle in XY plane)
            offset = new Vector3(
                Mathf.Cos(angleRad) * radius,
                Mathf.Sin(angleRad) * radius,
                0f
            );
        }
        else if (rotateAroundX && rotateAroundY && rotateAroundZ)
        {
            // All three axes - create a 3D spiral/complex path
            offset = new Vector3(
                Mathf.Cos(angleRad) * radius * 0.577f,
                Mathf.Sin(angleRad) * radius * 0.577f,
                Mathf.Sin(angleRad + Mathf.PI * 0.5f) * radius * 0.577f
            );
        }
        
        transform.position = _centerPoint + offset;
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

        Vector3 center = Application.isPlaying ? _centerPoint : transform.position;
        
        Gizmos.color = gizmoColor;
        
        // Draw circle(s) based on selected axes
        if (rotateAroundX && !rotateAroundY && !rotateAroundZ)
        {
            DrawCircle(center, Vector3.right, radius);
        }
        else if (rotateAroundY && !rotateAroundX && !rotateAroundZ)
        {
            DrawCircle(center, Vector3.up, radius);
        }
        else if (rotateAroundZ && !rotateAroundX && !rotateAroundY)
        {
            DrawCircle(center, Vector3.forward, radius);
        }
        else if (rotateAroundX && rotateAroundY && !rotateAroundZ)
        {
            // Draw both circles
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.5f);
            DrawCircle(center, Vector3.right, radius);
            DrawCircle(center, Vector3.up, radius);
        }
        else if (rotateAroundX && rotateAroundZ && !rotateAroundY)
        {
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.5f);
            DrawCircle(center, Vector3.right, radius);
            DrawCircle(center, Vector3.forward, radius);
        }
        else if (rotateAroundY && rotateAroundZ && !rotateAroundX)
        {
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.5f);
            DrawCircle(center, Vector3.up, radius);
            DrawCircle(center, Vector3.forward, radius);
        }
        else if (rotateAroundX && rotateAroundY && rotateAroundZ)
        {
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.3f);
            DrawCircle(center, Vector3.right, radius);
            DrawCircle(center, Vector3.up, radius);
            DrawCircle(center, Vector3.forward, radius);
        }
        
        // Draw center point
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center, 0.2f);
        
        // Draw line from center to platform
        Gizmos.color = gizmoColor;
        Gizmos.DrawLine(center, transform.position);
    }

    private void DrawCircle(Vector3 center, Vector3 normal, float circleRadius)
    {
        Vector3 forward = Vector3.Slerp(Vector3.up, Vector3.forward, 0.5f);
        if (Vector3.Dot(normal, forward) > 0.9f)
            forward = Vector3.right;
        
        Vector3 right = Vector3.Cross(normal, forward).normalized;
        forward = Vector3.Cross(right, normal).normalized;
        
        Vector3 lastPoint = center + forward * circleRadius;
        
        for (int i = 1; i <= gizmoSegments; i++)
        {
            float angle = (i / (float)gizmoSegments) * Mathf.PI * 2f;
            Vector3 point = center + (forward * Mathf.Cos(angle) + right * Mathf.Sin(angle)) * circleRadius;
            Gizmos.DrawLine(lastPoint, point);
            lastPoint = point;
        }
    }

    /// <summary>
    /// Set the rotation speed at runtime
    /// </summary>
    public void SetRotationSpeed(float speed)
    {
        rotationSpeed = speed;
    }

    /// <summary>
    /// Set the radius at runtime
    /// </summary>
    public void SetRadius(float newRadius)
    {
        radius = newRadius;
        _centerPoint = transform.position - GetCurrentOffset();
    }

    /// <summary>
    /// Reset the platform to starting position
    /// </summary>
    public void ResetPlatform()
    {
        _currentAngle = startAngle;
        UpdatePosition();
        _lastPosition = transform.position;
    }

    private Vector3 GetCurrentOffset()
    {
        return transform.position - _centerPoint;
    }
}
