using UnityEngine;

public class OccasionalSound : MonoBehaviour
{
    private AudioSource audioSource;
    private float timer;
    private float nextPlayTime;

    [Header("Time Range in Seconds")]
    public float minTime = 5f;
    public float maxTime = 15f;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        SetNextTime();
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= nextPlayTime)
        {
            audioSource.Play();
            SetNextTime();
            timer = 0f;
        }
    }

    void SetNextTime()
    {
        nextPlayTime = Random.Range(minTime, maxTime);
    }
}
