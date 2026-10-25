using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class WeaponUpgradeUIController : MonoBehaviour
{
    // =========================================================
    // UI
    // =========================================================

    [Header("Choice UI")]

    [SerializeField]
    private GameObject choicePanel;

    [SerializeField]
    private Button yesButton;

    [SerializeField]
    private Button noButton;


    [Header("Weapon Upgrade UI")]

    [SerializeField]
    private GameObject weaponUpgradePanel;

    [SerializeField]
    private Button closeButton;


    // =========================================================
    // Runtime
    // =========================================================

    private PlayerGameplayControl playerGameplayControl;

    private bool upgradePanelOpen;

    private bool choicePanelOpen;


    public bool IsUpgradePanelOpen =>
        upgradePanelOpen;


    // =========================================================
    // Awake
    // =========================================================

    private void Awake()
    {
        if (yesButton != null)
        {
            yesButton.onClick.AddListener(
                OnYesButtonClicked
            );
        }


        if (noButton != null)
        {
            noButton.onClick.AddListener(
                OnNoButtonClicked
            );
        }


        if (closeButton != null)
        {
            closeButton.onClick.AddListener(
                CloseUpgradePanel
            );
        }


        if (choicePanel != null)
        {
            choicePanel.SetActive(
                false
            );
        }


        if (weaponUpgradePanel != null)
        {
            weaponUpgradePanel.SetActive(
                false
            );
        }
    }


    // =========================================================
    // Input
    // =========================================================

    private void Update()
    {
        if (Keyboard.current == null)
            return;


        // ==========================================
        // 강화 Panel이 열린 상태
        // Tab 또는 ESC로 종료
        // ==========================================

        if (upgradePanelOpen)
        {
            if (Keyboard.current.tabKey.wasPressedThisFrame ||
                Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CloseUpgradePanel();

                return;
            }
        }


        // ==========================================
        // Yes / No 선택창에서는
        // ESC = No
        // ==========================================

        if (choicePanelOpen)
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                OnNoButtonClicked();
            }
        }
    }


    // =========================================================
    // Player 자동 탐색
    // =========================================================

    private void FindPlayer()
    {
        if (playerGameplayControl != null)
            return;


        playerGameplayControl =
            FindFirstObjectByType<PlayerGameplayControl>();


        if (playerGameplayControl == null)
        {
            Debug.LogWarning(
                "[WeaponUpgradeUI] PlayerGameplayControl을 찾지 못했습니다."
            );
        }
    }


    // =========================================================
    // Dialogue가 끝난 뒤 호출
    // =========================================================

    public void OpenUpgradeChoice()
    {
        if (choicePanel == null)
        {
            Debug.LogError(
                "[WeaponUpgradeUI] Choice Panel이 없습니다."
            );

            return;
        }


        FindPlayer();


        // ==========================================
        // Player 조작 정지
        // ==========================================

        if (playerGameplayControl != null)
        {
            playerGameplayControl.LockControls();
        }


        choicePanel.SetActive(
            true
        );


        choicePanel.transform.SetAsLastSibling();


        choicePanelOpen =
            true;


        upgradePanelOpen =
            false;


        Cursor.lockState =
            CursorLockMode.None;

        Cursor.visible =
            true;


        Debug.Log(
            "[WeaponUpgradeUI] 강화 선택창 Open"
        );
    }


    // =========================================================
    // YES
    // =========================================================

    private void OnYesButtonClicked()
    {
        if (!choicePanelOpen)
            return;


        if (choicePanel != null)
        {
            choicePanel.SetActive(
                false
            );
        }


        choicePanelOpen =
            false;


        if (weaponUpgradePanel == null)
        {
            Debug.LogError(
                "[WeaponUpgradeUI] Weapon Upgrade Panel이 없습니다."
            );


            RestoreGameplay();

            return;
        }


        weaponUpgradePanel.SetActive(
            true
        );


        weaponUpgradePanel.transform.SetAsLastSibling();


        upgradePanelOpen =
            true;


        // Player는 계속 조작 불가 상태
        Cursor.lockState =
            CursorLockMode.None;

        Cursor.visible =
            true;


        Debug.Log(
            "[WeaponUpgradeUI] Weapon Upgrade Panel Open"
        );
    }


    // =========================================================
    // NO
    // =========================================================

    private void OnNoButtonClicked()
    {
        if (choicePanel != null)
        {
            choicePanel.SetActive(
                false
            );
        }


        choicePanelOpen =
            false;


        Debug.Log(
            "[WeaponUpgradeUI] 강화 취소"
        );


        RestoreGameplay();
    }


    // =========================================================
    // 강화 Panel 종료
    // =========================================================

    public void CloseUpgradePanel()
    {
        if (weaponUpgradePanel != null)
        {
            weaponUpgradePanel.SetActive(
                false
            );
        }


        upgradePanelOpen =
            false;


        Debug.Log(
            "[WeaponUpgradeUI] Weapon Upgrade Panel Close"
        );


        RestoreGameplay();
    }


    // =========================================================
    // Gameplay 복구
    // =========================================================

    private void RestoreGameplay()
    {
        if (playerGameplayControl == null)
        {
            FindPlayer();
        }


        if (playerGameplayControl != null)
        {
            playerGameplayControl.UnlockControls();
        }
        else
        {
            Cursor.lockState =
                CursorLockMode.Locked;

            Cursor.visible =
                false;
        }
    }
}