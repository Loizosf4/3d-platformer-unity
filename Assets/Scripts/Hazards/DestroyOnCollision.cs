using UnityEngine;

/// <summary>
/// Destroys the GameObject when it collides with something (like ground).
/// Used for projectiles that should break on impact.
/// </summary>
public class DestroyOnCollision : MonoBehaviour
{
    [Tooltip("Delay before destroying (seconds)")]
    [SerializeField] private float destroyDelay = 0.1f;
    
    [Tooltip("Only destroy when hitting these layers (leave empty for any collision)")]
    [SerializeField] private LayerMask collisionLayers = -1;
    
    private bool _hasCollided = false;
    
    private void OnCollisionEnter(Collision collision)
    {
        if (_hasCollided) return;
        
        // Check if we should destroy on this collision
        int objectLayer = 1 << collision.gameObject.layer;
        
        if (collisionLayers.value == -1 || (collisionLayers.value & objectLayer) != 0)
        {
            _hasCollided = true;
            Destroy(gameObject, destroyDelay);
        }
    }
}
