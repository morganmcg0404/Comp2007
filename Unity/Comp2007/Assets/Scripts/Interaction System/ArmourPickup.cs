using UnityEngine;

/// <summary>
/// Represents an armour plate pickup that can be interacted with
/// Fully restores all armor plates when picked up
/// </summary>
public class ArmourPickup : MonoBehaviour, IInteractable
{
    [Header("Interaction Settings")]
    [SerializeField] private string _prompt = "Press E to buy Armour Plates (100 points)"; // UI prompt text
    [SerializeField] private bool destroyAfterUse = true;                       // Whether to destroy the item after use
    [SerializeField] private int pointCost = 100;                              // Cost in points to use this pickup
    
    [Header("Effects")]
    /// <summary>
    /// Sound name for the pickup sound effect
    /// </summary>
    [SerializeField] private string pickupSoundName = "ItemPickup";            // Sound name for pickup in SoundLibrary
    
    /// <summary>
    /// Audio mixer group to use for the pickup sound
    /// </summary>
    [SerializeField] private string audioMixerGroup = "SFX";                   // Mixer group name
    
    /// <summary>
    /// Optional particle effect when picking up
    /// </summary>
    [SerializeField] private GameObject pickupEffect;                          // Optional particle effect when picking up
    
    /// <summary>
    /// How long the effect lasts before being destroyed
    /// </summary>
    [SerializeField] private float effectDuration = 2f;                        // How long the effect lasts before being destroyed
    
    private HealthArmourSystem _healthArmourSystem;                           // Reference to player's health/armour system
    private PointSystem pointSystem;                                          // Reference to the point system
    
    /// <summary>
    /// Initializes references and updates the interaction prompt with actual cost
    /// </summary>
    private void Start()
    {
        pointSystem = PointSystem.Instance;
        
        // Update prompt with actual cost
        _prompt = $"Press E to buy Armour Plates ({pointCost} points)";
    }
    
    /// <summary>
    /// Property required by IInteractable interface
    /// Returns the prompt to display when player is near
    /// </summary>
    public string InteractionPrompt => _prompt;
    
    /// <summary>
    /// Called when player interacts with the armour plate pickup
    /// Checks if player has enough points, then restores all armour plates
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
        
        
        // Try to find the HealthArmourSystem component
        if (_healthArmourSystem == null)
        {
            // Try on the interactor first
            _healthArmourSystem = interactor.GetComponent<HealthArmourSystem>();
            
            // If not on the interactor directly, try on its parent (player)
            if (_healthArmourSystem == null)
            {
                _healthArmourSystem = interactor.GetComponentInParent<HealthArmourSystem>();
                
                // Last resort - find any instance in the scene
                if (_healthArmourSystem == null)
                {
                    _healthArmourSystem = FindAnyObjectByType<HealthArmourSystem>();
                }
            }
        }
        
        // If we found a health system, try to restore all armour plates
        if (_healthArmourSystem != null)
        {
            
            bool platesChanged = RestoreAllArmourPlates();

            if (platesChanged)
            {
                // Deduct points
                pointSystem.AddPoints(-pointCost);
                
                // Play pickup sound using SoundManager
                PlayPickupSound(interactor.transform.position);
                
                // Spawn pickup effect if assigned
                if (pickupEffect != null)
                {
                    GameObject effect = Instantiate(pickupEffect, interactor.transform.position, Quaternion.identity);
                    Destroy(effect, effectDuration);
                }
                
                
                // Destroy the item if configured to do so
                if (destroyAfterUse)
                {
                    Destroy(gameObject);
                }
                
                return true;
            }
            else
            {
                // Player already has maximum armour plates at full health
                pointSystem.ShowInsufficientPointsWarning("Armor already full!");
                return false;
            }
        }
        else
        {
            Debug.LogError("No HealthArmourSystem component found on player or in scene");
            return false;
        }
    }
    
    /// <summary>
    /// Plays the pickup sound using SoundManager with mixer support
    /// </summary>
    /// <param name="position">Position to play the sound at</param>
    private void PlayPickupSound(Vector3 position)
    {
        if (string.IsNullOrEmpty(pickupSoundName)) return;
        
        // Get SoundManager instance
        SoundManager soundManager = SoundManager.GetInstance();
        if (soundManager == null) return;
        
        // Play the sound at the specified position with mixer group
        soundManager.PlaySound3DWithMixer(pickupSoundName, position, 1.0f, audioMixerGroup);
    }
    
    /// <summary>
    /// Restores all armour plates to full capacity
    /// First repairs any damaged existing plates, then adds new plates if needed
    /// </summary>
    /// <returns>True if any changes were made, false if all plates were already at maximum</returns>
    private bool RestoreAllArmourPlates()
    {
        bool anyChanges = false;
        
        // First, repair any existing damaged plates using the new RepairArmourPlate method
        for (int i = 0; i < 3; i++)
        {
            float currentPlateHealth = _healthArmourSystem.GetArmourPlateHealth(i);
            float maxPlateHealth = _healthArmourSystem.GetMaxArmourPlateHealth();
            
            // Check if this plate exists but is damaged
            if (currentPlateHealth > 0 && currentPlateHealth < maxPlateHealth)
            {
                // Use the public RepairArmourPlate method - this will repair one plate per call
                _healthArmourSystem.RepairArmourPlate();
                anyChanges = true;
                
                // Since RepairArmourPlate repairs the first damaged plate it finds,
                // we should check if this plate is now fixed
                if (_healthArmourSystem.GetArmourPlateHealth(i) >= maxPlateHealth)
                {
                    continue; // This plate is now repaired, move to the next one
                }
            }
        }
        
        // Then add new plates if needed
        int currentPlates = _healthArmourSystem.ArmourPlatesRemaining;
        int platesNeeded = 3 - currentPlates;
        
        for (int i = 0; i < platesNeeded; i++)
        {
            if (_healthArmourSystem.AddArmourPlate())
            {
                anyChanges = true;
            }
        }
        
        return anyChanges;
    }
}