using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PauseMenuUI : MonoBehaviour
{
    [Header("Buttons")]
    public Button resumeButton;
    public Button backToMainMenuButton;

    private PauseSystem _pauseSystem;

    private void Awake()
    {
        _pauseSystem = FindFirstObjectByType<PauseSystem>();

        if (_pauseSystem == null)
        {
            Debug.LogError("[PauseMenuUI] PauseSystem not found in scene.");
            return;
        }

        // IMPORTANT: clear old listeners so we don't double-bind if prefab is reused
        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(_pauseSystem.Resume);
        }

        if (backToMainMenuButton != null)
        {
            backToMainMenuButton.onClick.RemoveAllListeners();
            backToMainMenuButton.onClick.AddListener(_pauseSystem.BackToMainMenu);
        }
    }

    private void OnEnable()
    {
        // Auto-select Resume for controller/keyboard navigation
        if (resumeButton != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(resumeButton.gameObject);
    }

    private void Update()
    {
        // Keep a selected button so gamepad/keyboard navigation continues
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null)
        {
            if (resumeButton != null)
                EventSystem.current.SetSelectedGameObject(resumeButton.gameObject);
        }
    }

}
