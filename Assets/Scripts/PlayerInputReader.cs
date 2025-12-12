using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputReader : MonoBehaviour
{
    [Header("Input Actions")]
    [Tooltip("Drag your IA_Player input actions asset here.")]
    [SerializeField] private InputActionAsset actions;

    [Tooltip("Action Map name inside the asset (e.g., Gameplay).")]
    [SerializeField] private string actionMapName = "Gameplay";

    [Tooltip("Action name for Move (Vector2).")]
    [SerializeField] private string moveActionName = "Move";

    [Tooltip("Action name for Jump (Button).")]
    [SerializeField] private string jumpActionName = "Jump";

    [Tooltip("Action name for Dash (Button).")]
    [SerializeField] private string dashActionName = "Dash";

    public Vector2 Move { get; private set; }

    public bool JumpPressedThisFrame { get; private set; }
    public bool JumpHeld { get; private set; }

    public bool DashPressedThisFrame { get; private set; }
    public bool DashHeld { get; private set; }

    private InputAction _moveAction;
    private InputAction _jumpAction;
    private InputAction _dashAction;

    private void OnEnable()
    {
        if (actions == null)
        {
            Debug.LogError($"{nameof(PlayerInputReader)} on {name}: No InputActionAsset assigned.");
            return;
        }

        var map = actions.FindActionMap(actionMapName, true);
        _moveAction = map.FindAction(moveActionName, true);
        _jumpAction = map.FindAction(jumpActionName, true);
        _dashAction = map.FindAction(dashActionName, true);

        _moveAction.Enable();
        _jumpAction.Enable();
        _dashAction.Enable();

        _jumpAction.started += OnJumpStarted;
        _jumpAction.canceled += OnJumpCanceled;

        _dashAction.started += OnDashStarted;
        _dashAction.canceled += OnDashCanceled;
    }

    private void OnDisable()
    {
        if (_jumpAction != null)
        {
            _jumpAction.started -= OnJumpStarted;
            _jumpAction.canceled -= OnJumpCanceled;
        }

        if (_dashAction != null)
        {
            _dashAction.started -= OnDashStarted;
            _dashAction.canceled -= OnDashCanceled;
        }
    }

    private void Update()
    {
        if (_moveAction != null)
            Move = _moveAction.ReadValue<Vector2>();
    }

    private void LateUpdate()
    {
        // Clear one-frame press flags here so they're valid for the whole Update of other scripts.
        JumpPressedThisFrame = false;
        DashPressedThisFrame = false;
    }

    private void OnJumpStarted(InputAction.CallbackContext ctx)
    {
        JumpPressedThisFrame = true;
        JumpHeld = true;
    }

    private void OnJumpCanceled(InputAction.CallbackContext ctx)
    {
        JumpHeld = false;
    }

    private void OnDashStarted(InputAction.CallbackContext ctx)
    {
        DashPressedThisFrame = true;
        DashHeld = true;
    }

    private void OnDashCanceled(InputAction.CallbackContext ctx)
    {
        DashHeld = false;
    }

    public void AssignActions(InputActionAsset newActions) => actions = newActions;
}
