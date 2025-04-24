using UnityEngine;

/// Represents a group of related sound effect variations
[System.Serializable]
public struct SoundEffect
{
    public string GroupID;      // Unique identifier for the sound effect group
    public AudioClip[] Clip;    // Array of audio clip variations for the sound
}

/// Manages a collection of sound effects and provides access to them by name
/// Allows for variation in sound effects by randomly selecting from multiple clips
public class SoundLibrary : MonoBehaviour
{
    /// Array of sound effect groups available in the library
    /// Exposed to Unity Inspector for easy configuration
    public SoundEffect[] soundEffects;

    /// Retrieves a random audio clip from the specified sound effect group
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
}