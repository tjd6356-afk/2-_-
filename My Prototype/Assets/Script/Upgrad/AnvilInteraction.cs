using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
public class AnvilInteraction : MonoBehaviour
{
    [Header("Upgrade UI")]
    [Tooltip("상호작용 시 활성화할 무기 업그레이드 패널 (UI GameObject)")]
    [SerializeField]
    private GameObject upgradeUIPanel;

    [Header("Mouse Interaction")]
    [Tooltip("모루를 클릭할 때 사용할 카메라")]
    [SerializeField]
    private Camera interactionCamera;

    [Tooltip("모루 클릭 Raycast 최대 거리")]
    [SerializeField]
    private float clickRayDistance = 100f;

    [Tooltip("모루 클릭 판정에 사용할 Layer")]
    [SerializeField]
    private LayerMask clickLayerMask = ~0;


    // =========================================================
    // Runtime
    // =========================================================
    private bool playerInside;

    // 상호작용 시 무기 정보를 넘겨주기 위한 매니저 캐싱
    private PlayerWeaponManager cachedWeaponManager;

    // ✨ 추가: UI가 열렸을 때 조작을 잠그기 위한 컨트롤 캐싱
    private PlayerGameplayControl cachedPlayerControl;


    private void Awake()
    {
        if (interactionCamera == null)
        {
            interactionCamera = Camera.main;
        }

        if (upgradeUIPanel != null && upgradeUIPanel.activeSelf)
        {
            upgradeUIPanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (!playerInside)
            return;

        // ==========================================
        // UI가 열려있을 때의 처리 (닫기)
        // ==========================================
        if (upgradeUIPanel != null && upgradeUIPanel.activeSelf)
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CloseUpgradePanel();
            }
            return;
        }

        // ==========================================
        // UI가 닫혀있을 때의 처리 (열기)
        // ==========================================
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            OpenUpgradePanel();
            return;
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryOpenPanelByMouse();
        }
    }

    private void TryOpenPanelByMouse()
    {
        if (interactionCamera == null) return;

        Ray ray;

        if (Cursor.lockState == CursorLockMode.Locked)
            ray = interactionCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        else
            ray = interactionCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (!Physics.Raycast(ray, out RaycastHit hit, clickRayDistance, clickLayerMask, QueryTriggerInteraction.Ignore))
            return;

        if (hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform))
        {
            OpenUpgradePanel();
        }
    }

    // =========================================================
    // 패널 열기 & 게임 잠금
    // =========================================================
    // =========================================================
    // 패널 열기 & 게임 잠금
    // =========================================================
    private void OpenUpgradePanel()
    {
        if (upgradeUIPanel == null) return;

        // ✨ 수정된 부분: WeaponUpgradePanelUI 컴포넌트를 찾아서 정보를 넘기며 엽니다.
        WeaponUpgradePanelUI panelUI = upgradeUIPanel.GetComponent<WeaponUpgradePanelUI>();
        if (panelUI != null)
        {
            panelUI.OpenPanel(cachedWeaponManager);
        }
        else
        {
            upgradeUIPanel.SetActive(true); // 혹시 스크립트가 안 붙어있을 때를 대비한 예외처리
        }

        if (cachedPlayerControl != null)
        {
            cachedPlayerControl.LockControls();
        }
    }

    // =========================================================
    // 패널 닫기 & 게임 재개
    // =========================================================
    private void CloseUpgradePanel()
    {
        if (upgradeUIPanel == null) return;

        upgradeUIPanel.SetActive(false);

        // ✨ 수정됨: PlayerGameplayControl에 이미 구현된 완벽한 해제 함수를 호출합니다.
        if (cachedPlayerControl != null)
        {
            cachedPlayerControl.UnlockControls();
        }
    }

    // =========================================================
    // Player 범위 감지 및 캐싱
    // =========================================================
    private void OnTriggerEnter(Collider other)
    {
        PlayerWeaponManager weaponManager = other.GetComponentInParent<PlayerWeaponManager>();
        PlayerGameplayControl playerControl = other.GetComponentInParent<PlayerGameplayControl>();

        if (weaponManager == null || playerControl == null) return;

        playerInside = true;
        cachedWeaponManager = weaponManager;
        cachedPlayerControl = playerControl; // 플레이어 컨트롤러를 찾아 저장해 둡니다.
    }

    private void OnTriggerExit(Collider other)
    {
        // 나갈 때는 저장해둔 캐싱 정보들을 모두 비워줍니다.
        PlayerWeaponManager weaponManager = other.GetComponentInParent<PlayerWeaponManager>();
        if (weaponManager == null) return;

        playerInside = false;
        cachedWeaponManager = null;
        cachedPlayerControl = null;
    }
}