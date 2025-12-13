using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DamageOnTouch : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private string playerTag = "Player";

    [Header("Source")]
    [SerializeField] private DamageSource source;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;

        source = GetComponent<DamageSource>();
    }

    private void Awake()
    {
        if (source == null)
            source = GetComponent<DamageSource>();
    }

    private void OnTriggerEnter(Collider other) => TryDamage(other);
    private void OnTriggerStay(Collider other) => TryDamage(other);

    private void TryDamage(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        var health = other.GetComponentInParent<PlayerHealthController>();
        if (health == null) return;

        int amount = source != null ? source.DamageAmount : 1;
        health.TryTakeDamage(transform.position, amount);
    }
}
