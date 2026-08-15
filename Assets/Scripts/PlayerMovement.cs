using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(InputReader))]
public class PlayerMovement : MonoBehaviour
{
    // ════════════════════════════════════════════════════════════════════════
    //  INSPECTOR SETTINGS
    // ════════════════════════════════════════════════════════════════════════

    [Header("Speed")]
    public float walkSpeed   = 4.5f;
    public float sprintSpeed = 9f;

    [Header("Feel – Ground Movement")]
    [Tooltip("Seconds to reach full speed from rest.")]
    public float accelerationTime = 0.10f;
    [Tooltip("Seconds to stop. Shorter = snappier.")]
    public float decelerationTime = 0.05f;
    [Tooltip("How fast raw input direction is smoothed. Higher = more responsive.")]
    public float inputSharpness   = 20f;
    [Tooltip("Degrees/sec to rotate toward movement direction.")]
    public float turnSpeed        = 720f;
    [Tooltip("Toggle sprint on/off instead of holding Shift.")]
    public bool  sprintToggle     = false;

    [Header("Feel – Air")]
    [Tooltip("Movement control while airborne (0 = no air control, 1 = full).")]
    [Range(0f, 1f)]
    public float airControl      = 0.6f;
    [Tooltip("Seconds to change direction in the air — higher = floatier steering.")]
    public float airAccelTime    = 0.25f;

    [Header("Feel – Jump & Gravity")]
    public float jumpHeight        = 2.2f;
    [Tooltip("Base gravity. Stronger = snappier overall arc.")]
    public float gravity           = -28f;
    [Tooltip("Extra gravity multiplier applied while falling (>1 removes floatiness).")]
    public float fallMultiplier    = 2.5f;
    [Tooltip("Extra gravity when jump is released early (enables short hop).")]
    public float lowJumpMultiplier = 2.0f;
    [Tooltip("Grace window (sec) to jump after walking off a ledge.")]
    public float coyoteTime        = 0.15f;
    [Tooltip("Press jump slightly early and it fires on landing.")]
    public float jumpBufferTime    = 0.15f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float     groundDistance = 0.35f;
    public LayerMask groundMask;

    [Header("Animation")]
    public Animator animator;
    public float    animSmoothTime = 0.08f;

    // ════════════════════════════════════════════════════════════════════════
    //  ANIMATOR HASHES  (static = computed once per class, not per instance)
    // ════════════════════════════════════════════════════════════════════════

    private static readonly int HashMoveX       = Animator.StringToHash("MoveX");
    private static readonly int HashMoveZ       = Animator.StringToHash("MoveZ");
    private static readonly int HashIsSprinting = Animator.StringToHash("IsSprinting");
    private static readonly int HashIsGrounded  = Animator.StringToHash("IsGrounded");
    private static readonly int HashJump        = Animator.StringToHash("Jump");

    // ════════════════════════════════════════════════════════════════════════
    //  CACHED COMPONENTS
    // ════════════════════════════════════════════════════════════════════════

    private CharacterController _cc;
    private Camera              _cam;
    private PlayerCombat        _combat;
    private InputReader         _input;

    // ════════════════════════════════════════════════════════════════════════
    //  RUNTIME STATE
    // ════════════════════════════════════════════════════════════════════════

    // Physics
    private Vector3 _velocity;
    private bool    _isGrounded;
    private float   _coyoteTimer;
    private float   _jumpBufferTimer;
    private bool    _jumpHeld;          // true while jump button is held
    private bool    _wasJumpPressed;
    private bool    _wasSprintPressed;
    private float   _jumpCooldown;      // prevents same-frame re-trigger after landing

    // Movement
    private float   _currentSpeed;
    private float   _speedVelocity;
    private Vector2 _smoothInput;       // smoothed directional input (no allocation)

    // Sprint toggle state
    private bool    _sprintToggled;

    // Animation
    private float   _animMoveZ, _animVelZ;
    private float   _animMoveX, _animVelX;

    private Vector3 _camF, _camR;
    
    // Physics arrays (zero allocation)
    private Collider[] _groundHits = new Collider[10];

    // ════════════════════════════════════════════════════════════════════════
    //  UNITY LIFECYCLE
    // ════════════════════════════════════════════════════════════════════════

    void Awake()
    {
        _cc  = GetComponent<CharacterController>();
        _combat = GetComponent<PlayerCombat>();
        _input = GetComponent<InputReader>();
        _cam = Camera.main;                     // cached here — avoids FindObjectOfType per frame

        if (groundMask == 0) groundMask = ~0;   // fallback to Everything if not set
        _coyoteTimer = coyoteTime;              // allow jump immediately at game start

        // Ensure minimap is attached
        if (GetComponent<CircularMinimap>() == null)
        {
            gameObject.AddComponent<CircularMinimap>();
        }
    }

