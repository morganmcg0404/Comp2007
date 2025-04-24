using UnityEngine;
using System.Collections;
using DG.Tweening;

/// <summary>
/// Handles aiming down sights functionality for weapons
/// </summary>
public class AimDownSights : MonoBehaviour
{
    [Header("ADS Settings")]
    [SerializeField] private Vector3 adsPosition = new Vector3(0f, -0.07f, 0.35f); // Position when aiming
    [SerializeField] private Vector3 hipPosition;  // Will be set to initial position
    
    // Remove unused transition speed fields or use them in a transition method
    // [SerializeField] private float adsTransitionSpeed = 8f;  // Speed of transition to ADS
    // [SerializeField] private float hipTransitionSpeed = 6f;  // Speed of transition to hip fire
    
    [Header("FOV Settings")]
    [SerializeField] private float hipFOV = 60f;  // Normal FOV
    [SerializeField] private float adsFOV = 45f;  // ADS FOV
    [SerializeField] private float fovTransitionSpeed = 7f;

    [Header("Sensitivity Settings")]
    [SerializeField] private float adsSensitivityMultiplier = 0.7f;
    [Range(0.1f, 2f)]
    [SerializeField] private float minSensitivityMultiplier = 0.1f;
    [Range(0.1f, 2f)]
    [SerializeField] private float maxSensitivityMultiplier = 2f;

    [Header("Movement Settings")]
    [Tooltip("How much to reduce player movement speed during ADS (0-1)")]
    [SerializeField] private float movementSpeedReduction = 0.25f; // 25% reduction = 75% speed
    
    [Header("Toggle Mode")]
    [SerializeField] private bool useToggleMode = false;
    [SerializeField] private KeyCode toggleADSKey = KeyCode.Mouse1;

    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private MouseLook mouseLook;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private WeaponCamera weaponCamera;

    // Private variables
    private bool isAiming = false;
    private float originalSensitivity;
    private WeaponManager weaponManager;
    private Shoot shootScript;
    private GunPositioner gunPositioner;
    private bool hasRightMouseInput = false;
    private float currentFOV;
    private float targetSensitivity;

    private void Awake()
    {
        // Initialize references that weren't set in the inspector
        if (playerCamera == null)
            playerCamera = Camera.main;
            
        if (mouseLook == null)
            mouseLook = FindFirstObjectByType<MouseLook>();
            
        if (playerMovement == null)
            playerMovement = FindFirstObjectByType<PlayerMovement>();
            
        weaponManager = FindFirstObjectByType<WeaponManager>();
        gunPositioner = GetComponent<GunPositioner>();
    }

    private void Start()
    {
        // Store the initial position as the hip position
        hipPosition = transform.localPosition;

        // Store the original mouse sensitivity
        if (mouseLook != null)
        {
            originalSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 1.0f);
            targetSensitivity = originalSensitivity;
        }

        // Store the initial FOV
        if (playerCamera != null)
        {
            hipFOV = playerCamera.fieldOfView;
            currentFOV = hipFOV;
        }

