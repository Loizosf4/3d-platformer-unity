using UnityEngine;

public class PersistentCameraRig : MonoBehaviour
{
    private static PersistentCameraRig _instance;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
