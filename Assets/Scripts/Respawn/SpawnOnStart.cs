using UnityEngine;

public class SpawnOnStart : MonoBehaviour
{
    private void Start()
    {
        if (RespawnManager.Instance != null)
            RespawnManager.Instance.SpawnPlayerAtDefaultIfNoCheckpoint(gameObject);
    }
}

