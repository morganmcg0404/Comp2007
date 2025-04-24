using UnityEngine;

/// Controls camera movement in an orbital pattern around a target point
/// Provides smooth circular movement with configurable height and distance
public class CameraRotate : MonoBehaviour
{
    [SerializeField] private Transform target;         // The point for the camera to orbit around
    [SerializeField] private float rotationSpeed = 1f; // Speed of orbital rotation in degrees per second
    [SerializeField] private float distance = 10f;     // Radius of orbital circle
    [SerializeField] private float height = 2f;        // Vertical offset from target
    [SerializeField] private bool autoRotate = true;   // Toggle for automatic rotation
    
    private float currentRotation = 0f;                // Current angle of rotation in degrees

    /// Initializes camera position and creates default target if none specified
    private void Start()
    {
        // Create default target at origin if none provided
        if (target == null)
        {
            GameObject emptyTarget = new GameObject("CameraTarget");
            target = emptyTarget.transform;
            target.position = Vector3.zero;
        }

        // Initialize camera position
        UpdatePosition();
    }

    /// Updates camera rotation and position each frame when autoRotate is enabled
    private void Update()
    {
        if (autoRotate)
        {
            // Increment rotation based on speed and time
            currentRotation += rotationSpeed * Time.deltaTime;
            
            // Wrap rotation angle between 0-360 degrees
            if (currentRotation >= 360f)
                currentRotation -= 360f;

            // Apply updated position
            UpdatePosition();
        }
    }

    /// Calculates and sets camera position based on current rotation angle
    /// Uses circular motion equations to determine position
    private void UpdatePosition()
    {
        // Calculate new position using parametric equations for circular motion
        float x = target.position.x + distance * Mathf.Sin(currentRotation * Mathf.Deg2Rad);
        float z = target.position.z + distance * Mathf.Cos(currentRotation * Mathf.Deg2Rad);
        
        // Update camera position with calculated coordinates and height offset
        transform.position = new Vector3(x, target.position.y + height, z);
        
        // Ensure camera is looking at target
        transform.LookAt(target);
    }

    /// Allows external control of rotation speed
    public void SetRotationSpeed(float speed)
    {
        rotationSpeed = speed;
    }
}