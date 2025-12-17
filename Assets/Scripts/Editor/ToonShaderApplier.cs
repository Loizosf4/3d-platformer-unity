using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Editor tool to apply toon shaders to objects in the scene.
/// Run from Tools > Toon Shader > Apply to Selected or Apply to All
/// </summary>
public class ToonShaderApplier : EditorWindow
{
    private enum ToonShaderType
    {
        Standard,           // Full toon with optional outline, shadows, highlights, rim
        NoOutline,          // Toon shading without any outline
        Transparent,        // For transparent objects
        Unlit,              // Simple unlit with optional outline
        OutlineOnly         // Only add black outline, preserve original material
    }
    
    private ToonShaderType selectedShaderType = ToonShaderType.Standard;
    private bool includeChildren = true;
    private bool createMaterialCopies = true;
    private bool enableOutline = true;
    
    private Color shadowColor = new Color(0.3f, 0.3f, 0.4f, 1f);
    private float shadowTolerance = 0f;
    private Color outlineColor = Color.black;
    private float outlineWidth = 0.005f;
    private Color rimColor = Color.white;
    private float rimIntensity = 0.5f;
    
    [MenuItem("Tools/Toon Shader/Open Toon Shader Window")]
    public static void ShowWindow()
    {
        GetWindow<ToonShaderApplier>("Toon Shader Applier");
    }
    
    [MenuItem("Tools/Toon Shader/Apply Toon WITH Outline to Selected")]
    public static void ApplyWithOutlineQuick()
    {
        ApplyToonShaderToSelected(ToonShaderType.Standard, true, true, true);
    }
    
    [MenuItem("Tools/Toon Shader/Apply Toon WITHOUT Outline to Selected")]
    public static void ApplyNoOutlineQuick()
    {
        ApplyToonShaderToSelected(ToonShaderType.NoOutline, true, true, false);
    }
    
    [MenuItem("Tools/Toon Shader/Apply OUTLINE ONLY to Selected")]
    public static void ApplyOutlineOnlyQuick()
    {
        ApplyToonShaderToSelected(ToonShaderType.OutlineOnly, true, true, true);
    }
    
