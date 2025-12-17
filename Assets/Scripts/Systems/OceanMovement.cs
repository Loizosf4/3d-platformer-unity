using UnityEngine;

public class WaterScroll : MonoBehaviour
{
    public float speedX = 0.005f;
    public float speedY = 0.002f;
    Renderer r;

    void Start()
    {
        r = GetComponent<Renderer>();
    }

    void Update()
    {
        r.material.mainTextureOffset +=
            new Vector2(speedX, speedY) * Time.deltaTime;
    }
}
