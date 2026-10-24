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
    private float dialogueInventoryAlpha = 0.25f;

    // =========================================================
    // Dialogue
    // =========================================================

    [Header("Dialogue")]

    [SerializeField]
    private ItemUseDialogueUI itemDialogueUI;


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
        // Item Type별 효과
        // =========================================================

        switch (data.ItemType)
        {
            // =====================================================
            // Food
            // 회복 + 소모
            // =====================================================

            case ItemType.Food:

                playerStats.Heal(
                    data.HealAmount
                );

                break;


            // =====================================================
            // Danger
            // 데미지 + 소모
            // =====================================================

            case ItemType.Danger:

                playerStats.TakeDamage(
                    data.DamageAmount
                );

                break;


            // =====================================================
            // Parts / History
            // 효과는 없고 Dialogue만
            // =====================================================

            case ItemType.Parts:
            case ItemType.History:

                break;
        }


        // =========================================================
        // Food / Danger만 Inventory에서 제거
        // =========================================================

        if (data.IsConsumable &&
            InventoryManager.Instance != null)
        {
            InventoryManager.Instance.RemoveItem(
                data,
                1
            );
        }


        // Item Action 메뉴는 닫기
        Close();


        // =========================================================
        // Dialogue 표시
        // =========================================================

        if (itemDialogueUI != null)
        {
            FadeInventoryForDialogue();


            itemDialogueUI.Show(
                data,
                RestoreInventoryAfterDialogue
            );
        }
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


        Close();
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
            if (itemDialogueUI != null)
            {
                itemDialogueUI.ShowText(
                    "손에 들기",
                    "더 이상 아이템을 들 수 없습니다.",
                    null
                );
            }

            return;
        }


        Close();
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

    private void FadeInventoryForDialogue()
    {
        if (inventoryCanvasGroup == null)
            return;


        // =========================================================
        // Dialogue 뒤에서 Inventory가 보이도록 반투명
        // =========================================================

        inventoryCanvasGroup.alpha =
            dialogueInventoryAlpha;


        // Dialogue 중 Inventory 클릭 금지
        inventoryCanvasGroup.interactable =
            false;


        inventoryCanvasGroup.blocksRaycasts =
            false;


        Debug.Log(
            "[Inventory] Dialogue 상태 - Inventory Fade"
        );
    }

    private void RestoreInventoryAfterDialogue()
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
            "[Inventory] Dialogue 종료 - Inventory 복구"
        );
    }


    // 인벤토리가 닫히면 반드시 같이 닫힘
    private void OnDisable()
    {
        Close();
    }
}