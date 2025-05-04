using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using UnityEngine.UI;

/// <summary>
/// Handles main menu functionality including volume control and scene transitions
/// </summary>
public class MainMenu : MonoBehaviour
{
    // References to Unity components
    [SerializeField] private AudioMixer audioMixer;    // Controls audio mixing
    [SerializeField] private GameSettings gameSettings; // Reference to GameSettings

    // AudioMixer parameter names - make sure these match the exposed parameters in your AudioMixer
    private const string MUSIC_VOLUME_PARAM = "MusicVolume";
    private const string SFX_VOLUME_PARAM = "SFXVolume";

    [Header("Confirmation Dialog")]
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

    /// <summary>
    /// Called when the script instance is being loaded
    /// Initializes volume settings and starts menu music
    /// </summary>
    private void Start()
    {
        LoadVolume();  // Load saved volume settings
        MusicManager.Instance.PlayMusic("Main Menu");  // Start playing menu music

        // Initialize GameSettings reference if not set in inspector
        if (gameSettings == null)
        {
            gameSettings = FindFirstObjectByType<GameSettings>();
        }
        
        // Ensure time is running in the main menu
        Time.timeScale = 1f;
        
        // Make sure cursor is visible and unlocked in menu
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Hide confirmation dialog at startup
        if (confirmQuitDialog != null)
        {
            confirmQuitDialog.SetActive(false);
        }
    }

    /// <summary>
    /// Handles input processing for the menu
    /// Used primarily for handling Escape key to close settings
    /// </summary>
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // If settings are visible, close them
            if (gameSettings != null && gameSettings.IsVisible())
            {
                gameSettings.CloseSettingsMenu();
            }
            else if (Time.timeScale == 0f)
            {
                // If the game is paused but settings aren't visible, unpause
                Time.timeScale = 1f;
                if (PauseManager.IsPaused())
                {
                    PauseManager.SetPaused(false);
                }
            }
        }
    }

    /// <summary>
    /// Loads the main game scene when play button is clicked
    /// Sets up necessary game state flags before scene transition
    /// </summary>
    public void Play()
    {
        Debug.Log("Starting new game...");
        
        // Ensure time is running when starting the game
        Time.timeScale = 1f;
        
        // Reset the death flag when starting a new game
        PlayerPrefs.SetInt("ComingFromDeath", 0);
        
        // Set a flag to ensure the main camera is enabled in the game scene
        PlayerPrefs.SetInt("EnableMainCamera", 1);
        PlayerPrefs.Save();
        
        SceneManager.LoadScene("Game");
    }

    /// <summary>
    /// Quits the application when quit button is clicked
    /// </summary>
    public void Quit()
    {
        Application.Quit();
    }

    /// <summary>
    /// Opens the settings menu using the GameSettings component
    /// </summary>
    public void OpenSettings()
    {
        if (gameSettings != null)
        {
            gameSettings.ShowSettingsMenu();
        }
        else
        {
            Debug.LogError("GameSettings reference not set in MainMenu script");
        }
    }

    /// <summary>
    /// Updates the music volume in the audio mixer
    /// </summary>
    /// <param name="volume">The new volume level for music (typically in decibels)</param>
    public void UpdateMusicVolume(float volume)
    {
        if (audioMixer != null)
        {
            audioMixer.SetFloat(MUSIC_VOLUME_PARAM, volume);
        }
        else
        {
            Debug.LogError("AudioMixer reference not set in MainMenu script");
        }
    }

    /// <summary>
    /// Updates the sound effects volume in the audio mixer
    /// </summary>
    /// <param name="volume">The new volume level for sound effects (typically in decibels)</param>
    public void UpdateSoundVolume(float volume)
    {
        if (audioMixer != null)
        {
            audioMixer.SetFloat(SFX_VOLUME_PARAM, volume);
        }
        else
        {
            Debug.LogError("AudioMixer reference not set in MainMenu script");
        }
    }

    /// <summary>
    /// Saves current volume settings to PlayerPrefs for persistence between sessions
    /// </summary>
    public void SaveVolume()
    {
        if (audioMixer == null)
        {
            Debug.LogError("AudioMixer reference not set in MainMenu script");
            return;
        }

        // Get and save music volume
        audioMixer.GetFloat(MUSIC_VOLUME_PARAM, out float musicVolume);
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);

        // Get and save sound effects volume
        audioMixer.GetFloat(SFX_VOLUME_PARAM, out float sfxVolume);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
    }

    /// <summary>
    /// Loads saved volume settings from PlayerPrefs or uses defaults if not found
    /// </summary>
    public void LoadVolume()
    {
        if (audioMixer == null)
        {
            Debug.LogError("AudioMixer reference not set in MainMenu script");
            return;
        }

        // Default volume settings
        const float DEFAULT_MUSIC_VOLUME = -10f;
        const float DEFAULT_SFX_VOLUME = 0f;

        // Load music volume (default: -10f)
        float musicVolume = PlayerPrefs.HasKey("MusicVolume") 
            ? PlayerPrefs.GetFloat("MusicVolume") 
            : DEFAULT_MUSIC_VOLUME;

        // Load sound effects volume (default: 0f)
        float sfxVolume = PlayerPrefs.HasKey("SFXVolume")
            ? PlayerPrefs.GetFloat("SFXVolume")
            : DEFAULT_SFX_VOLUME;

        // Apply loaded volumes to the audio mixer
        UpdateMusicVolume(musicVolume);
        UpdateSoundVolume(sfxVolume);
    }

    /// <summary>
    /// Shows quit confirmation dialog when quit button is clicked
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
    /// Quits the application after confirmation
    /// </summary>
    private void QuitGame()
    {
        Debug.Log("Quitting game...");
        
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
    /// Closes the quit confirmation dialog without taking action
    /// </summary>
    private void CloseQuitConfirmation()
    {
        if (confirmQuitDialog != null)
        {
            confirmQuitDialog.SetActive(false);
        }
        
        // Restore focus to the main menu
        // If you have a specific button to select, do it here
    }
}