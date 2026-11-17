using UnityEngine;
using UnityEngine.UI;

public class ShopItemIconUI : MonoBehaviour
{
    // =========================================================
    // UI
    // =========================================================

    [Header("UI")]

    [SerializeField]
    private Button button;


    [SerializeField]
    private Image itemIcon;


    // =========================================================
    // Runtime
    // =========================================================

    private MerchantShopEntry entry;

    private ShopUIController shopUI;


    // =========================================================
    // Setup
    // =========================================================

    public void Setup(
        MerchantShopEntry shopEntry,
        ShopUIController owner
    )
    {
        entry =
            shopEntry;


        shopUI =
            owner;


        if (entry == null ||
            entry.ItemData == null)
        {
            gameObject.SetActive(
                false
            );

            return;
        }


        // ==========================================
        // Item Image
        // ==========================================

        if (itemIcon != null)
        {
            itemIcon.sprite =
                entry.ItemData.ItemIcon;


            itemIcon.enabled =
                entry.ItemData.ItemIcon != null;
        }


        // ==========================================
        // Click
        // ==========================================

        if (button != null)
        {
            button.onClick.RemoveAllListeners();


            button.onClick.AddListener(
                SelectItem
            );
        }
    }


    // =========================================================
    // 아이템 선택
    // =========================================================

    private void SelectItem()
    {
        if (shopUI == null ||
            entry == null)
        {
            return;
        }


        shopUI.SelectItem(
            entry
        );
    }
}