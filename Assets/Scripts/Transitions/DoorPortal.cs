using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DoorPortal : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private string targetSceneName;
    [SerializeField] private string targetSpawnId;

    [Header("Detection")]
    [SerializeField] private string playerTag = "Player";

    private bool _used;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_used) return;
        if (!other.CompareTag(playerTag)) return;

        if (SceneTransitionManager.Instance == null)
        {
            Debug.LogError("[DoorPortal] SceneTransitionManager.Instance not found.");
            return;
        }

        Debug.Log($"[DoorPortal] Triggered. Loading '{targetSceneName}' -> Spawn '{targetSpawnId}'");

        _used = true;
        SceneTransitionManager.Instance.RequestTransition(targetSceneName, targetSpawnId);
    }
}
