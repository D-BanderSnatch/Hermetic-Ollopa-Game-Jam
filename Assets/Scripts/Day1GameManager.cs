using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Day1GameManager : MonoBehaviour, IGameOverHandler
{
    [Header("Jumpscare")]
    public GameObject jumpscarePanel;
    public JumpscareController jumpscareController;
    [Tooltip("The Retry button and 'You Lose' text — hidden until the jumpscare sequence finishes.")]
    public GameObject[] postJumpscareUI;

    [Header("Day Complete")]
    public GameObject dayCompletePanel;
    [Tooltip("CanvasGroup on the dayCompletePanel object, used to fade it in after the white flash.")]
    public CanvasGroup dayCompleteCanvasGroup;
    [Tooltip("How long the Day Complete panel takes to fade in, in seconds.")]
    public float dayCompleteFadeDuration = 0.5f;

    [Header("Victory Fade")]
    [Tooltip("A full-screen white Image with a CanvasGroup, used to fade to white before showing the Day Complete panel.")]
    public CanvasGroup victoryFadeCanvasGroup;
    [Tooltip("How long the fade to white takes, in seconds.")]
    public float victoryFadeDuration = 1f;
    [Tooltip("How long to hold at full white before the Day Complete panel appears, in seconds.")]
    public float victoryHoldDuration = 0.3f;

    [Header("Victory Music")]
    [Tooltip("Music that starts playing once the player successfully survives this day.")]
    public AudioClip victoryMusic;

    [Header("Next Day")]
    [Tooltip("Scene name to load when the player continues past Day 1. Must match the exact scene name in Build Settings.")]
    public string day2SceneName = "Day2";

    [Header("Main Menu")]
    [Tooltip("Scene name of your Main Menu scene. Must match the exact scene name in Build Settings.")]
    public string mainMenuSceneName = "Main menu";

    private bool gameEnded = false;

    void Start()
    {
        GameProgress.SaveCurrentSceneAsContinuePoint();

        if (jumpscarePanel != null)
        {
            jumpscarePanel.SetActive(false);
        }

        // Keep the Retry button / "You Lose" text hidden until the jumpscare sequence finishes.
        foreach (var ui in postJumpscareUI)
        {
            if (ui != null) ui.SetActive(false);
        }

        if (dayCompletePanel != null)
        {
            dayCompletePanel.SetActive(false);
        }

        if (dayCompleteCanvasGroup != null)
        {
            dayCompleteCanvasGroup.alpha = 0f;
            dayCompleteCanvasGroup.interactable = false;
            dayCompleteCanvasGroup.blocksRaycasts = false;
        }

        if (victoryFadeCanvasGroup != null)
        {
            victoryFadeCanvasGroup.gameObject.SetActive(true);
            victoryFadeCanvasGroup.alpha = 0f;
            victoryFadeCanvasGroup.interactable = false;
            victoryFadeCanvasGroup.blocksRaycasts = false;
        }
    }

    public void PlayerLost()
    {
        if (gameEnded)
        {
            return;
        }

        gameEnded = true;

        Debug.Log("DAY 1 LOST!");

        // Stop time
        DayTimeManager timeManager =
            FindFirstObjectByType<DayTimeManager>();

        if (timeManager != null)
        {
            timeManager.StopDay();
        }

        // Stop monster
        Day1Monster monster =
            FindFirstObjectByType<Day1Monster>();

        if (monster != null)
        {
            monster.StopMonster();
        }

        // Stop background music
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.StopMusic();
        }

        // Show jumpscare panel, then actually run the jumpscare sequence
        if (jumpscarePanel != null)
        {
            jumpscarePanel.SetActive(true);
        }

        if (jumpscareController != null)
        {
            jumpscareController.onGameOver = ShowPostJumpscareUI;
            jumpscareController.TriggerJumpscare();
        }
        else
        {
            Debug.LogWarning("Day1GameManager: jumpscareController is not assigned, jumpscare sequence will not play.");
            ShowPostJumpscareUI();
        }
    }

    // Called once the jumpscare sequence (flash/jitter/hold/fade) finishes.
    private void ShowPostJumpscareUI()
    {
        foreach (var ui in postJumpscareUI)
        {
            if (ui != null) ui.SetActive(true);
        }
    }

    public void DayComplete()
    {
        if (gameEnded)
        {
            return;
        }

        gameEnded = true;

        Debug.Log("DAY 1 SURVIVED!");

        // Player has now earned Day 2 — advance the continue point immediately.
        // This ensures Menu -> Continue lands on Day 2 even if the player goes
        // straight to the main menu from this win panel instead of pressing Continue.
        GameProgress.SaveContinuePoint(day2SceneName);

        Day1Monster monster =
            FindFirstObjectByType<Day1Monster>();

        if (monster != null)
        {
            monster.StopMonster();
        }

        // Switch to victory music
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.PlayMusic(victoryMusic, true, false);
        }

        StartCoroutine(FadeToWhiteThenShowDayComplete());
    }

    private IEnumerator FadeToWhiteThenShowDayComplete()
    {
        Debug.Log("FadeToWhiteThenShowDayComplete: started");

        if (victoryFadeCanvasGroup != null)
        {
            Debug.Log("FadeToWhiteThenShowDayComplete: victoryFadeCanvasGroup is assigned, starting fade");

            victoryFadeCanvasGroup.blocksRaycasts = true;

            float timer = 0f;
            float startingAlpha = victoryFadeCanvasGroup.alpha;

            while (timer < victoryFadeDuration)
            {
                timer += Time.unscaledDeltaTime;
                victoryFadeCanvasGroup.alpha = Mathf.Lerp(startingAlpha, 1f, timer / victoryFadeDuration);
                yield return null;
            }

            victoryFadeCanvasGroup.alpha = 1f;

            Debug.Log("FadeToWhiteThenShowDayComplete: fade complete, alpha is now " + victoryFadeCanvasGroup.alpha);

            if (victoryHoldDuration > 0f)
            {
                yield return new WaitForSecondsRealtime(victoryHoldDuration);
            }

            Debug.Log("FadeToWhiteThenShowDayComplete: hold finished");
        }
        else
        {
            Debug.LogWarning("FadeToWhiteThenShowDayComplete: victoryFadeCanvasGroup is NULL, skipping fade entirely");
        }

        if (dayCompletePanel != null)
        {
            dayCompletePanel.SetActive(true);
            Debug.Log("FadeToWhiteThenShowDayComplete: dayCompletePanel activated");

            yield return StartCoroutine(FadeInDayCompletePanel());
        }
        else
        {
            Debug.LogWarning("FadeToWhiteThenShowDayComplete: dayCompletePanel is NULL");
        }
    }

    private IEnumerator FadeInDayCompletePanel()
    {
        if (dayCompleteCanvasGroup == null)
        {
            // No CanvasGroup assigned - nothing to fade, panel just shows instantly (already active).
            yield break;
        }

        dayCompleteCanvasGroup.alpha = 0f;
        dayCompleteCanvasGroup.interactable = false;
        dayCompleteCanvasGroup.blocksRaycasts = false;

        float timer = 0f;

        while (timer < dayCompleteFadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            dayCompleteCanvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / dayCompleteFadeDuration);
            yield return null;
        }

        dayCompleteCanvasGroup.alpha = 1f;
        dayCompleteCanvasGroup.interactable = true;
        dayCompleteCanvasGroup.blocksRaycasts = true;
    }

    // BUTTON FUNCTION
    public void RetryDay1()
    {
        Debug.Log("Restarting Day 1...");

        string currentScene = SceneManager.GetActiveScene().name;

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.StartTransition(currentScene);
        }
        else
        {
            Debug.LogWarning("Day1GameManager: No SceneTransitionManager found, loading directly.");
            SceneManager.LoadScene(currentScene);
        }
    }

    // BUTTON FUNCTION - hook this up to the "Continue" button
    // on the Day Complete panel
    public void ContinueToday2()
    {
        Debug.Log("Continuing to Day 2...");

        if (string.IsNullOrEmpty(day2SceneName))
        {
            Debug.LogError("Day1GameManager: day2SceneName is empty!");
            return;
        }

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.StartTransition(day2SceneName);
        }
        else
        {
            Debug.LogWarning("Day1GameManager: No SceneTransitionManager found, loading directly.");
            SceneManager.LoadScene(day2SceneName);
        }
    }

    // BUTTON FUNCTION - hook this up to the "Menu" button
    // on the Day Complete panel (and/or the jumpscare panel, if it has one too)
    public void ReturnToMainMenu()
    {
        Debug.Log("Returning to Main Menu...");

        if (string.IsNullOrEmpty(mainMenuSceneName))
        {
            Debug.LogError("Day1GameManager: mainMenuSceneName is empty!");
            return;
        }

        Time.timeScale = 1f; // in case anything paused it, don't carry that into the Main Menu

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.StartTransition(mainMenuSceneName);
        }
        else
        {
            Debug.LogWarning("Day1GameManager: No SceneTransitionManager found, loading directly.");
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}