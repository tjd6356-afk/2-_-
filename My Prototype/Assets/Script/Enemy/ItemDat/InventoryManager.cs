using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance
    {
        get;
        private set;
    }


    // =========================================================
    // Inventory Slot
    // =========================================================

    [Serializable]
    public class InventorySlot
    {
        [SerializeField]
        private ItemData itemData;

        [SerializeField]
        private int amount;


        public ItemData ItemData =>
            itemData;


        public int Amount =>
            amount;


        public InventorySlot(
            ItemData data,
            int initialAmount
        )
        {
            itemData =
                data;

            amount =
                initialAmount;
        }


        public void AddAmount(
            int value
        )
        {
            amount +=
                value;
        }
    }


    // =========================================================
    // Inventory
    // =========================================================

    [Header("Inventory")]

    [SerializeField]
    private List<InventorySlot> items =
        new List<InventorySlot>();


    public IReadOnlyList<InventorySlot> Items =>
        items;


    public event Action OnInventoryChanged;


    // =========================================================
    // Singleton
    // =========================================================

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);

            return;
        }


        Instance =
            this;


        DontDestroyOnLoad(
            gameObject
        );
    }


    // =========================================================
    // Add Item
    // =========================================================

    public bool AddItem(
        ItemData itemData,
        int amount = 1
    )
    {
        if (itemData == null)
            return false;


        if (amount <= 0)
            return false;


        InventorySlot existingSlot =
            FindSlot(
                itemData
            );


        // ==========================================
        // 기존 아이템이 있다면 수량 증가
        // ==========================================

        if (existingSlot != null)
        {
            existingSlot.AddAmount(
                amount
            );
        }

        // ==========================================
        // 없다면 새 Slot
        // ==========================================

        else
        {
            InventorySlot newSlot =
                new InventorySlot(
                    itemData,
                    amount
                );


            items.Add(
                newSlot
            );
        }


        Debug.Log(
            $"[Inventory] 아이템 획득 : " +
            $"{itemData.ItemTitle} x{amount}"
        );


        OnInventoryChanged?.Invoke();


        return true;
    }


    // =========================================================
    // Find
    // =========================================================

    private InventorySlot FindSlot(
        ItemData itemData
    )
    {
        foreach (
            InventorySlot slot in items)
        {
            if (slot == null ||
                slot.ItemData == null)
            {
                continue;
            }


            if (slot.ItemData.ItemId ==
                itemData.ItemId)
            {
                return slot;
            }
        }


        return null;
    }
}