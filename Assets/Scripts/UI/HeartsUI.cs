using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HeartsUI : MonoBehaviour
{
    [Header("Slot Prefab")]
    [SerializeField] private Image heartSlotPrefab;

    [Header("Sprites (optional for now)")]
    [SerializeField] private Sprite fullSprite;
    [SerializeField] private Sprite emptySprite;

    [Header("Visuals")]
    [SerializeField] private Color fullColor = Color.white;
    [SerializeField] private Color emptyColor = new Color(1f, 1f, 1f, 0.25f);

    [Header("Config")]
    [SerializeField] private int maxSupportedHearts = 5;

    private readonly List<Image> _slots = new List<Image>();

    public void SetHearts(int current, int max)
    {
        max = Mathf.Clamp(max, 0, maxSupportedHearts);
        current = Mathf.Clamp(current, 0, max);

        EnsureSlotCount(max);

        for (int i = 0; i < _slots.Count; i++)
            _slots[i].gameObject.SetActive(i < max);

        for (int i = 0; i < max; i++)
        {
            bool isFull = i < current;
            Image img = _slots[i];

            // Sprite: use full/empty if provided, otherwise keep whatever prefab uses
            if (fullSprite != null && emptySprite != null)
                img.sprite = isFull ? fullSprite : emptySprite;
            else if (fullSprite != null)
                img.sprite = fullSprite;

            // Color always makes the state obvious
            img.color = isFull ? fullColor : emptyColor;
        }
    }

    private void EnsureSlotCount(int needed)
    {
        if (heartSlotPrefab == null)
        {
            Debug.LogError($"{nameof(HeartsUI)} on {name}: Heart slot prefab is not assigned.");
            return;
        }

        while (_slots.Count < needed)
        {
            Image slot = Instantiate(heartSlotPrefab, transform);
            slot.name = $"Heart_{_slots.Count + 1}";
            _slots.Add(slot);
        }
    }
}
