using UnityEngine;
using UnityEngine.InputSystem;

public class InventoryUIController : MonoBehaviour
{
    // =========================================================
    // UI
    // =========================================================

    [Header("Inventory UI")]

    [Tooltip("열고 닫을 Inventory 전체 패널")]
    [SerializeField]
    private GameObject inventoryRoot;


    [Header("Start State")]

    [Tooltip("게임 시작 시 Inventory를 닫아둘지 않을지 선택")]
    [SerializeField]
    private bool startClosed = true;


    // =========================================================
    // Runtime
    // =========================================================

    private bool isOpen;


    public bool IsOpen
    {
        get { return isOpen; }
    }


    // =========================================================
    // Start
    // =========================================================

    private void Start()
    {
        if (inventoryRoot == null)
        {
            Debug.LogError(
                "[InventoryUI] Inventory Root가 연결되지 않았습니다."
            );

            return;
        }


        if (startClosed)
        {
            CloseInventory();
        }
        else
        {
            OpenInventory();
        }
    }


    // =========================================================
    // Input
    // =========================================================

    private void Update()
    {
        if (Keyboard.current == null)
            return;


        // I키를 누른 순간
        if (Keyboard.current.iKey.wasPressedThisFrame)
        {
            ToggleInventory();
        }
    }


    // =========================================================
    // Toggle
    // =========================================================

    public void ToggleInventory()
    {
        if (isOpen)
        {
            CloseInventory();
        }
        else
        {
            OpenInventory();
        }
    }


    // =========================================================
    // Open
    // =========================================================

    public void OpenInventory()
    {
        if (inventoryRoot == null)
            return;


        inventoryRoot.SetActive(
            true
        );


        // 다른 UI보다 앞으로
        inventoryRoot.transform.SetAsLastSibling();


        isOpen =
            true;


        Debug.Log(
            "[InventoryUI] Inventory Open"
        );
    }


    // =========================================================
    // Close
    // =========================================================

    public void CloseInventory()
    {
        if (inventoryRoot == null)
            return;


        inventoryRoot.SetActive(
            false
        );


        isOpen =
            false;


        Debug.Log(
            "[InventoryUI] Inventory Close"
        );
    }
}