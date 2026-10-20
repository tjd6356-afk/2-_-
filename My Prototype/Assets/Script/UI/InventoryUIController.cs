using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class InventoryUIController : MonoBehaviour
{
    // =========================================================
    // Inventory UI
    // =========================================================

    [Header("Inventory UI")]

    [SerializeField]
    private GameObject inventoryRoot;


    [Header("Blocked Message")]

    [Tooltip("전투 중 I키를 눌렀을 때 표시할 Text")]
    [SerializeField]
    private TMP_Text unavailableMessageText;

    [SerializeField]
    private float unavailableMessageDuration = 1.2f;


    // =========================================================
    // Runtime Player
    // =========================================================

    private PlayerGameplayControl gameplayControl;


    // =========================================================
    // State
    // =========================================================

    private bool isOpen;

    private Coroutine messageCoroutine;


    public bool IsOpen =>
        isOpen;


    // =========================================================
    // Start
    // =========================================================

    private void Start()
    {
        if (inventoryRoot == null)
        {
            Debug.LogError(
                "[InventoryUI] Inventory Root가 없습니다."
            );

            return;
        }


        // 처음에는 Inventory 닫기
        inventoryRoot.SetActive(
            false
        );


        isOpen =
            false;


        // 사용 불가 메시지도 처음에는 숨김
        if (unavailableMessageText != null)
        {
            unavailableMessageText.gameObject.SetActive(
                false
            );
        }


        FindPlayerGameplayControl();
    }


    // =========================================================
    // Input
    // =========================================================

    private void Update()
    {
        if (Keyboard.current == null)
            return;


        if (Keyboard.current.iKey.wasPressedThisFrame)
        {
            HandleInventoryInput();
        }
    }


    // =========================================================
    // I Key
    // =========================================================

    private void HandleInventoryInput()
    {
        // ==========================================
        // 전투 중
        // ==========================================

        if (IsBattleActive())
        {
            // 혹시 이미 Inventory가 열려있다면 닫음
            if (isOpen)
            {
                CloseInventory();
            }


            ShowUnavailableMessage();

            return;
        }


        // ==========================================
        // 평상시
        // ==========================================

        ToggleInventory();
    }


    // =========================================================
    // Battle 검사
    // =========================================================

    private bool IsBattleActive()
    {
        RoomBattleController[] battleRooms =
            FindObjectsByType<RoomBattleController>(
                FindObjectsSortMode.None
            );


        foreach (
            RoomBattleController battleRoom
            in battleRooms)
        {
            if (battleRoom == null)
                continue;


            if (battleRoom.BattleStarted &&
                !battleRoom.BattleCompleted)
            {
                return true;
            }
        }


        return false;
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


        // 전투 중 외부에서 OpenInventory를 호출해도 차단
        if (IsBattleActive())
        {
            ShowUnavailableMessage();

            return;
        }


        // Runtime Player가 아직 연결되지 않았다면 다시 탐색
        if (gameplayControl == null)
        {
            FindPlayerGameplayControl();
        }


        inventoryRoot.SetActive(
            true
        );


        // 다른 UI 위로 올림
        inventoryRoot.transform.SetAsLastSibling();


        isOpen =
            true;


        // ==========================================
        // 플레이어 조작 잠금
        // ==========================================

        if (gameplayControl != null)
        {
            gameplayControl.LockControls();
        }


        // ==========================================
        // 마우스 강제 활성화
        // ==========================================

        Cursor.lockState =
            CursorLockMode.None;

        Cursor.visible =
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


        // ==========================================
        // 플레이어 조작 복구
        // ==========================================

        if (gameplayControl == null)
        {
            FindPlayerGameplayControl();
        }


        if (gameplayControl != null)
        {
            gameplayControl.UnlockControls();
        }
        else
        {
            Cursor.lockState =
                CursorLockMode.Locked;

            Cursor.visible =
                false;
        }


        Debug.Log(
            "[InventoryUI] Inventory Close"
        );
    }


    // =========================================================
    // Runtime Player 찾기
    // =========================================================

    private void FindPlayerGameplayControl()
    {
        gameplayControl =
            FindFirstObjectByType<PlayerGameplayControl>();


        if (gameplayControl != null)
        {
            Debug.Log(
                $"[InventoryUI] PlayerGameplayControl 연결 : " +
                $"{gameplayControl.gameObject.name}"
            );
        }
    }


    // =========================================================
    // 사용 불가 메시지
    // =========================================================

    private void ShowUnavailableMessage()
    {
        if (unavailableMessageText == null)
        {
            Debug.Log(
                "사용이 불가 합니다."
            );

            return;
        }


        if (messageCoroutine != null)
        {
            StopCoroutine(
                messageCoroutine
            );
        }


        messageCoroutine =
            StartCoroutine(
                UnavailableMessageRoutine()
            );
    }


    private IEnumerator UnavailableMessageRoutine()
    {
        unavailableMessageText.text =
            "사용이 불가 합니다.";


        unavailableMessageText.gameObject.SetActive(
            true
        );


        yield return
            new WaitForSecondsRealtime(
                unavailableMessageDuration
            );


        unavailableMessageText.gameObject.SetActive(
            false
        );


        messageCoroutine =
            null;
    }
}