using UnityEngine;
using UnityEngine.SceneManagement;

public class Day5GameManager : MonoBehaviour, IGameOverHandler
{
    [Header("Ending Scene Names")]
    public string goodEndingScene = "GoodEnding"; // monster got in, OR survived to 12AM without all windows barricaded
    public string badEndingScene = "BadEnding";   // every window barricaded, whenever that happens

    [Header("UI Cleanup")]
    [Tooltip("Assign the root Canvas/GameObject holding your gameplay HUD (Clock, PickUpPrompt, BarricadePrompt, Barricade Panel, InventoryPanel, etc.). Gets hidden immediately once an ending triggers, so nothing flashes on screen before the fade covers it.")]
    public GameObject gameplayUI;

    private bool gameEnded = false;

    void Start()
    {
        GameProgress.SaveCurrentSceneAsContinuePoint();
    }

    void OnEnable()
    {
        // Listen for ANY window being barricaded, so we can check immediately
        // whether that was the LAST one - instead of only checking once at 12AM.
        WindowBarricade.OnWindowBarricaded += HandleWindowBarricaded;
    }

    void OnDisable()
    {
        WindowBarricade.OnWindowBarricaded -= HandleWindowBarricaded;
    }

    // ==============================================
    // CALLED EVERY TIME A WINDOW GETS BARRICADED
    // ==============================================

    private void HandleWindowBarricaded()
    {
        if (gameEnded)
        {
            return;
        }

        if (AreAllWindowsBarricaded())
        {
            Debug.Log("DAY 5 - Last window barricaded -> BAD ENDING (immediate)");
            TriggerEnding(badEndingScene);
        }
    }

    // ==============================================
    // MONSTER GOT IN MID-GAME -> GOOD ENDING
    // (unchanged - separate, earlier loss path)
    // ==============================================

    public void PlayerLost()
    {
        if (gameEnded)
        {
            return;
        }

        Debug.Log("DAY 5 - Monster got in -> GOOD ENDING");
        TriggerEnding(goodEndingScene);
    }

    // ==============================================
    // TIMER RAN OUT (12AM) WITHOUT EVERY WINDOW BEING
    // BARRICADED YET -> GOOD ENDING
    //
    // (If every window HAD been barricaded already, HandleWindowBarricaded()
    // would have ended the day earlier via the Bad Ending, and gameEnded would
    // already be true by the time this fires - so reaching here for real means
    // at least one window was still open when time ran out.)
    // ==============================================

    public void DayComplete()
    {
        if (gameEnded)
        {
            return;
        }

        Debug.Log("DAY 5 - Survived until 12AM without barricading every window -> GOOD ENDING");
        TriggerEnding(goodEndingScene);
    }

    // ==============================================
    // SHARED ENDING LOGIC
    // ==============================================

    private void TriggerEnding(string sceneName)
    {
        if (gameEnded)
        {
            return;
        }

        gameEnded = true;

        // Hide the HUD right away, before anything else - so there's no chance
        // of a prompt/counter still being visible while the fade starts covering it.
        if (gameplayUI != null)
        {
            gameplayUI.SetActive(false);
        }

        StopEverything();
        GameProgress.ClearContinueSave();
        LoadEndingScene(sceneName);
    }

    // ==============================================
    // BARRICADE DETECTION
    // ==============================================

    private bool AreAllWindowsBarricaded()
    {
        WindowBarricade[] windows =
            FindObjectsByType<WindowBarricade>(FindObjectsSortMode.None);

        if (windows == null || windows.Length == 0)
        {
            Debug.LogWarning("Day5GameManager: No WindowBarricade objects found in the scene - defaulting to NOT all barricaded.");
            return false;
        }

        foreach (WindowBarricade window in windows)
        {
            if (window == null || !window.IsBarricaded())
            {
                return false;
            }
        }

        return true;
    }

    // ==============================================
    // LOAD AN ENDING SCENE, FADED
    // ==============================================

    void LoadEndingScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("Day5GameManager: ending scene name is empty!");
            return;
        }

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.StartTransition(sceneName);
        }
        else
        {
            Debug.LogWarning("Day5GameManager: No SceneTransitionManager found, loading directly.");
            SceneManager.LoadScene(sceneName);
        }
    }

    // ==============================================
    // SHARED CLEANUP
    // ==============================================

    void StopEverything()
    {
        DayTimeManager timeManager =
            FindFirstObjectByType<DayTimeManager>();

        if (timeManager != null)
        {
            timeManager.StopDay();
        }

        Day1Monster[] monsters =
            FindObjectsByType<Day1Monster>(FindObjectsSortMode.None);

        foreach (Day1Monster monster in monsters)
        {
            monster.StopMonster();
        }

        if (BarricadeInventory.Instance != null)
        {
            BarricadeInventory.Instance.HidePanel();
        }
    }
}