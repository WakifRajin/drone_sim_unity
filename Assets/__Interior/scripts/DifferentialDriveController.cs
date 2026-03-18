using UnityEngine;

public class DifferentialDriveController : MonoBehaviour
{
    [Header("Wheel Configuration")]
    public float wheelBase = 0.5f;          // Distance between left and right wheels (meters)
    public float wheelRadius = 0.08f;       // Wheel radius (meters)
    public float maxWheelSpeed = 10f;       // Maximum wheel speed (rad/s)
    
    [Header("Motor Control")]
    public float acceleration = 20f;        // Acceleration factor
    public float deceleration = 15f;        // Deceleration factor
    public float rotationSpeedMultiplier = 0.5f;  // Rotation speed multiplier (0-1)
    
    private Rigidbody rb;
    private float leftWheelSpeed = 0f;      // Left wheel angular velocity (rad/s)
    private float rightWheelSpeed = 0f;     // Right wheel angular velocity (rad/s)
    private float targetLeftSpeed = 0f;
    private float targetRightSpeed = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody component required!");
        }
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    void Update()
    {
        HandleInput();
    }

    void FixedUpdate()
    {
        UpdateWheelSpeeds();
        ApplyMotion();
    }

    void HandleInput()
    {
        float moveInput = 0f;
        float turnInput = 0f;

        // WASD Controls
        if (Input.GetKey(KeyCode.W))
            moveInput = 1f;
        if (Input.GetKey(KeyCode.S))
            moveInput = -1f;

        if (Input.GetKey(KeyCode.A))
            turnInput = 1f;
        if (Input.GetKey(KeyCode.D))
            turnInput = -1f;

        // Calculate target wheel speeds
        float baseSpeed = moveInput * maxWheelSpeed;
        float turnSpeed = turnInput * maxWheelSpeed * rotationSpeedMultiplier;

        targetLeftSpeed = baseSpeed + turnSpeed;
        targetRightSpeed = baseSpeed - turnSpeed;

        // Clamp speeds
        targetLeftSpeed = Mathf.Clamp(targetLeftSpeed, -maxWheelSpeed, maxWheelSpeed);
        targetRightSpeed = Mathf.Clamp(targetRightSpeed, -maxWheelSpeed, maxWheelSpeed);
    }

    void UpdateWheelSpeeds()
    {
        // Smooth acceleration/deceleration
        leftWheelSpeed = Mathf.Lerp(leftWheelSpeed, targetLeftSpeed, acceleration * Time.fixedDeltaTime);
        rightWheelSpeed = Mathf.Lerp(rightWheelSpeed, targetRightSpeed, acceleration * Time.fixedDeltaTime);
    }

    void ApplyMotion()
    {
        // Convert wheel angular speeds to linear speeds
        float vL = leftWheelSpeed * wheelRadius;
        float vR = rightWheelSpeed * wheelRadius;

        // Differential drive kinematics
        float linearVelocity = (vR + vL) / 2f;
        float angularVelocity = (vR - vL) / wheelBase;

        // Apply linear motion
        Vector3 velocity = transform.forward * linearVelocity;
        velocity.y = rb.linearVelocity.y; // Preserve vertical velocity (gravity)
        rb.linearVelocity = velocity;

        // Apply angular motion (rotation)
        rb.angularVelocity = new Vector3(0, angularVelocity, 0);
    }

    // Optional: Get current wheel speeds for debugging
    public float GetLeftWheelSpeed() => leftWheelSpeed;
    public float GetRightWheelSpeed() => rightWheelSpeed;

    // Optional: Get current linear and angular velocities
    public float GetLinearVelocity() => (rightWheelSpeed * wheelRadius + leftWheelSpeed * wheelRadius) / 2f;
    public float GetAngularVelocity() => (rightWheelSpeed * wheelRadius - leftWheelSpeed * wheelRadius) / wheelBase;
}