        // Get the shoot script component
        shootScript = GetComponent<Shoot>();
    }

    private void Update()
    {
        // Skip all processing if game is paused
        if (PauseManager.IsPaused())
            return;
            
        // Only handle ADS for the active weapon
        bool isActiveWeapon = IsActiveWeapon();
        if (!isActiveWeapon)
        {
            if (isAiming)
                ExitADS();
            return;
        }

        // Get the shoot script for the current weapon
        if (shootScript == null)
            shootScript = GetComponent<Shoot>();

        // Handle ADS input based on toggle mode
        if (useToggleMode)
        {
            if (Input.GetKeyDown(toggleADSKey))
            {
                ToggleADS();
            }
        }
        else
        {
            // Hold mode
            bool adsInput = Input.GetKey(KeyCode.Mouse1);
            
            // Check for input changes to prevent repeat calls
            if (adsInput && !hasRightMouseInput)
            {
                hasRightMouseInput = true;
                EnterADS();
            }
            else if (!adsInput && hasRightMouseInput)
            {
                hasRightMouseInput = false;
                ExitADS();
            }
        }

        // Check for conditions that should cancel ADS
        if (isAiming)
        {
            if (weaponManager != null && weaponManager.IsSwitchingWeapon())
            {
                ExitADS();
            }

            if (playerMovement != null)
            {
                // Check for sprint
                if (IsPlayerSprinting())
                {
                    ExitADS();
                }

                // Check for jumping
                if (!IsPlayerGrounded())
                {
                    ExitADS();
                }
            }
            
            // Check for reloading
            if (shootScript != null && shootScript.IsReloading())
            {
                ExitADS();
            }
        }

        // Apply smooth transitions for FOV and sensitivity
        UpdateFOV();
        UpdateSensitivity();
    }

    private void UpdateFOV()
    {
        if (playerCamera == null) return;

        // Target FOV based on aiming state
        float targetFOV = isAiming ? adsFOV : hipFOV;
        
        // Smooth transition
        currentFOV = Mathf.Lerp(currentFOV, targetFOV, Time.deltaTime * fovTransitionSpeed);
        
        // Update main camera
        playerCamera.fieldOfView = currentFOV;
        
        // Update weapon camera too if it exists
        if (weaponCamera != null)
        {
            weaponCamera.UpdateFOV(currentFOV);
        }
    }

    private void UpdateSensitivity()
    {
        if (mouseLook == null) return;

        // Smoothly transition sensitivity
        float currentSensitivity = Mathf.Lerp(
            mouseLook.GetSensitivity(), 
            targetSensitivity,
            Time.deltaTime * fovTransitionSpeed
        );
        
        mouseLook.SetSensitivity(currentSensitivity);
    }

    public void EnterADS()
    {
        if (isAiming) return;
        
        isAiming = true;
        
        // Tell gun positioner to enter ADS state
        if (gunPositioner != null)
            gunPositioner.SetAimingState(true);
        
        // Reduce sensitivity for aiming
        float savedMultiplier = PlayerPrefs.GetFloat("AimSensitivity", adsSensitivityMultiplier);
        savedMultiplier = Mathf.Clamp(savedMultiplier, minSensitivityMultiplier, maxSensitivityMultiplier);
        targetSensitivity = originalSensitivity * savedMultiplier;
        
        // Apply movement speed reduction
        if (playerMovement != null)
        {
            playerMovement.SetSpeedMultiplier(1f - movementSpeedReduction);
        }
    }

    public void ExitADS()
    {
        if (!isAiming) return;
        
        isAiming = false;
        
        // Tell gun positioner to exit ADS state
        if (gunPositioner != null)
            gunPositioner.SetAimingState(false);
            
        targetSensitivity = originalSensitivity;
        
        // Restore movement speed
        if (playerMovement != null)
        {
            playerMovement.SetSpeedMultiplier(1f);
        }
    }

    public void ToggleADS()
    {
        if (isAiming)
            ExitADS();
        else
            EnterADS();
    }

    private bool IsActiveWeapon()
    {
        if (weaponManager == null) return true;
        
        // Use the gun positioner if available
        if (gunPositioner != null)
            return gunPositioner.IsActiveWeapon();
        
        // Fallback: check if this object matches the current weapon
        return weaponManager.GetCurrentWeaponObject() == gameObject;
    }

    private bool IsPlayerSprinting()
    {
        if (playerMovement == null) return false;
        return playerMovement.IsSprinting();
    }

    private bool IsPlayerGrounded()
    {
        if (playerMovement == null) return true;
        return playerMovement.IsGrounded();
    }

    // Public getters/setters for UI and settings
    public float GetADSSensitivityMultiplier()
    {
        return adsSensitivityMultiplier;
    }

    public void SetADSSensitivityMultiplier(float multiplier)
    {
        adsSensitivityMultiplier = Mathf.Clamp(multiplier, minSensitivityMultiplier, maxSensitivityMultiplier);
        PlayerPrefs.SetFloat("AimSensitivity", adsSensitivityMultiplier);
        
        // Update current sensitivity if aiming
        if (isAiming)
            targetSensitivity = originalSensitivity * adsSensitivityMultiplier;
    }

    public void SetToggleMode(bool useToggle)
    {
        useToggleMode = useToggle;
        PlayerPrefs.SetInt("ToggleAim", useToggle ? 1 : 0);
        
        // If we're switching to hold mode while already aiming, exit ADS if not holding right mouse
        if (!useToggle && isAiming && !Input.GetKey(KeyCode.Mouse1))
        {
            ExitADS();
        }
    }

    public bool IsAiming()
    {
        return isAiming;
    }
}