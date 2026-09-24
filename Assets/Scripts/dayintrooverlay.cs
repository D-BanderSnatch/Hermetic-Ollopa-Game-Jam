using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Drop one of these into each day scene's Canvas (Day1, Day2, etc.) for a black-screen
/// intro: black overlay -> one or more dialogue lines -> optional "what's new today"
/// text briefing -> optional story hint -> fade out to reveal gameplay, then starts
/// background music.
///
/// SETUP - Core intro (unchanged from before):
/// 1. Create a full-screen black Image, add a CanvasGroup to it, assign to "Overlay Group".
/// 2. Add a TMP_Text as a child of that same object (so it fades with the black), assign to "Dialogue Text".
/// 3. Fill in "Dialogue Lines" with whatever text should appear, one entry per line.
/// 4. This object's Canvas sort order should be high enough to render above all other gameplay UI.
/// 5. Assign a clip to "Background Music" if you want music to start once the intro finishes.
///
/// SETUP - Day briefing (text-only):
/// 1. As a child of the same object as Overlay Group, create an empty GameObject "BriefingPanel"
///    with a CanvasGroup on it. Assign that CanvasGroup to "Briefing Panel" below.
/// 2. Inside it, add a TMP_Text for a heading (assign to "Briefing Title Text") and another
///    TMP_Text for the body (assign to "Briefing Text").
/// 3. Fill in "Briefing Lines" per day scene. Leave it empty to skip the briefing that day.
///
/// SETUP - Story Hint (new, completely separate styling from the above two):
/// 1. As another child of the same object as Overlay Group, create an empty GameObject
///    "StoryHintPanel" with its own CanvasGroup on it. Assign it to "Story Hint Panel" below.
/// 2. Inside it, add ONE TMP_Text for the hint's body, assign to "Story Hint Text". This is a
///    completely separate TMP_Text component from Dialogue Text and Briefing Text, so changing
///    its font asset, color, or size in the Inspector only ever affects this hint - nothing else.
///    Add an optional second TMP_Text for a heading (e.g. "Did You Know?") and assign it to
///    "Story Hint Title Text" if you want one.
/// 3. Fill in "Story Hint Lines" per day scene. Leave it empty to skip the hint that day.
/// </summary>
public class DayIntroOverlay : MonoBehaviour
{
    [Header("References")]
    [Tooltip("CanvasGroup on a full-screen black Image, covering the whole scene.")]
    public CanvasGroup overlayGroup;

    [Tooltip("TMP_Text that shows each dialogue line. Should be a child of the same object as Overlay Group, so it fades with the black.")]
    public TMP_Text dialogueText;

    [Header("Dialogue")]
    [Tooltip("One or more lines shown in sequence before the overlay fades away.")]
    public string[] dialogueLines;

    [Header("Timing")]
    [Tooltip("How long the screen stays pure black (no text yet) before the first line appears.")]
    public float blackHoldBeforeText = 1f;

    [Tooltip("If true, the player must click/press a key to advance each line. If false, each line auto-advances after Line Display Duration.")]
    public bool waitForInputToAdvance = true;

    [Tooltip("Only used if Wait For Input To Advance is false: how long each line stays on screen before auto-advancing.")]
    public float lineDisplayDuration = 3f;

    [Tooltip("How long the final fade from black to fully revealed gameplay takes, in seconds.")]
    public float finalFadeOutDuration = 1f;

    [Header("Input (only used if Wait For Input To Advance is true)")]
    public Key advanceKey = Key.Space;
    public bool advanceOnMouseClick = true;

    [Header("Gameplay Freeze")]
    [Tooltip("If true, Time.timeScale is set to 0 while this intro plays, so the day's timer/monster don't start ticking underneath the black screen, then restored to 1 once the intro finishes.")]
    public bool pauseGameplayDuringIntro = true;

    [Header("Day Briefing (shown after dialogue)")]
    [Tooltip("CanvasGroup on the briefing panel - should be a child of the same object as Overlay Group so it fades with the black background at the end.")]
    public CanvasGroup briefingPanel;

    [Tooltip("Optional heading text for the briefing, e.g. a TMP_Text showing 'New Today'.")]
    public TMP_Text briefingTitleText;

    [Tooltip("Text shown in Briefing Title Text.")]
    public string briefingTitle = "New Today";

    [Tooltip("Body TMP_Text where each briefing line gets listed, one per line.")]
    public TMP_Text briefingText;

