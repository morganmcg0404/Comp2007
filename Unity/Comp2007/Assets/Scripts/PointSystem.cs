using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Manages the player's point economy, including earning, spending, and tracking points
/// Handles purchases of ammo, health, and armor, and provides feedback through UI
/// Implements a singleton pattern for global access
/// </summary>
public class PointSystem : MonoBehaviour
{
    [Header("Points Configuration")]
    /// <summary>
    /// Current point total available to the player
    /// </summary>
    [SerializeField] private int currentPoints = 500;       // Starting points
    
    /// <summary>
    /// Number of points awarded for each successful enemy hit
    /// </summary>
    [SerializeField] private int pointsPerHit = 10;         // Points earned per enemy hit
    
    /// <summary>
    /// Number of points awarded for each enemy kill
    /// </summary>
    [SerializeField] private int pointsPerKill = 100;       // Points earned per enemy kill
    
    /// <summary>
    /// Total number of enemies killed in the current session
    /// </summary>
    [SerializeField] private int killCount = 0;             // Total enemy kills

    [Header("Purchase Costs")]
    /// <summary>
    /// Cost in points to purchase ammunition
    /// </summary>
    [SerializeField] private int ammoCost = 50;             // Cost for ammo purchase
    
    /// <summary>
    /// Cost in points to purchase health restoration
    /// </summary>
    [SerializeField] private int healCost = 75;             // Cost for health purchase
    
    /// <summary>
    /// Cost in points to purchase an armor plate
    /// </summary>
    [SerializeField] private int armorPlateCost = 100;      // Cost for armor plate purchase

    [Header("UI References")]
    /// <summary>
    /// Text component to display current point total
    /// </summary>
    [SerializeField] private TextMeshProUGUI pointsText;    // Text to display current points
    
    /// <summary>
    /// GameObject containing warning message for failed purchases
    /// </summary>
    [SerializeField] private GameObject insufficientPointsWarning; // Warning message for insufficient points
    
    /// <summary>
    /// Duration in seconds that the warning message displays
    /// </summary>
    [SerializeField] private float warningDuration = 2f;    // How long the warning shows

    [Header("Audio")]
    /// <summary>
    /// Sound played when points are earned
    /// </summary>
    [SerializeField] private AudioSource pointsEarnedSound;
    
    /// <summary>
    /// Sound played when a purchase is successful
    /// </summary>
    [SerializeField] private AudioSource purchaseSound;
    
    /// <summary>
    /// Sound played when a purchase fails
    /// </summary>
    [SerializeField] private AudioSource failedPurchaseSound;

    // References to required components
    /// <summary>
    /// Reference to the weapon management system
    /// </summary>
    private WeaponManager weaponManager;
    
    /// <summary>
    /// Reference to the player's health and armor system
    /// </summary>
    private HealthArmourSystem healthArmourSystem;

    // Singleton instance
    /// <summary>
    /// Singleton instance for global access to the point system
    /// </summary>
    public static PointSystem Instance { get; private set; }

