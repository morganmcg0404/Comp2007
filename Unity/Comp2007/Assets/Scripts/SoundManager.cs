using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

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

    [SerializeField] private AudioMixer audioMixer;

    private Dictionary<string, AudioMixerGroup> mixerGroups = new Dictionary<string, AudioMixerGroup>();

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
    /// Gets an AudioMixerGroup by name from the assigned AudioMixer
    /// </summary>
    /// <param name="groupName">Name of the mixer group</param>
    /// <returns>The AudioMixerGroup or null if not found</returns>
    public AudioMixerGroup GetAudioMixerGroup(string groupName)
    {
        // Return from cache if available
        if (mixerGroups.ContainsKey(groupName))
            return mixerGroups[groupName];
    
        // Not in cache, try to find it
        if (audioMixer != null)
        {
            AudioMixerGroup[] groups = audioMixer.FindMatchingGroups(groupName);
            if (groups.Length > 0)
            {
                // Cache and return the found group
                mixerGroups[groupName] = groups[0];
                return groups[0];
            }
        }
    
        return null;
    }

    /// <summary>
    /// Plays a 3D sound with specific mixer group assignment
    /// </summary>
    /// <param name="soundName">The name of the sound</param>
    /// <param name="position">World position for sound</param>
    /// <param name="volume">Volume level from 0-1</param>
    /// <param name="mixerGroupName">Name of the audio mixer group</param>
    /// <param name="pitch">Pitch adjustment (1.0 is normal)</param>
    public void PlaySound3DWithMixer(string soundName, Vector3 position, float volume = 1.0f, 
                                    string mixerGroupName = "SFX", float pitch = 1.0f)
    {
        AudioClip clip = sfxLibrary.GetClipFromName(soundName);
        if (clip == null) return;
    
        // Create temporary audio source at position
        GameObject tempGO = new GameObject("TempAudio_" + soundName);
        tempGO.transform.position = position;
    
        // Add and configure audio source
        AudioSource audioSource = tempGO.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.pitch = pitch;
        audioSource.spatialBlend = 1.0f; // Full 3D
    
        // Assign mixer group if available
        AudioMixerGroup group = GetAudioMixerGroup(mixerGroupName);
        if (group != null)
            audioSource.outputAudioMixerGroup = group;
    
        // Play sound
        audioSource.Play();
    
        // Destroy the GameObject after the clip is done
        Destroy(tempGO, clip.length + 0.1f);
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