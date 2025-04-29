using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Handles the display and calculation of frames per second (FPS) information
/// </summary>
public class FPSCounter : MonoBehaviour
{
    [SerializeField] private TMP_Text fpsText;
    [SerializeField] private float updateInterval = 0.5f;
    
    private float accum = 0f;
    private int frames = 0;
    private float timeleft;
    private bool isVisible = false;
    
    /// <summary>
    /// Initializes the FPS counter with saved visibility settings
    /// </summary>
    void Awake()
    {
        // Check if we should display FPS based on saved setting
        isVisible = PlayerPrefs.GetInt("ShowFPS", 0) == 1;
        
        // Initialize timer
        timeleft = updateInterval;
        
        // Apply initial visibility state
        ApplyVisibility();
    }
    
    /// <summary>
    /// Secondary initialization to ensure correct visibility state is applied
    /// </summary>
    void Start()
    {
        // Secondary check to make sure we apply the correct visibility
        ApplyVisibility();
    }
    
    /// <summary>
    /// Updates FPS calculation and display at the specified interval
    /// </summary>
    void Update()
    {
        // Only calculate FPS if the counter is visible
        if (!isVisible) 
            return;
            
        // Update FPS counter
        timeleft -= Time.deltaTime;
        accum += Time.timeScale / Time.deltaTime;
        frames++;
        
        if (timeleft <= 0.0)
        {
            float fps = accum / frames;
            string format = string.Format("{0:F1} FPS", fps);
            
            if (fpsText != null)
            {
                fpsText.text = format;
                
                // Change color based on FPS
                if (fps < 30)
                    fpsText.color = Color.red;
                else if (fps < 60)
                    fpsText.color = Color.yellow;
                else
                    fpsText.color = Color.green;
            }
            
            timeleft = updateInterval;
            accum = 0.0f;
            frames = 0;
        }
    }
    
    /// <summary>
    /// Sets the visibility state of the FPS counter
    /// </summary>
    /// <param name="visible">Whether the FPS counter should be visible</param>
    public void SetVisible(bool visible)
    {
        isVisible = visible;
        ApplyVisibility();
    }
    
    /// <summary>
    /// Applies the current visibility setting to the UI element
    /// </summary>
    private void ApplyVisibility()
    {
        // Make sure the Text component exists
        if (fpsText != null)
        {
            fpsText.gameObject.SetActive(isVisible);
        }
        else
        {
            Debug.LogError("FPS Text component is missing. Please assign it in the FPSCounter inspector.");
        }
    }
    
    /// <summary>
    /// Unity editor callback to validate component setup
    /// </summary>
    private void OnValidate()
    {
        if (fpsText == null)
        {
            Debug.LogWarning("FPS Counter is missing its TextMeshPro reference. Please assign it in the inspector.");
        }
    }
    
    /// <summary>
    /// Toggles visibility of the FPS counter
    /// </summary>
    public void ToggleVisibility()
    {
        SetVisible(!isVisible);
        
        // Save preference
        PlayerPrefs.SetInt("ShowFPS", isVisible ? 1 : 0);
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// Gets the current visibility state of the FPS counter
    /// </summary>
    /// <returns>True if the FPS counter is visible, false otherwise</returns>
    public bool IsVisible()
    {
        return isVisible;
    }
    
    /// <summary>
    /// Gets the current FPS value
    /// </summary>
    /// <returns>The current calculated frames per second value</returns>
    public float GetCurrentFPS()
    {
        if (frames > 0)
            return accum / frames;
        return 0f;
    }
}