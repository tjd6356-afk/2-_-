using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InventoryItemActionMenu : MonoBehaviour
{
    // =========================================================
    // UI
    // =========================================================

    [Header("Action Panel")]

    [SerializeField]
    private RectTransform actionPanel;


    [SerializeField]
    private Button useButton;


    [SerializeField]
    private Button dropButton;


    [SerializeField]
    private Button holdButton;


    [SerializeField]
    private Button cancelButton;


    [Tooltip("아이템 버튼 우측에서 떨어질 거리")]
    [SerializeField]
    private Vector2 popupOffset =
        new Vector2(
            15f,
            0f
        );

    [Header("Inventory Fade")]

    [SerializeField]
    private CanvasGroup inventoryCanvasGroup;


    [Range(0f, 1f)]
    [SerializeField]
    private float effectInventoryAlpha = 0.25f;

    // =========================================================
    // Item Effect Message
    // =========================================================

    [Header("Item Effect Message")]

    [SerializeField]
    private ItemEffectMessageUI itemEffectUI;


    // =========================================================
    // Runtime
    // =========================================================

    private InventoryManager.InventorySlot currentSlot;

    private RectTransform currentSourceButton;

    private Canvas rootCanvas;

    private Camera uiCamera;

    private int openedFrame = -1;


    private void Awake()
    {
        rootCanvas =
            GetComponentInParent<Canvas>();


        if (rootCanvas != null &&
            rootCanvas.renderMode !=
            RenderMode.ScreenSpaceOverlay)
        {
            uiCamera =
                rootCanvas.worldCamera;
        }


        if (useButton != null)
        {
            useButton.onClick.AddListener(
                UseItem
            );
        }


        if (dropButton != null)
        {
            dropButton.onClick.AddListener(
                DropItem
            );
        }


        if (holdButton != null)
        {
            holdButton.onClick.AddListener(
                HoldItem
            );
        }


        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(
                Close
            );
        }


        if (actionPanel != null)
        {
            actionPanel.gameObject.SetActive(
                false
            );
        }
    }


    // =========================================================
    // 다른 곳 클릭 감지
    // =========================================================

    private void Update()
    {
        if (actionPanel == null ||
            !actionPanel.gameObject.activeSelf)
        {
            return;
        }


        // ActionPanel을 연 바로 그 클릭은 무시
        if (Time.frameCount == openedFrame)
            return;


        if (Mouse.current == null)
            return;


        if (!Mouse.current.leftButton.wasPressedThisFrame)
            return;


        Vector2 mousePosition =
            Mouse.current.position.ReadValue();


        // ActionPanel 내부 클릭
        if (RectTransformUtility.RectangleContainsScreenPoint(
                actionPanel,
                mousePosition,
                uiCamera))
        {
            return;
        }


        // 현재 ItemButton 자체를 클릭한 경우
        if (currentSourceButton != null &&
            RectTransformUtility.RectangleContainsScreenPoint(
                currentSourceButton,
                mousePosition,
                uiCamera))
        {
            return;
        }


        // 그 외 영역 클릭
        Close();
    }


    // =========================================================
    // Toggle
    // =========================================================

    public void Toggle(
        InventoryManager.InventorySlot slot,
        RectTransform sourceButton
    )
    {
        if (slot == null)
            return;


        // 이미 같은 아이템 메뉴가 열려있다면 닫기
        if (actionPanel.gameObject.activeSelf &&
            currentSlot == slot)
        {
            Close();

            return;
        }


        Open(
            slot,
            sourceButton
        );
    }


    // =========================================================
    // Open
    // =========================================================

    private void Open(
        InventoryManager.InventorySlot slot,
        RectTransform sourceButton
    )
    {
        currentSlot =
            slot;


        currentSourceButton =
            sourceButton;


        actionPanel.gameObject.SetActive(
            true
        );


        actionPanel.SetAsLastSibling();


        PositionNextToButton(
            sourceButton
        );


        openedFrame =
            Time.frameCount;
    }


    // =========================================================
    // Item 오른쪽에 배치
    // =========================================================

    private void PositionNextToButton(
        RectTransform sourceButton
    )
    {
        if (sourceButton == null ||
            actionPanel.parent == null)
        {
            return;
        }


        Vector3[] corners =
            new Vector3[4];


        sourceButton.GetWorldCorners(
            corners
        );


        // 버튼 오른쪽 중앙
        Vector3 rightCenter =
            (
                corners[2] +
                corners[3]
            ) * 0.5f;


        Vector2 screenPosition =
            RectTransformUtility.WorldToScreenPoint(
                uiCamera,
                rightCenter
            );


        RectTransform parentRect =
            actionPanel.parent
            as RectTransform;


        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                screenPosition,
                uiCamera,
                out Vector2 localPoint))
        {
            actionPanel.anchoredPosition =
                localPoint +
                popupOffset;
        }
    }


    // =========================================================
    // 사용하기
    // =========================================================

    private void UseItem()
    {
        if (currentSlot == null ||
            currentSlot.ItemData == null)
        {
            return;
        }


        ItemData data =
            currentSlot.ItemData;

        ItemData usedItemData =
            currentSlot.ItemData;

        PlayerStats playerStats =
            FindFirstObjectByType<PlayerStats>();


        if (playerStats == null)
        {
            Debug.LogError(
                "[ItemAction] PlayerStats를 찾지 못했습니다."
            );

            return;
        }


        // =========================================================
        // Item 효과
        // =========================================================

        switch (data.ItemType)
        {
            // =====================================================
            // Food
            // =====================================================

            case ItemType.Food:

                playerStats.Heal(
                    data.HealAmount
                );

                playerStats.TakeDamage(
                data.DamageAmount
                );

                break;


            // =====================================================
            // Danger
            // =====================================================

            case ItemType.Danger:

                playerStats.Heal(
                    data.HealAmount
                );

                playerStats.TakeDamage(
                    data.DamageAmount
                );

                break;


            // =====================================================
            // History
            // =====================================================

            case ItemType.History:

                playerStats.Heal(
                    data.HealAmount
                );

                playerStats.TakeDamage(
                    data.DamageAmount
                );

                break;

            // =====================================================
            // History
            // =====================================================

            case ItemType.Parts:

                playerStats.Heal(
                    data.HealAmount
                );

                playerStats.TakeDamage(
                    data.DamageAmount
                );

                break;
        }


        // =========================================================
        // Food / Danger만 소비
        // =========================================================

        bool consumeItem =
        data.ItemType == ItemType.Food ||
        data.ItemType == ItemType.Danger;


        if (consumeItem &&
            InventoryManager.Instance != null)
        {
            InventoryManager.Instance.RemoveItem(
                data,
                1
            );
        }


        // =========================================================
        // Inventory 반투명
        // =========================================================

        FadeInventoryForEffect();


        // =========================================================
        // Item Effect Message
        // =========================================================

        if (itemEffectUI != null)
        {
            itemEffectUI.Show(
                data,
                RestoreInventoryAfterEffect
            );
        }
        else
        {
            // UI가 없으면 Inventory는 즉시 복구
            RestoreInventoryAfterEffect();


            Debug.LogWarning(
                "[ItemAction] ItemEffectMessageUI가 없습니다."
            );
        }

        RefreshCurrentItemOrClose(
            usedItemData);
    }


    // =========================================================
    // 버리기
    // =========================================================

    private void DropItem()
    {
        if (currentSlot == null ||
            currentSlot.ItemData == null)
        {
            return;
        }


        ItemData data =
            currentSlot.ItemData;

        ItemData usedItemData =
            currentSlot.ItemData;

        PlayerStats player =
            FindFirstObjectByType<PlayerStats>();


        if (player == null)
            return;


        if (InventoryManager.Instance == null)
            return;


        // 먼저 Inventory에서 제거
        if (!InventoryManager.Instance.RemoveItem(
                data,
                1))
        {
            return;
        }


        Vector3 dropPosition =
            player.transform.position +
            player.transform.forward * 1.5f +
            Vector3.up * 0.3f;


        ItemPickup spawned =
            ItemWorldSpawner.Spawn(
                data,
                dropPosition,
                1
            );


        // 생성 실패 시 아이템 되돌려주기
        if (spawned == null)
        {
            InventoryManager.Instance.AddItem(
                data,
                1
            );
        }

        RefreshCurrentItemOrClose(
        usedItemData);

    }


    // =========================================================
    // 손에 들기
    // =========================================================

    private void HoldItem()
    {
        if (currentSlot == null ||
            currentSlot.ItemData == null)
        {
            return;
        }


        PlayerHeldItemController holder =
            FindFirstObjectByType<PlayerHeldItemController>();

        ItemData usedItemData =
            currentSlot.ItemData;

        if (holder == null)
        {
            Debug.LogError(
                "[ItemAction] PlayerHeldItemController가 없습니다."
            );

            return;
        }


        bool success =
            holder.TryHold(
                currentSlot.ItemData
            );


        if (!success)
        {
            if (itemEffectUI != null)
            {
                itemEffectUI.Show(
                    "더 이상 아이템을 들 수 없습니다.",
                    null
                );
            }

            return;
        }

        RefreshCurrentItemOrClose(
            usedItemData);
    }


    // =========================================================
    // Close
    // =========================================================

    public void Close()
    {
        if (actionPanel != null)
        {
            actionPanel.gameObject.SetActive(
                false
            );
        }


        currentSlot =
            null;

        currentSourceButton =
            null;
        
    }

    private void FadeInventoryForEffect()
    {
        if (inventoryCanvasGroup == null)
            return;


        inventoryCanvasGroup.alpha =
            effectInventoryAlpha;


        inventoryCanvasGroup.interactable =
            false;


        inventoryCanvasGroup.blocksRaycasts =
            false;


        Debug.Log(
            "[Inventory] Item Effect 상태"
        );
    }

    private void RestoreInventoryAfterEffect()
    {
        if (inventoryCanvasGroup == null)
            return;


        inventoryCanvasGroup.alpha =
            1f;


        inventoryCanvasGroup.interactable =
            true;


        inventoryCanvasGroup.blocksRaycasts =
            true;


        Debug.Log(
            "[Inventory] Item Effect 종료"
        );
    }

    // =========================================================
    // Action Menu 강제 Open
    //
    // 같은 Item이어도 닫지 않고 계속 유지
    // =========================================================

    public void Show(
        InventoryManager.InventorySlot slot,
        RectTransform sourceButton
    )
    {
        if (slot == null)
            return;


        Open(
            slot,
            sourceButton
        );
    }

    // =========================================================
    // 현재 Item이 Inventory에 아직 남아있는지 확인
    // =========================================================

    private void RefreshCurrentItemOrClose(
        ItemData itemData
    )
    {
        if (itemData == null)
        {
            Close();

            return;
        }


        if (InventoryManager.Instance == null)
        {
            Close();

            return;
        }


        InventoryManager.InventorySlot
            foundSlot =
                null;


        // =========================================================
        // 같은 ItemData가 Inventory에 아직 존재하는지 검사
        // =========================================================

        foreach (
            InventoryManager.InventorySlot slot
            in InventoryManager.Instance.Items)
        {
            if (slot == null ||
                slot.ItemData == null)
            {
                continue;
            }


            if (slot.ItemData ==
                itemData)
            {
                foundSlot =
                    slot;

                break;
            }
        }


        // =========================================================
        // 더 이상 Inventory에 없음
        // → 마지막 Item을 사용/버린 것
        // =========================================================

        if (foundSlot == null)
        {
            Close();

            return;
        }


        // =========================================================
        // 아직 Item이 남아있음
        //
        // 새 Slot 참조로 갱신
        // ActionPanel은 그대로 유지
        // =========================================================

        currentSlot =
            foundSlot;
    }

    // 인벤토리가 닫히면 반드시 같이 닫힘
    private void OnDisable()
    {
        Close();
    }
}