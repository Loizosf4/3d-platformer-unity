using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor script to set up the player prefab with the character model.
/// Run this from the Unity menu: Tools > Setup Player Character Model
/// </summary>
public class SetupPlayerCharacterModel
{
    [MenuItem("Tools/Setup Player Character Model")]
    public static void SetupCharacter()
    {
        // Load the player prefab
        string prefabPath = "Assets/Prefabs/Player/PF_Player.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        
        if (prefab == null)
        {
            Debug.LogError($"Could not find player prefab at: {prefabPath}");
            return;
        }
        
        // Load the character model (Idle.fbx)
        string modelPath = "Assets/Prefabs/Player/Idle.fbx";
        GameObject characterModel = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        
        if (characterModel == null)
        {
            Debug.LogError($"Could not find character model at: {modelPath}");
            return;
        }
        
        // Open prefab for editing
        string assetPath = AssetDatabase.GetAssetPath(prefab);
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);
        
        try
        {
            // Find and disable the Visual (capsule) child
            Transform visualChild = prefabRoot.transform.Find("Visual");
            if (visualChild != null)
            {
                visualChild.gameObject.SetActive(false);
                Debug.Log("Disabled 'Visual' (capsule) child");
            }
            
            // Check if character model already exists
            Transform existingModel = prefabRoot.transform.Find("CharacterModel");
            if (existingModel != null)
            {
                Debug.Log("Character model already exists, updating it...");
                Object.DestroyImmediate(existingModel.gameObject);
            }
            
            // Instantiate the character model as a child
            GameObject modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(characterModel, prefabRoot.transform);
            modelInstance.name = "CharacterModel";
            
            // Position at origin, no rotation
            modelInstance.transform.localPosition = Vector3.zero;
            modelInstance.transform.localRotation = Quaternion.identity;
            modelInstance.transform.localScale = Vector3.one;
            
            // Set layer to match parent (layer 8)
            SetLayerRecursively(modelInstance, prefabRoot.layer);
            
            // Add Animator component if not present
            Animator animator = modelInstance.GetComponent<Animator>();
            if (animator == null)
            {
                animator = modelInstance.AddComponent<Animator>();
                Debug.Log("Added Animator component");
            }
            
            // Try to find and assign the animator controller
            string controllerPath = "Assets/Prefabs/Player/AC_Player.controller";
            RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
            if (controller != null)
            {
                animator.runtimeAnimatorController = controller;
                Debug.Log("Assigned Animator Controller");
            }
            else
            {
                Debug.LogWarning($"Animator Controller not found at {controllerPath}. Run 'Tools > Create Player Animator Controller' first, then run this again.");
            }
            
            // Add PlayerAnimator component if not present
            var playerAnimator = modelInstance.GetComponent<PlayerAnimator>();
            if (playerAnimator == null)
            {
                playerAnimator = modelInstance.AddComponent<PlayerAnimator>();
                Debug.Log("Added PlayerAnimator component");
            }
            
            // Save the prefab
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
            Debug.Log("Player prefab updated successfully!");
            Debug.Log("The capsule 'Visual' has been disabled and the character model has been added.");
        }
        finally
        {
            // Unload the prefab contents
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
        
        // Refresh the asset database
        AssetDatabase.Refresh();
        
        // Select the prefab in the project window
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
    }
    
    private static void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
}
