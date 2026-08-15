using UnityEngine;

/// <summary>
/// Fixed-angle isometric/top-down follow camera.
/// - Locks to a configurable pitch + yaw (no drift).
/// - Resolves wall collisions by pulling the camera forward.
/// - Optionally adds a vertical look-ahead so the player never hits
///   the top/bottom of screen while jumping.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════════
    //  INSPECTOR
    // ═══════════════════════════════════════════════════════════════

    [Header("Target")]
    [Tooltip("Assign the player Transform here.")]
    public Transform target;

    [Header("Camera Distance & Height")]
    [Tooltip("Distance the camera sits behind the aim point.")]
    public float distance = 8f;

    [Header("Camera Angles (fixed world-space)")]
    [Tooltip("Up/Down angle (Pitch). 45° = classic isometric look.")]
    public float pitch = 45f;
    [Tooltip("Left/Right angle (Yaw). 0° = camera on the -Z side (looking +Z).")]
    public float yaw   = 0f;

    [Header("Aim Offset")]
    [Tooltip("World-space offset added to the target before framing it.\n"
           + "Increase Y to raise the look-at point so feet aren't centred.")]
    public Vector3 aimOffset = new Vector3(0f, 1.2f, 0f);

    [Header("Follow Smoothing")]
    [Tooltip("Time (sec) for the camera to catch up to the player. Lower = snappier.")]
    public float positionSmoothTime = 0.12f;
    [Tooltip("Extra smoothing applied only on the Y axis to absorb jump bounce.\n"
           + "Set higher than positionSmoothTime to decouple vertical following.")]
    public float verticalSmoothTime = 0.20f;

    [Header("Collision")]
    [Tooltip("Physics layers the camera should not clip through.")]
    public LayerMask collisionMask;
    [Tooltip("Radius of the sphere used for collision probing.")]
    public float collisionRadius = 0.3f;
    [Tooltip("Minimum distance the camera is allowed to pull to.")]
    public float minCollisionDistance = 1.0f;

    // ═══════════════════════════════════════════════════════════════
    //  INTERNAL STATE
    // ═══════════════════════════════════════════════════════════════

    // Separate XZ and Y smooth-damp velocities so vertical smoothing
    // is independent (absorbs jump bounce without side-lag).
    private Vector3 _xzVelocity;   // used for XZ plane damping
    private float   _yVelocity;    // used for Y-axis damping

    // Cached fixed rotation — re-computed only when pitch/yaw change.
    private Quaternion _fixedRotation;
    private float      _cachedPitch;
    private float      _cachedYaw;

    // ═══════════════════════════════════════════════════════════════
    //  UNITY LIFECYCLE
    // ═══════════════════════════════════════════════════════════════

    private void Awake()
    {
        RefreshRotationCache();
        
        // Ensure there is always an AudioListener so music/SFX can be heard!
        if (GetComponent<AudioListener>() == null)
        {
            gameObject.AddComponent<AudioListener>();
        }
    }

    private void Start()
    {
        if (target == null)
        {
            // Auto-find tagged player if not assigned in Inspector
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) 
            {
                target = player.transform;
            }
            else
            {
                // Fallback: look for the movement script since the player isn't tagged
                var movementScript = Object.FindAnyObjectByType<PlayerMovement>();
                if (movementScript != null)
                {
                    target = movementScript.transform;
                }
            }
        }

        if (target != null)
        {
            // Snap to correct position on the first frame — no lerp-in.
            Vector3 snap = DesiredPosition(target.position);
            transform.position = snap;
            transform.rotation = _fixedRotation;
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // 1. Re-cache rotation if the user tweaked pitch/yaw at runtime (Inspector).
        if (!Mathf.Approximately(_cachedPitch, pitch) || !Mathf.Approximately(_cachedYaw, yaw))
            RefreshRotationCache();

        Vector3 desired = DesiredPosition(target.position);

        // 2. Collision — pull camera forward if a wall is in the way.
        desired = ResolveCollision(desired, target.position + aimOffset);

        // 3. Smooth XZ separately from Y for jump-feel independence.
        Vector3 current = transform.position;

        // XZ smooth
        Vector3 nextXZ = Vector3.SmoothDamp(
            new Vector3(current.x, 0f, current.z),
            new Vector3(desired.x, 0f, desired.z),
            ref _xzVelocity,
            positionSmoothTime);

        // Y smooth (higher smoothTime = less vertical bounce when jumping)
        float nextY = Mathf.SmoothDamp(current.y, desired.y, ref _yVelocity, verticalSmoothTime);

        transform.position = new Vector3(nextXZ.x, nextY, nextXZ.z);

        // 4. Rotation is purely the fixed pitch/yaw — never drifts.
        transform.rotation = _fixedRotation;
    }

    // ═══════════════════════════════════════════════════════════════
    //  HELPERS
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Compute where the camera wants to sit for a given target world position.</summary>
    private Vector3 DesiredPosition(Vector3 targetPos)
    {
        // Aim at the player's chest/head rather than their feet.
        Vector3 focus = targetPos + aimOffset;

        // Step backward from the focus point along the camera's forward.
        // _fixedRotation * Vector3.forward = the direction the camera looks.
        // We want to sit BEHIND that, so subtract.
        return focus - (_fixedRotation * Vector3.forward) * distance;
    }

    /// <summary>
    /// Sphere-cast from the aim point toward the desired camera position.
    /// If something is in the way, pull the camera closer to avoid clipping.
    /// </summary>
    private Vector3 ResolveCollision(Vector3 desiredPos, Vector3 focusPoint)
    {
        if (collisionMask == 0) return desiredPos;

        Vector3 dir     = desiredPos - focusPoint;
        float   maxDist = dir.magnitude;
        if (maxDist < 0.001f) return desiredPos;

        Vector3 normDir = dir / maxDist;

        if (Physics.SphereCast(
            focusPoint, collisionRadius, normDir,
            out RaycastHit hit, maxDist,
            collisionMask, QueryTriggerInteraction.Ignore))
        {
            float safeDistance = Mathf.Max(minCollisionDistance, hit.distance - collisionRadius);
            return focusPoint + normDir * safeDistance;
        }

        return desiredPos;
    }

    /// <summary>Cache the fixed-angle rotation so we don't call Quaternion.Euler every frame.</summary>
    private void RefreshRotationCache()
    {
        _cachedPitch  = pitch;
        _cachedYaw    = yaw;
        _fixedRotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    // ═══════════════════════════════════════════════════════════════
    //  EDITOR GIZMOS
    // ═══════════════════════════════════════════════════════════════

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (target == null) return;

        RefreshRotationCache();
        Vector3 focus   = target.position + aimOffset;
        Vector3 desired = DesiredPosition(target.position);

        // Draw the ideal camera position
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(desired, 0.15f);

        // Draw a line from focus point to camera
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(focus, desired);

        // Draw the aim offset point
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(focus, 0.1f);
    }
#endif
}
