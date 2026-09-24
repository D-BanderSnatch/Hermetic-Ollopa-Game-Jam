using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    public Image slotIcon;

    // We now store a NAME, not a direct GameObject reference.
    // This avoids Unity's self-reference remapping bug when items get cloned.
    private string currentItemName;

    private bool hasItem = false;

    void Awake()
    {
        Instance = this;
    }

    public bool HasItem()
    {
        return hasItem;
    }

    public bool AddItem(Sprite itemIcon, string itemName)
    {
        if (hasItem)
        {
            Debug.Log("Inventory is already full!");
            return false;
        }

        hasItem = true;
        currentItemName = itemName;

        Debug.Log("ADD ITEM - Stored item name: " + itemName);

        if (slotIcon != null)
        {
            slotIcon.sprite = itemIcon;
            slotIcon.color = Color.white;
        }

        Debug.Log("Item added to inventory!");

        return true;
    }

    public GameObject GetCurrentPrefab()
    {
        if (string.IsNullOrEmpty(currentItemName))
        {
            Debug.Log("GET PREFAB - No item name stored.");
            return null;
        }

        GameObject prefab = ItemDatabase.Instance.GetPrefab(currentItemName);

        Debug.Log("GET PREFAB - Looked up '" + currentItemName + "' -> " + (prefab != null ? prefab.name : "NULL"));

        return prefab;
    }

    public void ClearItem()
    {
        hasItem = false;
        currentItemName = null;

        Debug.Log("CLEAR ITEM CALLED");

        if (slotIcon != null)
        {
            slotIcon.sprite = null;
            slotIcon.color = new Color(1, 1, 1, 0);
        }

        Debug.Log("Inventory is empty!");
    }
}