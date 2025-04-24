using UnityEngine;
using System.Collections;

/// <summary>
/// Manages weapon switching between primary, secondary, and melee weapons
/// </summary>
public class WeaponManager : MonoBehaviour
{
    [Header("Weapons")]
    [SerializeField] private GameObject primaryWeapon;
    [SerializeField] private GameObject secondaryWeapon;
    [SerializeField] private GameObject meleeWeapon;
    
    [Header("Settings")]
    [SerializeField] private float weaponSwitchTime = 0.5f;
    [SerializeField] private bool canSwitchDuringReload = false;
    [SerializeField] private bool keepWeaponsActive = true; // Keep all weapons active but reposition them
    
    [Header("Input")]
    [SerializeField] private KeyCode primaryWeaponKey = KeyCode.Alpha1;
    [SerializeField] private KeyCode secondaryWeaponKey = KeyCode.Alpha2;
    [SerializeField] private KeyCode meleeWeaponKey = KeyCode.Alpha3;
    [SerializeField] private bool enableScrollWheel = true;
    
    [Header("Audio")]
    [SerializeField] private AudioSource weaponSwitchSound;
    
    // Track current weapon
    public enum WeaponSlot
    {
        Primary,
        Secondary,
        Melee
    }
    
    private WeaponSlot currentWeapon = WeaponSlot.Primary;
    private bool isSwitchingWeapon = false;
    
    // Cached GunPositioner components
    private GunPositioner primaryPositioner;
    private GunPositioner secondaryPositioner;
    private GunPositioner meleePositioner;
    
    // Start is called before the first frame update
    void Start()
    {
        // Get GunPositioner components
        if (primaryWeapon != null)
            primaryPositioner = primaryWeapon.GetComponent<GunPositioner>();
            
        if (secondaryWeapon != null)
            secondaryPositioner = secondaryWeapon.GetComponent<GunPositioner>();
            
        if (meleeWeapon != null)
            meleePositioner = meleeWeapon.GetComponent<GunPositioner>();
    
        // Initialize weapons setup
        InitializeWeapons();
        
        // Set starting weapon (primary by default)
        SwitchToWeaponSlot(WeaponSlot.Primary, true);
    }
    
    // Update is called once per frame
    void Update()
    {
        // Don't process input if game is paused
        if (PauseManager.IsPaused())
            return;
        
        // Don't process input if currently switching
        if (isSwitchingWeapon)
            return;
            
        // Check for keyboard input
        if (Input.GetKeyDown(primaryWeaponKey))
        {
            SwitchToWeaponSlot(WeaponSlot.Primary);
        }
        else if (Input.GetKeyDown(secondaryWeaponKey))
        {
            SwitchToWeaponSlot(WeaponSlot.Secondary);
        }
        else if (Input.GetKeyDown(meleeWeaponKey))
        {
            SwitchToWeaponSlot(WeaponSlot.Melee);
        }
        
        // Scroll wheel support
        if (enableScrollWheel)
        {
            float scrollDelta = Input.GetAxis("Mouse ScrollWheel");
            
            if (scrollDelta > 0f)
            {
                // Scroll up cycles forward
                CycleWeapon(true);
            }
            else if (scrollDelta < 0f)
            {
                // Scroll down cycles backward
                CycleWeapon(false);
            }
        }
    }
    
    private void InitializeWeapons()
    {
        // Check if weapons are assigned
        if (primaryWeapon == null)
        {
            Debug.LogWarning("Primary weapon not assigned in WeaponManager");
        }
        
        if (secondaryWeapon == null)
        {
            Debug.LogWarning("Secondary weapon not assigned in WeaponManager");
        }
        
        if (meleeWeapon == null)
        {
            Debug.LogWarning("Melee weapon not assigned in WeaponManager");
        }
        
        if (keepWeaponsActive)
        {
            // Keep all weapons active but set positions to inactive
            if (primaryWeapon != null) 
            {
                primaryWeapon.SetActive(true);
                if (primaryPositioner != null) primaryPositioner.UpdatePosition(false);
            }
            
            if (secondaryWeapon != null)
            {
                secondaryWeapon.SetActive(true);
                if (secondaryPositioner != null) secondaryPositioner.UpdatePosition(false);
            }
            
            if (meleeWeapon != null)
            {
                meleeWeapon.SetActive(true);
                if (meleePositioner != null) meleePositioner.UpdatePosition(false);
            }
        }
        else
        {
            // Traditional approach - disable weapons
            if (primaryWeapon != null) primaryWeapon.SetActive(false);
            if (secondaryWeapon != null) secondaryWeapon.SetActive(false);
            if (meleeWeapon != null) meleeWeapon.SetActive(false);
        }
    }
    
    /// <summary>
    /// Switch to specified weapon slot
    /// </summary>
    /// <param name="slot">The weapon slot to switch to</param>
    /// <param name="immediate">If true, switches instantly with no animation</param>
    public void SwitchToWeaponSlot(WeaponSlot slot, bool immediate = false)
    {
        // Skip if trying to switch to current weapon
        if (slot == currentWeapon && !immediate)
            return;
            
        // Check if weapon is assigned before switching
        GameObject weaponToSwitch = null;
        GunPositioner positionerToSwitch = null;
        
        switch (slot)
        {
            case WeaponSlot.Primary:
                weaponToSwitch = primaryWeapon;
                positionerToSwitch = primaryPositioner;
                break;
            case WeaponSlot.Secondary:
                weaponToSwitch = secondaryWeapon;
                positionerToSwitch = secondaryPositioner;
                break;
            case WeaponSlot.Melee:
                weaponToSwitch = meleeWeapon;
                positionerToSwitch = meleePositioner;
                break;
        }
        
        // Skip if weapon isn't assigned
        if (weaponToSwitch == null)
        {
            Debug.LogWarning("Cannot switch to " + slot + " weapon - not assigned");
            return;
        }
        
        // Check if current weapon is reloading
        if (!canSwitchDuringReload)
        {
            Shoot currentShootScript = GetCurrentWeaponScript();
            if (currentShootScript != null && currentShootScript.IsReloading())
            {
                return;
            }
        }
        
        // Start switching
        if (immediate)
        {
            CompleteWeaponSwitch(slot);
        }
        else
        {
            StartCoroutine(SwitchWeaponRoutine(slot));
        }
    }
    
