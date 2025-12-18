using UnityEngine;

/// <summary>
/// Simple component that forwards trigger events to the parent HailShooterEnemy.
/// Used for stomp detection on top of the enemy.
/// </summary>
public class StompDetector : MonoBehaviour
{
    [HideInInspector]
    public HailShooterEnemy enemy;
    
    private void OnTriggerEnter(Collider other)
    {
        if (enemy != null && other.CompareTag("Player"))
        {
            Debug.Log($"StompDetector: Player trigger detected! Forwarding to enemy.");
            enemy.OnPlayerStomp(other);
        }
    }
}
