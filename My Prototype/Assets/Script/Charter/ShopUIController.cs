using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static InventoryManager;

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

    // =========================================================
    // Merchant Camera
    // =========================================================

    private MerchantShopViewPoint currentViewPoint;

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

    // =========================================================
    // Open + Merchant Camera
    // =========================================================

    public void Open(
        MerchantNPCData merchant,
        Action<bool> closedCallback,
        MerchantShopViewPoint viewPoint
    )
    {
        if (merchant == null)
            return;


        currentMerchant =
            merchant;


        onShopClosed =
            closedCallback;


        currentViewPoint =
            viewPoint;


        purchasedAnyItem =
            false;


        isOpen =
            true;


        ClearSelection();


        // =====================================================
        // 상점 UI 활성화
        // =====================================================

        if (shopRoot != null)
        {
            shopRoot.SetActive(
                true
            );


            shopRoot.transform.SetAsLastSibling();
        }


        // =====================================================
        // 상인 전용 카메라 구도로 전환
        // =====================================================

        if (ShopCameraDirector.Instance != null &&
            currentViewPoint != null)
        {
            ShopCameraDirector.Instance.EnterShopView(
                currentViewPoint
            );
        }


        // =====================================================
        // 상품 목록 생성
        // =====================================================

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
                CanAfford(
                    selectedEntry
                );
        }


        Debug.Log(
            $"[Shop] 선택 : " +
            $"{selectedEntry.ItemData.ItemTitle}"
        );
    }


    // =========================================================
    // 구입
    // =========================================================

    // =========================================================
    // 선택된 Item 구매
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
        // 1. 재료 확인
        // =====================================================

        if (!CanAfford(
                selectedEntry))
        {
            Debug.Log(
                "[Shop] 거래에 필요한 아이템이 부족합니다."
            );


            return;
        }


        // =====================================================
        // 2. 재료 소비
        // =====================================================

        bool consumed =
            ConsumeRequirements(
                selectedEntry
            );


        if (!consumed)
        {
            Debug.LogError(
                "[Shop] 거래 재료 소비에 실패했습니다."
            );


            return;
        }


        // =====================================================
        // 3. 상품 지급
        // =====================================================

        bool added =
            InventoryManager.Instance.AddItem(
                selectedEntry.ItemData,
                selectedEntry.Amount
            );


        if (!added)
        {
            Debug.LogError(
                "[Shop] 구매한 아이템을 Inventory에 " +
                "추가하지 못했습니다."
            );


            return;
        }


        // =====================================================
        // 4. 구매 성공
        // =====================================================

        purchasedAnyItem =
            true;


        Debug.Log(
            $"[Shop] 물물교환 성공 : " +
            $"{selectedEntry.ItemData.ItemTitle} " +
            $"x{selectedEntry.Amount}"
        );


        // =====================================================
        // 5. 선택 UI 초기화
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


        // =====================================================
        // Shop UI 종료
        // =====================================================

        if (shopRoot != null)
        {
            shopRoot.SetActive(
                false
            );
        }


        ClearSelection();

        ClearGeneratedIcons();


        // =====================================================
        // 카메라 원래 위치로 복귀
        // =====================================================

        if (ShopCameraDirector.Instance != null &&
            currentViewPoint != null)
        {
            ShopCameraDirector.Instance.ExitShopView();
        }


        // =====================================================
        // 기존 Callback 정보 저장
        // =====================================================

        Action<bool> callback =
            onShopClosed;


        bool purchased =
            purchasedAnyItem;


        // =====================================================
        // Runtime 초기화
        // =====================================================

        currentMerchant =
            null;


        currentViewPoint =
            null;


        onShopClosed =
            null;


        purchasedAnyItem =
            false;


        // =====================================================
        // 상점 종료 알림
        // =====================================================

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

    // =========================================================
    // 현재 선택 상품을 구매할 수 있는지 확인
    // =========================================================

    private bool CanAfford(
        MerchantShopEntry entry
    )
    {
        if (entry == null)
            return false;


        if (InventoryManager.Instance == null)
            return false;


        // ==========================================
        // 필요한 재료 전부 확인
        // ==========================================

        foreach (
            MerchantTradeRequirement requirement
            in entry.Requirements)
        {
            if (requirement == null)
                continue;


            ItemData requiredItem =
                requirement.RequiredItem;


            if (requiredItem == null)
                continue;


            int requiredAmount =
                requirement.RequiredAmount;


            int ownedAmount =
                GetInventoryItemCount(
                    requiredItem
                );


            // 필요한 수량보다 적음
            if (ownedAmount <
                requiredAmount)
            {
                return false;
            }
        }


        return true;
    }

    // =========================================================
    // Inventory 안의 특정 ItemData 총 수량
    // =========================================================

    private int GetInventoryItemCount(
        ItemData itemData
    )
    {
        if (itemData == null)
            return 0;


        if (InventoryManager.Instance == null)
            return 0;


        int totalAmount =
            0;


        foreach (
            InventorySlot slot
            in InventoryManager.Instance.Items)
        {
            if (slot == null)
                continue;


            if (slot.ItemData !=
                itemData)
            {
                continue;
            }


            totalAmount +=
                slot.Amount;
        }


        return totalAmount;
    }

    // =========================================================
    // 거래에 필요한 Item 제거
    // =========================================================

    private bool ConsumeRequirements(
        MerchantShopEntry entry
    )
    {
        if (entry == null)
            return false;


        if (InventoryManager.Instance == null)
            return false;


        // =====================================================
        // 반드시 먼저 모든 조건을 검사
        //
        // 일부 재료만 사라지는 현상 방지
        // =====================================================

        if (!CanAfford(entry))
        {
            return false;
        }


        // =====================================================
        // 실제 재료 소비
        // =====================================================

        foreach (
            MerchantTradeRequirement requirement
            in entry.Requirements)
        {
            if (requirement == null)
                continue;


            ItemData requiredItem =
                requirement.RequiredItem;


            if (requiredItem == null)
                continue;


            bool removed =
                InventoryManager.Instance.RemoveItem(
                    requiredItem,
                    requirement.RequiredAmount
                );


            if (!removed)
            {
                Debug.LogError(
                    $"[Shop] 거래 재료 제거 실패 : " +
                    $"{requiredItem.name}"
                );


                return false;
            }
        }


        return true;
    }
}