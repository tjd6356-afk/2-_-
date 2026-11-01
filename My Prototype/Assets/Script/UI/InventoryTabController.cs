using UnityEngine;
using UnityEngine.UI;

public class InventoryTabController : MonoBehaviour
{
    // =========================================================
    // Buttons
    // =========================================================

    [Header("Tab Buttons")]

    [SerializeField]
    private Button inventoryButton;


    [SerializeField]
    private Button settingButton;


    // =========================================================
    // Panels
    // =========================================================

    [Header("Tab Panels")]

    [SerializeField]
    private GameObject inventoryContentPanel;


    [SerializeField]
    private GameObject settingPanel;


    // =========================================================
    // Awake
    // =========================================================

    private void Awake()
    {
        if (inventoryButton != null)
        {
            inventoryButton.onClick.AddListener(
                OpenInventoryTab
            );
        }


        if (settingButton != null)
        {
            settingButton.onClick.AddListener(
                OpenSettingTab
            );
        }
    }


    // =========================================================
    // Inventory 창이 열릴 때 기본 탭
    // =========================================================

    public void ResetToDefaultTab()
    {
        OpenInventoryTab();
    }


    // =========================================================
    // Inventory
    // =========================================================

    public void OpenInventoryTab()
    {
        if (inventoryContentPanel != null)
        {
            inventoryContentPanel.SetActive(
                true
            );
        }


        if (settingPanel != null)
        {
            settingPanel.SetActive(
                false
            );
        }


        Debug.Log(
            "[InventoryTab] Inventory"
        );
    }


    // =========================================================
    // Setting
    // =========================================================

    public void OpenSettingTab()
    {
        if (inventoryContentPanel != null)
        {
            inventoryContentPanel.SetActive(
                false
            );
        }


        if (settingPanel != null)
        {
            settingPanel.SetActive(
                true
            );
        }


        Debug.Log(
            "[InventoryTab] Setting"
        );
    }
}