    [Tooltip("What to show this day, one line each (e.g. \"+1 New Room\", \"+1 Monster\", \"+1 Furniture\", \"+2 Windows\"). Leave empty to skip the briefing entirely for this day.")]
    public string[] briefingLines;

    [Tooltip("How long the briefing panel takes to fade in, in seconds.")]
    public float briefingFadeInDuration = 0.4f;

    [Tooltip("If true, the player must press Space/click (same as dialogue) to dismiss the briefing. If false, it auto-advances on its own after Briefing Display Duration.")]
    public bool briefingWaitForInput = false;

    [Tooltip("Only used if Briefing Wait For Input is false: how long the briefing stays fully visible before automatically fading out and continuing.")]
    public float briefingDisplayDuration = 3f;

    [Header("Story Hint (shown after the briefing, before gameplay)")]
    [Tooltip("CanvasGroup on the story hint panel - a completely separate object from Briefing Panel, so it can be styled independently.")]
    public CanvasGroup storyHintPanel;

    [Tooltip("Optional heading text for the hint, e.g. 'Did You Know?'. A separate TMP_Text component from every other text in this script.")]
    public TMP_Text storyHintTitleText;

    [Tooltip("Text shown in Story Hint Title Text.")]
    public string storyHintTitle = "Did You Know?";

    [Tooltip("Body TMP_Text for the hint itself. Its own separate component - change its font asset, color, or size here without affecting Dialogue Text or Briefing Text.")]
    public TMP_Text storyHintText;

    [Tooltip("What to show this day, one line each. Lines are joined together in Story Hint Text. Leave empty to skip the hint entirely for this day.")]
    public string[] storyHintLines;

    [Tooltip("How long the story hint panel takes to fade in, in seconds.")]
    public float storyHintFadeInDuration = 0.4f;

    [Tooltip("If true, the player must press Space/click (same as dialogue) to dismiss the hint. If false, it auto-advances on its own after Story Hint Display Duration.")]
    public bool storyHintWaitForInput = false;

    [Tooltip("Only used if Story Hint Wait For Input is false: how long the hint stays fully visible before automatically fading out and continuing to gameplay.")]
    public float storyHintDisplayDuration = 4f;

    [Header("Music")]
    [Tooltip("Background music to start once the intro fully fades out and gameplay is revealed. Leave empty if this day shouldn't start/change music.")]
    public AudioClip backgroundMusic;

    private bool waitingForInput = false;
    private int currentLineIndex = -1;

    private void Start()
    {
        if (overlayGroup == null)
        {
            Debug.LogWarning("DayIntroOverlay: Overlay Group is not assigned - intro will be skipped.");
            return;
        }

        overlayGroup.gameObject.SetActive(true);
        overlayGroup.alpha = 1f;
        overlayGroup.interactable = true;
        overlayGroup.blocksRaycasts = true;

        if (dialogueText != null)
        {
            dialogueText.text = string.Empty;
        }

        if (briefingPanel != null)
        {
            briefingPanel.gameObject.SetActive(false);
        }

        if (storyHintPanel != null)
        {
            storyHintPanel.gameObject.SetActive(false);
        }

        if (pauseGameplayDuringIntro)
        {
            Time.timeScale = 0f;
        }

        // Prevent Escape (or the pause button) from opening the pause menu
        // while this black-screen intro is playing.
        if (PauseManager.Instance != null)
        {
            PauseManager.Instance.allowPause = false;
        }

        StartCoroutine(PlayIntro());
    }

    private void Update()
    {
        if (!waitingForInput)
        {
            return;
        }

        bool pressedKey = advanceKey != Key.None && Keyboard.current != null && Keyboard.current[advanceKey].wasPressedThisFrame;
        bool clicked = advanceOnMouseClick && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;

        if (pressedKey || clicked)
        {
            waitingForInput = false;
        }
    }

