using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShopUIController : MonoBehaviour
{
    // =========================================================
    // Shop UI
    // =========================================================

    [Header("Shop UI")]

    [SerializeField]
    private GameObject shopRoot;


    [SerializeField]
    private Transform contentRoot;


    [Tooltip("상점 목록에 생성되는 아이템 Icon Prefab")]
    [SerializeField]
    private ShopItemIconUI itemIconPrefab;


    [SerializeField]
    private Button closeButton;


    // =========================================================
    // Selected Item
    // =========================================================

    [Header("Selected Item")]

    [Tooltip("사진의 빨간 박스 위치에 있는 큰 아이템 이미지")]
    [SerializeField]
    private Image selectedItemImage;


    [Tooltip("선택한 아이템을 실제로 구입하는 버튼")]
    [SerializeField]
    private Button buyButton;


    // =========================================================
    // Runtime
    // =========================================================

    private readonly List<GameObject>
        generatedIcons =
            new List<GameObject>();


    private MerchantNPCData currentMerchant;


    private MerchantShopEntry selectedEntry;


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
        // ==========================================
        // Close
        // ==========================================

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(
                Close
            );
        }


        // ==========================================
        // Buy
        // ==========================================

        if (buyButton != null)
        {
            buyButton.onClick.AddListener(
                PurchaseSelectedItem
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


        ClearSelection();
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


        ClearSelection();


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
        ClearGeneratedIcons();


        if (currentMerchant == null)
            return;


        if (itemIconPrefab == null)
        {
            Debug.LogError(
                "[Shop] Item Icon Prefab이 없습니다."
            );

            return;
        }


        if (contentRoot == null)
        {
            Debug.LogError(
                "[Shop] Content Root가 없습니다."
            );

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


            ShopItemIconUI icon =
                Instantiate(
                    itemIconPrefab,
                    contentRoot
                );


            icon.Setup(
                entry,
                this
            );


            generatedIcons.Add(
                icon.gameObject
            );
        }
    }


    // =========================================================
    // Item 선택
    // =========================================================

    public void SelectItem(
        MerchantShopEntry entry
    )
    {
        if (entry == null ||
            entry.ItemData == null)
        {
            return;
        }


        selectedEntry =
            entry;


        // ==========================================
        // 사진의 큰 Item Image 변경
        // ==========================================

        if (selectedItemImage != null)
        {
            selectedItemImage.sprite =
                selectedEntry.ItemData.ItemIcon;


            selectedItemImage.enabled =
                selectedEntry.ItemData.ItemIcon != null;


            selectedItemImage.gameObject.SetActive(
                true
            );
        }


        // ==========================================
        // 구입 버튼 활성화
        // ==========================================

        if (buyButton != null)
        {
            buyButton.gameObject.SetActive(
                true
            );


            buyButton.interactable =
                true;
        }


        Debug.Log(
            $"[Shop] 선택 : " +
            $"{selectedEntry.ItemData.ItemTitle}"
        );
    }


    // =========================================================
    // 구입
    // =========================================================

    private void PurchaseSelectedItem()
    {
        if (selectedEntry == null ||
            selectedEntry.ItemData == null)
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
        // 현재는 돈 / 재료 조건 없음
        // =====================================================

        InventoryManager.Instance.AddItem(
            selectedEntry.ItemData,
            selectedEntry.Amount
        );


        purchasedAnyItem =
            true;


        Debug.Log(
            $"[Shop] 구매 완료 : " +
            $"{selectedEntry.ItemData.ItemTitle} " +
            $"x{selectedEntry.Amount}"
        );


        // =====================================================
        // 구매 후 상세 이미지 제거
        // =====================================================

        ClearSelection();
    }


    // =========================================================
    // 선택 초기화
    // =========================================================

    private void ClearSelection()
    {
        selectedEntry =
            null;


        // ==========================================
        // Item Image 제거
        // ==========================================

        if (selectedItemImage != null)
        {
            selectedItemImage.sprite =
                null;


            selectedItemImage.enabled =
                false;


            selectedItemImage.gameObject.SetActive(
                false
            );
        }


        // ==========================================
        // Buy Button 숨김
        // ==========================================

        if (buyButton != null)
        {
            buyButton.gameObject.SetActive(
                false
            );
        }
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


        ClearSelection();

        ClearGeneratedIcons();


        Action<bool> callback =
            onShopClosed;


        bool purchased =
            purchasedAnyItem;


        currentMerchant =
            null;


        onShopClosed =
            null;


        purchasedAnyItem =
            false;


        callback?.Invoke(
            purchased
        );
    }


    // =========================================================
    // 생성된 Icon 제거
    // =========================================================

    private void ClearGeneratedIcons()
    {
        foreach (
            GameObject icon
            in generatedIcons)
        {
            if (icon != null)
            {
                Destroy(
                    icon
                );
            }
        }


        generatedIcons.Clear();
    }
}