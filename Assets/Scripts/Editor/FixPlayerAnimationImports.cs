using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Editor script to fix FBX animation import settings for Mixamo characters.
/// Run this from the Unity menu: Tools > Fix Player Animation Imports
/// </summary>
public class FixPlayerAnimationImports
{
    [MenuItem("Tools/Fix Player Animation Imports")]
    public static void FixAnimations()
    {
        string playerFolder = "Assets/Prefabs/Player";
        
        Debug.Log("=== Starting Animation Import Fix ===");
        
        // Define all FBX files and their desired clip names
        var fbxConfigs = new Dictionary<string, (string clipName, bool loop)>
        {
            { "Idle.fbx", ("Idle", true) },
            { "Running.fbx", ("Running", true) },
            { "Jumping.fbx", ("Jump", false) },
            { "Fall A Loop.fbx", ("Fall", true) },
            { "RunningJump.fbx", ("RunningJump", false) }
        };
        
        // First pass: Configure Idle.fbx to create the avatar
        string idlePath = Path.Combine(playerFolder, "Idle.fbx").Replace("\\", "/");
        ConfigureMainModel(idlePath, "Idle", true);
        
        // Force reimport to create the avatar
        AssetDatabase.ImportAsset(idlePath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.Refresh();
        
        // Find the created avatar
        Avatar avatar = null;
        Object[] idleAssets = AssetDatabase.LoadAllAssetsAtPath(idlePath);
        foreach (Object asset in idleAssets)
        {
            if (asset is Avatar a)
            {
                avatar = a;
                Debug.Log($"Found avatar: {avatar.name}, IsValid: {avatar.isValid}");
                break;
            }
        }
        
        if (avatar == null)
        {
            Debug.LogError("Failed to create avatar from Idle.fbx!");
            return;
        }
        
        // Second pass: Configure all other animation FBX files
        foreach (var kvp in fbxConfigs)
        {
            if (kvp.Key == "Idle.fbx") continue; // Already configured
            
            string path = Path.Combine(playerFolder, kvp.Key).Replace("\\", "/");
            if (File.Exists(path))
            {
                ConfigureAnimationFile(path, avatar, kvp.Value.clipName, kvp.Value.loop);
            }
            else
            {
                Debug.LogWarning($"FBX file not found: {path}");
            }
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("===========================================");
        Debug.Log("Animation import settings fixed!");
        Debug.Log("Now run: Tools > Create Player Animator Controller");
        Debug.Log("Then run: Tools > Setup Player Character Model");
        Debug.Log("===========================================");
    }
    
    private static void ConfigureMainModel(string path, string clipName, bool shouldLoop)
    {
        ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null)
        {
            Debug.LogError($"Could not get ModelImporter for: {path}");
            return;
        }
        
        // Set up as Generic with avatar creation
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation = true;
        
        // Configure the animation clip
        ConfigureClip(importer, clipName, shouldLoop);
        
        importer.SaveAndReimport();
        Debug.Log($"Configured {path} as avatar source with CreateFromThisModel");
    }
    
    private static void ConfigureAnimationFile(string path, Avatar sourceAvatar, string clipName, bool shouldLoop)
    {
        ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null)
        {
            Debug.LogError($"Could not get ModelImporter for: {path}");
            return;
        }
        
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.importAnimation = true;
        
        // Set avatar source
        importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
        importer.sourceAvatar = sourceAvatar;
        
        // Configure the animation clip
        ConfigureClip(importer, clipName, shouldLoop);
        
        importer.SaveAndReimport();
        Debug.Log($"Configured {path} - clip: {clipName}, loop: {shouldLoop}");
    }
    
