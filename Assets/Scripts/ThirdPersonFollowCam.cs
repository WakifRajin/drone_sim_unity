using UnityEngine;

/// <summary>
/// Third-person camera that follows a target and rotates with mouse input.
/// Features: smooth follow, mouse orbit, collision detection, zoom control.
/// </summary>
public class ThirdPersonFollowCam : MonoBehaviour
{
    [Header("Target Settings")]
    [Tooltip("The object the camera will follow (your drone)")]
    public Transform target;
    
    [Header("Camera Distance")]
    [Tooltip("Default distance from target")]
    public float defaultDistance = 10f;
    [Tooltip("Minimum zoom distance")]
    public float minDistance = 3f;
    [Tooltip("Maximum zoom distance")]
    public float maxDistance = 20f;
    [Tooltip("Zoom speed with mouse scroll")]
    public float zoomSpeed = 2f;
    
    [Header("Camera Position")]
    [Tooltip("Height offset above target")]
    public float heightOffset = 2f;
    [Tooltip("How smoothly camera follows target (0 = instant, higher = smoother)")]
    public float followSmoothness = 5f;
    
    [Header("Mouse Rotation")]
    [Tooltip("Mouse sensitivity for horizontal rotation")]
    public float mouseSensitivityX = 3f;
    [Tooltip("Mouse sensitivity for vertical rotation")]
    public float mouseSensitivityY = 3f;
    [Tooltip("Minimum vertical angle (looking down)")]
    public float minVerticalAngle = -30f;
    [Tooltip("Maximum vertical angle (looking up)")]
    public float maxVerticalAngle = 80f;
    [Tooltip("How smoothly camera rotates")]
    public float rotationSmoothness = 5f;
    [Tooltip("Invert vertical mouse movement")]
    public bool invertY = false;
    
    [Header("Cursor Lock")]
    [Tooltip("Press ESC to unlock cursor, click to lock again")]
    public bool allowCursorToggle = true;
    
    [Header("Collision Detection")]
    [Tooltip("Enable camera collision with environment")]
    public bool enableCollision = true;
    [Tooltip("Layers the camera should collide with")]
    public LayerMask collisionLayers = -1;
    [Tooltip("Radius of collision sphere")]
    public float collisionRadius = 0.3f;
    
    // Internal state
    private float currentDistance;
    private float targetDistance;
    private float currentYaw; // Horizontal rotation
    private float currentPitch; // Vertical rotation
    private float targetYaw;
    private float targetPitch;
    private Vector3 currentVelocity;
    private bool cursorLocked = true;

    void Start()
    {
        if (target == null)
        {
            Debug.LogError("ThirdPersonFollowCam: No target assigned!");
            enabled = false;
            return;
        }
        
        // Initialize distance
        currentDistance = defaultDistance;
        targetDistance = defaultDistance;
        
        // Initialize rotation from current camera orientation
        Vector3 angles = transform.eulerAngles;
        currentYaw = angles.y;
        currentPitch = angles.x;
        targetYaw = currentYaw;
        targetPitch = currentPitch;
        
        // Lock cursor automatically
        LockCursor();
    }

    void Update()
    {
        // Handle cursor lock/unlock toggle
        if (allowCursorToggle)
        {
            // Press ESC to unlock cursor
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                UnlockCursor();
            }
            
            // Click to lock cursor again
            if (Input.GetMouseButtonDown(0) && !cursorLocked)
            {
                LockCursor();
            }
        }
        
        // Press R to reset camera
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetCamera();
        }
    }

    void LateUpdate()
    {
        if (target == null) return;
        
        HandleMouseInput();
        HandleZoom();
        UpdateCameraPosition();
    }

    private void HandleMouseInput()
    {
        // Only rotate camera if cursor is locked
        if (cursorLocked)
        {
            // Get mouse input
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");
            
            // Apply sensitivity
            targetYaw += mouseX * mouseSensitivityX;
            targetPitch -= mouseY * mouseSensitivityY * (invertY ? -1f : 1f);
            
            // Clamp vertical rotation
            targetPitch = Mathf.Clamp(targetPitch, minVerticalAngle, maxVerticalAngle);
        }
        
        // Smoothly interpolate rotation
        currentYaw = Mathf.Lerp(currentYaw, targetYaw, Time.deltaTime * rotationSmoothness);
        currentPitch = Mathf.Lerp(currentPitch, targetPitch, Time.deltaTime * rotationSmoothness);
    }

    private void HandleZoom()
    {
        // Mouse scroll wheel for zoom
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        
        if (Mathf.Abs(scroll) > 0.01f)
        {
            targetDistance -= scroll * zoomSpeed;
            targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
        }
        
        // Smoothly interpolate distance
        currentDistance = Mathf.Lerp(currentDistance, targetDistance, Time.deltaTime * followSmoothness);
    }

    private void UpdateCameraPosition()
    {
        // Calculate the target position (with height offset)
        Vector3 targetPosition = target.position + Vector3.up * heightOffset;
        
        // Calculate desired camera position based on rotation and distance
        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
        Vector3 desiredPosition = targetPosition - (rotation * Vector3.forward * currentDistance);
        
        // Handle collision
        if (enableCollision)
        {
            desiredPosition = HandleCollision(targetPosition, desiredPosition);
        }
        
        // Smoothly move camera to desired position
        transform.position = Vector3.SmoothDamp(
            transform.position, 
            desiredPosition, 
            ref currentVelocity, 
            1f / followSmoothness
        );
        
        // Always look at target
        transform.LookAt(targetPosition);
    }

    private Vector3 HandleCollision(Vector3 targetPosition, Vector3 desiredPosition)
    {
        // Cast from target to desired camera position
        Vector3 direction = desiredPosition - targetPosition;
        float distance = direction.magnitude;
        
        RaycastHit hit;
        if (Physics.SphereCast(
            targetPosition, 
            collisionRadius, 
            direction.normalized, 
            out hit, 
            distance, 
            collisionLayers))
        {
            // Position camera just before the collision point
            return targetPosition + direction.normalized * (hit.distance - collisionRadius);
        }
        
        return desiredPosition;
    }

    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        cursorLocked = true;
        Debug.Log("Camera: Cursor locked (Press ESC to unlock)");
    }

    private void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        cursorLocked = false;
        Debug.Log("Camera: Cursor unlocked (Click to lock again)");
    }

    /// <summary>
    /// Reset camera to default position behind target
    /// </summary>
    public void ResetCamera()
    {
        if (target != null)
        {
            targetYaw = target.eulerAngles.y;
            targetPitch = 20f;
            targetDistance = defaultDistance;
        }
    }

    /// <summary>
    /// Set the camera target at runtime
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target != null)
        {
            ResetCamera();
        }
    }

    // Make sure cursor is locked when game regains focus
    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && cursorLocked && allowCursorToggle)
        {
            LockCursor();
        }
    }

    // Visualize camera settings in editor
    void OnDrawGizmosSelected()
    {
        if (target == null) return;
        
        // Draw target position
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(target.position + Vector3.up * heightOffset, 0.5f);
        
        // Draw min/max distance spheres
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(target.position + Vector3.up * heightOffset, minDistance);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(target.position + Vector3.up * heightOffset, maxDistance);
        
        // Draw collision radius
        if (enableCollision)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, collisionRadius);
        }
    }
}