    void Start()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (groundCheck == null)
        {
            var gc = new GameObject("GroundCheck");
            gc.transform.SetParent(transform);
            gc.transform.localPosition = Vector3.down;
            groundCheck = gc.transform;
        }
    }

    void Update()
    {
        CheckGround();
        HandleMovement();
        HandleJump();
        ApplyGravity();

#if UNITY_EDITOR
        // DEBUG: Draw a laser to see what we are standing on!
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 5f))
        {
            Debug.DrawLine(transform.position, hit.point, Color.magenta);
            if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame)
            {
                Debug.LogWarning("The character is standing on an invisible collider belonging to: " + hit.collider.gameObject.name);
            }
        }
#endif
    }

    void CheckGround()
    {
        // Tick jump cooldown — prevents the ground check from re-triggering a jump
        // on the very frame we leave the ground.
        if (_jumpCooldown > 0f) _jumpCooldown -= Time.deltaTime;

        // If we are moving upwards (jumping), ignore the ground check for a clean takeoff.
        if (_velocity.y > 0.1f)
        {
            _isGrounded = false;
        }
        else
        {
            // Probe at the bottom of the CharacterController capsule.
            // skinWidth pushes the CC slightly above real geometry, so we subtract it
            // to keep the sphere flush with the ground surface.
            float   radius    = _cc.radius * 0.95f;
            float   centerY   = _cc.height * 0.5f;                 // local centre of capsule
            float   bottom    = centerY - (_cc.height * 0.5f - radius); // bottom sphere centre
            Vector3 spherePos = transform.position
                              + _cc.center
                              + Vector3.down * (centerY - radius - _cc.skinWidth * 0.5f);

            int hitCount = Physics.OverlapSphereNonAlloc(spherePos, radius, _groundHits, groundMask, QueryTriggerInteraction.Ignore);
            _isGrounded = false;
            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = _groundHits[i];
                // Ignore our own player collider!
                if (hit.gameObject != gameObject)
                {
                    _isGrounded = true;
                    break;
                }
            }
            _isGrounded = _isGrounded || _cc.isGrounded;
        }

        if (_isGrounded)
        {
            _coyoteTimer = coyoteTime;
            if (_velocity.y < -2f) _velocity.y = -2f;   // keep grounded without sinking
        }
        else
        {
            _coyoteTimer -= Time.deltaTime;
        }

        if (animator != null)
            animator.SetBool(HashIsGrounded, _isGrounded);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  MOVEMENT
    // ════════════════════════════════════════════════════════════════════════

    void HandleMovement()
    {
        // ── Read raw input ────────────────────────────────────────────────────
        var raw = _input != null ? _input.MoveInput : Vector2.zero;

        // Prevent diagonal keyboard input being ~41% faster
        if (raw.sqrMagnitude > 1f) raw.Normalize();

        // ── Smooth input ──────────────────────────────────────────────────────
        // MoveTowards keeps magnitude capped at raw, giving snappy-yet-smooth feel
        _smoothInput = Vector2.MoveTowards(_smoothInput, raw, inputSharpness * Time.deltaTime);

        bool hasInput = raw.sqrMagnitude > 0.01f;

        // ── Sprint ────────────────────────────────────────────────────────────
        bool sprintDown = _input != null && _input.IsSprinting;
        bool sprintPressed = _input != null && _input.SprintPressedThisFrame;
        _wasSprintPressed  = sprintDown;
        bool sprintHeld    = sprintDown;

        bool isSprinting;
        if (sprintToggle)
        {
            if (sprintPressed) _sprintToggled = !_sprintToggled;
            isSprinting = _sprintToggled && hasInput;
        }
        else
        {
            isSprinting = sprintHeld && hasInput;
        }

        // ── Camera-relative world move vector ─────────────────────────────────
        // Update _camF / _camR via helper, then build world-space vector.
        // We do NOT normalize yet so we can compare raw magnitude for analog.
        CamRelativeDir(_smoothInput);    // refreshes _camF & _camR
        Vector3 worldMove = _camF * _smoothInput.y + _camR * _smoothInput.x;
        float   inputMag  = worldMove.magnitude;
        Vector3 moveDir   = inputMag > 0.001f ? worldMove / inputMag : Vector3.zero;

        // ── Target speed ──────────────────────────────────────────────────────
        bool isAttacking = _combat != null && _combat.IsAttacking;
        bool isDefending = _combat != null && _combat.IsDefending;

        // Base target speed
        float targetSpeed = (hasInput && !isAttacking)
            ? ((isSprinting && !isDefending) ? sprintSpeed : walkSpeed)
            : 0f;

        // Slow down movement while holding block
        if (isDefending && targetSpeed > 0f)
        {
            targetSpeed *= 0.45f; // Walk at 45% speed while blocking
        }

        // Analog input: scale speed by stick magnitude → gentle push = slow walk
        if (hasInput)
            targetSpeed *= Mathf.Clamp01(_smoothInput.magnitude);

        // ── Smooth speed ──────────────────────────────────────────────────────
        float accelTime = _isGrounded
            ? (hasInput ? accelerationTime : decelerationTime)
            : airAccelTime;

        _currentSpeed = Mathf.SmoothDamp(
            _currentSpeed, targetSpeed,
            ref _speedVelocity, accelTime);

        // ── Rotation ──────────────────────────────────────────────────────────
        // Character always faces the camera's yaw direction.
        // WASD moves camera-relative but does NOT rotate the character — it strafes.
        // The body only turns when the player rotates the camera (mouse drag).
        if (!isAttacking)
        {
            float   camYaw     = _cam != null ? _cam.transform.eulerAngles.y : transform.eulerAngles.y;
            Quaternion target  = Quaternion.Euler(0f, camYaw, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, turnSpeed * Time.deltaTime);
        }

        // ── Translate ─────────────────────────────────────────────────────────
        if (moveDir.sqrMagnitude > 0.001f && _currentSpeed > 0.05f)
        {
            float scale = _isGrounded ? 1f : airControl;
            _cc.Move(moveDir * (_currentSpeed * scale * Time.deltaTime));
        }

        // ── Animator ──────────────────────────────────────────────────────────
        // Convert world movement to LOCAL space so the blend tree gets:
        //   MoveZ > 0  → forward walk/run
        //   MoveZ < 0  → backward walk   ← key fix
        //   MoveX ≠ 0  → strafe left/right
        Vector3 localVel  = transform.InverseTransformDirection(moveDir);
        float   normSpeed = _currentSpeed / walkSpeed;   // 0=idle, 1=walk, >1=sprint

        _animMoveZ = Mathf.SmoothDamp(_animMoveZ,
            hasInput ? localVel.z * normSpeed : 0f, ref _animVelZ, animSmoothTime);
        _animMoveX = Mathf.SmoothDamp(_animMoveX,
            hasInput ? localVel.x * normSpeed : 0f, ref _animVelX, animSmoothTime);

        if (animator != null)
        {
            animator.SetFloat(HashMoveZ,       _animMoveZ);
            animator.SetFloat(HashMoveX,       _animMoveX);
            animator.SetBool (HashIsSprinting, isSprinting);
        }
    }


    // ════════════════════════════════════════════════════════════════════════
    //  JUMP  (coyote time + input buffer)
    // ════════════════════════════════════════════════════════════════════════

    void HandleJump()
    {
        _jumpBufferTimer -= Time.deltaTime;

        bool jumpDown = _input != null && _input.IsJumping;
        
        bool jumpPressed = jumpDown && !_wasJumpPressed;
        _wasJumpPressed  = jumpDown;
        _jumpHeld        = jumpDown;

        if (jumpPressed) _jumpBufferTimer = jumpBufferTime;

        // Reset float-trigger every frame
        if (animator != null) animator.SetFloat(HashJump, 0f);

        bool canJump = _coyoteTimer > 0f && _jumpBufferTimer > 0f && _jumpCooldown <= 0f;
        if (canJump)
        {
            // v = sqrt(2 * |g| * h) — physics-accurate jump velocity
            _velocity.y      = Mathf.Sqrt(jumpHeight * 2f * Mathf.Abs(gravity));
            _coyoteTimer     = 0f;
            _jumpBufferTimer = 0f;
            _jumpCooldown    = 0.15f; // 150 ms grace before next ground check matters

            if (animator != null) animator.SetFloat(HashJump, 1f);
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    //  GRAVITY  (fall multiplier + short hop)
    // ════════════════════════════════════════════════════════════════════════

    void ApplyGravity()
    {
        if (!_isGrounded)
        {
            // ── Determine the effective gravity multiplier this frame ──────────
            // We apply gravity once below, so these multipliers add EXTRA force
            // on top of base gravity (not a second full application).
            float multiplier = 1f;

            if (_velocity.y < 0f)
            {
                // Falling — heavier gravity removes floatiness
                multiplier = fallMultiplier;
            }
            else if (_velocity.y > 0f && !_jumpHeld)
            {
                // Rising but button released — cut the arc for a short hop
                multiplier = lowJumpMultiplier;
            }

            // Apply gravity once with the chosen multiplier
            _velocity.y += gravity * multiplier * Time.deltaTime;
        }
        else
        {
            // Grounded — apply base gravity only (keeps CC stuck to slopes).
            // Do NOT reset to 0 here; the clamp in CheckGround handles that.
            _velocity.y += gravity * Time.deltaTime;
        }

        _cc.Move(_velocity * Time.deltaTime);

        // If we bonk the ceiling, kill upward momentum immediately
        if ((_cc.collisionFlags & CollisionFlags.Above) != 0 && _velocity.y > 0f)
            _velocity.y = 0f;
    }

    // ════════════════════════════════════════════════════════════════════════
    //  HELPERS
    // ════════════════════════════════════════════════════════════════════════

    Vector3 CamRelativeDir(Vector2 input)
    {
        if (_cam != null)
        {
            var ct = _cam.transform;
            _camF = ct.forward; _camF.y = 0f; _camF.Normalize();
            _camR = ct.right;   _camR.y = 0f; _camR.Normalize();
        }
        else
        {
            _camF = Vector3.forward;
            _camR = Vector3.right;
        }
        return (_camF * input.y + _camR * input.x).normalized;
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = _isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
    }
}
