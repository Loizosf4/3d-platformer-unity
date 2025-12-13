using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class Checkpoint : MonoBehaviour
{
    [Header("Checkpoint")]
    [Tooltip("Optional: unique ID for save system later.")]
    [SerializeField] private string checkpointId = "CP_01";

    [Tooltip("Child transform named 'SpawnPoint' defines respawn position + rotation.")]
    [SerializeField] private Transform spawnPoint;

    [Header("Feedback")]
    [SerializeField] private Renderer feedbackRenderer;
    [SerializeField] private Color inactiveColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color activeColor = new Color(0.3f, 1f, 0.3f, 1f);

    private bool _activated;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;

        var rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // Auto-find SpawnPoint child
        var sp = transform.Find("SpawnPoint");
        if (sp != null) spawnPoint = sp;

        feedbackRenderer = GetComponentInChildren<Renderer>();
    }

    private void Awake()
    {
        if (spawnPoint == null)
        {
            var sp = transform.Find("SpawnPoint");
            if (sp != null) spawnPoint = sp;
        }

        if (feedbackRenderer != null)
            feedbackRenderer.material.color = inactiveColor;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (spawnPoint == null)
        {
            Debug.LogWarning($"[Checkpoint] {name} has no SpawnPoint assigned.");
            return;
        }

        var stats = PlayerStats.Instance;
        if (stats == null)
        {
            Debug.LogError("[Checkpoint] PlayerStats.Instance not found. Make sure GameState exists.");
            return;
        }

        stats.SetCheckpoint(spawnPoint);

        ActivateFeedback();
    }

    private void ActivateFeedback()
    {
        if (_activated) return;
        _activated = true;

        if (feedbackRenderer != null)
            feedbackRenderer.material.color = activeColor;
    }
}
