using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("Fade")]
    [SerializeField] private float fadeOutTime = 0.25f;
    [SerializeField] private float fadeInTime = 0.25f;

    private Canvas _fadeCanvas;
    private Image _fadeImage;

    private bool _isTransitioning;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureFadeUI();
        SetFadeAlpha(0f);
    }

    public void RequestTransition(string targetSceneName, string targetSpawnId)
    {
        Debug.Log($"[SceneTransitionManager] RequestTransition to '{targetSceneName}' spawn '{targetSpawnId}'");

        if (_isTransitioning) return;
        StartCoroutine(TransitionRoutine(targetSceneName, targetSpawnId));
    }

    private IEnumerator TransitionRoutine(string sceneName, string spawnId)
    {
        _isTransitioning = true;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        var locker = player != null ? player.GetComponent<PlayerControlLock>() : null;

        if (locker != null) locker.SetLocked(true);

        yield return FadeTo(1f, fadeOutTime);

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        while (!op.isDone) yield return null;

        player = GameObject.FindGameObjectWithTag("Player");
        locker = player != null ? player.GetComponent<PlayerControlLock>() : null;

        PlacePlayerAtSpawn(player, spawnId);

        yield return FadeTo(0f, fadeInTime);

        if (locker != null) locker.SetLocked(false);

        _isTransitioning = false;
    }

    private void PlacePlayerAtSpawn(GameObject player, string spawnId)
    {
        if (player == null)
        {
            Debug.LogError("[SceneTransitionManager] Player not found after scene load.");
            return;
        }

        var spawns = FindObjectsOfType<SpawnPoint>(true);
        SpawnPoint target = null;
        SpawnPoint fallback = null;

        foreach (var sp in spawns)
        {
            if (sp.IsDefaultSpawn) fallback = sp;
            if (!string.IsNullOrEmpty(spawnId) && sp.SpawnId == spawnId) target = sp;
        }

        if (target == null) target = fallback;

        if (target == null)
        {
            Debug.LogWarning($"[SceneTransitionManager] No spawn '{spawnId}' and no default spawn found.");
            return;
        }

        var cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        var motor = player.GetComponent<PlayerMotorCC>();
        if (motor != null) motor.ResetMovementState();

        player.transform.SetPositionAndRotation(target.transform.position, target.transform.rotation);

        if (cc != null)
        {
            cc.enabled = true;
            cc.Move(Vector3.zero);
        }
    }

    private void EnsureFadeUI()
    {
        if (_fadeCanvas != null && _fadeImage != null) return;

        GameObject canvasGO = new GameObject("FadeCanvas");
        _fadeCanvas = canvasGO.AddComponent<Canvas>();
        _fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _fadeCanvas.sortingOrder = 9999;

        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        GameObject imgGO = new GameObject("FadeImage");
        imgGO.transform.SetParent(canvasGO.transform, false);

        _fadeImage = imgGO.AddComponent<Image>();
        _fadeImage.color = Color.black;

        RectTransform rt = _fadeImage.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        DontDestroyOnLoad(canvasGO);
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        EnsureFadeUI();

        float startAlpha = _fadeImage.color.a;
        if (duration <= 0f)
        {
            SetFadeAlpha(targetAlpha);
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            SetFadeAlpha(Mathf.Lerp(startAlpha, targetAlpha, t / duration));
            yield return null;
        }

        SetFadeAlpha(targetAlpha);
    }

    private void SetFadeAlpha(float a)
    {
        Color c = _fadeImage.color;
        c.a = Mathf.Clamp01(a);
        _fadeImage.color = c;
    }
}
