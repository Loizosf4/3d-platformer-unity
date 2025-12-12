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

    public Vector2 Move { get; private set; }
    public bool JumpPressedThisFrame { get; private set; }
    public bool JumpHeld { get; private set; }

    private InputAction _moveAction;
    private InputAction _jumpAction;

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

        _moveAction.Enable();
        _jumpAction.Enable();

        _jumpAction.started += OnJumpStarted;
        _jumpAction.canceled += OnJumpCanceled;
    }

    private void OnDisable()
    {
        if (_jumpAction != null)
        {
            _jumpAction.started -= OnJumpStarted;
            _jumpAction.canceled -= OnJumpCanceled;
        }
    }

    private void Update()
    {
        JumpPressedThisFrame = false;
        if (_moveAction != null)
            Move = _moveAction.ReadValue<Vector2>();
    }

    private void LateUpdate()
    {
        // LateUpdate is a good place to clear one-frame flags, but we already reset in Update.
        // Keeping this here for clarity/extension.
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

    public void AssignActions(InputActionAsset newActions)
    {
        actions = newActions;
    }
}
