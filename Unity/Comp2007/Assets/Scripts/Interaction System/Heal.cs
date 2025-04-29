using UnityEngine;

/// <summary>
/// Represents a medkit item that can be interacted with to restore health
/// Implements IInteractable interface for interaction system integration
/// </summary>
public class Heal : MonoBehaviour, IInteractable
{
    [Header("Interaction Settings")]
    [SerializeField] private string _prompt = "Press E to buy Medkit (75 points)"; // UI prompt text
    [SerializeField] private float healAmount = 50f;                   // Amount of health to restore
    [SerializeField] private bool destroyAfterUse = true;              // Whether to destroy the medkit after use
    [SerializeField] private int pointCost = 75;                      // Cost in points to use this pickup
    
    [Header("Effects")]
    [SerializeField] private AudioSource healSound;                    // Optional sound effect when using the medkit
    [SerializeField] private GameObject healEffect;                    // Optional particle effect when healing
    [SerializeField] private float effectDuration = 2f;                // How long the effect lasts before being destroyed
    
    private HealthArmourSystem _healthArmourSystem;                   // Reference to player's health system
    private PointSystem pointSystem;                                  // Reference to the point system
    
    /// <summary>
    /// Called when the script instance is being loaded
    /// Finds and stores reference to the HealthSystem and PointSystem in the scene
    /// </summary>
    private void Start()
    {
        _healthArmourSystem = FindAnyObjectByType<HealthArmourSystem>();
        pointSystem = PointSystem.Instance;
        
        // Update prompt with actual cost
        _prompt = $"Press E to buy Medkit ({pointCost} points)";
    }
    
    /// <summary>
    /// Property required by IInteractable interface
    /// Returns the prompt to display when player is near
    /// </summary>
    public string InteractionPrompt => _prompt;

    /// <summary>
    /// Called when player interacts with the medkit
    /// Checks if player has enough points, then restores player health
    /// </summary>
    /// <param name="interactor">The player/entity interacting with this object</param>
    /// <returns>True if healing was applied successfully, false otherwise</returns>
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
        
        // If no health system was found, try to find it on the interactor
        if (_healthArmourSystem == null)
        {
            _healthArmourSystem = interactor.GetComponent<HealthArmourSystem>();
            
            if (_healthArmourSystem == null)
            {
                _healthArmourSystem = interactor.GetComponentInParent<HealthArmourSystem>();
                
                // If still null, search the scene again
                if (_healthArmourSystem == null)
                {
                    _healthArmourSystem = FindAnyObjectByType<HealthArmourSystem>();
                }
            }
        }
        
        if (_healthArmourSystem != null)
        {
            // Check if player needs healing
            if (_healthArmourSystem.CurrentHealth < _healthArmourSystem.MaxHealth)
            {
                // Apply healing and get amount actually healed
                float amountHealed = _healthArmourSystem.AddHealth(healAmount);
                
                if (amountHealed > 0)
                {
                    // Deduct points
                    pointSystem.AddPoints(-pointCost);
                    
                    // Play heal sound if assigned
                    if (healSound != null)
                    {
                        healSound.Play();
                    }
                    
                    // Spawn heal effect if assigned
                    if (healEffect != null)
                    {
                        GameObject effect = Instantiate(healEffect, interactor.transform.position, Quaternion.identity);
                        Destroy(effect, effectDuration);
                    }
                    
                    // Destroy the medkit if configured to do so
                    if (destroyAfterUse)
                    {
                        Destroy(gameObject);
                    }
                    
                    return true;
                }
            }
            else
            {
                // Player already at full health
                pointSystem.ShowInsufficientPointsWarning("Health already full!");
                return false;
            }
        }
        else
        {
            Debug.LogWarning("No HealthSystem component found on player");
        }
        
        return false;
    }
}