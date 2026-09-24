using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections;

public class TutorialManager : MonoBehaviour
{
    [Header("Tutorial UI")]
    public GameObject tutorialPanel;
    public CanvasGroup tutorialCanvasGroup;

    public TMP_Text stepCounter;
    public TMP_Text titleText;
    public TMP_Text descriptionText;

    [Header("Skip Tutorial")]
    public Button skipTutorialButton;

    [Header("Tutorial Arrows")]
    public GameObject furnitureArrow;
    public GameObject doorArrow;
    public GameObject window1Arrow;
    public GameObject window2Arrow;

    [Header("Barricading")]
    public BarricadeInventory barricadeInventory;

    [Header("Windows")]
    public Transform window1; // drag the window's PlacePoint transform here
    public Transform window2; // drag the window's PlacePoint transform here

    [Header("Monster")]
    public GameObject monster;

    public Transform monsterSpawnDoor;
    public Transform monsterSpawnWindow1;
    public Transform monsterSpawnWindow2;

    [Header("Monster Spawn Check")]
    public float furnitureCheckRadius = 0.5f;

    [Header("Day Start")]
    public GameObject dayStartPanel;
    [Tooltip("CanvasGroup on the Day Start panel, used to fade it in smoothly. Add a CanvasGroup component to the dayStartPanel object and assign it here.")]
    public CanvasGroup dayStartCanvasGroup;
    [Tooltip("How long the Day Start panel takes to fade in, in seconds.")]
    public float dayStartFadeDuration = 0.5f;
    public Button dayStartButton;
    public string day1SceneName = "Day1";

    private int step = 0;
    private int totalSteps = 4;

    private bool tutorialBarricadeGiven = false;
    private bool tutorialFinished = false;

    // ==============================================
    // PUBLIC GATE FLAG - used by BarricadeDetector to know
    // whether the player is actually allowed to barricade yet.
    // True once the tutorial has handed out the practice barricade
    // (step 4), OR once the tutorial has ended entirely (skip or
    // finished normally), so gameplay after the tutorial is never
    // blocked by this check.
    // ==============================================
    public bool BarricadingUnlocked => tutorialBarricadeGiven || tutorialFinished;

    private string[] titles =
    {
        "Movement",
        "Head There",
        "Place It Down",
        "Barricading"
    };

    private string[] descriptions =
    {
        "Use A, D, Left Arrow, or Right Arrow to move.",
        "Go to the furniture and pick it up using E.",
        "Choose any highlighted spot to place the furniture using Q.",
        "You've been given a barricade! Check the icon in the bottom of your screen next to the furniture slot — it shows how many you have. Go to a window and press R to use it."
    };

