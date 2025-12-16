using UnityEngine;
using UnityEngine.SceneManagement;

public class CameraRigBinder : MonoBehaviour
{
    [Header("Assign these if you use Cinemachine (optional)")]
    [SerializeField] private MonoBehaviour virtualCameraComponent; // drag your CinemachineVirtualCamera here if you have it
    [SerializeField] private Transform followTarget;              // usually the player or a camera pivot on player
    [SerializeField] private bool autoFindPlayerByTag = true;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        Rebind();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Rebind();
    }

    private void Rebind()
    {
        if (!autoFindPlayerByTag) return;

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        // If you have a specific camera pivot on the player, assign it manually later.
        followTarget = player.transform;

        // If you are using Cinemachine, you will manually drag the CinemachineVirtualCamera component
        // into virtualCameraComponent, then we set Follow/LookAt via reflection-free safe approach:
        // We can't reference Cinemachine types here without requiring the package in every assembly,
        // so we provide manual assignment in Inspector if needed.
    }
}
