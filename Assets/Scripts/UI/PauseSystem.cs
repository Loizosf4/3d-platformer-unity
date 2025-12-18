using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseSystem : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Your pause menu prefab (PF_PauseMenu_NEW). Must contain its own Canvas + EventSystem.")]
    public GameObject pauseMenuPrefab;

    [Tooltip("Drag the Pause action from IA_Player (e.g., Gameplay/Pause) here as an InputActionReference.")]
    public InputActionReference pauseAction;

    [Header("Optional")]
    [Tooltip("If your player input is driven by a component like PlayerInputReader, put its type name here or drag a reference later if you add it.")]
    public MonoBehaviour playerInputToDisable;

    [Header("Back To Main Menu")]
    [Tooltip("Scene name to load when pressing Back to Main Menu. Leave empty to do nothing for now.")]
    public string mainMenuSceneName = "";

    private GameObject _spawnedMenu;
    private bool _isPaused;

    private static PauseSystem _instance;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        // NEW: listen for scene loads
        SceneManager.sceneLoaded += OnSceneLoaded;

        // existing pause input hookup
        if (pauseAction != null && pauseAction.action != null)
        {
            pauseAction.action.Enable();
            pauseAction.action.performed += OnPausePerformed;
        }
    }


    private void OnDisable()
    {
        // NEW: stop listening for scene loads
        SceneManager.sceneLoaded -= OnSceneLoaded;

        // existing pause input unhook
        if (pauseAction != null && pauseAction.action != null)
        {
            pauseAction.action.performed -= OnPausePerformed;
            pauseAction.action.Disable();
        }
    }


    private void OnPausePerformed(InputAction.CallbackContext ctx)
    {
        if (_isPaused) Resume();
        else Pause();
    }

    public void Pause()
    {
        if (_isPaused) return;
        _isPaused = true;

        Time.timeScale = 0f;

        if (playerInputToDisable != null)
            playerInputToDisable.enabled = false;

        if (_spawnedMenu == null && pauseMenuPrefab != null)
            _spawnedMenu = Instantiate(pauseMenuPrefab);
    }

    public void Resume()
    {
        if (!_isPaused) return;
        _isPaused = false;

        Time.timeScale = 1f;

        if (_spawnedMenu != null)
            Destroy(_spawnedMenu);

        if (playerInputToDisable != null)
            playerInputToDisable.enabled = true;
    }

    // Hook this to your "Back to Main Menu" button
    public void BackToMainMenu()
    {
        // Always unpause first so the next scene isn't frozen
        Time.timeScale = 1f;
        _isPaused = false;

        if (_spawnedMenu != null)
            Destroy(_spawnedMenu);

        if (playerInputToDisable != null)
            playerInputToDisable.enabled = true;

        if (!string.IsNullOrEmpty(mainMenuSceneName))
            SceneManager.LoadScene(mainMenuSceneName);
        else
            Debug.LogWarning("[PauseSystem] mainMenuSceneName is empty. Set it in the Inspector.");
    }

    private void Start()
    {
        EnsurePauseActionEnabled();
    }

    private void EnsurePauseActionEnabled()
    {
        if (pauseAction != null && pauseAction.action != null)
        {
            pauseAction.action.Enable();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == mainMenuSceneName)
            CleanupForMainMenu();
    }

    private void CleanupForMainMenu()
    {
        // ensure time isn't frozen
        Time.timeScale = 1f;
        _isPaused = false;

        // remove spawned pause UI if any
        if (_spawnedMenu != null)
            Destroy(_spawnedMenu);

        // destroy gameplay persistent systems (adjust types if your names differ)
        DestroyIfExists<SceneTransitionManager>();
        DestroyIfExists<RespawnManager>();
        //DestroyIfExists<GameState>();
        DestroyIfExists<HUDController>();
        DestroyIfExists<CameraRigBinder>();

        // finally destroy PauseSystem itself
        Destroy(gameObject);
    }

    private void DestroyIfExists<T>() where T : Component
    {
        var obj = FindObjectOfType<T>(true);
        if (obj != null)
            Destroy(obj.gameObject);
    }



}
