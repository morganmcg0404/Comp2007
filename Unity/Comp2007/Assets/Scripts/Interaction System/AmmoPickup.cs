using UnityEngine;

/// <summary>
/// Represents an ammo box item that can be interacted with to restore ammunition
/// Implements IInteractable interface for interaction system integration
/// </summary>
public class AmmoPickup : MonoBehaviour, IInteractable
{
    [Header("Interaction Settings")]
    [SerializeField] private string _prompt = "Press E to buy Ammo (50 points)"; // UI prompt text showing cost
    [SerializeField] private bool restoreToMax = true;        // Whether to fully restore ammo
    [SerializeField] private int ammoAmount = 0;              // Amount to add if not restoring to max
    [SerializeField] private bool destroyAfterUse = true;     // Whether to destroy the ammo box after use
    [SerializeField] private int pointCost = 50;              // Cost in points to use this pickup
    
    [Header("Effects")]
    [SerializeField] private AudioSource pickupSound;         // Optional sound effect when picking up ammo
    [SerializeField] private GameObject pickupEffect;         // Optional particle effect when picking up
    [SerializeField] private float effectDuration = 2f;       // How long the effect lasts before being destroyed
    
    private WeaponManager weaponManager;                     // Reference to the weapon manager
    private PointSystem pointSystem;                         // Reference to the point system
    
    /// <summary>
    /// Called when the script instance is being loaded
    /// Finds and stores reference to the WeaponManager and PointSystem in the scene
    /// </summary>
    private void Start()
    {
        weaponManager = FindAnyObjectByType<WeaponManager>();
        pointSystem = PointSystem.Instance;
        
        // Update prompt with actual cost
        _prompt = $"Press E to buy Ammo ({pointCost} points)";
    }
    
    /// <summary>
    /// Property required by IInteractable interface
    /// Returns the prompt to display when player is near
    /// </summary>
    public string InteractionPrompt => _prompt;

    /// <summary>
    /// Called when player interacts with the ammo box
    /// Checks if player has enough points, then restores ammo for current weapon
    /// </summary>
    /// <param name="interactor">The player/entity interacting with this object</param>
    /// <returns>True if interaction was successful, false otherwise</returns>
    public bool Interact(Interactor interactor)
    {
        // Check if game is paused
        if (PauseManager.IsPaused())
            return false;
        
        // Find the point system if not already found
        if (pointSystem == null)
        {
            pointSystem = PointSystem.Instance;
            if (pointSystem == null)
            {
                Debug.LogWarning("Point System not found in scene");
                return false;
            }
        }
        
        // Check if player has enough points
        if (pointSystem.GetCurrentPoints() < pointCost)
        {
            // Calculate how many more points the player needs
            int pointsNeeded = pointCost - pointSystem.GetCurrentPoints();
            
            // Show insufficient points warning with specific amount needed
            pointSystem.ShowInsufficientPointsWarning($"Need {pointsNeeded} more points!");
            return false;
        }
        
        // If no weapon manager was found, try to find it on the interactor
        if (weaponManager == null)
        {
            weaponManager = interactor.GetComponent<WeaponManager>();
            
            if (weaponManager == null)
            {
                weaponManager = interactor.GetComponentInParent<WeaponManager>();
            }
        }
        
        // Get the current weapon
        GameObject currentWeapon = null;
        if (weaponManager != null)
        {
            currentWeapon = weaponManager.GetCurrentWeaponObject();
        }
        
        // Check if current weapon has a Shoot component
        Shoot shootComponent = null;
        if (currentWeapon != null)
        {
            shootComponent = currentWeapon.GetComponent<Shoot>();
        }
        
        // If no Shoot component found, try to find it directly on the interactor or its children
        if (shootComponent == null)
        {
            shootComponent = interactor.GetComponentInChildren<Shoot>();
        }
        
        // If we found a Shoot component, restore ammo
        if (shootComponent != null)
        {
            // Check if the weapon needs ammo
            bool needsAmmo = shootComponent.GetCurrentTotalAmmo() < shootComponent.GetMaxAmmo();
            
            if (needsAmmo)
            {
                // Restore ammo
                int ammoAdded = shootComponent.AddAmmo(restoreToMax ? int.MaxValue : ammoAmount);
                
                if (ammoAdded > 0)
                {
                    // Deduct points
                    pointSystem.AddPoints(-pointCost);
                    
                    // Play pickup sound if assigned
                    if (pickupSound != null)
                    {
                        pickupSound.Play();
                    }
                    
                    // Spawn pickup effect if assigned
                    if (pickupEffect != null)
                    {
                        GameObject effect = Instantiate(pickupEffect, interactor.transform.position, Quaternion.identity);
                        Destroy(effect, effectDuration);
                    }
                    
                    // Destroy the ammo box if configured to do so
                    if (destroyAfterUse)
                    {
                        Destroy(gameObject);
                    }
                    
                    return true;
                }
                else
                {
                    // Show warning that no ammo was added (likely because the weapon is already full)
                    pointSystem.ShowInsufficientPointsWarning("Weapon already full!");
                    return false;
                }
            }
            else
            {
                // Show warning that weapon is already full
                pointSystem.ShowInsufficientPointsWarning("Weapon already full!");
                return false;
            }
        }
        else
        {
            Debug.LogWarning("No Shoot component found on current weapon");
            return false;
        }
    }
}