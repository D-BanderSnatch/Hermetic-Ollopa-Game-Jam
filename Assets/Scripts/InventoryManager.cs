using UnityEngine;
using TMPro;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    public TextMeshProUGUI inventoryText;

    private void Awake()
    {
        Instance = this;
    }

    public void AddItem(string itemName)
    {
        inventoryText.text += itemName + "\n";
    }
}
