using UnityEngine;
using System.Collections.Generic;

public class Day1Monster : MonoBehaviour
{
    [System.Serializable]
    public class AttackPoint
    {
        public string pointName;

        // Where the monster visually appears
        public Transform monsterSpot;

        // Furniture placement point
        public Transform checkPoint;
    }

    [Header("Monster Start")]
    public float startDelay = 10f;

    [Header("Protection")]
    public float protectionTime = 15f;

    [Header("Wait Before New Point")]
    public float waitTime = 5f;

    [Header("Attack Points")]
    public AttackPoint[] attackPoints;

    [Header("Sound")]
    [Tooltip("AudioSource used to play the monster's appear sound cue. Should be on the monster itself (or a child), so the sound comes from wherever it currently is.")]
    public AudioSource audioSource;
    [Tooltip("One or more sounds (knock, growl, scratching, etc.) - a random one plays each time the monster appears at a new attack point.")]
    public AudioClip[] appearSounds;
    [Tooltip("Random pitch variation applied to each appear sound, e.g. 0.1 = ±10%.")]
    public float appearSoundPitchVariation = 0.1f;

    private AttackPoint currentPoint;

    private static HashSet<Transform> occupiedPoints =
        new HashSet<Transform>();

    private float startTimer;
    private float protectionTimer;
    private float waitTimer;

    private bool monsterStarted = false;
    private bool waitingForBlock = false;
    private bool waitingForNextPoint = false;
    private bool gameOver = false;

    private Renderer[] monsterRenderers;


    // =========================================================
    // START
    // =========================================================

    void Start()
    {
        startTimer = startDelay;

        monsterRenderers =
            GetComponentsInChildren<Renderer>();

        SetVisible(false);
    }


    // =========================================================
    // UPDATE
    // =========================================================

    void Update()
    {
        if (gameOver)
        {
            return;
        }


        // =====================================================
        // WAIT 10 SECONDS BEFORE MONSTER STARTS
        // =====================================================

        if (!monsterStarted)
        {
            startTimer -= Time.deltaTime;

            if (startTimer <= 0f)
            {
                StartMonster();
            }

            return;
        }


        // =====================================================
        // WAIT 5 SECONDS BEFORE NEXT POINT
        // =====================================================

        if (waitingForNextPoint)
        {
            waitTimer -= Time.deltaTime;

            if (waitTimer <= 0f)
            {
                waitingForNextPoint = false;

                SpawnAtNewPoint();
            }

            return;
        }


        // =====================================================
        // MONSTER IS WAITING FOR PLAYER TO BLOCK
        // =====================================================

        if (waitingForBlock)
        {
            CheckProtection();

            return;
        }
    }


    // =========================================================
    // START MONSTER
    // =========================================================

    void StartMonster()
    {
        monsterStarted = true;

        SetVisible(true);

        SpawnAtNewPoint();

        Debug.Log("MONSTER STARTED!");
    }


    // =========================================================
    // SHOW / HIDE MONSTER
    // =========================================================

    void SetVisible(bool visible)
    {
        if (monsterRenderers == null)
        {
            return;
        }

        foreach (Renderer r in monsterRenderers)
        {
            if (r != null)
            {
                r.enabled = visible;
            }
        }
    }


    // =========================================================
    // PLAY APPEAR SOUND CUE
    // =========================================================

    void PlayAppearSound()
    {
        if (audioSource == null || appearSounds == null || appearSounds.Length == 0)
        {
            return;
        }

        AudioClip clip = appearSounds[Random.Range(0, appearSounds.Length)];

        audioSource.pitch = 1f + Random.Range(-appearSoundPitchVariation, appearSoundPitchVariation);
        audioSource.PlayOneShot(clip);
    }


    // =========================================================
    // CHOOSE RANDOM ATTACK POINT
    // =========================================================

