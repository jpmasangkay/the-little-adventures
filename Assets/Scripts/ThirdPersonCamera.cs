using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target & Positioning")]
    [Tooltip("Drag the player GameObject here")]
    public Transform target; 
    [Tooltip("Offset to aim at player's head/shoulders")]
    public Vector3 targetOffset = new Vector3(0f, 1.2f, 0f); 
    public float distance = 5f;

    [Header("Camera Control")]
    [Tooltip("How fast the camera rotates left/right")]
    public float sensitivityX = 2f;
    [Tooltip("The fixed up/down angle of the camera (since vertical movement is locked)")]
    public float fixedPitch = 40f;

    [Header("Collision")]
    public float cameraRadius = 0.3f;
    [Tooltip("Layers the camera should collide with (e.g., Default, Ground)")]
    public LayerMask collisionMask = ~0; 

    [Header("Smoothing")]
    [Tooltip("How smoothly the camera follows the player's position")]
    public float positionSmoothTime = 0.05f;
    [Tooltip("How smoothly the camera rotates when you move the mouse")]
    public float rotationSmoothSpeed = 15f;
    [Tooltip("How smoothly the camera zooms IN when hitting a wall (keep low to avoid clipping)")]
    public float zoomInSmoothTime = 0.08f;
    [Tooltip("How smoothly the camera zooms OUT after passing a wall")]
    public float distanceSmoothTime = 0.2f;

    private float _currentX = 0f;
    private float _currentY = 20f;
    private bool _isOrbiting;
    
    private Vector3 _smoothedFocusPoint;
    private Vector3 _focusVelocity;
    private float _smoothDistance;
    private float _distanceVelocity;
    private RaycastHit[] _cameraHits = new RaycastHit[20];
    
    private InputReader _input;

    private void Start()
    {
        if (target == null)
        {
            // Try to find the player if not assigned
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

        Vector3 angles = transform.eulerAngles;
        _currentX = angles.y;
        _currentY = fixedPitch;

        _smoothDistance = distance;
        _smoothedFocusPoint = target.position + targetOffset;

        if (target != null)
        {
            _input = target.GetComponent<InputReader>();
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        bool isOrbiting = _input != null && _input.IsOrbiting;
        
        // OPTIMIZATION: Calling Cursor.lockState every frame causes heavy OS overhead and stuttering.
        // We now only call it exactly when the state changes.
        if (isOrbiting != _isOrbiting)
        {
            _isOrbiting = isOrbiting;
            Cursor.lockState = _isOrbiting ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !_isOrbiting;
        }

        Vector2 lookInput = _input != null ? _input.LookInput : Vector2.zero;
        
        // Note: If you want to require Right-Click to use the mouse to look (like an MMO), 
        // you should set up a "Button With One Modifier" binding in the Input Action asset 
        // for Mouse Delta, using Right Button as the modifier.
        // For gamepads, LookInput will just work automatically.

        // Feature: Zooming
        if (_input != null)
        {
            float scroll = _input.ZoomInput;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                distance -= scroll * 0.005f; // Adjust zoom speed
                distance = Mathf.Clamp(distance, 1.5f, 15f); // Keep camera within bounds
            }
        }

        // Lock vertical pitch to the fixed angle
        _currentY = fixedPitch;

        // Only rotate left/right based on input
        _currentX += lookInput.x * sensitivityX * 0.1f;

        // 1. Smoothly follow the player's position
        Vector3 targetFocus = target.position + targetOffset;
        _smoothedFocusPoint = Vector3.SmoothDamp(_smoothedFocusPoint, targetFocus, ref _focusVelocity, positionSmoothTime);

        // 2. Smoothly rotate the camera
        Quaternion targetRotation = Quaternion.Euler(_currentY, _currentX, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSmoothSpeed);

        // 3. Calculate collision from the smoothed rotation and focus point
        Vector3 backDirection = -(transform.rotation * Vector3.forward);
        
        float closestHit = distance;
        int hitCount = Physics.SphereCastNonAlloc(_smoothedFocusPoint, cameraRadius, backDirection, _cameraHits, distance, collisionMask, QueryTriggerInteraction.Ignore);
        
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = _cameraHits[i];
            // Ignore if we started inside the collider (distance == 0) 
            // This prevents huge invisible scene boundaries from crushing the camera
            if (hit.distance > 0.05f &&
                !hit.collider.transform.IsChildOf(target) && 
                !hit.collider.transform.IsChildOf(target.root) && 
                !hit.collider.CompareTag("Player"))
            {
                float safeHitDist = hit.distance - 0.1f;
                if (safeHitDist < closestHit)
                {
                    closestHit = safeHitDist;
                }
            }
        }
        
        // Don't let it zoom in closer than 2.5 to avoid looking inside the player's head
        float targetDistance = Mathf.Max(2.5f, closestHit);

        // 4. Smooth the zoom (fast smooth inward to avoid teleporting, slow smooth outward to avoid jerking)
        if (targetDistance < _smoothDistance)
        {
            _smoothDistance = Mathf.SmoothDamp(_smoothDistance, targetDistance, ref _distanceVelocity, zoomInSmoothTime);
        }
        else
        {
            _smoothDistance = Mathf.SmoothDamp(_smoothDistance, targetDistance, ref _distanceVelocity, distanceSmoothTime);
        }

        // 5. Apply final position tightly coupled to the smoothed components
        transform.position = _smoothedFocusPoint + (backDirection * _smoothDistance);
    }
}
