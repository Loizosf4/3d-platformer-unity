using UnityEngine;

public class CollectibleMotion : MonoBehaviour
{
    [Header("Rotate")]
    [SerializeField] private Vector3 rotationDegreesPerSecond = new Vector3(0f, 120f, 0f);

    [Header("Bob")]
    [SerializeField] private float bobHeight = 0.15f;
    [SerializeField] private float bobSpeed = 2.0f;

    private Vector3 _startLocalPos;

    private void Awake()
    {
        _startLocalPos = transform.localPosition;
    }

    private void Update()
    {
        transform.Rotate(rotationDegreesPerSecond * Time.deltaTime, Space.Self);

        float y = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.localPosition = _startLocalPos + new Vector3(0f, y, 0f);
    }
}