    void SpawnAtNewPoint()
    {
        if (attackPoints == null ||
            attackPoints.Length == 0)
        {
            Debug.LogError(
                "No attack points assigned to Day1Monster!"
            );

            return;
        }


        List<AttackPoint> available =
            new List<AttackPoint>();


        // =====================================================
        // FIND AVAILABLE POINTS
        // =====================================================

        foreach (AttackPoint point in attackPoints)
        {
            if (point == null)
            {
                continue;
            }


            // Don't immediately choose the same point
            if (point == currentPoint)
            {
                continue;
            }


            // =================================================
            // BARRICADED = PERMANENTLY SAFE
            // =================================================

            if (IsPointBarricaded(point))
            {
                Debug.Log(
                    point.pointName +
                    " is permanently barricaded. Monster will skip it."
                );

                continue;
            }


            // =================================================
            // ANOTHER MONSTER IS USING THIS POINT
            // =================================================

            if (
                point.checkPoint != null &&
                occupiedPoints.Contains(point.checkPoint)
            )
            {
                continue;
            }


            available.Add(point);
        }


        // =====================================================
        // FALLBACK
        // =====================================================

        if (available.Count == 0)
        {
            foreach (AttackPoint point in attackPoints)
            {
                if (point == null)
                {
                    continue;
                }


                if (IsPointBarricaded(point))
                {
                    continue;
                }


                if (
                    point.checkPoint != null &&
                    !occupiedPoints.Contains(point.checkPoint)
                )
                {
                    available.Add(point);
                }
            }
        }


        // =====================================================
        // NOTHING AVAILABLE
        // =====================================================

        if (available.Count == 0)
        {
            Debug.Log(
                "No available attack points. Monster waiting..."
            );

            StartWaiting();

            return;
        }


        // =====================================================
        // PICK RANDOM POINT
        // =====================================================

        AttackPoint newPoint =
            available[
                Random.Range(
                    0,
                    available.Count
                )
            ];


        // =====================================================
        // RELEASE OLD POINT
        // =====================================================

        if (
            currentPoint != null &&
            currentPoint.checkPoint != null
        )
        {
            occupiedPoints.Remove(
                currentPoint.checkPoint
            );

            SetFurnitureLocked(
                currentPoint.checkPoint,
                false
            );
        }


        // =====================================================
        // SET NEW POINT
        // =====================================================

        currentPoint = newPoint;


        if (currentPoint.checkPoint != null)
        {
            occupiedPoints.Add(
                currentPoint.checkPoint
            );
        }


        // =====================================================
        // MOVE MONSTER
        // =====================================================

        if (currentPoint.monsterSpot == null)
        {
            Debug.LogError(
                "Monster Spot is not assigned for: " +
                currentPoint.pointName
            );

            return;
        }


        transform.position =
            currentPoint.monsterSpot.position;


        Debug.Log(
            "Monster spawned at: " +
            currentPoint.pointName
        );

        PlayAppearSound();


        // =====================================================
        // CHECK IF BLOCKED
        // =====================================================

        CheckPoint();
    }


    // =========================================================
    // CHECK ATTACK POINT
    // =========================================================

    void CheckPoint()
    {
        if (IsPointBlocked())
        {
            Debug.Log(
                currentPoint.pointName +
                " is BLOCKED."
            );


            // No protection timer
            waitingForBlock = false;


            // Only lock furniture.
            // If the point is barricaded, there may be
            // no furniture to lock.
            if (!IsPointBarricaded(currentPoint))
            {
                SetFurnitureLocked(
                    currentPoint.checkPoint,
                    true
                );
            }


            // Wait 5 seconds
            StartWaiting();
        }
        else
        {
            Debug.Log(
                currentPoint.pointName +
                " is NOT BLOCKED."
            );


            // =================================================
            // ONLY NOW START 15 SECOND TIMER
            // =================================================

            protectionTimer =
                protectionTime;

            waitingForBlock = true;


            Debug.Log(
                "15 SECOND PROTECTION TIMER STARTED!"
            );
        }
    }


    // =========================================================
    // CHECK 15 SECOND PROTECTION TIMER
    // =========================================================

    void CheckProtection()
    {
        // =====================================================
        // PLAYER BLOCKED IT
        // =====================================================

        if (IsPointBlocked())
        {
            Debug.Log(
                "PLAYER BLOCKED " +
                currentPoint.pointName +
                "!"
            );


            protectionTimer = 0f;

            waitingForBlock = false;


            // Only lock furniture if furniture was used
            if (!IsPointBarricaded(currentPoint))
            {
                SetFurnitureLocked(
                    currentPoint.checkPoint,
                    true
                );
            }


            // Wait 5 seconds
            StartWaiting();

            return;
        }


        // =====================================================
        // COUNTDOWN
        // =====================================================

        protectionTimer -=
            Time.deltaTime;


        Debug.Log(
            "Protection time: " +
            Mathf.CeilToInt(
                protectionTimer
            )
        );


        // =====================================================
        // PLAYER FAILED
        // =====================================================

        if (protectionTimer <= 0f)
        {
            LoseGame();
        }
    }


    // =========================================================
    // WAIT 5 SECONDS
    // =========================================================

    void StartWaiting()
    {
        waitingForNextPoint = true;

        waitingForBlock = false;

        waitTimer = waitTime;


        Debug.Log(
            "Monster will choose another point in " +
            waitTime +
            " seconds."
        );
    }


    // =========================================================
    // IMPORTANT:
    // CHECK IF CURRENT POINT IS BLOCKED
    //
    // BLOCKED IF:
    // 1. Furniture is there
    // OR
    // 2. Window is permanently barricaded
    // =========================================================

