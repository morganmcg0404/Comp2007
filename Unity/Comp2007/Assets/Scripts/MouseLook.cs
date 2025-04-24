using UnityEngine;

public class MouseLook : MonoBehaviour
{
    [Header("Sensitivity Settings")]
    [Range(0.1f, 10.0f)]
    [SerializeField] private float mouseSensitivity = 1.0f;
    [SerializeField] private float baseLookSpeed = 100.0f;  // Base speed multiplier
    [SerializeField] private bool invertY = false;

    [Header("Camera Settings")]
    [SerializeField] private Camera playerCamera;
    [Range(50f, 120f)]
    [SerializeField] private float baseFOV = 60f;
    [SerializeField] private bool maintainConstantFOV = false; // Set this to false to prevent forcing FOV
    
    [Header("References")]
    public Transform playerBody;
    
    private float xRotation = 0f;
    private WeaponCamera weaponCameraScript; // Reference to WeaponCamera script

    private void Start()
    {
        // Apply saved settings
        LoadSettings();
        
        // Lock the cursor
        Cursor.lockState = CursorLockMode.Locked;
        
        // Find WeaponCamera if it exists
        weaponCameraScript = FindFirstObjectByType<WeaponCamera>();
    }

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
    
    // Load settings from PlayerPrefs
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
    
    // Set sensitivity directly (for use by settings menu)
    public void SetSensitivity(float sensitivity)
    {
        mouseSensitivity = Mathf.Clamp(sensitivity, 0.1f, 10.0f);
    }
    
    // Set invert Y directly (for use by settings menu)
    public void SetInvertY(bool invert)
    {
        invertY = invert;
    }
    
    // Set FOV directly (for use by settings menu)
    public void SetFOV(float fov)
    {
        if (playerCamera != null)
        {
            // Clamp FOV between 50 and 120
            baseFOV = Mathf.Clamp(fov, 50f, 120f);
            playerCamera.fieldOfView = baseFOV;
            
            // Update weapon camera FOV too if it exists
            if (weaponCameraScript != null)
            {
                weaponCameraScript.UpdateFOV(baseFOV);
            }
        }
    }

    // Get current sensitivity
    public float GetSensitivity()
    {
        return mouseSensitivity;
    }
}