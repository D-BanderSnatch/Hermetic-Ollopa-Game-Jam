using UnityEngine;
using UnityEngine.UI;

public class AudioOptionsFunctionality : MonoBehaviour
{
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider soundSlider;

    void Start()
    {
        var mgr = GameSettingsManager.Instance;

        // Just reflect the manager's current values, don't recalculate anything
        masterSlider.value = mgr.MasterVolume;
        musicSlider.value = mgr.MusicVolume;
        soundSlider.value = mgr.SoundVolume;
    }

    // Hook these up to each Slider's OnValueChanged event in the Inspector

    public void SetMasterVolume(float value)
    {
        GameSettingsManager.Instance.SetMasterVolume(value);
    }

    public void SetMusicVolume(float value)
    {
        GameSettingsManager.Instance.SetMusicVolume(value);
    }

    public void SetSoundVolume(float value)
    {
        GameSettingsManager.Instance.SetSoundVolume(value);
    }
}