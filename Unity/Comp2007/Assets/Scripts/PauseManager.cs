using UnityEngine;

public static class PauseManager
{
    private static bool isPaused = false;

    public static bool IsPaused()
    {
        return isPaused;
    }

    public static void SetPaused(bool paused)
    {
        isPaused = paused;
    }
}