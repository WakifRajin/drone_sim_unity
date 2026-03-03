using UnityEngine;

/// <summary>
/// Top-down camera that follows a target from above.
/// Only rotates around Y-axis (yaw), ignoring pitch and roll.
/// Perfect for minimaps and overhead views.
/// </summary>
public class TopDownMinimapCamera : MonoBehaviour
{
    [Header("Target Settings")]
    [Tooltip("The object to follow (your drone)")]
    public Transform target;
    
    [Header("Camera Height")]
    [Tooltip("Height above the target")]
    public float height = 30f;
    [Tooltip("Minimum height")]
    public float minHeight = 10f;
    [Tooltip("Maximum height")]
    public float maxHeight = 100f;
    
    [Header("Follow Settings")]
    [Tooltip("How smoothly camera follows target position (0 = instant, higher = smoother)")]
    public float positionSmoothness = 5f;
    [Tooltip("Offset from target position (useful for centering minimap)")]
    public Vector2 offset = Vector2.zero;
    
    [Header("Rotation Settings")]
    [Tooltip("Follow target's Y-axis rotation (yaw only)")]
    public bool followTargetRotation = true;
    [Tooltip("How smoothly camera rotates to match target")]
    public float rotationSmoothness = 5f;
    [Tooltip("Lock to North (ignore target rotation)")]
    public bool lockToNorth = false;
    [Tooltip("Fixed rotation angle when locked to North")]
    public float northAngle = 0f;
    
    [Header("Camera Angle")]
    [Tooltip("Pitch angle of camera (90 = straight down, 45 = angled)")]
    [Range(0f, 90f)]
    public float cameraPitch = 90f;
    [Tooltip("Additional distance from target based on angle")]
    public float angleOffset = 0f;
    
    [Header("Zoom Control")]
    [Tooltip("Allow mouse scroll zoom")]
    public bool allowZoom = true;
    [Tooltip("Zoom speed with mouse scroll")]
    public float zoomSpeed = 5f;
    [Tooltip("Use smooth zoom transition")]
    public bool smoothZoom = true;
    
    [Header("Minimap Mode")]
    [Tooltip("Optimize for minimap rendering")]
    public bool minimapMode = false;
    [Tooltip("Render layer mask for minimap")]
    public LayerMask minimapLayers = -1;
    
    // Internal state
    private float currentHeight;
    private float targetHeight;
    private float currentYaw;
    private float targetYaw;
    private Vector3 velocity = Vector3.zero;

    void Start()
    {
        if (target == null)
        {
            Debug.LogError("TopDownMinimapCamera: No target assigned!");
            enabled = false;
            return;
        }
        
        // Initialize height
        currentHeight = height;
        targetHeight = height;
        
        // Initialize rotation
        if (lockToNorth)
        {
            currentYaw = northAngle;
            targetYaw = northAngle;
        }
        else if (followTargetRotation)
        {
            currentYaw = target.eulerAngles.y;
            targetYaw = currentYaw;
        }
        else
        {
            currentYaw = transform.eulerAngles.y;
            targetYaw = currentYaw;
        }
        
        // Setup minimap mode
        if (minimapMode)
        {
            Camera cam = GetComponent<Camera>();
            if (cam != null)
            {
                cam.cullingMask = minimapLayers;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            }
        }
        
        // Position camera immediately at start
        UpdateCameraPosition();
    }

    void LateUpdate()
    {
        if (target == null) return;
        
        HandleZoom();
        HandleRotation();
        UpdateCameraPosition();
    }

    private void HandleZoom()
    {
        if (!allowZoom) return;
        
        // Mouse scroll wheel for zoom
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        
        if (Mathf.Abs(scroll) > 0.01f)
        {
            targetHeight -= scroll * zoomSpeed;
            targetHeight = Mathf.Clamp(targetHeight, minHeight, maxHeight);
        }
        
        // Smoothly interpolate height or snap instantly
        if (smoothZoom)
        {
            currentHeight = Mathf.Lerp(currentHeight, targetHeight, Time.deltaTime * positionSmoothness);
        }
        else
        {
            currentHeight = targetHeight;
        }
    }

