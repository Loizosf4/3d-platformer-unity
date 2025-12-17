using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SpeedBoostZone : MonoBehaviour
{
    [Header("Speed Boost")]
    [Tooltip("How much to multiply the player's speed (2.0 = double speed)")]
    [SerializeField] private float boostAmount = 1.5f;
    
    [Tooltip("How long the boost effect lingers after leaving the zone")]
    [SerializeField] private float lingerDuration = 0.35f;
    
    [Header("Zone Setup")]
    [Tooltip("If true, automatically resizes the trigger collider to match the mesh bounds on Start")]
    [SerializeField] private bool autoFitToMesh = false;
    
    [Tooltip("If true, use manual size settings below instead of auto-fit")]
    [SerializeField] private bool useManualSize = true;
    
    [Header("Manual Zone Size (when useManualSize = true)")]
    [Tooltip("Size of the boost zone trigger collider")]
    [SerializeField] private Vector3 zoneSize = new Vector3(5f, 2f, 5f);
    
    [Tooltip("Center offset of the boost zone trigger collider")]
    [SerializeField] private Vector3 zoneCenter = Vector3.zero;
    
    [Header("Auto-Fit Settings (when autoFitToMesh = true)")]
    [Tooltip("Extra padding added to auto-fitted collider (useful for larger detection area)")]
    [SerializeField] private Vector3 colliderPadding = new Vector3(0.2f, 0.5f, 0.2f);

    private Collider _collider;

    private void Awake()
    {
        _collider = GetComponent<Collider>();
    }
    
    private void Start()
    {
        if (_collider is BoxCollider boxCollider)
        {
            if (useManualSize)
            {
                // Use manual size settings
                boxCollider.size = zoneSize;
                boxCollider.center = zoneCenter;
                Debug.Log($"SpeedBoostZone: Set manual size - Size: {zoneSize}, Center: {zoneCenter}");
            }
            else if (autoFitToMesh)
            {
                // Auto-fit to mesh
                FitColliderToMesh();
            }
        }
    }
    
    private void FitColliderToMesh()
    {
        // Try to find a mesh renderer on this object, children, or parent
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            meshRenderer = GetComponentInChildren<MeshRenderer>();
        }
        if (meshRenderer == null && transform.parent != null)
        {
            meshRenderer = transform.parent.GetComponent<MeshRenderer>();
        }
        if (meshRenderer == null && transform.parent != null)
        {
            meshRenderer = transform.parent.GetComponentInChildren<MeshRenderer>();
        }
        
        if (meshRenderer != null && _collider is BoxCollider boxCollider)
        {
            Bounds meshBounds = meshRenderer.bounds;
            
            // Convert world bounds to local space
            Vector3 localCenter = transform.InverseTransformPoint(meshBounds.center);
            Vector3 localSize = transform.InverseTransformVector(meshBounds.size);
            
            // Apply padding
            localSize += colliderPadding;
            
            boxCollider.center = localCenter;
            boxCollider.size = localSize;
            
            Debug.Log($"SpeedBoostZone: Auto-fitted collider to mesh. Size: {localSize}, Center: {localCenter}");
        }
        else
        {
            Debug.LogWarning("SpeedBoostZone: Could not find MeshRenderer for auto-fit. Using manual size instead.");
        }
    }

    private void Update()
    {
        // Manually check for player using OverlapBox since triggers don't work well with CharacterController
        PlayerMotorCC motor = FindPlayerInZone();
        if (motor != null)
        {
            motor.ApplySpeedMultiplier(boostAmount, lingerDuration);
        }
    }

    private PlayerMotorCC FindPlayerInZone()
    {
        Bounds bounds = _collider.bounds;
        Collider[] hits = Physics.OverlapBox(bounds.center, bounds.extents, transform.rotation);
        
        foreach (var hit in hits)
        {
            // Only detect objects tagged as "Player" to avoid detecting projectiles like hail
            if (!hit.CompareTag("Player")) continue;
            
            var motor = hit.GetComponent<PlayerMotorCC>();
            if (motor != null)
            {
                return motor;
            }
        }
        return null;
    }

    private void OnDrawGizmos()
    {
        DrawZoneGizmo(false);
    }
    
    private void OnDrawGizmosSelected()
    {
        DrawZoneGizmo(true);
    }
    
    private void DrawZoneGizmo(bool selected)
    {
        // Get the collider or use manual settings for preview
        BoxCollider col = GetComponent<BoxCollider>();
        
        Vector3 center;
        Vector3 size;
        
        if (col != null)
        {
            center = transform.TransformPoint(col.center);
            size = Vector3.Scale(col.size, transform.lossyScale);
        }
        else if (useManualSize)
        {
            // Preview manual size even without collider
            center = transform.TransformPoint(zoneCenter);
            size = Vector3.Scale(zoneSize, transform.lossyScale);
        }
        else
        {
            return;
        }
        
        // Color intensity based on boost amount (brighter = more boost)
        float intensity = Mathf.Clamp01(boostAmount / 5f);
        
        if (selected)
        {
            // Brighter when selected
            Gizmos.color = new Color(0f, intensity * 1.5f, 0.8f, 0.5f);
            Gizmos.DrawCube(center, size);
            
            // Draw bright wire frame
            Gizmos.color = new Color(0f, 1f, 0.5f, 1f);
            Gizmos.DrawWireCube(center, size);
            
            // Draw axis lines to show orientation
            Gizmos.color = Color.red;
            Gizmos.DrawLine(center, center + transform.right * size.x * 0.6f);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(center, center + transform.up * size.y * 0.6f);
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(center, center + transform.forward * size.z * 0.6f);
        }
        else
        {
            // Dimmer when not selected
            Gizmos.color = new Color(0f, intensity, 0.5f, 0.2f);
            Gizmos.DrawCube(center, size);
            
            // Draw wire frame for clarity
            Gizmos.color = new Color(0f, 1f, 0.5f, 0.5f);
            Gizmos.DrawWireCube(center, size);
        }
    }
}
