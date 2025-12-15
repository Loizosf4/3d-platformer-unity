using UnityEngine;

/// <summary>
/// A platform that rotates in a circular path around a specified axis.
/// Supports players with CharacterController by properly moving them with the platform.
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
    
    [Header("Player Detection")]
    [Tooltip("Layer mask for detecting player")]
    [SerializeField] private LayerMask playerLayer = -1;
    
    [Tooltip("Extra height above platform to check for player")]
    [SerializeField] private float detectionHeight = 0.5f;
    
    [Header("Gizmo Visualization")]
    [Tooltip("Show circular path in Scene view")]
    [SerializeField] private bool showDebugPath = true;
    
    [SerializeField] private Color gizmoColor = Color.cyan;
    
    [Tooltip("Number of segments to draw the circle")]
    [SerializeField] private int gizmoSegments = 64;

    // Internal state
    private Vector3 _centerPoint;
    private float _currentAngle;
    private Vector3 _lastPosition;
    private Vector3 _deltaMovement;
    private Vector3 _platformVelocity;
    
    // Track player on platform
    private PlayerMotorCC _playerMotor;

    private void Start()
    {
        _centerPoint = transform.position;
        _currentAngle = startAngle;
        UpdatePosition();
        _lastPosition = transform.position;
    }

    private void FixedUpdate()
    {
        Vector3 previousPosition = transform.position;
        
        // Increment angle
        _currentAngle += rotationSpeed * Time.fixedDeltaTime;
        if (_currentAngle >= 360f) _currentAngle -= 360f;
        else if (_currentAngle < 0f) _currentAngle += 360f;
        
        // Update position
        UpdatePosition();
        
        // Calculate movement delta
        _deltaMovement = transform.position - previousPosition;
        _platformVelocity = _deltaMovement / Time.fixedDeltaTime;
        _lastPosition = transform.position;
        
        // Move player with platform
        MovePlayerWithPlatform();
    }

    private void UpdatePosition()
    {
        float angleRad = _currentAngle * Mathf.Deg2Rad;
        Vector3 offset = CalculateOffset(angleRad);
        transform.position = _centerPoint + offset;
    }
    
    private Vector3 CalculateOffset(float angleRad)
    {
        if (rotateAroundY && !rotateAroundX && !rotateAroundZ)
        {
            return new Vector3(Mathf.Cos(angleRad) * radius, 0f, Mathf.Sin(angleRad) * radius);
        }
        else if (rotateAroundX && !rotateAroundY && !rotateAroundZ)
        {
            return new Vector3(0f, Mathf.Cos(angleRad) * radius, Mathf.Sin(angleRad) * radius);
        }
        else if (rotateAroundZ && !rotateAroundX && !rotateAroundY)
        {
            return new Vector3(Mathf.Cos(angleRad) * radius, Mathf.Sin(angleRad) * radius, 0f);
        }
        else if (rotateAroundX && rotateAroundY && !rotateAroundZ)
        {
            return new Vector3(
                Mathf.Cos(angleRad) * radius * 0.707f,
                Mathf.Sin(angleRad) * radius * 0.707f,
                Mathf.Cos(angleRad + Mathf.PI * 0.5f) * radius * 0.707f
            );
        }
        // Default to Y axis rotation
        return new Vector3(Mathf.Cos(angleRad) * radius, 0f, Mathf.Sin(angleRad) * radius);
    }

    private void MovePlayerWithPlatform()
    {
        if (_deltaMovement.sqrMagnitude < 0.00001f) return;
        
        BoxCollider col = GetComponent<BoxCollider>();
        if (col == null) return;
        
        Vector3 center = col.bounds.center + Vector3.up * (col.bounds.extents.y + detectionHeight * 0.5f);
        Vector3 halfExtents = new Vector3(col.bounds.extents.x * 0.95f, detectionHeight * 0.5f, col.bounds.extents.z * 0.95f);
        
        Collider[] hits = Physics.OverlapBox(center, halfExtents, transform.rotation, playerLayer);
        
        bool foundPlayer = false;
        
        foreach (Collider hit in hits)
        {
            PlayerMotorCC motor = hit.GetComponent<PlayerMotorCC>();
            if (motor != null && IsPlayerOnPlatform(hit.transform))
            {
                foundPlayer = true;
                motor.SetPlatformMovement(_deltaMovement);
                
                // Set platform state if not already set
                if (_playerMotor != motor)
                {
                    _playerMotor = motor;
                    motor.SetOnPlatform(transform);
                }
            }
        }
        
        // If no player found and we had one, clear the platform state
        if (!foundPlayer && _playerMotor != null)
        {
            _playerMotor.ClearPlatform(_platformVelocity);
            _playerMotor = null;
        }
    }
    
    private bool IsPlayerOnPlatform(Transform player)
    {
        RaycastHit hit;
        Vector3 rayStart = player.position + Vector3.up * 0.1f;
        
        if (Physics.Raycast(rayStart, Vector3.down, out hit, 1.5f))
        {
            return hit.collider != null && hit.collider.transform == transform;
        }
        return false;
    }

    private void OnDrawGizmos()
    {
        if (!showDebugPath) return;

        Vector3 center = Application.isPlaying ? _centerPoint : transform.position;
        Gizmos.color = gizmoColor;
        
        // Draw appropriate circle based on rotation axis
        if (rotateAroundY && !rotateAroundX && !rotateAroundZ)
            DrawCircle(center, Vector3.up, radius);
        else if (rotateAroundX && !rotateAroundY && !rotateAroundZ)
            DrawCircle(center, Vector3.right, radius);
        else if (rotateAroundZ && !rotateAroundX && !rotateAroundY)
            DrawCircle(center, Vector3.forward, radius);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center, 0.2f);
        Gizmos.DrawLine(center, transform.position);
    }

    private void DrawCircle(Vector3 center, Vector3 normal, float circleRadius)
    {
        Vector3 forward = Vector3.Slerp(Vector3.up, Vector3.forward, 0.5f);
        if (Vector3.Dot(normal, forward) > 0.9f) forward = Vector3.right;
        
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

    public Vector3 GetPlatformVelocity() => _platformVelocity;
    public Vector3 GetDeltaMovement() => _deltaMovement;
    public void SetRotationSpeed(float speed) => rotationSpeed = speed;

    public void ResetPlatform()
    {
        _currentAngle = startAngle;
        UpdatePosition();
        _lastPosition = transform.position;
    }
}
