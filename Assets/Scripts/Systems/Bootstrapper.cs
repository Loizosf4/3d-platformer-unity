using UnityEngine;

public class Bootstrapper : MonoBehaviour
{
    [SerializeField] private GameObject gameStatePrefab;
    [SerializeField] private GameObject hudPrefab;

    private void Awake()
    {
        if (PlayerStats.Instance == null && gameStatePrefab != null)
            Instantiate(gameStatePrefab);

        // Find existing HUD (persisted) or create it
        if (FindObjectOfType<HUDController>(true) == null && hudPrefab != null)
            Instantiate(hudPrefab);
    }
}
