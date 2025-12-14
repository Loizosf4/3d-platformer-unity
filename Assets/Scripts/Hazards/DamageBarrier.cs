using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DamageBarrier : MonoBehaviour
{
    [Header("Barrier Damage")]
    [SerializeField] private int damageAmount = 1;

    [Tooltip("If true, barrier forces respawn even if the player did not die.")]
    [SerializeField] private bool forceRespawn = true;

    [Tooltip("If true, only deals damage when player is NOT invincible. Respawn can still happen.")]
    [SerializeField] private bool useInvincibilityCheck = true;

    [Tooltip("Optional tiny delay before respawn (0 to 0.2).")]
    [Range(0f, 0.2f)]
    [SerializeField] private float respawnDelay = 0f;

    [Header("Hit Source / Knockback Direction")]
    [Tooltip("If true, uses this barrier's position as sourcePosition for knockback direction.")]
    [SerializeField] private bool useBarrierPositionAsHitSource = true;

    [Header("Detection")]
    [SerializeField] private string playerTag = "Player";

    // Safety: prevent repeated triggers in same frame / rapid re-entry
    private bool _busy;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        TryBarrier(other);
    }

    private void OnTriggerStay(Collider other)
    {
        // Safe even on stay; we gate it with _busy and respawn happens right away.
        TryBarrier(other);
    }

    private void TryBarrier(Collider other)
    {
        if (_busy) return;
        if (!other.CompareTag(playerTag)) return;

        var player = other.GetComponentInParent<PlayerHealthController>();
        if (player == null) return;

        StartCoroutine(BarrierRoutine(player));
    }

    private IEnumerator BarrierRoutine(PlayerHealthController player)
    {
        _busy = true;

        // Optional damage (only if damageAmount > 0)
        if (damageAmount > 0)
        {
            bool canDamage = true;

            if (useInvincibilityCheck && player.IsInvincible)
                canDamage = false;

            if (canDamage)
            {
                Vector3 src = useBarrierPositionAsHitSource ? transform.position : player.transform.position;
                player.TryTakeDamage(src, damageAmount);
            }
        }

        // Respawn (even if invincible, if forceRespawn = true)
        if (forceRespawn)
        {
            if (respawnDelay > 0f)
                yield return new WaitForSeconds(respawnDelay);

            if (RespawnManager.Instance != null)
                RespawnManager.Instance.RespawnPlayer(player.gameObject);
            else
                Debug.LogError("[DamageBarrier] RespawnManager.Instance not found.");
        }

        // Small cooldown so it doesn't double-trigger on weird overlaps
        yield return null;
        _busy = false;
    }
}

