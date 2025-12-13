using UnityEngine;

public class DamageSource : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private int damageAmount = 1;

    public int DamageAmount => Mathf.Max(1, damageAmount);
}
