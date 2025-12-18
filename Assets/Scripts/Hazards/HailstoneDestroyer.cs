using UnityEngine;

/// <summary>
/// Destroys the hailstone when it hits the ground or walls, but NOT when it hits the player or other hailstones.
/// This allows the hailstone to damage the player and keep moving.
/// </summary>
public class HailstoneDestroyer : MonoBehaviour
{
    [Tooltip("Player tag to ignore")]
    public string playerTag = "Player";
    
    [Tooltip("Delay before destroying (seconds)")]
    [SerializeField] private float destroyDelay = 0.5f;
    
    private bool _hasCollided = false;
    
    private void Start()
    {
        // Verify we have a non-trigger collider for collisions
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError("HailstoneDestroyer: No collider found!");
        }
        else if (col.isTrigger)
        {
            Debug.LogError("HailstoneDestroyer: Collider is a trigger! OnCollisionEnter won't fire!");
        }
        else
        {
            Debug.Log($"HailstoneDestroyer: Setup complete with solid collider");
        }
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"HailstoneDestroyer.OnCollisionEnter: Hit {collision.gameObject.name}, hasCollided={_hasCollided}");
        
        if (_hasCollided) return;
        
        // Don't destroy when hitting the player - let it bounce off and keep going
        if (collision.gameObject.CompareTag(playerTag))
        {
            Debug.Log($"Hailstone hit player, continuing...");
            return;
        }
        
        // Don't destroy when hitting other hailstones - let them pass through each other
        if (collision.gameObject.GetComponent<HailstoneDestroyer>() != null)
        {
            Debug.Log($"Hailstone hit another hailstone, ignoring...");
            Physics.IgnoreCollision(collision.collider, GetComponent<Collider>());
            return;
        }
        
        // Destroy when hitting anything else (ground, walls, etc.)
        Debug.Log($"Hailstone hit {collision.gameObject.name} (layer: {collision.gameObject.layer}), destroying in {destroyDelay}s");
        _hasCollided = true;
        Destroy(gameObject, destroyDelay);
    }
}