    private static void ConfigureClip(ModelImporter importer, string clipName, bool shouldLoop)
    {
        // Get default clips from the file to find the frame range
        ModelImporterClipAnimation[] defaultClips = importer.defaultClipAnimations;
        
        if (defaultClips.Length == 0)
        {
            Debug.LogWarning($"No default clips found, using fallback frame range");
            // Create a default clip with assumed frame range
            ModelImporterClipAnimation newClip = new ModelImporterClipAnimation
            {
                name = clipName,
                takeName = "mixamo.com",
                firstFrame = 0,
                lastFrame = 100,
                loopTime = shouldLoop,
                loopPose = false,
                lockRootRotation = true,
                lockRootHeightY = true,
                lockRootPositionXZ = true,
                keepOriginalOrientation = true,
                keepOriginalPositionY = true,
                keepOriginalPositionXZ = true
            };
            importer.clipAnimations = new ModelImporterClipAnimation[] { newClip };
            return;
        }
        
        // Find the main animation clip (the one with the most frames)
        ModelImporterClipAnimation sourceClip = defaultClips[0];
        float maxFrames = 0;
        
        foreach (var clip in defaultClips)
        {
            float frames = clip.lastFrame - clip.firstFrame;
            if (frames > maxFrames)
            {
                maxFrames = frames;
                sourceClip = clip;
            }
        }
        
        // Create the properly named clip
        ModelImporterClipAnimation newAnimClip = new ModelImporterClipAnimation
        {
            name = clipName,
            takeName = sourceClip.takeName,
            firstFrame = sourceClip.firstFrame,
            lastFrame = sourceClip.lastFrame,
            loopTime = shouldLoop,
            loopPose = false,
            lockRootRotation = true,
            lockRootHeightY = true,
            lockRootPositionXZ = true,
            keepOriginalOrientation = true,
            keepOriginalPositionY = true,
            keepOriginalPositionXZ = true
        };
        
        importer.clipAnimations = new ModelImporterClipAnimation[] { newAnimClip };
        Debug.Log($"  Clip configured: {clipName}, frames {sourceClip.firstFrame}-{sourceClip.lastFrame}, loop: {shouldLoop}");
    }
    
    [MenuItem("Tools/Debug - List Animation Clips")]
    public static void ListAnimationClips()
    {
        string playerFolder = "Assets/Prefabs/Player";
        string[] fbxFiles = { "Idle.fbx", "Running.fbx", "Jumping.fbx", "Fall A Loop.fbx", "RunningJump.fbx" };
        
        Debug.Log("========== ANIMATION DEBUG INFO ==========");
        
        foreach (string fbxFile in fbxFiles)
        {
            string path = Path.Combine(playerFolder, fbxFile).Replace("\\", "/");
            Debug.Log($"\n=== {fbxFile} ===");
            
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            bool hasClip = false;
            bool hasAvatar = false;
            
            foreach (Object asset in assets)
            {
                if (asset is AnimationClip clip && !clip.name.Contains("__preview__"))
                {
                    Debug.Log($"  CLIP: '{clip.name}', Length: {clip.length:F2}s, Looping: {clip.isLooping}");
                    hasClip = true;
                }
                else if (asset is Avatar avatar)
                {
                    Debug.Log($"  AVATAR: '{avatar.name}', IsValid: {avatar.isValid}");
                    hasAvatar = true;
                }
            }
            
            if (!hasClip) Debug.LogWarning($"  NO ANIMATION CLIPS FOUND!");
            if (!hasAvatar) Debug.LogWarning($"  NO AVATAR FOUND!");
            
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer != null)
            {
                Debug.Log($"  Import Settings:");
                Debug.Log($"    AnimationType: {importer.animationType}");
                Debug.Log($"    AvatarSetup: {importer.avatarSetup}");
                Debug.Log($"    ImportAnimation: {importer.importAnimation}");
                
                if (importer.clipAnimations != null && importer.clipAnimations.Length > 0)
                {
                    foreach (var clipAnim in importer.clipAnimations)
                    {
                        Debug.Log($"    ClipAnimation: '{clipAnim.name}', Take: '{clipAnim.takeName}', Frames: {clipAnim.firstFrame}-{clipAnim.lastFrame}, Loop: {clipAnim.loopTime}");
                    }
                }
                
                if (importer.sourceAvatar != null)
                {
                    Debug.Log($"    SourceAvatar: {importer.sourceAvatar.name}");
                }
            }
        }
        
        // Also check the animator controller
        Debug.Log("\n=== AC_Player.controller ===");
        string controllerPath = "Assets/Prefabs/Player/AC_Player.controller";
        var controller = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(controllerPath);
        if (controller != null)
        {
            Debug.Log($"  Controller found with {controller.layers.Length} layer(s)");
            foreach (var layer in controller.layers)
            {
                Debug.Log($"  Layer: {layer.name}");
                foreach (var state in layer.stateMachine.states)
                {
                    var motion = state.state.motion;
                    string motionInfo = motion != null ? $"'{motion.name}'" : "NULL";
                    Debug.Log($"    State: '{state.state.name}' -> Motion: {motionInfo}");
                }
            }
        }
        else
        {
            Debug.LogWarning($"  Controller not found at {controllerPath}");
        }
        
        Debug.Log("\n========== END DEBUG INFO ==========");
    }
}
