using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerHeldItemController : MonoBehaviour
{
    public static PlayerHeldItemController Instance
    {
        get;
        private set;
    }


    [Header("Hold")]

    [SerializeField]
    private int maxHeldItems = 3;


    [SerializeField]
    private List<Transform> holdPoints =
        new List<Transform>();


    private class HeldItem
    {
        public ItemData data;

        public GameObject visual;
    }


    private readonly List<HeldItem> heldItems =
        new List<HeldItem>();


    public bool HasHeldItems =>
        heldItems.Count > 0;


    public int HeldCount =>
        heldItems.Count;


    private void Awake()
    {
        Instance =
            this;
    }


    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance =
                null;
        }
    }


    // =========================================================
    // Input
    // =========================================================

    private void Update()
    {
        if (!HasHeldItems)
            return;


        // Inventory 등의 UI가 열린 상태에서는
        // R/E 입력을 처리하지 않음
        if (PlayerGameplayControl.GameplayLocked)
            return;


        if (Keyboard.current == null)
            return;


        // ==========================================
        // R = 들고 있는 모든 Item 버리기
        // ==========================================

        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            DropAll();

            return;
        }


        // ==========================================
        // E = 들고 있는 모든 Item 다시 Inventory
        // ==========================================

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            ReturnAllToInventory();
        }
    }


    // =========================================================
    // Hold
    // =========================================================

    public bool TryHold(
        ItemData itemData
    )
    {
        if (itemData == null)
            return false;


        if (heldItems.Count >=
            maxHeldItems)
        {
            return false;
        }


        if (heldItems.Count >=
            holdPoints.Count)
        {
            Debug.LogError(
                "[HeldItem] HoldPoint가 부족합니다."
            );

            return false;
        }


        if (InventoryManager.Instance == null)
            return false;


        // Inventory에서 1개 제거
        if (!InventoryManager.Instance.RemoveItem(
                itemData,
                1))
        {
            return false;
        }


        Transform point =
            holdPoints[
                heldItems.Count
            ];


        GameObject prefab =
            itemData.HeldVisualPrefab;


        // 별도 Held Prefab이 없다면 Pickup 모델 사용
        if (prefab == null &&
            itemData.WorldPickupPrefab != null)
        {
            prefab =
                itemData.WorldPickupPrefab.gameObject;
        }


        if (prefab == null)
        {
            InventoryManager.Instance.AddItem(
                itemData,
                1
            );

            return false;
        }


        GameObject visual =
            Instantiate(
                prefab,
                point
            );


        visual.transform.localPosition =
            Vector3.zero;


        visual.transform.localRotation =
            Quaternion.identity;


        // ==========================================
        // 들고 있는 상태에서는 Pickup 기능 OFF
        // ==========================================

        ItemPickup[] pickups =
            visual.GetComponentsInChildren<ItemPickup>(
                true
            );


        foreach (ItemPickup pickup in pickups)
        {
            pickup.enabled =
                false;
        }


        Collider[] colliders =
            visual.GetComponentsInChildren<Collider>(
                true
            );


        foreach (Collider collider in colliders)
        {
            collider.enabled =
                false;
        }


        Rigidbody[] rigidbodies =
            visual.GetComponentsInChildren<Rigidbody>(
                true
            );


        foreach (Rigidbody rb in rigidbodies)
        {
            rb.useGravity =
                false;

            rb.isKinematic =
                true;

            rb.detectCollisions =
                false;
        }


        HeldItem held =
            new HeldItem
            {
                data =
                    itemData,

                visual =
                    visual
            };


        heldItems.Add(
            held
        );


        Debug.Log(
            $"[HeldItem] 손에 들기 : " +
            $"{itemData.ItemTitle} " +
            $"({heldItems.Count}/{maxHeldItems})"
        );


        return true;
    }


    // =========================================================
    // R : 전부 버리기
    // =========================================================

    public void DropAll()
    {
        if (heldItems.Count == 0)
            return;


        List<HeldItem> copy =
            new List<HeldItem>(
                heldItems
            );


        heldItems.Clear();


        for (int i = 0;
             i < copy.Count;
             i++)
        {
            HeldItem held =
                copy[i];


            if (held.visual != null)
            {
                Destroy(
                    held.visual
                );
            }


            Vector3 dropPosition =
                transform.position +
                transform.forward * 1.5f +
                transform.right *
                (
                    (i - (copy.Count - 1) * 0.5f)
                    * 0.7f
                ) +
                Vector3.up * 0.3f;


            ItemPickup pickup =
                ItemWorldSpawner.Spawn(
                    held.data,
                    dropPosition,
                    1
                );


            // Spawn 실패 시 Inventory로 복구
            if (pickup == null &&
                InventoryManager.Instance != null)
            {
                InventoryManager.Instance.AddItem(
                    held.data,
                    1
                );
            }
        }


        Debug.Log(
            "[HeldItem] 들고 있던 모든 Item 버림"
        );
    }


    // =========================================================
    // E : 다시 Inventory
    // =========================================================

    public void ReturnAllToInventory()
    {
        if (InventoryManager.Instance == null)
            return;


        foreach (
            HeldItem held in heldItems)
        {
            InventoryManager.Instance.AddItem(
                held.data,
                1
            );


            if (held.visual != null)
            {
                Destroy(
                    held.visual
                );
            }
        }


        heldItems.Clear();


        Debug.Log(
            "[HeldItem] 모든 Item Inventory로 회수"
        );
    }
}