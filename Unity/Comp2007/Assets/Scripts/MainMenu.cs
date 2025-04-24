using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using UnityEngine.UI;

/// Handles main menu functionality including volume control and scene transitions
public class MainMenu : MonoBehaviour
{
    // References to Unity components
    [SerializeField] private AudioMixer audioMixer;    // Controls audio mixing
    [SerializeField] private GameSettings gameSettings; // Reference to GameSettings

    // AudioMixer parameter names - make sure these match the exposed parameters in your AudioMixer
    private const string MUSIC_VOLUME_PARAM = "MusicVolume";
    private const string SFX_VOLUME_PARAM = "SFXVolume";

    /// Called when the script instance is being loaded
    /// Initializes volume settings and starts menu music
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
    }

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

    /// Loads the main game scene when play button is clicked
    public void Play()
    {
        // Ensure time is running when starting the game
        Time.timeScale = 1f;
        SceneManager.LoadScene("Game");
    }

    /// Quits the application when quit button is clicked
    public void Quit()
    {
        Application.Quit();
    }

    /// Opens the settings menu
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

    /// Updates the music volume in the audio mixer
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

    /// Updates the sound effects volume in the audio mixer
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

    /// Saves current volume settings to PlayerPrefs for persistence
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

    /// Loads saved volume settings from PlayerPrefs or uses defaults
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
}