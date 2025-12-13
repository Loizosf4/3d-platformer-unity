using UnityEngine;

public class RespawnManager : MonoBehaviour
{
    public static RespawnManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private PlayerStats stats;

    [Tooltip("Used if no checkpoint has ever been touched.")]
    [SerializeField] private Transform defaultSpawnPoint;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (stats == null)
            stats = PlayerStats.Instance;

        if (defaultSpawnPoint == null)
        {
            var dsp = GameObject.Find("DefaultSpawnPoint");
            if (dsp != null) defaultSpawnPoint = dsp.transform;
        }
    }

    public void SetDefaultSpawnPoint(Transform t)
    {
        defaultSpawnPoint = t;
    }

    // Call this at the start of the scene/game
    public void SpawnPlayerAtDefaultIfNoCheckpoint(GameObject player)
    {
        if (player == null) return;

        if (stats == null)
            stats = PlayerStats.Instance;

        if (stats == null)
        {
            Debug.LogError("[RespawnManager] PlayerStats not found (GameState missing).");
            return;
        }

        // Only move player to DefaultSpawnPoint if they have NOT touched a checkpoint yet
        if (!stats.HasCheckpoint && defaultSpawnPoint != null)
        {
            TeleportAndReset(player, defaultSpawnPoint.position, defaultSpawnPoint.rotation);
            // Usually you also want to start full health
            stats.FullHeal();
        }
    }

    // Call this on death
    public void RespawnPlayer(GameObject player)
    {
        if (player == null) return;

        if (stats == null)
            stats = PlayerStats.Instance;

        if (stats == null)
        {
            Debug.LogError("[RespawnManager] PlayerStats not found (GameState missing).");
            return;
        }

        if (stats.HasCheckpoint)
        {
            var cp = stats.LastCheckpoint;
            TeleportAndReset(player, cp.position, cp.rotation);
        }
        else if (defaultSpawnPoint != null)
        {
            TeleportAndReset(player, defaultSpawnPoint.position, defaultSpawnPoint.rotation);
        }
        else
        {
            TeleportAndReset(player, Vector3.zero, Quaternion.identity);
        }

        stats.FullHeal();
    }

    private void TeleportAndReset(GameObject player, Vector3 pos, Quaternion rot)
    {
        // CharacterController-safe teleport
        var cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        // Clear motor velocity/state
        var motor = player.GetComponent<PlayerMotorCC>();
        if (motor != null)
            motor.ResetMovementState();

        player.transform.SetPositionAndRotation(pos, rot);

        if (cc != null) cc.enabled = true;
    }
}
