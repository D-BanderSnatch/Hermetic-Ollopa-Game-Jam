using UnityEngine;
using UnityEngine.InputSystem;

public class BarricadeDetector : MonoBehaviour
{
    [Header("UI")]
    public GameObject barricadePrompt;

    [Header("Windows")]
    public Transform[] placePoints;

    [Header("Distance")]
    public float barricadeXDistance = 2f;

    [Header("Animation")]
    [Tooltip("Reference to the player's PlayerMovement script, used to play the barricading animation.")]
    public PlayerMovement playerMovement;

    [Header("Sound Effects")]
    [Tooltip("AudioSource used to play the barricading sound. Can be the same one used elsewhere on the player, or a separate one.")]
    public AudioSource sfxAudioSource;
    [Tooltip("Sound played when a window is successfully barricaded.")]
    public AudioClip barricadeSound;

    // ==============================================
    // TUTORIAL GATE
    // ==============================================
    // Automatically found at runtime if one exists in the scene.
    // Stays null in every scene that has no TutorialManager (e.g. Day1+),
    // in which case barricading is never blocked by this check -
    // it just behaves exactly as before.
    private TutorialManager tutorialManager;

    private Transform nearbyPlacePoint;
    private WindowBarricade nearbyWindow;

    // 1 = facing right
    // -1 = facing left
    private float facingDirection = 1f;

    void Start()
    {
        if (barricadePrompt != null)
        {
            barricadePrompt.SetActive(false);
        }

        // Will simply stay null in scenes with no TutorialManager -
        // totally fine, see IsBarricadingAllowed() below.
        tutorialManager = FindAnyObjectByType<TutorialManager>();
    }

