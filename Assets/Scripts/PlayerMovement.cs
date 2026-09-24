using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private Animator Estheranim;
    [SerializeField] private SpriteRenderer spriteRenderer;
    public float speed = 5f;
    private CharacterController controller;

    [Header("Footsteps")]
    [Tooltip("AudioSource used to play footstep sounds. Add an AudioSource component to the player and assign it here.")]
    public AudioSource footstepAudioSource;
    [Tooltip("One or more footstep clips - a random one plays each step, so it doesn't sound too repetitive.")]
    public AudioClip[] footstepClips;
    [Tooltip("Time between footstep sounds while moving, in seconds.")]
    public float footstepInterval = 0.35f;
    [Tooltip("Random pitch variation applied to each footstep, e.g. 0.1 = ±10%.")]
    public float footstepPitchVariation = 0.1f;

    private float footstepTimer = 0f;
    private bool wasMoving = false;
    [Tooltip("How long the pickup/place animation lasts, in seconds.")]
    public float pickupDuration = 0.6f;

    [Header("Barricade")]
    [Tooltip("How long the barricading animation lasts, in seconds.")]
    public float barricadeDuration = 0.6f;

    private bool isPickingUp = false;
    private bool isBarricading = false;

    [Header("Facing Direction")]
    [Tooltip("Logical facing direction, kept in sync with the sprite flip. 1 = facing right, -1 = facing left. " +
             "Read this from other scripts (e.g. PickupDetector) instead of transform.right, since the player " +
             "sprite flips via SpriteRenderer.flipX rather than rotation.")]
    [HideInInspector]
    public int facingDirection = 1; // 1 = facing right, -1 = facing left

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    /// <summary>
    /// Call this from PickupDetector to play the pickup/place animation and block movement.
    /// The optional onComplete callback fires once the animation duration has elapsed —
    /// put the actual "take the item" / "place the item" logic there, not before calling this.
    /// Returns true if the animation actually started (false if one was already playing).
    /// </summary>
    public bool TriggerPickupAnimation(System.Action onComplete = null)
    {
        if (isPickingUp)
        {
            return false;
        }

        StartCoroutine(DoPickup(onComplete));
        return true;
    }

    /// <summary>
    /// Call this from BarricadeDetector to play the barricading animation and block movement.
    /// The optional onComplete callback fires once the animation duration has elapsed —
    /// put the actual "apply the barricade" logic there, not before calling this.
    /// Returns true if the animation actually started (false if one was already playing).
    /// </summary>
    public bool TriggerBarricadeAnimation(System.Action onComplete = null)
    {
        if (isBarricading)
        {
            return false;
        }

        StartCoroutine(DoBarricade(onComplete));
        return true;
    }

    void Update()
    {
        // Freeze all input/animation/footstep logic while paused — Time.timeScale
        // alone doesn't stop this, since Update() and raw input reads aren't scaled.
        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused)
        {
            return;
        }

        float moveX = 0f;
        bool isRunning = false;

        // Don't allow movement while picking up/placing or barricading.
        if (!isPickingUp && !isBarricading && Keyboard.current != null)
        {
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            {
                moveX = 1f;
                isRunning = true;
            }
            else if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            {
                moveX = -1f;
                isRunning = true;
            }
        }

        Vector3 move = new Vector3(moveX, 0, 0) * speed * Time.deltaTime;
        controller.Move(move);

        HandleFootsteps(isRunning);

        Estheranim.SetBool("isRunning", isRunning);
        Estheranim.SetBool("isPickingup", isPickingUp);
        Estheranim.SetBool("isBarricading", isBarricading);

        // Keep the sprite flip AND the logical facing direction in sync,
        // updated together in the same frame. PickupDetector reads
        // facingDirection instead of transform.right, since this player
        // never rotates — only the sprite flips.
        if (moveX > 0f)
        {
            spriteRenderer.flipX = false;
            facingDirection = 1;
        }
        else if (moveX < 0f)
        {
            spriteRenderer.flipX = true;
            facingDirection = -1;
        }
        // if moveX == 0 (standing still), keep whatever facingDirection already was.
    }

    private void HandleFootsteps(bool isMoving)
    {
        if (!isMoving)
        {
            // Reset so the first step after stopping/starting again
            // doesn't play instantly if timer already happened to be near 0.
            footstepTimer = footstepInterval;
            wasMoving = false;
            return;
        }

        // Just started moving this frame - play a step right away instead of
        // waiting for the timer to count down first.
        if (!wasMoving)
        {
            PlayFootstepSound();
            footstepTimer = footstepInterval;
            wasMoving = true;
            return;
        }

        footstepTimer -= Time.deltaTime;

        if (footstepTimer <= 0f)
        {
            PlayFootstepSound();
            footstepTimer = footstepInterval;
        }
    }

    private void PlayFootstepSound()
    {
        if (footstepAudioSource == null || footstepClips == null || footstepClips.Length == 0)
        {
            return;
        }

        AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];

        footstepAudioSource.pitch = 1f + Random.Range(-footstepPitchVariation, footstepPitchVariation);
        footstepAudioSource.PlayOneShot(clip);
    }

    private IEnumerator DoPickup(System.Action onComplete)
    {
        isPickingUp = true;
        yield return new WaitForSeconds(pickupDuration);
        isPickingUp = false;
        onComplete?.Invoke();
    }

    private IEnumerator DoBarricade(System.Action onComplete)
    {
        isBarricading = true;
        yield return new WaitForSeconds(barricadeDuration);
        isBarricading = false;
        onComplete?.Invoke();
    }
}