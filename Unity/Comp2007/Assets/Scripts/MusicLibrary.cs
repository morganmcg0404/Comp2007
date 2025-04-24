using UnityEngine;

/// Represents a music track with its name and associated audio clips
[System.Serializable]
public struct MusicTrack
{
    public string trackName;    // Identifier for the track
    public AudioClip[] Clip;    // Array of audio clips for variation
}

/// Manages a collection of music tracks and provides access to them by name
/// Inherits from MonoBehaviour to be attached to a GameObject
public class MusicLibrary : MonoBehaviour
{
    /// Array of music tracks available in the library
    /// Exposed to Unity Inspector for easy configuration
    public MusicTrack[] musicTracks;

    /// Retrieves a random audio clip from the specified track
    public AudioClip GetClipFromName(string trackName)
    {
        // Search through all tracks
        foreach (var track in musicTracks)
        {
            // If track name matches
            if (track.trackName == trackName)
            {
                // Return random clip from array if there are multiple clips
                // Uses Unity's Random.Range for random selection
                return track.Clip[Random.Range(0, track.Clip.Length)];
            }
        }
        // Return null if track not found
        return null;
    }
}