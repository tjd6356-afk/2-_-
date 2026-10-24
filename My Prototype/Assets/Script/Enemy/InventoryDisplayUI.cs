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

        if (actionMenu != null)
        {
            actionMenu.Close();
        }

        selectedSlot =
            null;
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
        if (slot == null)
            return;


        // =========================================================
        // 같은 아이템을 다시 클릭
        // =========================================================

        if (selectedSlot == slot)
        {
            if (actionMenu != null)
            {
                actionMenu.Toggle(
                    slot,
                    buttonRect
                );
            }

            return;
        }


        // =========================================================
        // 다른 아이템 선택
        // =========================================================

        if (actionMenu != null)
        {
            actionMenu.Close();
        }


        selectedSlot =
            slot;


        ShowItemDetail(
            slot
        );
    }
}