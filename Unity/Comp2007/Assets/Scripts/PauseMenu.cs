using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Linq;
using UnityEngine.InputSystem;

/// <summary>
/// Manages the pause menu functionality including UI display, time manipulation, and cursor handling
/// Handles transitioning between game, pause menu, and settings views
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [Header("Pause Menu UI")]
    /// <summary>
    /// Root GameObject containing all pause menu UI elements
    /// </summary>
    [SerializeField] private GameObject pauseMenuUI;
    
    /// <summary>
    /// Panel containing the main pause menu options (resume, settings, etc.)
    /// </summary>
    [SerializeField] private GameObject pauseMenuPanel;
    
    [Header("Background Overlay")]
    /// <summary>
    /// Image used as darkened background overlay when game is paused
    /// </summary>
    [SerializeField] private Image backgroundOverlay;
    
    /// <summary>
    /// Alpha transparency value for the background overlay (0-1)
    /// </summary>
    [SerializeField] private float overlayAlpha = 0.7f;
    
    [Header("Settings")]
    /// <summary>
    /// Key used to toggle the pause menu on/off
    /// </summary>
    [SerializeField] private KeyCode pauseKey = KeyCode.Escape;
    
    /// <summary>
    /// When enabled, shows additional debug warnings and error messages
    /// </summary>
    [SerializeField] private bool debugMode = true; // Enable for debugging
    
    [Header("Menu Navigation")]
    /// <summary>
    /// Panel containing game settings options
    /// </summary>
    [SerializeField] private GameObject settingsPanel; // Reference to the settings panel
    
    /// <summary>
    /// Panel containing main pause menu options
    /// </summary>
    [SerializeField] private GameObject mainMenuPanel; // Reference to the main pause menu panel

    [Header("Confirmation Dialog")]
    /// <summary>
    /// Dialog panel that appears when asking to restart level
    /// </summary>
    [SerializeField] private GameObject confirmResetDialog;

    /// <summary>
    /// Button for confirming restart level
    /// </summary>
    [SerializeField] private Button resetYesButton;

    /// <summary>
    /// Button for rejecting restart level
    /// </summary>
    [SerializeField] private Button resetNoButton;

    /// <summary>
    /// Dialog panel that appears when asking to return to main menu
    /// </summary>
    [SerializeField] private GameObject confirmMainMenuDialog;

    /// <summary>
    /// Button for confirming return to main menu
    /// </summary>
    [SerializeField] private Button mainMenuYesButton;

    /// <summary>
    /// Button for rejecting return to main menu
    /// </summary>
    [SerializeField] private Button mainMenuNoButton;

    /// <summary>
    /// Dialog panel that appears when asking to Quit game
    /// </summary>
    [SerializeField] private GameObject confirmQuitDialog;

    /// <summary>
    /// Button for confirming quit action
    /// </summary>
    [SerializeField] private Button quitYesButton;

    /// <summary>
    /// Button for rejecting quit action
    /// </summary>
    [SerializeField] private Button quitNoButton;

    // State tracking
    /// <summary>
    /// Tracks whether the game is currently paused
    /// </summary>
    private bool isPaused = false;
    
    /// <summary>
    /// Tracks whether the settings menu is currently open
    /// </summary>
    private bool isInSettingsMenu = false;
    
    /// <summary>
    /// Stores the original time scale to restore when unpausing
    /// </summary>
    private float originalTimeScale;
    
    /// <summary>
    /// Reference to the scene's EventSystem for UI navigation
    /// </summary>
    private EventSystem eventSystem;
    
    /// <summary>
    /// Reference to the external settings manager
    /// </summary>
    private GameSettings settingsScript;
    
    /// <summary>
    /// Initializes the pause menu system and ensures it's hidden at game start
    /// </summary>
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

        // Hide confirmation dialogs at startup
        if (confirmResetDialog != null)
            confirmResetDialog.SetActive(false);
        if (confirmMainMenuDialog != null)
            confirmMainMenuDialog.SetActive(false);
        if (confirmQuitDialog != null)
            confirmQuitDialog.SetActive(false);
    }
    
    /// <summary>
    /// Checks for pause input and ensures pause state consistency
    /// </summary>
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
    
    /// <summary>
    /// Toggles between paused and unpaused states
    /// </summary>
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
    
    /// <summary>
    /// Pauses the game by stopping time, showing the pause menu, and unlocking the cursor
    /// </summary>
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
    
    /// <summary>
    /// Resumes the game by restoring time, hiding the pause menu, and relocking the cursor
    /// </summary>
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
    
    /// <summary>
    /// Handles the Resume button click in the pause menu
    /// </summary>
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
    
    /// <summary>
    /// Handles the Main Menu button click, showing confirmation before returning to main menu
    /// </summary>
    public void OnMainMenuButtonClicked()
    {
        // Show confirmation dialog
        if (confirmMainMenuDialog != null)
        {
            // Show the confirmation dialog
            confirmMainMenuDialog.SetActive(true);
            
            // Set up the button listeners
            if (mainMenuYesButton != null)
            {
                mainMenuYesButton.onClick.RemoveAllListeners();
                mainMenuYesButton.onClick.AddListener(ReturnToMainMenu);
            }
            
            if (mainMenuNoButton != null)
            {
                mainMenuNoButton.onClick.RemoveAllListeners();
                mainMenuNoButton.onClick.AddListener(CloseMainMenuConfirmation);
            }
            
            // Select the No button by default for safety
            if (mainMenuNoButton != null)
            {
                mainMenuNoButton.Select();
            }
        }
        else
        {
            // No confirmation dialog, proceed directly
            ReturnToMainMenu();
        }
    }

    /// <summary>
    /// Handles the Restart button click, showing confirmation before restarting
    /// </summary>
    public void OnRestartButtonClicked()
    {
        // Show confirmation dialog
        if (confirmResetDialog != null)
        {
            // Show the confirmation dialog
            confirmResetDialog.SetActive(true);
            
            // Set up the button listeners
            if (resetYesButton != null)
            {
                resetYesButton.onClick.RemoveAllListeners();
                resetYesButton.onClick.AddListener(RestartLevel);
            }
            
            if (resetNoButton != null)
            {
                resetNoButton.onClick.RemoveAllListeners();
                resetNoButton.onClick.AddListener(CloseRestartConfirmation);
            }
            
            // Select the No button by default for safety
            if (resetNoButton != null)
            {
                resetNoButton.Select();
            }
        }
        else
        {
            // No confirmation dialog, proceed directly
            RestartLevel();
        }
    }

    /// <summary>
    /// Handles the Quit button click, showing confirmation before exiting
    /// </summary>
    public void OnQuitButtonClicked()
    {
        // Show confirmation dialog
        if (confirmQuitDialog != null)
        {
            // Show the confirmation dialog
            confirmQuitDialog.SetActive(true);
            
            // Set up the button listeners
            if (quitYesButton != null)
            {
                quitYesButton.onClick.RemoveAllListeners();
                quitYesButton.onClick.AddListener(QuitGame);
            }
            
            if (quitNoButton != null)
            {
                quitNoButton.onClick.RemoveAllListeners();
                quitNoButton.onClick.AddListener(CloseQuitConfirmation);
            }
            
            // Select the No button by default for safety
            if (quitNoButton != null)
            {
                quitNoButton.Select();
            }
        }
        else
        {
            // No confirmation dialog, proceed directly
            QuitGame();
        }
    }
    
    /// <summary>
    /// Returns to the main menu scene after confirmation
    /// Properly destroys objects marked with DontDestroyOnLoad before scene transition
    /// </summary>
    private void ReturnToMainMenu()
    {
        // Close the confirmation dialog first
        if (confirmMainMenuDialog != null)
        {
            confirmMainMenuDialog.SetActive(false);
        }

        // Find and destroy any DontDestroyOnLoad objects that should be reset
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Destroy(player);
        }
        
        // Find and destroy persistent game objects EXCEPT audio managers
        DestroyPersistentGameObjects("GameManager");
        DestroyPersistentGameObjects("WeaponManager");
        
        // Reset time scale before loading a new scene
        Time.timeScale = originalTimeScale;
        
        // Reset the pause state in PauseManager
        isPaused = false;
        PauseManager.SetPaused(false);
        
        // Load main menu scene
        SceneManager.LoadScene("MainMenu");
    }
    
    /// <summary>
    /// Restarts the current level after confirmation
    /// Properly destroys objects marked with DontDestroyOnLoad before restarting
    /// </summary>
    private void RestartLevel()
    {
        // Close the confirmation dialog first
        if (confirmResetDialog != null)
        {
            confirmResetDialog.SetActive(false);
        }

        // Find and destroy any DontDestroyOnLoad objects that should be reset
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Destroy(player);
        }
        
        // Find and destroy persistent game objects EXCEPT audio managers
        DestroyPersistentGameObjects("GameManager");
        DestroyPersistentGameObjects("WeaponManager");
        
        // Reset time scale before restarting
        Time.timeScale = originalTimeScale;
        
        // Reset the pause state in PauseManager
        isPaused = false;
        PauseManager.SetPaused(false);
        
        // Hide cursor if your game uses locked cursor
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        
        // Reload the current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    
    /// <summary>
    /// Finds and destroys persistent game objects with the specified tag or name
    /// Used to clean up objects marked with DontDestroyOnLoad before scene reload
    /// </summary>
    /// <param name="identifierName">The tag or name of the objects to find and destroy</param>
    /// <param name="useTag">If true, searches by tag; if false, searches by name</param>
    private void DestroyPersistentGameObjects(string identifierName, bool useTag = false)
    {
        // Skip destroying audio managers and their cameras
        if (identifierName == "SoundManager" || identifierName == "MusicManager" || 
            identifierName == "AudioCamera" || identifierName == "AudioListener")
        {
            return;
        }
        
        GameObject[] objectsToDestroy;
        
        if (useTag)
        {
            // Try to find objects by tag
            try
            {
                objectsToDestroy = GameObject.FindGameObjectsWithTag(identifierName);
            }
            catch
            {
                Debug.LogWarning("Tag not found: " + identifierName);
                return;
            }
        }
        else
        {
            // Find objects by name - using the non-obsolete method
            objectsToDestroy = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None)
                .Where(obj => obj.name == identifierName).ToArray();
        }
        
        foreach (GameObject obj in objectsToDestroy)
        {
            // Double-check to never destroy audio managers or their cameras
            if (obj.GetComponent<SoundManager>() != null || 
                obj.GetComponent<MusicManager>() != null ||
                obj.GetComponent<Camera>() != null && 
                (obj.name.Contains("Audio"))
                )
            {
                continue;
            }
            
            Destroy(obj);
        }
    }
    
    /// <summary>
    /// Quits the application after confirmation
    /// </summary>
    private void QuitGame()
    {
        // Close the confirmation dialog first
        if (confirmQuitDialog != null)
        {
            confirmQuitDialog.SetActive(false);
        }

        // Quit the application (works in builds, not in editor)
        Application.Quit();
        
        // Optional: For testing in editor
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
    
    /// <summary>
    /// Handles the Settings button click, showing the settings menu
    /// </summary>
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

    /// <summary>
    /// Event handler for when the settings menu is closed
    /// Restores the main pause menu and maintains the paused state
    /// </summary>
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
    
    /// <summary>
    /// Returns from settings view to the main pause menu
    /// Used for backward compatibility
    /// </summary>
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
    
    /// <summary>
    /// Disables gameplay-related controller scripts while paused
    /// Preserves UI-related scripts for menu interaction
    /// </summary>
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
    
    /// <summary>
    /// Re-enables gameplay controller scripts when unpausing
    /// Preserves runtime changes to mouse sensitivity
    /// </summary>
    private void EnablePlayerInput()
    {
        // CRITICAL: FIRST store current sensitivity before anything else
        float currentSensitivity = 1.0f;
        MouseLook mouseLook = FindAnyObjectByType<MouseLook>();
        if (mouseLook != null)
        {
            currentSensitivity = mouseLook.GetCurrentSensitivity();
            Debug.Log($"[PauseMenu] Preserving mouse sensitivity during unpause: {currentSensitivity}");
        }

        // Re-enable all controller scripts
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
        
        // IMPORTANT: Give a small delay before resetting the sensitivity
        // This ensures MouseLook's Start() method completes first if it was re-enabled
        StartCoroutine(RestoreSensitivityAfterDelay(currentSensitivity, 0.05f));
    }

    /// <summary>
    /// Restores the mouse sensitivity after a short delay
    /// </summary>
    /// <param name="sensitivity">The sensitivity value to restore</param>
    /// <param name="delay">Delay in seconds</param>
    private System.Collections.IEnumerator RestoreSensitivityAfterDelay(float sensitivity, float delay)
    {
        // Wait for the real-time delay
        yield return new WaitForSecondsRealtime(delay);
        
        // Find and set the sensitivity on all MouseLook components
        MouseLook[] mouseLooks = FindObjectsByType<MouseLook>(FindObjectsSortMode.None);
        foreach (MouseLook ml in mouseLooks)
        {
            if (ml != null)
            {
                // Force override the sensitivity and save it to PlayerPrefs
                ml.SetSensitivity(sensitivity);
                PlayerPrefs.SetFloat("MouseSensitivity", sensitivity);
                PlayerPrefs.Save();
                
                Debug.Log($"[PauseMenu] Restored mouse sensitivity to: {sensitivity}");
            }
        }
    }
    
    /// <summary>
    /// Clears any pending input that may have accumulated during pause
    /// Resets both old and new input systems to prevent unwanted actions
    /// </summary>
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

    /// <summary>
    /// Creates a brief cooldown period before accepting new inputs
    /// Prevents accidental input processing immediately after unpausing
    /// </summary>
    /// <param name="duration">Duration of the cooldown in seconds (real time)</param>
    /// <returns>IEnumerator for coroutine execution</returns>
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

    /// <summary>
    /// Closes the restart confirmation dialog without taking action
    /// </summary>
    private void CloseRestartConfirmation()
    {
        if (confirmResetDialog != null)
        {
            confirmResetDialog.SetActive(false);
        }
        
        // Restore focus to the pause menu
        if (eventSystem != null)
        {
            eventSystem.SetSelectedGameObject(null);
        }
    }

    /// <summary>
    /// Closes the main menu confirmation dialog without taking action
    /// </summary>
    private void CloseMainMenuConfirmation()
    {
        if (confirmMainMenuDialog != null)
        {
            confirmMainMenuDialog.SetActive(false);
        }
        
        // Restore focus to the pause menu
        if (eventSystem != null)
        {
            eventSystem.SetSelectedGameObject(null);
        }
    }

    /// <summary>
    /// Closes the quit confirmation dialog without taking action
    /// </summary>
    private void CloseQuitConfirmation()
    {
        if (confirmQuitDialog != null)
        {
            confirmQuitDialog.SetActive(false);
        }
        
        // Restore focus to the pause menu
        if (eventSystem != null)
        {
            eventSystem.SetSelectedGameObject(null);
        }
    }
}