    /// <summary>
    /// Initializes the singleton instance and ensures only one PointSystem exists
    /// </summary>
    private void Awake()
    {
        // Singleton pattern setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Optional: keep between scenes
        }
    }

    /// <summary>
    /// Finds required components and initializes the UI on start
    /// </summary>
    void Start()
    {
        // Find required components
        weaponManager = FindAnyObjectByType<WeaponManager>();
        healthArmourSystem = FindAnyObjectByType<HealthArmourSystem>();

        // Ensure UI is updated at start
        UpdatePointsDisplay();
        
        if (insufficientPointsWarning != null)
        {
            insufficientPointsWarning.SetActive(false);
        }
    }

    #region Points Management

    /// <summary>
    /// Awards points when player hits an enemy with a bullet
    /// </summary>
    public void EnemyHit()
    {
        AddPoints(pointsPerHit);
    }

    /// <summary>
    /// Awards points and increments kill count when player kills an enemy
    /// </summary>
    public void EnemyKilled()
    {
        AddPoints(pointsPerKill);
        killCount++;  // Track kill count
    }

    /// <summary>
    /// Adds specified number of points to player's total and updates UI
    /// </summary>
    /// <param name="amount">Amount of points to add</param>
    public void AddPoints(int amount)
    {
        currentPoints += amount;
        UpdatePointsDisplay();
        
        if (pointsEarnedSound != null)
        {
            pointsEarnedSound.Play();
        }
    }

    /// <summary>
    /// Updates the UI text displaying current points
    /// </summary>
    private void UpdatePointsDisplay()
    {
        if (pointsText != null)
        {
            pointsText.text = $"{currentPoints}";
        }
    }

    /// <summary>
    /// Returns current points for external access
    /// </summary>
    /// <returns>The current point total</returns>
    public int GetCurrentPoints()
    {
        return currentPoints;
    }

    /// <summary>
    /// Returns current kill count for external access
    /// </summary>
    /// <returns>The current enemy kill count</returns>
    public int GetKillCount()
    {
        return killCount;
    }

    #endregion

    #region Purchases

    /// <summary>
    /// Attempts to purchase ammo for the current weapon
    /// </summary>
    /// <returns>True if purchase was successful, false otherwise</returns>
    public bool PurchaseAmmo()
    {
        if (currentPoints >= ammoCost)
        {
            // Find the current weapon
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
            
            if (shootComponent != null)
            {
                // Check if the weapon needs ammo
                bool needsAmmo = shootComponent.GetCurrentTotalAmmo() < shootComponent.GetMaxAmmo();
                
                if (needsAmmo)
                {
                    // Add some ammo (not full) - arbitrary amount
                    int ammoAdded = shootComponent.AddAmmo(shootComponent.GetMaxAmmo() / 2);
                    
                    if (ammoAdded > 0)
                    {
                        // Deduct points
                        currentPoints -= ammoCost;
                        UpdatePointsDisplay();
                        
                        if (purchaseSound != null)
                        {
                            purchaseSound.Play();
                        }
                        return true;
                    }
                    else
                    {
                        ShowWarning("Weapon already full!");
                        return false;
                    }
                }
                else
                {
                    ShowWarning("Weapon already full!");
                    return false;
                }
            }
            else
            {
                ShowWarning("No weapon equipped!");
                return false;
            }
        }
        else
        {
            int pointsNeeded = ammoCost - currentPoints;
            ShowWarning($"Need {pointsNeeded} more points!");
            return false;
        }
    }

    /// <summary>
    /// Attempts to purchase health restoration for the player
    /// </summary>
    /// <returns>True if purchase was successful, false otherwise</returns>
    public bool PurchaseHealth()
    {
        if (currentPoints >= healCost)
        {
            if (healthArmourSystem != null)
            {
                // Check if player needs healing
                if (healthArmourSystem.CurrentHealth < healthArmourSystem.MaxHealth)
                {
                    // Heal for 50% of max health
                    float healAmount = healthArmourSystem.MaxHealth * 0.5f;
                    float amountHealed = healthArmourSystem.AddHealth(healAmount);
                    
                    if (amountHealed > 0)
                    {
                        // Deduct points
                        currentPoints -= healCost;
                        UpdatePointsDisplay();
                        
                        if (purchaseSound != null)
                        {
                            purchaseSound.Play();
                        }
                        return true;
                    }
                    else
                    {
                        ShowWarning("Already at full health!");
                        return false;
                    }
                }
                else
                {
                    ShowWarning("Already at full health!");
                    return false;
                }
            }
            else
            {
                ShowWarning("Health system not found!");
                return false;
            }
        }
        else
        {
            int pointsNeeded = healCost - currentPoints;
            ShowWarning($"Need {pointsNeeded} more points!");
            return false;
        }
    }

    /// <summary>
    /// Attempts to purchase an armor plate for the player
    /// </summary>
    /// <returns>True if purchase was successful, false otherwise</returns>
    public bool PurchaseArmorPlate()
    {
        if (currentPoints >= armorPlateCost)
        {
            if (healthArmourSystem != null)
            {
                // Check if player needs armor plates
                if (healthArmourSystem.ArmourPlatesRemaining < 3)
                {
                    healthArmourSystem.AddArmourPlate();
                    
                    // Deduct points
                    currentPoints -= armorPlateCost;
                    UpdatePointsDisplay();
                    
                    if (purchaseSound != null)
                    {
                        purchaseSound.Play();
                    }
                    return true;
                }
                else
                {
                    ShowWarning("Max armor plates already!");
                    return false;
                }
            }
            else
            {
                ShowWarning("Armor system not found!");
                return false;
            }
        }
        else
        {
            int pointsNeeded = armorPlateCost - currentPoints;
            ShowWarning($"Need {pointsNeeded} more points!");
            return false;
        }
    }

    #endregion

    #region Warning Display

    /// <summary>
    /// Shows a warning message to the player for insufficient points
    /// </summary>
    /// <param name="message">The warning message to display</param>
    public void ShowInsufficientPointsWarning(string message)
    {
        ShowWarning(message);
    }

    /// <summary>
    /// Shows a warning message to the player with the specified text
    /// </summary>
    /// <param name="message">The warning message to display</param>
    private void ShowWarning(string message)
    {
        if (insufficientPointsWarning != null)
        {
            // If there's a Text or TMP component, update its message
            TextMeshProUGUI warningText = insufficientPointsWarning.GetComponentInChildren<TextMeshProUGUI>();
            if (warningText != null)
            {
                warningText.text = message;
            }
            
            Text legacyText = insufficientPointsWarning.GetComponentInChildren<Text>();
            if (legacyText != null)
            {
                legacyText.text = message;
            }
            
            insufficientPointsWarning.SetActive(true);
            
            // Play failed purchase sound
            if (failedPurchaseSound != null)
            {
                failedPurchaseSound.Play();
            }
            
            // Hide warning after duration
            Invoke(nameof(HideWarning), warningDuration);
        }
    }

    /// <summary>
    /// Hides the warning message
    /// </summary>
    private void HideWarning()
    {
        if (insufficientPointsWarning != null)
        {
            insufficientPointsWarning.SetActive(false);
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Gets the cost of ammo purchase
    /// </summary>
    /// <returns>The cost in points to purchase ammo</returns>
    public int GetAmmoCost() => ammoCost;

    /// <summary>
    /// Gets the cost of health purchase
    /// </summary>
    /// <returns>The cost in points to purchase health</returns>
    public int GetHealthCost() => healCost;

    /// <summary>
    /// Gets the cost of armor plate purchase
    /// </summary>
    /// <returns>The cost in points to purchase an armor plate</returns>
    public int GetArmorPlateCost() => armorPlateCost;

    #endregion
}