    bool IsPointBlocked()
    {
        if (
            currentPoint == null ||
            currentPoint.checkPoint == null
        )
        {
            return false;
        }


        // =====================================================
        // CHECK BARRICADE FIRST
        // =====================================================

        if (IsPointBarricaded(currentPoint))
        {
            Debug.Log(
                currentPoint.pointName +
                " is BLOCKED BY BARRICADE!"
            );

            return true;
        }


        // =====================================================
        // CHECK FURNITURE
        // =====================================================

        PickupItem furniture =
            GetFurnitureAt(
                currentPoint.checkPoint
            );


        if (furniture != null)
        {
            Debug.Log(
                currentPoint.pointName +
                " is BLOCKED BY FURNITURE!"
            );

            return true;
        }


        return false;
    }


    // =========================================================
    // CHECK IF WINDOW IS BARRICADED
    //
    // THIS IS THE IMPORTANT FIX
    // =========================================================

    bool IsPointBarricaded(AttackPoint point)
    {
        if (
            point == null ||
            point.checkPoint == null
        )
        {
            return false;
        }


        WindowBarricade[] allWindows =
            FindObjectsByType<WindowBarricade>(
                FindObjectsSortMode.None
            );


        foreach (
            WindowBarricade window
            in allWindows
        )
        {
            if (window == null)
            {
                continue;
            }


            // Match the monster's furniture point
            // to the WindowBarricade's placePoint.
            if (
                window.placePoint ==
                point.checkPoint
            )
            {
                return window.IsBarricaded();
            }
        }


        return false;
    }


    // =========================================================
    // FIND FURNITURE AT POINT
    // =========================================================

    PickupItem GetFurnitureAt(
        Transform point
    )
    {
        if (point == null)
        {
            return null;
        }


        Collider[] objects =
            Physics.OverlapSphere(
                point.position,
                0.7f
            );


        foreach (Collider obj in objects)
        {
            if (obj == null)
            {
                continue;
            }


            // Ignore monster
            if (
                obj.transform == transform ||
                obj.transform.IsChildOf(transform)
            )
            {
                continue;
            }


            // Ignore player
            if (obj.CompareTag("Player"))
            {
                continue;
            }


            PickupItem furniture =
                obj.GetComponentInParent<PickupItem>();


            if (furniture != null)
            {
                return furniture;
            }
        }


        return null;
    }


    // =========================================================
    // LOCK / UNLOCK FURNITURE
    // =========================================================

    void SetFurnitureLocked(
        Transform point,
        bool locked
    )
    {
        PickupItem furniture =
            GetFurnitureAt(point);


        if (furniture != null)
        {
            furniture.isLocked = locked;
        }
    }


    // =========================================================
    // PLAYER LOSES
    // =========================================================

    void LoseGame()
    {
        if (gameOver)
        {
            return;
        }


        gameOver = true;


        Debug.Log(
            "PLAYER LOST! JUMPSCARE!"
        );


        MonoBehaviour[] allObjects =
            FindObjectsByType<MonoBehaviour>(
                FindObjectsSortMode.None
            );


        foreach (
            MonoBehaviour obj
            in allObjects
        )
        {
            if (
                obj is IGameOverHandler handler
            )
            {
                handler.PlayerLost();

                break;
            }
        }
    }


    // =========================================================
    // STOP MONSTER
    // =========================================================

    public void StopMonster()
    {
        gameOver = true;

        waitingForBlock = false;

        waitingForNextPoint = false;


        if (
            currentPoint != null &&
            currentPoint.checkPoint != null
        )
        {
            occupiedPoints.Remove(
                currentPoint.checkPoint
            );
        }
    }


    // =========================================================
    // CLEANUP
    // =========================================================

    void OnDestroy()
    {
        if (
            currentPoint != null &&
            currentPoint.checkPoint != null
        )
        {
            occupiedPoints.Remove(
                currentPoint.checkPoint
            );
        }
    }


    // =========================================================
    // DEBUG GIZMOS
    // =========================================================

    void OnDrawGizmos()
    {
        if (attackPoints == null)
        {
            return;
        }


        foreach (
            AttackPoint point
            in attackPoints
        )
        {
            if (point == null)
            {
                continue;
            }


            if (point.monsterSpot != null)
            {
                Gizmos.color = Color.red;

                Gizmos.DrawWireSphere(
                    point.monsterSpot.position,
                    0.3f
                );
            }


            if (point.checkPoint != null)
            {
                Gizmos.color = Color.green;

                Gizmos.DrawWireSphere(
                    point.checkPoint.position,
                    0.7f
                );
            }
        }
    }
}