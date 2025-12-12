using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;

public class CinemachineDualLookInput : MonoBehaviour, AxisState.IInputAxisProvider
{
    [Header("Input Actions Asset")]
    [SerializeField] private InputActionAsset actions;
    [SerializeField] private string actionMapName = "Gameplay";
    [SerializeField] private string lookMouseActionName = "LookMouse";
    [SerializeField] private string lookStickActionName = "LookStick";

    [Header("Sensitivity (Inspector Now, Settings Later)")]
    [Tooltip("Mouse sensitivity multiplier (mouse delta). Typical range: 0.02 to 0.2")]
    [SerializeField] private float mouseSensitivity = 0.08f;

    [Tooltip("Stick sensitivity multiplier (right stick). Typical range: 0.5 to 3")]
    [SerializeField] private float stickSensitivity = 1.5f;

    [Tooltip("Invert horizontal look (X axis).")]
    [SerializeField] private bool invertX = false;

    [Tooltip("Invert vertical look (Y axis).")]
    [SerializeField] private bool invertY = false;

    [Header("Clamping")]
    [Tooltip("Clamp the final axis input to this range to keep it stable.")]
    [SerializeField] private float clamp = 1f;

    private InputAction _lookMouse;
    private InputAction _lookStick;

    private void OnEnable()
    {
        if (actions == null)
        {
            Debug.LogError($"{nameof(CinemachineDualLookInput)} on {name}: No InputActionAsset assigned.");
            return;
        }

        var map = actions.FindActionMap(actionMapName, true);
        _lookMouse = map.FindAction(lookMouseActionName, true);
        _lookStick = map.FindAction(lookStickActionName, true);

        _lookMouse.Enable();
        _lookStick.Enable();
    }

    private void OnDisable()
    {
        _lookMouse?.Disable();
        _lookStick?.Disable();
    }

    // axis 0 = X, axis 1 = Y, axis 2 = Z (unused here)
    public float GetAxisValue(int axis)
    {
        if (_lookMouse == null || _lookStick == null)
            return 0f;

        Vector2 mouseDelta = _lookMouse.ReadValue<Vector2>() * mouseSensitivity;
        Vector2 stick = _lookStick.ReadValue<Vector2>() * stickSensitivity;

        // Combine inputs
        Vector2 combined = mouseDelta + stick;

        float x = Mathf.Clamp(combined.x, -clamp, clamp);
        float y = Mathf.Clamp(combined.y, -clamp, clamp);

        if (invertX) x = -x;
        if (invertY) y = -y;

        switch (axis)
        {
            case 0: return x;        // X axis
            case 1: return y;        // Y axis
            default: return 0f;
        }
    }

    // For later settings menu
    public void SetSensitivities(float newMouse, float newStick, bool newInvertX, bool newInvertY)
    {
        mouseSensitivity = newMouse;
        stickSensitivity = newStick;
        invertX = newInvertX;
        invertY = newInvertY;
    }
}

