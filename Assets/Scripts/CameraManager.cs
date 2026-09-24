using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance;

    void Awake()
    {
        Instance = this;
    }

    public void SwitchTo(Camera targetCamera)
    {
        // Turn off all cameras tagged appropriately, then enable only the target
        Camera[] allCameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);

        foreach (Camera cam in allCameras)
        {
            cam.gameObject.SetActive(false);
        }

        targetCamera.gameObject.SetActive(true);
    }
}