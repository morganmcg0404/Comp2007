using UnityEngine;
using TMPro;

public class AmmoUIController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI currentAmmoText;   // Current ammo in magazine
    [SerializeField] private TextMeshProUGUI maxClipSizeText;   // Max clip capacity
    [SerializeField] private TextMeshProUGUI remainingAmmoText; // Total remaining ammo

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

    void Start()
    {
        // Find the weapon manager in the scene
        weaponManager = FindAnyObjectByType<WeaponManager>();
        
        // Set default text values
        if (currentAmmoText) currentAmmoText.text = "--";
        if (maxClipSizeText) maxClipSizeText.text = "--";
        if (remainingAmmoText) remainingAmmoText.text = "--";
    }

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
                }
            }
        }

        // Update UI if we have a valid weapon
        if (activeWeapon != null)
        {
            // Only update if the weapon isn't already determined to be melee
            // This prevents the values from being overridden by the previous weapon's ammo
            if (!currentWeaponIsMelee)
            {
                UpdateAmmoDisplay(activeWeapon);
            }
        }
    }

    // Update all ammo display elements based on the active weapon
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

    // Display ammo for ranged weapons
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

    // Display ammo for melee weapons (1 / 1 1)
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

    // Determine if a weapon is melee based on certain properties
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