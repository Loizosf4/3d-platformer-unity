using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HeartsUI : MonoBehaviour
{
    [Header("Slot Prefab")]
    [SerializeField] private Image heartSlotPrefab;

    [Header("Sprites")]
    [Tooltip("Sprite used when the heart slot is filled.")]
    [SerializeField] private Sprite fullSprite;

    [Tooltip("Sprite used when the heart slot is empty.")]
    [SerializeField] private Sprite emptySprite;

    [Header("Config")]
    [SerializeField] private int maxSupportedHearts = 5;

    private readonly List<Image> _slots = new List<Image>();

    private void Awake()
    {
        if (heartSlotPrefab == null)
        {
            Debug.LogError($"{nameof(HeartsUI)} on {name}: Heart slot prefab is not assigned.");
        }
    }

    public void SetHearts(int current, int max)
    {
        max = Mathf.Clamp(max, 0, maxSupportedHearts);
        current = Mathf.Clamp(current, 0, max);

        EnsureSlotCount(max);

        // Enable only up to max, disable the rest
        for (int i = 0; i < _slots.Count; i++)
        {
            _slots[i].gameObject.SetActive(i < max);
        }

        // Set sprite state
        for (int i = 0; i < max; i++)
        {
            bool isFull = i < current;
            _slots[i].sprite = isFull ? fullSprite : emptySprite;
        }
    }

    private void EnsureSlotCount(int needed)
    {
        while (_slots.Count < needed)
        {
            Image slot = Instantiate(heartSlotPrefab, transform);
            slot.name = $"Heart_{_slots.Count + 1}";
            _slots.Add(slot);
        }
    }
}
