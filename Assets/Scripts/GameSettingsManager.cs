using UnityEngine;
using UnityEngine.Audio;

public class GameSettingsManager : MonoBehaviour
{
    public static GameSettingsManager Instance { get; private set; }

    [Header("Assign your AudioMixer asset here")]
    public AudioMixer audioMixer;

    // Current values, exposed so UI scripts can read them on Start()
    public float MasterVolume { get; private set; }
    public float MusicVolume { get; private set; }
    public float SoundVolume { get; private set; }
    public int QualityIndex { get; private set; }
    public bool IsFullscreen { get; private set; }
    public int ResolutionWidth { get; private set; }
    public int ResolutionHeight { get; private set; }

    // Automatically creates the manager if it doesn't exist yet — this runs
    // before any scene's Start() methods, so it fixes the "tested this scene
    // directly, GameSettingsManager was never created" problem.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void EnsureExists()
    {
        Debug.Log("GameSettingsManager.EnsureExists() called.");

        if (Instance != null)
        {
            Debug.Log("GameSettingsManager: Instance already exists, skipping spawn.");
            return;
        }

        GameObject prefab = Resources.Load<GameObject>("GameSettingsManager");
        if (prefab == null)
        {
            Debug.LogError("GameSettingsManager: no prefab found at Resources/GameSettingsManager. " +
                "Create one: put this script + your AudioMixer on a prefab named " +
                "exactly 'GameSettingsManager' inside a folder named 'Resources'.");
            return;
        }

        Debug.Log("GameSettingsManager: prefab found, instantiating now.");
        GameObject spawned = Instantiate(prefab);
        Debug.Log("GameSettingsManager: instantiated. Instance is now " + (Instance != null ? "SET" : "STILL NULL"));
    }

    void Awake()
    {
        Debug.Log("GameSettingsManager.Awake() called on " + gameObject.name);

        // Singleton pattern: if one already exists (e.g. you re-entered
        // the bootstrap/main menu scene), destroy this duplicate.
        if (Instance != null && Instance != this)
        {
            Debug.Log("GameSettingsManager: duplicate detected, destroying this one.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Debug.Log("GameSettingsManager: Instance set successfully.");
        DontDestroyOnLoad(gameObject);

        LoadAndApplyAll();
    }

    void LoadAndApplyAll()
    {
        MasterVolume = PlayerPrefs.GetFloat("MasterVolume", 0.75f);
        MusicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.75f);
        SoundVolume = PlayerPrefs.GetFloat("SoundVolume", 0.75f);
        QualityIndex = PlayerPrefs.GetInt("QualityIndex", QualitySettings.GetQualityLevel());
        IsFullscreen = PlayerPrefs.GetInt("Fullscreen", Screen.fullScreen ? 1 : 0) == 1;
        ResolutionWidth = PlayerPrefs.GetInt("ResolutionWidth", Screen.currentResolution.width);
        ResolutionHeight = PlayerPrefs.GetInt("ResolutionHeight", Screen.currentResolution.height);

        SetMasterVolume(MasterVolume);
        SetMusicVolume(MusicVolume);
        SetSoundVolume(SoundVolume);
        SetQuality(QualityIndex);
        SetFullScreen(IsFullscreen);
        SetResolution(ResolutionWidth, ResolutionHeight);
    }

    public void SetMasterVolume(float value)
    {
        MasterVolume = value;
        audioMixer.SetFloat("MasterVolume", LinearToDecibel(value));
        PlayerPrefs.SetFloat("MasterVolume", value);
    }

    public void SetMusicVolume(float value)
    {
        MusicVolume = value;
        audioMixer.SetFloat("MusicVolume", LinearToDecibel(value));
        PlayerPrefs.SetFloat("MusicVolume", value);
    }

    public void SetSoundVolume(float value)
    {
        SoundVolume = value;
        audioMixer.SetFloat("SFXVolume", LinearToDecibel(value));
        PlayerPrefs.SetFloat("SoundVolume", value);
    }

    public void SetQuality(int qualityIndex)
    {
        QualityIndex = qualityIndex;
        QualitySettings.SetQualityLevel(qualityIndex);
        PlayerPrefs.SetInt("QualityIndex", qualityIndex);
    }

    public void SetFullScreen(bool isFullscreen)
    {
        IsFullscreen = isFullscreen;
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
    }

    public void SetResolution(int width, int height)
    {
        ResolutionWidth = width;
        ResolutionHeight = height;
        Screen.SetResolution(width, height, Screen.fullScreen);
        PlayerPrefs.SetInt("ResolutionWidth", width);
        PlayerPrefs.SetInt("ResolutionHeight", height);
    }

    float LinearToDecibel(float linear)
    {
        // Avoid Log10(0), which returns negative infinity
        if (linear <= 0.0001f)
            return -80f;

        return Mathf.Log10(linear) * 20f;
    }
}