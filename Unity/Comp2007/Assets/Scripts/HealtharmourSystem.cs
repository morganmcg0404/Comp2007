using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Manages player health and Armour visualization and gameplay mechanics
/// </summary>
public class HealthArmourSystem : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float _baseMaxHealth = 100f;    // Base maximum health value
    [SerializeField] private float _currentHealth;           // Current health value
    
    [Header("Health UI")]
    [SerializeField] private TextMeshProUGUI _currentHealthText;  // Text display for current health
    [SerializeField] private TextMeshProUGUI _maxHealthText;      // Text display for maximum health
    [SerializeField] private string _healthNumberFormat = "0";    // Format for health numbers (0 = no decimals)
    
    [Header("Armour Settings")]
    [SerializeField] private List<Image> _ArmourPlateImages = new List<Image>(3); // 3 Armour plate UI elements
    [SerializeField] private float _maxArmourPlateHealth = 50f;                  // Health per Armour plate
    [SerializeField] private float _ArmourDamageReduction = 0.15f;               // 15% damage reduction when wearing Armour
    [SerializeField] private float _ArmourDamageSplit = 0.50f;                   // 50% of damage goes to Armour
    [SerializeField] private Color _fullArmourColor = Color.white;               // Color for full Armour plates
    [SerializeField] private Color _lowArmourColor = Color.red;                  // Color for nearly broken Armour plates
    [SerializeField] private Color _emptyArmourColor = new Color(0.3f, 0.3f, 0.3f, 0.5f); // Color for empty armor plates
    
    [Header("UI Settings")]
    [SerializeField] private float _ArmourSlideAnimTime = 0.5f;                 // How quickly Armour plates visually deplete
    [SerializeField] private float _healthCounterAnimTime = 0.5f;               // How quickly health counter animates
    [SerializeField] private bool _showEmptyArmourPlates = true;                // Whether to show empty armor plate outlines
    
    [Header("Armour Plate Animation")]
    [SerializeField] private float _plateSlideAnimTime = 0.4f;                  // How long the slide animation takes
    
    // Internal Armour tracking
    private float[] _ArmourPlateHealths = new float[3];
    private int _activeArmourPlates = 0;
    private Vector2[] _originalPlatePositions = new Vector2[3];                 // Store original positions for reset
    
    // Variables for health counter animation
    private int _displayedCurrentHealth;
    private int _displayedMaxHealth;
    
    // Properties
    public float CurrentHealth => _currentHealth;
    public float MaxHealth => _baseMaxHealth;
    public int ArmourPlatesRemaining => _activeArmourPlates;
    public float CurrentTotalArmour => GetTotalRemainingArmour();

    /// <summary>
    /// Initializes health and Armour system on startup
    /// </summary>
    private void Awake()
    {
        // Initialize health
        _currentHealth = _baseMaxHealth;
        _displayedCurrentHealth = Mathf.RoundToInt(_currentHealth);
        _displayedMaxHealth = Mathf.RoundToInt(_baseMaxHealth);
        UpdateHealthDisplay(true); // Force immediate update
        
        // Initialize Armour plates to empty
        InitializeArmourPlates();
    }

    /// <summary>
    /// Initialize the Armour plate UI elements
    /// </summary>
    private void InitializeArmourPlates()
    {
        _activeArmourPlates = 0;
        
        for (int i = 0; i < 3; i++)
        {
            // Reset internal tracking
            _ArmourPlateHealths[i] = 0f;
            
            // Make sure we have valid UI references
            if (i < _ArmourPlateImages.Count && _ArmourPlateImages[i] != null)
            {
                // Store the original position for reset
                _originalPlatePositions[i] = _ArmourPlateImages[i].rectTransform.anchoredPosition;
                
                // Set initial fill and color immediately (no animation for initialization)
                if (_showEmptyArmourPlates)
                {
                    // Show empty plate outlines
                    _ArmourPlateImages[i].fillAmount = 1f;
                    _ArmourPlateImages[i].color = _emptyArmourColor;
                }
                else
                {
                    // Hide empty plates completely
                    _ArmourPlateImages[i].fillAmount = 0f;
                    _ArmourPlateImages[i].color = _fullArmourColor;
                }
            }
        }
    }

    /// <summary>
    /// Processes damage to the player, including Armour calculations
    /// </summary>
    /// <param name="incomingDamage">Base damage amount before reductions</param>
    public void TakeDamage(float incomingDamage)
    {   
        float effectiveDamage = incomingDamage;
        float healthDamage = incomingDamage;
        
        // Apply Armour damage reduction and splitting if player has Armour
        if (_activeArmourPlates > 0)
        {
            // Reduce total damage due to wearing Armour
            effectiveDamage *= (1f - _ArmourDamageReduction);
            
            // Calculate portion of damage going to Armour vs health
            float ArmourDamage = effectiveDamage * _ArmourDamageSplit;
            healthDamage = effectiveDamage * (1f - _ArmourDamageSplit);
            
            // Apply damage to Armour plates
            DamageArmourPlates(ArmourDamage);
        }
        
        // Apply damage to health
        _currentHealth = Mathf.Max(0f, _currentHealth - healthDamage);
        
        // Update UI displays
        UpdateHealthDisplay();
    }

    /// <summary>
    /// Applies damage across Armour plates and updates UI
    /// </summary>
    /// <param name="ArmourDamage">Amount of damage to apply to Armour</param>
    private void DamageArmourPlates(float ArmourDamage)
    {
        // Start with the last (right-most) active Armour plate
        float remainingDamage = ArmourDamage;
        
        // Process plates from right to left (highest index first)
        for (int i = 2; i >= 0 && remainingDamage > 0; i--)
        {
            // Skip empty plates
            if (_ArmourPlateHealths[i] <= 0) continue;
            
            // Calculate how much damage this plate can absorb
            float damageToThisPlate = Mathf.Min(remainingDamage, _ArmourPlateHealths[i]);
            float previousHealth = _ArmourPlateHealths[i];
            
            // Apply damage to the plate
            _ArmourPlateHealths[i] -= damageToThisPlate;
            remainingDamage -= damageToThisPlate;
            
            // Update the UI for this plate
            UpdateArmourPlateUI(i, previousHealth > 0 && _ArmourPlateHealths[i] <= 0);
            
            // If plate is destroyed, reduce active count
            if (_ArmourPlateHealths[i] <= 0)
            {
                _activeArmourPlates--;
                
                // If showing empty plates, update UI to show empty state
                if (_showEmptyArmourPlates && i < _ArmourPlateImages.Count && _ArmourPlateImages[i] != null)
                {
                    _ArmourPlateImages[i].DOColor(_emptyArmourColor, _ArmourSlideAnimTime * 0.5f);
                }
            }
        }
    }

    /// <summary>
    /// Animates an armor plate with a simple slide effect without bounce or shake
    /// </summary>
    private void AnimatePlateSlide(int plateIndex, bool isRefill, float intensityMultiplier = 1.0f)
    {
        // Make sure we have valid UI references
        if (plateIndex >= 0 && plateIndex < _ArmourPlateImages.Count && _ArmourPlateImages[plateIndex] != null)
        {
            // Kill any existing animations on this plate
            DOTween.Kill(_ArmourPlateImages[plateIndex].rectTransform);
            DOTween.Kill($"PlateScale_{plateIndex}");
            DOTween.Kill($"PlateScaleReset_{plateIndex}");
            DOTween.Kill($"PlateSlide_{plateIndex}");
            
            // Get the original position as the base
            Vector2 originalPos = _originalPlatePositions[plateIndex];
            
            if (isRefill)
            {
                // For refill: Simple fade-in animation instead of position movement
                // This avoids any position changes that could cause shaking
                
                // First reset position exactly to original
                _ArmourPlateImages[plateIndex].rectTransform.anchoredPosition = originalPos;
                
                // Then do a simple alpha fade-in animation
                if (_ArmourPlateImages[plateIndex].color.a < 1.0f)
                {
                    Color startColor = _ArmourPlateImages[plateIndex].color;
                    Color targetColor = new Color(
                        _fullArmourColor.r, 
                        _fullArmourColor.g, 
                        _fullArmourColor.b, 
                        1.0f
                    );
                    
                    _ArmourPlateImages[plateIndex].DOColor(targetColor, _plateSlideAnimTime)
                        .SetEase(Ease.InQuad)
                        .SetId($"PlateColor_{plateIndex}");
                }
                
                // Reset scale to ensure consistency
                _ArmourPlateImages[plateIndex].transform.localScale = Vector3.one;
            }
            else
            {
                // For damage: Just do a quick pulse/scale effect instead of position movement
                
                // Reset position to original (in case it was moved by another animation)
                _ArmourPlateImages[plateIndex].rectTransform.anchoredPosition = originalPos;
                
                // Quick scale pulse instead of position change
                _ArmourPlateImages[plateIndex].transform
                    .DOScale(Vector3.one * (1 - 0.1f * intensityMultiplier), _plateSlideAnimTime * 0.5f)
                    .SetEase(Ease.OutQuad)
                    .SetId($"PlateScale_{plateIndex}")
                    .OnComplete(() => {
                        // Return to normal scale
                        _ArmourPlateImages[plateIndex].transform
                            .DOScale(Vector3.one, _plateSlideAnimTime * 0.5f)
                            .SetEase(Ease.InQuad)
                            .SetId($"PlateScaleReset_{plateIndex}");
                    });
            }
        }
    }
    
    /// <summary>
    /// Updates the visual display for a specific Armour plate
    /// </summary>
    /// <param name="plateIndex">The index of the plate to update</param>
    /// <param name="wasDestroyed">True if the plate was just destroyed</param>
    private void UpdateArmourPlateUI(int plateIndex, bool wasDestroyed = false)
    {
        // Ensure valid index and UI reference
        if (plateIndex >= 0 && plateIndex < _ArmourPlateImages.Count && _ArmourPlateImages[plateIndex] != null)
        {
            float fillRatio;
            
            // If plate is empty and we're showing empty plates, keep fill at 1 but with empty color
            if (_ArmourPlateHealths[plateIndex] <= 0 && _showEmptyArmourPlates)
            {
                fillRatio = 1f;
                _ArmourPlateImages[plateIndex].DOColor(_emptyArmourColor, _ArmourSlideAnimTime * 0.5f);
            }
            else
            {
                // Calculate fill ratio based on current health
                fillRatio = Mathf.Max(0, _ArmourPlateHealths[plateIndex] / _maxArmourPlateHealth);
                
                // Animate the plate fill level
                _ArmourPlateImages[plateIndex].DOFillAmount(fillRatio, _ArmourSlideAnimTime)
                    .SetEase(Ease.OutQuad);
                
                // MODIFIED: Always use full armor color regardless of health percentage
                if (fillRatio > 0)
                {
                    _ArmourPlateImages[plateIndex].DOColor(_fullArmourColor, _ArmourSlideAnimTime * 0.5f);
                }
                else if (!_showEmptyArmourPlates)
                {
                    // Hide the plate completely if we're not showing empty plates
                    _ArmourPlateImages[plateIndex].DOFillAmount(0, _ArmourSlideAnimTime)
                        .SetEase(Ease.OutQuad);
                }
            }
        }
    }

    /// <summary>
    /// Animates and updates the health counter displays
    /// Uses two separate counters for current and max health
    /// </summary>
    /// <param name="immediate">If true, updates instantly without animation</param>
    private void UpdateHealthDisplay(bool immediate = false)
    {
        int targetCurrentHealth = Mathf.RoundToInt(_currentHealth);
        int targetMaxHealth = Mathf.RoundToInt(_baseMaxHealth);
        
        // Update current health counter
        if (_currentHealthText != null)
        {
            if (immediate)
            {
                _displayedCurrentHealth = targetCurrentHealth;
                _currentHealthText.text = targetCurrentHealth.ToString(_healthNumberFormat);
            }
            else
            {
                // Animate the current health counter
                DOTween.To(() => _displayedCurrentHealth, x => {
                    _displayedCurrentHealth = x;
                    _currentHealthText.text = x.ToString(_healthNumberFormat);
                }, targetCurrentHealth, _healthCounterAnimTime)
                .SetEase(Ease.OutQuad);
            }
        }
        
        // Update max health counter
        if (_maxHealthText != null)
        {
            if (immediate)
            {
                _displayedMaxHealth = targetMaxHealth;
                _maxHealthText.text = targetMaxHealth.ToString(_healthNumberFormat);
            }
            else
            {
                // Animate the max health counter
                DOTween.To(() => _displayedMaxHealth, x => {
                    _displayedMaxHealth = x;
                    _maxHealthText.text = x.ToString(_healthNumberFormat);
                }, targetMaxHealth, _healthCounterAnimTime)
                .SetEase(Ease.OutQuad);
            }
        }
    }

    /// <summary>
    /// Adds health to the player, not exceeding maximum
    /// </summary>
    /// <param name="amount">Amount of health to add</param>
    /// <returns>Amount of health actually added</returns>
    public float AddHealth(float amount)
    {
        float originalHealth = _currentHealth;
        _currentHealth = Mathf.Min(_currentHealth + amount, _baseMaxHealth);
        UpdateHealthDisplay();
        
        // Return how much health was actually added
        return _currentHealth - originalHealth;
    }

    /// <summary>
    /// Increases the player's maximum health
    /// </summary>
    /// <param name="amount">Amount to increase max health by</param>
    /// <param name="healToNewMax">If true, also increases current health by the same amount</param>
    public void IncreaseMaxHealth(float amount, bool healToNewMax = false)
    {
        _baseMaxHealth += amount;
        
        // Optionally, also heal the player by the same amount
        if (healToNewMax)
        {
            _currentHealth += amount;
        }
        
        UpdateHealthDisplay();
    }

    /// <summary>
    /// Adds an Armour plate to the player
    /// </summary>
    /// <returns>True if successful, false if already at max plates</returns>
    public bool AddArmourPlate()
    {
        // Find the first empty plate slot
        for (int i = 0; i < 3; i++)
        {
            if (_ArmourPlateHealths[i] <= 0)
            {
                // Add a new full plate
                _ArmourPlateHealths[i] = _maxArmourPlateHealth;
                _activeArmourPlates++;
                
                // Animate the plate sliding in from right
                AnimatePlateSlide(i, true);
                
                // Update UI
                UpdateArmourPlateUI(i);
                return true;
            }
        }
        return false;
    }
    
    /// <summary>
    /// Repairs a damaged armor plate to full health
    /// </summary>
    /// <returns>True if successful, false if no damaged plate was found</returns>
    public bool RepairArmourPlate()
    {
        // First check if we have any damaged plates (not empty, not full)
        for (int i = 0; i < 3; i++)
        {
            // Look for plates that exist but are damaged
            if (_ArmourPlateHealths[i] > 0 && _ArmourPlateHealths[i] < _maxArmourPlateHealth)
            {
                float previousHealth = _ArmourPlateHealths[i];
                
                // Repair to full health
                _ArmourPlateHealths[i] = _maxArmourPlateHealth;
                
                // Animate the plate repair
                AnimatePlateSlide(i, true, 0.5f);
                
                // Update UI
                UpdateArmourPlateUI(i);

                return true;
            }
        }
        
        // No damaged plates found, try adding a new one instead
        return AddArmourPlate();
    }
    
    /// <summary>
    /// Gets the total remaining Armour across all plates
    /// </summary>
    private float GetTotalRemainingArmour()
    {
        float total = 0f;
        for (int i = 0; i < 3; i++)
        {
            total += _ArmourPlateHealths[i];
        }
        return total;
    }
    
    /// <summary>
    /// Gets the current value of a specific Armour plate
    /// </summary>
    /// <param name="plateIndex">Index of the Armour plate (0-2)</param>
    public float GetArmourPlateHealth(int plateIndex)
    {
        if (plateIndex >= 0 && plateIndex < 3)
        {
            return _ArmourPlateHealths[plateIndex];
        }
        return 0f;
    }
    
    /// <summary>
    /// Gets the maximum health of a single Armour plate
    /// </summary>
    public float GetMaxArmourPlateHealth()
    {
        return _maxArmourPlateHealth;
    }

    /// <summary>
    /// Handles armor purchase or repair
    /// </summary>
    /// <param name="cost">Money cost for the operation</param>
    /// <param name="playerMoney">Reference to player's current money</param>
    /// <returns>True if purchase was successful, false if not enough money or already at max armor</returns>
    public bool PurchaseOrRepairArmour(int cost, ref int playerMoney)
    {
        // Check if player has enough money
        if (playerMoney < cost)
        {
            return false;
        }
        
        // Try to repair any damaged plates first, or add a new one if none are damaged
        bool success = RepairArmourPlate();
        
        // If successful, deduct cost
        if (success)
        {
            playerMoney -= cost;
            return true;
        }
        
        return false;
    }
}