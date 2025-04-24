using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using UnityEngine.UI;

/// Handles main menu functionality including volume control and scene transitions
public class MainMenu : MonoBehaviour
{
    // References to Unity components
    [SerializeField] private AudioMixer audioMixer;    // Controls audio mixing
    [SerializeField] private Slider musicSlider;       // UI slider for music volume
    [SerializeField] private Slider soundSlider;       // UI slider for sound effects volume

    /// Called when the script instance is being loaded
    /// Initializes volume settings and starts menu music
    private void Start()
    {
        LoadVolume();  // Load saved volume settings
        MusicManager.Instance.PlayMusic("Main Menu");  // Start playing menu music
    }

    /// Loads the main game scene when play button is clicked
    public void Play()
    {
        SceneManager.LoadScene("Game");
    }

    /// Quits the application when quit button is clicked
    public void Quit()
    {
        Application.Quit();
    }

    /// Updates the music volume in the audio mixer
    public void UpdateMusicVolume(float volume)
    {
        audioMixer.SetFloat("MusicVolume", volume);
    }

    /// Updates the sound effects volume in the audio mixer
    public void UpdateSoundVolume(float volume)
    {
        audioMixer.SetFloat("SFXVolume", volume);
    }

    /// Saves current volume settings to PlayerPrefs for persistence
    public void SaveVolume()
    {
        // Get and save music volume
        audioMixer.GetFloat("MusicVolume", out float musicVolume);
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);

        // Get and save sound effects volume
        audioMixer.GetFloat("SFXVolume", out float sfxVolume);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
    }

    /// Loads saved volume settings from PlayerPrefs or uses defaults
    /// Updates UI sliders and audio mixer with loaded values
    public void LoadVolume()
    {
        // Default volume settings
        const float DEFAULT_MUSIC_VOLUME = -10f;
        const float DEFAULT_SFX_VOLUME = 0f;

        // Load music volume (default: -10f)
        musicSlider.value = PlayerPrefs.HasKey("MusicVolume")
            ? PlayerPrefs.GetFloat("MusicVolume")
            : DEFAULT_MUSIC_VOLUME;

        // Load sound effects volume (default: 0f)
        soundSlider.value = PlayerPrefs.HasKey("SFXVolume")
            ? PlayerPrefs.GetFloat("SFXVolume")
            : DEFAULT_SFX_VOLUME;

        // Apply loaded volumes to the audio mixer
        UpdateMusicVolume(musicSlider.value);
        UpdateSoundVolume(soundSlider.value);
    }
}