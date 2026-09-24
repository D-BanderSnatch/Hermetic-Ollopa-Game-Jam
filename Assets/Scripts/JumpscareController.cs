using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives a short, hard-cut jumpscare + game-over sequence, entirely within a
/// Screen Space - Overlay Canvas. No camera involved — the "shake" is done by
/// jittering the panel's own RectTransform.
/// Attach to the JumpscarePanel GameObject. Wire up references in the Inspector.
/// Call TriggerJumpscare() from wherever your game logic decides the scare should fire.
/// </summary>
public class JumpscareController : MonoBehaviour
{
    [Header("Core References")]
    public RectTransform panelRect;           // the JumpscarePanel's own RectTransform
    public Image monsterImage;                // MonsterImage's Image component
    public AudioSource ambientSource;         // your normal looping ambience
    public AudioSource stingSource;           // dedicated source for the scare sting
    public AudioClip screamSting;             // the sharp audio cue

    [Header("Optional Screen Flash")]
    public Image flashOverlay;                // full-screen UI Image, alpha 0 by default, color white or red

    [Header("Timing (seconds)")]
    public float silenceBeforeDuration = 0.3f;
    public float flashDuration = 0.06f;       // ~1-2 frames at 30fps
    public float jitterDuration = 0.25f;      // jagged jitter phase, right after the cut
    public float holdDuration = 0.8f;         // still, unmoving hold before fading to black
    public float fadeToBlackDuration = 1.2f;
    public float jitterMagnitude = 15f;       // in pixels, since this is UI space now

    [Header("Game Over")]
    public Image blackFadeOverlay;            // separate full-screen black Image, alpha 0 by default
    public MonoBehaviour[] playerControlScripts; // scripts to disable the instant the scare fires
    public System.Action onGameOver;          // hook your Game Over screen/scene load here

    private Vector2 originalAnchoredPos;
    private bool isPlaying = false;

    public void TriggerJumpscare()
    {
        if (isPlaying) return;
        StartCoroutine(JumpscareSequence());
    }

    private IEnumerator JumpscareSequence()
    {
        isPlaying = true;

        // 0. Lock player input immediately — no lingering control during the death beat
        foreach (var script in playerControlScripts)
            if (script != null) script.enabled = false;

        // 1. Silence / audio dip
        float startVolume = ambientSource != null ? ambientSource.volume : 0f;
        if (ambientSource != null)
        {
            float t = 0f;
            while (t < silenceBeforeDuration)
            {
                t += Time.deltaTime;
                ambientSource.volume = Mathf.Lerp(startVolume, 0f, t / silenceBeforeDuration);
                yield return null;
            }
            ambientSource.volume = 0f;
        }
        else
        {
            yield return new WaitForSeconds(silenceBeforeDuration);
        }

        // 2. Screen flash (fires same frame the sprite appears)
        if (flashOverlay != null)
            StartCoroutine(FlashScreen());

        // 3. Hard cut: monster panel appears
        originalAnchoredPos = panelRect.anchoredPosition;

        monsterImage.gameObject.SetActive(false); // ensure it was "absent"
        yield return null;                          // wait exactly one frame
        monsterImage.gameObject.SetActive(true);
        panelRect.anchoredPosition = originalAnchoredPos;

        // 4. Frame-synced audio sting
        if (stingSource != null && screamSting != null)
            stingSource.PlayOneShot(screamSting);

        // 5. Jitter the panel in jagged single-frame jumps (not smooth bobbing)
        // This replaces camera shake — since we're Screen Space Overlay, shaking
        // the panel's own RectTransform is what reads as a "shake" here.
        float jitterElapsed = 0f;
        while (jitterElapsed < jitterDuration)
        {
            Vector2 jitter = new Vector2(
                Random.Range(-jitterMagnitude, jitterMagnitude),
                Random.Range(-jitterMagnitude, jitterMagnitude)
            );
            panelRect.anchoredPosition = originalAnchoredPos + jitter;
            jitterElapsed += Time.deltaTime;
            yield return null; // one raw frame per jump, no interpolation
        }

        // 6. Go still — no more jitter. This is the beat that sells finality.
        panelRect.anchoredPosition = originalAnchoredPos;
        yield return new WaitForSeconds(holdDuration);

        // 7. Fade to black over the held, silent image
        if (blackFadeOverlay != null)
        {
            float t = 0f;
            Color c = blackFadeOverlay.color;
            while (t < fadeToBlackDuration)
            {
                t += Time.deltaTime;
                c.a = Mathf.Lerp(0f, 1f, t / fadeToBlackDuration);
                blackFadeOverlay.color = c;
                yield return null;
            }
            c.a = 1f;
            blackFadeOverlay.color = c;
        }

        // Ambience stays out deliberately — do not restore it. Silence is the point.

        isPlaying = false;

        // 8. Hand off to the Game Over flow
        onGameOver?.Invoke();
    }

    private IEnumerator FlashScreen()
    {
        flashOverlay.color = new Color(flashOverlay.color.r, flashOverlay.color.g, flashOverlay.color.b, 1f);
        yield return new WaitForSeconds(flashDuration);
        flashOverlay.color = new Color(flashOverlay.color.r, flashOverlay.color.g, flashOverlay.color.b, 0f);
    }
}