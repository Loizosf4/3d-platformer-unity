using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

/// <summary>
/// Editor script to create the Player Animator Controller.
/// Run this from the Unity menu: Tools > Create Player Animator Controller
/// </summary>
public class CreatePlayerAnimatorController
{
    // ============ TWEAK THESE VALUES ============
    // Speed threshold for transitioning between Idle and Run
    private const float IDLE_TO_RUN_THRESHOLD = 0.2f;  // Start running when speed >= this
    private const float RUN_TO_IDLE_THRESHOLD = 1f;  // Go back to idle when speed < this (hysteresis)
    
    // Transition durations (in seconds) - how long the blend takes
    private const float IDLE_RUN_TRANSITION_DURATION = 0.15f;
    private const float JUMP_TRANSITION_DURATION = 0.05f;
    private const float FALL_TRANSITION_DURATION = 0.1f;
    private const float LAND_TRANSITION_DURATION = 0.05f;
    
    // Fall detection threshold
    private const float FALL_VELOCITY_THRESHOLD = -0.1f;  // Start falling anim when velocity < this
    // ============================================

    [MenuItem("Tools/Create Player Animator Controller")]
    public static void CreateController()
    {
        // Create the animator controller
        string path = "Assets/Prefabs/Player/AC_Player.controller";
        
        // Delete existing controller if it exists
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(path) != null)
        {
            AssetDatabase.DeleteAsset(path);
        }
        
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        
        // Add parameters
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("VerticalVelocity", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsRunning", AnimatorControllerParameterType.Bool);  // Used for Idle <-> Run transitions
        controller.AddParameter("IsJumping", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsDashing", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsFalling", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsWallSliding", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Land", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Dash", AnimatorControllerParameterType.Trigger);
        
        // Get the root state machine
        AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;
        
        // Find animation clips from the FBX files
        AnimationClip idleClip = FindClipInFBX("Assets/Prefabs/Player/Idle.fbx", "Idle");
        AnimationClip runClip = FindClipInFBX("Assets/Prefabs/Player/Running.fbx", "Running");
        AnimationClip jumpClip = FindClipInFBX("Assets/Prefabs/Player/Jump.fbx", "Jump");
        AnimationClip fallClip = FindClipInFBX("Assets/Prefabs/Player/Fall.fbx", "Fall");
        AnimationClip runningJumpClip = FindClipInFBX("Assets/Prefabs/Player/Running Jump.fbx", "Running Jump");
        
        Debug.Log($"Found clips - Idle: {idleClip != null}, Run: {runClip != null}, Jump: {jumpClip != null}, Fall: {fallClip != null}");
        
        // Create states
        AnimatorState idleState = rootStateMachine.AddState("Idle", new Vector3(0, 0, 0));
        AnimatorState runState = rootStateMachine.AddState("Run", new Vector3(250, 0, 0));
        AnimatorState jumpState = rootStateMachine.AddState("Jump", new Vector3(125, -120, 0));
        AnimatorState fallState = rootStateMachine.AddState("Fall", new Vector3(250, -120, 0));
        AnimatorState landState = rootStateMachine.AddState("Land", new Vector3(375, -120, 0));
        
        // Assign clips to states (if found)
        if (idleClip != null) idleState.motion = idleClip;
        if (runClip != null) runState.motion = runClip;
        if (jumpClip != null) jumpState.motion = jumpClip;
        if (fallClip != null) fallState.motion = fallClip;
        // Land can use idle or a specific land animation if you have one
        if (idleClip != null) landState.motion = idleClip;
        
        // Set default state
        rootStateMachine.defaultState = idleState;
        
        // === CREATE TRANSITIONS ===
        
        // Idle -> Run (when IsRunning is true)
        AnimatorStateTransition idleToRun = idleState.AddTransition(runState);
        idleToRun.AddCondition(AnimatorConditionMode.If, 0, "IsRunning");
        idleToRun.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
        idleToRun.hasExitTime = false;
        idleToRun.duration = IDLE_RUN_TRANSITION_DURATION;
        
        // Run -> Idle (when IsRunning is false)
        AnimatorStateTransition runToIdle = runState.AddTransition(idleState);
        runToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsRunning");
        runToIdle.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
        runToIdle.hasExitTime = false;
        runToIdle.duration = IDLE_RUN_TRANSITION_DURATION;
        
        // Any State -> Jump (on Jump trigger)
        AnimatorStateTransition anyToJump = rootStateMachine.AddAnyStateTransition(jumpState);
        anyToJump.AddCondition(AnimatorConditionMode.If, 0, "Jump");
        anyToJump.hasExitTime = false;
        anyToJump.duration = JUMP_TRANSITION_DURATION;
        anyToJump.canTransitionToSelf = false;
        
        // Jump -> Fall (when VerticalVelocity < 0)
        AnimatorStateTransition jumpToFall = jumpState.AddTransition(fallState);
        jumpToFall.AddCondition(AnimatorConditionMode.Less, 0f, "VerticalVelocity");
        jumpToFall.hasExitTime = false;
        jumpToFall.duration = FALL_TRANSITION_DURATION;
        
        // Fall -> Land (when IsGrounded)
        AnimatorStateTransition fallToLand = fallState.AddTransition(landState);
        fallToLand.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
        fallToLand.hasExitTime = false;
        fallToLand.duration = LAND_TRANSITION_DURATION;
        
        // Land -> Idle (after exit time)
        AnimatorStateTransition landToIdle = landState.AddTransition(idleState);
        landToIdle.hasExitTime = true;
        landToIdle.exitTime = 0.9f;
        landToIdle.duration = FALL_TRANSITION_DURATION;
        
        // Land -> Run (if still moving)
        AnimatorStateTransition landToRun = landState.AddTransition(runState);
        landToRun.AddCondition(AnimatorConditionMode.Greater, IDLE_TO_RUN_THRESHOLD, "Speed");
        landToRun.hasExitTime = true;
        landToRun.exitTime = 0.5f;
        landToRun.duration = FALL_TRANSITION_DURATION;
        
        // Idle -> Fall (if not grounded and falling)
        AnimatorStateTransition idleToFall = idleState.AddTransition(fallState);
        idleToFall.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");
        idleToFall.AddCondition(AnimatorConditionMode.Less, FALL_VELOCITY_THRESHOLD, "VerticalVelocity");
        idleToFall.hasExitTime = false;
        idleToFall.duration = FALL_TRANSITION_DURATION;
        
        // Run -> Fall (if not grounded and falling)
        AnimatorStateTransition runToFall = runState.AddTransition(fallState);
        runToFall.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");
        runToFall.AddCondition(AnimatorConditionMode.Less, FALL_VELOCITY_THRESHOLD, "VerticalVelocity");
        runToFall.hasExitTime = false;
        runToFall.duration = FALL_TRANSITION_DURATION;
        
        // Save the controller
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"Player Animator Controller created at: {path}");
        Debug.Log("States created: Idle, Run, Jump, Fall, Land");
        Debug.Log($"Thresholds: Idle->Run at {IDLE_TO_RUN_THRESHOLD}, Run->Idle at {RUN_TO_IDLE_THRESHOLD}");
        Debug.Log("Remember to:");
        Debug.Log("1. Open the Animator Controller and verify the transitions");
        Debug.Log("2. Assign any missing animation clips to the states");
        Debug.Log("3. Add the Animator component to your character model");
        Debug.Log("4. Assign this controller to the Animator");
        Debug.Log("5. Add the PlayerAnimator script to the character model");
        
        // Select the created asset
        Selection.activeObject = controller;
    }
    
