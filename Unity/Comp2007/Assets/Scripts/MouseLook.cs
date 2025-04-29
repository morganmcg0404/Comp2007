using UnityEngine;

/// <summary>
/// Controls camera movement based on mouse input for first-person camera controls
/// Handles mouse sensitivity, FOV settings, and Y-axis inversion preferences
/// </summary>
public class MouseLook : MonoBehaviour
{
    [Header("Sensitivity Settings")]
    [Range(0.1f, 10.0f)]
    /// <summary>
    /// Mouse sensitivity multiplier affecting how quickly the camera rotates
    /// </summary>
    [SerializeField] private float mouseSensitivity = 1.0f;
    
    /// <summary>
    /// Base rotation speed before sensitivity is applied
    /// </summary>
    [SerializeField] private float baseLookSpeed = 100.0f;  // Base speed multiplier
    
    /// <summary>
    /// Whether to invert the vertical (Y) mouse axis
    /// </summary>
    [SerializeField] private bool invertY = false;

    [Header("Camera Settings")]
    /// <summary>
    /// Reference to the player's camera component
    /// </summary>
    [SerializeField] private Camera playerCamera;
    
    [Range(50f, 120f)]
    /// <summary>
    /// Base field of view in degrees
    /// </summary>
    [SerializeField] private float baseFOV = 60f;
    
    /// <summary>
    /// If true, will ensure FOV always returns to base value when not aiming
    /// </summary>
    [SerializeField] private bool maintainConstantFOV = false; // Set this to false to prevent forcing FOV
    
    [Header("References")]
    /// <summary>
    /// Reference to the player's body transform for horizontal rotation
    /// </summary>
    public Transform playerBody;
    
    /// <summary>
    /// Current vertical rotation in degrees
    /// </summary>
    private float xRotation = 0f;

    /// <summary>
    /// Initializes the mouse look system by loading saved settings and locking the cursor
    /// </summary>
    private void Start()
    {
        // Apply saved settings
        LoadSettings();
        
        // Lock the cursor
        Cursor.lockState = CursorLockMode.Locked;
    }

    /// <summary>
    /// Processes mouse input every frame and updates camera rotation
    /// Skips processing if the game is paused
    /// </summary>
    private void Update()
    {
        // Skip mouse look if game is paused
        if (Time.timeScale == 0)
            return;
            
        // Calculate look speed based on base speed and sensitivity
        float effectiveLookSpeed = baseLookSpeed * mouseSensitivity;
        
        // Get mouse input
        float mouseX = Input.GetAxis("Mouse X") * effectiveLookSpeed * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * effectiveLookSpeed * Time.deltaTime;

        // Apply inversion if enabled
        if (invertY)
            mouseY = -mouseY;

        // Handle looking up and down
        xRotation -= mouseY; // Subtract to invert axis
        xRotation = Mathf.Clamp(xRotation, -89f, 89f); // Clamp to slightly less than 90 degrees to prevent distortion
        
        // Apply rotation using Quaternion for smoother rotation
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        
        // Handle rotation around Y axis (looking left and right)
        playerBody.Rotate(Vector3.up * mouseX);
        
        // Update camera FOV as needed
        UpdateCameraFOV();
    }
    
    /// <summary>
    /// Ensures camera maintains the correct field of view when not aiming down sights
    /// Checks for active AimDownSights components to avoid conflicting with aiming
    /// </summary>
    private void UpdateCameraFOV()
    {
        // Only maintain FOV if enabled AND no AimDownSights is active
        if (maintainConstantFOV && playerCamera != null)
        {
            // Check if we're currently aiming down sights
            bool isAiming = false;
            AimDownSights[] adsScripts = FindObjectsByType<AimDownSights>(FindObjectsSortMode.None);
            foreach (AimDownSights ads in adsScripts)
            {
                if (ads.IsAiming())
                {
                    isAiming = true;
                    break;
                }
            }
            
            // Only force FOV if we're not aiming
            if (!isAiming)
            {
                float currentFOV = playerCamera.fieldOfView;
                if (Mathf.Abs(currentFOV - baseFOV) > 0.1f)
                {
                    SetFOV(baseFOV);
                }
            }
        }
    }
    
    /// <summary>
    /// Loads user settings from PlayerPrefs including sensitivity, inversion, and FOV
    /// </summary>
    public void LoadSettings()
    {
        mouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 1.0f);
        invertY = PlayerPrefs.GetInt("InvertY", 0) == 1;
        
        if (playerCamera != null)
        {
            // Load FOV from PlayerPrefs with constrained range
            float savedFOV = PlayerPrefs.GetFloat("FOV", baseFOV);
            savedFOV = Mathf.Clamp(savedFOV, 50f, 120f);
            baseFOV = savedFOV;
            
            // Only set camera FOV directly if we're not in a special state like aiming
            bool isAiming = false;
            AimDownSights[] adsScripts = FindObjectsByType<AimDownSights>(FindObjectsSortMode.None);
            foreach (AimDownSights ads in adsScripts)
            {
                if (ads.IsAiming())
                {
                    isAiming = true;
                    break;
                }
            }
            
            if (!isAiming)
            {
                playerCamera.fieldOfView = savedFOV;
            }
        }
    }
    
    /// <summary>
    /// Sets mouse sensitivity with range safety checks
    /// </summary>
    /// <param name="sensitivity">Sensitivity value between 0.1 and 10.0</param>
    public void SetSensitivity(float sensitivity)
    {
        mouseSensitivity = Mathf.Clamp(sensitivity, 0.1f, 10.0f);
    }
    
    /// <summary>
    /// Sets whether the Y axis should be inverted
    /// </summary>
    /// <param name="invert">True to invert Y axis, false for normal controls</param>
    public void SetInvertY(bool invert)
    {
        invertY = invert;
    }
    
    /// <summary>
    /// Sets the camera field of view with range safety checks
    /// </summary>
    /// <param name="fov">Field of view in degrees (50-120)</param>
    public void SetFOV(float fov)
    {
        if (playerCamera != null)
        {
            // Clamp FOV between 50 and 120
            baseFOV = Mathf.Clamp(fov, 50f, 120f);
            playerCamera.fieldOfView = baseFOV;
        }
    }

    /// <summary>
    /// Gets the current mouse sensitivity value
    /// </summary>
    /// <returns>Current mouse sensitivity multiplier</returns>
    public float GetSensitivity()
    {
        return mouseSensitivity;
    }
}