using UnityEngine;

/// <summary>
/// Static utility class that manages the global pause state across the game
/// Provides a centralized way to check and modify pause state from any script
/// </summary>
public static class PauseManager
{
    /// <summary>
    /// Tracks whether the game is currently paused
    /// </summary>
    private static bool isPaused = false;

    /// <summary>
    /// Checks if the game is currently paused
    /// </summary>
    /// <returns>True if the game is paused, false otherwise</returns>
    public static bool IsPaused()
    {
        return isPaused;
    }

    /// <summary>
    /// Sets the game's pause state
    /// </summary>
    /// <param name="paused">True to pause the game, false to unpause</param>
    public static void SetPaused(bool paused)
    {
        isPaused = paused;
    }

    /// <summary>
    /// Toggles the current pause state of the game
    /// </summary>
    /// <returns>The new pause state after toggling</returns>
    public static bool TogglePause()
    {
        isPaused = !isPaused;
        return isPaused;
    }
}