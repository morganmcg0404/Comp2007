using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Linq;
using UnityEngine.InputSystem;

public class PauseMenu : MonoBehaviour
{
    [Header("Pause Menu UI")]
    [SerializeField] private GameObject pauseMenuUI;
    [SerializeField] private GameObject pauseMenuPanel;
    
    [Header("Background Overlay")]
    [SerializeField] private Image backgroundOverlay;
    [SerializeField] private float overlayAlpha = 0.7f;
    
    [Header("Settings")]
    [SerializeField] private KeyCode pauseKey = KeyCode.Escape;
    [SerializeField] private bool debugMode = true; // Enable for debugging
    
    [Header("Menu Navigation")]
    [SerializeField] private GameObject settingsPanel; // Reference to the settings panel
    [SerializeField] private GameObject mainMenuPanel; // Reference to the main pause menu panel

    // State tracking
    private bool isPaused = false;
    private bool isInSettingsMenu = false;
    
    // Store the original time scale
    private float originalTimeScale;
    
    // Reference to the event system
    private EventSystem eventSystem;
    
    // Reference to settings script (optional)
    private GameSettings settingsScript;
    
    private void Awake()
    {
        // Store the original time scale (usually 1.0f)
        originalTimeScale = Time.timeScale;
        
        // Get reference to the event system
        eventSystem = EventSystem.current;
        if (eventSystem == null && debugMode)
        {
            Debug.LogError("No EventSystem found in the scene. UI interactions will not work.");
        }
        
        // Try to find the settings script if it exists
        settingsScript = FindFirstObjectByType<GameSettings>();
        
        // Ensure menu is hidden at start
        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(false);
        else if (debugMode)
            Debug.LogError("Pause Menu UI reference is missing. Please assign it in the inspector.");
            
        // Set the background overlay to be transparent at start
        if (backgroundOverlay != null)
        {
            Color color = backgroundOverlay.color;
            color.a = 0f;
            backgroundOverlay.color = color;
            backgroundOverlay.gameObject.SetActive(false);
        }
        else if (debugMode)
            Debug.LogWarning("Background overlay reference is missing. The background won't darken when paused.");
        
        // Make sure settings panel is hidden initially
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }
    
    private void Update()
    {
        // Safety check - if we think we're paused but the game is running, force pause
        if (isPaused && Time.timeScale != 0f && !isInSettingsMenu)
        {
            Debug.LogWarning("Pause state mismatch detected! Forcing pause state.");
            Time.timeScale = 0f;
            PauseManager.SetPaused(true);
        }
        
        // Check for pause key press
        if (Input.GetKeyDown(pauseKey))
        {
            // First check if the settings menu is visible through the GameSettings script
            bool isSettingsVisible = settingsScript != null && settingsScript.IsVisible();
            
            // If settings are visible, let the settings script handle it
            if (isSettingsVisible)
            {
                return;
            }
            
            // If we're in settings (old method), go back to main pause menu
            else if (isPaused && isInSettingsMenu)
            {
                BackToMainPauseMenu();
            }
            // Otherwise toggle the pause state
            else
            {
                TogglePause();
            }
        }
    }
    
    public void TogglePause()
    {
        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }
    
    public void PauseGame()
    {
        // Set paused state
        isPaused = true;
        PauseManager.SetPaused(true);
        
        // Stop time
        Time.timeScale = 0f;
        
        // Enable cursor for menu interaction
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        
        // Show the main menu panel, hide settings panel
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(true);
            
            // Ensure we're showing the main pause menu, not settings
            if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            isInSettingsMenu = false;
            
            // Clear selection instead
            if (eventSystem != null)
            {
                eventSystem.SetSelectedGameObject(null);
            }
        }
            
        // Show and fade in the background overlay
        if (backgroundOverlay != null)
        {
            backgroundOverlay.gameObject.SetActive(true);
            Color color = backgroundOverlay.color;
            color.a = overlayAlpha;
            backgroundOverlay.color = color;
        }
        
