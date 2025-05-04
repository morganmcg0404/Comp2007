using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages game settings including audio, graphics, gameplay, and control options
/// Provides a UI-based settings menu with tab navigation and persistence via PlayerPrefs
/// </summary>
public class GameSettings : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private GameObject settingsMenuUI;          // The entire settings menu object
    [SerializeField] private GameObject settingsPanel;           // The panel containing the settings UI
    [SerializeField] private Image backgroundPanel;              // Background overlay for the settings menu
    [SerializeField] private float backgroundAlpha = 0.7f;       // Alpha value for background overlay
    
    [Header("Settings")]
    [SerializeField] private KeyCode backKey = KeyCode.Escape;   // Key to return to pause menu
    
    [Header("Tab Navigation")]
    [SerializeField] private GameObject gameplayTabContent;
    [SerializeField] private GameObject audioTabContent;
    [SerializeField] private GameObject graphicsTabContent;
    [SerializeField] private GameObject controlsTabContent;
    
    [SerializeField] private Button gameplayTabButton;
    [SerializeField] private Button audioTabButton;
    [SerializeField] private Button graphicsTabButton;
    [SerializeField] private Button controlsTabButton;
    
    [Header("Audio Settings")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private TMP_InputField masterVolumeInput;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private TMP_InputField musicVolumeInput;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private TMP_InputField sfxVolumeInput;
    
    [Header("Graphics Settings")]
    [SerializeField] private TMP_Dropdown fullscreenDropdown;
    [SerializeField] private TMP_Dropdown qualityDropdown;
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    
    [Header("Gameplay Settings")]
    [SerializeField] private Slider mouseSensitivitySlider;
    [SerializeField] private TMP_InputField mouseSensitivityInput;
    [SerializeField] private Slider aimSensitivitySlider;
    [SerializeField] private TMP_InputField aimSensitivityInput;
    [SerializeField] private Slider fovSlider;
    [SerializeField] private TMP_InputField fovInput;
    [SerializeField] private Toggle invertYToggle;
    [SerializeField] private Toggle showFPSToggle;
    [SerializeField] private FPSCounter fpsCounter;
    [SerializeField] private Toggle showHUDToggle;
    
    [Header("Controls Settings")]
    [SerializeField] private Toggle toggleAimToggle;
    
    [Header("Settings Storage")]
    [SerializeField] private bool loadOnAwake = true;
    [SerializeField] private bool hideOnAwake = true;
    
    // Theme colors for selected/unselected tabs
    [SerializeField] private Color selectedTabColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color unselectedTabColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    
    [Header("Confirmation Dialog")]
    /// <summary>
    /// Dialog panel that appears when asking to save settings
    /// </summary>
    [SerializeField] private GameObject confirmSaveDialog;

    /// <summary>
    /// Button for confirming save action
    /// </summary>
    [SerializeField] private Button saveYesButton;

    /// <summary>
    /// Button for rejecting save action
    /// </summary>
    [SerializeField] private Button saveNoButton;
    
    /// <summary>
    /// Event triggered when the settings menu is closed
    /// Useful for notifying other systems (like PauseMenu) to update their state
    /// </summary>
    public System.Action OnSettingsMenuClosed;
    
    private Resolution[] resolutions;
    private bool isVisible = false;
    
    /// <summary>
    /// Initializes the settings menu, sets up UI listeners, and loads saved settings
    /// </summary>
    private void Awake()
    {
        // Setup tab button listeners
        SetupTabButtons();
        
        // Setup input field listeners
        SetupInputFieldListeners();
        
        if (loadOnAwake)
            LoadSettings();
        
        // Initialize UI with saved values
        InitializeUI();
        
        // Show gameplay tab by default
        ShowGameplayTab();
        
        // Hide the settings menu at startup if specified
        if (hideOnAwake && settingsMenuUI != null)
        {
            settingsMenuUI.SetActive(false);
            isVisible = false;
        }
    }
    
    /// <summary>
    /// Handles user input to close the settings menu when the back key is pressed
    /// </summary>
    private void Update()
    {
        // Check for Escape key press to close settings
        if (isVisible && Input.GetKeyUp(backKey))
        {
            // Show confirmation dialog instead of closing directly
            ShowSaveConfirmationDialog();
        }
    }
    
    /// <summary>
    /// Configures tab button click listeners for tab navigation
    /// </summary>
    private void SetupTabButtons()
    {
        if (gameplayTabButton != null)
            gameplayTabButton.onClick.AddListener(ShowGameplayTab);
            
        if (audioTabButton != null)
            audioTabButton.onClick.AddListener(ShowAudioTab);
            
        if (graphicsTabButton != null)
            graphicsTabButton.onClick.AddListener(ShowGraphicsTab);
            
        if (controlsTabButton != null)
            controlsTabButton.onClick.AddListener(ShowControlsTab);
    }
    
    #region Menu Visibility Control
    
    /// <summary>
    /// Shows the settings menu and initializes UI elements with current values
    /// </summary>
    public void ShowSettingsMenu()
    {
        if (settingsMenuUI != null)
        {
            settingsMenuUI.SetActive(true);
            isVisible = true;
            
            // Also explicitly activate the settings panel
            if (settingsPanel != null)
                settingsPanel.SetActive(true);
            
            // Set background alpha
            if (backgroundPanel != null)
            {
                Color bgColor = backgroundPanel.color;
                bgColor.a = backgroundAlpha;
                backgroundPanel.color = bgColor;
            }
            
            // Always show gameplay tab first when opening settings
            ShowGameplayTab();
            
            // Refresh UI values
            InitializeUI();
        }
    }
    
    /// <summary>
    /// Closes the settings menu, with option to save settings
    /// </summary>
    public void CloseSettingsMenu()
    {
        ShowSaveConfirmationDialog();
    }
    
    /// <summary>
    /// Gets whether the settings menu is currently visible
    /// </summary>
    /// <returns>True if the settings menu is visible, false otherwise</returns>
    public bool IsVisible()
    {
        return isVisible;
    }
    
    /// <summary>
    /// Toggles the visibility of the settings menu
    /// </summary>
    public void ToggleSettingsMenu()
    {
        if (isVisible)
            CloseSettingsMenu();
        else
            ShowSettingsMenu();
    }
    
    #endregion
    
    /// <summary>
    /// Shows the gameplay settings tab and hides other tabs
    /// </summary>
    public void ShowGameplayTab()
    {
        SetActiveTab(gameplayTabContent, gameplayTabButton);
    }
    
    /// <summary>
    /// Shows the audio settings tab and hides other tabs
    /// </summary>
    public void ShowAudioTab()
    {
        SetActiveTab(audioTabContent, audioTabButton);
    }
    
    /// <summary>
    /// Shows the graphics settings tab and hides other tabs
    /// </summary>
    public void ShowGraphicsTab()
    {
        SetActiveTab(graphicsTabContent, graphicsTabButton);
    }
    
    /// <summary>
    /// Shows the controls settings tab and hides other tabs
    /// </summary>
    public void ShowControlsTab()
    {
        SetActiveTab(controlsTabContent, controlsTabButton);
    }
    
    /// <summary>
    /// Activates the specified tab and updates UI to show it as selected
    /// </summary>
    /// <param name="activeTab">The tab content to show</param>
    /// <param name="activeButton">The tab button to mark as selected</param>
    private void SetActiveTab(GameObject activeTab, Button activeButton)
    {
        // Hide all tabs
        if (gameplayTabContent != null) gameplayTabContent.SetActive(false);
        if (audioTabContent != null) audioTabContent.SetActive(false);
        if (graphicsTabContent != null) graphicsTabContent.SetActive(false);
        if (controlsTabContent != null) controlsTabContent.SetActive(false);
        
        // Reset all tab button colors
        ResetTabButtonColors();
        
        // Show the active tab
        if (activeTab != null) activeTab.SetActive(true);
        
        // Set the active button's color
        if (activeButton != null)
        {
            ColorBlock colors = activeButton.colors;
            colors.normalColor = selectedTabColor;
            activeButton.colors = colors;
        }
    }
    
    /// <summary>
    /// Resets all tab buttons to their unselected color state
    /// </summary>
    private void ResetTabButtonColors()
    {
        SetButtonColor(gameplayTabButton, unselectedTabColor);
        SetButtonColor(audioTabButton, unselectedTabColor);
        SetButtonColor(graphicsTabButton, unselectedTabColor);
        SetButtonColor(controlsTabButton, unselectedTabColor);
    }
    
    /// <summary>
    /// Sets the color of a button's ColorBlock
    /// </summary>
    /// <param name="button">The button to modify</param>
    /// <param name="color">The color to apply</param>
    private void SetButtonColor(Button button, Color color)
    {
        if (button != null)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = color;
            button.colors = colors;
        }
    }
    
    /// <summary>
    /// Initializes all UI elements with current settings values
    /// </summary>
    public void InitializeUI()
    {
        // Initialize various UI elements...
        
        // Make sure FOV slider has the correct min/max values
        if (fovSlider != null)
        {
            fovSlider.minValue = 50f;
            fovSlider.maxValue = 120f;
            fovSlider.value = PlayerPrefs.GetFloat("FOV", 60f);
        }
        
        // Initialize settings by category
        InitializeAudioSettings();
        InitializeGraphicsSettings();
        InitializeGameplaySettings();
        InitializeControlsSettings();
    }
    
    /// <summary>
    /// Initializes audio-related UI elements with current saved values
    /// </summary>
    private void InitializeAudioSettings()
    {
        // Setup audio sliders
        if (audioMixer != null)
        {
            // Master volume
            if (masterVolumeSlider != null)
            {
                float masterVolume;
                if (audioMixer.GetFloat("MasterVolume", out masterVolume))
                {
                    masterVolumeSlider.value = Mathf.Pow(10, masterVolume / 20);
                    if (masterVolumeInput != null)
                        masterVolumeInput.text = masterVolumeSlider.value.ToString("F1", CultureInfo.InvariantCulture);
                }
                else
                {
                    masterVolumeSlider.value = 1.0f;
                    if (masterVolumeInput != null)
                        masterVolumeInput.text = "1.0";
                }
            }
            
            // Music volume
            if (musicVolumeSlider != null)
            {
                float musicVolume;
                if (audioMixer.GetFloat("MusicVolume", out musicVolume))
                {
                    musicVolumeSlider.value = Mathf.Pow(10, musicVolume / 20);
                    if (musicVolumeInput != null)
                        musicVolumeInput.text = musicVolumeSlider.value.ToString("F1", CultureInfo.InvariantCulture);
                }
                else
                {
                    musicVolumeSlider.value = 1.0f;
                    if (musicVolumeInput != null)
                        musicVolumeInput.text = "1.0";
                }
            }
            
            // SFX volume
            if (sfxVolumeSlider != null)
            {
                float sfxVolume;
                if (audioMixer.GetFloat("SFXVolume", out sfxVolume))
                {
                    sfxVolumeSlider.value = Mathf.Pow(10, sfxVolume / 20);
                    if (sfxVolumeInput != null)
                        sfxVolumeInput.text = sfxVolumeSlider.value.ToString("F1", CultureInfo.InvariantCulture);
                }
                else
                {
                    sfxVolumeSlider.value = 1.0f;
                    if (sfxVolumeInput != null)
                        sfxVolumeInput.text = "1.0";
                }
            }
        }
    }
    
    /// <summary>
    /// Initializes graphics-related UI elements with current saved values
    /// </summary>
    private void InitializeGraphicsSettings()
    {
        // Setup resolution dropdown
        if (resolutionDropdown != null)
        {
            resolutions = Screen.resolutions;
            resolutionDropdown.ClearOptions();
            
            int currentResolutionIndex = 0;
            List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
            
            // Find the saved resolution or use current as default
            int savedWidth = PlayerPrefs.GetInt("ScreenWidth", Screen.currentResolution.width);
            int savedHeight = PlayerPrefs.GetInt("ScreenHeight", Screen.currentResolution.height);
            
            for (int i = 0; i < resolutions.Length; i++)
            {
                // Use refreshRateRatio instead of refreshRate
                string option = resolutions[i].width + " x " + resolutions[i].height + " @ " + 
                    resolutions[i].refreshRateRatio.value.ToString("F1") + "Hz";
                options.Add(new TMP_Dropdown.OptionData(option));
                
                // Check if this is the currently saved resolution
                if (resolutions[i].width == savedWidth && resolutions[i].height == savedHeight)
                {
                    currentResolutionIndex = i;
                }
            }
            
            resolutionDropdown.AddOptions(options);
            resolutionDropdown.value = currentResolutionIndex;
            resolutionDropdown.RefreshShownValue();
        }
        
        // Setup quality dropdown
        if (qualityDropdown != null)
        {
            qualityDropdown.ClearOptions();
            List<TMP_Dropdown.OptionData> qualityOptions = new List<TMP_Dropdown.OptionData>();
            
            foreach (string qualityName in QualitySettings.names)
            {
                qualityOptions.Add(new TMP_Dropdown.OptionData(qualityName));
            }
            
            qualityDropdown.AddOptions(qualityOptions);
            qualityDropdown.value = QualitySettings.GetQualityLevel();
            qualityDropdown.RefreshShownValue();
        }
        
        // Setup fullscreen dropdown
        if (fullscreenDropdown != null)
        {
            fullscreenDropdown.ClearOptions();
            List<TMP_Dropdown.OptionData> windowModeOptions = new List<TMP_Dropdown.OptionData>
            {
                new TMP_Dropdown.OptionData("Fullscreen"),
                new TMP_Dropdown.OptionData("Borderless Windowed"),
                new TMP_Dropdown.OptionData("Windowed")
            };
            
            fullscreenDropdown.AddOptions(windowModeOptions);
            
            // Set the dropdown to the saved or current value
            int windowMode = PlayerPrefs.GetInt("WindowMode", Screen.fullScreen ? 0 : 2);
            fullscreenDropdown.value = windowMode;
            fullscreenDropdown.RefreshShownValue();
        }
    }
    
    /// <summary>
    /// Initializes gameplay-related UI elements with current saved values
    /// </summary>
    private void InitializeGameplaySettings()
    {
        // Get fresh values from PlayerPrefs since we may have just updated them
        float currentMouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 1.0f);
        float currentAimSensitivity = PlayerPrefs.GetFloat("AimSensitivity", 0.7f);
        float currentFOV = PlayerPrefs.GetFloat("FOV", 60f);

        if (mouseSensitivitySlider != null)
        {
            mouseSensitivitySlider.value = currentMouseSensitivity;
            if (mouseSensitivityInput != null)
                mouseSensitivityInput.text = currentMouseSensitivity.ToString("F1", CultureInfo.InvariantCulture);
        }
            
        if (aimSensitivitySlider != null)
        {
            aimSensitivitySlider.value = currentAimSensitivity;
            if (aimSensitivityInput != null)
                aimSensitivityInput.text = currentAimSensitivity.ToString("F1", CultureInfo.InvariantCulture);
        }
            
        if (fovSlider != null)
        {
            fovSlider.value = currentFOV;
            if (fovInput != null)
                fovInput.text = Mathf.RoundToInt(currentFOV).ToString();
        }
        
        if (invertYToggle != null)
            invertYToggle.isOn = PlayerPrefs.GetInt("InvertY", 0) == 1;
            
        if (showFPSToggle != null)
            showFPSToggle.isOn = PlayerPrefs.GetInt("ShowFPS", 0) == 1;
            
        if (showHUDToggle != null)
            showHUDToggle.isOn = PlayerPrefs.GetInt("ShowHUD", 1) == 1;
    }
    
    /// <summary>
    /// Initializes controls-related UI elements with current saved values
    /// </summary>
    private void InitializeControlsSettings()
    {
        if (toggleAimToggle != null)
        {
            toggleAimToggle.isOn = PlayerPrefs.GetInt("ToggleAim", 0) == 1;
            
            // Connect the toggle to our SetToggleAim method
            toggleAimToggle.onValueChanged.RemoveAllListeners();
            toggleAimToggle.onValueChanged.AddListener(SetToggleAim);
        }
    }
    
    /// <summary>
    /// Refreshes UI with current values when settings panel is opened
    /// </summary>
    public void OnSettingsOpened()
    {
        // First, check for any active MouseLook components to get real-time values
        UpdateSensitivityFromActiveMouseLook();

        // Then refresh UI with current values
        InitializeUI();
        
        // Always show gameplay tab first
        ShowGameplayTab();
    }

    /// <summary>
    /// Updates sensitivity slider with the value from the active MouseLook component
    /// Ensures UI matches actual in-game settings
    /// </summary>
    private void UpdateSensitivityFromActiveMouseLook()
    {
        // Find any active MouseLook component in the scene
        MouseLook[] mouseLooks = FindObjectsByType<MouseLook>(FindObjectsSortMode.None);
        
        if (mouseLooks != null && mouseLooks.Length > 0)
        {
            // Use the first active MouseLook component's sensitivity
            float currentSensitivity = mouseLooks[0].GetCurrentSensitivity();
            
            // Update PlayerPrefs with this value
            PlayerPrefs.SetFloat("MouseSensitivity", currentSensitivity);
            
            // Update the UI slider (will be applied in InitializeUI)
            if (mouseSensitivitySlider != null)
            {
                mouseSensitivitySlider.value = currentSensitivity;
            }
        }
    }
    
    /// <summary>
    /// Saves all settings to PlayerPrefs
    /// </summary>
    public void SaveSettings()
    {
        // Save audio settings
        SaveAudioSettings();
        
        // Save graphics settings
        SaveGraphicsSettings();
        
        // Save gameplay settings
        SaveGameplaySettings();
        
        // Save controls settings
        SaveControlsSettings();
        
        // Save all PlayerPrefs
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// Saves audio settings to PlayerPrefs and applies them to the audio mixer
    /// </summary>
    private void SaveAudioSettings()
    {
        if (audioMixer != null)
        {
            if (masterVolumeSlider != null)
            {
                float masterVolume = masterVolumeSlider.value > 0.001f ? 
                    Mathf.Log10(masterVolumeSlider.value) * 20 : -80f;
                audioMixer.SetFloat("MasterVolume", masterVolume);
                PlayerPrefs.SetFloat("MasterVolume", masterVolumeSlider.value);
            }
            
            if (musicVolumeSlider != null)
            {
                float musicVolume = musicVolumeSlider.value > 0.001f ? 
                    Mathf.Log10(musicVolumeSlider.value) * 20 : -80f;
                audioMixer.SetFloat("MusicVolume", musicVolume);
                PlayerPrefs.SetFloat("MusicVolume", musicVolumeSlider.value);
            }
            
            if (sfxVolumeSlider != null)
            {
                float sfxVolume = sfxVolumeSlider.value > 0.001f ? 
                    Mathf.Log10(sfxVolumeSlider.value) * 20 : -80f;
                audioMixer.SetFloat("SFXVolume", sfxVolume);
                PlayerPrefs.SetFloat("SFXVolume", sfxVolumeSlider.value);
            }
        }
    }
    
    /// <summary>
    /// Saves graphics settings to PlayerPrefs and applies them to the game
    /// </summary>
    private void SaveGraphicsSettings()
    {
        // Save resolution using our new method
        if (resolutionDropdown != null && resolutions != null)
        {
            SetResolution(resolutionDropdown.value);
        }
        
        // Save quality
        if (qualityDropdown != null)
        {
            QualitySettings.SetQualityLevel(qualityDropdown.value);
            PlayerPrefs.SetInt("QualityLevel", qualityDropdown.value);
        }
        
        // Save window mode
        if (fullscreenDropdown != null)
        {
            SetWindowMode(fullscreenDropdown.value);
        }
    }
    
    /// <summary>
    /// Saves gameplay settings to PlayerPrefs
    /// </summary>
    private void SaveGameplaySettings()
    {
        if (mouseSensitivitySlider != null)
            PlayerPrefs.SetFloat("MouseSensitivity", mouseSensitivitySlider.value);
            
        if (aimSensitivitySlider != null)
            PlayerPrefs.SetFloat("AimSensitivity", aimSensitivitySlider.value);
            
        if (fovSlider != null)
            PlayerPrefs.SetFloat("FOV", fovSlider.value);
            
        if (invertYToggle != null)
            PlayerPrefs.SetInt("InvertY", invertYToggle.isOn ? 1 : 0);
            
        if (showFPSToggle != null)
            PlayerPrefs.SetInt("ShowFPS", showFPSToggle.isOn ? 1 : 0);
            
        if (showHUDToggle != null)
            PlayerPrefs.SetInt("ShowHUD", showHUDToggle.isOn ? 1 : 0);
    }
    
    /// <summary>
    /// Saves controls settings to PlayerPrefs
    /// </summary>
    private void SaveControlsSettings()
    {
        if (toggleAimToggle != null)
            PlayerPrefs.SetInt("ToggleAim", toggleAimToggle.isOn ? 1 : 0);
    }
    
    /// <summary>
    /// Loads all settings from PlayerPrefs and applies them
    /// </summary>
    public void LoadSettings()
    {
        // Load quality
        QualitySettings.SetQualityLevel(PlayerPrefs.GetInt("QualityLevel", QualitySettings.GetQualityLevel()));
        
        // Load resolution
        int width = PlayerPrefs.GetInt("ScreenWidth", Screen.currentResolution.width);
        int height = PlayerPrefs.GetInt("ScreenHeight", Screen.currentResolution.height);
        
        // Load window mode first so we know what fullscreen mode to use
        int windowMode = PlayerPrefs.GetInt("WindowMode", 0); // Default to fullscreen
        FullScreenMode fullScreenMode;
        
        switch (windowMode)
        {
            case 0:
                fullScreenMode = FullScreenMode.ExclusiveFullScreen;
                break;
            case 1:
                fullScreenMode = FullScreenMode.FullScreenWindow;
                break;
            default:
                fullScreenMode = FullScreenMode.Windowed;
                break;
        }
        
        // Apply resolution with the correct screen mode
        Screen.SetResolution(width, height, fullScreenMode);
        
        // Load audio settings
        if (audioMixer != null)
        {
            float masterValue = PlayerPrefs.GetFloat("MasterVolume", 1.0f);
            float masterVolume = masterValue > 0.001f ? Mathf.Log10(masterValue) * 20 : -80f;
            audioMixer.SetFloat("MasterVolume", masterVolume);
            
            float musicValue = PlayerPrefs.GetFloat("MusicVolume", 1.0f);
            float musicVolume = musicValue > 0.001f ? Mathf.Log10(musicValue) * 20 : -80f;
            audioMixer.SetFloat("MusicVolume", musicVolume);
            
            float sfxValue = PlayerPrefs.GetFloat("SFXVolume", 1.0f);
            float sfxVolume = sfxValue > 0.001f ? Mathf.Log10(sfxValue) * 20 : -80f;
            audioMixer.SetFloat("SFXVolume", sfxVolume);
        }
    }
    
    #region UI Callbacks
    
    /// <summary>
    /// Sets the master volume level and applies it to the audio mixer
    /// </summary>
    /// <param name="volume">Volume level from 0 to 1</param>
    public void SetMasterVolume(float volume)
    {
        if (audioMixer != null)
        {
            // Convert slider value (0-1) to logarithmic scale (-80dB to 0dB)
            float dbVolume = volume > 0.001f ? Mathf.Log10(volume) * 20 : -80f;
            audioMixer.SetFloat("MasterVolume", dbVolume);
            PlayerPrefs.SetFloat("MasterVolume", volume);
        }
    }
    
    /// <summary>
    /// Sets the music volume level and applies it to the audio mixer
    /// </summary>
    /// <param name="volume">Volume level from 0 to 1</param>
    public void SetMusicVolume(float volume)
    {
        if (audioMixer != null)
        {
            // Convert slider value (0-1) to logarithmic scale (-80dB to 0dB)
            float dbVolume = volume > 0.001f ? Mathf.Log10(volume) * 20 : -80f;
            audioMixer.SetFloat("MusicVolume", dbVolume);
            PlayerPrefs.SetFloat("MusicVolume", volume);
        }
    }
    
    /// <summary>
    /// Sets the sound effects volume level and applies it to the audio mixer
    /// </summary>
    /// <param name="volume">Volume level from 0 to 1</param>
    public void SetSFXVolume(float volume)
    {
        if (audioMixer != null)
        {
            // Convert slider value (0-1) to logarithmic scale (-80dB to 0dB)
            float dbVolume = volume > 0.001f ? Mathf.Log10(volume) * 20 : -80f;
            audioMixer.SetFloat("SFXVolume", dbVolume);
            PlayerPrefs.SetFloat("SFXVolume", volume);
        }
    }
    
    /// <summary>
    /// Sets the quality level preset
    /// </summary>
    /// <param name="qualityIndex">Index of the quality preset to use</param>
    public void SetQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
        PlayerPrefs.SetInt("QualityLevel", qualityIndex);
    }
    
    /// <summary>
    /// Sets the window mode (fullscreen, borderless, or windowed)
    /// </summary>
    /// <param name="windowModeIndex">Window mode index: 0=Fullscreen, 1=Borderless, 2=Windowed</param>
    public void SetWindowMode(int windowModeIndex)
    {
        // 0 = Fullscreen, 1 = Borderless Windowed, 2 = Windowed
        switch (windowModeIndex)
        {
            case 0: // Fullscreen
                Screen.fullScreen = true;
                Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
                break;
            case 1: // Borderless Windowed
                Screen.fullScreen = true;
                Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
                break;
            case 2: // Windowed
                Screen.fullScreen = false;
                Screen.fullScreenMode = FullScreenMode.Windowed;
                break;
        }
        
        // Save the setting
        PlayerPrefs.SetInt("WindowMode", windowModeIndex);
    }
    
    /// <summary>
    /// Sets the mouse sensitivity and updates all MouseLook components
    /// </summary>
    /// <param name="sensitivity">The new sensitivity value</param>
    public void SetMouseSensitivity(float sensitivity)
    {
        // Store the value in PlayerPrefs
        PlayerPrefs.SetFloat("MouseSensitivity", sensitivity);
        
        // Apply to all active MouseLook components in the scene immediately
        MouseLook[] mouseLooks = FindObjectsByType<MouseLook>(FindObjectsSortMode.None);
        foreach (MouseLook mouseLook in mouseLooks)
        {
            if (mouseLook != null)
            {
                mouseLook.SetSensitivity(sensitivity);
            }
        }
    }
    
    /// <summary>
    /// Sets the aim-down-sights sensitivity multiplier and updates all AimDownSights components
    /// </summary>
    /// <param name="sensitivity">The new ADS sensitivity multiplier</param>
    public void SetAimSensitivity(float sensitivity)
    {
        PlayerPrefs.SetFloat("AimSensitivity", sensitivity);
        
        // Update active AimDownSights components
        AimDownSights[] adsSystems = FindObjectsByType<AimDownSights>(FindObjectsSortMode.None);
        foreach (AimDownSights ads in adsSystems)
        {
            ads.SetADSSensitivityMultiplier(sensitivity);
        }
    }
    
    /// <summary>
    /// Sets the field of view angle and updates all MouseLook components
    /// </summary>
    /// <param name="fov">The new FOV value in degrees</param>
    public void SetFOV(float fov)
    {
        // Round to integer before clamping and saving
        int roundedFov = Mathf.RoundToInt(fov);
        roundedFov = Mathf.Clamp(roundedFov, 50, 120);
        
        PlayerPrefs.SetFloat("FOV", roundedFov);
        
        // Update any active MouseLook components
        MouseLook[] mouseLooks = FindObjectsByType<MouseLook>(FindObjectsSortMode.None);
        foreach (MouseLook mouseLook in mouseLooks)
        {
            mouseLook.SetFOV(roundedFov);
        }
    }
    
    /// <summary>
    /// Sets whether vertical mouse input should be inverted
    /// </summary>
    /// <param name="invert">True to invert Y axis, false for normal controls</param>
    public void SetInvertY(bool invert)
    {
        PlayerPrefs.SetInt("InvertY", invert ? 1 : 0);
        
        // Update any active MouseLook components
        MouseLook[] mouseLooks = FindObjectsByType<MouseLook>(FindObjectsSortMode.None);
        foreach (MouseLook mouseLook in mouseLooks)
        {
            mouseLook.SetInvertY(invert);
        }
    }
    
    /// <summary>
    /// Sets whether the FPS counter should be displayed
    /// </summary>
    /// <param name="showFPS">True to show FPS counter, false to hide it</param>
    public void SetShowFPS(bool showFPS)
    {
        PlayerPrefs.SetInt("ShowFPS", showFPS ? 1 : 0);
        
        // Toggle FPS display using the direct reference
        if (fpsCounter != null)
        {
            fpsCounter.SetVisible(showFPS);
        }
        else
        {
            Debug.LogWarning("FPS Counter reference is missing. Please assign it in the GameSettings inspector.");
        }
    }
    
    /// <summary>
    /// Sets whether HUD elements should be displayed
    /// </summary>
    /// <param name="showHUD">True to show HUD, false to hide it</param>
    public void SetShowHUD(bool showHUD)
    {
        PlayerPrefs.SetInt("ShowHUD", showHUD ? 1 : 0);
        
        // Update HUD visibility immediately
        HUDManager.UpdateHUDVisibility(showHUD);
    }
    
    /// <summary>
    /// Connects a slider with an input field for synchronized value editing
    /// </summary>
    /// <param name="inputField">The input field to synchronize</param>
    /// <param name="slider">The slider to synchronize</param>
    /// <param name="minValue">Minimum allowed value</param>
    /// <param name="maxValue">Maximum allowed value</param>
    private void SetupInputField(TMP_InputField inputField, Slider slider, float minValue, float maxValue)
    {
        if (inputField != null && slider != null)
        {
            // Update input field when slider changes
            slider.onValueChanged.AddListener((value) => {
                if (inputField != null)
                {
                    // Format to one decimal place
                    inputField.text = value.ToString("F1", CultureInfo.InvariantCulture);
                }
            });
            
            // Update slider when input field changes
            inputField.onEndEdit.AddListener((text) => {
                if (slider != null)
                {
                    // Try to parse the input value
                    if (float.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out float value))
                    {
                        // Clamp the value to the slider's range
                        value = Mathf.Clamp(value, minValue, maxValue);
                        
                        // Round to one decimal place
                        value = Mathf.Round(value * 10f) / 10f;
                        
                        // Update the slider (this will trigger the onValueChanged event above)
                        slider.value = value;
                        
                        // Update the input field with formatted value
                        inputField.text = value.ToString("F1", CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        // Invalid input, reset to current slider value
                        inputField.text = slider.value.ToString("F1", CultureInfo.InvariantCulture);
                    }
                }
            });
            
            // Initialize input field with current slider value
            inputField.text = slider.value.ToString("F1", CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// Sets up all input field listeners for settings UI
    /// </summary>
    private void SetupInputFieldListeners()
    {
        // Setup gameplay input fields
        SetupInputField(mouseSensitivityInput, mouseSensitivitySlider, 0.1f, 10.0f);
        SetupInputField(aimSensitivityInput, aimSensitivitySlider, 0.1f, 10.0f);
        
        // Use the new integer input field setup for FOV
        SetupIntegerInputField(fovInput, fovSlider, 50, 120);
        
        // Setup audio input fields with immediate effect
        SetupAudioInputField(masterVolumeInput, masterVolumeSlider, 0f, 1f, "MasterVolume", "MasterVolume");
        SetupAudioInputField(musicVolumeInput, musicVolumeSlider, 0f, 1f, "MusicVolume", "MusicVolume");
        SetupAudioInputField(sfxVolumeInput, sfxVolumeSlider, 0f, 1f, "SFXVolume", "SFXVolume");
        
        // Add onValueChanged listeners to audio sliders for immediate effect
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
        
        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
        
        // Add listener for mouse sensitivity and FOV to apply immediately
        if (mouseSensitivitySlider != null)
            mouseSensitivitySlider.onValueChanged.AddListener(SetMouseSensitivity);
        
        if (fovSlider != null)
            fovSlider.onValueChanged.AddListener(SetFOV);
        
        if (aimSensitivitySlider != null)
            aimSensitivitySlider.onValueChanged.AddListener(SetAimSensitivity);
    }
    
    /// <summary>
    /// Connects an audio mixer parameter to a slider and input field with real-time updates
    /// </summary>
    /// <param name="inputField">The input field to synchronize</param>
    /// <param name="slider">The slider to synchronize</param>
    /// <param name="minValue">Minimum allowed value</param>
    /// <param name="maxValue">Maximum allowed value</param>
    /// <param name="mixerParam">The name of the audio mixer parameter</param>
    /// <param name="prefsKey">The PlayerPrefs key to save the value</param>
    private void SetupAudioInputField(TMP_InputField inputField, Slider slider, float minValue, float maxValue, string mixerParam, string prefsKey)
    {
        if (inputField != null && slider != null)
        {
            // Update input field when slider changes
            slider.onValueChanged.AddListener((value) => {
                if (inputField != null)
                {
                    // Format to one decimal place
                    inputField.text = value.ToString("F1", CultureInfo.InvariantCulture);
                }
                
                // Apply volume change immediately
                if (audioMixer != null)
                {
                    // Convert slider value (0-1) to logarithmic scale (-80dB to 0dB)
                    float dbVolume = value > 0.001f ? Mathf.Log10(value) * 20 : -80f;
                    audioMixer.SetFloat(mixerParam, dbVolume);
                    PlayerPrefs.SetFloat(prefsKey, value);
                }
            });
            
            // Update slider when input field changes
            inputField.onEndEdit.AddListener((text) => {
                if (slider != null)
                {
                    // Try to parse the input value
                    if (float.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out float value))
                    {
                        // Clamp the value to the slider's range
                        value = Mathf.Clamp(value, minValue, maxValue);
                        
                        // Round to one decimal place
                        value = Mathf.Round(value * 10f) / 10f;
                        
                        // Update the slider (this will trigger the onValueChanged event above)
                        slider.value = value;
                        
                        // Update the input field with formatted value
                        inputField.text = value.ToString("F1", CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        // Invalid input, reset to current slider value
                        inputField.text = slider.value.ToString("F1", CultureInfo.InvariantCulture);
                    }
                }
            });
            
            // Initialize input field with current slider value
            inputField.text = slider.value.ToString("F1", CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// Sets the screen resolution based on the selected resolution index
    /// </summary>
    /// <param name="resolutionIndex">Index into the resolutions array</param>
    public void SetResolution(int resolutionIndex)
    {
        if (resolutions != null && resolutionIndex >= 0 && resolutionIndex < resolutions.Length)
        {
            Resolution resolution = resolutions[resolutionIndex];
            
            // Store the current fullscreen mode to maintain it
            FullScreenMode currentMode = Screen.fullScreenMode;
            
            // Apply the resolution while maintaining the current fullscreen mode
            Screen.SetResolution(resolution.width, resolution.height, currentMode);
            
            // Save to player prefs
            PlayerPrefs.SetInt("ScreenWidth", resolution.width);
            PlayerPrefs.SetInt("ScreenHeight", resolution.height);
            PlayerPrefs.SetInt("ResolutionIndex", resolutionIndex);
        }
    }

    /// <summary>
    /// Connects an integer input field with a slider for synchronized value editing
    /// </summary>
    /// <param name="inputField">The input field to synchronize</param>
    /// <param name="slider">The slider to synchronize</param>
    /// <param name="minValue">Minimum allowed integer value</param>
    /// <param name="maxValue">Maximum allowed integer value</param>
    private void SetupIntegerInputField(TMP_InputField inputField, Slider slider, int minValue, int maxValue)
    {
        if (inputField != null && slider != null)
        {
            // Update input field when slider changes
            slider.onValueChanged.AddListener((value) => {
                if (inputField != null)
                {
                    // Format to integer (whole number)
                    inputField.text = Mathf.RoundToInt(value).ToString();
                }
            });
            
            // Update slider when input field changes
            inputField.onEndEdit.AddListener((text) => {
                if (slider != null)
                {
                    // Try to parse the input value as integer
                    if (int.TryParse(text, out int value))
                    {
                        // Clamp the value to the slider's range
                        value = Mathf.Clamp(value, minValue, maxValue);
                        
                        // Update the slider
                        slider.value = value;
                        
                        // Update the input field with formatted value
                        inputField.text = value.ToString();
                    }
                    else
                    {
                        // Invalid input, reset to current slider value
                        inputField.text = Mathf.RoundToInt(slider.value).ToString();
                    }
                }
            });
            
            // Initialize input field with current slider value as integer
            inputField.text = Mathf.RoundToInt(slider.value).ToString();
        }
    }

    /// <summary>
    /// Sets whether aiming down sights uses toggle mode or hold mode
    /// </summary>
    /// <param name="toggleAim">True for toggle mode, false for hold mode</param>
    public void SetToggleAim(bool toggleAim)
    {
        PlayerPrefs.SetInt("ToggleAim", toggleAim ? 1 : 0);
        
        // Update all AimDownSights components in the scene
        AimDownSights[] aimScripts = FindObjectsByType<AimDownSights>(FindObjectsSortMode.None);
        foreach (AimDownSights aim in aimScripts)
        {
            aim.SetToggleMode(toggleAim);
        }
    }
    
    /// <summary>
    /// Shows the confirmation dialog asking if user wants to save settings
    /// </summary>
    private void ShowSaveConfirmationDialog()
    {
        // If no dialog UI is assigned, just close with saving
        if (confirmSaveDialog == null)
        {
            SaveAndClose();
            return;
        }

        // Show the dialog
        confirmSaveDialog.SetActive(true);
        
        // Setup button listeners (removing any existing ones first)
        if (saveYesButton != null)
        {
            saveYesButton.onClick.RemoveAllListeners();
            saveYesButton.onClick.AddListener(SaveAndClose);
        }
        
        if (saveNoButton != null)
        {
            saveNoButton.onClick.RemoveAllListeners();
            saveNoButton.onClick.AddListener(CloseWithoutSaving);
        }
        
        // Focus the Yes button by default
        if (saveYesButton != null)
        {
            saveYesButton.Select();
        }
    }

    /// <summary>
    /// Saves settings and closes the settings menu
    /// </summary>
    private void SaveAndClose()
    {
        // Hide the confirmation dialog if it was showing
        if (confirmSaveDialog != null)
        {
            confirmSaveDialog.SetActive(false);
        }
        
        // Save settings and close menu
        SaveSettings();
        CloseSettingsMenuInternal(true);
    }

    /// <summary>
    /// Closes the settings menu without saving
    /// </summary>
    private void CloseWithoutSaving()
    {
        // Hide the confirmation dialog if it was showing
        if (confirmSaveDialog != null)
        {
            confirmSaveDialog.SetActive(false);
        }
        
        // Just close the menu without saving
        CloseSettingsMenuInternal(false);
    }

    /// <summary>
    /// Internal method to handle closing the settings menu with or without saving
    /// </summary>
    /// <param name="wasSaved">Whether settings were saved</param>
    private void CloseSettingsMenuInternal(bool wasSaved)
    {
        if (settingsMenuUI != null)
        {
            // Hide the menu
            settingsMenuUI.SetActive(false);
            isVisible = false;
            
            // Check if we're in the main menu scene
            bool isMainMenu = SceneManager.GetActiveScene().name == "MainMenu";
            
            // Only pause the game if we're NOT in the main menu
            if (!isMainMenu)
            {
                // This is needed because some event might be resuming the game
                Time.timeScale = 0f;
                PauseManager.SetPaused(true);
            }
            else
            {
                // Ensure time is running in the main menu
                Time.timeScale = 1f;
                if (PauseManager.IsPaused())
                {
                    PauseManager.SetPaused(false);
                }
            }
            
            // Trigger the close event to notify PauseMenu
            if (OnSettingsMenuClosed != null)
                OnSettingsMenuClosed.Invoke();
            
            // If settings weren't saved, reload the previously saved settings to revert changes
            if (!wasSaved)
            {
                // Reload the saved settings - this will undo any unsaved changes
                LoadSettings();
                
                // Initialize UI with reloaded values - not necessary in this case since the menu is closing
                //InitializeUI();
            }
        }
    }
    
    #endregion
}