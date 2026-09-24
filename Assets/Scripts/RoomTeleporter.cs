using UnityEngine;
using System.Collections;

public class RoomTeleporter : MonoBehaviour
{
    [Header("Destination")]
    public Transform destination; 

    [Header("Camera Switch")]
    public Camera targetCamera; 

    [Header("Optional Fade")]
    public CanvasGroup fadeCanvasGroup; 
    public float fadeDuration = 0.3f;

    private bool isTeleporting = false;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isTeleporting)
        {
            StartCoroutine(TeleportPlayer(other.transform));
        }
    }

    IEnumerator TeleportPlayer(Transform player)
    {
        isTeleporting = true;

        if (fadeCanvasGroup != null)
        {
            yield return StartCoroutine(Fade(0f, 1f)); 
        }

        CharacterController controller = player.GetComponent<CharacterController>();

        if (controller != null)
        {
            controller.enabled = false;
        }

        player.position = destination.position;

        if (controller != null)
        {
            controller.enabled = true;
        }

        if (targetCamera != null && CameraManager.Instance != null)
        {
            CameraManager.Instance.SwitchTo(targetCamera);
        }

        if (fadeCanvasGroup != null)
        {
            yield return StartCoroutine(Fade(1f, 0f)); 
        }

        isTeleporting = false;
    }

    IEnumerator Fade(float from, float to)
    {
        float t = 0f;
        fadeCanvasGroup.alpha = from;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(from, to, t / fadeDuration);
            yield return null;
        }

        fadeCanvasGroup.alpha = to;
    }
}