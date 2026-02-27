using UnityEngine;

public sealed class DroneFollowCam : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform droneTransform;
    [SerializeField] private Vector3 offset = new Vector3(0f, 2f, -5f);

    [Header("Movement Smoothness")]
    [Range(0f, 1f)] [SerializeField] private float positionSmoothTime = 0.2f;
    [Range(0f, 20f)] [SerializeField] private float rotationSpeed = 5f;

    private Vector3 _currentVelocity = Vector3.zero;

    private void LateUpdate()
    {
        if (!droneTransform) return;

        HandlePosition();
        HandleRotation();
    }

    private void HandlePosition()
    {
        // Calculate the ideal position based on the drone's current rotation
        Vector3 targetPosition = droneTransform.TransformPoint(offset);

        // Smoothly move the camera to that target position
        transform.position = Vector3.SmoothDamp(
            transform.position, 
            targetPosition, 
            ref _currentVelocity, 
            positionSmoothTime
        );
    }

    private void HandleRotation()
    {
        // Determine the direction to look
        Vector3 direction = droneTransform.position - transform.position;
        Quaternion targetRotation = Quaternion.LookRotation(direction, droneTransform.up);

        // Slerp provides that fluid, non-snappy rotation
        transform.rotation = Quaternion.Slerp(
            transform.rotation, 
            targetRotation, 
            rotationSpeed * Time.deltaTime
        );
    }
}