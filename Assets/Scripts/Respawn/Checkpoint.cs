using Unity.VisualScripting.Antlr3.Runtime.Misc;
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

    [Header("Audio")]
    [Tooltip("Sound played when checkpoint is activated.")]
    [SerializeField] private AudioClip activationSound;
    [Tooltip("Volume for activation sound (0-1).")]
    [SerializeField, Range(0f, 1f)] private float activationVolume = 0.8f;
    [Tooltip("Spatial blend. 0 = 2D, 1 = 3D.")]
    [SerializeField, Range(0f, 1f)] private float spatialBlend = 0.7f;

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

        var stats = PlayerStats.Instance;
        if (stats == null)
        {
            Debug.LogError("[Checkpoint] PlayerStats.Instance is null (GameState missing).");
            return;
        }

        stats.SetCheckpoint(spawnPoint);
        stats.FullHeal(); // heal on touch
        ActivateFeedback();
    }


    private void ActivateFeedback()
    {
        if (!string.IsNullOrWhiteSpace(checkpointId))
            Debug.Log($"[Checkpoint] Activated: {checkpointId}");

        if (feedbackRenderer != null)
            feedbackRenderer.material.color = activeColor;

        // Play checkpoint activation sound
        if (activationSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayAtPosition(
                activationSound,
                transform.position,
                activationVolume,
                spatialBlend
            );
        }

        _activated = true; // optional: keep as "ever activated" if you want
    }

}
