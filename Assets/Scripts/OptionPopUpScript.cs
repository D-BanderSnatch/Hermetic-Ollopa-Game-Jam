using UnityEngine;
using UnityEngine.UI;

public class OptionPopUpScript : MonoBehaviour
{
   public GameObject OptionsModal;
   public GameObject VideoModal;
   public GameObject AudioModal;
   public GameObject QuitPrompt;
   public GameObject BackgroundOverlay;

   public void ShowPanel(GameObject panelToShow)
    {
        OptionsModal.SetActive(false);
        VideoModal.SetActive(false);
        AudioModal.SetActive(false);
        QuitPrompt.SetActive(false);

        BackgroundOverlay.SetActive(true);

        panelToShow.SetActive(true);
    }

    // Wire this to whatever opens the Options panel (e.g. a button on the pause menu,
    // or the same place Esc currently opens it from) so BackgroundOverlay always shows.
    public void OpenOptionsMenu()
    {
        ShowPanel(OptionsModal);
    }

    public void BacktoOptions(GameObject panelToShow)
    {
        panelToShow.SetActive(false);
        OptionsModal.SetActive(true);
    }

    // X button on the Options panel, or Esc pressed while any options-related panel is open
    public void ExitOptions()
    {
        BackgroundOverlay.SetActive(false);
        OptionsModal.SetActive(false);
        VideoModal.SetActive(false);
        AudioModal.SetActive(false);
        QuitPrompt.SetActive(false);

        // The whole overlay is closing, so tell PauseManager to actually resume
        if (PauseManager.Instance != null)
            PauseManager.Instance.Resume();
    }

    // Lets other scripts (like PauseManager) check if any modal is currently open,
    // without needing a separate reference to BackgroundOverlay.
    public bool IsAnyPanelOpen()
    {
        return OptionsModal.activeSelf
            || VideoModal.activeSelf
            || AudioModal.activeSelf
            || QuitPrompt.activeSelf;
    }

    public void OpenModal()
    {
        BackgroundOverlay.SetActive(true);
        ShowPanel(QuitPrompt);
    }

    // "Cancel" on the quit prompt
    public void CloseModal()
    {
        BackgroundOverlay.SetActive(false);
        QuitPrompt.SetActive(false);

        // Same reasoning: closing the quit prompt closes the whole overlay
        if (PauseManager.Instance != null)
            PauseManager.Instance.Resume();
    }

    public void QuitGame()
    {
        Application.Quit();

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}