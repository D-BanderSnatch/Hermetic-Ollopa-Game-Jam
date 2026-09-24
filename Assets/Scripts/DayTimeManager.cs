using UnityEngine;
using TMPro;

public class DayTimeManager : MonoBehaviour
{
    [Header("Clock UI")]
    public TMP_Text timeText;

    [Header("Day Settings")]
    public float realDayDuration = 150f;

    private float elapsedTime = 0f;
    private bool dayRunning = true;

    private const int START_TIME = 6 * 60;
    private const int END_TIME = 24 * 60;

    void Start()
    {
        elapsedTime = 0f;
        dayRunning = true;

        UpdateClock();
    }

    void Update()
    {
        if (!dayRunning)
        {
            return;
        }

        elapsedTime += Time.deltaTime;

        if (elapsedTime >= realDayDuration)
        {
            elapsedTime = realDayDuration;

            UpdateClock();

            dayRunning = false;

            Debug.Log("DAY COMPLETE - 12:00 AM");

            // Find WHATEVER manager is in this scene (Day1GameManager,
            // Day2GameManager, Day3GameManager, etc.) as long as it
            // implements IGameOverHandler. No more hardcoding a specific day.
            MonoBehaviour[] allObjects =
                FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);

            foreach (MonoBehaviour obj in allObjects)
            {
                if (obj is IGameOverHandler handler)
                {
                    handler.DayComplete();
                    break;
                }
            }

            return;
        }

        UpdateClock();
    }

    void UpdateClock()
    {
        float progress =
            elapsedTime / realDayDuration;

        int currentHour =
            Mathf.FloorToInt(
                progress * 18f
            );

        int gameHour =
            6 + currentHour;

        if (gameHour >= 24)
        {
            gameHour = 24;
        }

        string period;
        int displayHour;

        if (gameHour == 24)
        {
            displayHour = 12;
            period = "AM";
        }
        else if (gameHour < 12)
        {
            displayHour = gameHour;
            period = "AM";
        }
        else if (gameHour == 12)
        {
            displayHour = 12;
            period = "PM";
        }
        else
        {
            displayHour = gameHour - 12;
            period = "PM";
        }

        if (timeText != null)
        {
            timeText.text =
                displayHour.ToString("00") +
                ":00 " +
                period;
        }
    }

    public void StopDay()
    {
        dayRunning = false;
    }
}