    private IEnumerator PlayIntro()
    {
        // Pure black hold before any text appears.
        yield return WaitRealtimeOrScaled(blackHoldBeforeText);

        if (dialogueLines != null)
        {
            for (int i = 0; i < dialogueLines.Length; i++)
            {
                currentLineIndex = i;

                if (dialogueText != null)
                {
                    dialogueText.text = dialogueLines[i];
                }

                if (waitForInputToAdvance)
                {
                    waitingForInput = true;

                    while (waitingForInput)
                    {
                        yield return null;
                    }
                }
                else
                {
                    yield return WaitRealtimeOrScaled(lineDisplayDuration);
                }
            }
        }

        if (dialogueText != null)
        {
            dialogueText.text = string.Empty;
        }

        // Show the "what's new today" text briefing, if this day has any lines configured.
        yield return StartCoroutine(ShowBriefing());

        // Show a story hint, if this day has any lines configured.
        yield return StartCoroutine(ShowStoryHint());

        // Fade the whole overlay (black + text + briefing + hint together) away, revealing gameplay.
        overlayGroup.interactable = false;
        overlayGroup.blocksRaycasts = false;

        float timer = 0f;

        while (timer < finalFadeOutDuration)
        {
            timer += pauseGameplayDuringIntro ? Time.unscaledDeltaTime : Time.deltaTime;
            overlayGroup.alpha = Mathf.Lerp(1f, 0f, timer / finalFadeOutDuration);
            yield return null;
        }

        overlayGroup.alpha = 0f;
        overlayGroup.gameObject.SetActive(false);

        // Gameplay is now fully revealed - start this day's background music, if any.
        if (backgroundMusic != null && MusicManager.Instance != null)
        {
            MusicManager.Instance.PlayMusic(backgroundMusic);
        }

        // Intro is over - let Escape/the pause button work again.
        if (PauseManager.Instance != null)
        {
            PauseManager.Instance.allowPause = true;
        }

        if (pauseGameplayDuringIntro)
        {
            Time.timeScale = 1f;
        }
    }

    private IEnumerator ShowBriefing()
    {
        // Nothing configured for this day (panel not assigned, or no lines) - skip entirely.
        if (briefingPanel == null || briefingLines == null || briefingLines.Length == 0)
        {
            yield break;
        }

        if (briefingTitleText != null)
        {
            briefingTitleText.text = briefingTitle;
        }

        if (briefingText != null)
        {
            briefingText.text = string.Join("\n", briefingLines);
        }

        briefingPanel.gameObject.SetActive(true);
        briefingPanel.alpha = 0f;

        float fadeTimer = 0f;

        while (fadeTimer < briefingFadeInDuration)
        {
            fadeTimer += pauseGameplayDuringIntro ? Time.unscaledDeltaTime : Time.deltaTime;
            briefingPanel.alpha = Mathf.Clamp01(fadeTimer / briefingFadeInDuration);
            yield return null;
        }

        briefingPanel.alpha = 1f;

        if (briefingWaitForInput)
        {
            // Reuses the same advance input as dialogue lines (Space / mouse click by default).
            waitingForInput = true;

            while (waitingForInput)
            {
                yield return null;
            }
        }
        else
        {
            yield return WaitRealtimeOrScaled(briefingDisplayDuration);
        }

        briefingPanel.gameObject.SetActive(false);
    }

    private IEnumerator ShowStoryHint()
    {
        // Nothing configured for this day (panel not assigned, or no lines) - skip entirely.
        if (storyHintPanel == null || storyHintLines == null || storyHintLines.Length == 0)
        {
            yield break;
        }

        if (storyHintTitleText != null)
        {
            storyHintTitleText.text = storyHintTitle;
        }

        if (storyHintText != null)
        {
            storyHintText.text = string.Join("\n", storyHintLines);
        }

        storyHintPanel.gameObject.SetActive(true);
        storyHintPanel.alpha = 0f;

        float fadeTimer = 0f;

        while (fadeTimer < storyHintFadeInDuration)
        {
            fadeTimer += pauseGameplayDuringIntro ? Time.unscaledDeltaTime : Time.deltaTime;
            storyHintPanel.alpha = Mathf.Clamp01(fadeTimer / storyHintFadeInDuration);
            yield return null;
        }

        storyHintPanel.alpha = 1f;

        if (storyHintWaitForInput)
        {
            // Reuses the same advance input as dialogue lines (Space / mouse click by default).
            waitingForInput = true;

            while (waitingForInput)
            {
                yield return null;
            }
        }
        else
        {
            yield return WaitRealtimeOrScaled(storyHintDisplayDuration);
        }

        storyHintPanel.gameObject.SetActive(false);
    }

    private IEnumerator WaitRealtimeOrScaled(float seconds)
    {
        if (pauseGameplayDuringIntro)
        {
            yield return new WaitForSecondsRealtime(seconds);
        }
        else
        {
            yield return new WaitForSeconds(seconds);
        }
    }
}