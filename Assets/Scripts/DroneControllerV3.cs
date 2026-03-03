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
    [Tooltip("Total rotor thrust force (simulates quadcopter motors) - set to ~9.81 to balance gravity")]
    public float rotorThrust = 9.81f;
    [Tooltip("Additional hover force when Space is held (relative to base thrust)")]
    public float additionalThrustMultiplier = 1.5f;
    [Tooltip("Compensate for tilt to maintain altitude")]
    public bool altitudeCompensation = true;

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
        ApplyRotationControl();
        ApplyRealisticRotorThrust();
        ApplyDirectMovement();
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

    private void ApplyRotationControl()
    {
        // Pitch (around local X-axis) - controlled by up/down arrows
        // Positive pitch input should pitch forward (nose down)
        rb.AddTorque(transform.right * pitchRollInput.x * pitchRollTorque, ForceMode.Acceleration);
        
        // Roll (around local Z-axis) - controlled by left/right arrows
        // Negative torque for correct left/right banking
        rb.AddTorque(transform.forward * -pitchRollInput.y * pitchRollTorque, ForceMode.Acceleration);
        
        // Yaw (around local Y-axis) - controlled by Q/E
        rb.AddTorque(transform.up * yawInput * yawTorque, ForceMode.Acceleration);
    }

    private void ApplyRealisticRotorThrust()
    {
        // REALISTIC QUADCOPTER PHYSICS:
        // Rotors always push in the direction of the drone's "up" vector
        // When tilted, this creates both horizontal movement AND upward lift
        
        // Calculate base thrust (should roughly equal gravity to hover)
        float currentThrust = rotorThrust;
        
        // Add additional thrust when vertical input is positive (Space key)
        // Reduce thrust when vertical input is negative (LShift key)
        if (verticalInput > 0.01f)
        {
            currentThrust *= (1f + verticalInput * additionalThrustMultiplier);
        }
        else if (verticalInput < -0.01f)
        {
            currentThrust *= (1f + verticalInput * 0.5f); // Reduce thrust for descent
            currentThrust = Mathf.Max(currentThrust, 0f); // Prevent negative thrust
        }
        
        // Altitude compensation: increase thrust when tilted to maintain altitude
        if (altitudeCompensation)
        {
            // Get the angle between transform.up and world up
            float tiltAngle = Vector3.Angle(transform.up, Vector3.up);
            // Compensate for loss of vertical thrust component
            // When tilted 45°, cos(45°) ≈ 0.707, so we need ~1.41x thrust
            float compensationFactor = 1f / Mathf.Cos(tiltAngle * Mathf.Deg2Rad);
            compensationFactor = Mathf.Clamp(compensationFactor, 1f, 2f); // Limit to 2x max
            currentThrust *= compensationFactor;
        }
        
        // Apply thrust in the direction the rotors are pointing (transform.up)
        Vector3 thrustDirection = transform.up;
        rb.AddForce(thrustDirection * currentThrust * rb.mass, ForceMode.Force);
        
        // When drone tilts:
        // - Forward tilt (nose down) → thrust points forward+up → moves forward
        // - Left tilt → thrust points left+up → moves left
        // - Right tilt → thrust points right+up → moves right
        // - Backward tilt (nose up) → thrust points backward+up → moves backward
    }

    private void ApplyDirectMovement()
    {
        // Apply additional WASD force for supplemental control
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

    private void ApplyAutoLeveling()
    {
        // Only apply auto-leveling when there's no pitch/roll input
        if (pitchRollInput.magnitude < 0.01f)
        {
            // Get current euler angles
            Vector3 currentEuler = transform.rotation.eulerAngles;
            
            // Convert to -180 to 180 range for proper angle calculation
            float pitch = Mathf.DeltaAngle(0, currentEuler.x);
            float roll = Mathf.DeltaAngle(0, currentEuler.z);
            
            // Apply corrective torque to level out
            // Negative values to oppose the current tilt
            Vector3 correctiveTorque = Vector3.zero;
            correctiveTorque += transform.right * -pitch * autoLevelStrength;
            correctiveTorque += transform.forward * -roll * autoLevelStrength;
            
            rb.AddTorque(correctiveTorque, ForceMode.Acceleration);
            
            // Dampen pitch and roll angular velocity
            Vector3 localAngularVel = transform.InverseTransformDirection(rb.angularVelocity);
            localAngularVel.x *= 0.3f; // Stronger damping for faster leveling
            localAngularVel.z *= 0.3f;
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
        Vector3 angVel = rb.angularVelocity;
        
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
            
            // Zero out angular velocity in pitch and roll to prevent overshoot
            Vector3 localAngVel = transform.InverseTransformDirection(angVel);
            localAngVel.x = 0;
            localAngVel.z = 0;
            rb.angularVelocity = transform.TransformDirection(localAngVel);
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
        
        // Draw velocity vector (green)
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + rb.linearVelocity);
        
        // Draw forward direction (blue)
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 2f);
        
        // Draw up direction / rotor thrust direction (red)
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + transform.up * 2f);
        
        // Draw rotor thrust force (yellow)
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, transform.up * 3f);
    }
}