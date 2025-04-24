using UnityEngine;

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
    
    // Update the target position based on active and ADS states
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
    
    // Update the target rotation based on active state
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
    
    // Public method to set ADS state
    public void SetAimingState(bool isAiming)
    {
        if (isAimingDownSights == isAiming)
            return;
            
        isAimingDownSights = isAiming;
        UpdateTargetPosition();
    }
    
    // Public method to force position update
    public void UpdatePosition(bool active)
    {
        isActiveWeapon = active;
        UpdateTargetPosition();
        UpdateTargetRotation();
    }
    
    // Helper method to get current weapon slot
    public WeaponManager.WeaponSlot GetWeaponSlot()
    {
        return myWeaponSlot;
    }
    
    // Helper to check if this is the active weapon
    public bool IsActiveWeapon()
    {
        return isActiveWeapon;
    }
    
    // Helper to check if this weapon is currently aiming
    public bool IsAimingDownSights()
    {
        return isAimingDownSights;
    }
    
    // For visualizing positions in the editor
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