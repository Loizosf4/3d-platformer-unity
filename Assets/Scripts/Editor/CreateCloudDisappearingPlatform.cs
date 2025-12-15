using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor script to create a cloud-themed disappearing platform.
/// Run this from the Unity menu: Tools > Create Cloud Disappearing Platform
/// </summary>
public class CreateCloudDisappearingPlatform
{
    [MenuItem("Tools/Create Cloud Disappearing Platform")]
    public static void CreatePlatform()
    {
        // Load the cloud mesh from the FBX
        string cloudFbxPath = "Assets/Art/Clouds/cloud 2.fbx";
        GameObject cloudFbx = AssetDatabase.LoadAssetAtPath<GameObject>(cloudFbxPath);
        
        if (cloudFbx == null)
        {
            Debug.LogError($"Could not find cloud mesh at: {cloudFbxPath}");
            return;
        }
        
        // Get the mesh from the FBX
        MeshFilter fbxMeshFilter = cloudFbx.GetComponentInChildren<MeshFilter>();
        Mesh cloudMesh = null;
        
        if (fbxMeshFilter != null)
        {
            cloudMesh = fbxMeshFilter.sharedMesh;
        }
        
        // If no mesh filter, try to load the mesh directly from the FBX assets
        if (cloudMesh == null)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(cloudFbxPath);
            foreach (Object asset in assets)
            {
                if (asset is Mesh mesh)
                {
                    cloudMesh = mesh;
                    break;
                }
            }
        }
        
        if (cloudMesh == null)
        {
            Debug.LogError("Could not find mesh in cloud FBX file");
            return;
        }
        
        Debug.Log($"Found cloud mesh: {cloudMesh.name}");
        
        // Load the original disappearing platform prefab
        string originalPrefabPath = "Assets/Prefabs/Platforms/PF_DisappearingPlatform.prefab";
        GameObject originalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(originalPrefabPath);
        
        if (originalPrefab == null)
        {
            Debug.LogError($"Could not find original prefab at: {originalPrefabPath}");
            return;
        }
        
        // Create a new GameObject
        GameObject newPlatform = new GameObject("PF_CloudDisappearingPlatform");
        
        // Add MeshFilter and assign the cloud mesh
        MeshFilter meshFilter = newPlatform.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = cloudMesh;
        
        // Add MeshRenderer and copy material from original
        MeshRenderer meshRenderer = newPlatform.AddComponent<MeshRenderer>();
        MeshRenderer originalRenderer = originalPrefab.GetComponent<MeshRenderer>();
        if (originalRenderer != null && originalRenderer.sharedMaterial != null)
        {
            meshRenderer.sharedMaterial = originalRenderer.sharedMaterial;
        }
        
        // Add BoxCollider - adjust size based on mesh bounds
        BoxCollider boxCollider = newPlatform.AddComponent<BoxCollider>();
        Bounds meshBounds = cloudMesh.bounds;
        boxCollider.center = meshBounds.center;
        boxCollider.size = meshBounds.size;
        
        // Copy the DisappearingPlatform script component
        MonoBehaviour originalScript = originalPrefab.GetComponent("DisappearingPlatform") as MonoBehaviour;
        if (originalScript != null)
        {
            System.Type scriptType = originalScript.GetType();
            Component newScript = newPlatform.AddComponent(scriptType);
            EditorUtility.CopySerialized(originalScript, newScript);
        }
        else
        {
            Debug.LogWarning("Could not find DisappearingPlatform script on original prefab. You may need to add it manually.");
        }
        
        // Set a reasonable default scale
        newPlatform.transform.localScale = Vector3.one;
        
        // Save as prefab
        string newPrefabPath = "Assets/Prefabs/Platforms/PF_CloudDisappearingPlatform.prefab";
        
        // Delete existing if it exists
        if (AssetDatabase.LoadAssetAtPath<GameObject>(newPrefabPath) != null)
        {
            AssetDatabase.DeleteAsset(newPrefabPath);
        }
        
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(newPlatform, newPrefabPath);
        
        // Clean up the temporary GameObject
        Object.DestroyImmediate(newPlatform);
        
        // Select the new prefab
        Selection.activeObject = prefab;
        
        Debug.Log($"Cloud Disappearing Platform created at: {newPrefabPath}");
        Debug.Log("You may need to adjust the collider size and scale in the prefab.");
    }
}
