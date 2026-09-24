using UnityEngine;

public class PickupItem : MonoBehaviour
{
    public Sprite icon;

    [Header("Must match an entry in ItemDatabase")]
    public string itemName; // e.g. "Cabinet", "Table" - NOT a prefab reference anymore

    [Header("Placement Adjustment")]
    public Vector3 placementOffset = Vector3.zero; // fine-tune where THIS item lands relative to the PlacePoint

    [HideInInspector]
    public bool isLocked = false;

    [Header("Locked Visual")]
    public Color lockedColor = Color.red;

    private Renderer[] renderers;
    private Color[] originalColors;
    private bool wasLocked = false;

    void Start()
    {
        renderers = GetComponentsInChildren<Renderer>();
        originalColors = new Color[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                originalColors[i] = renderers[i].material.color;
            }
        }
    }

    void Update()
    {
        if (isLocked != wasLocked)
        {
            wasLocked = isLocked;
            ApplyVisual();
        }
    }

    void ApplyVisual()
    {
        if (renderers == null)
        {
            return;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
            {
                continue;
            }

            renderers[i].material.color = isLocked ? lockedColor : originalColors[i];
        }
    }
}