using UnityEngine;
using TMPro;

public class BarricadeInventory : MonoBehaviour
{
    public static BarricadeInventory Instance;

    [Header("Barricade Items")]
    public int barricadeCount = 0;

    [Header("UI - Top Right Icon + Count")]
    public GameObject barricadeUI;
    public TMP_Text countText;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        UpdateUI();
    }

    public void AddBarricade(int amount)
    {
        barricadeCount += amount;

        Debug.Log(
            "Barricades received: " + amount +
            " | Total: " + barricadeCount
        );

        UpdateUI();
    }

    public bool HasBarricade()
    {
        return barricadeCount > 0;
    }

    public bool UseBarricade()
    {
        if (barricadeCount <= 0)
        {
            return false;
        }

        barricadeCount--;

        Debug.Log(
            "Barricade used! Remaining: " +
            barricadeCount
        );

        UpdateUI();

        return true;
    }

    void UpdateUI()
    {
        // ALWAYS keep the barricade UI visible
        if (barricadeUI != null)
        {
            barricadeUI.SetActive(true);
        }

        // Always show the current number
        if (countText != null)
        {
            countText.text = barricadeCount.ToString();
        }
    }

    // ==============================================
    // CALLED FROM GameManager.PlayerLost() -
    // hides the panel specifically on fail state
    // ==============================================

    public void HidePanel()
    {
        if (barricadeUI != null)
        {
            barricadeUI.SetActive(false);
        }
    }
}