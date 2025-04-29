using UnityEngine;
using UnityEditor;

/// <summary>
/// Controls camera movement in an orbital pattern around a target point
/// Provides smooth circular movement with configurable height and distance
/// </summary>
public class CameraRotate : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform target;         // The point for the camera to orbit around
    [Tooltip("Should camera automatically rotate around target")]
    [SerializeField] private bool autoRotate = false;  // Toggle for automatic rotation - default to off

    [Header("Rotation Settings")]
    [Tooltip("Speed of rotation in degrees per second")]
    [SerializeField] private float rotationSpeed = 1f; // Speed of orbital rotation in degrees per second
    [SerializeField] private float distance = 10f;     // Radius of orbital circle
    [SerializeField] private float height = 2f;        // Vertical offset from target
    
    private float currentRotation = 0f;                // Current angle of rotation in degrees
    private bool isInitialized = false;                // Track if we've initialized the position

    // Properties with Inspector visibility
    public bool AutoRotate 
    { 
        get => autoRotate; 
        set 
        {
            // Only update if there's a change
            if (autoRotate != value)
            {
                autoRotate = value;
                
                // Make sure we're initialized before activating
                if (autoRotate && !isInitialized)
                {
                    Initialize();
                }
                
                // Update position immediately if activating
                if (autoRotate)
                {
                    UpdatePosition();
                }
            }
        }
    }
    
    public float RotationSpeed
    {
        get => rotationSpeed;
        set => rotationSpeed = value;
    }
    
    public float Distance
    {
        get => distance;
        set
        {
            distance = value;
            if (isInitialized) UpdatePosition();
        }
    }
    
    public float Height
    {
        get => height;
        set
        {
            height = value;
            if (isInitialized) UpdatePosition();
        }
    }

    /// <summary>
    /// Initializes camera position and creates default target if none specified
    /// </summary>
    private void Start()
    {
        Initialize();
    }
    
    /// <summary>
    /// Set up the camera rotation system
    /// </summary>
    public void Initialize()
    {
        if (isInitialized) return;
        
        // Create default target at origin if none provided
        if (target == null)
        {
            GameObject emptyTarget = new GameObject("CameraTarget");
            target = emptyTarget.transform;
            target.position = Vector3.zero;
        }

        // Initialize camera position
        UpdatePosition();
        isInitialized = true;
    }

    /// <summary>
    /// Updates camera rotation and position each frame when autoRotate is enabled
    /// </summary>
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

    /// <summary>
    /// Enable or disable camera rotation
    /// </summary>
    /// <param name="enable">Whether rotation should be enabled</param>
    public void ToggleRotation(bool enable)
    {
        AutoRotate = enable;  // Use the property to ensure proper setup
    }
    
    /// <summary>
    /// Configures the camera rotation with common settings at once
    /// </summary>
    /// <param name="newTarget">Target to rotate around</param>
    /// <param name="speed">Rotation speed</param>
    /// <param name="distanceFromTarget">Distance from target</param>
    /// <param name="heightOffset">Height offset from target</param>
    /// <param name="startRotating">Whether to enable rotation immediately</param>
    public void ConfigureRotation(Transform newTarget, float speed, float distanceFromTarget, float heightOffset, bool startRotating = true)
    {
        // Update all properties
        target = newTarget;
        rotationSpeed = speed;
        distance = distanceFromTarget;
        height = heightOffset;
        
        // Make sure we're initialized
        Initialize();
        
        // Enable rotation if requested (use property to ensure proper handling)
        AutoRotate = startRotating;
    }

    /// <summary>
    /// Calculates and sets camera position based on current rotation angle
    /// Uses circular motion equations to determine position
    /// </summary>
    private void UpdatePosition()
    {
        if (target == null) return;
        
        // Calculate new position using parametric equations for circular motion
        float x = target.position.x + distance * Mathf.Sin(currentRotation * Mathf.Deg2Rad);
        float z = target.position.z + distance * Mathf.Cos(currentRotation * Mathf.Deg2Rad);
        
        // Update camera position with calculated coordinates and height offset
        transform.position = new Vector3(x, target.position.y + height, z);
        
        // Ensure camera is looking at target
        transform.LookAt(target);
    }

    /// <summary>
    /// Allows external control of rotation speed
    /// </summary>
    public void SetRotationSpeed(float speed)
    {
        rotationSpeed = speed;
    }

    /// <summary>
    /// Sets the target for the camera to orbit around
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (isInitialized)
        {
            UpdatePosition();
        }
    }

    /// <summary>
    /// Sets the height offset of the camera from the target
    /// </summary>
    public void SetHeight(float newHeight)
    {
        Height = newHeight;  // Use property
    }

    /// <summary>
    /// Sets the distance of the camera from the target
    /// </summary>
    public void SetDistance(float newDistance)
    {
        Distance = newDistance;  // Use property
    }

#if UNITY_EDITOR
    /// <summary>
    /// Draws debug visualization in the scene view
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (target == null) return;
        
        // Draw orbit circle
        Handles.color = Color.cyan;
        Handles.DrawWireDisc(new Vector3(target.position.x, target.position.y + height, target.position.z), 
                            Vector3.up, distance);
        
        // Draw line to current position if initialized
        if (isInitialized)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(target.position, transform.position);
        }
    }
#endif
}