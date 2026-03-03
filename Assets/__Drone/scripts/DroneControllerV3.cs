using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;

/// <summary>
/// Physics-based Drone Controller with independent pitch/roll control
/// and realistic flight dynamics.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class DroneControllerV3 : MonoBehaviour
{
    private Rigidbody rb;
    private ROSConnection ros;

    [Header("Movement Settings")]
    [Tooltip("Force applied for horizontal WASD movement")]
    public float horizontalForce = 10f;
    [Tooltip("Force applied for vertical movement (Space/LShift)")]
    public float verticalForce = 15f;
    [Tooltip("Maximum horizontal speed")]
    public float maxHorizontalSpeed = 8f;
    [Tooltip("Maximum vertical speed")]
    public float maxVerticalSpeed = 5f;

    [Header("Rotation Settings")]
    [Tooltip("Torque applied for pitch and roll (Arrow keys)")]
    public float pitchRollTorque = 20f;
    [Tooltip("Torque applied for yaw rotation (Q/E keys)")]
    public float yawTorque = 15f;
    [Tooltip("Maximum pitch/roll angle in degrees")]
    public float maxTiltAngle = 45f;

    [Header("Stabilization")]
    [Tooltip("How quickly the drone returns to horizontal when no input")]
    public float autoLevelStrength = 8f;
    [Tooltip("Damping for linear movement")]
    public float linearDamping = 1.5f;
    [Tooltip("Damping for rotation")]
    public float angularDamping = 3f;

    [Header("Physics-Based Flight")]
    [Tooltip("Thrust force generated based on tilt for realistic movement")]
    public float tiltThrustMultiplier = 15f;
    [Tooltip("Base hover force to counteract gravity")]
    public float hoverForce = 9.81f;

    [Header("ROS Topic")]
    public string cmdVelTopic = "cmd_vel";
    public bool useRosControl = true;

    // Input state
    private Vector2 pitchRollInput; // Arrow keys
    private float yawInput; // Q/E
    private Vector2 horizontalInput; // WASD
    private float verticalInput; // Space/LShift

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        // Enable gravity for realistic physics
        rb.useGravity = true;
        
        // Configure physical properties
        rb.linearDamping = linearDamping;
        rb.angularDamping = angularDamping;

        // Initialize ROS
        ros = ROSConnection.GetOrCreateInstance();
        ros.Subscribe<TwistMsg>(cmdVelTopic, OnCmdVelReceived);
    }

    void Update()
    {
        // Toggle control mode
        if (Input.GetKeyDown(KeyCode.M))
        {
            useRosControl = !useRosControl;
            Debug.Log($"Control mode: {(useRosControl ? "ROS" : "Keyboard")}");
        }

        if (!useRosControl)
        {
            HandleKeyboardInput();
        }
    }

    void FixedUpdate()
    {
        ApplyHoverForce();
        ApplyRotationControl();
        ApplyPhysicsBasedMovement();
        ApplyDirectVerticalMovement();
        ApplyAutoLeveling();
        LimitTiltAngle();
    }

    private void HandleKeyboardInput()
    {
        // Arrow keys for pitch and roll
        float pitch = 0f;
        float roll = 0f;
        
        if (Input.GetKey(KeyCode.UpArrow)) pitch = 1f;
        else if (Input.GetKey(KeyCode.DownArrow)) pitch = -1f;
        
        if (Input.GetKey(KeyCode.RightArrow)) roll = 1f;
        else if (Input.GetKey(KeyCode.LeftArrow)) roll = -1f;
        
        pitchRollInput = new Vector2(pitch, roll);

        // Q and E for yaw (rotation around own vertical axis)
        yawInput = 0f;
        if (Input.GetKey(KeyCode.E)) yawInput = 1f;
        else if (Input.GetKey(KeyCode.Q)) yawInput = -1f;

        // WASD for horizontal movement intent
        float horizontal = Input.GetAxis("Horizontal"); // A/D
        float vertical = Input.GetAxis("Vertical"); // W/S
        horizontalInput = new Vector2(horizontal, vertical);

        // Space and LShift for vertical movement
        verticalInput = 0f;
        if (Input.GetKey(KeyCode.Space)) verticalInput = 1f;
        else if (Input.GetKey(KeyCode.LeftShift)) verticalInput = -1f;
    }

    private void ApplyHoverForce()
    {
        // Apply upward force to counteract gravity
        // This creates a stable hover baseline
        rb.AddForce(Vector3.up * hoverForce * rb.mass, ForceMode.Force);
    }

    private void ApplyRotationControl()
    {
        // Pitch (around local X-axis) - controlled by up/down arrows
        rb.AddTorque(transform.right * pitchRollInput.x * pitchRollTorque, ForceMode.Acceleration);
        
        // Roll (around local Z-axis) - controlled by left/right arrows
        rb.AddTorque(transform.forward * pitchRollInput.y * pitchRollTorque, ForceMode.Acceleration);
        
        // Yaw (around local Y-axis) - controlled by Q/E
        rb.AddTorque(transform.up * yawInput * yawTorque, ForceMode.Acceleration);
    }

    private void ApplyPhysicsBasedMovement()
    {
        // Get current tilt angles
        Vector3 currentRotation = transform.rotation.eulerAngles;
        float pitchAngle = Mathf.DeltaAngle(0, currentRotation.x);
        float rollAngle = Mathf.DeltaAngle(0, currentRotation.z);

        // Calculate thrust direction based on drone's tilt
        // When tilted forward, thrust pushes forward; when tilted right, thrust pushes right
        Vector3 tiltDirection = new Vector3(rollAngle, 0, -pitchAngle).normalized;
        Vector3 worldTiltDirection = transform.TransformDirection(tiltDirection);
        
        // Apply physics-based thrust from tilt
        float tiltMagnitude = new Vector2(pitchAngle, rollAngle).magnitude / maxTiltAngle;
        rb.AddForce(worldTiltDirection * tiltMagnitude * tiltThrustMultiplier, ForceMode.Acceleration);

        // Apply additional WASD force for direct control
        Vector3 moveDirection = new Vector3(horizontalInput.x, 0, horizontalInput.y);
        Vector3 worldMoveDirection = transform.TransformDirection(moveDirection);
        worldMoveDirection.y = 0; // Keep horizontal only
        
        // Limit horizontal speed
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        if (horizontalVelocity.magnitude < maxHorizontalSpeed)
        {
            rb.AddForce(worldMoveDirection * horizontalForce, ForceMode.Acceleration);
        }
    }

    private void ApplyDirectVerticalMovement()
    {
        // Direct vertical control with Space/LShift
        if (Mathf.Abs(verticalInput) > 0.01f)
        {
            float currentVerticalSpeed = rb.linearVelocity.y;
            
            if (Mathf.Abs(currentVerticalSpeed) < maxVerticalSpeed)
            {
                rb.AddForce(Vector3.up * verticalInput * verticalForce, ForceMode.Acceleration);
            }
        }
    }

    private void ApplyAutoLeveling()
    {
        // Only apply auto-leveling when there's no pitch/roll input
        if (pitchRollInput.magnitude < 0.01f)
        {
            // Calculate the rotation needed to align with world up
            Quaternion targetRotation = Quaternion.FromToRotation(transform.up, Vector3.up) * transform.rotation;
            Quaternion deltaRotation = Quaternion.Inverse(transform.rotation) * targetRotation;
            
            // Extract torque needed for leveling (only pitch and roll, preserve yaw)
            Vector3 axis;
            float angle;
            deltaRotation.ToAngleAxis(out angle, out axis);
            
            if (angle > 180f) angle -= 360f;
            
            Vector3 localAxis = transform.InverseTransformDirection(axis);
            localAxis.y = 0; // Preserve yaw
            
            Vector3 worldAxis = transform.TransformDirection(localAxis);
            Vector3 levelingTorque = worldAxis * angle * autoLevelStrength;
            
            rb.AddTorque(levelingTorque, ForceMode.Acceleration);
            
            // Dampen pitch and roll angular velocity
            Vector3 localAngularVel = transform.InverseTransformDirection(rb.angularVelocity);
            localAngularVel.x *= 0.5f;
            localAngularVel.z *= 0.5f;
            rb.angularVelocity = transform.TransformDirection(localAngularVel);
        }
    }

    private void LimitTiltAngle()
    {
        // Prevent excessive tilt beyond maxTiltAngle
        Vector3 currentEuler = transform.rotation.eulerAngles;
        float pitch = Mathf.DeltaAngle(0, currentEuler.x);
        float roll = Mathf.DeltaAngle(0, currentEuler.z);
        
        bool needsCorrection = false;
        
        if (Mathf.Abs(pitch) > maxTiltAngle)
        {
            pitch = Mathf.Clamp(pitch, -maxTiltAngle, maxTiltAngle);
            needsCorrection = true;
        }
        
        if (Mathf.Abs(roll) > maxTiltAngle)
        {
            roll = Mathf.Clamp(roll, -maxTiltAngle, maxTiltAngle);
            needsCorrection = true;
        }
        
        if (needsCorrection)
        {
            float yaw = currentEuler.y;
            transform.rotation = Quaternion.Euler(pitch, yaw, roll);
        }
    }

    private void OnCmdVelReceived(TwistMsg msg)
    {
        if (!useRosControl) return;

        // Map ROS Twist to drone controls
        // Linear: x=forward/back, y=left/right, z=up/down
        horizontalInput = new Vector2(-(float)msg.linear.y, (float)msg.linear.x);
        verticalInput = (float)msg.linear.z;
        
        // Angular: x=pitch, y=yaw, z=roll
        pitchRollInput = new Vector2((float)msg.angular.y, (float)msg.angular.x);
        yawInput = (float)msg.angular.z;
    }

    // Debug visualization
    void OnDrawGizmos()
    {
        if (rb == null) return;
        
        // Draw velocity vector
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + rb.linearVelocity);
        
        // Draw forward direction
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 2f);
        
        // Draw up direction
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + transform.up * 2f);
    }
}