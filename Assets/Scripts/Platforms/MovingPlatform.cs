using UnityEngine;

/// <summary>
/// A platform that moves back and forth between two points.
/// Supports players with CharacterController by parenting them to the platform.
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
    
    [Header("Player Detection")]
    [Tooltip("Layer mask for detecting player")]
    [SerializeField] private LayerMask playerLayer = -1;
    
    [Tooltip("Extra height above platform to check for player")]
    [SerializeField] private float detectionHeight = 0.5f;
    
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
    
    // Platform movement tracking
    private Vector3 _lastPosition;
    private Vector3 _platformVelocity;
    private Vector3 _deltaMovement;
    
    // Track player on platform
    private Transform _playerOnPlatform;
    private PlayerMotorCC _playerMotor;
    private Vector3 _playerLocalPos;
    private bool _playerWasParented;

    private void Start()
    {
        _startPosition = transform.position;
        moveDirection = moveDirection.normalized;
        _endPosition = _startPosition + (moveDirection * moveDistance);
        _lastPosition = transform.position;
    }
    
    private void FixedUpdate()
    {
        Vector3 previousPosition = transform.position;
        
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
            _deltaMovement = Vector3.zero;
            return;
        }

        // Calculate movement progress
        float step = moveSpeed * Time.fixedDeltaTime;
        
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
        
        // Apply position
        float t = useSmoothMovement ? Mathf.SmoothStep(0f, 1f, _progress) : _progress;
        transform.position = Vector3.Lerp(_startPosition, _endPosition, t);
        
        // Calculate delta movement
        _deltaMovement = transform.position - previousPosition;
        _platformVelocity = _deltaMovement / Time.fixedDeltaTime;
        _lastPosition = transform.position;
        
        // Move player with platform
        MovePlayerWithPlatform();
    }
    
    private void MovePlayerWithPlatform()
    {
        if (_deltaMovement.sqrMagnitude < 0.00001f) return;
        
        // Find player on platform using overlap box
        BoxCollider col = GetComponent<BoxCollider>();
        if (col == null) return;
        
        Vector3 center = col.bounds.center + Vector3.up * (col.bounds.extents.y + detectionHeight * 0.5f);
        Vector3 halfExtents = new Vector3(col.bounds.extents.x * 0.95f, detectionHeight * 0.5f, col.bounds.extents.z * 0.95f);
        
        Collider[] hits = Physics.OverlapBox(center, halfExtents, transform.rotation, playerLayer);
        
        bool foundPlayer = false;
        
        foreach (Collider hit in hits)
        {
            // Check for PlayerMotorCC and use its platform movement system
            PlayerMotorCC motor = hit.GetComponent<PlayerMotorCC>();
            if (motor != null)
            {
                // Verify the player is actually standing on this platform
                if (IsPlayerOnPlatform(hit.transform))
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
        // Raycast down from player to check if they're on this platform
        RaycastHit hit;
        Vector3 rayStart = player.position + Vector3.up * 0.1f;
        
        if (Physics.Raycast(rayStart, Vector3.down, out hit, 1.5f))
        {
            return hit.collider != null && hit.collider.transform == transform;
        }
        return false;
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            // Only attach if player is landing on top (collision normal points up)
            bool isLandingOnTop = false;
            foreach (ContactPoint contact in collision.contacts)
            {
                if (contact.normal.y > 0.5f) // Normal pointing upward
                {
                    isLandingOnTop = true;
                    break;
                }
            }
            
            if (isLandingOnTop)
            {
                _playerOnPlatform = collision.transform;
                _playerMotor = collision.collider.GetComponent<PlayerMotorCC>();
                if (_playerMotor != null)
                {
                    _playerMotor.SetOnPlatform(transform);
                }
            }
        }
    }
    
    private void OnCollisionStay(Collision collision)
    {
        if (collision.collider.CompareTag("Player") && _playerMotor != null)
        {
            _playerMotor.SetPlatformMovement(_deltaMovement);
        }
    }
    
    private void OnCollisionExit(Collision collision)
    {
        if (collision.collider.CompareTag("Player") && _playerOnPlatform == collision.transform)
        {
            if (_playerMotor != null)
            {
                _playerMotor.ClearPlatform(_platformVelocity);
            }
            _playerMotor = null;
            _playerOnPlatform = null;
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // Alternative detection using trigger - only attach if player is grounded
        if (other.CompareTag("Player"))
        {
            _playerOnPlatform = other.transform;
            _playerMotor = other.GetComponent<PlayerMotorCC>();
            if (_playerMotor != null && _playerMotor.IsGrounded)
            {
                _playerMotor.SetOnPlatform(transform);
            }
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && _playerOnPlatform == other.transform)
        {
            // Only clear if player is actually leaving the platform (not just becoming airborne briefly)
            if (_playerMotor != null && !IsPlayerOnPlatform(other.transform))
            {
                _playerMotor.ClearPlatform(_platformVelocity);
                _playerMotor = null;
                _playerOnPlatform = null;
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (!showDebugPath) return;

        Vector3 startPos = Application.isPlaying ? _startPosition : transform.position;
        Vector3 direction = moveDirection.normalized;
        Vector3 endPos = startPos + (direction * moveDistance);

        Gizmos.color = gizmoColor;
        Gizmos.DrawLine(startPos, endPos);
        Gizmos.DrawWireSphere(startPos, 0.3f);
        Gizmos.DrawWireSphere(endPos, 0.3f);
        
        // Draw arrow
        Vector3 perpendicular = Vector3.Cross(direction, Vector3.up).normalized;
        if (perpendicular == Vector3.zero) perpendicular = Vector3.Cross(direction, Vector3.right).normalized;
        Gizmos.DrawLine(endPos, endPos - direction * 0.5f + perpendicular * 0.25f);
        Gizmos.DrawLine(endPos, endPos - direction * 0.5f - perpendicular * 0.25f);
    }

    public Vector3 GetPlatformVelocity() => _platformVelocity;
    public Vector3 GetDeltaMovement() => _deltaMovement;

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
