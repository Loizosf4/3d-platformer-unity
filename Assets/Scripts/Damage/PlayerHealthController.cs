using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PlayerMotorCC))]
public class PlayerHealthController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerStats stats;
    [SerializeField] private PlayerMotorCC motor;

    [Header("Invincibility Frames")]
    [SerializeField] private float invincibilityDuration = 1.0f;

    [Tooltip("Optional simple blink by toggling renderers.")]
    [SerializeField] private bool blinkDuringInvincibility = true;

    [SerializeField] private float blinkRate = 0.12f;

    [Header("Knockback (tune in Inspector)")]
    [SerializeField] private float knockbackHorizontalStrength = 8f;
    [SerializeField] private float knockbackVerticalStrength = 6f;

    [Tooltip("Lock movement input briefly so knockback isn't overwritten.")]
    [SerializeField] private float knockbackControlLockTime = 0.18f;

    [Header("Death Placeholder (until Checkpoints phase)")]
    [SerializeField] private bool healAndLogOnDeath = true;

    private bool _invincible;
    private Coroutine _blinkRoutine;
    private Renderer[] _renderers;

    private void Awake()
    {
        if (motor == null)
            motor = GetComponent<PlayerMotorCC>();

        // Try to get stats in the most reliable way
        if (stats == null)
            stats = PlayerStats.Instance;

        if (stats == null)
            stats = FindObjectOfType<PlayerStats>(true);

        _renderers = GetComponentsInChildren<Renderer>(true);
    }


    public bool IsInvincible => _invincible;

    public void TryTakeDamage(Vector3 sourcePosition, int amount = 1)
    {
        if (amount <= 0) return;
        if (_invincible) return;

        if (stats == null)
        {
            stats = PlayerStats.Instance;
            if (stats == null)
                stats = FindObjectOfType<PlayerStats>(true);
        }

        if (stats == null)
        {
            Debug.LogError($"{nameof(PlayerHealthController)}: PlayerStats not found. Add GameState with PlayerStats to the scene.");
            return;
        }


        // 1) Apply damage to numbers
        stats.TakeDamage(amount);

        // 2) Knockback away from source (XZ) + upward
        Vector3 away = (transform.position - sourcePosition);
        away.y = 0f;

        if (away.sqrMagnitude < 0.0001f)
            away = -transform.forward;

        away.Normalize();

        Vector3 impulse = away * knockbackHorizontalStrength + Vector3.up * knockbackVerticalStrength;

        // Uses the new motor method we added
        motor.ApplyExternalImpulse(impulse, knockbackControlLockTime);

        // 3) Invincibility frames
        StartInvincibility(invincibilityDuration);

        // 4) Temporary death behavior (real respawn next phase)
        if (stats.CurrentHearts <= 0)
        {
            stats.RegisterDeath();

            if (RespawnManager.Instance != null)
                RespawnManager.Instance.RespawnPlayer(gameObject);
            else
                stats.FullHeal();
        }


    }

    private void StartInvincibility(float duration)
    {
        if (duration <= 0f) return;

        _invincible = true;

        if (_blinkRoutine != null)
            StopCoroutine(_blinkRoutine);

        if (blinkDuringInvincibility)
            _blinkRoutine = StartCoroutine(BlinkRoutine(duration));
        else
            StartCoroutine(InvincibilityTimer(duration));
    }

    private IEnumerator InvincibilityTimer(float duration)
    {
        yield return new WaitForSeconds(duration);
        _invincible = false;
    }

    private IEnumerator BlinkRoutine(float duration)
    {
        float timer = 0f;
        bool visible = true;

        while (timer < duration)
        {
            visible = !visible;
            SetRenderersVisible(visible);

            float wait = Mathf.Max(0.02f, blinkRate);
            yield return new WaitForSeconds(wait);
            timer += wait;
        }

        SetRenderersVisible(true);
        _invincible = false;
    }

    private void SetRenderersVisible(bool visible)
    {
        if (_renderers == null) return;

        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] != null)
                _renderers[i].enabled = visible;
        }
    }
}
