using UnityEngine;

public class Bootstrapper : MonoBehaviour
{
    [Header("Core Prefabs")]
    [SerializeField] private GameObject gameStatePrefab;
    [SerializeField] private GameObject hudPrefab;

    [Header("Player + Camera")]
    [SerializeField] private GameObject playerPrefab;     // PF_player
    [SerializeField] private GameObject cameraRigPrefab;  // PF_CameraRig

    [Header("Transitions")]
    [SerializeField] private GameObject sceneTransitionManagerPrefab; // PF_SceneTransitionManager
    [SerializeField] private GameObject respawnManagerPrefab; // PF_RespawnManager

    private void Awake()
    {
        if (PlayerStats.Instance == null && gameStatePrefab != null)
            Instantiate(gameStatePrefab);

        if (RespawnManager.Instance == null && respawnManagerPrefab != null)
            Instantiate(respawnManagerPrefab);

        if (FindObjectOfType<HUDController>(true) == null && hudPrefab != null)
            Instantiate(hudPrefab);

        if (FindObjectOfType<CameraRigBinder>(true) == null && cameraRigPrefab != null)
            Instantiate(cameraRigPrefab);

        if (GameObject.FindGameObjectWithTag("Player") == null && playerPrefab != null)
            Instantiate(playerPrefab);

        if (SceneTransitionManager.Instance == null && sceneTransitionManagerPrefab != null)
            Instantiate(sceneTransitionManagerPrefab);
    }


}
