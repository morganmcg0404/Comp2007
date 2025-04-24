using UnityEngine;
using UnityEngine.UI;

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
    
    // Cache the state
    private bool isHUDVisible = true;
    
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
    
    // Call this method to toggle the HUD visibility
    public void ToggleHUD()
    {
        SetHUDVisibility(!isHUDVisible);
        
        // Save setting to PlayerPrefs
        PlayerPrefs.SetInt("ShowHUD", isHUDVisible ? 1 : 0);
        PlayerPrefs.Save();
    }
    
    // Call this method to set the HUD visibility directly
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
    
    // This method can be called by the GameSettings script when the ShowHUD toggle is changed
    public static void UpdateHUDVisibility(bool visible)
    {
        // Find the HUDManager in the scene and update it
        HUDManager hudManager = FindFirstObjectByType<HUDManager>();
        if (hudManager != null)
        {
            hudManager.SetHUDVisibility(visible);
        }
    }
}