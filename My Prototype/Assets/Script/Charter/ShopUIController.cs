using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShopUIController : MonoBehaviour
{
    // =========================================================
    // UI
    // =========================================================

    [Header("Shop UI")]

    [SerializeField]
    private GameObject shopRoot;


    [SerializeField]
    private Transform contentRoot;


    [SerializeField]
    private ShopItemButtonUI itemButtonPrefab;


    [SerializeField]
    private Button closeButton;


    // =========================================================
    // Runtime
    // =========================================================

    private readonly List<GameObject>
        generatedButtons =
            new List<GameObject>();


    private MerchantNPCData currentMerchant;


    private Action<bool> onShopClosed;


    private bool purchasedAnyItem;

    private bool isOpen;


    public bool IsOpen =>
        isOpen;


    // =========================================================
    // Awake
    // =========================================================

    private void Awake()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(
                Close
            );
        }
    }


    // =========================================================
    // Start
    // =========================================================

    private void Start()
    {
        if (shopRoot != null)
        {
            shopRoot.SetActive(
                false
            );
        }
    }


    // =========================================================
    // Open
    // =========================================================

    public void Open(
        MerchantNPCData merchant,
        Action<bool> closedCallback
    )
    {
        if (merchant == null)
            return;


        currentMerchant =
            merchant;


        onShopClosed =
            closedCallback;


        purchasedAnyItem =
            false;


        isOpen =
            true;


        if (shopRoot != null)
        {
            shopRoot.SetActive(
                true
            );


            shopRoot.transform.SetAsLastSibling();
        }


        BuildShop();
    }


    // =========================================================
    // Shop List
    // =========================================================

    private void BuildShop()
    {
        ClearButtons();


        if (currentMerchant == null ||
            itemButtonPrefab == null ||
            contentRoot == null)
        {
            return;
        }


        foreach (
            MerchantShopEntry entry
            in currentMerchant.ShopItems)
        {
            if (entry == null ||
                entry.ItemData == null)
            {
                continue;
            }


            ShopItemButtonUI button =
                Instantiate(
                    itemButtonPrefab,
                    contentRoot
                );


            button.Setup(
                entry,
                this
            );


            generatedButtons.Add(
                button.gameObject
            );
        }
    }


    // =========================================================
    // Purchase
    // =========================================================

    public void PurchaseItem(
        MerchantShopEntry entry
    )
    {
        if (entry == null ||
            entry.ItemData == null)
        {
            return;
        }


        if (InventoryManager.Instance == null)
        {
            Debug.LogError(
                "[Shop] InventoryManager를 찾을 수 없습니다."
            );

            return;
        }


        // =====================================================
        // 현재는 돈 / 재료 / 조건 없음
        // 무조건 구매 성공
        // =====================================================

        InventoryManager.Instance.AddItem(
            entry.ItemData,
            entry.Amount
        );


        purchasedAnyItem =
            true;


        Debug.Log(
            $"[Shop] 구입 : " +
            $"{entry.ItemData.ItemTitle} x{entry.Amount}"
        );
    }


    // =========================================================
    // Close
    // =========================================================

    public void Close()
    {
        if (!isOpen)
            return;


        isOpen =
            false;


        if (shopRoot != null)
        {
            shopRoot.SetActive(
                false
            );
        }


        ClearButtons();


        Action<bool> callback =
            onShopClosed;


        bool result =
            purchasedAnyItem;


        currentMerchant =
            null;


        onShopClosed =
            null;


        purchasedAnyItem =
            false;


        callback?.Invoke(
            result
        );
    }


    // =========================================================
    // Clear
    // =========================================================

    private void ClearButtons()
    {
        foreach (
            GameObject button
            in generatedButtons)
        {
            if (button != null)
            {
                Destroy(
                    button
                );
            }
        }


        generatedButtons.Clear();
    }
}