        // Be selective about which controllers to disable
        DisableGameplayControllers();
    }
    
    public void ResumeGame()
    {
        // Set unpaused state
        isPaused = false;
        PauseManager.SetPaused(false);
        
        // Resume time
        Time.timeScale = originalTimeScale;
        
        // Hide cursor if your game uses locked cursor
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        
        // Hide the menu
        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(false);
            
        // Hide the background overlay
        if (backgroundOverlay != null)
        {
            Color color = backgroundOverlay.color;
            color.a = 0f;
            backgroundOverlay.color = color;
            backgroundOverlay.gameObject.SetActive(false);
        }
        
        // Re-enable player input scripts
        EnablePlayerInput();
        
        // Clear any pending input actions to prevent actions queuing up during pause
        ClearInputBuffers();

        // Optional: add a small delay before accepting new inputs
        StartCoroutine(InputCooldownRoutine(0.1f));
    }
    
    // Methods for UI buttons
    
    public void OnResumeButtonClicked()
    {
        // Clear any settings menu references first
        isInSettingsMenu = false;
        
        // Close the settings menu if it's open
        if (settingsScript != null && settingsScript.IsVisible())
        {
            settingsScript.CloseSettingsMenu();
        }
        
        // Resume the game
        ResumeGame();
    }
    
    public void OnMainMenuButtonClicked()
    {
        // Reset time scale before loading a new scene
        Time.timeScale = originalTimeScale;
        
        // Load main menu scene
        SceneManager.LoadScene("MainMenu"); // Replace with your actual main menu scene name
    }
    
    public void OnRestartButtonClicked()
    {
        // Reset time scale before restarting
        Time.timeScale = originalTimeScale;
        
        // Reload the current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    
    public void OnQuitButtonClicked()
    {
        // Quit the application (works in builds, not in editor)
        Application.Quit();
        
        // Optional: For testing in editor
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
    
    public void OnSettingsButtonClicked()
    {
        // Find the settings script if not already assigned
        if (settingsScript == null)
            settingsScript = FindFirstObjectByType<GameSettings>();
        
        if (settingsScript != null)
        {
            // Hide the pause menu panels but KEEP the background overlay
            if (mainMenuPanel != null)
                mainMenuPanel.SetActive(false);
            
            // Note: We're NOT activating settingsPanel here anymore since
            // we're using the external GameSettings UI
            
            // Keep track that we're in settings
            isInSettingsMenu = true;
            
            // Subscribe to the settings closed event if not already subscribed
            settingsScript.OnSettingsMenuClosed -= OnSettingsMenuClosed; // Remove any existing subscription first
            settingsScript.OnSettingsMenuClosed += OnSettingsMenuClosed;
            
            // Show the settings menu
            settingsScript.ShowSettingsMenu();
        }
        else
        {
            Debug.LogError("GameSettings script not found in scene! Make sure you have a GameObject with GameSettings component in your scene.");
        }
    }

    private void OnSettingsMenuClosed()
    {
        // Unsubscribe from the event
        if (settingsScript != null)
        {
            settingsScript.OnSettingsMenuClosed -= OnSettingsMenuClosed;
        }
        
        // CRITICAL - Ensure we immediately set these before any other code runs
        isPaused = true;
        PauseManager.SetPaused(true);
        Time.timeScale = 0f;
        
        // Show the pause menu again
        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(true);
            
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);
        
        // We're no longer in settings
        isInSettingsMenu = false;
        
        // Clear selection instead
        if (eventSystem != null)
        {
            eventSystem.SetSelectedGameObject(null);
        }
    }
    
    public void BackToMainPauseMenu()
    {
        // This method is now just for backward compatibility - we're using GameSettings directly
        if (settingsScript != null)
        {
            settingsScript.CloseSettingsMenu(); // This will trigger OnSettingsMenuClosed
        }
        else
        {
            // Fallback to old behavior if needed
            if (settingsPanel != null && mainMenuPanel != null)
            {
                settingsPanel.SetActive(false);
                mainMenuPanel.SetActive(true);
                isInSettingsMenu = false;
            }
        }
    }
    
    // Helper methods to disable/enable player input
    // You may need to modify these based on your player controller implementation
    private void DisableGameplayControllers()
    {
        // Find and disable player controller scripts, but avoid UI-related ones
        var playerControllers = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .Where(mb => 
                (mb.GetType().Name.Contains("Controller") || mb.GetType().Name.Contains("Input")) &&
                !mb.GetType().Name.Contains("UI") &&
                !mb.GetType().Name.Contains("EventSystem") &&
                !mb.GetType().Name.Contains("PauseMenu")
            )
            .ToArray();
            
        foreach (var controller in playerControllers)
        {       
            controller.enabled = false;
        }
    }
    
    private void EnablePlayerInput()
    {
        // Re-enable player controller scripts
        var playerControllers = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .Where(mb => 
                (mb.GetType().Name.Contains("Controller") || mb.GetType().Name.Contains("Input")) &&
                !mb.GetType().Name.Contains("UI") &&
                !mb.GetType().Name.Contains("EventSystem") &&
                !mb.GetType().Name.Contains("PauseMenu")
            )
            .ToArray();
            
        foreach (var controller in playerControllers)
        {
            controller.enabled = true;
        }
    }
    
    // Clears any pending input that may have accumulated during pause
    private void ClearInputBuffers()
    {
        // Clear old input system buffers
        Input.ResetInputAxes();
        
        // If you're using the new Input System, also clear its state
        #if ENABLE_INPUT_SYSTEM
        if (Gamepad.current != null)
            InputSystem.ResetDevice(Gamepad.current);
        if (Keyboard.current != null)
            InputSystem.ResetDevice(Keyboard.current);
        if (Mouse.current != null)
            InputSystem.ResetDevice(Mouse.current);
        #endif
        
        // Add a small delay before accepting inputs again
        Time.timeScale = originalTimeScale;
    }

    // Creates a brief cooldown period before accepting new inputs
    private System.Collections.IEnumerator InputCooldownRoutine(float duration)
    {
        // Create a temporary component to disable player input
        var playerControllers = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .Where(mb => 
                (mb.GetType().Name.Contains("Controller") || mb.GetType().Name.Contains("Input")) &&
                !mb.GetType().Name.Contains("UI") &&
                !mb.GetType().Name.Contains("EventSystem") &&
                !mb.GetType().Name.Contains("PauseMenu")
            )
            .ToArray();
        
        // Wait for the real-time delay (not affected by Time.timeScale)
        float endTime = Time.unscaledTime + duration;
        while (Time.unscaledTime < endTime)
        {
            yield return null;
        }
        
        // Re-enable input controllers after the delay
        foreach (var controller in playerControllers)
        {
            controller.enabled = true;
        }
    }
}
