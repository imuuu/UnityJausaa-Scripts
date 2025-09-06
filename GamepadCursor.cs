using UnityEngine;

/// <summary>
/// Handles cursor movement with either gamepad or mouse.
/// Provides unified state flags (GamepadMouse0, GamepadE, etc.)
/// so other scripts can treat controller input as virtual mouse/keyboard.
/// </summary>
public class GamepadCursor : MonoBehaviour
{
    public enum BackendMode { Auto, InputSystemOnly, LegacyOnly }

    [Header("Backend")]
    [SerializeField] private BackendMode _backendMode = BackendMode.Auto;

    [Header("Movement")]
    [SerializeField] private float _speed = 1300f;
    [Tooltip("Hide OS cursor while controlling with gamepad.")]
    [SerializeField] private bool _hideHardwareCursor = false;

    [Header("Auto-switch")]
    [SerializeField] private bool _autoSwitch = true;
    [Range(0f, 1f)] [SerializeField] private float _stickDeadzone = 0.2f;
    [SerializeField] private float _mouseMoveThreshold = 0.5f;
    [SerializeField] private float _stickHoldGrace = 0.5f;

    [Header("Legacy (Old Input Manager) axis/button names")]
    [Tooltip("Dedicated RT axis (0..1). Leave empty if not used.")]
    [SerializeField] private string _legacyRtAxis = "RT";
    [Tooltip("Combined Triggers axis (-1..+1, LT<0, RT>0). Leave empty if not used.")]
    [SerializeField] private string _legacyTriggersAxis = "Triggers";
    [Tooltip("Alternative RT button mapping. Leave empty if not used.")]
    [SerializeField] private string _legacyRtButton = "FireRT";
    [Tooltip("Threshold at which RT counts as pressed.")]
    [SerializeField] private float _triggerPressThreshold = 0.2f;

    [Header("Optional UI Cursor")]
    [SerializeField] private RectTransform _uiCursor;

    // --- Public states available for other scripts ---
    public static Vector2 CurrentScreenPosition { get; private set; }
    public static bool GamepadMouse0 { get; private set; }        // RT held
    public static bool GamepadMouse0_Down { get; private set; }   // RT pressed this frame
    public static bool GamepadMouse0_Up { get; private set; }     // RT released this frame
    public static bool GamepadE { get; private set; }             // A held
    public static bool GamepadE_Down { get; private set; }        // A pressed this frame
    public static bool GamepadE_Up { get; private set; }          // A released this frame
    public static bool ControllingWithGamepad { get; private set; }

    // --- Internals ---
    private static bool _lastGamepadMouse0;
    private Vector2 _lastMousePos;
    private float _gamepadLockUntil;
    private bool _lastE;
    private bool _lastCursorVisible;

#if ENABLE_INPUT_SYSTEM
    private UnityEngine.InputSystem.Gamepad GP => UnityEngine.InputSystem.Gamepad.current;
    private UnityEngine.InputSystem.Mouse MS => UnityEngine.InputSystem.Mouse.current;
#endif

    private void Start()
    {
#if ENABLE_INPUT_SYSTEM
        if (UseInputSystem() && MS != null)
            CurrentScreenPosition = MS.position.ReadValue();
        else
#endif
            CurrentScreenPosition = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        _lastMousePos = CurrentScreenPosition;
        ApplyCursorVisibility();
    }