    private void HandleRotation()
    {
        // Determine target yaw based on mode
        if (lockToNorth)
        {
            // Always face north
            targetYaw = northAngle;
        }
        else if (followTargetRotation)
        {
            // Follow target's yaw only (ignore pitch and roll)
            targetYaw = target.eulerAngles.y;
        }
        // else: keep current yaw (static)
        
        // Smoothly interpolate rotation
        if (rotationSmoothness > 0f)
        {
            currentYaw = Mathf.LerpAngle(currentYaw, targetYaw, Time.deltaTime * rotationSmoothness);
        }
        else
        {
            currentYaw = targetYaw;
        }
    }

    private void UpdateCameraPosition()
    {
        // Get target position (only X and Z, ignore target's Y tilt)
        Vector3 targetPosition = target.position;
        
        // Apply horizontal offset based on camera rotation
        if (offset.magnitude > 0.01f)
        {
            Quaternion yawRotation = Quaternion.Euler(0f, currentYaw, 0f);
            Vector3 worldOffset = yawRotation * new Vector3(offset.x, 0f, offset.y);
            targetPosition += worldOffset;
        }
        
        // Calculate camera position based on pitch angle
        Vector3 desiredPosition;
        
        if (cameraPitch >= 89.9f)
        {
            // Perfectly top-down (straight above)
            desiredPosition = targetPosition + Vector3.up * currentHeight;
        }
        else
        {
            // Angled view
            float pitchRad = cameraPitch * Mathf.Deg2Rad;
            float horizontalDistance = currentHeight / Mathf.Tan(pitchRad) + angleOffset;
            
            // Position camera at an angle
            Quaternion rotation = Quaternion.Euler(0f, currentYaw, 0f);
            Vector3 offset3D = rotation * new Vector3(0f, 0f, -horizontalDistance);
            desiredPosition = targetPosition + offset3D + Vector3.up * currentHeight;
        }
        
        // Smoothly move to desired position
        if (positionSmoothness > 0f)
        {
            transform.position = Vector3.SmoothDamp(
                transform.position,
                desiredPosition,
                ref velocity,
                1f / positionSmoothness
            );
        }
        else
        {
            transform.position = desiredPosition;
        }
        
        // Set camera rotation (only pitch and yaw, no roll)
        transform.rotation = Quaternion.Euler(cameraPitch, currentYaw, 0f);
    }

    /// <summary>
    /// Set the height of the camera
    /// </summary>
    public void SetHeight(float newHeight)
    {
        targetHeight = Mathf.Clamp(newHeight, minHeight, maxHeight);
    }

    /// <summary>
    /// Toggle between following rotation and locking to north
    /// </summary>
    public void ToggleRotationLock()
    {
        lockToNorth = !lockToNorth;
        Debug.Log($"TopDownCamera: Lock to North = {lockToNorth}");
    }

    /// <summary>
    /// Set the camera target at runtime
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target != null && followTargetRotation && !lockToNorth)
        {
            currentYaw = target.eulerAngles.y;
            targetYaw = currentYaw;
        }
    }

    /// <summary>
    /// Reset zoom to default height
    /// </summary>
    public void ResetZoom()
    {
        targetHeight = height;
    }

    // Visualize camera settings in editor
    void OnDrawGizmos()
    {
        if (target == null) return;
        
        // Draw target position marker
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(target.position, 1f);
        
        // Draw camera to target line
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, target.position);
        
        // Draw height indicators
        Gizmos.color = Color.green;
        Vector3 heightPos = target.position + Vector3.up * currentHeight;
        Gizmos.DrawWireSphere(heightPos, 0.5f);
        
        // Draw field of view cone (approximate)
        Camera cam = GetComponent<Camera>();
        if (cam != null)
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            float fovRadius = currentHeight * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            
            // Draw FOV circle at target height
            DrawCircle(target.position, fovRadius, 32);
        }
    }

    private void DrawCircle(Vector3 center, float radius, int segments)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);
        
        for (int i = 1; i <= segments; i++)
        {
            float angle = angleStep * i * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(
                Mathf.Cos(angle) * radius,
                0,
                Mathf.Sin(angle) * radius
            );
            
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }
}