using UnityEngine;
using UnityEngine.InputSystem;

public class ItemPickup : MonoBehaviour
{
    // =========================================================
    // Item
    // =========================================================

    [Header("Item")]

    [SerializeField]
    private ItemData itemData;


    [Min(1)]
    [SerializeField]
    private int amount = 1;


    // =========================================================
    // Runtime
    // =========================================================

    private bool playerInside;

    private bool collected;


    public ItemData Data =>
        itemData;


    public void SetAmount(
        int newAmount
    )
    {
        amount =
            Mathf.Max(
                1,
                newAmount
            );
    }

    public void Initialize(
    ItemData data,
    int newAmount = 1
)
    {
        itemData =
            data;


        amount =
            Mathf.Max(
                1,
                newAmount
            );
    }

    // =========================================================
    // Input
    // =========================================================

    private void Update()
    {
        if (!playerInside)
            return;


        if (collected)
            return;


        if (Keyboard.current == null)
            return;

        if (PlayerHeldItemController.Instance != null &&
        PlayerHeldItemController.Instance.HasHeldItems)
        {
            return;
        }

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            CollectItem();
        }
    }


    // =========================================================
    // Collect
    // =========================================================

    private void CollectItem()
    {
        if (itemData == null)
        {
            Debug.LogError(
                $"[ItemPickup] ItemData가 없습니다 : {gameObject.name}"
            );

            return;
        }


        if (InventoryManager.Instance == null)
        {
            Debug.LogError(
                "[ItemPickup] InventoryManager가 없습니다."
            );

            return;
        }


        bool success =
            InventoryManager.Instance.AddItem(
                itemData,
                amount
            );


        if (!success)
            return;


        collected =
            true;


        Debug.Log(
            $"[ItemPickup] 수집 : " +
            $"{itemData.ItemTitle} x{amount}"
        );


        Destroy(
            gameObject
        );
    }


    // =========================================================
    // Trigger
    // =========================================================

    private void OnTriggerEnter(
        Collider other
    )
    {
        PlayerStats player =
            other.GetComponentInParent<PlayerStats>();


        if (player == null)
            return;


        playerInside =
            true;


        Debug.Log(
            $"[ItemPickup] E키로 획득 가능 : " +
            $"{itemData?.ItemTitle}"
        );
    }


    private void OnTriggerExit(
        Collider other
    )
    {
        PlayerStats player =
            other.GetComponentInParent<PlayerStats>();


        if (player == null)
            return;


        playerInside =
            false;
    }
}