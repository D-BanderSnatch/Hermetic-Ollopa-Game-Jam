using UnityEngine;
using System.Collections;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    [Header("Audio Source")]
    [Tooltip("AudioSource that will actually play the music. Set Loop = true, Play On Awake = false.")]
    public AudioSource musicSource;

    [Header("Default Track (optional)")]
    public AudioClip defaultTrack;

    [Header("Fade Settings")]
    public float fadeDuration = 1.5f;

    void Awake()
    {
        // Standard persistent singleton - survives scene loads, only one ever exists.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Call this from wherever your intro/panel finishes, e.g.
    // MusicManager.Instance.PlayMusic(backgroundTrack);
    //
    // loop: whether this track should repeat once it finishes. Leave true for
    // normal background/day music. Pass false for one-shot stings (like a
    // victory jingle) so they don't loop forever and bleed into the next scene.
    public void PlayMusic(AudioClip clip, bool fadeIn = true, bool loop = true)
    {
        if (musicSource == null || clip == null)
        {
            Debug.LogWarning("MusicManager: missing musicSource or clip.");
            return;
        }

        // Already playing this exact track - don't restart it.
        if (musicSource.clip == clip && musicSource.isPlaying)
        {
            return;
        }

        StopAllCoroutines();
        musicSource.loop = loop;

        if (fadeIn)
        {
            StartCoroutine(FadeInRoutine(clip));
        }
        else
        {
            musicSource.clip = clip;
            musicSource.volume = 1f;
            musicSource.Play();
        }
    }

    public void StopMusic(bool fadeOut = true)
    {
        StopAllCoroutines();

        if (fadeOut)
        {
            StartCoroutine(FadeOutRoutine());
        }
        else
        {
            musicSource.Stop();
        }
    }

    IEnumerator FadeInRoutine(AudioClip clip)
    {
        musicSource.clip = clip;
        musicSource.volume = 0f;
        musicSource.Play();

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            musicSource.volume = Mathf.Clamp01(t / fadeDuration);
            yield return null;
        }

        musicSource.volume = 1f;
    }

    IEnumerator FadeOutRoutine()
    {
        float startVolume = musicSource.volume;
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, t / fadeDuration);
            yield return null;
        }

        musicSource.Stop();
        musicSource.volume = startVolume;
    }
}