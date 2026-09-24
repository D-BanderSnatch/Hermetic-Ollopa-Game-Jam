using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CutSceneFadeIn : MonoBehaviour
{
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 1.0f;

    private void Start()
    {
        // Ensure the image starts fully opaque (black screen)
        SetImageAlpha(1f);
        
        // Start fading into the scene
        StartCoroutine(FadeInRoutine());
    }

    private IEnumerator FadeInRoutine()
    {
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            // Interpolate alpha from 1 (opaque) to 0 (transparent)
            float alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            SetImageAlpha(alpha);
            yield return null; // Wait for the next frame
        }

        // Deactivate the image object so it doesn't consume performance
        fadeImage.gameObject.SetActive(false);
    }

    private void SetImageAlpha(float alpha)
    {
        if (fadeImage != null)
        {
            Color color = fadeImage.color;
            color.a = alpha;
            fadeImage.color = color;
        }
    }
}