    private void Update()
    {
        bool useIS = UseInputSystem();

        // 1) Detect mouse movement
        bool mouseMoved = false;
        if (useIS)
        {
#if ENABLE_INPUT_SYSTEM
            if (MS != null)
            {
                Vector2 mpos = MS.position.ReadValue();
                if ((mpos - _lastMousePos).sqrMagnitude >= _mouseMoveThreshold * _mouseMoveThreshold)
                    mouseMoved = true;
                _lastMousePos = mpos;
            }
#endif
        }
        else
        {
            Vector2 mpos = Input.mousePosition;
            if ((mpos - _lastMousePos).sqrMagnitude >= _mouseMoveThreshold * _mouseMoveThreshold)
                mouseMoved = true;
            _lastMousePos = mpos;
        }

        // 2) Detect stick movement
        Vector2 stick = Vector2.zero;
        bool stickActive = false;
        if (useIS)
        {
#if ENABLE_INPUT_SYSTEM
            if (GP != null)
            {
                stick = GP.rightStick.ReadValue();
                stickActive = stick.magnitude > _stickDeadzone;
            }
#endif
        }
        else
        {
            float h = SafeGetAxisRaw("RightStickX");
            float v = SafeGetAxisRaw("RightStickY");
            stick = new Vector2(h, v);
            stickActive = stick.magnitude > _stickDeadzone;
        }

        // 3) Auto-switch between mouse and gamepad control
        if (_autoSwitch)
        {
            if (stickActive)
            {
                ControllingWithGamepad = true;
                _gamepadLockUntil = Time.unscaledTime + _stickHoldGrace;
            }
            else if (mouseMoved && Time.unscaledTime > _gamepadLockUntil)
            {
                ControllingWithGamepad = false;
            }
        }

        // 4) Update cursor position
        if (ControllingWithGamepad)
        {
            Vector2 pos = CurrentScreenPosition + stick * _speed * Time.unscaledDeltaTime;
            pos.x = Mathf.Clamp(pos.x, 0, Screen.width - 1);
            pos.y = Mathf.Clamp(pos.y, 0, Screen.height - 1);
            CurrentScreenPosition = pos;

#if ENABLE_INPUT_SYSTEM
            if (useIS && MS != null)
            {
                MS.WarpCursorPosition(CurrentScreenPosition);
                UnityEngine.InputSystem.InputSystem.Update();
            }
#endif
        }
        else
        {
            CurrentScreenPosition = useIS
#if ENABLE_INPUT_SYSTEM
                ? (MS != null ? MS.position.ReadValue() : (Vector2)Input.mousePosition)
#endif
                : (Vector2)Input.mousePosition;
        }

        // 5) Apply cursor visibility rules
        ApplyCursorVisibility();

        // 6) Update UI cursor if assigned
        if (_uiCursor != null) _uiCursor.position = CurrentScreenPosition;

        // 7) RT → virtual Mouse0
        GamepadMouse0 = ReadRightTriggerPressed(useIS);
        GamepadMouse0_Down = !_lastGamepadMouse0 && GamepadMouse0;
        GamepadMouse0_Up = _lastGamepadMouse0 && !GamepadMouse0;
        _lastGamepadMouse0 = GamepadMouse0;

        // 8) A → virtual "E"
        bool ePressed = ReadAButton(useIS);
        GamepadE = ePressed;
        GamepadE_Down = !_lastE && ePressed;
        GamepadE_Up = _lastE && !ePressed;
        _lastE = ePressed;
    }

    /// <summary>
    /// Allow external scripts (e.g. snapper) to override cursor position.
    /// </summary>
    public static void InjectScreenPosition(Vector2 newPos)
    {
        CurrentScreenPosition = newPos;
    }

    // --- Helpers ---
    private void ApplyCursorVisibility()
    {
        bool targetVisible = !(_hideHardwareCursor && ControllingWithGamepad);
        if (targetVisible != _lastCursorVisible)
        {
            Cursor.visible = targetVisible;
            _lastCursorVisible = targetVisible;
        }
    }

    private bool UseInputSystem()
    {
        if (_backendMode == BackendMode.InputSystemOnly) return IsInputSystemAvailable();
        if (_backendMode == BackendMode.LegacyOnly) return false;
        return IsInputSystemAvailable();
    }

    private bool IsInputSystemAvailable()
    {
#if ENABLE_INPUT_SYSTEM
        return true;
#else
        return false;
#endif
    }

    private float SafeGetAxisRaw(string name)
    {
        try { return string.IsNullOrEmpty(name) ? 0f : Input.GetAxisRaw(name); }
        catch { return 0f; }
    }

    private bool SafeGetButton(string name)
    {
        try { return !string.IsNullOrEmpty(name) && Input.GetButton(name); }
        catch { return false; }
    }

    private bool ReadRightTriggerPressed(bool useIS)
    {
        if (useIS)
        {
#if ENABLE_INPUT_SYSTEM
            if (GP == null) return false;
            float val = GP.rightTrigger.ReadValue();
            return val >= _triggerPressThreshold;
#else
            return false;
#endif
        }
        else
        {
            float rt = SafeGetAxisRaw(_legacyRtAxis);
            if (rt > 0f && rt >= _triggerPressThreshold) return true;

            float trig = SafeGetAxisRaw(_legacyTriggersAxis);
            if (trig > 0f && trig >= _triggerPressThreshold) return true;

            if (SafeGetButton(_legacyRtButton)) return true;
            return false;
        }
    }

    private bool ReadAButton(bool useIS)
    {
        if (useIS)
        {
#if ENABLE_INPUT_SYSTEM
            return GP != null && GP.buttonSouth.isPressed; // Xbox A
#else
            return false;
#endif
        }
        else
        {
            return SafeGetButton("A"); // usually joystick button 0 mapped as "A"
        }
    }
}
