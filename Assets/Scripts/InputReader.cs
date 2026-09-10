using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class InputReader : MonoBehaviour
{
    [Header("Input Action Asset")]
    public InputActionAsset inputActions;

    // Actions
    private InputAction _moveAction;
    private InputAction _lookAction;
    private InputAction _jumpAction;
    private InputAction _sprintAction;
    private InputAction _attackAction;
    private InputAction _defendAction; 
    private InputAction _potionAction;
    private InputAction _inventoryAction;
    private InputAction _zoomAction;

    // State
    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }
    public bool IsGamepadLook { get; private set; }
    public float ZoomInput { get; private set; }
    public bool IsOrbiting { get; private set; } // Derived from right-click or similar

    public bool IsJumping { get; private set; }
    public bool IsSprinting { get; private set; }
    public bool IsAttacking { get; private set; }
    public bool IsDefending { get; private set; }

    public bool IsUsingPotion { get; private set; }
    public bool PotionPressedThisFrame { get; private set; }
    public bool InventoryToggledThisFrame { get; private set; }

    public bool JumpPressedThisFrame { get; private set; }
    public bool AttackPressedThisFrame { get; private set; }
    public bool SprintPressedThisFrame { get; private set; }
    public bool SprintWasToggledThisFrame { get; private set; }

    private void Awake()
    {
        if (inputActions == null)
        {
            Debug.LogError("InputReader: InputActionAsset is missing!");
            return;
        }

        var playerMap = inputActions.FindActionMap("Player");
        if (playerMap == null)
        {
            Debug.LogError("InputReader: 'Player' action map not found!");
            return;
        }

        playerMap.Enable();

        _moveAction = playerMap.FindAction("Move");
        _lookAction = playerMap.FindAction("Look");
        _jumpAction = playerMap.FindAction("Jump");
        _sprintAction = playerMap.FindAction("Sprint");
        _attackAction = playerMap.FindAction("Attack");
        
        _defendAction = playerMap.FindAction("Defend");
        if (_defendAction == null) _defendAction = playerMap.FindAction("Interact");
        
        _potionAction = playerMap.FindAction("Potion");
        _inventoryAction = playerMap.FindAction("Inventory");

        // Try to find a Zoom action, if it doesn't exist, we will gracefully handle it
        _zoomAction = playerMap.FindAction("Zoom");

        // Load any saved custom keybindings
        KeybindManager.LoadBindings(inputActions);
    }

    private void OnDestroy()
    {
        if (inputActions != null)
        {
            var playerMap = inputActions.FindActionMap("Player");
            if (playerMap != null) playerMap.Disable();
        }
    }

    private void Update()
    {
        if (inputActions == null) return;

        // Reset frame-specific triggers
        JumpPressedThisFrame = false;
        AttackPressedThisFrame = false;
        SprintPressedThisFrame = false;
        SprintWasToggledThisFrame = false;
        PotionPressedThisFrame = false;
        InventoryToggledThisFrame = false;

        // Read Values
        if (_moveAction != null) MoveInput = _moveAction.ReadValue<Vector2>();
        
        if (_lookAction != null) 
        {
            LookInput = _lookAction.ReadValue<Vector2>();

            var control = _lookAction.activeControl;
            if (control != null)
            {
                var dev = control.device;
                if (dev is Gamepad || dev is Joystick)
                    IsGamepadLook = true;
                else if (dev is Pointer || dev is Keyboard)
                    IsGamepadLook = false;
            }
            else if (Gamepad.current != null && Gamepad.current.rightStick.ReadValue().sqrMagnitude > 0.01f)
            {
                IsGamepadLook = true;
            }
        }

        if (_zoomAction != null) 
        {
            // Usually scroll wheel is a Vector2, we only care about y
            var zoomVal = _zoomAction.ReadValueAsObject();
            if (zoomVal is Vector2 v) ZoomInput = v.y;
            else if (zoomVal is float f) ZoomInput = f;
        }
        else if (Mouse.current != null)
        {
            ZoomInput = Mouse.current.scroll.ReadValue().y;
        }

        if (Mouse.current != null)
        {
            IsOrbiting = Mouse.current.rightButton.isPressed;
        }

        // Read Buttons
        if (_jumpAction != null)
        {
            IsJumping = _jumpAction.IsPressed();
            JumpPressedThisFrame = _jumpAction.WasPressedThisFrame();
        }

        if (_sprintAction != null)
        {
            IsSprinting = _sprintAction.IsPressed();
            SprintPressedThisFrame = _sprintAction.WasPressedThisFrame();
            SprintWasToggledThisFrame = _sprintAction.WasPressedThisFrame();
        }

        if (_attackAction != null)
        {
            IsAttacking = _attackAction.IsPressed();
            AttackPressedThisFrame = _attackAction.WasPressedThisFrame();
        }

        if (_defendAction != null)
        {
            IsDefending = _defendAction.IsPressed();
        }

        if (_potionAction != null)
        {
            IsUsingPotion = _potionAction.IsPressed();
            PotionPressedThisFrame = _potionAction.WasPressedThisFrame();
        }

        if (_inventoryAction != null)
        {
            InventoryToggledThisFrame = _inventoryAction.WasPressedThisFrame();
        }
    }
}
