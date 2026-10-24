using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryItemButtonUI : MonoBehaviour
{
    [Header("UI")]

    [SerializeField]
    private Button button;


    [SerializeField]
    private Image itemIcon;


    [SerializeField]
    private TMP_Text itemNameText;


    [SerializeField]
    private TMP_Text amountText;


    private InventoryManager.InventorySlot slot;


    public void Setup(
        InventoryManager.InventorySlot newSlot,
        Action<
            InventoryManager.InventorySlot,
            RectTransform
        > onClicked
    )
    {
        slot =
            newSlot;


        if (slot == null ||
            slot.ItemData == null)
        {
            return;
        }


        ItemData data =
            slot.ItemData;


        if (itemIcon != null)
        {
            itemIcon.sprite =
                data.ItemIcon;

            itemIcon.enabled =
                data.ItemIcon != null;
        }


        if (itemNameText != null)
        {
            itemNameText.text =
                data.ItemTitle;
        }


        if (amountText != null)
        {
            amountText.text =
                $"x{slot.Amount}";
        }


        if (button != null)
        {
            button.onClick.RemoveAllListeners();


            button.onClick.AddListener(
                () =>
                {
                    onClicked?.Invoke(
                        slot,
                        transform as RectTransform
                    );
                }
            );
        }
    }
}