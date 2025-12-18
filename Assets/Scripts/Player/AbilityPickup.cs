using UnityEngine;

public enum AbilityType
{
    DoubleJump,
    Dash,
    WallJump
}

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class AbilityPickup : MonoBehaviour
{
    [Header("Pickup")]
    [SerializeField] private AbilityType ability = AbilityType.DoubleJump;
    [SerializeField] private string promptText = "Press E / East to collect";
    [SerializeField] private bool destroyOnCollect = true;

    [Header("Audio")]
    [Tooltip("Sound played when ability is collected.")]
    [SerializeField] private AudioClip collectSound;
    [Tooltip("Volume for collect sound (0-1).")]
    [SerializeField, Range(0f, 1f)] private float collectVolume = 1f;
    [Tooltip("Spatial blend. 0 = 2D, 1 = 3D.")]
    [SerializeField, Range(0f, 1f)] private float spatialBlend = 0.5f;

    private AbilityProgress _playerProgressInRange;
    private PlayerInputReader _playerInputInRange;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;

        var rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        var progress = other.GetComponentInParent<AbilityProgress>();
        var input = other.GetComponentInParent<PlayerInputReader>();

        if (progress != null && input != null)
        {
            _playerProgressInRange = progress;
            _playerInputInRange = input;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var progress = other.GetComponentInParent<AbilityProgress>();
        if (progress != null && progress == _playerProgressInRange)
        {
            _playerProgressInRange = null;
            _playerInputInRange = null;
        }
    }

    private void Update()
    {
        if (_playerProgressInRange == null || _playerInputInRange == null)
            return;

        if (_playerInputInRange.InteractPressedThisFrame)
        {
            Collect(_playerProgressInRange);
        }
    }

    private void Collect(AbilityProgress progress)
    {
        // Play collect sound
        if (collectSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayAtPosition(
                collectSound,
                transform.position,
                collectVolume,
                spatialBlend
            );
        }

        switch (ability)
        {
            case AbilityType.DoubleJump:
                progress.UnlockDoubleJump();
                break;
            case AbilityType.Dash:
                progress.UnlockDash();
                break;
            case AbilityType.WallJump:
                progress.UnlockWallJump();
                break;
        }

        if (destroyOnCollect)
            Destroy(gameObject);
        else
            gameObject.SetActive(false);
    }

    // Simple editor-only hint (no UI system yet)
    private void OnGUI()
    {
        if (_playerProgressInRange == null) return;

        var cam = Camera.main;
        if (cam == null) return;

        Vector3 screen = cam.WorldToScreenPoint(transform.position + Vector3.up * 1.2f);
        if (screen.z <= 0f) return;

        var rect = new Rect(screen.x - 120f, Screen.height - screen.y - 20f, 240f, 24f);
        GUI.Label(rect, promptText);
    }
}
