using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem; // requires the Input System package
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    [Header("Enable/Disable Pausing")]
    [Tooltip("If false, pausing is completely disabled in this scene (e.g. during cutscenes) - " +
             "Escape and the Pause Button below will both do nothing. Audio/video settings from " +
             "GameSettingsManager are unaffected either way, since that's a separate system.")]
    public bool allowPause = true;

    [Header("Assign the pause panel for THIS scene")]
    public GameObject pauseMenuUI;

    [Header("Assign the OptionPopUpScript that manages Options/Video/Audio/Quit panels")]
    public OptionPopUpScript optionPopUp;

    [Header("Optional: a UI Button that also toggles pause (in addition to Escape)")]
    [Tooltip("If assigned, clicking this button does the same thing as pressing Escape. Leave empty if you only want keyboard/Esc control.")]
    public Button pauseButton;

    [Header("Main Menu")]
    [Tooltip("Scene name of your Main Menu scene. Must match the exact scene name in Build Settings.")]
    public string mainMenuSceneName = "Main menu";

    [Tooltip("Extra UI objects to hide when the Menu button is clicked (e.g. HUD canvas, inventory, crosshair). " +
             "The pause panel, options modals, overlay and pause button are hidden automatically.")]
    public GameObject[] extraUIToHideOnMenu;

    public bool IsPaused { get; private set; }

    private bool isLeavingToMenu;

    void Awake()
    {
        // Local to this scene on purpose — not DontDestroyOnLoad.
        // Each scene has its own pause panel, so each scene gets its own instance.
        Instance = this;

        if (pauseButton != null)
        {
            pauseButton.onClick.RemoveListener(TogglePause);
            pauseButton.onClick.AddListener(TogglePause);
        }
    }

    void Update()
    {
        if (!allowPause)
        {
            return;
        }

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    /// <summary>
    /// Shared by Escape and the optional Pause Button. Does nothing if allowPause is false.
    /// </summary>
    public void TogglePause()
    {
        if (!allowPause)
        {
            return;
        }

        // If any options-related modal is open, let this close all of them
        // and resume, instead of just toggling the plain pause menu.
        if (optionPopUp != null && optionPopUp.IsAnyPanelOpen())
        {
            optionPopUp.ExitOptions(); // handles hiding panels AND resuming
            return;
        }

        if (IsPaused) Resume();
        else Pause();
    }

    public void Pause()
    {
        if (!allowPause)
        {
            return;
        }

        // Route through OptionPopUpScript if assigned, so BackgroundOverlay
        // and any other modal-management logic stays consistent everywhere.
        if (optionPopUp != null)
            optionPopUp.OpenOptionsMenu();
        else
            pauseMenuUI.SetActive(true);

        Time.timeScale = 0f;
        IsPaused = true;
    }

    public void Resume()
    {
        if (optionPopUp != null)
        {
            optionPopUp.OptionsModal.SetActive(false);
            optionPopUp.VideoModal.SetActive(false);
            optionPopUp.AudioModal.SetActive(false);
            optionPopUp.QuitPrompt.SetActive(false);
            optionPopUp.BackgroundOverlay.SetActive(false);
        }
        else
        {
            pauseMenuUI.SetActive(false);
        }

        Time.timeScale = 1f;
        IsPaused = false;
    }

    // Wire this to a "Resume"/"Continue" button as an alternative to Esc
    public void OnResumeButtonPressed()
    {
        Resume();
    }

    // Wire this to the new "Menu" button on the pause panel.
    // Hides all pause/gameplay UI, resets timeScale and pause state before leaving,
    // so the Main Menu (and any scene loaded after it) doesn't inherit a frozen game.
    public void ReturnToMainMenu()
    {
        if (string.IsNullOrEmpty(mainMenuSceneName))
        {
            Debug.LogError("PauseManager: mainMenuSceneName is empty!");
            return;
        }

        // Ignore repeated clicks while the transition is already running.
        if (isLeavingToMenu)
        {
            return;
        }

        isLeavingToMenu = true;

        // The fade can take a few seconds. Disable pausing so pressing Escape
        // during the transition can't bring the pause UI back.
        allowPause = false;

        HideAllUIForMenuExit();

        Time.timeScale = 1f;
        IsPaused = false;

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.StartTransition(mainMenuSceneName);
        }
        else
        {
            Debug.LogWarning("PauseManager: No SceneTransitionManager found, loading directly.");
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }

    private void HideAllUIForMenuExit()
    {
        if (optionPopUp != null)
        {
            if (optionPopUp.OptionsModal != null) optionPopUp.OptionsModal.SetActive(false);
            if (optionPopUp.VideoModal != null) optionPopUp.VideoModal.SetActive(false);
            if (optionPopUp.AudioModal != null) optionPopUp.AudioModal.SetActive(false);
            if (optionPopUp.QuitPrompt != null) optionPopUp.QuitPrompt.SetActive(false);
            if (optionPopUp.BackgroundOverlay != null) optionPopUp.BackgroundOverlay.SetActive(false);
        }

        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false);
        }

        if (pauseButton != null)
        {
            pauseButton.gameObject.SetActive(false);
        }

        if (extraUIToHideOnMenu != null)
        {
            foreach (GameObject ui in extraUIToHideOnMenu)
            {
                if (ui != null) ui.SetActive(false);
            }
        }
    }
}