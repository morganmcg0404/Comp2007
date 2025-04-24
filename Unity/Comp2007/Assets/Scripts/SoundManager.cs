using UnityEngine;

/// Manages sound effects playback for both 2D and 3D audio sources
/// Implements the Singleton pattern for global access
public class SoundManager : MonoBehaviour
{
    /// Singleton instance accessible from anywhere
    public static SoundManager Instance;

    [SerializeField] private SoundLibrary sfxLibrary;    // Reference to sound effect library
    [SerializeField] private AudioSource sfx2DSource;     // Audio source for 2D sounds (UI, etc.)

    /// Initializes the singleton instance
    /// Ensures only one SoundManager exists across scenes
    public void Awake()
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

    /// Plays a 3D sound at a specific position in the game world
    public void PlaySound3D(AudioClip clip, Vector3 pos)
    {
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, pos);
        }
    }

    /// Plays a 3D sound by name at a specific position
    /// Uses the SoundLibrary to find the appropriate clip
    public void PlaySound3D(string soundName, Vector3 pos)
    {
        PlaySound3D(sfxLibrary.GetClipFromName(soundName), pos);
    }

    /// Plays a 2D sound (no position, good for UI sounds)
    /// Uses the SoundLibrary to find the appropriate clip
    public void PlaySound2D(string soundName)
    {
        sfx2DSource.PlayOneShot(sfxLibrary.GetClipFromName(soundName));
    }
}