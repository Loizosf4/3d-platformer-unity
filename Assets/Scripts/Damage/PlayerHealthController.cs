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
    [SerializeField] private bool blinkDuringInvincibility = true;
    [SerializeField] private float blinkRate = 0.12f;

    [Header("Knockback (tune in Inspector)")]
    [SerializeField] private float knockbackHorizontalStrength = 8f;
    [SerializeField] private float knockbackVerticalStrength = 6f;
    [SerializeField] private float knockbackControlLockTime = 0.18f;

    private bool _invincible;
    private bool _handlingDeath;

    private Coroutine _blinkRoutine;
    private Renderer[] _renderers;

    private void Awake()
    {
        if (motor == null)
            motor = GetComponent<PlayerMotorCC>();

        // Always try to find stats reliably
        if (stats == null)
            stats = PlayerStats.Instance;

        if (stats == null)
            stats = FindObjectOfType<PlayerStats>(true);

        _renderers = GetComponentsInChildren<Renderer>(true);
    }

    private void OnEnable()
    {
        StartCoroutine(BindToStatsWhenReady());
    }

    private System.Collections.IEnumerator BindToStatsWhenReady()
    {
        // Try for up to 2 seconds (plenty)
        float timeout = 2f;

        while (stats == null && timeout > 0f)
        {
            stats = PlayerStats.Instance;
            if (stats == null)
                stats = FindObjectOfType<PlayerStats>(true);

            if (stats != null)
                break;

            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (stats == null)
        {
            Debug.LogError("[PlayerHealthController] PlayerStats still not found after waiting. GameState is missing in this scene.");
            yield break;
        }

        stats.OnHeartsChanged += OnHeartsChanged;
        Debug.Log("[PlayerHealthController] Subscribed to PlayerStats.OnHeartsChanged");
    }



    private void OnDisable()
    {
        if (stats != null)
            stats.OnHeartsChanged -= OnHeartsChanged;
    }


    private void OnHeartsChanged(int current, int max)
    {
        Debug.Log($"[PlayerHealthController] OnHeartsChanged -> {current}/{max}");

        if (_handlingDeath) return;

        if (current <= 0)
            HandleDeath();
    }


    public bool IsInvincible => _invincible;

    public void TryTakeDamage(Vector3 sourcePosition, int amount = 1)
    {
        if (amount <= 0) return;
        if (_invincible) return;
        if (_handlingDeath) return;

        if (stats == null)
        {
            stats = PlayerStats.Instance;
            if (stats == null) stats = FindObjectOfType<PlayerStats>(true);
        }

        if (stats == null)
        {
            Debug.LogError($"{nameof(PlayerHealthController)}: PlayerStats not found. Make sure GameState exists.");
            return;
        }

        // 1) Numbers
        stats.TakeDamage(amount);

        // 2) Knockback away from source (XZ) + upward
        Vector3 away = (transform.position - sourcePosition);
        away.y = 0f;

        if (away.sqrMagnitude < 0.0001f)
            away = -transform.forward;

        away.Normalize();

        Vector3 impulse = away * knockbackHorizontalStrength + Vector3.up * knockbackVerticalStrength;
        motor.ApplyExternalImpulse(impulse, knockbackControlLockTime);

        // 3) I-frames
        StartInvincibility(invincibilityDuration);
    }

    private void HandleDeath()
    {
        _handlingDeath = true;

        // Stop invincibility visuals
        StopInvincibilityVisuals();

        if (stats != null)
            stats.RegisterDeath();

        if (RespawnManager.Instance != null)
        {
            RespawnManager.Instance.RespawnPlayer(gameObject);
        }
        else
        {
            // Fallback: at least heal so you aren't stuck at 0
            if (stats != null) stats.FullHeal();
            Debug.LogWarning("[PlayerHealthController] RespawnManager not found. FullHeal fallback used.");
        }

        _handlingDeath = false;
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

    private void StopInvincibilityVisuals()
    {
        if (_blinkRoutine != null)
        {
            StopCoroutine(_blinkRoutine);
            _blinkRoutine = null;
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
