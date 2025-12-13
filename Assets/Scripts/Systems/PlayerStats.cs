using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerStats : MonoBehaviour
{
    // ===== Singleton (persistent source of truth) =====
    public static PlayerStats Instance { get; private set; }

    // ===== Constants / Rules =====
    public const int HealthShardRequired = 4;
    public const int MaxHeartsCap = 5;

    // ===== Events (UI will subscribe later) =====
    public event Action<int, int> OnHeartsChanged;                 // current, max
    public event Action<int, int> OnHealthShardsChanged;           // shards, required(4)
    public event Action<int> OnStarsChanged;                       // total
    public event Action<CoinType, int> OnCoinChanged;              // type, total
    public event Action<int> OnDeathsChanged;                      // total
    public event Action<CheckpointData> OnCheckpointChanged;       // full checkpoint info

    // ===== Data =====
    [Header("Hearts")]
    [SerializeField] private int currentHearts = 3;
    [SerializeField] private int maxHearts = 3;

    [Header("Health Shards")]
    [SerializeField, Range(0, HealthShardRequired - 1)]
    private int healthShardCount = 0;

    [Header("Progression")]
    [SerializeField] private int starsTotal = 0;

    [Header("Coins")]
    [SerializeField] private int coinsHub = 0;
    [SerializeField] private int coinsBiome1 = 0;
    [SerializeField] private int coinsBiome2 = 0;
    [SerializeField] private int coinsBiome3 = 0;

    [Header("Deaths")]
    [SerializeField] private int deathsTotal = 0;

    [Header("Checkpoint")]
    [SerializeField] private CheckpointData lastCheckpoint;

    // ===== Public read-only accessors (useful for UI later) =====
    public int CurrentHearts => currentHearts;
    public int MaxHearts => maxHearts;
    public int HealthShardCount => healthShardCount;
    public int StarsTotal => starsTotal;
    public int DeathsTotal => deathsTotal;
    public CheckpointData LastCheckpoint => lastCheckpoint;

    public bool IsMaxHealthUpgraded => maxHearts >= MaxHeartsCap;

    // ===== Unity lifecycle =====
    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Ensure valid initial state
        ClampAll();

        // Fire initial events once so UI (later) can initialize immediately
        RaiseAllEvents();
    }

    // ===== Public API =====

    public void AddStar(int amount = 1)
    {
        if (amount <= 0) return;
        starsTotal = SafeAddNonNegative(starsTotal, amount);
        OnStarsChanged?.Invoke(starsTotal);
    }

    public void AddCoin(CoinType type, int amount = 1)
    {
        if (amount <= 0) return;

        int newTotal = SafeAddNonNegative(GetCoin(type), amount);
        SetCoin(type, newTotal);

        OnCoinChanged?.Invoke(type, newTotal);
    }

    public void AddHealthShard(int amount = 1)
    {
        if (amount <= 0) return;

        if (IsMaxHealthUpgraded)
        {
            // At max hearts: shards are irrelevant for upgrade.
            // Keep count at 0 so later UI can show MAX via IsMaxHealthUpgraded.
            healthShardCount = 0;
            OnHealthShardsChanged?.Invoke(healthShardCount, HealthShardRequired);
            return;
        }

        for (int i = 0; i < amount; i++)
        {
            healthShardCount++;

            if (healthShardCount >= HealthShardRequired)
            {
                healthShardCount = 0;
                IncreaseMaxHearts(1);
            }
        }

        OnHealthShardsChanged?.Invoke(healthShardCount, HealthShardRequired);
    }

    public void TakeDamage(int amount = 1)
    {
        if (amount <= 0) return;

        currentHearts = Mathf.Clamp(currentHearts - amount, 0, maxHearts);
        OnHeartsChanged?.Invoke(currentHearts, maxHearts);
    }

    public void Heal(int amount = 1)
    {
        if (amount <= 0) return;

        currentHearts = Mathf.Clamp(currentHearts + amount, 0, maxHearts);
        OnHeartsChanged?.Invoke(currentHearts, maxHearts);
    }

    public void FullHeal()
    {
        currentHearts = maxHearts;
        OnHeartsChanged?.Invoke(currentHearts, maxHearts);
    }

    // Recommended: store position + rotation + scene name (Transform itself won't persist across scenes reliably)
    public void SetCheckpoint(Transform checkpointTransform)
    {
        if (checkpointTransform == null) return;

        var data = new CheckpointData
        {
            sceneName = SceneManager.GetActiveScene().name,
            position = checkpointTransform.position,
            rotation = checkpointTransform.rotation
        };

        lastCheckpoint = data;
        OnCheckpointChanged?.Invoke(lastCheckpoint);
    }

    // Overload for systems that already know scene/pos/rot
    public void SetCheckpoint(string sceneName, Vector3 position, Quaternion rotation)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            sceneName = SceneManager.GetActiveScene().name;

        lastCheckpoint = new CheckpointData
        {
            sceneName = sceneName,
            position = position,
            rotation = rotation
        };

        OnCheckpointChanged?.Invoke(lastCheckpoint);
    }

    public void RegisterDeath()
    {
        deathsTotal = SafeAddNonNegative(deathsTotal, 1);
        OnDeathsChanged?.Invoke(deathsTotal);
    }

    // ===== Helpers =====

    private void IncreaseMaxHearts(int amount)
    {
        if (amount <= 0) return;

        int oldMax = maxHearts;
        maxHearts = Mathf.Clamp(maxHearts + amount, 0, MaxHeartsCap);

        // Optional rule (feel-good): when max increases, also heal up by the same amount
        // so upgrades feel rewarding immediately.
        int gained = maxHearts - oldMax;
        if (gained > 0)
            currentHearts = Mathf.Clamp(currentHearts + gained, 0, maxHearts);
        else
            currentHearts = Mathf.Clamp(currentHearts, 0, maxHearts);

        OnHeartsChanged?.Invoke(currentHearts, maxHearts);

        // If we hit max hearts, shards should effectively show MAX later
        if (IsMaxHealthUpgraded)
            healthShardCount = 0;
    }

    private int GetCoin(CoinType type)
    {
        return type switch
        {
            CoinType.Hub => coinsHub,
            CoinType.Biome1 => coinsBiome1,
            CoinType.Biome2 => coinsBiome2,
            CoinType.Biome3 => coinsBiome3,
            _ => 0
        };
    }

    private void SetCoin(CoinType type, int total)
    {
        total = Mathf.Max(0, total);

        switch (type)
        {
            case CoinType.Hub: coinsHub = total; break;
            case CoinType.Biome1: coinsBiome1 = total; break;
            case CoinType.Biome2: coinsBiome2 = total; break;
            case CoinType.Biome3: coinsBiome3 = total; break;
        }
    }

    private static int SafeAddNonNegative(int current, int add)
    {
        if (add <= 0) return current;
        long sum = (long)current + add;
        if (sum > int.MaxValue) sum = int.MaxValue;
        return (int)Mathf.Max(0, sum);
    }

    private void ClampAll()
    {
        maxHearts = Mathf.Clamp(maxHearts, 0, MaxHeartsCap);
        currentHearts = Mathf.Clamp(currentHearts, 0, maxHearts);

        if (IsMaxHealthUpgraded)
            healthShardCount = 0;
        else
            healthShardCount = Mathf.Clamp(healthShardCount, 0, HealthShardRequired - 1);

        starsTotal = Mathf.Max(0, starsTotal);

        coinsHub = Mathf.Max(0, coinsHub);
        coinsBiome1 = Mathf.Max(0, coinsBiome1);
        coinsBiome2 = Mathf.Max(0, coinsBiome2);
        coinsBiome3 = Mathf.Max(0, coinsBiome3);

        deathsTotal = Mathf.Max(0, deathsTotal);
        if (string.IsNullOrWhiteSpace(lastCheckpoint.sceneName))
        {
            lastCheckpoint.sceneName = SceneManager.GetActiveScene().name;
            lastCheckpoint.position = Vector3.zero;
            lastCheckpoint.rotation = Quaternion.identity;
        }
    }

    private void RaiseAllEvents()
    {
        OnHeartsChanged?.Invoke(currentHearts, maxHearts);
        OnHealthShardsChanged?.Invoke(healthShardCount, HealthShardRequired);
        OnStarsChanged?.Invoke(starsTotal);

        OnCoinChanged?.Invoke(CoinType.Hub, coinsHub);
        OnCoinChanged?.Invoke(CoinType.Biome1, coinsBiome1);
        OnCoinChanged?.Invoke(CoinType.Biome2, coinsBiome2);
        OnCoinChanged?.Invoke(CoinType.Biome3, coinsBiome3);

        OnDeathsChanged?.Invoke(deathsTotal);
        OnCheckpointChanged?.Invoke(lastCheckpoint);
    }
}

[Serializable]
public struct CheckpointData
{
    public string sceneName;
    public Vector3 position;
    public Quaternion rotation;
}
