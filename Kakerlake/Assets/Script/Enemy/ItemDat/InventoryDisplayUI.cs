using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryDisplayUI : MonoBehaviour
{
    [Header("Action Menu")]

    [SerializeField]
    private InventoryItemActionMenu actionMenu;
    private InventoryManager.InventorySlot selectedSlot;

    // =========================================================
    // Double Click
    // =========================================================

    [Header("Double Click")]

    [Tooltip("이 시간 안에 같은 아이템을 두 번 클릭하면 더블클릭")]
    [SerializeField]
    private float doubleClickTime =
        0.3f;


    private InventoryManager.InventorySlot
        lastClickedSlot;


    private float lastClickTime =
        -10f;

    // =========================================================
    // List
    // =========================================================

    [Header("Inventory List")]

    [SerializeField]
    private Transform contentRoot;


    [SerializeField]
    private InventoryItemButtonUI itemButtonPrefab;


    // =========================================================
    // Detail
    // =========================================================

    [Header("Item Detail")]

    [SerializeField]
    private Image itemDataImage;


    [SerializeField]
    private TMP_Text itemDataTitleText;


    [SerializeField]
    private TMP_Text itemDataText;


    // =========================================================
    // Enable
    // =========================================================

    private void OnEnable()
    {
        // =========================================================
        // Inventory를 새로 열 때
        // ActionPanel은 반드시 닫힌 상태로 시작
        // =========================================================

        if (actionMenu != null)
        {
            actionMenu.Close();
        }


        selectedSlot =
            null;


        lastClickedSlot =
            null;


        lastClickTime =
            -10f;


        StartCoroutine(
            RefreshNextFrame()
        );
    }


    private IEnumerator RefreshNextFrame()
    {
        yield return null;


        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged +=
                RefreshInventory;


            RefreshInventory();
        }
        else
        {
            Debug.LogWarning(
                "[InventoryDisplayUI] InventoryManager가 없습니다."
            );
        }
    }


    private void OnDisable()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -=
                RefreshInventory;
        }


        // Inventory가 닫히면
        // ActionPanel도 완전히 초기화
        if (actionMenu != null)
        {
            actionMenu.Close();
        }


        selectedSlot =
            null;


        lastClickedSlot =
            null;


        lastClickTime =
            -10f;
    }


    // =========================================================
    // Refresh
    // =========================================================

    public void RefreshInventory()
    {
        if (contentRoot == null ||
            itemButtonPrefab == null)
        {
            return;
        }


        // 기존 버튼 제거
        for (int i =
                 contentRoot.childCount - 1;
             i >= 0;
             i--)
        {
            Destroy(
                contentRoot.GetChild(i).gameObject
            );
        }


        if (InventoryManager.Instance == null)
            return;


        foreach (
            InventoryManager.InventorySlot slot
            in InventoryManager.Instance.Items)
        {
            InventoryItemButtonUI button =
                Instantiate(
                    itemButtonPrefab,
                    contentRoot
                );


            button.Setup(
            slot,
            OnItemClicked
            );
        }
    }


    // =========================================================
    // Detail
    // =========================================================

    private void ShowItemDetail(
        InventoryManager.InventorySlot slot
    )
    {
        if (slot == null ||
            slot.ItemData == null)
        {
            return;
        }


        ItemData data =
            slot.ItemData;


        if (itemDataImage != null)
        {
            itemDataImage.sprite =
                data.ItemIcon;


            itemDataImage.enabled =
                data.ItemIcon != null;
        }


        if (itemDataTitleText != null)
        {
            itemDataTitleText.text =
                data.ItemTitle;
        }


        if (itemDataText != null)
        {
            itemDataText.text =
                data.ItemDescription;
        }


        Debug.Log(
            $"[InventoryUI] 선택 : {data.ItemTitle}"
        );
    }

    private void OnItemClicked(
        InventoryManager.InventorySlot slot,
        RectTransform buttonRect
    )
    {
        if (slot == null ||
            slot.ItemData == null)
        {
            return;
        }


        // =========================================================
        // 실제 Double Click 검사
        // =========================================================

        bool isDoubleClick =
            lastClickedSlot == slot &&
            (
                Time.unscaledTime -
                lastClickTime
            ) <= doubleClickTime;


        // =========================================================
        // 다른 Item을 클릭
        // =========================================================

        if (selectedSlot != slot)
        {
            // 기존 ActionPanel 닫기
            if (actionMenu != null)
            {
                actionMenu.Close();
            }


            selectedSlot =
                slot;


            // Detail은 새로운 Item으로 변경
            ShowItemDetail(
                slot
            );
        }


        // =========================================================
        // 같은 Item Double Click
        // =========================================================

        if (isDoubleClick)
        {
            if (actionMenu != null)
            {
                // Toggle이 아니라 Show
                //
                // 이미 열려 있어도 닫히지 않는다.
                actionMenu.Show(
                    slot,
                    buttonRect
                );
            }


            // 연속 3클릭이 또 Double Click으로
            // 처리되는 것을 방지
            lastClickedSlot =
                null;


            lastClickTime =
                -10f;


            return;
        }


        // =========================================================
        // 이번 Click 저장
        // =========================================================

        lastClickedSlot =
            slot;


        lastClickTime =
            Time.unscaledTime;
    }
}