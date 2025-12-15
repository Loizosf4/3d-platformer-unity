using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor script to create a GameState object with PlayerStats in the scene.
/// Run this from the Unity menu: Tools > Create GameState
/// </summary>
public class CreateGameState
{
    [MenuItem("Tools/Create GameState")]
    public static void Create()
    {
        // Check if GameState already exists
        PlayerStats existingStats = Object.FindObjectOfType<PlayerStats>();
        if (existingStats != null)
        {
            Debug.Log($"GameState already exists: {existingStats.gameObject.name}");
            Selection.activeGameObject = existingStats.gameObject;
            return;
        }
        
        // Create new GameState object
        GameObject gameState = new GameObject("GameState");
        gameState.AddComponent<PlayerStats>();
        
        // Register undo
        Undo.RegisterCreatedObjectUndo(gameState, "Create GameState");
        
        // Select it
        Selection.activeGameObject = gameState;
        
        Debug.Log("GameState created with PlayerStats component. It will persist across scenes (DontDestroyOnLoad).");
        Debug.Log("Make sure to save your scene!");
    }
    
    [MenuItem("Tools/Create GameState", true)]
    public static bool CreateValidate()
    {
        // Only allow in play mode or edit mode with a scene open
        return !EditorApplication.isCompiling;
    }
}
