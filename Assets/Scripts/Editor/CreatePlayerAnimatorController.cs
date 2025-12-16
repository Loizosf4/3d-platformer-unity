using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

/// <summary>
/// Editor script to create the Player Animator Controller from scratch.
/// Run this from the Unity menu: Tools > Create Player Animator Controller
/// </summary>
public class CreatePlayerAnimatorController
{
    // Animation clip information from FBX files
    private const string IDLE_FBX_PATH = "Assets/Prefabs/Player/Idle.fbx";
    private const string IDLE_CLIP_NAME = "Idle";
    
    private const string RUN_FBX_PATH = "Assets/Prefabs/Player/Running.fbx";
    private const string RUN_CLIP_NAME = "Running";
    
    private const string JUMP_FBX_PATH = "Assets/Prefabs/Player/Jumping.fbx";
    private const string JUMP_CLIP_NAME = "Jump";
    
    private const string FALL_FBX_PATH = "Assets/Prefabs/Player/Fall A Loop.fbx";
    private const string FALL_CLIP_NAME = "Fall";
    
    private const string RUNNING_JUMP_FBX_PATH = "Assets/Prefabs/Player/RunningJump.fbx";
    private const string RUNNING_JUMP_CLIP_NAME = "RunningJump";

    [MenuItem("Tools/Create Player Animator Controller")]
    public static void CreateController()
    {
        string controllerPath = "Assets/Prefabs/Player/AC_Player.controller";
        
        Debug.Log("=== Creating Player Animator Controller ===");
        
        // Delete existing controller if it exists
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath) != null)
        {
            AssetDatabase.DeleteAsset(controllerPath);
            Debug.Log("Deleted existing controller");
        }
        
        // Create new animator controller
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        
        // Add parameters
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("VerticalVelocity", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsRunning", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsJumping", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsDashing", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsFalling", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsWallSliding", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Land", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Dash", AnimatorControllerParameterType.Trigger);
        
        Debug.Log("Added animator parameters");
        
        // Load animation clips from FBX files
        AnimationClip idleClip = LoadClip(IDLE_FBX_PATH, IDLE_CLIP_NAME);
        AnimationClip runClip = LoadClip(RUN_FBX_PATH, RUN_CLIP_NAME);
        AnimationClip jumpClip = LoadClip(JUMP_FBX_PATH, JUMP_CLIP_NAME);
        AnimationClip fallClip = LoadClip(FALL_FBX_PATH, FALL_CLIP_NAME);
        
        // Verify all clips loaded
        if (idleClip == null) Debug.LogError("Failed to load Idle clip!");
        if (runClip == null) Debug.LogError("Failed to load Run clip!");
        if (jumpClip == null) Debug.LogError("Failed to load Jump clip!");
        if (fallClip == null) Debug.LogError("Failed to load Fall clip!");
        
        // Get the root state machine
        AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;
        
        // Create states
        AnimatorState idleState = rootStateMachine.AddState("Idle", new Vector3(300, 0, 0));
        AnimatorState runState = rootStateMachine.AddState("Run", new Vector3(300, 100, 0));
        AnimatorState jumpState = rootStateMachine.AddState("Jump", new Vector3(50, 200, 0));
        AnimatorState fallState = rootStateMachine.AddState("Fall", new Vector3(300, 200, 0));
        AnimatorState landState = rootStateMachine.AddState("Land", new Vector3(550, 200, 0));
        
        // Assign animation clips to states
        idleState.motion = idleClip;
        runState.motion = runClip;
        jumpState.motion = jumpClip;
        fallState.motion = fallClip;
        landState.motion = idleClip;  // Reuse idle for landing
        
        // Set default state
        rootStateMachine.defaultState = idleState;
        
        Debug.Log("Created animator states with clips");
        
        // === CREATE TRANSITIONS ===
        // Note: PlayerAnimator.cs uses direct CrossFade/Play calls, so these transitions are mostly for manual testing
        // The actual animation logic is driven by the code, not the state machine
        
        // Idle <-> Run transitions
        AnimatorStateTransition idleToRun = idleState.AddTransition(runState);
        idleToRun.hasExitTime = false;
        idleToRun.duration = 0.1f;
        idleToRun.AddCondition(AnimatorConditionMode.If, 0, "IsRunning");
        
        AnimatorStateTransition runToIdle = runState.AddTransition(idleState);
        runToIdle.hasExitTime = false;
        runToIdle.duration = 0.1f;
        runToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsRunning");
        
        // Any State -> Jump (for manual triggers)
        AnimatorStateTransition anyToJump = rootStateMachine.AddAnyStateTransition(jumpState);
        anyToJump.hasExitTime = false;
        anyToJump.duration = 0.05f;
        anyToJump.canTransitionToSelf = false;
        anyToJump.AddCondition(AnimatorConditionMode.If, 0, "Jump");
        
        // Jump -> Fall
        AnimatorStateTransition jumpToFall = jumpState.AddTransition(fallState);
        jumpToFall.hasExitTime = false;
        jumpToFall.duration = 0.1f;
        jumpToFall.AddCondition(AnimatorConditionMode.Less, 0, "VerticalVelocity");
        
        // Fall -> Jump (for trampoline bounces)
        AnimatorStateTransition fallToJump = fallState.AddTransition(jumpState);
        fallToJump.hasExitTime = false;
        fallToJump.duration = 0f;  // Instant
        fallToJump.AddCondition(AnimatorConditionMode.Greater, 0.1f, "VerticalVelocity");
        
        // Fall -> Land
        AnimatorStateTransition fallToLand = fallState.AddTransition(landState);
        fallToLand.hasExitTime = false;
        fallToLand.duration = 0.05f;
        fallToLand.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
        
        // Jump -> Land
        AnimatorStateTransition jumpToLand = jumpState.AddTransition(landState);
        jumpToLand.hasExitTime = false;
        jumpToLand.duration = 0.05f;
        jumpToLand.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
        
        // Land -> Idle
        AnimatorStateTransition landToIdle = landState.AddTransition(idleState);
        landToIdle.hasExitTime = true;
        landToIdle.exitTime = 0.8f;
        landToIdle.duration = 0.1f;
        
        // Land -> Run
        AnimatorStateTransition landToRun = landState.AddTransition(runState);
        landToRun.hasExitTime = false;
        landToRun.duration = 0.1f;
        landToRun.AddCondition(AnimatorConditionMode.If, 0, "IsRunning");
        
        // Idle -> Fall (when walking off edge)
        AnimatorStateTransition idleToFall = idleState.AddTransition(fallState);
        idleToFall.hasExitTime = false;
        idleToFall.duration = 0.1f;
        idleToFall.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");
        idleToFall.AddCondition(AnimatorConditionMode.Less, -0.1f, "VerticalVelocity");
        
        // Run -> Fall (when running off edge)
        AnimatorStateTransition runToFall = runState.AddTransition(fallState);
        runToFall.hasExitTime = false;
        runToFall.duration = 0.1f;
        runToFall.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");
        runToFall.AddCondition(AnimatorConditionMode.Less, -0.1f, "VerticalVelocity");
        
        // Idle -> Jump (for manual jumps)
        AnimatorStateTransition idleToJump = idleState.AddTransition(jumpState);
        idleToJump.hasExitTime = false;
        idleToJump.duration = 0.05f;
        idleToJump.AddCondition(AnimatorConditionMode.If, 0, "Jump");
        
        // Run -> Jump (for manual jumps)
        AnimatorStateTransition runToJump = runState.AddTransition(jumpState);
        runToJump.hasExitTime = false;
        runToJump.duration = 0.05f;
        runToJump.AddCondition(AnimatorConditionMode.If, 0, "Jump");
        
        Debug.Log("Created all transitions");
        
        // Save the controller
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("=== Animator Controller Created Successfully ===");
        Debug.Log($"Path: {controllerPath}");
        Debug.Log("States: Idle, Run, Jump, Fall, Land");
        Debug.Log("Animation clips loaded:");
        Debug.Log($"  - Idle: {(idleClip != null ? "OK" : "MISSING")}");
        Debug.Log($"  - Run: {(runClip != null ? "OK" : "MISSING")}");
        Debug.Log($"  - Jump: {(jumpClip != null ? "OK" : "MISSING")}");
        Debug.Log($"  - Fall: {(fallClip != null ? "OK" : "MISSING")}");
        
        // Select the created asset
        Selection.activeObject = controller;
    }
    
    private static AnimationClip LoadClip(string fbxPath, string clipName)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        
        foreach (Object asset in assets)
        {
            if (asset is AnimationClip clip)
            {
                // Skip preview clips
                if (clip.name.Contains("__preview__"))
                    continue;
                
                // Check if this is the clip we're looking for
                if (clip.name == clipName)
                {
                    Debug.Log($"✓ Loaded '{clipName}' from {fbxPath}");
                    return clip;
                }
            }
        }
        
        Debug.LogError($"✗ Could not find clip '{clipName}' in {fbxPath}");
        return null;
    }
}
