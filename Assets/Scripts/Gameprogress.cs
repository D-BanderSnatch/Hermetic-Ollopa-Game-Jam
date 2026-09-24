using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Lightweight save system for a "Continue" feature. Not a MonoBehaviour/singleton -
/// just a static wrapper around PlayerPrefs, since this doesn't need to persist in-memory
/// across scenes (it reads/writes disk directly) or run any per-frame logic.
///
/// USAGE:
/// - Call GameProgress.SaveCurrentSceneAsContinuePoint() at the start of each day scene
///   (Day1GameManager, Day2GameManager, etc.) to mark that scene as the continue point.
/// - Call GameProgress.SaveContinuePoint(nextSceneName) the moment a day is WON, so the
///   continue point advances to the next day immediately, even if the player never actually
///   loads that next scene this session (e.g. they hit "Menu" from the win panel instead
///   of "Continue").
/// - Call GameProgress.HasContinueSave() / GetContinueScene() from your Main Menu's
///   Continue button logic.
/// - Call GameProgress.ClearContinueSave() when starting a brand new game (so a fresh
///   playthrough doesn't jump back into old progress) and when the player reaches an
///   ending (so Continue doesn't linger after finishing the game).
/// </summary>
public static class GameProgress
{
    private const string ContinueSceneKey = "ContinueScene";

    public static void SaveCurrentSceneAsContinuePoint()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        PlayerPrefs.SetString(ContinueSceneKey, sceneName);
        PlayerPrefs.Save();

        Debug.Log("GameProgress: Saved continue point -> " + sceneName);
    }

    /// <summary>
    /// Explicitly saves the given scene name as the continue point, regardless of which
    /// scene is currently active. Use this when a day is completed, so the save advances
    /// to the NEXT day right away instead of waiting for that next scene to actually load.
    /// </summary>
    public static void SaveContinuePoint(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("GameProgress: Tried to save an empty continue point, ignoring.");
            return;
        }

        PlayerPrefs.SetString(ContinueSceneKey, sceneName);
        PlayerPrefs.Save();

        Debug.Log("GameProgress: Saved continue point -> " + sceneName);
    }

    public static bool HasContinueSave()
    {
        return PlayerPrefs.HasKey(ContinueSceneKey) && !string.IsNullOrEmpty(PlayerPrefs.GetString(ContinueSceneKey));
    }

    public static string GetContinueScene()
    {
        return PlayerPrefs.GetString(ContinueSceneKey, string.Empty);
    }

    public static void ClearContinueSave()
    {
        PlayerPrefs.DeleteKey(ContinueSceneKey);
        PlayerPrefs.Save();

        Debug.Log("GameProgress: Continue save cleared.");
    }
}