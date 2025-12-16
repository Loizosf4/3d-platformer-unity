using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    [SerializeField] private string spawnId = "Default";
    [SerializeField] private bool isDefaultSpawn = false;

    public string SpawnId => spawnId;
    public bool IsDefaultSpawn => isDefaultSpawn;

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = isDefaultSpawn ? Color.green : Color.cyan;
        Gizmos.DrawSphere(transform.position, 0.2f);
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 0.8f);
    }
#endif
}
