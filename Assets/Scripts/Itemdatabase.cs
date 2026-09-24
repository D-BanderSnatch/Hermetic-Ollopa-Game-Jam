using UnityEngine;
using System.Collections.Generic;

public class ItemDatabase : MonoBehaviour
{
    public static ItemDatabase Instance;

    [System.Serializable]
    public class ItemEntry
    {
        public string itemName;   // e.g. "Cabinet", "Table"
        public GameObject prefab; // drag the REAL prefab asset here, once, in the Inspector
    }

    [Header("Register every furniture type here ONCE")]
    public List<ItemEntry> items = new List<ItemEntry>();

    void Awake()
    {
        Instance = this;
    }

    public GameObject GetPrefab(string itemName)
    {
        foreach (ItemEntry entry in items)
        {
            if (entry.itemName == itemName)
            {
                return entry.prefab;
            }
        }

        Debug.LogError("No prefab registered in ItemDatabase for: " + itemName);
        return null;
    }
}