    void Start()
    {
        // Show tutorial
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);
        }

        if (tutorialCanvasGroup != null)
        {
            tutorialCanvasGroup.alpha = 1f;
            tutorialCanvasGroup.interactable = true;
            tutorialCanvasGroup.blocksRaycasts = true;
        }

        // Hide Day Start panel
        if (dayStartPanel != null)
        {
            dayStartPanel.SetActive(false);
        }

        if (dayStartCanvasGroup != null)
        {
            dayStartCanvasGroup.alpha = 0f;
            dayStartCanvasGroup.interactable = false;
            dayStartCanvasGroup.blocksRaycasts = false;
        }

        // Hook up the Day 1 button
        if (dayStartButton != null)
        {
            dayStartButton.onClick.RemoveListener(OnDayStartButtonPressed);
            dayStartButton.onClick.AddListener(OnDayStartButtonPressed);
        }

        // Hook up the Skip Tutorial button
        if (skipTutorialButton != null)
        {
            skipTutorialButton.onClick.RemoveListener(SkipTutorial);
            skipTutorialButton.onClick.AddListener(SkipTutorial);

            skipTutorialButton.gameObject.SetActive(true);
        }

        // Hide monster at beginning
        if (monster != null)
        {
            monster.SetActive(false);
        }

        // Keep the barricade UI hidden until we actually reach
        // the barricading step - it shouldn't appear early.
        HideBarricadeUI();

        DisableAllArrows();

        step = 0;

        ShowStep();
    }

    void Update()
    {
        if (tutorialFinished)
        {
            return;
        }

        if (step == 0)
        {
            CheckMovement();
        }
    }

    void CheckMovement()
    {
        bool moved = false;

        if (Keyboard.current != null)
        {
            if (
                Keyboard.current.aKey.isPressed ||
                Keyboard.current.dKey.isPressed ||
                Keyboard.current.leftArrowKey.isPressed ||
                Keyboard.current.rightArrowKey.isPressed
            )
            {
                moved = true;
            }
        }

        if (moved)
        {
            Debug.Log(
                "Tutorial: Player moved!"
            );

            step = 1;

            ShowStep();
        }
    }

    void ShowStep()
    {
        DisableAllArrows();

        if (step < 0 || step >= totalSteps)
        {
            return;
        }

        if (stepCounter != null)
        {
            stepCounter.text =
                "Step " +
                (step + 1) +
                " / " +
                totalSteps;
        }

        if (titleText != null)
        {
            titleText.text =
                titles[step];
        }

        if (descriptionText != null)
        {
            descriptionText.text =
                descriptions[step];
        }

        // STEP 2
        if (step == 1)
        {
            if (furnitureArrow != null)
            {
                furnitureArrow.SetActive(true);
            }

            Debug.Log(
                "Tutorial Step 2: Find the furniture."
            );
        }

        // STEP 3
        if (step == 2)
        {
            Debug.Log(
                "Tutorial Step 3: Enable all placement arrows."
            );

            if (doorArrow != null)
            {
                doorArrow.SetActive(true);
            }

            if (window1Arrow != null)
            {
                window1Arrow.SetActive(true);
            }

            if (window2Arrow != null)
            {
                window2Arrow.SetActive(true);
            }
        }

        // STEP 4
        if (step == 3)
        {
            GiveTutorialBarricade();

            Debug.Log(
                "Tutorial Step 4: Barricading."
            );

            Debug.Log(
                "Go to a window and press R."
            );
        }
    }

    public void OnFurniturePickedUp()
    {
        if (tutorialFinished)
        {
            return;
        }

        Debug.Log(
            "Tutorial: Furniture picked up!"
        );

        if (step >= 3)
        {
            return;
        }

        if (furnitureArrow != null)
        {
            furnitureArrow.SetActive(false);
        }

        step = 2;

        ShowStep();
    }

    public void OnFurniturePlaced()
    {
        if (tutorialFinished)
        {
            return;
        }

        Debug.Log(
            "Tutorial: Furniture placed!"
        );

        DisableAllArrows();

        step = 3;

        ShowStep();
    }

    void GiveTutorialBarricade()
    {
        if (tutorialBarricadeGiven)
        {
            return;
        }

        if (barricadeInventory == null)
        {
            Debug.LogError(
                "TutorialManager: BarricadeInventory is NOT assigned!"
            );

            return;
        }

        // NOW is the right moment to reveal the panel -
        // right as the player actually receives their first barricade.
        ShowBarricadeUI();

        barricadeInventory.AddBarricade(1);

        tutorialBarricadeGiven = true;

        Debug.Log(
            "Tutorial: Player received 1 barricade."
        );
    }

    // ==============================================
    // SHOW / HIDE THE TOP-RIGHT BARRICADE PANEL
    // ==============================================

    void HideBarricadeUI()
    {
        if (barricadeInventory != null && barricadeInventory.barricadeUI != null)
        {
            barricadeInventory.barricadeUI.SetActive(false);
        }
    }

    void ShowBarricadeUI()
    {
        if (barricadeInventory != null && barricadeInventory.barricadeUI != null)
        {
            barricadeInventory.barricadeUI.SetActive(true);
        }
    }

    public void OnWindowBarricaded()
    {
        if (tutorialFinished)
        {
            return;
        }

        if (step != 3)
        {
            return;
        }

        Debug.Log(
            "Tutorial: Window successfully barricaded!"
        );

        tutorialFinished = true;

        DisableAllArrows();

        HideSkipButton();

        StartCoroutine(
            SpawnMonsterThenFinishTutorial()
        );
    }

    // ==============================================
    // SKIP TUTORIAL
    // ==============================================

    public void SkipTutorial()
    {
        if (tutorialFinished)
        {
            return;
        }

        Debug.Log(
            "Tutorial: Skipped by player."
        );

        tutorialFinished = true;

        step = totalSteps;

        StopAllCoroutines();

        DisableAllArrows();

        HideSkipButton();

        StartCoroutine(
            FadeOutThenShowDayStart()
        );
    }

    void HideSkipButton()
    {
        if (skipTutorialButton != null)
        {
            skipTutorialButton.interactable = false;

            skipTutorialButton.gameObject.SetActive(false);
        }
    }

    IEnumerator FadeOutThenShowDayStart()
    {
        yield return StartCoroutine(
            FadeOutTutorial()
        );

        ShowDayStart();
    }

    IEnumerator SpawnMonsterThenFinishTutorial()
    {
        Debug.Log(
            "Tutorial: Finding an open entry point..."
        );

        Transform spawnPoint =
            FindOpenMonsterSpawn();

        if (spawnPoint != null)
        {
            SpawnMonster(spawnPoint);

            yield return new WaitForSeconds(1f);
        }
        else
        {
            Debug.Log(
                "Tutorial: All entry points are blocked."
            );

            yield return new WaitForSeconds(0.5f);
        }

        Debug.Log(
            "Tutorial: Fading out."
        );

        yield return StartCoroutine(
            FadeOutTutorial()
        );

        ShowDayStart();
    }

    void ShowDayStart()
    {
        // The tutorial is over, so the top-right barricade panel
        // should not be visible behind the Day Start screen.
        HideBarricadeUI();

        HideSkipButton();

        if (dayStartPanel == null)
        {
            Debug.LogError(
                "TutorialManager: DayStartPanel is NOT assigned!"
            );

            return;
        }

        dayStartPanel.SetActive(true);

        // Re-bind here as well, in case the button lives inside
        // the Day Start panel and was inactive during Start().
        if (dayStartButton != null)
        {
            dayStartButton.onClick.RemoveListener(OnDayStartButtonPressed);
            dayStartButton.onClick.AddListener(OnDayStartButtonPressed);
        }

        StartCoroutine(FadeInDayStart());

        Debug.Log(
            "DAY 1 START PANEL SHOWN."
        );
    }

    IEnumerator FadeInDayStart()
    {
        if (dayStartCanvasGroup == null)
        {
            // No CanvasGroup assigned - fall back to the old instant behavior,
            // but still enable the button so the panel isn't shown-but-unusable.
            if (dayStartButton != null)
            {
                dayStartButton.interactable = true;
            }

            yield break;
        }

        // Make sure it starts fully invisible and non-interactable,
        // in case this runs more than once or alpha was left in a weird state.
        dayStartCanvasGroup.alpha = 0f;
        dayStartCanvasGroup.interactable = false;
        dayStartCanvasGroup.blocksRaycasts = false;

        float timer = 0f;

        while (timer < dayStartFadeDuration)
        {
            timer += Time.deltaTime;

            float progress = timer / dayStartFadeDuration;

            dayStartCanvasGroup.alpha = Mathf.Lerp(0f, 1f, progress);

            yield return null;
        }

        dayStartCanvasGroup.alpha = 1f;
        dayStartCanvasGroup.interactable = true;
        dayStartCanvasGroup.blocksRaycasts = true;

        if (dayStartButton != null)
        {
            dayStartButton.interactable = true;
        }
    }

    // ==============================================
    // DAY 1 BUTTON
    // ==============================================

    public void OnDayStartButtonPressed()
    {
        Debug.Log(
            "Tutorial: Loading scene -> " + day1SceneName
        );

        if (dayStartButton != null)
        {
            dayStartButton.interactable = false;
        }

        // Make sure nothing from the tutorial HUD carries over.
        HideBarricadeUI();

        if (string.IsNullOrEmpty(day1SceneName))
        {
            Debug.LogError(
                "TutorialManager: day1SceneName is empty!"
            );

            return;
        }

        // Use the fade manager if it survived from the menu scene.
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.StartTransition(day1SceneName);
        }
        else
        {
            Debug.LogWarning(
                "TutorialManager: No SceneTransitionManager found, loading directly."
            );

            SceneManager.LoadScene(day1SceneName);
        }
    }

    Transform FindOpenMonsterSpawn()
    {
        Transform doorPlacePoint =
            FindDoorPlacePoint();

        if (
            monsterSpawnDoor != null &&
            doorPlacePoint != null &&
            !IsFurnitureAtPlacePoint(
                doorPlacePoint
            )
        )
        {
            Debug.Log(
                "Monster spawn selected: DOOR"
            );

            return monsterSpawnDoor;
        }

        WindowBarricade window1Barricade = GetWindowBarricade(window1);

        if (
            monsterSpawnWindow1 != null &&
            window1Barricade != null &&
            !window1Barricade.IsBarricaded() &&
            !IsFurnitureAtPlacePoint(window1)
        )
        {
            Debug.Log(
                "Monster spawn selected: WINDOW 1"
            );

            return monsterSpawnWindow1;
        }

        WindowBarricade window2Barricade = GetWindowBarricade(window2);

        if (
            monsterSpawnWindow2 != null &&
            window2Barricade != null &&
            !window2Barricade.IsBarricaded() &&
            !IsFurnitureAtPlacePoint(window2)
        )
        {
            Debug.Log(
                "Monster spawn selected: WINDOW 2"
            );

            return monsterSpawnWindow2;
        }

        return null;
    }

    // Looks up the WindowBarricade component from a PlacePoint Transform,
    // the same way BarricadeDetector does.
    WindowBarricade GetWindowBarricade(Transform placePoint)
    {
        if (placePoint == null)
        {
            return null;
        }

        return placePoint.GetComponentInParent<WindowBarricade>();
    }

    Transform FindDoorPlacePoint()
    {
        GameObject door =
            GameObject.Find("Door");

        if (door == null)
        {
            Debug.LogWarning(
                "TutorialManager: Door GameObject not found."
            );

            return null;
        }

        Transform placePoint =
            door.transform.Find("Placepoint");

        if (placePoint == null)
        {
            Debug.LogWarning(
                "TutorialManager: Door Placepoint not found."
            );
        }

        return placePoint;
    }

    bool IsFurnitureAtPlacePoint(
        Transform placePoint
    )
    {
        if (placePoint == null)
        {
            return false;
        }

        Collider[] colliders =
            Physics.OverlapSphere(
                placePoint.position,
                furnitureCheckRadius
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

    void SpawnMonster(
        Transform spawnPoint
    )
    {
        if (monster == null)
        {
            Debug.LogError(
                "TutorialManager: Monster is NOT assigned!"
            );

            return;
        }

        if (spawnPoint == null)
        {
            Debug.LogError(
                "TutorialManager: Monster Spawn Point is missing!"
            );

            return;
        }

        monster.transform.position =
            spawnPoint.position;

        monster.transform.rotation =
            spawnPoint.rotation;

        monster.SetActive(true);

        Debug.Log(
            "Monster appeared at: " +
            spawnPoint.name
        );
    }

    void DisableAllArrows()
    {
        if (furnitureArrow != null)
        {
            furnitureArrow.SetActive(false);
        }

        if (doorArrow != null)
        {
            doorArrow.SetActive(false);
        }

        if (window1Arrow != null)
        {
            window1Arrow.SetActive(false);
        }

        if (window2Arrow != null)
        {
            window2Arrow.SetActive(false);
        }
    }

    IEnumerator FadeOutTutorial()
    {
        if (tutorialCanvasGroup == null)
        {
            if (tutorialPanel != null)
            {
                tutorialPanel.SetActive(false);
            }

            yield break;
        }

        float duration = 0.5f;
        float timer = 0f;

        float startingAlpha =
            tutorialCanvasGroup.alpha;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float progress =
                timer / duration;

            tutorialCanvasGroup.alpha =
                Mathf.Lerp(
                    startingAlpha,
                    0f,
                    progress
                );

            yield return null;
        }

        tutorialCanvasGroup.alpha = 0f;

        tutorialCanvasGroup.interactable = false;
        tutorialCanvasGroup.blocksRaycasts = false;

        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
        }

        Debug.Log(
            "Tutorial UI closed."
        );
    }
}