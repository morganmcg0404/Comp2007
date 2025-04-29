using UnityEngine;

/// <summary>
/// Represents a music track with its name and associated audio clips
/// </summary>
[System.Serializable]
public struct MusicTrack
{
    /// <summary>
    /// Identifier for the track used for lookup
    /// </summary>
    public string trackName;
    
    /// <summary>
    /// Array of audio clips for variation when playing this track
    /// One clip will be randomly selected when the track is requested
    /// </summary>
    public AudioClip[] Clip;
}

/// <summary>
/// Manages a collection of music tracks and provides access to them by name
/// Inherits from MonoBehaviour to be attached to a GameObject
/// </summary>
public class MusicLibrary : MonoBehaviour
{
    /// <summary>
    /// Array of music tracks available in the library
    /// Exposed to Unity Inspector for easy configuration
    /// </summary>
    public MusicTrack[] musicTracks;

    /// <summary>
    /// Retrieves a random audio clip from the specified track
    /// </summary>
    /// <param name="trackName">The name of the track to search for</param>
    /// <returns>A random AudioClip from the matching track, or null if track not found</returns>
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
    
    /// <summary>
    /// Checks if a track exists in the library
    /// </summary>
    /// <param name="trackName">The name of the track to search for</param>
    /// <returns>True if the track exists, false otherwise</returns>
    public bool HasTrack(string trackName)
    {
        foreach (var track in musicTracks)
        {
            if (track.trackName == trackName)
            {
                return true;
            }
        }
        return false;
    }
    
    /// <summary>
    /// Gets the number of available clips for a specific track
    /// </summary>
    /// <param name="trackName">The name of the track to search for</param>
    /// <returns>The number of clips in the track, or 0 if track not found</returns>
    public int GetClipCount(string trackName)
    {
        foreach (var track in musicTracks)
        {
            if (track.trackName == trackName)
            {
                return track.Clip.Length;
            }
        }
        return 0;
    }
}