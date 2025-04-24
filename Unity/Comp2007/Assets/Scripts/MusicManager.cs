using UnityEngine;
using System.Collections;

/// Manages background music playback and transitions with crossfading
/// Implements the Singleton pattern for global access
public class MusicManager : MonoBehaviour
{
    // Singleton instance accessible from anywhere
    public static MusicManager Instance;

    [SerializeField] private MusicLibrary musicLibrary;    // Reference to music track collection
    [SerializeField] private AudioSource musicSource;      // Audio source for playing music

    /// Initializes the singleton instance
    /// Ensures only one MusicManager exists in the scene
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

    /// Starts playing a music track with smooth crossfading
    public void PlayMusic(string trackName, float fadeDuration = 0.5f)
    {
        StartCoroutine(AnimateMusicCrossfade(musicLibrary.GetClipFromName(trackName), fadeDuration));
    }

    /// Coroutine that handles the crossfade animation between tracks
    /// Fades out current track, switches to new track, then fades in
    IEnumerator AnimateMusicCrossfade(AudioClip nextTrack, float fadeDuration = 0.5f)
    {
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
}