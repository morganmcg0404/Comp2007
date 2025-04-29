using UnityEngine;
using System.Collections;

/// <summary>
/// Manages weapon switching between primary, secondary, and melee weapons
/// Handles weapon selection via key presses and scroll wheel input
/// Controls weapon positions, switching animations, and state management
/// </summary>
public class WeaponManager : MonoBehaviour
{
    [Header("Weapons")]
    /// <summary>
    /// Reference to the primary weapon GameObject (typically an assault rifle or similar)
    /// </summary>
    [SerializeField] private GameObject primaryWeapon;
    
    /// <summary>
    /// Reference to the secondary weapon GameObject (typically a pistol or similar)
    /// </summary>
    [SerializeField] private GameObject secondaryWeapon;
    
    /// <summary>
    /// Reference to the melee weapon GameObject (typically a knife or similar)
    /// </summary>
    [SerializeField] private GameObject meleeWeapon;
    
    [Header("Settings")]
    /// <summary>
    /// Time in seconds for the weapon switching animation to complete
    /// </summary>
    [SerializeField] private float weaponSwitchTime = 0.5f;
    
    /// <summary>
    /// Whether weapons can be switched while in the middle of reloading
    /// </summary>
    [SerializeField] private bool canSwitchDuringReload = false;
    
    /// <summary>
    /// If true, keeps all weapons active in the scene but repositions them off-screen when not selected
    /// If false, weapons are completely disabled when not in use
    /// </summary>
    [SerializeField] private bool keepWeaponsActive = true; // Keep all weapons active but reposition them
    
    [Header("Input")]
    /// <summary>
    /// Key used to select the primary weapon
    /// </summary>
    [SerializeField] private KeyCode primaryWeaponKey = KeyCode.Alpha1;
    
    /// <summary>
    /// Key used to select the secondary weapon
    /// </summary>
    [SerializeField] private KeyCode secondaryWeaponKey = KeyCode.Alpha2;
    
    /// <summary>
    /// Key used to select the melee weapon
    /// </summary>
    [SerializeField] private KeyCode meleeWeaponKey = KeyCode.Alpha3;
    
    /// <summary>
    /// Whether the mouse scroll wheel can be used to cycle through weapons
    /// </summary>
    [SerializeField] private bool enableScrollWheel = true;
    
    [Header("Audio")]
    /// <summary>
    /// Sound played when switching between weapons
    /// </summary>
    [SerializeField] private AudioSource weaponSwitchSound;
    
    /// <summary>
    /// Enumeration representing the three weapon slots available to the player
    /// </summary>
    public enum WeaponSlot
    {
        /// <summary>Primary weapon slot, typically for assault rifles or similar</summary>
        Primary,
        
        /// <summary>Secondary weapon slot, typically for pistols or similar</summary>
        Secondary,
        
        /// <summary>Melee weapon slot, typically for knives or similar</summary>
        Melee
    }
    
    /// <summary>
    /// Tracks which weapon slot is currently active
    /// </summary>
    private WeaponSlot currentWeapon = WeaponSlot.Primary;
    
    /// <summary>
    /// Whether a weapon switch is currently in progress
    /// </summary>
    private bool isSwitchingWeapon = false;
    
    // Cached GunPositioner components
    /// <summary>
    /// Reference to the GunPositioner component on the primary weapon
    /// </summary>
    private GunPositioner primaryPositioner;
    
    /// <summary>
    /// Reference to the GunPositioner component on the secondary weapon
    /// </summary>
    private GunPositioner secondaryPositioner;
    
    /// <summary>
    /// Reference to the GunPositioner component on the melee weapon
    /// </summary>
    private GunPositioner meleePositioner;
    
    /// <summary>
    /// Initializes weapon references and sets up the starting weapon
    /// </summary>
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
    
    /// <summary>
    /// Processes input for weapon switching via keyboard keys and scroll wheel
    /// </summary>
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
    
    /// <summary>
    /// Initializes weapons by setting their active states and positions
    /// Logs warnings if weapon references are missing
    /// </summary>
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
    /// Switches to the specified weapon slot with optional immediate transition
    /// </summary>
    /// <param name="slot">The weapon slot to switch to</param>
    /// <param name="immediate">If true, switches instantly with no animation</param>
    /// <remarks>
    /// When immediate is false, a smooth transition animation plays.
    /// Switching may be prevented if the current weapon is reloading and canSwitchDuringReload is false.
    /// </remarks>
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
    
    /// <summary>
    /// Coroutine that handles the weapon switching process with animation
    /// </summary>
    /// <param name="newSlot">The weapon slot to switch to</param>
    /// <returns>IEnumerator for coroutine execution</returns>
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
    
    /// <summary>
    /// Completes the weapon switch by activating the new weapon and updating state
    /// </summary>
    /// <param name="newSlot">The weapon slot to activate</param>
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
    
    /// <summary>
    /// Disables or repositions the current weapon when switching away from it
    /// </summary>
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
    /// Cycles through available weapons in the specified direction
    /// </summary>
    /// <param name="forward">Direction to cycle (true = forward, false = backward)</param>
    /// <remarks>
    /// Forward cycling: Primary → Secondary → Melee → Primary
    /// Backward cycling: Primary → Melee → Secondary → Primary
    /// </remarks>
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
    /// Gets the Shoot script component on the current weapon
    /// </summary>
    /// <returns>The Shoot component on the current weapon, or null if not found</returns>
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
    /// Returns the currently active weapon slot
    /// </summary>
    /// <returns>The enum value representing the current weapon slot</returns>
    public WeaponSlot GetCurrentWeaponSlot()
    {
        return currentWeapon;
    }

    /// <summary>
    /// Gets the GameObject reference for the currently active weapon
    /// </summary>
    /// <returns>The GameObject of the current weapon, or null if not found</returns>
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
    /// Checks if a weapon switch animation is currently in progress
    /// </summary>
    /// <returns>True if a weapon switch is in progress, false otherwise</returns>
    public bool IsSwitchingWeapon()
    {
        return isSwitchingWeapon;
    }
}