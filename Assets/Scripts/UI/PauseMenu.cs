using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

/// <summary>
/// Simple pause menu that disables player controls and shows pause UI
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
    [Tooltip("Drag your IA_Player input actions asset here")]
    [SerializeField] private InputActionAsset inputActions;
    
    private bool _isPaused = false;
    private InputAction _pauseAction;
    private PlayerInputReader _playerInput;
    private PlayerMotorCC _playerMotor;
    
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
        
        // Find player components
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            _playerInput = player.GetComponentInChildren<PlayerInputReader>();
            _playerMotor = player.GetComponent<PlayerMotorCC>();
        }
    }
    
    private void Start()
    {
        // Make sure pause menu is hidden at start
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }
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
        // Disable player controls instead of Time.timeScale
        if (_playerInput != null) _playerInput.enabled = false;
        if (_playerMotor != null) _playerMotor.enabled = false;
        
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
        }
        
        // Ensure cursor is visible and unlocked
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        
        Debug.Log("Game Paused - Player disabled");
    }
    
    /// <summary>
    /// Resume the game
    /// </summary>
    public void Resume()
    {
        _isPaused = false;
        
        // Re-enable player controls
        if (_playerInput != null) _playerInput.enabled = true;
        if (_playerMotor != null) _playerMotor.enabled = true;
        
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }
        
        Debug.Log("Game Resumed - Player enabled");
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
        // Re-enable player before leaving (cleanup)
        if (_playerInput != null) _playerInput.enabled = true;
        if (_playerMotor != null) _playerMotor.enabled = true;
        
        Debug.Log($"Loading main menu: {mainMenuSceneName}");
        SceneManager.LoadScene(mainMenuSceneName);
    }
    
    /// <summary>
    /// Called when Exit to Desktop button is clicked
    /// </summary>
    public void OnExitToDesktopClicked()
    {
        Debug.Log("Exiting to desktop...");
        
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
