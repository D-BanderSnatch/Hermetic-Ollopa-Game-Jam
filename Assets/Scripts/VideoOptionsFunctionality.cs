using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VideoOptionsFunctionality : MonoBehaviour
{
    public TMP_Dropdown resolutionDropdown;
    public Toggle fullscreenToggle;
    public TMP_Dropdown qualityDropdown;

    Resolution[] filteredResolutions;
    int currentResolutionIndex = 0;

    void Start()
    {
        var mgr = GameSettingsManager.Instance;

        // --- Build resolution dropdown (must be rebuilt per-scene) ---
        Resolution[] allResolutions = Screen.resolutions;
        filteredResolutions = FilterDuplicateResolutions(allResolutions);

        resolutionDropdown.ClearOptions();
        List<string> options = new List<string>();

        for (int i = 0; i < filteredResolutions.Length; i++)
        {
            string option = filteredResolutions[i].width + "x" + filteredResolutions[i].height;
            options.Add(option);

            // Match against the saved resolution, not just the current screen state
            if (filteredResolutions[i].width == mgr.ResolutionWidth &&
                filteredResolutions[i].height == mgr.ResolutionHeight)
            {
                currentResolutionIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();

        // --- Sync other controls to saved values ---
        if (fullscreenToggle != null)
            fullscreenToggle.isOn = mgr.IsFullscreen;

        if (qualityDropdown != null)
            qualityDropdown.value = mgr.QualityIndex;
    }

    Resolution[] FilterDuplicateResolutions(Resolution[] source)
    {
        List<Resolution> unique = new List<Resolution>();
        HashSet<string> seen = new HashSet<string>();

        foreach (Resolution res in source)
        {
            string key = res.width + "x" + res.height;

            if (!seen.Contains(key))
            {
                seen.Add(key);
                unique.Add(res);
            }
        }

        return unique.ToArray();
    }

    // Hook these up to the UI's OnValueChanged events in the Inspector

    public void SetQuality(int qualityIndex)
    {
        GameSettingsManager.Instance.SetQuality(qualityIndex);
    }

    public void SetFullScreen(bool isFullscreen)
    {
        GameSettingsManager.Instance.SetFullScreen(isFullscreen);
    }

    public void SetResolution(int resolutionIndex)
    {
        Resolution resolution = filteredResolutions[resolutionIndex];
        GameSettingsManager.Instance.SetResolution(resolution.width, resolution.height);
    }
}