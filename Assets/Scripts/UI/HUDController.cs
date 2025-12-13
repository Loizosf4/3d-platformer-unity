using UnityEngine;
using TMPro;

public class HUDController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerStats stats;

    [Header("Top Left")]
    [SerializeField] private HeartsUI heartsUI;
    [SerializeField] private TMP_Text shardsText;
    [SerializeField] private TMP_Text deathsText;

    [Header("Top Right")]
    [SerializeField] private TMP_Text starsText;
    [SerializeField] private TMP_Text coinHubText;
    [SerializeField] private TMP_Text coinBiome1Text;
    [SerializeField] private TMP_Text coinBiome2Text;
    [SerializeField] private TMP_Text coinBiome3Text;

    private void Awake()
    {
        // Auto-find PlayerStats if not assigned
        if (stats == null)
            stats = PlayerStats.Instance;

        if (stats == null)
            Debug.LogError($"{nameof(HUDController)}: PlayerStats.Instance not found. Make sure your GameState object exists in the scene.");
    }

    private void OnEnable()
    {
        if (stats == null) return;

        // Subscribe
        stats.OnHeartsChanged += HandleHeartsChanged;
        stats.OnHealthShardsChanged += HandleShardsChanged;
        stats.OnStarsChanged += HandleStarsChanged;
        stats.OnCoinChanged += HandleCoinChanged;
        stats.OnDeathsChanged += HandleDeathsChanged;

        // Initialize immediately using current values
        HandleHeartsChanged(stats.CurrentHearts, stats.MaxHearts);
        HandleShardsChanged(stats.HealthShardCount, PlayerStats.HealthShardRequired);
        HandleStarsChanged(stats.StarsTotal);
        HandleDeathsChanged(stats.DeathsTotal);

        // Coins: trigger text initialization for all types
        HandleCoinChanged(CoinType.Hub, GetCoinSafe(CoinType.Hub));
        HandleCoinChanged(CoinType.Biome1, GetCoinSafe(CoinType.Biome1));
        HandleCoinChanged(CoinType.Biome2, GetCoinSafe(CoinType.Biome2));
        HandleCoinChanged(CoinType.Biome3, GetCoinSafe(CoinType.Biome3));
    }

    private void OnDisable()
    {
        if (stats == null) return;

        // Unsubscribe
        stats.OnHeartsChanged -= HandleHeartsChanged;
        stats.OnHealthShardsChanged -= HandleShardsChanged;
        stats.OnStarsChanged -= HandleStarsChanged;
        stats.OnCoinChanged -= HandleCoinChanged;
        stats.OnDeathsChanged -= HandleDeathsChanged;
    }

    private void HandleHeartsChanged(int current, int max)
    {
        if (heartsUI != null)
            heartsUI.SetHearts(current, max);
    }

    private void HandleShardsChanged(int shards, int required)
    {
        if (shardsText == null) return;

        if (stats != null && stats.IsMaxHealthUpgraded)
            shardsText.text = "Shards: MAX";
        else
            shardsText.text = $"Shards: {shards}/{required}";
    }

    private void HandleStarsChanged(int total)
    {
        if (starsText != null)
            starsText.text = $"Stars: {total}";
    }

    private void HandleDeathsChanged(int total)
    {
        if (deathsText != null)
            deathsText.text = $"Deaths: {total}x";
    }

    private void HandleCoinChanged(CoinType type, int total)
    {
        switch (type)
        {
            case CoinType.Hub:
                if (coinHubText != null) coinHubText.text = $"Hub: {total}";
                break;
            case CoinType.Biome1:
                if (coinBiome1Text != null) coinBiome1Text.text = $"Biome1: {total}";
                break;
            case CoinType.Biome2:
                if (coinBiome2Text != null) coinBiome2Text.text = $"Biome2: {total}";
                break;
            case CoinType.Biome3:
                if (coinBiome3Text != null) coinBiome3Text.text = $"Biome3: {total}";
                break;
        }
    }

    // PlayerStats doesn't expose per-coin getters in the earlier version.
    // If you later add getters, update this to use them.
    private int GetCoinSafe(CoinType type)
    {
        // We rely on events for real updates, this is just initial display.
        // If you want perfect initialization, add public getters in PlayerStats.
        return 0;
    }
}
