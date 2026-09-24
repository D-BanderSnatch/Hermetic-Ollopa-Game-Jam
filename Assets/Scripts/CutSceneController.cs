using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Plays a sequence of still images as a cutscene, with fade transitions
/// and player-controlled advancement (click / key press), or auto-advance on a timer.
///
/// SETUP IN UNITY:
/// 1. Create a Canvas (Screen Space - Overlay works well).
/// 2. Inside the Canvas, add a UI > Image called "CutsceneImage", stretched to fill the screen.
/// 3. Add a CanvasGroup component to that same Image object (used for fading).
/// 4. Create an empty GameObject, add this script to it.
/// 5. Drag the Image's CanvasGroup into "Display Group" and the Image component into "Display Image".
/// 6. Drag your 5 sprites into the "Slides" array in the order you want them shown.
/// 7. Optionally hook up "On Cutscene Finished" in the Inspector to load the next scene, etc.
/// </summary>
public class CutsceneController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("CanvasGroup on the same object as the Image, used for fading.")]
    public CanvasGroup displayGroup;

    [Tooltip("The UI Image component that shows the current slide.")]
    public Image displayImage;

    [Header("Audio")]
    [Tooltip("The cutscene's AudioSource. UNCHECK 'Play On Awake' on this AudioSource in the Inspector - " +
             "this script calls Play() explicitly instead, so audio always starts in sync with the visuals " +
             "regardless of scene-load/fade delays before this script's Start() actually runs.")]
    public AudioSource cutsceneAudioSource;

    [Header("Text Overlay")]
    [Tooltip("Optional TMP_Text shown alongside the image. If Independent Text Fade (below) is OFF, this should " +
             "be a CHILD of the same object as Display Group, so it fades in/out automatically along with the " +
             "image - no separate alpha handling needed. If Independent Text Fade is ON, this should instead be " +
             "a child of Text Display Group so it can fade on its own timing.")]
    public TMPro.TMP_Text displayText;

    [Tooltip("Optional per-slide text. Index matches the Slides array. Leave an entry blank for a slide with no text. " +
             "Can be used alone (e.g. a black/blank slide with just text) or alongside an image.")]
    public string[] slideTexts;

    [Tooltip("Only relevant if using the Dissolve transition with text: TMP_Text on the same object as Secondary Display Group.")]
    public TMPro.TMP_Text secondaryDisplayText;

    [Tooltip("If true, Display Text fades in and out on its own timing (see below) instead of just following the " +
             "image's alpha. Requires Text Display Group to be assigned. Useful for letting text linger after the " +
             "image cuts/glitches/dissolves, or for staggering text in after the image has already appeared.")]
    public bool independentTextFade = false;

    [Tooltip("CanvasGroup on the same object as Display Text (or a parent of it). Should start at alpha 0 in the " +
             "scene. Only used if Independent Text Fade is true. Do NOT make this a child of Display Group in that " +
             "case, or the text will end up fading twice.")]
    public CanvasGroup textDisplayGroup;

    [Tooltip("How long the independent text fade in/out takes, in seconds. Only used if Independent Text Fade is true.")]
    public float textFadeDuration = 0.5f;

    [Tooltip("Delay after a new slide's text is ready to appear before it actually starts fading in, in seconds. " +
             "Lets the image/transition settle first. Only used if Independent Text Fade is true.")]
    public float textFadeInDelay = 0f;

    [Header("White Flash Transition")]
    [Tooltip("Optional per-slide flag: if true, the transition FROM that slide TO the next one flashes to solid " +
             "white and back, instead of fade/cut/glitch/dissolve. Index matches the Slides array. Priority order " +
             "when multiple flags are set on the same index: Glitch > Dissolve > White Flash > Instant Cut > Fade.")]
    public bool[] flashWhiteToNext;

    [Tooltip("A full-screen white Image's CanvasGroup, used for the flash. Should start at alpha 0.")]
    public CanvasGroup whiteFlashGroup;

    [Tooltip("How long the fade TO white and the fade back FROM white each take, in seconds (so total flash time is roughly double this, plus the hold).")]
    public float flashFadeDuration = 0.15f;

    [Tooltip("How long to hold at full white before revealing the next slide, in seconds.")]
    public float flashHoldDuration = 0.2f;

    [Tooltip("Optional per-slide override for flash fade duration. Index matches the Slides array. Leave at 0 to use the shared value above.")]
    public float[] flashFadeDurations;

    [Tooltip("Optional per-slide override for flash hold duration. Index matches the Slides array. Leave at 0 to use the shared value above.")]
    public float[] flashHoldDurations;

    [Header("Dissolve Transition")]
    [Tooltip("A second Image, positioned exactly on top of Display Image (same size/position), used to crossfade " +
             "between slides. Its sibling order should be ABOVE Display Image in the Hierarchy so it renders on top.")]
    public Image secondaryDisplayImage;

    [Tooltip("CanvasGroup on the same object as Secondary Display Image.")]
    public CanvasGroup secondaryDisplayGroup;

    [Tooltip("Optional per-slide flag: if true, the transition FROM that slide TO the next one is a dissolve " +
             "(crossfade) instead of a fade-through-black, cut, or glitch. Index matches the Slides array. " +
             "If Glitch Transition To Next is also true for the same index, glitch takes priority over dissolve.")]
    public bool[] dissolveTransitionToNext;

    [Tooltip("How long the dissolve crossfade lasts, in seconds. Used as the default when no per-slide override is set below.")]
    public float dissolveDuration = 0.75f;

    [Tooltip("Optional per-slide override for dissolve duration, in seconds. Index matches the Slides array " +
             "(only relevant on indices where Dissolve Transition To Next is checked). Leave an entry at 0 " +
             "(or leave the array shorter than Slides) to fall back to the shared Dissolve Duration above.")]
    public float[] dissolveDurations;

    [Header("Vignette Dissolve")]
    [Tooltip("The Global Volume in the scene whose Vignette should gradually reduce/disappear during this cutscene.")]
    public UnityEngine.Rendering.Volume vignetteVolume;

    [Tooltip("The slide index (0-based, matches Slides array) at which the vignette starts dissolving. Set to -1 to disable this feature entirely.")]
    public int vignetteDissolveOnSlideIndex = -1;

    [Tooltip("How long the vignette takes to fade from its starting intensity down to the end intensity, in seconds.")]
    public float vignetteDissolveDuration = 2f;

    [Tooltip("Intensity to fade the vignette DOWN TO. 0 = fully gone.")]
    public float vignetteEndIntensity = 0f;

    [Tooltip("If true, forces the vignette to a specific starting intensity right before the dissolve begins, instead of using whatever it's currently set to.")]
    public bool overrideVignetteStartIntensity = false;

    [Tooltip("Only used if Override Vignette Start Intensity is checked.")]
    public float vignetteStartIntensity = 0.4f;

    private bool vignetteDissolveTriggered = false;

    [Header("Slides")]
    [Tooltip("Your 5 (or however many) cutscene images, in order.")]
    public Sprite[] slides;

    [Header("Timing")]
    [Tooltip("How long a fade in/out takes, in seconds.")]
    public float fadeDuration = 0.5f;

    [Tooltip("If true, slides auto-advance after 'autoAdvanceDelay' seconds. If false, player must click/press a key.")]
    public bool autoAdvance = false;

    [Tooltip("Seconds to hold on each slide before auto-advancing (only used if autoAdvance is true).")]
    public float autoAdvanceDelay = 3f;

    [Tooltip("Optional per-slide override for how long to hold, in seconds (only used if autoAdvance is true). " +
             "Index matches the Slides array. Leave an entry at 0 (or leave the array shorter than Slides) " +
             "to fall back to autoAdvanceDelay for that slide.")]
    public float[] slideDurations;

    [Tooltip("Optional per-slide flag: if true, the transition FROM that slide TO the next one is an instant " +
             "cut with no fade at all, instead of the normal fade-out/fade-in. Index matches the Slides array " +
             "(e.g. index 3 = 'cut instead of fade when going from slide 4 to slide 5').")]
    public bool[] instantCutToNext;

    [Header("Glitch Transition")]
    [Tooltip("Optional per-slide flag: if true, the transition FROM that slide TO the next one plays a glitch " +
             "effect (jitter + flicker) instead of a fade or cut. Index matches the Slides array. If both this " +
             "and Instant Cut To Next are true for the same index, glitch takes priority.")]
    public bool[] glitchTransitionToNext;

    [Tooltip("How long the glitch effect lasts, in seconds. Used as the default when no per-slide override is set below.")]
    public float glitchDuration = 0.3f;

    [Tooltip("Optional per-slide override for glitch duration, in seconds. Index matches the Slides array " +
             "(only relevant on indices where Glitch Transition To Next is checked). Leave an entry at 0 " +
             "(or leave the array shorter than Slides) to fall back to the shared Glitch Duration above.")]
    public float[] glitchDurations;

    [Tooltip("Max random position offset during the glitch, in pixels.")]
    public float glitchPositionOffset = 15f;

    [Tooltip("How many times the image randomly jumps/flickers during the glitch.")]
    public int glitchFlickerCount = 8;

    [Tooltip("Optional static/noise sprite briefly flashed in during the glitch, for extra effect. Leave empty to skip.")]
    public Sprite glitchNoiseSprite;

    [Header("Input (only used if autoAdvance is false)")]
    [Tooltip("Key that advances the slide. Uses the new Input System's Key enum.")]
    public Key advanceKey = Key.Space;
    public bool advanceOnMouseClick = true;

    [Header("Ending")]
    [Tooltip("How long to wait, in seconds, after the final fade-out before loading the next scene / firing the finished event.")]
    public float endDelay = 1f;

    [Tooltip("Name of the scene to load when the cutscene ends. Leave blank to skip scene loading (e.g. if you just want to use the event below instead).")]
    public string nextSceneName;

    [Header("Events")]
    [Tooltip("Fires after the end delay, right before (or instead of) loading the next scene.")]
    public UnityEvent onCutsceneFinished;

    private int currentIndex = -1;
    private bool waitingForInput = false;
    private bool isTransitioning = false;
    private Coroutine textFadeCoroutine;

    private void Start()
    {
        if (slides == null || slides.Length == 0)
        {
            Debug.LogWarning("CutsceneController: No slides assigned.");
            return;
        }

        displayGroup.alpha = 0f;

        if (secondaryDisplayGroup != null)
        {
            secondaryDisplayGroup.alpha = 0f;
        }

        if (whiteFlashGroup != null)
        {
            whiteFlashGroup.alpha = 0f;
        }

        if (independentTextFade && textDisplayGroup != null)
        {
            textDisplayGroup.alpha = 0f;
        }

        if (cutsceneAudioSource != null)
        {
            cutsceneAudioSource.Play();
        }

        StartCoroutine(PlaySlide(0));
    }

    private void Update()
    {
        if (!waitingForInput) return;

        bool pressedKey = Keyboard.current != null && Keyboard.current[advanceKey].wasPressedThisFrame;
        bool clicked = advanceOnMouseClick && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;

        if (pressedKey || clicked)
        {
            waitingForInput = false;
            StartCoroutine(AdvanceToNextSlide());
        }
    }

    private IEnumerator PlaySlide(int index)
    {
        isTransitioning = true;
        currentIndex = index;
        displayImage.sprite = slides[index];
        SetDisplayText(index);

        yield return StartCoroutine(Fade(0f, 1f));

        isTransitioning = false;

        yield return StartCoroutine(AfterSlideShown());
    }

    private IEnumerator ShowSlideInstant(int index)
    {
        // Hard cut: alpha is already at 1 from the previous slide, so just swap
        // the sprite with no fade at all.
        isTransitioning = true;
        currentIndex = index;
        displayImage.sprite = slides[index];
        SetDisplayText(index);
        displayGroup.alpha = 1f;

        isTransitioning = false;

        yield return StartCoroutine(AfterSlideShown());
    }

    private string GetTextForSlide(int index)
    {
        if (slideTexts != null && index >= 0 && index < slideTexts.Length)
        {
            return slideTexts[index];
        }

        return string.Empty;
    }

    private void SetDisplayText(int index)
    {
        string text = GetTextForSlide(index);

        if (independentTextFade && textDisplayGroup != null)
        {
            if (textFadeCoroutine != null)
            {
                StopCoroutine(textFadeCoroutine);
            }

            textFadeCoroutine = StartCoroutine(FadeTextToSlide(text));
        }
        else if (displayText != null)
        {
            displayText.text = text;
        }
    }

    /// <summary>
    /// Fades Display Text out (if currently visible), swaps in the new slide's text, waits
    /// Text Fade In Delay, then fades it back in. If the new text is blank, it's left hidden.
    /// Runs independently of whatever image transition is happening at the same time.
    /// </summary>
    private IEnumerator FadeTextToSlide(string newText)
    {
        if (textDisplayGroup.alpha > 0f)
        {
            yield return StartCoroutine(FadeCanvasGroup(textDisplayGroup, textDisplayGroup.alpha, 0f, textFadeDuration));
        }

        if (displayText != null)
        {
            displayText.text = newText;
        }

        if (string.IsNullOrEmpty(newText))
        {
            // Nothing to show on this slide - leave the text hidden.
            yield break;
        }

        if (textFadeInDelay > 0f)
        {
            yield return new WaitForSeconds(textFadeInDelay);
        }

        yield return StartCoroutine(FadeCanvasGroup(textDisplayGroup, 0f, 1f, textFadeDuration));
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
    {
        float elapsed = 0f;
        group.alpha = from;

        if (duration <= 0f)
        {
            group.alpha = to;
            yield break;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        group.alpha = to;
    }

    private IEnumerator AfterSlideShown()
    {
        CheckVignetteDissolveTrigger();

        if (autoAdvance)
        {
            yield return new WaitForSeconds(GetDurationForSlide(currentIndex));
            StartCoroutine(AdvanceToNextSlide());
        }
        else
        {
            waitingForInput = true;
        }
    }

    private void CheckVignetteDissolveTrigger()
    {
        if (vignetteDissolveTriggered)
        {
            return;
        }

        if (vignetteDissolveOnSlideIndex < 0 || currentIndex != vignetteDissolveOnSlideIndex)
        {
            return;
        }

        if (vignetteVolume == null || vignetteVolume.profile == null)
        {
            Debug.LogWarning("CutsceneController: Vignette dissolve is set up, but Vignette Volume is not assigned.");
            return;
        }

        if (!vignetteVolume.profile.TryGet(out Vignette vignette))
        {
            Debug.LogWarning("CutsceneController: The assigned Volume's profile has no Vignette override.");
            return;
        }

        vignetteDissolveTriggered = true;
        StartCoroutine(DissolveVignette(vignette));
    }

    private IEnumerator DissolveVignette(Vignette vignette)
    {
        if (overrideVignetteStartIntensity)
        {
            vignette.intensity.value = vignetteStartIntensity;
        }

        float startIntensity = vignette.intensity.value;
        float timer = 0f;

        while (timer < vignetteDissolveDuration)
        {
            timer += Time.deltaTime;
            vignette.intensity.value = Mathf.Lerp(startIntensity, vignetteEndIntensity, timer / vignetteDissolveDuration);
            yield return null;
        }

        vignette.intensity.value = vignetteEndIntensity;
    }

    private float GetDurationForSlide(int index)
    {
        if (slideDurations != null && index >= 0 && index < slideDurations.Length && slideDurations[index] > 0f)
        {
            return slideDurations[index];
        }

        return autoAdvanceDelay;
    }

    private bool ShouldCutInstantly(int index)
    {
        return instantCutToNext != null && index >= 0 && index < instantCutToNext.Length && instantCutToNext[index];
    }

    private bool ShouldGlitch(int index)
    {
        return glitchTransitionToNext != null && index >= 0 && index < glitchTransitionToNext.Length && glitchTransitionToNext[index];
    }

    private float GetGlitchDurationForSlide(int index)
    {
        if (glitchDurations != null && index >= 0 && index < glitchDurations.Length && glitchDurations[index] > 0f)
        {
            return glitchDurations[index];
        }

        return glitchDuration;
    }

    private IEnumerator GlitchToSlide(int nextIndex)
    {
        isTransitioning = true;

        RectTransform rect = displayImage.rectTransform;
        Vector2 originalPosition = rect.anchoredPosition;
        Sprite currentSprite = displayImage.sprite;
        Sprite nextSprite = slides[nextIndex];

        // Switch to the next slide roughly halfway through the flicker count,
        // so the glitch reads as "tearing between" the two images rather than
        // just shaking the old one and then cutting clean to the new one.
        int switchAtFlicker = glitchFlickerCount / 2;
        float thisGlitchDuration = GetGlitchDurationForSlide(currentIndex);
        float perFlickerDelay = glitchFlickerCount > 0 ? thisGlitchDuration / glitchFlickerCount : thisGlitchDuration;

        for (int i = 0; i < glitchFlickerCount; i++)
        {
            // Random jitter offset.
            rect.anchoredPosition = originalPosition + new Vector2(
                UnityEngine.Random.Range(-glitchPositionOffset, glitchPositionOffset),
                UnityEngine.Random.Range(-glitchPositionOffset, glitchPositionOffset)
            );

            // Occasionally flash the noise sprite instead of the real image, if one was provided.
            if (glitchNoiseSprite != null && UnityEngine.Random.value < 0.35f)
            {
                displayImage.sprite = glitchNoiseSprite;
            }
            else
            {
                displayImage.sprite = (i >= switchAtFlicker) ? nextSprite : currentSprite;
            }

            // Occasional alpha flicker for extra instability.
            displayGroup.alpha = UnityEngine.Random.value < 0.2f
                ? UnityEngine.Random.Range(0.6f, 1f)
                : 1f;

            yield return new WaitForSeconds(perFlickerDelay);
        }

        // Settle cleanly on the next slide with everything reset.
        rect.anchoredPosition = originalPosition;
        displayGroup.alpha = 1f;
        displayImage.sprite = nextSprite;
        SetDisplayText(nextIndex);
        currentIndex = nextIndex;

        isTransitioning = false;

        yield return StartCoroutine(AfterSlideShown());
    }

    private bool ShouldDissolve(int index)
    {
        return dissolveTransitionToNext != null && index >= 0 && index < dissolveTransitionToNext.Length && dissolveTransitionToNext[index];
    }

    private float GetDissolveDurationForSlide(int index)
    {
        if (dissolveDurations != null && index >= 0 && index < dissolveDurations.Length && dissolveDurations[index] > 0f)
        {
            return dissolveDurations[index];
        }

        return dissolveDuration;
    }

    private IEnumerator DissolveToSlide(int nextIndex)
    {
        isTransitioning = true;

        if (secondaryDisplayImage == null || secondaryDisplayGroup == null)
        {
            Debug.LogWarning("CutsceneController: Dissolve requested but Secondary Display Image/Group is not assigned. Falling back to a normal fade.");

            yield return StartCoroutine(Fade(1f, 0f));
            isTransitioning = false;
            yield return StartCoroutine(PlaySlide(nextIndex));
            yield break;
        }

        float thisDissolveDuration = GetDissolveDurationForSlide(currentIndex);

        // Put the next slide on the overlay layer, fully transparent, on top of the current one.
        secondaryDisplayImage.sprite = slides[nextIndex];
        secondaryDisplayGroup.alpha = 0f;

        if (secondaryDisplayText != null)
        {
            secondaryDisplayText.text = GetTextForSlide(nextIndex);
        }

        float timer = 0f;

        while (timer < thisDissolveDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / thisDissolveDuration;

            displayGroup.alpha = Mathf.Lerp(1f, 0f, progress);
            secondaryDisplayGroup.alpha = Mathf.Lerp(0f, 1f, progress);

            yield return null;
        }

        // Settle: move the now-current slide onto the base layer, reset the overlay for next time.
        displayImage.sprite = slides[nextIndex];
        SetDisplayText(nextIndex);
        displayGroup.alpha = 1f;
        secondaryDisplayGroup.alpha = 0f;

        currentIndex = nextIndex;
        isTransitioning = false;

        yield return StartCoroutine(AfterSlideShown());
    }

    private bool ShouldFlashWhite(int index)
    {
        return flashWhiteToNext != null && index >= 0 && index < flashWhiteToNext.Length && flashWhiteToNext[index];
    }

    private float GetFlashFadeDurationForSlide(int index)
    {
        if (flashFadeDurations != null && index >= 0 && index < flashFadeDurations.Length && flashFadeDurations[index] > 0f)
        {
            return flashFadeDurations[index];
        }

        return flashFadeDuration;
    }

    private float GetFlashHoldDurationForSlide(int index)
    {
        if (flashHoldDurations != null && index >= 0 && index < flashHoldDurations.Length && flashHoldDurations[index] > 0f)
        {
            return flashHoldDurations[index];
        }

        return flashHoldDuration;
    }

    private IEnumerator FlashWhiteToSlide(int nextIndex)
    {
        isTransitioning = true;

        if (whiteFlashGroup == null)
        {
            Debug.LogWarning("CutsceneController: White Flash requested but White Flash Group is not assigned. Falling back to a normal fade.");

            yield return StartCoroutine(Fade(1f, 0f));
            isTransitioning = false;
            yield return StartCoroutine(PlaySlide(nextIndex));
            yield break;
        }

        float thisFadeDuration = GetFlashFadeDurationForSlide(currentIndex);
        float thisHoldDuration = GetFlashHoldDurationForSlide(currentIndex);

        // Fade the screen to solid white, covering the current slide.
        float timer = 0f;
        while (timer < thisFadeDuration)
        {
            timer += Time.deltaTime;
            whiteFlashGroup.alpha = Mathf.Lerp(0f, 1f, timer / thisFadeDuration);
            yield return null;
        }
        whiteFlashGroup.alpha = 1f;

        // Swap to the next slide while hidden behind the white.
        displayImage.sprite = slides[nextIndex];
        SetDisplayText(nextIndex);
        displayGroup.alpha = 1f;

        if (thisHoldDuration > 0f)
        {
            yield return new WaitForSeconds(thisHoldDuration);
        }

        // Fade the white away, revealing the next slide.
        timer = 0f;
        while (timer < thisFadeDuration)
        {
            timer += Time.deltaTime;
            whiteFlashGroup.alpha = Mathf.Lerp(1f, 0f, timer / thisFadeDuration);
            yield return null;
        }
        whiteFlashGroup.alpha = 0f;

        currentIndex = nextIndex;
        isTransitioning = false;

        yield return StartCoroutine(AfterSlideShown());
    }

    private IEnumerator AdvanceToNextSlide()
    {
        if (isTransitioning) yield break;
        isTransitioning = true;

        bool glitch = ShouldGlitch(currentIndex);
        bool dissolve = !glitch && ShouldDissolve(currentIndex);
        bool flashWhite = !glitch && !dissolve && ShouldFlashWhite(currentIndex);
        bool cutInstantly = !glitch && !dissolve && !flashWhite && ShouldCutInstantly(currentIndex);

        if (!glitch && !dissolve && !flashWhite && !cutInstantly)
        {
            yield return StartCoroutine(Fade(1f, 0f));
        }

        int nextIndex = currentIndex + 1;
        if (nextIndex >= slides.Length)
        {
            yield return new WaitForSeconds(endDelay);

            isTransitioning = false;
            onCutsceneFinished?.Invoke();

            if (!string.IsNullOrEmpty(nextSceneName))
            {
                if (SceneTransitionManager.Instance != null)
                {
                    SceneTransitionManager.Instance.StartTransition(nextSceneName);
                }
                else
                {
                    Debug.LogWarning("CutsceneController: No SceneTransitionManager found, loading directly.");
                    SceneManager.LoadScene(nextSceneName);
                }
            }

            yield break;
        }

        isTransitioning = false;

        if (glitch)
        {
            StartCoroutine(GlitchToSlide(nextIndex));
        }
        else if (dissolve)
        {
            StartCoroutine(DissolveToSlide(nextIndex));
        }
        else if (flashWhite)
        {
            StartCoroutine(FlashWhiteToSlide(nextIndex));
        }
        else if (cutInstantly)
        {
            StartCoroutine(ShowSlideInstant(nextIndex));
        }
        else
        {
            StartCoroutine(PlaySlide(nextIndex));
        }
    }

    private IEnumerator Fade(float from, float to)
    {
        float elapsed = 0f;
        displayGroup.alpha = from;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            displayGroup.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            yield return null;
        }

        displayGroup.alpha = to;
    }

    /// <summary>
    /// Call this publicly (e.g. from a "Skip" button) to jump straight to the end.
    /// </summary>
    public void SkipCutscene()
    {
        StopAllCoroutines();
        displayGroup.alpha = 0f;
        onCutsceneFinished?.Invoke();
    }
}