using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Handles the Main Menu's own Start/Continue/Options/Quit buttons. Lives ONLY as a
/// normal object in the Main Menu scene - NOT a singleton, NOT DontDestroyOnLoad.
/// This is intentional: every time Main Menu is loaded (including returning to
/// it after Pause -> Menu from a Day scene), a fresh instance of this script
/// gets created and wires listeners onto that fresh scene's actual button
/// GameObjects. Scene loading/fading itself is delegated to the persistent
/// SceneTransitionManager singleton. Options and Quit behaviour is delegated
/// to OptionPopUpScript.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("UI Setup")]
    [SerializeField] private Button startButton;
    [SerializeField] private GameObject menuContainer;
    [SerializeField] private TMP_Text gametitle;

    [Header("Continue Feature")]
    [Tooltip("The Continue button in the Main Menu. Will be disabled/hidden automatically if there's no saved progress.")]
    [SerializeField] private Button continueButton;

    [Tooltip("If true, the Continue button is fully hidden (SetActive false) when there's no save. If false, it stays visible but non-interactable.")]
    [SerializeField] private bool hideContinueWhenNoSave = true;

    [Header("Options & Quit")]
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button quitButton;

    [Tooltip("The OptionPopUpScript that owns the options/quit modals. If left empty, the controller will try to find one in the loaded scenes.")]
    [SerializeField] private OptionPopUpScript optionPopUp;

    [Tooltip("If true, Quit opens the quit confirmation prompt. If false, it quits immediately.")]
    [SerializeField] private bool confirmBeforeQuit = true;

    [Header("Settings")]
    [SerializeField] private string targetSceneName = "Day1";

    private void Start()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(OnStartButtonPressed);
            startButton.onClick.AddListener(OnStartButtonPressed);
        }

        if (optionsButton != null)
        {
            optionsButton.onClick.RemoveListener(OnOptionsButtonPressed);
            optionsButton.onClick.AddListener(OnOptionsButtonPressed);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(OnQuitButtonPressed);
            quitButton.onClick.AddListener(OnQuitButtonPressed);
        }

        SetUpContinueButton();
    }

    private void OnDestroy()
    {
        // Safety net so no stale listeners survive if the buttons outlive this object.
        if (startButton != null) startButton.onClick.RemoveListener(OnStartButtonPressed);
        if (continueButton != null) continueButton.onClick.RemoveListener(OnContinueButtonPressed);
        if (optionsButton != null) optionsButton.onClick.RemoveListener(OnOptionsButtonPressed);
        if (quitButton != null) quitButton.onClick.RemoveListener(OnQuitButtonPressed);
    }

    private void SetUpContinueButton()
    {
        if (continueButton == null)
        {
            return;
        }

        continueButton.onClick.RemoveListener(OnContinueButtonPressed);
        continueButton.onClick.AddListener(OnContinueButtonPressed);

        bool hasSave = GameProgress.HasContinueSave();

        if (hideContinueWhenNoSave)
        {
            continueButton.gameObject.SetActive(hasSave);
        }
        else
        {
            continueButton.interactable = hasSave;
        }
    }

    private void OnStartButtonPressed()
    {
        if (startButton != null) startButton.interactable = false;

        HideMenu();

        // Starting a brand new game clears any old save, so a fresh playthrough
        // doesn't leave a stale Continue button pointing at old progress.
        GameProgress.ClearContinueSave();

        LoadScene(targetSceneName);
    }

    private void OnContinueButtonPressed()
    {
        if (!GameProgress.HasContinueSave())
        {
            Debug.LogWarning("MainMenuController: Continue pressed but no save exists.");
            return;
        }

        string savedScene = GameProgress.GetContinueScene();
        Debug.Log($"MainMenuController: Continue pressed. Saved scene = '{savedScene}'");

        // Validate BEFORE hiding the menu, so a bad save doesn't leave the player on an empty screen.
        if (string.IsNullOrEmpty(savedScene) || !Application.CanStreamedLevelBeLoaded(savedScene))
        {
            Debug.LogError($"MainMenuController: Saved scene '{savedScene}' is empty or not in Build Settings. " +
                           "Check what GameProgress is saving and that the scene name matches exactly.");
            return;
        }

        if (continueButton != null) continueButton.interactable = false;
        if (startButton != null) startButton.interactable = false;

        HideMenu();

        LoadScene(savedScene);
    }

    // Only accepts an OptionPopUpScript that lives in THIS scene. A popup on a
    // DontDestroyOnLoad object would hold references to destroyed modals after
    // the Main menu is reloaded, so it is deliberately rejected.
    private OptionPopUpScript ResolvePopUp()
    {
        if (optionPopUp != null && optionPopUp.gameObject.scene == gameObject.scene)
        {
            return optionPopUp;
        }

        OptionPopUpScript[] all = FindObjectsByType<OptionPopUpScript>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (OptionPopUpScript candidate in all)
        {
            if (candidate.gameObject.scene == gameObject.scene)
            {
                optionPopUp = candidate;
                return candidate;
            }
        }

        Debug.LogError("MainMenuController: No OptionPopUpScript found in the Main menu scene. " +
                       "One exists only on a DontDestroyOnLoad object, whose modal references go stale after a scene reload.");
        return null;
    }

    private void OnOptionsButtonPressed()
    {
        OptionPopUpScript popUp = ResolvePopUp();

        if (popUp == null)
        {
            return;
        }

        popUp.OpenOptionsMenu();
    }

    private void OnQuitButtonPressed()
    {
        OptionPopUpScript popUp = ResolvePopUp();

        if (popUp == null)
        {
            Debug.LogWarning("MainMenuController: Quitting directly because there is no quit prompt to show.");
            QuitDirectly();
            return;
        }

        if (confirmBeforeQuit)
        {
            popUp.OpenModal();
        }
        else
        {
            popUp.QuitGame();
        }
    }

    private void QuitDirectly()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void HideMenu()
    {
        if (menuContainer != null)
        {
            menuContainer.SetActive(false);
        }

        if (gametitle != null)
        {
            gametitle.gameObject.SetActive(false);
        }
    }

    private void LoadScene(string sceneName)
    {
        Debug.Log($"MainMenuController: Loading '{sceneName}' (SceneTransitionManager present: {SceneTransitionManager.Instance != null})");

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.StartTransition(sceneName);
        }
        else
        {
            Debug.LogWarning("MainMenuController: No SceneTransitionManager found, loading directly.");
            SceneManager.LoadScene(sceneName);
        }
    }
}