using UnityEngine;

/// <summary>
/// Bridges the PlayerMotorCC with the Animator component to drive character animations.
/// Attach this script to the character model child object that has the Animator component.
/// All animation parameters are exposed in the Inspector for easy tweaking without rebuilding the Animator Controller.
/// </summary>
[RequireComponent(typeof(Animator))]
public class PlayerAnimator : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the PlayerMotorCC script. Will auto-find in parent if not assigned.")]
    [SerializeField] private PlayerMotorCC motor;
    
    [Tooltip("Reference to the PlayerInputReader script. Will auto-find in parent if not assigned.")]
    [SerializeField] private PlayerInputReader inputReader;
    
    [Tooltip("Reference to the CharacterController. Will auto-find in parent if not assigned.")]
    [SerializeField] private CharacterController characterController;
    
    [Header("=== RUNNING DETECTION ===")]
    [Tooltip("Minimum input magnitude to consider as 'pressing movement keys'. Set to small value like 0.1 to filter stick drift.")]
    [Range(0f, 1f)]
    [SerializeField] private float inputDeadzone = 0.1f;
    
    [Header("=== ANIMATION THRESHOLDS ===")]
    [Tooltip("(Legacy) Start running animation when horizontal speed >= this value. Only used if inputReader is missing.")]
    [SerializeField] private float runThreshold = 0.1f;
    
    [Tooltip("(Legacy) Return to idle when horizontal speed < this value. Only used if inputReader is missing.")]
    [SerializeField] private float idleThreshold = 0.1f;
    
    [Tooltip("Start falling animation when vertical velocity < this value (should be negative).")]
    [SerializeField] private float fallVelocityThreshold = -0.1f;
    
    [Tooltip("Consider jumping when vertical velocity > this value.")]
    [SerializeField] private float jumpVelocityThreshold = 0.1f;
    
    [Header("=== TRANSITION DURATIONS ===")]
    [Tooltip("How long to blend from Idle to Run (seconds). Lower = snappier.")]
    [Range(0f, 0.5f)]
    [SerializeField] private float idleToRunDuration = 0.05f;
    
    [Tooltip("How long to blend from Run to Idle (seconds). Lower = snappier.")]
    [Range(0f, 0.5f)]
    [SerializeField] private float runToIdleDuration = 0.05f;
    
    [Tooltip("How long to blend into Jump animation (seconds).")]
    [Range(0f, 0.3f)]
    [SerializeField] private float jumpTransitionDuration = 0.02f;
    
    [Tooltip("How long to blend into Fall animation (seconds).")]
    [Range(0f, 0.3f)]
    [SerializeField] private float fallTransitionDuration = 0.05f;
    
    [Tooltip("How long to blend into Land animation (seconds).")]
    [Range(0f, 0.3f)]
    [SerializeField] private float landTransitionDuration = 0.02f;
    
    [Header("=== SMOOTHING ===")]
    [Tooltip("How quickly the speed parameter smooths to target value. Set to 0 for instant response.")]
    [Range(0f, 0.5f)]
    [SerializeField] private float speedSmoothTime = 0.05f;
    
    [Header("=== GROUND DETECTION ===")]
    [Tooltip("Minimum time in air before landing animation plays (prevents flickering).")]
    [SerializeField] private float minAirTimeForLand = 0.1f;
    
    [Tooltip("Skip the landing animation and go straight to Idle/Run if player is holding movement input.")]
    [SerializeField] private bool skipLandIfMoving = true;
    
    [Header("=== MOVING PLATFORM TOLERANCE ===")]
    [Tooltip("Vertical velocity tolerance for 'grounded' state. Helps with moving platforms.")]
    [SerializeField] private float groundedVelocityTolerance = 2f;
    
    [Tooltip("Grace period after becoming grounded before allowing fall animation (prevents flickering on platforms).")]
    [SerializeField] private float groundedGracePeriod = 0.1f;
    
    [Header("=== DEBUG ===")]
    [SerializeField] private bool showDebugInfo = false;
    
    private Animator _animator;
    private float _smoothedSpeed;
    private float _speedSmoothVelocity;
    
    // State tracking
    private AnimState _currentState = AnimState.Idle;
    private bool _wasGrounded = true;
    private float _airborneTimer;
    private float _groundedTimer;  // Time since becoming grounded
    private bool _isRunning;       // Cached for use in UpdateAnimationState
    private float _jumpLockTimer;  // Prevents immediate override of jump animation
    
    // Animation state hashes
    private static readonly int IdleState = Animator.StringToHash("Idle");
    private static readonly int RunState = Animator.StringToHash("Run");
    private static readonly int JumpState = Animator.StringToHash("Jump");
    private static readonly int FallState = Animator.StringToHash("Fall");
    private static readonly int LandState = Animator.StringToHash("Land");
    
    // Animator parameter hashes (still used for blend trees if needed)
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int VerticalVelocityHash = Animator.StringToHash("VerticalVelocity");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int IsRunningHash = Animator.StringToHash("IsRunning");
    private static readonly int IsJumpingHash = Animator.StringToHash("IsJumping");
    private static readonly int IsDashingHash = Animator.StringToHash("IsDashing");
    private static readonly int IsFallingHash = Animator.StringToHash("IsFalling");
    private static readonly int IsWallSlidingHash = Animator.StringToHash("IsWallSliding");
    private static readonly int JumpTrigger = Animator.StringToHash("Jump");  // Trigger parameter for jump
    
    private enum AnimState
    {
        Idle,
        Run,
        Jump,
        Fall,
        Land
    }
    
    private void Awake()
    {
        _animator = GetComponent<Animator>();
        
        if (motor == null)
            motor = GetComponentInParent<PlayerMotorCC>();
        
        if (inputReader == null)
            inputReader = GetComponentInParent<PlayerInputReader>();
        
        if (characterController == null)
            characterController = GetComponentInParent<CharacterController>();
            
        if (motor == null)
            Debug.LogError($"{nameof(PlayerAnimator)} on {name}: Could not find PlayerMotorCC in parent!");
            
        if (characterController == null)
            Debug.LogError($"{nameof(PlayerAnimator)} on {name}: Could not find CharacterController in parent!");
        
        if (inputReader == null)
            Debug.LogWarning($"{nameof(PlayerAnimator)} on {name}: Could not find PlayerInputReader in parent. Falling back to speed-based running detection.");
    }
    
    private void OnEnable()
    {
        if (motor != null)
        {
            motor.OnJump += HandleJump;
            motor.OnDash += HandleDash;
            motor.OnLand += HandleLand;
        }
    }
    
    private void OnDisable()
    {
        if (motor != null)
        {
            motor.OnJump -= HandleJump;
            motor.OnDash -= HandleDash;
            motor.OnLand -= HandleLand;
        }
    }
    
    private void Update()
    {
        if (motor == null || characterController == null || _animator == null)
            return;
            
        UpdateAnimatorParameters();
        UpdateAnimationState();
    }
    
    private void UpdateAnimatorParameters()
    {
        float horizontalSpeed = motor.HorizontalSpeed;
        float verticalVelocity = motor.VerticalVelocity;
        bool isGrounded = motor.IsGrounded;
        bool isDashing = motor.IsDashing;
        bool isTouchingWall = motor.IsTouchingWall;
        
        // Smooth speed (or use instant if smoothTime is 0)
        if (speedSmoothTime > 0)
        {
            _smoothedSpeed = Mathf.SmoothDamp(_smoothedSpeed, horizontalSpeed, ref _speedSmoothVelocity, speedSmoothTime);
        }
        else
        {
            _smoothedSpeed = horizontalSpeed;
        }
        
        // Track grounded time (helps with moving platforms)
        if (isGrounded)
        {
            _groundedTimer += Time.deltaTime;
        }
        else
        {
            _groundedTimer = 0f;
        }
        
        // Determine if running: TRUE when player is pressing movement keys
        // Works both grounded and in air (for responsive transitions on landing)
        bool hasMovementInput = false;
        if (inputReader != null)
        {
            Vector2 moveInput = inputReader.Move;
            hasMovementInput = moveInput.sqrMagnitude > inputDeadzone * inputDeadzone;
        }
        else
        {
            // Fallback to speed-based detection if no input reader
            hasMovementInput = _smoothedSpeed >= runThreshold;
        }
        
        // Cache isRunning for use in UpdateAnimationState
        _isRunning = hasMovementInput;
        
        // For the animator parameter, we still want grounded check
        bool isRunningParam = hasMovementInput && isGrounded;
        
        // Determine fall/jump states with tolerance for moving platforms
        // Only consider falling if we've been ungrounded for a bit AND velocity is significantly negative
        bool isFalling = !isGrounded && 
                         verticalVelocity < fallVelocityThreshold && 
                         !isDashing &&
                         _airborneTimer > groundedGracePeriod;
        
        bool isJumping = !isGrounded && verticalVelocity > jumpVelocityThreshold && !isDashing;
        
        // Set animator parameters
        _animator.SetFloat(SpeedHash, _smoothedSpeed);
        _animator.SetFloat(VerticalVelocityHash, verticalVelocity);
        _animator.SetBool(IsGroundedHash, isGrounded);
        _animator.SetBool(IsRunningHash, isRunningParam);
        _animator.SetBool(IsFallingHash, isFalling);
        _animator.SetBool(IsJumpingHash, isJumping);
        _animator.SetBool(IsDashingHash, isDashing);
        _animator.SetBool(IsWallSlidingHash, isTouchingWall && !isGrounded);
        
        // Track airborne time
        if (!isGrounded)
        {
            _airborneTimer += Time.deltaTime;
        }
        
        // Detect landing - but skip land animation if player is holding movement and skipLandIfMoving is true
        // Also don't trigger landing if we're in a locked jump state (e.g., just hit a trampoline)
        if (isGrounded && !_wasGrounded && _airborneTimer >= minAirTimeForLand && _jumpLockTimer <= 0f)
        {
            if (skipLandIfMoving && hasMovementInput)
            {
                // Skip land, go straight to run
                TransitionTo(AnimState.Run, idleToRunDuration);
            }
            else
            {
                TransitionTo(AnimState.Land, landTransitionDuration);
            }
        }
        
        if (isGrounded)
        {
            _airborneTimer = 0f;
        }
        
        _wasGrounded = isGrounded;
    }
    
    private void UpdateAnimationState()
    {
        bool isGrounded = motor.IsGrounded;
        float verticalVelocity = motor.VerticalVelocity;
        bool isDashing = motor.IsDashing;
        
        // Don't change state while dashing (could add dash animation here)
        if (isDashing)
            return;
        
        // Decrement jump lock timer
        if (_jumpLockTimer > 0f)
        {
            _jumpLockTimer -= Time.deltaTime;
        }
        
        // If we're in jump state and locked, don't allow ground states to override
        if (_currentState == AnimState.Jump && _jumpLockTimer > 0f)
        {
            // Only allow transition to fall if velocity is negative
            if (verticalVelocity < 0f)
            {
                TransitionTo(AnimState.Fall, fallTransitionDuration);
            }
            return;
        }
        
        AnimState targetState = _currentState;
        float transitionDuration = 0.1f;
        
        if (isGrounded)
        {
            // Ground states: Idle or Run
            if (_currentState == AnimState.Land)
            {
                // Check if we should exit land state early
                AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
                bool landFinished = stateInfo.normalizedTime >= 0.5f || !stateInfo.IsName("Land");
                
                // Exit land immediately if player is pressing movement, or when animation finishes
                if (_isRunning || landFinished)
                {
                    if (_isRunning)
                    {
                        targetState = AnimState.Run;
                        transitionDuration = idleToRunDuration;
                    }
                    else
                    {
                        targetState = AnimState.Idle;
                        transitionDuration = runToIdleDuration;
                    }
                }
            }
            else if (_currentState == AnimState.Idle)
            {
                // Use input-based _isRunning instead of speed
                if (_isRunning)
                {
                    targetState = AnimState.Run;
                    transitionDuration = idleToRunDuration;
                }
            }
            else if (_currentState == AnimState.Run)
            {
                // Use input-based _isRunning instead of speed
                if (!_isRunning)
                {
                    targetState = AnimState.Idle;
                    transitionDuration = runToIdleDuration;
                }
            }
            else if (_currentState == AnimState.Jump || _currentState == AnimState.Fall)
            {
                // Just landed - this is handled in UpdateAnimatorParameters
                // But as a fallback, transition to appropriate state
                if (_isRunning)
                {
                    targetState = AnimState.Run;
                    transitionDuration = idleToRunDuration;
                }
                else
                {
                    targetState = AnimState.Idle;
                    transitionDuration = runToIdleDuration;
                }
            }
        }
        else
        {
            // Air states: Jump or Fall
            // Only transition if we've been airborne long enough (prevents flickering on moving platforms)
            if (_airborneTimer > groundedGracePeriod)
            {
                if (_currentState != AnimState.Jump && _currentState != AnimState.Fall)
                {
                    // Just left ground
                    if (verticalVelocity > jumpVelocityThreshold)
                    {
                        targetState = AnimState.Jump;
                        transitionDuration = jumpTransitionDuration;
                    }
                    else if (verticalVelocity < fallVelocityThreshold)
                    {
                        targetState = AnimState.Fall;
                        transitionDuration = fallTransitionDuration;
                    }
                }
                else if (_currentState == AnimState.Jump && verticalVelocity < 0)
                {
                    // Transitioning from jump to fall
                    targetState = AnimState.Fall;
                    transitionDuration = fallTransitionDuration;
                }
            }
        }
        
        // Apply state change
        if (targetState != _currentState)
        {
            TransitionTo(targetState, transitionDuration);
        }
    }
    
    private void TransitionTo(AnimState newState, float duration)
    {
        // Don't transition to the same state (prevents animation restart/glitching)
        if (newState == _currentState)
            return;
        
        // Don't transition away from Jump if locked (prevents trampoline glitching)
        if (_currentState == AnimState.Jump && _jumpLockTimer > 0f && newState != AnimState.Fall)
            return;
            
        int stateHash = GetStateHash(newState);
        
        if (showDebugInfo)
        {
            Debug.Log($"Animation: {_currentState} -> {newState} (duration: {duration}s)");
        }
        
        _animator.CrossFadeInFixedTime(stateHash, duration);
        _currentState = newState;
    }
    
    private int GetStateHash(AnimState state)
    {
        switch (state)
        {
            case AnimState.Idle: return IdleState;
            case AnimState.Run: return RunState;
            case AnimState.Jump: return JumpState;
            case AnimState.Fall: return FallState;
            case AnimState.Land: return LandState;
            default: return IdleState;
        }
    }
    
    // Event handlers called by PlayerMotorCC
    private void HandleJump()
    {
        // If already in jump state with active lock, just refresh the lock timer
        // This prevents animation restart/glitching on trampolines
        if (_currentState == AnimState.Jump)
        {
            _jumpLockTimer = 0.3f;
            _airborneTimer = 0f;
            return;
        }
        
        // Force transition to jump state using CrossFade
        // The _jumpLockTimer prevents UpdateAnimationState from overriding this
        int stateHash = GetStateHash(AnimState.Jump);
        _animator.CrossFadeInFixedTime(stateHash, jumpTransitionDuration);
        _currentState = AnimState.Jump;
        _jumpLockTimer = 0.3f;  // Lock to prevent state machine from overriding
        _airborneTimer = 0f;    // Reset airborne timer on jump
        
        if (showDebugInfo)
        {
            Debug.Log($"HandleJump called - transitioning to Jump state (hash: {stateHash})");
        }
    }
    
    private void HandleDash()
    {
        // Could add dash animation state here
        if (showDebugInfo)
            Debug.Log("Dash triggered");
    }
    
    private void HandleLand()
    {
        // Landing is handled in UpdateAnimatorParameters based on airborne time
    }
    
    /// <summary>
    /// Force a specific animation state (useful for external triggers)
    /// </summary>
    public void ForceState(string stateName, float transitionDuration = 0.1f)
    {
        _animator.CrossFadeInFixedTime(stateName, transitionDuration);
    }
}
