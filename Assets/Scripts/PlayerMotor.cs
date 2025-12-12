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

        UpdateTimers();
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

            // reset downward velocity so we "stick" to ground
            if (_velocity.y < 0f)
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
    }

    private void HandleMovement()
    {
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

            desiredDir = (camRight * desiredDir.x + camForward * desiredDir.z);
        }

        float desiredMagnitude = Mathf.Clamp01(desiredDir.magnitude);
        Vector3 desiredVelocity = desiredDir.normalized * (maxMoveSpeed * desiredMagnitude);

        // Accel/decel
        float accel = (_planarVelocity.magnitude < desiredVelocity.magnitude) ? acceleration : deceleration;

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
            else if (canAirJump)
            {
                DoJump();
                _jumpBufferTimer = 0f;
                _extraJumpsRemaining--;
            }
        }

        // Variable jump height (jump cut)
        if (variableJumpHeight && !input.JumpHeld && _velocity.y > 0f)
        {
            // Apply extra gravity while rising if jump released early
            _velocity.y += gravity * (jumpCutGravityMultiplier - 1f) * Time.deltaTime;
        }
    }

    private void DoJump()
    {
        // v = sqrt(2 * h * -g)
        float jumpVelocity = Mathf.Sqrt(2f * jumpHeight * -gravity);
        _velocity.y = jumpVelocity;
    }



    private void ApplyGravityAndMove()
    {
        // Apply gravity
        _velocity.y += gravity * Time.deltaTime;

        Vector3 totalMove = _planarVelocity;
        totalMove.y = _velocity.y;

        // CharacterController.Move expects displacement, not velocity
        _cc.Move(totalMove * Time.deltaTime);
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

}

