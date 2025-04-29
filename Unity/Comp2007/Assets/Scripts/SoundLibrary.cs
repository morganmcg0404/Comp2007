using UnityEngine;

/// <summary>
/// Represents a group of related sound effect variations
/// Stores multiple audio clips for the same effect to add variety
/// </summary>
[System.Serializable]
public struct SoundEffect
{
    /// <summary>
    /// Unique identifier used to look up this sound effect group
    /// </summary>
    public string GroupID;      // Unique identifier for the sound effect group
    
    /// <summary>
    /// Array of audio clip variations for the sound
    /// One clip will be randomly selected when the sound is played
    /// </summary>
    public AudioClip[] Clip;    // Array of audio clip variations for the sound
}

/// <summary>
/// Manages a collection of sound effects and provides access to them by name
/// Allows for variation in sound effects by randomly selecting from multiple clips
/// </summary>
public class SoundLibrary : MonoBehaviour
{
    /// <summary>
    /// Array of sound effect groups available in the library
    /// Exposed to Unity Inspector for easy configuration
    /// </summary>
    public SoundEffect[] soundEffects;

    /// <summary>
    /// Retrieves a random audio clip from the specified sound effect group
    /// </summary>
    /// <param name="name">The identifier of the sound effect group to search for</param>
    /// <returns>A random AudioClip from the matching group, or null if not found</returns>
    public AudioClip GetClipFromName(string name)
    {
        // Search through all sound effect groups
        foreach (var soundEffect in soundEffects)
        {
            if (soundEffect.GroupID == name)
            {
                // Return random clip from the array for variation
                return soundEffect.Clip[Random.Range(0, soundEffect.Clip.Length)];
            }
        }
        
        // Log warning if sound effect not found
        Debug.LogWarning($"Sound effect {name} not found!");
        return null;
    }
    
    /// <summary>
    /// Checks if a sound effect group exists in the library
    /// </summary>
    /// <param name="name">The identifier of the sound effect group to check for</param>
    /// <returns>True if the sound effect exists, false otherwise</returns>
    public bool HasSound(string name)
    {
        foreach (var soundEffect in soundEffects)
        {
            if (soundEffect.GroupID == name)
            {
                return true;
            }
        }
        return false;
    }
    
    /// <summary>
    /// Gets the number of variations available for a specific sound effect
    /// </summary>
    /// <param name="name">The identifier of the sound effect group</param>
    /// <returns>The number of audio clips in the group, or 0 if not found</returns>
    public int GetVariationCount(string name)
    {
        foreach (var soundEffect in soundEffects)
        {
            if (soundEffect.GroupID == name)
            {
                return soundEffect.Clip.Length;
            }
        }
        return 0;
    }
}