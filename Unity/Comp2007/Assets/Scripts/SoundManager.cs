using UnityEngine;

/// <summary>
/// Manages sound effects playback for both 2D and 3D audio sources
/// Implements the Singleton pattern for global access
/// </summary>
public class SoundManager : MonoBehaviour
{
    /// <summary>
    /// Singleton instance accessible from anywhere
    /// </summary>
    public static SoundManager Instance;

    /// <summary>
    /// Reference to sound effect library containing all available audio clips
    /// </summary>
    [SerializeField] private SoundLibrary sfxLibrary;
    
    /// <summary>
    /// Audio source for 2D sounds such as UI elements
    /// </summary>
    [SerializeField] private AudioSource sfx2DSource;
    
    [Header("Audio Camera")]
    /// <summary>
    /// Camera that should persist between scenes for consistent audio
    /// Can be set to the same camera as MusicManager uses
    /// </summary>
    [SerializeField] private Camera audioCamera;

    /// <summary>
    /// Initializes the singleton instance
    /// Ensures only one SoundManager exists across scenes
    /// </summary>
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
            DontDestroyOnLoad(gameObject);      // Preserve across scene changes
            
            // Make sure audio camera persists if assigned
            if (audioCamera != null)
            {
                DontDestroyOnLoad(audioCamera.gameObject);
                Debug.Log("Audio camera preserved between scenes");
            }
        }
    }

    /// <summary>
    /// Plays a 3D sound at a specific position in the game world
    /// </summary>
    /// <param name="clip">The audio clip to play</param>
    /// <param name="pos">The world position where the sound should originate</param>
    public void PlaySound3D(AudioClip clip, Vector3 pos)
    {
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, pos);
        }
    }

    /// <summary>
    /// Plays a 3D sound by name at a specific position
    /// Uses the SoundLibrary to find the appropriate clip
    /// </summary>
    /// <param name="soundName">The identifier of the sound in the SoundLibrary</param>
    /// <param name="pos">The world position where the sound should originate</param>
    public void PlaySound3D(string soundName, Vector3 pos)
    {
        PlaySound3D(sfxLibrary.GetClipFromName(soundName), pos);
    }

    /// <summary>
    /// Plays a 2D sound (no position, good for UI sounds)
    /// Uses the SoundLibrary to find the appropriate clip
    /// </summary>
    /// <param name="soundName">The identifier of the sound in the SoundLibrary</param>
    public void PlaySound2D(string soundName)
    {
        sfx2DSource.PlayOneShot(sfxLibrary.GetClipFromName(soundName));
    }
    
    /// <summary>
    /// Plays a 2D sound directly from an AudioClip
    /// </summary>
    /// <param name="clip">The audio clip to play</param>
    public void PlaySound2D(AudioClip clip)
    {
        if (clip != null && sfx2DSource != null)
        {
            sfx2DSource.PlayOneShot(clip);
        }
    }
    
    /// <summary>
    /// Plays a 3D sound with custom volume and pitch settings
    /// </summary>
    /// <param name="soundName">The identifier of the sound in the SoundLibrary</param>
    /// <param name="pos">The world position where the sound should originate</param>
    /// <param name="volume">Volume level between 0 and 1</param>
    /// <param name="pitch">Pitch adjustment (1 is normal pitch)</param>
    public void PlaySound3D(string soundName, Vector3 pos, float volume, float pitch = 1.0f)
    {
        AudioClip clip = sfxLibrary.GetClipFromName(soundName);
        if (clip != null)
        {
            // Create temporary audio source at position
            GameObject tempGO = new GameObject("TempAudio");
            tempGO.transform.position = pos;
            
            // Add and configure audio source
            AudioSource audioSource = tempGO.AddComponent<AudioSource>();
            audioSource.clip = clip;
            audioSource.volume = volume;
            audioSource.pitch = pitch;
            audioSource.spatialBlend = 1.0f; // Full 3D
            
            // Play sound
            audioSource.Play();
            
            // Destroy the GameObject after the clip is done
            Destroy(tempGO, clip.length);
        }
    }
    
    /// <summary>
    /// Sets the volume level of the 2D audio source
    /// </summary>
    /// <param name="volume">Volume level between 0 and 1</param>
    public void SetSfxVolume(float volume)
    {
        if (sfx2DSource != null)
        {
            sfx2DSource.volume = Mathf.Clamp01(volume);
        }
    }
    
    /// <summary>
    /// Gets the SoundLibrary instance for direct access
    /// </summary>
    /// <returns>Reference to the SoundLibrary</returns>
    public SoundLibrary GetSoundLibrary()
    {
        return sfxLibrary;
    }
    
    /// <summary>
    /// Gets the preserved audio camera
    /// </summary>
    /// <returns>The audio camera that persists between scenes</returns>
    public Camera GetAudioCamera()
    {
        return audioCamera;
    }
    
    /// <summary>
    /// Sets a camera as the persistent audio camera
    /// </summary>
    /// <param name="camera">Camera to mark as persistent</param>
    public void SetAudioCamera(Camera camera)
    {
        if (camera != null)
        {
            audioCamera = camera;
            DontDestroyOnLoad(camera.gameObject);
            Debug.Log("New audio camera set to persist between scenes");
        }
    }
    
    /// <summary>
    /// Ensures the SoundManager is available, even when accessed before initialization
    /// </summary>
    /// <returns>The singleton SoundManager instance</returns>
    public static SoundManager GetInstance()
    {
        if (Instance == null)
        {
            // Try to find existing instance
            Instance = FindFirstObjectByType<SoundManager>();
            
            // If still not found, create one
            if (Instance == null)
            {
                GameObject go = new GameObject("SoundManager");
                Instance = go.AddComponent<SoundManager>();
                DontDestroyOnLoad(go);
                Debug.Log("Created new SoundManager instance");
            }
        }
        
        return Instance;
    }
}