    void Update()
    {
        UpdateFacingDirection();

        FindNearbyWindow();

        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            TryBarricade();
        }
    }

    // =========================================================
    // TUTORIAL GATE CHECK
    // =========================================================

    bool IsBarricadingAllowed()
    {
        // No tutorial in this scene at all -> always allowed.
        if (tutorialManager == null)
        {
            return true;
        }

        return tutorialManager.BarricadingUnlocked;
    }

    // =========================================================
    // PLAYER FACING
    // =========================================================

    void UpdateFacingDirection()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (
            Keyboard.current.dKey.isPressed ||
            Keyboard.current.rightArrowKey.isPressed
        )
        {
            facingDirection = 1f;
        }

        if (
            Keyboard.current.aKey.isPressed ||
            Keyboard.current.leftArrowKey.isPressed
        )
        {
            facingDirection = -1f;
        }
    }

    // =========================================================
    // FIND NEARBY WINDOW
    // =========================================================

    void FindNearbyWindow()
    {
        nearbyPlacePoint = null;
        nearbyWindow = null;

        if (placePoints == null || placePoints.Length == 0)
        {
            SetPrompt(false);
            return;
        }

        float closestDistance = Mathf.Infinity;

        foreach (Transform point in placePoints)
        {
            if (point == null)
            {
                continue;
            }

            // -------------------------------------------------
            // X DISTANCE
            // -------------------------------------------------

            float xDistance =
                Mathf.Abs(
                    transform.position.x -
                    point.position.x
                );

            if (xDistance > barricadeXDistance)
            {
                continue;
            }

            // -------------------------------------------------
            // CHECK FACING DIRECTION
            // -------------------------------------------------

            float xDirection =
                point.position.x -
                transform.position.x;

            if (Mathf.Abs(xDirection) < 0.05f)
            {
                continue;
            }

            // Window is RIGHT of player
            // Player must face RIGHT
            if (xDirection > 0 && facingDirection < 0)
            {
                continue;
            }

            // Window is LEFT of player
            // Player must face LEFT
            if (xDirection < 0 && facingDirection > 0)
            {
                continue;
            }

            // -------------------------------------------------
            // FIND THE WINDOW
            // -------------------------------------------------

            WindowBarricade window =
                FindWindowForPlacePoint(point);

            if (window == null)
            {
                continue;
            }

            // Already barricaded
            if (window.IsBarricaded())
            {
                continue;
            }

            // Furniture is already there
            if (IsFurnitureAtPlacePoint(point))
            {
                continue;
            }

            // -------------------------------------------------
            // USE X DISTANCE TO FIND CLOSEST
            // -------------------------------------------------

            if (xDistance < closestDistance)
            {
                closestDistance = xDistance;

                nearbyPlacePoint = point;
                nearbyWindow = window;
            }
        }

        UpdatePrompt();
    }

    // =========================================================
    // FIND WINDOW CONNECTED TO THIS BARRICADE POINT
    // =========================================================

    WindowBarricade FindWindowForPlacePoint(Transform point)
    {
        WindowBarricade[] allWindows =
            FindObjectsByType<WindowBarricade>(
                FindObjectsSortMode.None
            );

        foreach (WindowBarricade window in allWindows)
        {
            if (window == null)
            {
                continue;
            }

            if (window.barricadePlacePoint == point)
            {
                return window;
            }
        }

        return null;
    }

    // =========================================================
    // CHECK FURNITURE
    // =========================================================

    bool IsFurnitureAtPlacePoint(Transform point)
    {
        if (point == null)
        {
            return false;
        }

        Collider[] colliders =
            Physics.OverlapSphere(
                point.position,
                0.7f
            );

        foreach (Collider collider in colliders)
        {
            if (collider == null)
            {
                continue;
            }

            PickupItem furniture =
                collider.GetComponentInParent<PickupItem>();

            if (furniture != null)
            {
                return true;
            }
        }

        return false;
    }

    // =========================================================
    // UPDATE PROMPT
    // =========================================================

    void UpdatePrompt()
    {
        if (barricadePrompt == null)
        {
            return;
        }

        bool hasBarricade =
            BarricadeInventory.Instance != null &&
            BarricadeInventory.Instance.HasBarricade();

        bool canBarricade =
            nearbyWindow != null &&
            nearbyPlacePoint != null &&
            hasBarricade &&
            IsBarricadingAllowed();

        barricadePrompt.SetActive(canBarricade);
    }

    // =========================================================
    // HIDE PROMPT
    // =========================================================

    void SetPrompt(bool state)
    {
        if (barricadePrompt != null)
        {
            barricadePrompt.SetActive(state);
        }
    }

    // =========================================================
    // PRESS R
    // =========================================================

    void TryBarricade()
    {
        // IMPORTANT:
        // If there is no valid nearby window,
        // R does absolutely nothing.

        if (nearbyWindow == null)
        {
            Debug.Log("No window available to barricade.");
            return;
        }

        if (nearbyPlacePoint == null)
        {
            return;
        }

        if (BarricadeInventory.Instance == null)
        {
            return;
        }

        if (!BarricadeInventory.Instance.HasBarricade())
        {
            return;
        }

        // Respect the tutorial gate here too, so pressing R can't
        // sneak a barricade in before the tutorial has unlocked it.
        if (!IsBarricadingAllowed())
        {
            Debug.Log("Barricading is not unlocked yet.");
            return;
        }

        // Check furniture again
        if (IsFurnitureAtPlacePoint(nearbyPlacePoint))
        {
            Debug.Log(
                "Cannot barricade this window. " +
                "Furniture is already placed here."
            );

            return;
        }

        if (playerMovement == null)
        {
            Debug.LogError("PlayerMovement reference is missing on BarricadeDetector!");
            return;
        }

        // Capture these now, since nearbyWindow/nearbyPlacePoint get
        // reset every frame by FindNearbyWindow() while the animation plays.
        WindowBarricade windowToBarricade = nearbyWindow;
        Transform placePointToBarricade = nearbyPlacePoint;

        PlaySfx(barricadeSound);

        bool started = playerMovement.TriggerBarricadeAnimation(() =>
        {
            FinishBarricade(windowToBarricade, placePointToBarricade);
        });

        if (!started)
        {
            Debug.Log("Already busy picking up/placing/barricading — ignoring this press.");
        }
    }

    void PlaySfx(AudioClip clip)
    {
        if (sfxAudioSource == null || clip == null)
        {
            return;
        }

        sfxAudioSource.PlayOneShot(clip);
    }

    // =========================================================
    // ACTUALLY APPLY THE BARRICADE (runs after the animation finishes)
    // =========================================================

    void FinishBarricade(WindowBarricade window, Transform placePoint)
    {
        // Re-check in case state changed mid-animation (window destroyed,
        // already barricaded by something else, furniture placed there, etc.).
        if (window == null || placePoint == null)
        {
            return;
        }

        if (window.IsBarricaded())
        {
            return;
        }

        if (IsFurnitureAtPlacePoint(placePoint))
        {
            Debug.Log(
                "Cannot barricade this window. " +
                "Furniture is already placed here."
            );

            return;
        }

        bool barricaded = window.Barricade();

        if (!barricaded)
        {
            return;
        }

        Debug.Log(
            "Successfully barricaded: " +
            window.gameObject.name
        );

        if (nearbyWindow == window)
        {
            nearbyWindow = null;
        }

        if (nearbyPlacePoint == placePoint)
        {
            nearbyPlacePoint = null;
        }

        if (barricadePrompt != null)
        {
            barricadePrompt.SetActive(false);
        }

        TutorialManager tutorial =
            FindAnyObjectByType<TutorialManager>();

        if (tutorial != null)
        {
            tutorial.OnWindowBarricaded();
        }
    }
}