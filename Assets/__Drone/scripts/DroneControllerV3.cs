using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;

/// <summary>
/// Improved Drone Controller focusing on stable axial Y-axis rotation (Yaw)
/// and consistent local-space movement.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class DroneControllerV3 : MonoBehaviour
{
    private Rigidbody rb;
    private ROSConnection ros;

    [Header("Movement Settings")]
    public float moveForce = 15f;
    public float maxSpeed = 10f;
    public float angularSpeed = 90f;
    
    [Header("Smoothness & Leveling")]
    [Tooltip("How aggressively the drone levels itself on X and Z axes")]
    public float tiltRestorationForce = 15f; 
    [Tooltip("Resistance to linear movement")]
    public float linearDamping = 2f; 
    [Tooltip("Resistance to rotation")]
    public float angularDamping = 2f;

    [Header("ROS Topic")]
    public string cmdVelTopic = "cmd_vel";
    public bool useRosControl = true;

    private Vector3 targetLocalVelocity;
    private float targetYawSpeed;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        // Ensure gravity doesn't interfere with our manual flight logic
        rb.useGravity = false; 
        
        // Physical properties configuration
        rb.linearDamping = linearDamping;
        rb.angularDamping = angularDamping;
        rb.maxLinearVelocity = maxSpeed;

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
        ApplyMovement();
        ApplySelfLeveling();
        ConstrainRotation();
    }

    private void ApplyMovement()
    {
        // 1. LINEAR MOVEMENT
        // Convert the target local velocity (relative to drone orientation) to world space
        Vector3 worldTargetVel = transform.TransformDirection(targetLocalVelocity);
        
        // Calculate the difference between where we want to be and current velocity
        Vector3 velocityError = worldTargetVel - rb.linearVelocity;
        
        // Apply force to reach target velocity
        rb.AddForce(velocityError * moveForce, ForceMode.Acceleration);

        // 2. YAW ROTATION (AXIAL)
        // By multiplying by transform.up, we ensure the torque is applied 
        // strictly around the drone's current vertical axis.
        rb.AddTorque(transform.up * targetYawSpeed, ForceMode.Acceleration);
    }

    private void ApplySelfLeveling()
    {
        // Calculate rotation needed to align local Up with world Up
        Quaternion selfRightingRotation = Quaternion.FromToRotation(transform.up, Vector3.up);
        
        // Apply leveling torque only on X and Z (pitch/roll)
        // This prevents the drone from flipping over while allowing the Y-axis (Yaw) to stay free
        Vector3 torque = new Vector3(selfRightingRotation.x, 0, selfRightingRotation.z) * tiltRestorationForce;
        rb.AddTorque(torque, ForceMode.Acceleration);
    }

    private void ConstrainRotation()
    {
        // To ensure "Axial" rotation, we aggressively dampen any angular velocity 
        // that isn't on the local Y axis. This stops "wobbling" or off-axis spinning.
        Vector3 localAngularVel = transform.InverseTransformDirection(rb.angularVelocity);
        
        // Effectively kill X and Z angular momentum in local space
        localAngularVel.x *= 0.1f; 
        localAngularVel.z *= 0.1f;
        
        rb.angularVelocity = transform.TransformDirection(localAngularVel);
    }

    private void HandleKeyboardInput()
    {
        // Strafe/Forward movement
        float strafe = Input.GetAxis("Horizontal"); 
        float forward = Input.GetAxis("Vertical");
        
        // Altitude (E/Q)
        float upDown = 0;
        if (Input.GetKey(KeyCode.E)) upDown = 1f;
        else if (Input.GetKey(KeyCode.Q)) upDown = -1f;

        // Yaw (Rotation) - Arrows
        float yawInput = 0;
        if (Input.GetKey(KeyCode.RightArrow)) yawInput = 1f;
        else if (Input.GetKey(KeyCode.LeftArrow)) yawInput = -1f;

        // Map to target vectors
        targetLocalVelocity = new Vector3(strafe, upDown, forward) * maxSpeed;
        targetYawSpeed = yawInput * angularSpeed;
    }

    private void OnCmdVelReceived(TwistMsg msg)
    {
        if (!useRosControl) return;

        // ROS Standard: X = Forward, Y = Left, Z = Up
        // Unity Standard: Z = Forward, X = Right, Y = Up
        // Mapping: 
        // ROS.linear.x -> Unity.local.z
        // ROS.linear.y -> Unity.local.x (Inverted because ROS Y is Left)
        // ROS.linear.z -> Unity.local.y
        
        float localX = -(float)msg.linear.y;
        float localY = (float)msg.linear.z;
        float localZ = (float)msg.linear.x;

        targetLocalVelocity = new Vector3(localX, localY, localZ) * maxSpeed;
        
        // Angular Z in ROS is Yaw (rotation around Vertical axis)
        // ROS uses Radians, Unity uses Degrees for this calculation context
        targetYawSpeed = (float)msg.angular.z * Mathf.Rad2Deg;
    }
}