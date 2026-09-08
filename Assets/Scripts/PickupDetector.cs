using UnityEngine;
// 1. Add the new Input System namespace
using UnityEngine.InputSystem; 

public class PickupDetector : MonoBehaviour
{
    public GameObject pickupPrompt;
    public float pickupDistance = 3f;

    private GameObject nearbyItem;

    private void Start()
    {
        if (pickupPrompt != null)
            pickupPrompt.SetActive(false);
    }

    private void Update()
    {
        GameObject closestItem = FindClosestPickup();

        if (closestItem != null)
        {
            nearbyItem = closestItem;

            if (pickupPrompt != null)
                pickupPrompt.SetActive(true);

            // 2. Read the specific key frame state (or check Gamepad button)
            bool interactPressed = false;

            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                interactPressed = true;
            }
            else if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
            {
                // Optional: Adds controller support (A button on Xbox / X button on PlayStation)
                interactPressed = true;
            }

            if (interactPressed)
            {
                PickUp();
            }
        }
        else
        {
            nearbyItem = null;

            if (pickupPrompt != null)
                pickupPrompt.SetActive(false);
        }
    }

    private GameObject FindClosestPickup()
    {
        GameObject[] pickups = GameObject.FindGameObjectsWithTag("Pickup");

        GameObject closest = null;
        float closestDistance = pickupDistance;

        foreach (GameObject pickup in pickups)
        {
            float distance = Vector3.Distance(
                transform.position,
                pickup.transform.position
            );

            if (distance <= closestDistance)
            {
                closest = pickup;
                closestDistance = distance;
            }
        }

        return closest;
    }

    private void PickUp()
    {
        if (nearbyItem == null)
            return;

        // Add item name to inventory
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AddItem(nearbyItem.name);
        }

        Debug.Log("Picked up " + nearbyItem.name);

        Destroy(nearbyItem);

        nearbyItem = null;

        if (pickupPrompt != null)
            pickupPrompt.SetActive(false);
    }
}
