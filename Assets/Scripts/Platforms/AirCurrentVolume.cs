using UnityEngine;

/// <summary>
/// A volume that pushes the player in a specific direction like wind/air current.
/// </summary>
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class AirCurrentVolume : MonoBehaviour
{
    [Header("Air Current Settings")]
    [Tooltip("Direction the air pushes (will be normalized). Use world space or set to local in inspector.")]
    [SerializeField] private Vector3 pushDirection = Vector3.forward;
    
    [Tooltip("If true, push direction is relative to this object's rotation.")]
    [SerializeField] private bool useLocalDirection = true;
    
    [Tooltip("Force applied per second while inside the volume.")]
    [SerializeField] private float pushStrength = 10f;
    
    [Header("Visual Feedback")]
    [Tooltip("Color of the gizmo arrows showing air direction.")]
    [SerializeField] private Color gizmoColor = new Color(0.5f, 0.8f, 1f, 0.6f);
    
    [Tooltip("Show multiple arrows to visualize the volume.")]
    [SerializeField] private bool showDetailedGizmos = true;

    [Header("Audio")]
    [Tooltip("Looping sound played while player is inside the air current.")]
    [SerializeField] private AudioClip windSound;
    [Tooltip("Volume for wind sound (0-1).")]
    [SerializeField, Range(0f, 1f)] private float windVolume = 0.5f;
    [Tooltip("Spatial blend. 0 = 2D, 1 = 3D.")]
    [SerializeField, Range(0f, 1f)] private float spatialBlend = 1f;
    
    private Collider _collider;
    private Vector3 _worldPushDirection;
    private AudioSource _audioSource;
    private bool _playerInside;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;

        var rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    private void Start()
    {
        _collider = GetComponent<Collider>();
        UpdateWorldDirection();

        if (windSound != null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.clip = windSound;
            _audioSource.loop = true;
            _audioSource.volume = windVolume;
            _audioSource.spatialBlend = spatialBlend;

            if (AudioManager.Instance != null && AudioManager.Instance.audioMixer != null)
            {
                var sfxGroup = AudioManager.Instance.audioMixer.FindMatchingGroups("SFX");
                if (sfxGroup != null && sfxGroup.Length > 0)
                    _audioSource.outputAudioMixerGroup = sfxGroup[0];
            }

            _audioSource.Play();
        }
    }

    private void Update()
    {
        // Update direction each frame in case object rotates
        UpdateWorldDirection();
    }

    private void UpdateWorldDirection()
    {
        if (useLocalDirection)
        {
            _worldPushDirection = transform.TransformDirection(pushDirection.normalized);
        }
        else
        {
            _worldPushDirection = pushDirection.normalized;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        var motor = other.GetComponentInParent<PlayerMotorCC>();
        if (motor == null) return;

        _playerInside = true;
    }

    private void OnTriggerStay(Collider other)
    {
        var motor = other.GetComponentInParent<PlayerMotorCC>();
        if (motor == null) return;

        // Apply directional push force
        Vector3 pushForce = _worldPushDirection * pushStrength * Time.deltaTime;
        motor.AddDirectionalForce(pushForce);
    }

    private void OnTriggerExit(Collider other)
    {
        var motor = other.GetComponentInParent<PlayerMotorCC>();
        if (motor == null) return;

        _playerInside = false;
    }

    private void OnDrawGizmos()
    {
        if (_collider == null)
            _collider = GetComponent<Collider>();

        UpdateWorldDirection();
        
        Gizmos.color = gizmoColor;
        
        if (_collider != null)
        {
            Bounds bounds = _collider.bounds;
            
            // Draw semi-transparent box
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.2f);
            Gizmos.DrawCube(bounds.center, bounds.size);
            
            // Draw wireframe
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
            
            if (showDetailedGizmos)
            {
                // Draw multiple arrows showing flow direction
                DrawFlowArrows(bounds);
            }
            else
            {
                // Draw single arrow at center
                DrawArrow(bounds.center, _worldPushDirection, bounds.size.magnitude * 0.5f);
            }
        }
    }

    private void DrawFlowArrows(Bounds bounds)
    {
        // Draw a grid of arrows
        int gridSize = 3;
        Vector3 step = new Vector3(
            bounds.size.x / (gridSize + 1),
            bounds.size.y / (gridSize + 1),
            bounds.size.z / (gridSize + 1)
        );
        
        Vector3 start = bounds.min + step;
        
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                for (int z = 0; z < gridSize; z++)
                {
                    Vector3 pos = start + new Vector3(x * step.x, y * step.y, z * step.z);
                    DrawArrow(pos, _worldPushDirection, step.magnitude * 0.8f);
                }
            }
        }
    }

    private void DrawArrow(Vector3 pos, Vector3 direction, float length)
    {
        if (direction.sqrMagnitude < 0.001f) return;
        
        Vector3 end = pos + direction * length;
        
        // Draw main line
        Gizmos.DrawLine(pos, end);
        
        // Draw arrowhead
        Vector3 right = Vector3.Cross(direction, Vector3.up);
        if (right.sqrMagnitude < 0.001f)
            right = Vector3.Cross(direction, Vector3.right);
        right.Normalize();
        
        Vector3 up = Vector3.Cross(right, direction).normalized;
        
        float arrowSize = length * 0.2f;
        Gizmos.DrawLine(end, end - direction * arrowSize + right * arrowSize * 0.5f);
        Gizmos.DrawLine(end, end - direction * arrowSize - right * arrowSize * 0.5f);
        Gizmos.DrawLine(end, end - direction * arrowSize + up * arrowSize * 0.5f);
        Gizmos.DrawLine(end, end - direction * arrowSize - up * arrowSize * 0.5f);
    }
}
