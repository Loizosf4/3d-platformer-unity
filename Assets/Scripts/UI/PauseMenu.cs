using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

/// <summary>
/// Simple pause menu that freezes the game and shows pause UI
/// Press P or Esc to toggle pause
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The pause menu UI panel to show/hide")]
    [SerializeField] private GameObject pauseMenuPanel;
    
    [Header("Scene Settings")]
    [Tooltip("Name of the main menu scene to return to")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    
    [Header("Input Settings")]
    [Tooltip("Drag your IA_Player input actions asset here (optional - will also check legacy input)")]
    [SerializeField] private InputActionAsset inputActions;
    
    private bool _isPaused = false;
    private InputAction _pauseAction;
    
    private void Awake()
    {
        // Try to find and set up the pause action from the Input Actions asset
        if (inputActions != null)
        {
            var gameplayMap = inputActions.FindActionMap("Gameplay");
            if (gameplayMap != null)
            {
                _pauseAction = gameplayMap.FindAction("Pause");
                if (_pauseAction != null)
                {
                    _pauseAction.Enable();
                }
            }
        }
    }
    
    private void Start()
    {
        // Make sure pause menu is hidden at start
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }
        
        // Ensure game is running
        Time.timeScale = 1f;
    }
    
    private void Update()
    {
        // Check for pause input using new Input System if available
        bool pausePressed = false;
        
        if (_pauseAction != null && _pauseAction.WasPressedThisFrame())
        {
            pausePressed = true;
        }
        
        // Fallback to Keyboard class for Escape key (works with new Input System)
        if (Keyboard.current != null && (Keyboard.current.pKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame))
        {
            pausePressed = true;
        }
        
        if (pausePressed)
        {
            TogglePause();
        }
    }
    
    /// <summary>
    /// Toggle between paused and unpaused states
    /// </summary>
    public void TogglePause()
    {
        _isPaused = !_isPaused;
        
        if (_isPaused)
        {
            Pause();
        }
        else
        {
            Resume();
        }
    }
    
    /// <summary>
    /// Pause the game
    /// </summary>
    private void Pause()
    {
        Time.timeScale = 0f; // Freeze game
        
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
        }
        
        Debug.Log("Game Paused");
    }
    
    /// <summary>
    /// Resume the game
    /// </summary>
    public void Resume()
    {
        _isPaused = false;
        Time.timeScale = 1f; // Resume game
        
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }
        
        Debug.Log("Game Resumed");
    }
    
    private void OnDestroy()
    {
        // Clean up input action
        if (_pauseAction != null)
        {
            _pauseAction.Disable();
        }
    }
    
    /// <summary>
    /// Called when Continue button is clicked
    /// </summary>
    public void OnContinueClicked()
    {
        Resume();
    }
    
    /// <summary>
    /// Called when Exit to Main Menu button is clicked
    /// </summary>
    public void OnExitToMainMenuClicked()
    {
        Time.timeScale = 1f; // Reset time scale before loading
        Debug.Log($"Loading main menu: {mainMenuSceneName}");
        SceneManager.LoadScene(mainMenuSceneName);
    }
    
    /// <summary>
    /// Called when Exit to Desktop button is clicked
    /// </summary>
    public void OnExitToDesktopClicked()
    {
        Time.timeScale = 1f; // Reset time scale before quitting
        Debug.Log("Exiting to desktop...");
        
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
