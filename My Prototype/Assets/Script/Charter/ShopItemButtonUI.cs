using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopItemButtonUI : MonoBehaviour
{
    [Header("UI")]

    [SerializeField]
    private Image itemIcon;


    [SerializeField]
    private TMP_Text itemNameText;


    [SerializeField]
    private TMP_Text amountText;


    [SerializeField]
    private Button buyButton;


    private MerchantShopEntry currentEntry;

    private ShopUIController shopUI;


    public void Setup(
        MerchantShopEntry entry,
        ShopUIController owner
    )
    {
        currentEntry =
            entry;


        shopUI =
            owner;


        if (currentEntry == null ||
            currentEntry.ItemData == null)
        {
            gameObject.SetActive(
                false
            );

            return;
        }


        // ==========================================
        // Icon
        // ==========================================

        if (itemIcon != null)
        {
            itemIcon.sprite =
                currentEntry.ItemData.ItemIcon;


            itemIcon.enabled =
                currentEntry.ItemData.ItemIcon != null;
        }


        // ==========================================
        // Name
        // ==========================================

        if (itemNameText != null)
        {
            itemNameText.text =
                currentEntry.ItemData.ItemTitle;
        }


        // ==========================================
        // Amount
        // ==========================================

        if (amountText != null)
        {
            amountText.text =
                $"x{currentEntry.Amount}";
        }


        // ==========================================
        // Buy
        // ==========================================

        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();


            buyButton.onClick.AddListener(
                Buy
            );
        }
    }


    private void Buy()
    {
        if (shopUI == null ||
            currentEntry == null)
        {
            return;
        }


        shopUI.PurchaseItem(
            currentEntry
        );
    }
}