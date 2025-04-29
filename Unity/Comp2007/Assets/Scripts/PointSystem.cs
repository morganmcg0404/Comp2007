using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class PointSystem : MonoBehaviour
{
    [Header("Points Configuration")]
    [SerializeField] private int currentPoints = 500;       // Starting points
    [SerializeField] private int pointsPerHit = 10;         // Points earned per enemy hit
    [SerializeField] private int pointsPerKill = 100;       // Points earned per enemy kill
    [SerializeField] private int killCount = 0;             // Total enemy kills

    [Header("Purchase Costs")]
    [SerializeField] private int ammoCost = 50;             // Cost for ammo purchase
    [SerializeField] private int healCost = 75;             // Cost for health purchase
    [SerializeField] private int armorPlateCost = 100;      // Cost for armor plate purchase

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI pointsText;    // Text to display current points
    [SerializeField] private GameObject insufficientPointsWarning; // Warning message for insufficient points
    [SerializeField] private float warningDuration = 2f;    // How long the warning shows

    [Header("Audio")]
    [SerializeField] private AudioSource pointsEarnedSound;
    [SerializeField] private AudioSource purchaseSound;
    [SerializeField] private AudioSource failedPurchaseSound;

    // References to required components
    private WeaponManager weaponManager;
    private HealthArmourSystem healthArmourSystem;

    // Singleton instance
    public static PointSystem Instance { get; private set; }

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
    /// Called when player hits an enemy with a bullet
    /// </summary>
    public void EnemyHit()
    {
        AddPoints(pointsPerHit);
    }

    /// <summary>
    /// Called when player kills an enemy
    /// </summary>
    public void EnemyKilled()
    {
        AddPoints(pointsPerKill);
        killCount++;  // Track kill count
    }

    /// <summary>
    /// Adds specified number of points to player's total
    /// </summary>
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
    /// Returns current points (for external access)
    /// </summary>
    public int GetCurrentPoints()
    {
        return currentPoints;
    }

    /// <summary>
    /// Returns current kill count (for external access)
    /// </summary>
    public int GetKillCount()
    {
        return killCount;
    }

    #endregion

    #region Purchases

    /// <summary>
    /// Purchase ammo for the current weapon
    /// </summary>
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
            ShowWarning("Not enough points!");
            return false;
        }
    }

    /// <summary>
    /// Purchase health restoration
    /// </summary>
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
            ShowWarning("Not enough points!");
            return false;
        }
    }

    /// <summary>
    /// Purchase an armor plate
    /// </summary>
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
            ShowWarning("Not enough points!");
            return false;
        }
    }

    /// <summary>
    /// Shows a warning message to the player
    /// </summary>
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
    /// Get the cost of ammo purchase
    /// </summary>
    public int GetAmmoCost() => ammoCost;

    /// <summary>
    /// Get the cost of health purchase
    /// </summary>
    public int GetHealthCost() => healCost;

    /// <summary>
    /// Get the cost of armor plate purchase
    /// </summary>
    public int GetArmorPlateCost() => armorPlateCost;

    #endregion
}