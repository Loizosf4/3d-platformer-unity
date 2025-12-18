using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Main menu controller with Play and Quit functionality
/// </summary>
public class MainMenu : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("Name of the first game scene to load when Play is clicked")]
    [SerializeField] private string firstSceneName = "StartingArea";
    
    /// <summary>
    /// Called when the Play button is clicked
    /// </summary>
    public void OnPlayClicked()
    {
        Debug.Log($"Loading scene: {firstSceneName}");
        SceneManager.LoadScene(firstSceneName);
    }
    
    /// <summary>
    /// Called when the Quit button is clicked
    /// </summary>
    public void OnQuitClicked()
    {
        Debug.Log("Quitting game...");
        
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
