using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class Collectible : MonoBehaviour
{
    [Header("Collectible")]
    [SerializeField] private CollectibleType type = CollectibleType.Star;
    [SerializeField] private int amount = 1;

    [Header("Detection")]
    [Tooltip("Only objects with this tag can collect it.")]
    [SerializeField] private string playerTag = "Player";

    [Header("After Collection")]
    [Tooltip("If true, destroys the collectible GameObject. If false, disables it.")]
    [SerializeField] private bool destroyOnCollect = true;

    [Tooltip("Optional: disable these visuals on collect (so you can play a particle/audio).")]
    [SerializeField] private GameObject visualsRoot;

    [Tooltip("Optional: particle system to play on collect.")]
    [SerializeField] private ParticleSystem collectVfx;

    [Tooltip("Optional: rotate/bob script can be disabled on collect.")]
    [SerializeField] private MonoBehaviour motionScriptToDisable;

    [Tooltip("Delay before destroying (lets VFX play). 0 = destroy immediately.")]
    [SerializeField] private float destroyDelay = 0f;

    [Header("Persistence")]
    [SerializeField] private string collectibleId;


    private bool _collected;

    private void Awake()
    {
        // If already collected, disable immediately when the scene loads.
        var reg = GetRegistry();
        if (reg != null && !string.IsNullOrWhiteSpace(collectibleId) && reg.IsCollected(collectibleId))
        {
            DisableAsAlreadyCollected();
        }
    }

    private void DisableAsAlreadyCollected()
    {
        _collected = true;

        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        if (motionScriptToDisable != null)
            motionScriptToDisable.enabled = false;

        if (visualsRoot != null)
            visualsRoot.SetActive(false);

        // No VFX on load. Just remove it.
        if (destroyOnCollect)
            Destroy(gameObject);
        else
            gameObject.SetActive(false);
    }


    private void Reset()
    {
        // Trigger collider required
        var col = GetComponent<Collider>();
        col.isTrigger = true;

        // Rigidbody required for reliable trigger events (CharacterController player)
        var rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // Best guess for visuals root
        visualsRoot = transform.childCount > 0 ? transform.GetChild(0).gameObject : null;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_collected) return;
        if (!other.CompareTag(playerTag)) return;

        var stats = PlayerStats.Instance;
        if (stats == null)
        {
            Debug.LogError($"[{nameof(Collectible)}] No PlayerStats.Instance found. Make sure GameState exists.");
            return;
        }

        int safeAmount = Mathf.Max(1, amount);

        var reg = GetRegistry();
        if (reg == null)
        {
            Debug.LogError($"[{nameof(Collectible)}] No CollectedItemsRegistry found/created.");
            return;
        }

        // If somehow this collectible is already recorded, disable and do nothing.
        if (!string.IsNullOrWhiteSpace(collectibleId) && reg.IsCollected(collectibleId))
        {
            DisableAsAlreadyCollected();
            return;
        }


        switch (type)
        {
            case CollectibleType.Star:
                stats.AddStar(safeAmount);
                break;

            case CollectibleType.HealthShard:
                stats.AddHealthShard(safeAmount);
                break;

            case CollectibleType.CoinHub:
                stats.AddCoin(CoinType.Hub, safeAmount);
                break;

            case CollectibleType.CoinBiome1:
                stats.AddCoin(CoinType.Biome1, safeAmount);
                break;

            case CollectibleType.CoinBiome2:
                stats.AddCoin(CoinType.Biome2, safeAmount);
                break;

            case CollectibleType.CoinBiome3:
                stats.AddCoin(CoinType.Biome3, safeAmount);
                break;
        }

        // Record collected state for persistence across scene reloads
        if (!string.IsNullOrWhiteSpace(collectibleId))
            reg.MarkCollected(collectibleId);
        else
            Debug.LogWarning($"[{nameof(Collectible)}] collectibleId is empty on {name}. This pickup will respawn on revisit.");


        Collect();
    }

    private void Collect()
    {
        _collected = true;

        // Stop motion/interaction immediately (prevents double-collect)
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        if (motionScriptToDisable != null)
            motionScriptToDisable.enabled = false;

        if (visualsRoot != null)
            visualsRoot.SetActive(false);

        if (collectVfx != null)
        {
            collectVfx.transform.SetParent(null, true);
            collectVfx.Play();
            Destroy(collectVfx.gameObject, collectVfx.main.duration + collectVfx.main.startLifetime.constantMax + 0.2f);
        }

        if (destroyOnCollect)
            Destroy(gameObject, Mathf.Max(0f, destroyDelay));
        else
            gameObject.SetActive(false);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Auto-assign an ID if missing (editor only)
        if (string.IsNullOrWhiteSpace(collectibleId))
            collectibleId = System.Guid.NewGuid().ToString("N");
    }
#endif

    [ContextMenu("Generate New Collectible ID")]
    private void GenerateNewId()
    {
        collectibleId = System.Guid.NewGuid().ToString("N");
    }

    private CollectedItemsRegistry GetRegistry()
    {
        var reg = CollectedItemsRegistry.Instance;
        if (reg != null) return reg;

        // Create it automatically at runtime if missing
        if (Application.isPlaying)
        {
            var go = new GameObject("CollectedItemsRegistry");
            reg = go.AddComponent<CollectedItemsRegistry>();
        }
        return reg;
    }


}
