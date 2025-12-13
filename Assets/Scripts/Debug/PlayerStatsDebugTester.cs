using UnityEngine;

public class PlayerStatsDebugTester : MonoBehaviour
{
    [SerializeField] private PlayerStats stats;

    [Header("Test Amounts")]
    [SerializeField] private int addStarsAmount = 1;
    [SerializeField] private int addCoinsAmount = 1;
    [SerializeField] private int addShardsAmount = 1;
    [SerializeField] private int damageAmount = 1;
    [SerializeField] private int healAmount = 1;
    [SerializeField] private CoinType coinType = CoinType.Hub;

    [Header("Checkpoint Test")]
    [Tooltip("Optional: assign any Transform in the scene to store as checkpoint.")]
    [SerializeField] private Transform checkpointTransform;

    private void Awake()
    {
        if (stats == null)
            stats = GetComponent<PlayerStats>();

        if (stats == null)
        {
            Debug.LogError($"{nameof(PlayerStatsDebugTester)}: No PlayerStats found.");
            enabled = false;
            return;
        }

        // Subscribe to events so you see changes instantly in Console
        stats.OnHeartsChanged += (cur, max) => Debug.Log($"[PlayerStats] Hearts: {cur}/{max}");
        stats.OnHealthShardsChanged += (shards, req) =>
        {
            string maxTag = stats.IsMaxHealthUpgraded ? " (MAX)" : "";
            Debug.Log($"[PlayerStats] Health Shards: {shards}/{req}{maxTag}");
        };
        stats.OnStarsChanged += total => Debug.Log($"[PlayerStats] Stars: {total}");
        stats.OnCoinChanged += (type, total) => Debug.Log($"[PlayerStats] Coin {type}: {total}");
        stats.OnDeathsChanged += total => Debug.Log($"[PlayerStats] Deaths: {total}");
        stats.OnCheckpointChanged += cp => Debug.Log($"[PlayerStats] Checkpoint: scene='{cp.sceneName}' pos={cp.position}");
    }

    // ===== Inspector Buttons (right-click component header → Run) =====
    [ContextMenu("TEST/Add Star")]
    public void TestAddStar() => stats.AddStar(addStarsAmount);

    [ContextMenu("TEST/Add Coin")]
    public void TestAddCoin() => stats.AddCoin(coinType, addCoinsAmount);

    [ContextMenu("TEST/Add Health Shard")]
    public void TestAddShard() => stats.AddHealthShard(addShardsAmount);

    [ContextMenu("TEST/Take Damage")]
    public void TestTakeDamage() => stats.TakeDamage(damageAmount);

    [ContextMenu("TEST/Heal")]
    public void TestHeal() => stats.Heal(healAmount);

    [ContextMenu("TEST/Full Heal")]
    public void TestFullHeal() => stats.FullHeal();

    [ContextMenu("TEST/Register Death")]
    public void TestRegisterDeath() => stats.RegisterDeath();

    [ContextMenu("TEST/Set Checkpoint (Transform)")]
    public void TestSetCheckpoint()
    {
        if (checkpointTransform == null)
        {
            Debug.LogWarning("[PlayerStatsDebugTester] No checkpointTransform assigned.");
            return;
        }
        stats.SetCheckpoint(checkpointTransform);
    }
}
