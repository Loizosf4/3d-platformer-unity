using UnityEngine;
using Cinemachine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMotorCC : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInputReader input;
    [Tooltip("Usually the Main Camera transform.")]
    [SerializeField] private Transform cameraTransform;
    [Tooltip("Optional: assign the player's CameraTarget for Cinemachine follow/look.")]
    [SerializeField] private Transform cameraTarget;

    [Header("Movement")]
    [SerializeField] private float maxMoveSpeed = 7f;
    [SerializeField] private float acceleration = 25f;
    [SerializeField] private float deceleration = 30f;
    [SerializeField] private float airControlMultiplier = 0.6f;
    [SerializeField] private float rotationSpeed = 12f;

    [Header("Gravity & Jump")]
    [Header("Double Jump (Unlockable)")]
    [SerializeField] private bool doubleJumpUnlocked = false;

    [Tooltip("How many extra jumps in the air (1 = double jump).")]
    [SerializeField] private int extraJumpsAllowed = 1;

    private int _extraJumpsRemaining;

    [SerializeField] private float gravity = -25f;
    [SerializeField] private float jumpHeight = 1.6f;

    [Header("Dash (Unlockable)")]
    [SerializeField] private bool dashUnlocked = false;

    [Tooltip("Dash speed in meters/second.")]
    [SerializeField] private float dashSpeed = 16f;

    [Tooltip("How long the dash lasts (seconds).")]
    [SerializeField] private float dashDuration = 0.18f;

    [Tooltip("Cooldown after dash ends (seconds).")]
    [SerializeField] private float dashCooldown = 0.6f;

    [Tooltip("If true, dash direction uses input. If no input, dashes forward.")]
    [SerializeField] private bool dashUsesMoveInput = true;

    [Tooltip("If true, player keeps current vertical velocity during dash. If false, vertical is zeroed while dashing.")]
    [SerializeField] private bool dashPreserveVerticalVelocity = false;

    private bool _isDashing;
    private float _dashTimer;
    private float _dashCooldownTimer;
    private Vector3 _dashDirection;

    [Header("Wall Jump (Unlockable)")]
    [SerializeField] private bool wallJumpUnlocked = false;

    [Tooltip("Which layers count as walls for wall jumping.")]
    [SerializeField] private LayerMask wallLayers;

    [Tooltip("How far to check for a wall from the player's center.")]
    [SerializeField] private float wallCheckDistance = 0.6f;

    [Tooltip("Vertical offset for the wall check (relative to player position).")]
    [SerializeField] private float wallCheckHeightOffset = 0.9f;

    [Tooltip("How long after leaving a wall you can still wall jump.")]
    [SerializeField] private float wallCoyoteTime = 0.12f;

    [Tooltip("Horizontal push away from the wall.")]
    [SerializeField] private float wallJumpHorizontalForce = 8f;

    [Tooltip("Vertical jump velocity added when wall jumping.")]
    [SerializeField] private float wallJumpUpwardVelocity = 9f;

    [Tooltip("Seconds to lock movement input after wall jump (helps control).")]
    [SerializeField] private float wallJumpInputLockTime = 0.10f;

    [Tooltip("If true, wall jump direction is away from wall normal. If false, uses player forward + up.")]
    [SerializeField] private bool wallJumpUsesWallNormal = true;

    private float _wallCoyoteTimer;
    private bool _isTouchingWall;
    private Vector3 _lastWallNormal;

    // Mario-style rule: only one wall jump per wall contact
    private bool _wallJumpConsumedThisContact;
    private float _wallJumpInputLockTimer;
    private bool _wasTouchingWall;
    private Collider _currentWallCollider;

    [Header("Jump Feel")]
    [Tooltip("How long after leaving ground you can still jump.")]
    [SerializeField] private float coyoteTime = 0.12f;

    [Tooltip("How long before landing a jump press is buffered.")]
    [SerializeField] private float jumpBufferTime = 0.12f;

    [Tooltip("If true, releasing jump early reduces jump height.")]
    [SerializeField] private bool variableJumpHeight = true;

    [Tooltip("Extra gravity multiplier when jump is released early and player is moving up.")]
    [SerializeField] private float jumpCutGravityMultiplier = 2.2f;

    [Header("Grounding")]
    [Tooltip("Small downward force to keep controller grounded on slopes.")]
    [SerializeField] private float groundedStickForce = -2f;

    private CharacterController _cc;

    private Vector3 _velocity;          // includes vertical velocity in y
    private Vector3 _planarVelocity;    // xz movement we smooth
    private float _coyoteTimer;
    private float _jumpBufferTimer;

    // External modifiers (platforms, powerups)
    private float _speedMultiplier = 1f;
    private float _speedMultiplierTimer = 0f;

    private float _extraUpwardVelocity = 0f; // used for magnet lift this frame
    
    // Platform movement support
    private Vector3 _platformMovement = Vector3.zero;
    
    // Ice platform support
    private float _iceEffect = 0f; // 0 = normal, 1 = full ice
    private float _iceEffectTimer = 0f;

    // Gravity override (used by volumes)
    private bool _gravityOverrideActive = false;
    private float _gravityOverrideValue = -25f; // should be negative for "normal down" gravity
    private float CurrentGravity => _gravityOverrideActive ? _gravityOverrideValue : gravity;

    private float _externalControlLockTimer = 0f;

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();

        if (input == null)
            input = GetComponent<PlayerInputReader>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (cameraTarget == null)
        {
            var ct = transform.Find("CameraTarget");
            if (ct != null) cameraTarget = ct;
        }
    }

    private void Start()
    {
        AutoHookCinemachine();
    }

    private void Update()
    {
        if (input == null)
        {
            Debug.LogError($"{nameof(PlayerMotorCC)} on {name}: Missing PlayerInputReader reference.");
            return;
        }

        // Cooldown timer
        if (_dashCooldownTimer > 0f)
            _dashCooldownTimer -= Time.deltaTime;

        if (_externalControlLockTimer > 0f)
            _externalControlLockTimer -= Time.deltaTime;


        // If currently dashing, we override normal movement/jump/gravity
        if (_isDashing)
        {
            UpdateDash();
            return;
        }

        UpdateTimers();
        UpdateWallCheck();
        TryStartDash();     // dash check happens before movement/jump
        HandleMovement();
        HandleJump();
        ApplyGravityAndMove();
    }


    private void UpdateTimers()
    {
        // Grounded checks
        if (_cc.isGrounded)
        {
            _coyoteTimer = coyoteTime;

            // Reset extra jumps when grounded
            _extraJumpsRemaining = doubleJumpUnlocked ? Mathf.Max(0, extraJumpsAllowed) : 0;

            // Stick to ground ONLY when gravity pulls DOWN.
            // When gravity is reversed (positive), do NOT clamp to groundedStickForce.
            if (CurrentGravity < 0f && _velocity.y < 0f)
                _velocity.y = groundedStickForce;

        }
        else
        {
            _coyoteTimer -= Time.deltaTime;
        }


        // Jump buffer
        if (input.JumpPressedThisFrame)
            _jumpBufferTimer = jumpBufferTime;
        else
            _jumpBufferTimer -= Time.deltaTime;

        if (_speedMultiplierTimer > 0f)
        {
            _speedMultiplierTimer -= Time.deltaTime;
            if (_speedMultiplierTimer <= 0f)
                _speedMultiplier = 1f;
        }
        
        // Ice effect timer
        if (_iceEffectTimer > 0f)
        {
            _iceEffectTimer -= Time.deltaTime;
            if (_iceEffectTimer <= 0f)
                _iceEffect = 0f;
        }
    }

    private void HandleMovement()
    {
        if (_externalControlLockTimer > 0f)
        {
            // Still rotate to movement direction if we have velocity
            Vector3 faceDirLocked = new Vector3(_planarVelocity.x, 0f, _planarVelocity.z);
            if (faceDirLocked.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(faceDirLocked, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            }

            return;
        }


        if (_wallJumpInputLockTimer > 0f)
        {
            _wallJumpInputLockTimer -= Time.deltaTime;

            // Still rotate to movement direction if we have velocity
            Vector3 faceDirLocked = new Vector3(_planarVelocity.x, 0f, _planarVelocity.z);
            if (faceDirLocked.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(faceDirLocked, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            }

            return;
        }

        Vector2 moveInput = input.Move;
        Vector3 desiredDir = new Vector3(moveInput.x, 0f, moveInput.y);

        if (cameraTransform != null)
        {
            // Camera-centric: project camera forward/right onto XZ plane
            Vector3 camForward = cameraTransform.forward;
            Vector3 camRight = cameraTransform.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            desiredDir = camRight * moveInput.x + camForward * moveInput.y;
        }

        float desiredMagnitude = Mathf.Clamp01(moveInput.magnitude);
        float currentSpeed = maxMoveSpeed * _speedMultiplier;
        Vector3 desiredVelocity = desiredDir.normalized * (currentSpeed * desiredMagnitude);

        // Accel/decel - modified by ice effect
        float baseAccel = (_planarVelocity.magnitude < desiredVelocity.magnitude) ? acceleration : deceleration;
        float baseDecel = deceleration;
        
        // Ice reduces acceleration and deceleration dramatically
        float iceMultiplier = 1f - (_iceEffect * 0.9f); // Ice reduces control by up to 90%
        float accel = baseAccel * iceMultiplier;
        
        // On ice, deceleration is much lower (player keeps sliding)
        if (_iceEffect > 0f)
        {
            float iceDecel = baseDecel * (0.1f + (1f - _iceEffect) * 0.9f); // Ice reduces decel by up to 90%
            if (_planarVelocity.magnitude >= desiredVelocity.magnitude)
            {
                accel = iceDecel;
            }
        }

        // Air control
        if (!_cc.isGrounded)
            accel *= airControlMultiplier;

        _planarVelocity = Vector3.MoveTowards(_planarVelocity, desiredVelocity, accel * Time.deltaTime);

        // Face move direction (if moving)
        Vector3 faceDir = new Vector3(_planarVelocity.x, 0f, _planarVelocity.z);
        if (faceDir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(faceDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }
    }

    private void HandleJump()
    {
        bool wantsJump = _jumpBufferTimer > 0f;

        // Ground / coyote jump
        bool canCoyoteJump = _coyoteTimer > 0f;

        // Wall jump
        bool canWallJump =
            wallJumpUnlocked &&
            !_cc.isGrounded &&
            _wallCoyoteTimer > 0f &&
            !_wallJumpConsumedThisContact;

        // Air jump (double jump)
        bool canAirJump = !_cc.isGrounded && _extraJumpsRemaining > 0;

        if (wantsJump)
        {
            if (canCoyoteJump)
            {
                DoJump();
                _jumpBufferTimer = 0f;
                _coyoteTimer = 0f;
            }
            else if (canWallJump)
            {
                DoWallJump();
                _jumpBufferTimer = 0f;

                // Consume for this wall contact (Mario rule)
                _wallJumpConsumedThisContact = true;
                _wallCoyoteTimer = 0f;

                // Reset double jump after wall jump
                _extraJumpsRemaining = doubleJumpUnlocked ? Mathf.Max(0, extraJumpsAllowed) : 0;
            }
            else if (canAirJump)
            {
                DoJump();
                _jumpBufferTimer = 0f;
                _extraJumpsRemaining--;
            }
        }

        // Variable jump height (jump cut)
        if (variableJumpHeight && !input.JumpHeld)
        {
            // If gravity is down (negative), cut when moving up (+y).
            // If gravity is up (positive), cut when moving down (-y).
            bool movingAgainstGravity = (CurrentGravity < 0f && _velocity.y > 0f) ||
                                       (CurrentGravity > 0f && _velocity.y < 0f);

            if (movingAgainstGravity)
                _velocity.y += CurrentGravity * (jumpCutGravityMultiplier - 1f) * Time.deltaTime;
        }

    }


    private void DoJump()
    {
        // v = sqrt(2 * h * -g)
        float jumpVelocity = Mathf.Sqrt(2f * jumpHeight * -gravity);
        
        // Only replace velocity if it would increase the upward speed
        // This preserves trampoline momentum or other external upward forces
        if (jumpVelocity > _velocity.y)
        {
            _velocity.y = jumpVelocity;
        }
    }


    private void DoWallJump()
    {
        Vector3 away = wallJumpUsesWallNormal ? _lastWallNormal : -transform.forward;
        away.y = 0f;
        away.Normalize();

        // Horizontal push away from the wall
        _planarVelocity = away * wallJumpHorizontalForce;

        // Vertical boost
        _velocity.y = wallJumpUpwardVelocity;

        // Lock movement input briefly for better feel
        _wallJumpInputLockTimer = wallJumpInputLockTime;
    }



    private void ApplyGravityAndMove()
    {
        // Apply gravity
        float g = _gravityOverrideActive ? _gravityOverrideValue : gravity;
        _velocity.y += CurrentGravity * Time.deltaTime;


        // Add any external upward velocity (magnet platforms)
        _velocity.y += _extraUpwardVelocity;

        Vector3 totalMove = _planarVelocity;
        totalMove.y = _velocity.y;

        // CharacterController.Move expects displacement, not velocity
        _cc.Move(totalMove * Time.deltaTime);
        
        // Apply platform movement if any
        if (_platformMovement.sqrMagnitude > 0.0001f)
        {
            _cc.Move(_platformMovement);
            _platformMovement = Vector3.zero; // Reset for next frame
        }

        _extraUpwardVelocity = 0f;

    }

    private void AutoHookCinemachine()
    {
        if (cameraTarget == null) return;

        // Works for both VirtualCamera and FreeLook
        var freeLook = FindObjectOfType<CinemachineFreeLook>();
        if (freeLook != null)
        {
            freeLook.Follow = cameraTarget;
            freeLook.LookAt = cameraTarget;
            return;
        }

        var vcam = FindObjectOfType<CinemachineVirtualCamera>();
        if (vcam != null)
        {
            vcam.Follow = cameraTarget;
            vcam.LookAt = cameraTarget;
        }
    }

    private void TryStartDash()
    {
        if (!dashUnlocked) return;
        if (!input.DashPressedThisFrame) return;
        if (_dashCooldownTimer > 0f) return;

        // Start dash
        _isDashing = true;
        _dashTimer = dashDuration;
        _dashCooldownTimer = dashCooldown;

        // Determine dash direction
        Vector3 dir = transform.forward;

        if (dashUsesMoveInput)
        {
            Vector2 moveInput = input.Move;
            Vector3 desiredDir = new Vector3(moveInput.x, 0f, moveInput.y);

            if (cameraTransform != null)
            {
                Vector3 camForward = cameraTransform.forward;
                Vector3 camRight = cameraTransform.right;
                camForward.y = 0f;
                camRight.y = 0f;
                camForward.Normalize();
                camRight.Normalize();

                desiredDir = (camRight * desiredDir.x + camForward * desiredDir.z);
            }

            if (desiredDir.sqrMagnitude > 0.0001f)
                dir = desiredDir.normalized;
        }

        _dashDirection = dir;

        // Optional: remove vertical motion during dash
        if (!dashPreserveVerticalVelocity)
            _velocity.y = 0f;
    }

    private void UpdateDash()
    {
        _dashTimer -= Time.deltaTime;

        // Move during dash (ignore normal accel/decel & usually ignore gravity)
        Vector3 dashVel = _dashDirection * dashSpeed;

        if (dashPreserveVerticalVelocity)
        {
            // Still apply gravity if preserving vertical motion (optional feel)
            _velocity.y += gravity * Time.deltaTime;
            dashVel.y = _velocity.y;
        }
        else
        {
            dashVel.y = 0f;
        }

        _cc.Move(dashVel * Time.deltaTime);

        if (_dashTimer <= 0f)
        {
            _isDashing = false;
            
            // If on ice, preserve dash momentum as planar velocity
            if (_iceEffect > 0f)
            {
                _planarVelocity = new Vector3(dashVel.x, 0f, dashVel.z);
            }

            // Small grounded stick if we ended dash on ground
            if (_cc.isGrounded && _velocity.y < 0f)
                _velocity.y = groundedStickForce;
        }
    }

    private void UpdateWallCheck()
    {
        if (!wallJumpUnlocked)
        {
            _wasTouchingWall = false;
            _currentWallCollider = null;
            _isTouchingWall = false;
            _wallCoyoteTimer -= Time.deltaTime;
            return;
        }

        bool foundWall = false;
        Vector3 bestNormal = Vector3.zero;
        Collider bestCollider = null;

        Vector3 origin = transform.position + Vector3.up * wallCheckHeightOffset;

        // Check in 4 directions around the player
        Vector3[] dirs =
        {
        transform.forward,
        -transform.forward,
        transform.right,
        -transform.right
    };

        float bestScore = float.NegativeInfinity;

        for (int i = 0; i < dirs.Length; i++)
        {
            if (Physics.Raycast(origin, dirs[i], out RaycastHit hit, wallCheckDistance, wallLayers, QueryTriggerInteraction.Ignore))
            {
                foundWall = true;

                // Score to pick the "best" wall hit (stable choice)
                float score = -hit.distance; // prefer closer
                if (score > bestScore)
                {
                    bestScore = score;
                    bestNormal = hit.normal;
                    bestCollider = hit.collider;
                }
            }
        }

        _isTouchingWall = foundWall;

        if (_isTouchingWall)
        {
            // Determine if this is a NEW wall contact (enter) or a DIFFERENT wall
            bool enteredWallThisFrame = !_wasTouchingWall;
            bool switchedToDifferentWall = (_currentWallCollider != null && bestCollider != _currentWallCollider);

            _lastWallNormal = bestNormal != Vector3.zero ? bestNormal : _lastWallNormal;
            _wallCoyoteTimer = wallCoyoteTime;

            if (enteredWallThisFrame || switchedToDifferentWall)
            {
                // Only reset "consumed" when you newly contact a wall (or change walls)
                _wallJumpConsumedThisContact = false;
            }

            _currentWallCollider = bestCollider;
            _wasTouchingWall = true;
        }
        else
        {
            // Not touching a wall this frame
            _wallCoyoteTimer -= Time.deltaTime;

            // If we fully left the wall, clear current collider so next touch counts as a new contact
            _wasTouchingWall = false;
            _currentWallCollider = null;
        }
    }

    public void ApplySpeedMultiplier(float multiplier, float duration)
    {
        _speedMultiplier = Mathf.Max(0.1f, multiplier);
        _speedMultiplierTimer = Mathf.Max(0f, duration);
    }

    public void AddUpwardVelocityThisFrame(float upwardVel)
    {
        _extraUpwardVelocity += upwardVel;
    }
    
    public void AddDirectionalForce(Vector3 force)
    {
        // Add horizontal force to planar velocity
        Vector3 horizontalForce = new Vector3(force.x, 0f, force.z);
        _planarVelocity += horizontalForce;
        
        // Add vertical force separately
        if (force.y != 0f)
        {
            _extraUpwardVelocity += force.y;
        }
    }
    
    public void ApplyPlatformMovement(Vector3 movement)
    {
        _platformMovement = movement;
    }
    
    public void ApplyIceEffect(float slipperiness, float duration, float speedBoost = 1.0f)
    {
        _iceEffect = Mathf.Clamp01(slipperiness);
        _iceEffectTimer = duration;
        
        // Apply speed boost while on ice
        if (speedBoost > 1.0f)
        {
            _speedMultiplier = speedBoost;
            _speedMultiplierTimer = duration;
        }
    }
    public void SetGravityOverride(float gravityValue)
    {
        _gravityOverrideActive = true;
        _gravityOverrideValue = gravityValue;
    }

    public void ClearGravityOverride()
    {
        _gravityOverrideActive = false;
    }

    public void ResetAfterGravityVolumeExit(bool resetPlanar = false)
    {
        _gravityOverrideActive = false;   // same as ClearGravityOverride()
        _velocity.y = 0f;
        _extraUpwardVelocity = 0f;

        if (resetPlanar)
            _planarVelocity = Vector3.zero;
    }


    public void ResetVerticalVelocity()
    {
        _velocity.y = 0f;

        // Optional: also reset these if you want ZERO launch/momentum at all
        _extraUpwardVelocity = 0f;
        // _planarVelocity = Vector3.zero;
    }

    public void UnlockDoubleJump() => doubleJumpUnlocked = true;
    public void UnlockDash() => dashUnlocked = true;
    public void UnlockWallJump() => wallJumpUnlocked = true;

    public void ApplyExternalImpulse(Vector3 impulse, float controlLockTime)
    {
        AddDirectionalForce(impulse);
        _externalControlLockTimer = Mathf.Max(_externalControlLockTimer, Mathf.Max(0f, controlLockTime));
    }

    public void ResetMovementState()
    {
        _planarVelocity = Vector3.zero;
        _velocity = Vector3.zero;

        _isDashing = false;
        _dashTimer = 0f;

        _externalControlLockTimer = 0f;
        _wallJumpInputLockTimer = 0f;

        _platformMovement = Vector3.zero;

        // Clear wall timers (prevents instant wall jump after respawn)
        _wallCoyoteTimer = 0f;
        _isTouchingWall = false;
        _wallJumpConsumedThisContact = false;
        _wasTouchingWall = false;
        _currentWallCollider = null;

        // Clear buffered jump
        _jumpBufferTimer = 0f;
        _coyoteTimer = 0f;
    }

}

