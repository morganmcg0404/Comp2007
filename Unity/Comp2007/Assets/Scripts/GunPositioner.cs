using UnityEngine;

/// <summary>
/// Handles weapon positioning, transitions between hip, ADS, and inactive states
/// Provides smooth movement and rotation for weapons with clipping prevention
/// </summary>
public class GunPositioner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform playerBodyTransform; 
    [SerializeField] private WeaponManager weaponManager;
    
    [Header("Position Settings")]
    [SerializeField] private Vector3 activePosition = new Vector3(0.2f, -0.15f, 0.5f); // Hip position when active
    [SerializeField] private Vector3 inactivePosition = new Vector3(0.2f, -0.5f, 0.3f); // Position when holstered
    
    [Header("ADS Position Settings")]
    [SerializeField] private Vector3 adsPosition = new Vector3(0f, -0.07f, 0.35f); // Position when aiming down sights
    [SerializeField] private float adsTransitionSpeed = 15f; // Faster transition when aiming
    [SerializeField] private float hipTransitionSpeed = 10f; // Normal transition when returning to hip
    
    [Header("Rotation Settings")]
    [SerializeField] private float activeYRotationOffset = 90f;
    [SerializeField] private float inactiveYRotationOffset = 45f;
    [SerializeField] private Vector3 fixedInactiveRotation = new Vector3(0f, 0f, 0f);
    [SerializeField] private bool followCameraYawWhenInactive = true;
    [SerializeField] private float rotationSmoothSpeed = 15f;
    
    [Header("Weapon Identity")]
    [SerializeField] private WeaponManager.WeaponSlot myWeaponSlot;

    [Header("Clipping Prevention")]
    [SerializeField] private float minDistanceFromCamera = 0.05f; // Minimum distance gun can be from camera

    // State tracking
    private bool isActiveWeapon = false;
    private bool isAimingDownSights = false;
    
    // Target values to interpolate towards
    private Vector3 targetWorldPosition;
    private Quaternion targetRotation;
    
    // Current transition speed
    private float currentPositionSpeed;

    /// <summary>
    /// Initializes references, attempts to find missing components, and sets initial position/rotation
    /// </summary>
    private void Start()
    {
        // If camera isn't assigned, try to find main camera
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;
        
        // If player body transform isn't assigned, try to find it
        if (playerBodyTransform == null)
        {
            // Assume the parent of the camera is the player body
            if (cameraTransform != null && cameraTransform.parent != null)
                playerBodyTransform = cameraTransform.parent;
            else
            {
                // Fallback: Look for player tag or common player objects
                GameObject playerObj = GameObject.FindWithTag("Player");
                if (playerObj != null)
                    playerBodyTransform = playerObj.transform;
                else
                {
                    // Final fallback: use camera transform as reference
                    Debug.LogWarning("Player body transform not found for GunPositioner. Using camera as fallback.");
                    playerBodyTransform = cameraTransform;
                }
            }
        }
        
        // If weapon manager isn't assigned, try to find it
        if (weaponManager == null)
        {
            // Look in parent objects
            weaponManager = GetComponentInParent<WeaponManager>();
            
            // If still not found, find it in scene
            if (weaponManager == null)
                weaponManager = FindFirstObjectByType<WeaponManager>();
            
            if (weaponManager == null)
                Debug.LogWarning("WeaponManager not found for GunPositioner on " + gameObject.name);
        }
        
        // Set initial position and rotation to inactive
        currentPositionSpeed = hipTransitionSpeed;
        UpdateTargetPosition();
        UpdateTargetRotation();
    }

    /// <summary>
    /// Checks if weapon is active and updates state if necessary
    /// </summary>
    private void Update()
    {
        // Check if this is the active weapon
        if (weaponManager != null)
        {
            bool shouldBeActive = weaponManager.GetCurrentWeaponSlot() == myWeaponSlot;
            
            // Update target position and rotation if active state changed
            if (shouldBeActive != isActiveWeapon)
            {
                isActiveWeapon = shouldBeActive;
                
                // If weapon is no longer active, also exit ADS
                if (!isActiveWeapon && isAimingDownSights)
                    SetAimingState(false);
                    
                UpdateTargetPosition();
                UpdateTargetRotation();
            }
        }
    }

    /// <summary>
    /// Updates weapon position and rotation after camera and player movement is calculated
    /// </summary>
    private void LateUpdate()
    {
        // LateUpdate ensures this runs after camera and character movement is calculated
        
        // Update target position based on current reference transforms (handles moving player/camera)
        UpdateTargetPosition();
            
        // Smoothly move to target position
        transform.position = Vector3.Lerp(transform.position, targetWorldPosition, 
                                         currentPositionSpeed * Time.deltaTime);
        
        // Always update rotation to follow camera movement (for both active and inactive weapons)
        UpdateTargetRotation();
        
        // Apply rotation smoothly
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 
                                            rotationSmoothSpeed * Time.deltaTime);
    }
    
    /// <summary>
    /// Calculates the target position based on weapon state (active/inactive, aiming/not aiming)
    /// Handles clipping prevention for positions too close to the camera
    /// </summary>
    private void UpdateTargetPosition()
    {
        if (!isActiveWeapon && playerBodyTransform != null)
        {
            // Inactive weapon position is relative to player body (holstered)
            targetWorldPosition = playerBodyTransform.TransformPoint(inactivePosition);
            currentPositionSpeed = hipTransitionSpeed;
        }
        else if (cameraTransform != null)
        {
            if (isAimingDownSights)
            {
                // ADS position relative to camera
                Vector3 desiredPosition = cameraTransform.TransformPoint(adsPosition);
                
                // Check if the position is too close to the camera
                float distanceFromCamera = Vector3.Distance(cameraTransform.position, desiredPosition);
                if (distanceFromCamera < minDistanceFromCamera)
                {
                    // Move the gun position away from the camera along forward direction
                    Vector3 direction = (desiredPosition - cameraTransform.position).normalized;
                    desiredPosition = cameraTransform.position + (direction * minDistanceFromCamera);
                }
                
                targetWorldPosition = desiredPosition;
                currentPositionSpeed = adsTransitionSpeed;
            }
            else
            {
                // Active hip position relative to camera
                targetWorldPosition = cameraTransform.TransformPoint(activePosition);
                currentPositionSpeed = hipTransitionSpeed;
            }
        }
    }
    
    /// <summary>
    /// Calculates the target rotation based on weapon state and camera orientation
    /// Active weapons follow all camera rotations, inactive weapons can selectively follow yaw
    /// </summary>
    private void UpdateTargetRotation()
    {
        if (cameraTransform == null) return;
        
        if (isActiveWeapon)
        {
            // ACTIVE WEAPON: Follow ALL camera rotations (X, Y, Z)
            // Get camera rotation and apply Y offset
            Quaternion cameraRotation = cameraTransform.rotation;
            Quaternion yOffsetRotation = Quaternion.Euler(0, activeYRotationOffset, 0);
            targetRotation = cameraRotation * yOffsetRotation;
        }
        else
        {
            // INACTIVE WEAPON: Only follow Y-axis rotation (yaw)
            
            // Get reference rotation - either camera or player body 
            float yawToUse;
            
            // If we want weapons to turn with the camera view
            if (followCameraYawWhenInactive)
                yawToUse = cameraTransform.rotation.eulerAngles.y;
            // Otherwise use the player body's rotation (doesn't turn when looking around)
            else if (playerBodyTransform != null)
                yawToUse = playerBodyTransform.rotation.eulerAngles.y;
            else
                yawToUse = cameraTransform.rotation.eulerAngles.y;
            
            // Apply rotation with fixed X and Z values
            targetRotation = Quaternion.Euler(fixedInactiveRotation.x, yawToUse + inactiveYRotationOffset, fixedInactiveRotation.z);
        }
    }
    
    /// <summary>
    /// Sets the weapon's aiming state, which determines positioning (ADS vs hip)
    /// </summary>
    /// <param name="isAiming">True to aim down sights, false for hip fire position</param>
    public void SetAimingState(bool isAiming)
    {
        if (isAimingDownSights == isAiming)
            return;
            
        isAimingDownSights = isAiming;
        UpdateTargetPosition();
    }
    
    /// <summary>
    /// Forces an immediate update of the weapon's position and rotation based on activity state
    /// </summary>
    /// <param name="active">Whether the weapon should be positioned as active or inactive</param>
    public void UpdatePosition(bool active)
    {
        isActiveWeapon = active;
        UpdateTargetPosition();
        UpdateTargetRotation();
    }
    
    /// <summary>
    /// Gets the weapon slot that this gun occupies in the weapon manager
    /// </summary>
    /// <returns>The assigned weapon slot enum value</returns>
    public WeaponManager.WeaponSlot GetWeaponSlot()
    {
        return myWeaponSlot;
    }
    
    /// <summary>
    /// Determines if this weapon is currently the active one being used by the player
    /// </summary>
    /// <returns>True if this is the active weapon, false otherwise</returns>
    public bool IsActiveWeapon()
    {
        return isActiveWeapon;
    }
    
    /// <summary>
    /// Determines if the weapon is currently in aiming down sights mode
    /// </summary>
    /// <returns>True if player is aiming down sights with this weapon</returns>
    public bool IsAimingDownSights()
    {
        return isAimingDownSights;
    }
    
    /// <summary>
    /// Visualizes weapon positions in the Unity Editor for easier setup
    /// Shows colored spheres at hip, ADS, and inactive positions
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (cameraTransform != null)
        {
            // Draw active hip position
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(cameraTransform.TransformPoint(activePosition), 0.05f);
            
            // Draw ADS position
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(cameraTransform.TransformPoint(adsPosition), 0.05f);
        }
        
        if (playerBodyTransform != null)
        {
            // Draw inactive position
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(playerBodyTransform.TransformPoint(inactivePosition), 0.05f);
        }
    }
}