    [MenuItem("Tools/Toon Shader/Apply Toon to All Renderers in Scene")]
    public static void ApplyToAllInScene()
    {
        if (!EditorUtility.DisplayDialog("Apply Toon Shader", 
            "This will apply the toon shader to ALL renderers in the scene. Continue?", 
            "Yes", "Cancel"))
            return;
            
        Renderer[] allRenderers = Object.FindObjectsOfType<Renderer>();
        int count = 0;
        
        foreach (Renderer renderer in allRenderers)
        {
            if (ApplyToonToRenderer(renderer, ToonShaderType.Standard, true, true))
                count++;
        }
        
        Debug.Log($"Applied toon shader to {count} renderers.");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("Toon Shader Settings", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();
        
        selectedShaderType = (ToonShaderType)EditorGUILayout.EnumPopup("Shader Type", selectedShaderType);
        includeChildren = EditorGUILayout.Toggle("Include Children", includeChildren);
        createMaterialCopies = EditorGUILayout.Toggle("Create Material Copies", createMaterialCopies);
        
        EditorGUILayout.Space();
        
        // Only show outline options for shaders that support it
        if (selectedShaderType == ToonShaderType.Standard || selectedShaderType == ToonShaderType.Unlit || selectedShaderType == ToonShaderType.OutlineOnly)
        {
            GUILayout.Label("Outline Settings", EditorStyles.boldLabel);
            
            if (selectedShaderType != ToonShaderType.OutlineOnly)
            {
                enableOutline = EditorGUILayout.Toggle("Enable Outline", enableOutline);
            }
            else
            {
                enableOutline = true; // Always enabled for outline-only mode
                EditorGUILayout.HelpBox("Outline Only mode preserves your existing material and only adds a black outline.", MessageType.Info);
            }
            
            if (enableOutline)
            {
                EditorGUI.indentLevel++;
                outlineColor = EditorGUILayout.ColorField("Outline Color", outlineColor);
                outlineWidth = EditorGUILayout.Slider("Outline Width", outlineWidth, 0f, 0.03f);
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
        }
        
        // Don't show shading parameters for outline-only mode
        if (selectedShaderType != ToonShaderType.OutlineOnly)
        {
            GUILayout.Label("Shading Parameters", EditorStyles.boldLabel);
        shadowColor = EditorGUILayout.ColorField("Shadow Color", shadowColor);
        shadowTolerance = EditorGUILayout.Slider("Shadow Tolerance", shadowTolerance, 0f, 1f);
        EditorGUILayout.HelpBox("Shadow Tolerance: 0 = full shadows, 1 = no shadows", MessageType.None);
        rimColor = EditorGUILayout.ColorField("Rim Color", rimColor);
        rimIntensity = EditorGUILayout.Slider("Rim Intensity", rimIntensity, 0f, 1f);
        
            EditorGUILayout.Space();
        }
        
        if (GUILayout.Button("Apply to Selected Objects"))
        {
            ApplyToSelected();
        }
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Remove Toon Shader from Selected"))
        {
            RemoveFromSelected();
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Shader Types:\n" +
            "• Standard: Full cel shading with optional outline\n" +
            "• No Outline: Cel shading without outline pass (better performance)\n" +
            "• Transparent: For transparent/alpha objects\n" +
            "• Unlit: Flat color with optional outline\n" +
            "• Outline Only: Keeps original material, only adds black outline", 
            MessageType.Info);
    }
    
    private void ApplyToSelected()
    {
        ApplyToonShaderToSelected(selectedShaderType, includeChildren, createMaterialCopies, enableOutline,
            shadowColor, shadowTolerance, outlineColor, outlineWidth, rimColor, rimIntensity);
    }
    
    private static void ApplyToonShaderToSelected(ToonShaderType shaderType, bool includeChildren, bool createCopies, bool outline,
        Color? shadow = null, float? shadowTol = null, Color? outlineCol = null, float? outlineW = null, Color? rim = null, float? rimInt = null)
    {
        GameObject[] selectedObjects = Selection.gameObjects;
        
        if (selectedObjects.Length == 0)
        {
            Debug.LogWarning("No objects selected. Select objects in the hierarchy first.");
            return;
        }
        
        int count = 0;
        
        foreach (GameObject obj in selectedObjects)
        {
            Renderer[] renderers;
            
            if (includeChildren)
                renderers = obj.GetComponentsInChildren<Renderer>();
            else
                renderers = obj.GetComponents<Renderer>();
                
            foreach (Renderer renderer in renderers)
            {
                if (ApplyToonToRenderer(renderer, shaderType, createCopies, outline, shadow, shadowTol, outlineCol, outlineW, rim, rimInt))
                    count++;
            }
        }
        
        Debug.Log($"Applied toon shader to {count} renderers.");
    }
    
    private static bool ApplyToonToRenderer(Renderer renderer, ToonShaderType shaderType, bool createCopy, bool enableOutline,
        Color? shadow = null, float? shadowTol = null, Color? outline = null, float? outlineW = null, Color? rim = null, float? rimInt = null)
    {
        if (renderer == null) return false;
        
        // Skip certain renderer types
        if (renderer is ParticleSystemRenderer || renderer is TrailRenderer || renderer is LineRenderer)
            return false;
        
        // Special handling for Outline Only mode
        if (shaderType == ToonShaderType.OutlineOnly)
        {
            return ApplyOutlineOnly(renderer, outline ?? Color.black, outlineW ?? 0.005f);
        }
        
        string shaderName = shaderType switch
        {
            ToonShaderType.Standard => "Custom/ToonShader",
            ToonShaderType.NoOutline => "Custom/ToonShaderNoOutline",
            ToonShaderType.Transparent => "Custom/ToonShaderTransparent",
            ToonShaderType.Unlit => "Custom/ToonShaderUnlit",
            _ => "Custom/ToonShader"
        };
        
        Shader toonShader = Shader.Find(shaderName);
        
        if (toonShader == null)
        {
            Debug.LogError($"Could not find shader: {shaderName}. Make sure the shader files exist.");
            return false;
        }
        
        Material[] materials = renderer.sharedMaterials;
        Material[] newMaterials = new Material[materials.Length];
        
        for (int i = 0; i < materials.Length; i++)
        {
            Material originalMat = materials[i];
            
            if (originalMat == null)
            {
                newMaterials[i] = null;
                continue;
            }
            
            Material toonMat;
            
            if (createCopy)
            {
                toonMat = new Material(toonShader);
                toonMat.name = originalMat.name + "_Toon";
                
                // Preserve all textures from original material
                CopyTextures(originalMat, toonMat);
                
                // Copy main color if it exists
                if (originalMat.HasProperty("_Color"))
                    toonMat.SetColor("_Color", originalMat.GetColor("_Color"));
            }
            else
            {
                toonMat = originalMat;
                toonMat.shader = toonShader;
            }
            
            // Enable/disable outline keyword for shaders that support it
            if (shaderType == ToonShaderType.Standard || shaderType == ToonShaderType.Unlit)
            {
                if (enableOutline)
                {
                    toonMat.EnableKeyword("OUTLINE_ON");
                    toonMat.SetFloat("_OutlineEnabled", 1f);
                }
                else
                {
                    toonMat.DisableKeyword("OUTLINE_ON");
                    toonMat.SetFloat("_OutlineEnabled", 0f);
                }
            }
            
            // Apply custom parameters if provided
            if (shadow.HasValue && toonMat.HasProperty("_ShadowColor"))
                toonMat.SetColor("_ShadowColor", shadow.Value);
                
            if (shadowTol.HasValue && toonMat.HasProperty("_ShadowTolerance"))
                toonMat.SetFloat("_ShadowTolerance", shadowTol.Value);
                
            if (outline.HasValue && toonMat.HasProperty("_OutlineColor"))
                toonMat.SetColor("_OutlineColor", outline.Value);
                
            if (outlineW.HasValue && toonMat.HasProperty("_OutlineWidth"))
                toonMat.SetFloat("_OutlineWidth", outlineW.Value);
                
            if (rim.HasValue && toonMat.HasProperty("_RimColor"))
                toonMat.SetColor("_RimColor", rim.Value);
                
            if (rimInt.HasValue && toonMat.HasProperty("_RimIntensity"))
                toonMat.SetFloat("_RimIntensity", rimInt.Value);
            
            newMaterials[i] = toonMat;
        }
        
        Undo.RecordObject(renderer, "Apply Toon Shader");
        renderer.sharedMaterials = newMaterials;
        
        return true;
    }
    
    private static void CopyTextures(Material source, Material destination)
    {
        // Copy all texture properties
        if (source.HasProperty("_MainTex") && destination.HasProperty("_MainTex"))
            destination.SetTexture("_MainTex", source.GetTexture("_MainTex"));
        
        if (source.HasProperty("_BumpMap") && destination.HasProperty("_BumpMap"))
            destination.SetTexture("_BumpMap", source.GetTexture("_BumpMap"));
        
        if (source.HasProperty("_MetallicGlossMap") && destination.HasProperty("_MetallicGlossMap"))
            destination.SetTexture("_MetallicGlossMap", source.GetTexture("_MetallicGlossMap"));
        
        if (source.HasProperty("_OcclusionMap") && destination.HasProperty("_OcclusionMap"))
            destination.SetTexture("_OcclusionMap", source.GetTexture("_OcclusionMap"));
        
        if (source.HasProperty("_EmissionMap") && destination.HasProperty("_EmissionMap"))
            destination.SetTexture("_EmissionMap", source.GetTexture("_EmissionMap"));
        
        // Copy texture scales and offsets
        if (source.HasProperty("_MainTex") && destination.HasProperty("_MainTex"))
        {
            destination.SetTextureScale("_MainTex", source.GetTextureScale("_MainTex"));
            destination.SetTextureOffset("_MainTex", source.GetTextureOffset("_MainTex"));
        }
    }
    
    private static bool ApplyOutlineOnly(Renderer renderer, Color outlineColor, float outlineWidth)
    {
        // For outline only, we need to use a special approach
        // We'll keep the original material and add an outline pass
        
        Material[] materials = renderer.sharedMaterials;
        bool modified = false;
        
        for (int i = 0; i < materials.Length; i++)
        {
            Material mat = materials[i];
            if (mat == null) continue;
            
            // Check if material already has outline shader
            if (mat.shader.name.Contains("ToonShaderOutlineOnly"))
                continue;
            
            // Create a wrapper shader that uses the original material for rendering
            // but adds an outline pass
            Shader outlineShader = Shader.Find("Custom/ToonShaderOutlineOnly");
            
            if (outlineShader != null)
            {
                Material newMat = new Material(outlineShader);
                newMat.name = mat.name + "_WithOutline";
                
                // Copy ALL properties from original
                newMat.CopyPropertiesFromMaterial(mat);
                newMat.shader = outlineShader;
                
                // Set outline parameters
                newMat.SetColor("_OutlineColor", outlineColor);
                newMat.SetFloat("_OutlineWidth", outlineWidth);
                newMat.EnableKeyword("OUTLINE_ON");
                
                materials[i] = newMat;
                modified = true;
            }
            else
            {
                Debug.LogWarning("ToonShaderOutlineOnly shader not found. Creating it...");
                // Continue with other materials
            }
        }
        
        if (modified)
        {
            Undo.RecordObject(renderer, "Apply Outline Only");
            renderer.sharedMaterials = materials;
        }
        
        return modified;
    }
    
    private void RemoveFromSelected()
    {
        GameObject[] selectedObjects = Selection.gameObjects;
        
        if (selectedObjects.Length == 0)
        {
            Debug.LogWarning("No objects selected.");
            return;
        }
        
        int count = 0;
        Shader standardShader = Shader.Find("Standard");
        
        foreach (GameObject obj in selectedObjects)
        {
            Renderer[] renderers = includeChildren 
                ? obj.GetComponentsInChildren<Renderer>() 
                : obj.GetComponents<Renderer>();
                
            foreach (Renderer renderer in renderers)
            {
                Material[] materials = renderer.sharedMaterials;
                
                foreach (Material mat in materials)
                {
                    if (mat != null && mat.shader.name.Contains("Toon"))
                    {
                        Undo.RecordObject(mat, "Remove Toon Shader");
                        mat.shader = standardShader;
                        count++;
                    }
                }
            }
        }
        
        Debug.Log($"Reverted {count} materials to Standard shader.");
    }
}
