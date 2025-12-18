using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DamageOnTouch : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private string playerTag = "Player";

    [Header("Source")]
    [SerializeField] private DamageSource source;

    [Header("Audio")]
    [Tooltip("Sound played when hitting the player.")]
    [SerializeField] private AudioClip hitSound;
    [Tooltip("Volume for hit sound (0-1).")]
    [SerializeField, Range(0f, 1f)] private float hitVolume = 0.7f;
    [Tooltip("Spatial blend. 0 = 2D, 1 = 3D.")]
    [SerializeField, Range(0f, 1f)] private float spatialBlend = 1f;

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
        
        // Play hit sound when hitting player
        if (hitSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayAtPosition(
                hitSound,
                transform.position,
                hitVolume,
                spatialBlend
            );
        }
    }

    /// <summary>
    /// Set hit sound parameters at runtime (used for dynamically spawned projectiles).
    /// </summary>
    public void SetHitSound(AudioClip clip, float volume, float spatial)
    {
        hitSound = clip;
        hitVolume = Mathf.Clamp01(volume);
        spatialBlend = Mathf.Clamp01(spatial);
    }
}
