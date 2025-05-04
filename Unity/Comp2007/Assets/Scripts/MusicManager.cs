using UnityEngine;
using System.Collections;

/// <summary>
/// Manages background music playback and transitions with crossfading
/// Implements the Singleton pattern for global access
/// </summary>
public class MusicManager : MonoBehaviour
{
    /// <summary>
    /// Singleton instance accessible from anywhere
    /// </summary>
    public static MusicManager Instance;

    /// <summary>
    /// Reference to music track collection containing available audio clips
    /// </summary>
    [SerializeField] private MusicLibrary musicLibrary;
    
    /// <summary>
    /// Audio source component used for playing music tracks
    /// </summary>
    [SerializeField] private AudioSource musicSource;

    /// <summary>
    /// Initializes the singleton instance
    /// Ensures only one MusicManager exists in the scene
    /// </summary>
    private void Awake()
    {
        // Implement singleton pattern
        if (Instance != null)
        {
            Destroy(gameObject);    // Destroy duplicate instances
        }
        else
        {
            Instance = this;                    // Set this as the singleton instance
            DontDestroyOnLoad(gameObject);     // Preserve across scene changes
        }
    }

    /// <summary>
    /// Starts playing a music track with smooth crossfading
    /// </summary>
    /// <param name="trackName">Name of the track to play from the music library</param>
    /// <param name="fadeDuration">Duration of the crossfade transition in seconds</param>
    public void PlayMusic(string trackName, float fadeDuration = 0.5f)
    {
        StartCoroutine(AnimateMusicCrossfade(musicLibrary.GetClipFromName(trackName), fadeDuration));
    }

    /// <summary>
    /// Coroutine that handles the crossfade animation between tracks
    /// Fades out current track, switches to new track, then fades in
    /// </summary>
    /// <param name="nextTrack">AudioClip to transition to</param>
    /// <param name="fadeDuration">Duration of each fade (both in and out) in seconds</param>
    /// <returns>IEnumerator for coroutine execution</returns>
    private IEnumerator AnimateMusicCrossfade(AudioClip nextTrack, float fadeDuration = 0.5f)
    {
        // Skip crossfade if the clip is null (track wasn't found)
        if (nextTrack == null)
        {
            Debug.LogWarning("MusicManager: Attempted to play a null music track");
            yield break;
        }
        
        // Fade out current track
        float percent = 0;
        while (percent < 1)
        {
            percent += Time.deltaTime * 1 / fadeDuration;
            musicSource.volume = Mathf.Lerp(1f, 0, percent);
            yield return null;
        }

        // Switch to new track
        musicSource.clip = nextTrack;
        musicSource.Play();

        // Fade in new track
        percent = 0;
        while (percent < 1)
        {
            percent += Time.deltaTime * 1 / fadeDuration;
            musicSource.volume = Mathf.Lerp(0, 1f, percent);
            yield return null;
        }
    }
    
    /// <summary>
    /// Fades out all music without starting a new track
    /// </summary>
    /// <param name="fadeDuration">Duration of the fade out in seconds</param>
    public void StopMusic(float fadeDuration = 0.5f)
    {
        StartCoroutine(FadeOutMusic(fadeDuration));
    }
    
    /// <summary>
    /// Coroutine that fades out the current music
    /// </summary>
    /// <param name="fadeDuration">Duration of the fade out in seconds</param>
    /// <returns>IEnumerator for coroutine execution</returns>
    private IEnumerator FadeOutMusic(float fadeDuration)
    {
        // Fade out current track
        float percent = 0;
        while (percent < 1)
        {
            percent += Time.deltaTime * 1 / fadeDuration;
            musicSource.volume = Mathf.Lerp(1f, 0, percent);
            yield return null;
        }
        
        // Stop playback completely
        musicSource.Stop();
    }
    
    /// <summary>
    /// Gets the currently playing music track's AudioClip
    /// </summary>
    /// <returns>The currently playing AudioClip or null if nothing is playing</returns>
    public AudioClip GetCurrentTrack()
    {
        if (musicSource != null)
        {
            return musicSource.clip;
        }
        return null;
    }
    
    /// <summary>
    /// Sets the volume of the music playback directly without fading
    /// </summary>
    /// <param name="volume">Volume level between 0 (silent) and 1 (full volume)</param>
    public void SetVolume(float volume)
    {
        if (musicSource != null)
        {
            musicSource.volume = Mathf.Clamp01(volume);
        }
    }
    
    /// <summary>
    /// Ensures the MusicManager is available, even when accessed before initialization
    /// </summary>
    /// <returns>The singleton MusicManager instance</returns>
    public static MusicManager GetInstance()
    {
        if (Instance == null)
        {
            // Try to find existing instance
            Instance = FindFirstObjectByType<MusicManager>();
            
            // If still not found, create one
            if (Instance == null)
            {
                GameObject go = new GameObject("MusicManager");
                Instance = go.AddComponent<MusicManager>();
                DontDestroyOnLoad(go);
                Debug.Log("Created new MusicManager instance");
            }
        }
        
        return Instance;
    }
}