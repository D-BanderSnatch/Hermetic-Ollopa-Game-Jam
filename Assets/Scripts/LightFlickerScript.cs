using UnityEngine;
using System.Collections;

public class LightFlickerScript : MonoBehaviour
{
    private Light bulbLight;

    [Header("References")]
    public Renderer bulbRenderer;
    [Tooltip("Index of the emissive material in the Renderer's material list (check the Materials list in Inspector — e.g. 1 for LightBulbEmit).")]
    public int emissiveMaterialIndex = 1;
    public Color emissionColor = Color.white;

    [Header("Flicker Settings")]
    public float minIntensity = 200f;
    public float maxIntensity = 650f;
    public float flickerSpeed = 0.5f;

    [Header("Emission Settings")]
    public float minEmission = 0.3f;
    public float maxEmission = 2f;

    [Header("Occasional Dramatic Flicker")]
    public bool enableDramaticFlicker = true;
    [Tooltip("Chance (0-1) checked roughly once per second that a dramatic dip occurs.")]
    public float dramaticFlickerChance = 0.05f;
    [Tooltip("Minimum duration the dark dip holds, in seconds.")]
    public float minDramaticDuration = 0.1f;
    [Tooltip("Maximum duration the dark dip holds, in seconds.")]
    public float maxDramaticDuration = 0.4f;

    private float seed;
    private Material bulbMaterial;
    private bool isDramaticFlickerActive = false;

    void Start()
    {
        bulbLight = GetComponent<Light>();
        seed = Random.Range(0f, 100f);

        if (bulbRenderer != null)
        {
            Material[] mats = bulbRenderer.materials; // instances, safe to modify
            if (emissiveMaterialIndex >= 0 && emissiveMaterialIndex < mats.Length)
            {
                bulbMaterial = mats[emissiveMaterialIndex];
                bulbMaterial.EnableKeyword("_EMISSION");
            }
            else
            {
                Debug.LogWarning("LightFlicker: emissiveMaterialIndex is out of range for this renderer's materials.");
            }
        }

        if (enableDramaticFlicker)
        {
            StartCoroutine(DramaticFlickerRoutine());
        }
    }

    void Update()
    {
        // Skip normal flicker while a dramatic dip is holding
        if (isDramaticFlickerActive) return;

        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, seed);
        float t = noise;

        float targetIntensity = Mathf.Lerp(minIntensity, maxIntensity, t);
        float targetEmission = Mathf.Lerp(minEmission, maxEmission, t);

        if (bulbLight != null)
            bulbLight.intensity = targetIntensity;

        if (bulbMaterial != null)
            bulbMaterial.SetColor("_EmissionColor", emissionColor * targetEmission);
    }

    IEnumerator DramaticFlickerRoutine()
    {
        while (true)
        {
            // Check roughly once per second whether a dramatic flicker should trigger
            yield return new WaitForSeconds(1f);

            if (Random.value < dramaticFlickerChance)
            {
                isDramaticFlickerActive = true;

                // True zero — light fully off, emission fully black
                if (bulbLight != null)
                    bulbLight.intensity = 0f;

                if (bulbMaterial != null)
                {
                    bulbMaterial.DisableKeyword("_EMISSION");
                    bulbMaterial.SetColor("_EmissionColor", Color.black);
                }

                float duration = Random.Range(minDramaticDuration, maxDramaticDuration);
                yield return new WaitForSeconds(duration);

                if (bulbMaterial != null)
                    bulbMaterial.EnableKeyword("_EMISSION");

                isDramaticFlickerActive = false;
            }
        }
    }
}