    private static AnimationClip FindClipInFBX(string fbxPath, string expectedClipName)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        
        // First try to find by expected name
        foreach (Object asset in assets)
        {
            if (asset is AnimationClip clip && !clip.name.Contains("__preview__"))
            {
                if (clip.name == expectedClipName)
                {
                    Debug.Log($"Found clip '{clip.name}' in {fbxPath}");
                    return clip;
                }
            }
        }
        
        // If not found by name, find the longest clip (the actual animation, not the short empty ones)
        AnimationClip longestClip = null;
        float maxLength = 0;
        
        foreach (Object asset in assets)
        {
            if (asset is AnimationClip clip && !clip.name.Contains("__preview__"))
            {
                if (clip.length > maxLength)
                {
                    maxLength = clip.length;
                    longestClip = clip;
                }
            }
        }
        
        if (longestClip != null)
        {
            Debug.Log($"Found clip '{longestClip.name}' (length: {longestClip.length}s) in {fbxPath}");
        }
        else
        {
            Debug.LogWarning($"No valid animation clip found in {fbxPath}");
        }
        
        return longestClip;
    }
    
    private static AnimationClip FindAnimationClip(string name)
    {
        // Search in the Player prefabs folder
        string[] guids = AssetDatabase.FindAssets($"{name} t:AnimationClip", new[] { "Assets/Prefabs/Player" });
        
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            
            // Load all objects from FBX files to find embedded animations
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (Object asset in assets)
            {
                if (asset is AnimationClip clip && !clip.name.Contains("__preview__"))
                {
                    Debug.Log($"Found animation clip: {clip.name} at {assetPath}");
                    return clip;
                }
            }
        }
        
        // Also try to find clips embedded in FBX files
        string[] fbxGuids = AssetDatabase.FindAssets($"{name} t:Model", new[] { "Assets/Prefabs/Player" });
        foreach (string guid in fbxGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (Object asset in assets)
            {
                if (asset is AnimationClip clip && !clip.name.Contains("__preview__"))
                {
                    Debug.Log($"Found animation clip in FBX: {clip.name} at {assetPath}");
                    return clip;
                }
            }
        }
        
        Debug.LogWarning($"Animation clip '{name}' not found. You'll need to assign it manually.");
        return null;
    }
}
