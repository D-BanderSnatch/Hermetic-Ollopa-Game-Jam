using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Persistent, cross-scene singleton that fades to black, loads a scene, then
/// fades back in. Call SceneTransitionManager.Instance.StartTransition(sceneName)
/// from anywhere.
///
/// IMPORTANT: this object (and its Fade Image) must live on its OWN dedicated
/// GameObject/Canvas, separate from your Main Menu's scene UI (buttons, menu
/// container, etc). Main-Menu-specific button wiring belongs in MainMenuController
/// instead, since that needs to be re-wired fresh every time Main Menu reloads -
/// which a persistent singleton's Start() cannot do (Start() only runs once, ever).
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    [Header("Fade Image")]
    [Tooltip("Full-screen black Image, on a child of THIS SAME persistent GameObject (not part of any scene's own Canvas), so it survives every scene change.")]
    [SerializeField] private Image fadeImage;

    [Header("Timing")]
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private float delayBeforeLoad = 1.5f;

    private static SceneTransitionManager instance;

    public static SceneTransitionManager Instance
    {
        get { return instance; }
    }

    private bool isTransitioning = false;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;

            // DontDestroyOnLoad only works on ROOT GameObjects (no parent).
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void Start()
    {
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
            fadeImage.raycastTarget = false;
        }
    }

    public void StartTransition(string sceneName)
    {
        if (isTransitioning)
        {
            Debug.Log("SceneTransitionManager: Already transitioning.");
            return;
        }

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("SceneTransitionManager: Scene name is empty!");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError(
                "SceneTransitionManager: Scene '" + sceneName +
                "' is not in Build Settings (File > Build Settings > Scenes In Build)."
            );
            return;
        }

        isTransitioning = true;

        StartCoroutine(FadeAndLoadScene(sceneName));
    }

    private IEnumerator FadeAndLoadScene(string sceneName)
    {
        if (fadeImage != null) fadeImage.raycastTarget = true;

        Color currentColor = fadeImage != null ? fadeImage.color : Color.black;

        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            currentColor.a = Mathf.Clamp01(elapsedTime / fadeDuration);
            if (fadeImage != null) fadeImage.color = currentColor;
            yield return null;
        }
        currentColor.a = 1f;
        if (fadeImage != null) fadeImage.color = currentColor;

        yield return new WaitForSecondsRealtime(delayBeforeLoad);

        // In case anything paused the game before the transition.
        Time.timeScale = 1f;

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (asyncLoad != null && !asyncLoad.isDone)
        {
            yield return null;
        }

        elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            currentColor.a = Mathf.Clamp01(1f - (elapsedTime / fadeDuration));
            if (fadeImage != null) fadeImage.color = currentColor;
            yield return null;
        }
        currentColor.a = 0f;
        if (fadeImage != null) fadeImage.color = currentColor;

        if (fadeImage != null) fadeImage.raycastTarget = false;

        isTransitioning = false;
    }
}