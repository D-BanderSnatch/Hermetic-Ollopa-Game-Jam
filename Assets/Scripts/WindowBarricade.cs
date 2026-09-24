using UnityEngine;

public class WindowBarricade : MonoBehaviour
{
    [Header("Furniture Place Point")]
    public Transform placePoint;

    [Header("Barricade Detection Point")]
    public Transform barricadePlacePoint;

    [Header("Barricade Object")]
    public Transform barricadePoint;
    public GameObject barricadePrefab;

    [Header("Barricade Rotation")]
    public Vector3 barricadeRotation = new Vector3(0f, 90f, 180f);

    private bool isBarricaded = false;

    // Fired right after ANY WindowBarricade in the scene gets successfully
    // barricaded. Listen to this instead of polling every frame if you need
    // to react the instant barricading finishes (e.g. Day5GameManager checking
    // whether every window is now done).
    public static event System.Action OnWindowBarricaded;


    public bool IsBarricaded()
    {
        return isBarricaded;
    }


    public bool Barricade()
    {
        if (isBarricaded)
        {
            return false;
        }

        if (BarricadeInventory.Instance == null)
        {
            Debug.LogError("BarricadeInventory is missing!");
            return false;
        }

        if (!BarricadeInventory.Instance.HasBarricade())
        {
            Debug.Log("You don't have a barricade item.");
            return false;
        }

        bool used =
            BarricadeInventory.Instance.UseBarricade();

        if (!used)
        {
            return false;
        }


        if (barricadePrefab != null)
        {
            // Position comes ONLY from Barricade Point
            Transform spawnPoint =
                barricadePoint != null
                    ? barricadePoint
                    : barricadePlacePoint;

            if (spawnPoint != null)
            {
                Quaternion rotation =
                    Quaternion.Euler(
                        0f,
                        90f,
                        180f
                    );

                Instantiate(
                    barricadePrefab,
                    spawnPoint.position,
                    rotation
                );
            }
        }


        isBarricaded = true;

        Debug.Log(
            gameObject.name +
            " is now permanently barricaded!"
        );

        OnWindowBarricaded?.Invoke();

        return true;
    }
}