using UnityEngine;
using TMPro;

/// <summary>
/// Controls the display and update of ammunition UI elements for weapons
/// </summary>
public class AmmoUIController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI currentAmmoText;   // Current ammo in magazine
    [SerializeField] private TextMeshProUGUI maxClipSizeText;   // Max clip capacity
    [SerializeField] private TextMeshProUGUI remainingAmmoText; // Total remaining ammo
    [SerializeField] private GameObject reloadPrompt;           // Reference to the reload prompt UI element

    [Header("UI Settings")]
    [SerializeField] private Color lowAmmoColor = Color.red;   // Color when ammo is low
    [SerializeField] private Color normalAmmoColor = Color.white; // Normal ammo color
    [SerializeField] private float lowAmmoThreshold = 0.25f;   // Percentage to trigger low ammo warning

    [Header("Melee Weapon Settings")]
    [SerializeField] private string meleeDisplayValue = "1";    // Value to display for melee weapons

    private Shoot activeWeapon;
    private WeaponManager weaponManager;
    private GameObject lastTrackedWeapon;
    private string lastWeaponName = "";  // Track the last weapon's name to prevent duplicate logging
    private bool currentWeaponIsMelee = false; // Track if current weapon is melee

    /// <summary>
    /// Initializes the controller, finds necessary components, and sets default UI values
    /// </summary>
    void Start()
    {
        // Find the weapon manager in the scene
        weaponManager = FindAnyObjectByType<WeaponManager>();
        
        // Set default text values
        if (currentAmmoText) currentAmmoText.text = "--";
        if (maxClipSizeText) maxClipSizeText.text = "--";
        if (remainingAmmoText) remainingAmmoText.text = "--";
        
        // Hide reload prompt initially
        if (reloadPrompt) reloadPrompt.SetActive(false);
    }

    /// <summary>
    /// Updates the ammo display based on active weapon and its state
    /// </summary>
    void Update()
    {
        // Always check if the weapon has changed
        if (weaponManager != null)
        {
            GameObject currentWeapon = weaponManager.GetCurrentWeaponObject();
            
            // If weapon changed or we don't have an active weapon reference
            if (currentWeapon != lastTrackedWeapon || activeWeapon == null)
            {
                // Update our tracking references
                lastTrackedWeapon = currentWeapon;
                
                if (currentWeapon != null)
                {
                    // Get the new weapon's Shoot component
                    activeWeapon = currentWeapon.GetComponent<Shoot>();
                    
                    // Only log if it's a different weapon name to prevent spam
                    if (currentWeapon.name != lastWeaponName)
                    {
                        lastWeaponName = currentWeapon.name;
                    }
                    
                    // Immediately check if this is a melee weapon for proper display
                    if (activeWeapon != null)
                    {
                        currentWeaponIsMelee = IsMeleeWeapon(activeWeapon);
                        
                        // If it's a melee weapon, immediately update the display
                        if (currentWeaponIsMelee)
                        {
                            DisplayMeleeAmmo();
                            HideReloadPrompt(); // Hide reload prompt for melee weapons
                        }
                    }
                }
                else
                {
                    activeWeapon = null;
                    lastWeaponName = "";  // Reset weapon name tracking
                    currentWeaponIsMelee = false;
                    
                    // Reset UI when no weapon is equipped
                    if (currentAmmoText) currentAmmoText.text = "--";
                    if (maxClipSizeText) maxClipSizeText.text = "--";
                    if (remainingAmmoText) remainingAmmoText.text = "--";
                    HideReloadPrompt(); // Hide reload prompt when no weapon
                }
            }
        }

        // Update UI if we have a valid weapon
        if (activeWeapon != null)
        {
            // Only update if the weapon isn't already determined to be melee
            if (!currentWeaponIsMelee)
            {
                UpdateAmmoDisplay(activeWeapon);
                
                // Check if we need to show/hide the reload prompt
                UpdateReloadPrompt(activeWeapon);
            }
        }
    }

    /// <summary>
    /// Determines whether to show or hide reload prompt based on weapon's ammo status
    /// </summary>
    /// <param name="weapon">The weapon to check ammo status for</param>
    private void UpdateReloadPrompt(Shoot weapon)
    {
        if (weapon == null || reloadPrompt == null) return;
        
        // Skip if weapon is reloading
        if (weapon.IsReloading())
        {
            HideReloadPrompt();
            return;
        }
        
        // Show reload prompt if ammo is low and we have reserve ammo
        int lowAmmoThresholdValue = Mathf.CeilToInt(weapon.GetMagazineSize() * lowAmmoThreshold);
        
        if (weapon.GetCurrentAmmoInMag() <= lowAmmoThresholdValue && weapon.GetCurrentTotalAmmo() > 0)
        {
            ShowReloadPrompt();
        }
        else
        {
            HideReloadPrompt();
        }
    }

    /// <summary>
    /// Shows the reload prompt UI element
    /// </summary>
    private void ShowReloadPrompt()
    {
        if (reloadPrompt != null && !reloadPrompt.activeSelf)
        {
            reloadPrompt.SetActive(true);
        }
    }

    /// <summary>
    /// Hides the reload prompt UI element
    /// </summary>
    private void HideReloadPrompt()
    {
        if (reloadPrompt != null && reloadPrompt.activeSelf)
        {
            reloadPrompt.SetActive(false);
        }
    }

    /// <summary>
    /// Updates all ammo display elements based on the active weapon
    /// </summary>
    /// <param name="weapon">The weapon to display ammo information for</param>
    public void UpdateAmmoDisplay(Shoot weapon)
    {
        if (weapon == null) return;

        // Re-check if this is a melee weapon (in case something changed)
        currentWeaponIsMelee = IsMeleeWeapon(weapon);

        if (currentWeaponIsMelee)
        {
            // Special display for melee weapons
            DisplayMeleeAmmo();
        }
        else
        {
            // Normal display for ranged weapons
            DisplayRangedAmmo(weapon);
        }
    }

    /// <summary>
    /// Updates UI to display ammo information for ranged weapons
    /// </summary>
    /// <param name="weapon">The ranged weapon to display ammo for</param>
    private void DisplayRangedAmmo(Shoot weapon)
    {
        int currentAmmo = weapon.GetCurrentAmmoInMag();
        int magazineSize = weapon.GetMagazineSize();
        int remainingAmmo = weapon.GetCurrentTotalAmmo();
        bool isReloading = weapon.IsReloading();

        // Update current ammo text
        if (currentAmmoText != null)
        {
            // Always show the current ammo count, even during reload
            currentAmmoText.text = currentAmmo.ToString();
            
            // Show in red if ammo is low or reloading
            if ((float)currentAmmo / magazineSize <= lowAmmoThreshold || isReloading)
            {
                currentAmmoText.color = lowAmmoColor;
            }
            else
            {
                currentAmmoText.color = normalAmmoColor;
            }
        }

        // Update max clip size text
        if (maxClipSizeText != null)
        {
            // Display just the number without any separator
            maxClipSizeText.text = magazineSize.ToString();
        }

        // Update remaining ammo text
        if (remainingAmmoText != null)
        {
            remainingAmmoText.text = remainingAmmo.ToString();
        }
    }

    /// <summary>
    /// Updates UI to display placeholder values for melee weapons
    /// </summary>
    private void DisplayMeleeAmmo()
    {
        // Update all text elements with melee values (specifically "1" for all fields)
        if (currentAmmoText != null)
        {
            currentAmmoText.text = meleeDisplayValue;
            currentAmmoText.color = normalAmmoColor; // Always use normal color for melee
        }

        if (maxClipSizeText != null)
        {
            maxClipSizeText.text = meleeDisplayValue;
        }

        if (remainingAmmoText != null)
        {
            remainingAmmoText.text = meleeDisplayValue; // Changed from infinity symbol to "1"
        }
    }

    /// <summary>
    /// Determines if a weapon is a melee weapon based on various characteristics
    /// </summary>
    /// <param name="weapon">The weapon to check</param>
    /// <returns>True if the weapon is determined to be a melee weapon</returns>
    private bool IsMeleeWeapon(Shoot weapon)
    {
        if (weapon == null) return false;
        
        // Method 1: Check if the weapon has a "Melee" tag or component
        if (weapon.gameObject.CompareTag("MeleeWeapon"))
        {
            return true;
        }

        // Method 2: Check if it has a MeleeAttack component
        if (weapon.gameObject.GetComponent<MeleeAttack>() != null)
        {
            return true;
        }
        
        // Method 3: Check properties that might indicate it's a melee weapon
        try {
            if (weapon.GetMagazineSize() <= 0)
            {
                return true;
            }
        }
        catch (System.Exception) {
            // If the method throws an error, it might be because it's not a standard weapon
            return true;
        }
        
        // Check weapon name for common melee weapon terms
        string weaponName = weapon.gameObject.name.ToLower();
        if (weaponName.Contains("knife") || weaponName.Contains("sword") || 
            weaponName.Contains("axe") || weaponName.Contains("melee") || 
            weaponName.Contains("blade") || weaponName.Contains("dagger"))
        {
            return true;
        }
        
        return false;
    }
}