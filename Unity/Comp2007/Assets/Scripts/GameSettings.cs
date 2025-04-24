using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;
using System.Collections;
using System.Collections.Generic;  // Add this for List<T>
using System.Globalization;
using UnityEngine.SceneManagement; // Add this for SceneManager

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
    [SerializeField] private TMP_Dropdown fullscreenDropdown;  // Changed from Toggle to TMP_Dropdown
    [SerializeField] private TMP_Dropdown qualityDropdown;  // Changed from Dropdown to TMP_Dropdown
    [SerializeField] private TMP_Dropdown resolutionDropdown;  // Changed from Dropdown to TMP_Dropdown
    
    [Header("Gameplay Settings")]
    [SerializeField] private Slider mouseSensitivitySlider;      // Look sensitivity
    [SerializeField] private TMP_InputField mouseSensitivityInput;
    [SerializeField] private Slider aimSensitivitySlider;        // ADS sensitivity
    [SerializeField] private TMP_InputField aimSensitivityInput;
    [SerializeField] private Slider fovSlider;                   // Field of View slider
    [SerializeField] private TMP_InputField fovInput;
    [SerializeField] private Toggle invertYToggle;               // Invert Y axis
    [SerializeField] private Toggle showFPSToggle;               // Show FPS counter
    [SerializeField] private FPSCounter fpsCounter;              // Reference to the FPS counter object
    [SerializeField] private Toggle showHUDToggle;               // Show HUD elements
    
    [Header("Controls Settings")]
    [SerializeField] private Toggle toggleAimToggle;
    
    [Header("Settings Storage")]
    [SerializeField] private bool loadOnAwake = true;
    [SerializeField] private bool hideOnAwake = true;            // Whether to hide the menu when the game starts
    
    // Theme colors for selected/unselected tabs
    [SerializeField] private Color selectedTabColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color unselectedTabColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    
    // Events
    public System.Action OnSettingsMenuClosed;                   // Event triggered when settings menu is closed
    
    private Resolution[] resolutions;
    private bool isVisible = false;
    
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
    
    private void Update()
    {
        // Check for Escape key press to close settings
        if (isVisible && Input.GetKeyUp(backKey))
        {
            CloseSettingsMenu();
        }
    }
    
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
    
    public void CloseSettingsMenu()
    {
        if (settingsMenuUI != null)
        {
            // Save settings before closing
            SaveSettings();
            
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
        }
    }
    
    public bool IsVisible()
    {
        return isVisible;
    }
    
    // Toggle visibility of the settings menu
    public void ToggleSettingsMenu()
    {
        if (isVisible)
            CloseSettingsMenu();
        else
            ShowSettingsMenu();
    }
    
    #endregion
    
    public void ShowGameplayTab()
    {
        SetActiveTab(gameplayTabContent, gameplayTabButton);
    }
    
    public void ShowAudioTab()
    {
        SetActiveTab(audioTabContent, audioTabButton);
    }
    
    public void ShowGraphicsTab()
    {
        SetActiveTab(graphicsTabContent, graphicsTabButton);
    }
    
    public void ShowControlsTab()
    {
        SetActiveTab(controlsTabContent, controlsTabButton);
    }
    
    private void SetActiveTab(GameObject activeTab, Button activeButton)
    {
        // Hide all tabs
        if (gameplayTabContent != null) gameplayTabContent.SetActive(false);
        if (audioTabContent != null) audioTabContent.SetActive(false);  // FIX: was using gameplayTabContent
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
    
    private void ResetTabButtonColors()
    {
        SetButtonColor(gameplayTabButton, unselectedTabColor);
        SetButtonColor(audioTabButton, unselectedTabColor);
        SetButtonColor(graphicsTabButton, unselectedTabColor);
        SetButtonColor(controlsTabButton, unselectedTabColor);
    }
    
    private void SetButtonColor(Button button, Color color)
    {
        if (button != null)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = color;
            button.colors = colors;
        }
    }
    
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
        
        // Other initialization code...
        
        // Audio Settings
        InitializeAudioSettings();
        
        // Graphics Settings
        InitializeGraphicsSettings();
        
        // Gameplay Settings
        InitializeGameplaySettings();
        
        // Controls Settings
        InitializeControlsSettings();
    }
    
    private void InitializeAudioSettings()
    {
        // Setup audio sliders
        if (audioMixer != null)
        {
            // Master volume
            if (masterVolumeSlider != null)
            {
                float masterVolume;
                if (audioMixer.GetFloat("MasterVolume", out masterVolume))  // Changed from "Master"
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
                if (audioMixer.GetFloat("MusicVolume", out musicVolume))  // Changed from "Music"
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
                if (audioMixer.GetFloat("SFXVolume", out sfxVolume))  // Changed from "SFX"
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
    
    private void InitializeGameplaySettings()
    {
        if (mouseSensitivitySlider != null)
        {
            mouseSensitivitySlider.value = PlayerPrefs.GetFloat("MouseSensitivity", 1.0f);
            if (mouseSensitivityInput != null)
                mouseSensitivityInput.text = mouseSensitivitySlider.value.ToString("F1", CultureInfo.InvariantCulture);
        }
            
        if (aimSensitivitySlider != null)
        {
            aimSensitivitySlider.value = PlayerPrefs.GetFloat("AimSensitivity", 0.7f);
            if (aimSensitivityInput != null)
                aimSensitivityInput.text = aimSensitivitySlider.value.ToString("F1", CultureInfo.InvariantCulture);
        }
            
        if (fovSlider != null)
        {
            fovSlider.value = PlayerPrefs.GetFloat("FOV", 60f);
            if (fovInput != null)
                fovInput.text = Mathf.RoundToInt(fovSlider.value).ToString(); // Changed to integer
        }
        
        // Rest of your existing code
        if (invertYToggle != null)
            invertYToggle.isOn = PlayerPrefs.GetInt("InvertY", 0) == 1;
            
        if (showFPSToggle != null)
            showFPSToggle.isOn = PlayerPrefs.GetInt("ShowFPS", 0) == 1;
            
        if (showHUDToggle != null)
            showHUDToggle.isOn = PlayerPrefs.GetInt("ShowHUD", 1) == 1;
    }
    
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
    
    // Called when settings panel is opened
    public void OnSettingsOpened()
    {
        // Refresh UI with current values
        InitializeUI();
        
        // Always show gameplay tab first
        ShowGameplayTab();
    }
    
    // Save all settings
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
    
    private void SaveAudioSettings()
    {
        if (audioMixer != null)
        {
            if (masterVolumeSlider != null)
            {
                float masterVolume = masterVolumeSlider.value > 0.001f ? 
                    Mathf.Log10(masterVolumeSlider.value) * 20 : -80f;
                audioMixer.SetFloat("MasterVolume", masterVolume);  // Changed from "Master" to "MasterVolume"
                PlayerPrefs.SetFloat("MasterVolume", masterVolumeSlider.value);
            }
            
            if (musicVolumeSlider != null)
            {
                float musicVolume = musicVolumeSlider.value > 0.001f ? 
                    Mathf.Log10(musicVolumeSlider.value) * 20 : -80f;
                audioMixer.SetFloat("MusicVolume", musicVolume);  // Changed from "Music" to "MusicVolume"
                PlayerPrefs.SetFloat("MusicVolume", musicVolumeSlider.value);
            }
            
            if (sfxVolumeSlider != null)
            {
                float sfxVolume = sfxVolumeSlider.value > 0.001f ? 
                    Mathf.Log10(sfxVolumeSlider.value) * 20 : -80f;
                audioMixer.SetFloat("SFXVolume", sfxVolume);  // Changed from "SFX" to "SFXVolume"
                PlayerPrefs.SetFloat("SFXVolume", sfxVolumeSlider.value);
            }
        }
    }
    
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
    
    private void SaveControlsSettings()
    {
        if (toggleAimToggle != null)
            PlayerPrefs.SetInt("ToggleAim", toggleAimToggle.isOn ? 1 : 0);
    }
    
    // Load all settings
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
        
        // Rest of your existing code for audio settings...
        if (audioMixer != null)
        {
            float masterValue = PlayerPrefs.GetFloat("MasterVolume", 1.0f);
            float masterVolume = masterValue > 0.001f ? Mathf.Log10(masterValue) * 20 : -80f;
            audioMixer.SetFloat("MasterVolume", masterVolume);  // Changed from "Master"
            
            float musicValue = PlayerPrefs.GetFloat("MusicVolume", 1.0f);
            float musicVolume = musicValue > 0.001f ? Mathf.Log10(musicValue) * 20 : -80f;
            audioMixer.SetFloat("MusicVolume", musicVolume);  // Changed from "Music"
            
            float sfxValue = PlayerPrefs.GetFloat("SFXVolume", 1.0f);
            float sfxVolume = sfxValue > 0.001f ? Mathf.Log10(sfxValue) * 20 : -80f;
            audioMixer.SetFloat("SFXVolume", sfxVolume);  // Changed from "SFX"
        }
    }
    
    // UI Callbacks for direct slider/toggle connections
    
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
    
    public void SetQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
        PlayerPrefs.SetInt("QualityLevel", qualityIndex);
    }
    
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
    
    public void SetMouseSensitivity(float sensitivity)
    {
        PlayerPrefs.SetFloat("MouseSensitivity", sensitivity);
        
        // Update any active MouseLook components
        MouseLook[] mouseLooks = FindObjectsByType<MouseLook>(FindObjectsSortMode.None);
        foreach (MouseLook mouseLook in mouseLooks)
        {
            mouseLook.SetSensitivity(sensitivity);
        }
    }
    
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
    
    public void SetShowHUD(bool showHUD)
    {
        PlayerPrefs.SetInt("ShowHUD", showHUD ? 1 : 0);
        
        // Update HUD visibility immediately
        HUDManager.UpdateHUDVisibility(showHUD);
    }
    
    // Add this method to the GameSettings class to handle input fields for float values
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

    // Add this method to your Awake() or Start() to set up input field listeners
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
    }
    
    // Create a new method specifically for audio input fields
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

    // Add a new method for changing resolution:
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

    // Add this new method for integer-only input fields:
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
}