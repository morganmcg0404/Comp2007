using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the visibility and state of Heads-Up Display (HUD) elements in the game
/// Controls which UI components are shown or hidden based on user preferences
/// </summary>
public class HUDManager : MonoBehaviour
{
    [Header("HUD Element References")]
    [SerializeField] private GameObject healthDisplay;
    [SerializeField] private GameObject ammoDisplay;
    [SerializeField] private GameObject armorDisplay;
    [SerializeField] private GameObject pointsDisplay;
    [SerializeField] private GameObject waveDisplay;
    [SerializeField] private GameObject staminaDisplay;
    [SerializeField] private GameObject crosshair;
    
    [Header("Settings")]
    [SerializeField] private bool checkSettingsOnStart = true;
    
    /// <summary>
    /// Tracks whether the HUD is currently visible
    /// </summary>
    private bool isHUDVisible = true;
    
    /// <summary>
    /// Initializes the HUD visibility based on saved player preferences
    /// </summary>
    private void Start()
    {
        // Check settings on start
        if (checkSettingsOnStart)
        {
            // Load setting from PlayerPrefs
            isHUDVisible = PlayerPrefs.GetInt("ShowHUD", 1) == 1;
            
            // Apply initial state
            SetHUDVisibility(isHUDVisible);
        }
    }
    
    /// <summary>
    /// Toggles the visibility of all HUD elements and saves the preference
    /// </summary>
    public void ToggleHUD()
    {
        SetHUDVisibility(!isHUDVisible);
        
        // Save setting to PlayerPrefs
        PlayerPrefs.SetInt("ShowHUD", isHUDVisible ? 1 : 0);
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// Sets the visibility of all HUD elements directly
    /// </summary>
    /// <param name="visible">True to show HUD elements, false to hide them</param>
    public void SetHUDVisibility(bool visible)
    {
        isHUDVisible = visible;
        
        // Update individual HUD elements
        if (healthDisplay) healthDisplay.SetActive(visible);
        if (ammoDisplay) ammoDisplay.SetActive(visible);
        if (armorDisplay) armorDisplay.SetActive(visible);
        if (pointsDisplay) pointsDisplay.SetActive(visible);
        if (waveDisplay) waveDisplay.SetActive(visible);
        if (staminaDisplay) staminaDisplay.SetActive(visible);
        if (crosshair) crosshair.SetActive(visible);
    }
    
    /// <summary>
    /// Static method that updates HUD visibility across the game
    /// Can be called by other systems like GameSettings when preferences change
    /// </summary>
    /// <param name="visible">True to show HUD elements, false to hide them</param>
    public static void UpdateHUDVisibility(bool visible)
    {
        // Find the HUDManager in the scene and update it
        HUDManager hudManager = FindFirstObjectByType<HUDManager>();
        if (hudManager != null)
        {
            hudManager.SetHUDVisibility(visible);
        }
    }
    
    /// <summary>
    /// Gets the current visibility state of the HUD
    /// </summary>
    /// <returns>True if the HUD is currently visible, false otherwise</returns>
    public bool IsHUDVisible()
    {
        return isHUDVisible;
    }
    
    /// <summary>
    /// Sets visibility of a specific HUD element by name
    /// </summary>
    /// <param name="elementName">Name of the HUD element to modify (e.g., "health", "ammo")</param>
    /// <param name="visible">True to show the element, false to hide it</param>
    /// <returns>True if the element was found and modified, false otherwise</returns>
    public bool SetElementVisibility(string elementName, bool visible)
    {
        switch (elementName.ToLower())
        {
            case "health":
                if (healthDisplay) 
                {
                    healthDisplay.SetActive(visible);
                    return true;
                }
                break;
            case "ammo":
                if (ammoDisplay) 
                {
                    ammoDisplay.SetActive(visible);
                    return true;
                }
                break;
            case "armor":
                if (armorDisplay) 
                {
                    armorDisplay.SetActive(visible);
                    return true;
                }
                break;
            case "points":
                if (pointsDisplay) 
                {
                    pointsDisplay.SetActive(visible);
                    return true;
                }
                break;
            case "wave":
                if (waveDisplay) 
                {
                    waveDisplay.SetActive(visible);
                    return true;
                }
                break;
            case "stamina":
                if (staminaDisplay) 
                {
                    staminaDisplay.SetActive(visible);
                    return true;
                }
                break;
            case "crosshair":
                if (crosshair) 
                {
                    crosshair.SetActive(visible);
                    return true;
                }
                break;
        }
        
        return false; // Element not found
    }
}