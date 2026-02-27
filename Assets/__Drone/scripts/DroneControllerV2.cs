using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;

[RequireComponent(typeof(Rigidbody))]
public class DroneControllerV2 : MonoBehaviour
{
    private Rigidbody rb;
    private ROSConnection ros;

    [Header("Movement Settings")]
    public float moveForce = 15f;
    public float maxSpeed = 10f;
    public float angularSpeed = 90f;
    
    [Header("Smoothness & Leveling")]
    [Tooltip("How aggressively the drone levels itself")]
    public float tiltRestorationForce = 10f; 
    [Tooltip("Reduces 'floatiness'")]
    public float linearDamping = 2f; 
    public float angularDamping = 2f;

    [Header("ROS Topic")]
    public string cmdVelTopic = "cmd_vel";
    public bool useRosControl = true;

    private Vector3 targetLocalVelocity;
    private float targetYawSpeed;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false; 
        
        // Physics setup for "Fluid" feel
        rb.linearDamping = linearDamping;
        rb.angularDamping = angularDamping;
        rb.maxLinearVelocity = maxSpeed;

        ros = ROSConnection.GetOrCreateInstance();
        ros.Subscribe<TwistMsg>(cmdVelTopic, OnCmdVelReceived);
    }

    void Update()
    {
        if (!useRosControl) HandleKeyboardInput();
        
        if (Input.GetKeyDown(KeyCode.M))
        {
            useRosControl = !useRosControl;
            Debug.Log($"Control mode: {(useRosControl ? "ROS" : "Keyboard")}");
        }
    }

    void FixedUpdate()
    {
        ApplyMovement();
        ApplySelfLeveling();
    }

    private void ApplyMovement()
    {
        // Convert local target velocity to world space force
        Vector3 worldTargetVel = transform.TransformDirection(targetLocalVelocity);
        
        // Use a simple force application to reach target velocity
        Vector3 velocityError = worldTargetVel - rb.linearVelocity;
        rb.AddForce(velocityError * moveForce, ForceMode.Acceleration);

        // Apply Yaw
        rb.AddTorque(Vector3.up * targetYawSpeed, ForceMode.Acceleration);
    }

    private void ApplySelfLeveling()
    {
        // Calculate the rotation needed to get 'Up' to point toward world Up
        // This fixes the "bumped" or "tilted" drone automatically
        Quaternion selfRightingRotation = Quaternion.FromToRotation(transform.up, Vector3.up);
        
        // Apply a torque based on that rotation
        rb.AddTorque(new Vector3(selfRightingRotation.x, 0, selfRightingRotation.z) * tiltRestorationForce, ForceMode.Acceleration);

        // Counteract any "drifting" rotation on X and Z
        Vector3 angularVel = rb.angularVelocity;
        rb.AddTorque(new Vector3(-angularVel.x, 0, -angularVel.z) * (tiltRestorationForce * 0.5f), ForceMode.Acceleration);
    }

    // ... HandleKeyboardInput and OnCmdVelReceived remain mostly the same, 
    // but update 'targetLocalVelocity' and 'targetYawSpeed' instead of setting velocity directly.
    
    void HandleKeyboardInput()
    {
        float horizontal = Input.GetAxis("Horizontal"); 
        float vertical = Input.GetAxis("Vertical");
        float upDown = (Input.GetKey(KeyCode.E) ? 1 : 0) - (Input.GetKey(KeyCode.Q) ? 1 : 0);
        float yaw = (Input.GetKey(KeyCode.RightArrow) ? 1 : 0) - (Input.GetKey(KeyCode.LeftArrow) ? 1 : 0);

        targetLocalVelocity = new Vector3(horizontal, upDown, vertical) * maxSpeed;
        targetYawSpeed = yaw * angularSpeed;
    }

    void OnCmdVelReceived(TwistMsg msg)
    {
        targetLocalVelocity = new Vector3(-(float)msg.linear.y, (float)msg.linear.z, (float)msg.linear.x) * maxSpeed;
        targetYawSpeed = (float)msg.angular.z * Mathf.Rad2Deg;
    }
}