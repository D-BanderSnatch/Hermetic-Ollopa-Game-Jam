using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class PickupDetector : MonoBehaviour
{
    [Header("Pickup")]
    public GameObject pickupPrompt;
    public TMP_Text pickupPromptText; // the Text child inside pickupPrompt
    public float pickupXDistance = 2f;

    [Header("Furniture Placement")]
    public Transform[] placePoints;
    public float placeDistance = 1.5f;

    [Header("Placement Preview")]
    [Tooltip("Alpha of the ghost preview (0 = invisible, 1 = solid). Requires the furniture material to support transparency.")]
    [Range(0f, 1f)] public float previewAlpha = 0.4f;

    [Header("Animation")]
    [Tooltip("Reference to the player's PlayerMovement script, used to play the pickup/place animation.")]
    public PlayerMovement playerMovement;

    [Header("Sound Effects")]
    [Tooltip("AudioSource used to play pickup/place sounds. Can be the same one used for footsteps, or a separate one.")]
    public AudioSource sfxAudioSource;
    [Tooltip("Sound played when an item is successfully picked up.")]
    public AudioClip pickupSound;
    [Tooltip("Sound played when furniture is successfully placed down.")]
    public AudioClip placeSound;

    private GameObject nearbyItem;

    private GameObject previewInstance;
    private GameObject previewForPrefab; // tracks which prefab the current preview was built from
    private Transform currentTargetPoint;

    void Start()
    {
        if (pickupPrompt != null)
        {
            pickupPrompt.SetActive(false);
        }
    }

    void Update()
    {
        FindPickup();
        UpdatePlacementPreview();

        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (nearbyItem != null)
            {
                PickUpItem();
            }
        }

        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            TryPlaceItem();
        }
    }

    void FindPickup()
    {
        nearbyItem = null;

        GameObject[] items;

        try
        {
            items = GameObject.FindGameObjectsWithTag("Pickup");
        }
        catch
        {
            Debug.LogError("No object is using the 'Pickup' tag. Make sure the Pickup tag exists.");

            if (pickupPrompt != null)
            {
                pickupPrompt.SetActive(false);
            }

            return;
        }

        float closestXDistance = Mathf.Infinity;
        Vector3 playerPosition = transform.position;

        foreach (GameObject item in items)
        {
            if (item == null)
            {
                continue;
            }

            Vector3 itemPosition = item.transform.position;

            float xDistance = Mathf.Abs(playerPosition.x - itemPosition.x);
            float xDirection = itemPosition.x - playerPosition.x;

            bool facingCube = IsFacingCube(xDirection);

            if (xDistance <= pickupXDistance && facingCube)
            {
                if (xDistance < closestXDistance)
                {
                    closestXDistance = xDistance;
                    nearbyItem = item;
                }
            }
        }

        if (pickupPrompt != null)
        {
            pickupPrompt.SetActive(nearbyItem != null);
        }

        if (nearbyItem != null && pickupPromptText != null)
        {
            PickupItem nearbyPickupItem = nearbyItem.GetComponent<PickupItem>();

            if (nearbyPickupItem != null && nearbyPickupItem.isLocked)
            {
                pickupPromptText.text = "Blocking the monster — can't move it yet!";
            }
            else
            {
                pickupPromptText.text = "Press E to pick up";
            }
        }
    }

    // Uses PlayerMovement.facingDirection instead of transform.right, because
    // the player sprite flips via SpriteRenderer.flipX (not rotation), so
    // transform.right never actually changes. facingDirection is kept in
    // sync with the visual flip inside PlayerMovement.Update().
    bool IsFacingCube(float xDirection)
    {
        if (playerMovement == null)
        {
            return true; // fallback — shouldn't normally happen since playerMovement is required elsewhere
        }

        if (xDirection > 0)
        {
            return playerMovement.facingDirection > 0;
        }

        if (xDirection < 0)
        {
            return playerMovement.facingDirection < 0;
        }

        return true;
    }

    void PickUpItem()
    {
        if (InventoryManager.Instance == null)
        {
            Debug.LogError("InventoryManager is missing!");
            return;
        }

        if (InventoryManager.Instance.HasItem())
        {
            Debug.Log("Inventory is already full!");
            return;
        }

        if (nearbyItem == null)
        {
            return;
        }

        PickupItem item = nearbyItem.GetComponent<PickupItem>();

        if (item == null)
        {
            Debug.LogError("PickupItem script is missing from the pickup object!");
            return;
        }

        if (item.isLocked)
        {
            Debug.Log("This furniture is holding the monster back — you can't move it yet!");
            return;
        }

        // Capture the specific item now, since nearbyItem can change while the animation plays.
        GameObject itemToPickUp = nearbyItem;

        if (playerMovement == null)
        {
            Debug.LogError("PlayerMovement reference is missing on PickupDetector!");
            return;
        }

        PlaySfx(pickupSound);

        bool started = playerMovement.TriggerPickupAnimation(() =>
        {
            FinishPickUp(itemToPickUp, item);
        });

        if (!started)
        {
            Debug.Log("Already picking up/placing something — ignoring this press.");
        }
    }

    void FinishPickUp(GameObject itemToPickUp, PickupItem item)
    {
        // Re-check in case the object was destroyed or state changed mid-animation.
        if (itemToPickUp == null || item == null)
        {
            return;
        }

        bool pickedUp = InventoryManager.Instance.AddItem(item.icon, item.itemName);

        if (pickedUp)
        {
            Destroy(itemToPickUp);

            if (nearbyItem == itemToPickUp)
            {
                nearbyItem = null;
            }

            if (pickupPrompt != null)
            {
                pickupPrompt.SetActive(false);
            }

            Debug.Log("Picked up furniture!");

            TutorialManager tutorial = FindAnyObjectByType<TutorialManager>();

            if (tutorial != null)
            {
                tutorial.OnFurniturePickedUp();
            }
        }
    }

    void TryPlaceItem()
    {
        if (InventoryManager.Instance == null)
        {
            Debug.LogError("InventoryManager is missing!");
            return;
        }

        if (!InventoryManager.Instance.HasItem())
        {
            Debug.Log("You are not carrying anything.");
            return;
        }

        Transform closestPoint = currentTargetPoint;

        if (closestPoint == null)
        {
            Debug.Log("You are not close enough to a PlacePoint, or not facing it.");
            return;
        }

        if (IsWindowBarricaded(closestPoint))
        {
            Debug.Log("This window is already barricaded. Furniture cannot be placed here.");
            return;
        }

        if (IsPointOccupied(closestPoint))
        {
            Debug.Log("There's already furniture here. Pick it up first if you want to move it.");
            return;
        }

        if (playerMovement == null)
        {
            Debug.LogError("PlayerMovement reference is missing on PickupDetector!");
            return;
        }

        PlaySfx(placeSound);

        bool started = playerMovement.TriggerPickupAnimation(() =>
        {
            PlaceItem(closestPoint);
        });

        if (!started)
        {
            Debug.Log("Already picking up/placing something — ignoring this press.");
        }
    }

    bool IsPointOccupied(Transform point)
    {
        if (point == null)
        {
            return false;
        }

        Collider[] objects = Physics.OverlapSphere(point.position, 0.7f);

        foreach (Collider obj in objects)
        {
            if (obj.CompareTag("Player"))
            {
                continue;
            }

            PickupItem existingFurniture = obj.GetComponentInParent<PickupItem>();

            if (existingFurniture != null)
            {
                return true;
            }
        }

        return false;
    }

    // Compares horizontal (x) distance only, matching the same logic FindPickup()
    // already uses for pickup detection — so placement and pickup behave consistently.
    // Previously this used Vector3.Distance (full 3D distance), which let differences
    // in z-depth between PlacePoints override which one was actually in front of the
    // player on the x-axis (e.g. door vs window at different z positions).
    Transform GetClosestPlacePoint()
    {
        if (placePoints == null || placePoints.Length == 0)
        {
            Debug.LogError("No PlacePoints assigned! Assign your furniture placement points to the Place Points array in the Inspector.");
            return null;
        }

        Transform closestPoint = null;
        float closestXDistance = Mathf.Infinity;
        Vector3 playerPosition = transform.position;

        foreach (Transform point in placePoints)
        {
            if (point == null)
            {
                continue;
            }

            float xDistance = Mathf.Abs(playerPosition.x - point.position.x);

            if (xDistance > placeDistance)
            {
                continue;
            }

            float xDirection = point.position.x - playerPosition.x;

            if (!IsFacingCube(xDirection))
            {
                continue;
            }

            if (xDistance < closestXDistance)
            {
                closestXDistance = xDistance;
                closestPoint = point;
            }
        }

        return closestPoint;
    }

    bool IsWindowBarricaded(Transform point)
    {
        if (point == null)
        {
            return false;
        }

        WindowBarricade barricade = point.GetComponentInParent<WindowBarricade>();

        if (barricade == null)
        {
            return false;
        }

        return barricade.IsBarricaded();
    }

    void PlaySfx(AudioClip clip)
    {
        if (sfxAudioSource == null || clip == null)
        {
            return;
        }

        sfxAudioSource.PlayOneShot(clip);
    }

    void PlaceItem(Transform point)
    {
        if (point == null)
        {
            Debug.LogError("PlacePoint is null!");
            return;
        }

        GameObject prefab = InventoryManager.Instance.GetCurrentPrefab();

        if (prefab == null)
        {
            Debug.LogError("Item Prefab is not assigned in InventoryManager!");
            return;
        }

        // Read the offset from the prefab asset itself (safe, never gets destroyed/remapped)
        PickupItem prefabItemData = prefab.GetComponent<PickupItem>();
        Vector3 offset = prefabItemData != null ? prefabItemData.placementOffset : Vector3.zero;

        // Default: use whatever rotation the prefab itself already has
        Quaternion spawnRotation = prefab.transform.rotation;

        // SPECIAL CASE: Table always uses this exact rotation, everything else stays default
        if (prefabItemData != null && prefabItemData.itemName == "Table")
        {
            spawnRotation = new Quaternion(-0.032802999f, 0.0504383221f, -0.998049974f, 0.0166174583f);
        }

        Instantiate(prefab, point.position + offset, spawnRotation);

        InventoryManager.Instance.ClearItem();

        Debug.Log("Furniture placed at: " + point.name);

        TutorialManager tutorial = FindAnyObjectByType<TutorialManager>();

        if (tutorial != null)
        {
            tutorial.OnFurniturePlaced();
        }
    }

    void UpdatePlacementPreview()
    {
        // No item carried → no preview.
        if (InventoryManager.Instance == null || !InventoryManager.Instance.HasItem())
        {
            ClearPreview();
            return;
        }

        currentTargetPoint = GetClosestPlacePoint();

        // No valid point, or it's occupied/barricaded → no preview.
        if (currentTargetPoint == null ||
            IsWindowBarricaded(currentTargetPoint) ||
            IsPointOccupied(currentTargetPoint))
        {
            ClearPreview();
            return;
        }

        GameObject prefab = InventoryManager.Instance.GetCurrentPrefab();

        if (prefab == null)
        {
            ClearPreview();
            return;
        }

        // Rebuild the preview only if the prefab changed, or if the old instance
        // was destroyed some other way (Unity "fake null" check via bool cast).
        bool needsRebuild = previewInstance == null || !previewInstance || previewForPrefab != prefab;

        if (needsRebuild)
        {
            DestroyPreviewInstance();
            previewInstance = Instantiate(prefab);
            previewForPrefab = prefab;

            // Strip anything that would make the ghost behave like real furniture.
            PickupItem previewItemScript = previewInstance.GetComponent<PickupItem>();
            if (previewItemScript != null)
            {
                Destroy(previewItemScript);
            }

            Collider[] colliders = previewInstance.GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders)
            {
                col.enabled = false;
            }

            ApplyPreviewAlpha(previewInstance);
        }

        // Guard against a destroyed/null preview instance before using it.
        if (previewInstance == null || !previewInstance)
        {
            return;
        }

        // Position/rotate to match where PlaceItem() would actually put it.
        PickupItem prefabItemData = prefab.GetComponent<PickupItem>();
        Vector3 offset = prefabItemData != null ? prefabItemData.placementOffset : Vector3.zero;

        Quaternion rotation = prefab.transform.rotation;
        if (prefabItemData != null && prefabItemData.itemName == "Table")
        {
            rotation = new Quaternion(-0.032802999f, 0.0504383221f, -0.998049974f, 0.0166174583f);
        }

        previewInstance.transform.position = currentTargetPoint.position + offset;
        previewInstance.transform.rotation = rotation;
        previewInstance.SetActive(true);
    }

    void ApplyPreviewAlpha(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();

        foreach (Renderer rend in renderers)
        {
            if (rend == null || rend.material == null)
            {
                continue;
            }

            Color c = rend.material.color;
            c.a = previewAlpha;
            rend.material.color = c;
        }
    }

    void ClearPreview()
    {
        DestroyPreviewInstance();
        currentTargetPoint = null;
    }

    void DestroyPreviewInstance()
    {
        if (previewInstance != null)
        {
            Destroy(previewInstance);
        }

        previewInstance = null;
        previewForPrefab = null;
    }
}