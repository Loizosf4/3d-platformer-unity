using System.Collections.Generic;
using UnityEngine;

public class CollectedItemsRegistry : MonoBehaviour
{
    public static CollectedItemsRegistry Instance { get; private set; }

    // Runtime-only persistence (for now). Later you can serialize this for save/load.
    private readonly HashSet<string> _collected = new HashSet<string>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>Returns true if the collectible id has already been collected this play session.</summary>
    public bool IsCollected(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return false;
        return _collected.Contains(id);
    }

    /// <summary>Marks the collectible id as collected. Returns true if it was newly added.</summary>
    public bool MarkCollected(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return false;
        return _collected.Add(id);
    }

    // Optional helpers (handy later)
    public void ClearAll() => _collected.Clear();
}