    private IEnumerator SwitchWeaponRoutine(WeaponSlot newSlot)
    {
        isSwitchingWeapon = true;
        
        // Play switch sound if assigned
        if (weaponSwitchSound != null)
        {
            weaponSwitchSound.Play();
        }
        
        // Deactivate or reposition current weapon
        DisableCurrentWeapon();
        
        // Wait for switch time
        yield return new WaitForSeconds(weaponSwitchTime);
        
        // Complete the switch
        CompleteWeaponSwitch(newSlot);
        
        isSwitchingWeapon = false;
    }
    
    private void CompleteWeaponSwitch(WeaponSlot newSlot)
    {
        // Update weapon states
        if (keepWeaponsActive)
        {
            // Position-based approach
            if (primaryPositioner != null) primaryPositioner.UpdatePosition(newSlot == WeaponSlot.Primary);
            if (secondaryPositioner != null) secondaryPositioner.UpdatePosition(newSlot == WeaponSlot.Secondary);
            if (meleePositioner != null) meleePositioner.UpdatePosition(newSlot == WeaponSlot.Melee);
        }
        else
        {
            // Traditional enable/disable approach
            if (primaryWeapon != null) primaryWeapon.SetActive(newSlot == WeaponSlot.Primary);
            if (secondaryWeapon != null) secondaryWeapon.SetActive(newSlot == WeaponSlot.Secondary);
            if (meleeWeapon != null) meleeWeapon.SetActive(newSlot == WeaponSlot.Melee);
        }
        
        // Update current weapon
        currentWeapon = newSlot;
    }
    
    private void DisableCurrentWeapon()
    {
        if (keepWeaponsActive)
        {
            // Position-based approach - set current weapon to inactive position
            switch (currentWeapon)
            {
                case WeaponSlot.Primary:
                    if (primaryPositioner != null) primaryPositioner.UpdatePosition(false);
                    break;
                case WeaponSlot.Secondary:
                    if (secondaryPositioner != null) secondaryPositioner.UpdatePosition(false);
                    break;
                case WeaponSlot.Melee:
                    if (meleePositioner != null) meleePositioner.UpdatePosition(false);
                    break;
            }
        }
        else
        {
            // Traditional disable approach
            switch (currentWeapon)
            {
                case WeaponSlot.Primary:
                    if (primaryWeapon != null) primaryWeapon.SetActive(false);
                    break;
                case WeaponSlot.Secondary:
                    if (secondaryWeapon != null) secondaryWeapon.SetActive(false);
                    break;
                case WeaponSlot.Melee:
                    if (meleeWeapon != null) meleeWeapon.SetActive(false);
                    break;
            }
        }
    }
    
    /// <summary>
    /// Cycles through available weapons
    /// </summary>
    /// <param name="forward">Direction to cycle (true = forward, false = backward)</param>
    private void CycleWeapon(bool forward)
    {
        WeaponSlot nextSlot;
        
        if (forward)
        {
            // Cycle forward: Primary -> Secondary -> Melee -> Primary
            nextSlot = (WeaponSlot)(((int)currentWeapon + 1) % 3);
        }
        else
        {
            // Cycle backward: Primary -> Melee -> Secondary -> Primary
            nextSlot = (WeaponSlot)(((int)currentWeapon + 2) % 3);
        }
        
        SwitchToWeaponSlot(nextSlot);
    }
    
    /// <summary>
    /// Gets the Shoot script on the current weapon
    /// </summary>
    private Shoot GetCurrentWeaponScript()
    {
        GameObject currentWeaponObject = null;
        
        switch (currentWeapon)
        {
            case WeaponSlot.Primary:
                currentWeaponObject = primaryWeapon;
                break;
            case WeaponSlot.Secondary:
                currentWeaponObject = secondaryWeapon;
                break;
            case WeaponSlot.Melee:
                currentWeaponObject = meleeWeapon;
                break;
        }
        
        if (currentWeaponObject != null)
        {
            return currentWeaponObject.GetComponent<Shoot>();
        }
        
        return null;
    }
    
    /// <summary>
    /// Returns the current active weapon slot
    /// </summary>
    public WeaponSlot GetCurrentWeaponSlot()
    {
        return currentWeapon;
    }

    /// <summary>
    /// Gets the currently active weapon GameObject
    /// </summary>
    public GameObject GetCurrentWeaponObject()
    {
        switch (currentWeapon)
        {
            case WeaponSlot.Primary:
                return primaryWeapon;
            case WeaponSlot.Secondary:
                return secondaryWeapon;
            case WeaponSlot.Melee:
                return meleeWeapon;
            default:
                return null;
        }
    }
    
    /// <summary>
    /// Check if player is currently in the process of switching weapons
    /// </summary>
    public bool IsSwitchingWeapon()
    {
        return isSwitchingWeapon;
    }
}