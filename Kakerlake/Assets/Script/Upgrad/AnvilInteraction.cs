using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
public class AnvilInteraction : MonoBehaviour
{
    [Header("Upgrade UI")]
    [SerializeField] private GameObject upgradeUIPanel;

    [Header("Mouse Interaction")]
    [SerializeField] private Camera interactionCamera;
    [SerializeField] private float clickRayDistance = 100f;
    [SerializeField] private LayerMask clickLayerMask = ~0;

    private bool playerInside;
    private PlayerWeaponManager cachedWeaponManager;
    private PlayerGameplayControl cachedPlayerControl;

    private void Awake()
    {
        if (interactionCamera == null) interactionCamera = Camera.main;
        if (upgradeUIPanel != null && upgradeUIPanel.activeSelf) upgradeUIPanel.SetActive(false);
    }

    private void Update()
    {
        if (!playerInside) return;

        if (upgradeUIPanel != null && upgradeUIPanel.activeSelf)
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CloseUpgradePanel();
            }
            return;
        }

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
        Ray ray = (Cursor.lockState == CursorLockMode.Locked)
            ? interactionCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f))
            : interactionCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (!Physics.Raycast(ray, out RaycastHit hit, clickRayDistance, clickLayerMask, QueryTriggerInteraction.Ignore)) return;

        if (hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform))
        {
            OpenUpgradePanel();
        }
    }

    private void OpenUpgradePanel()
    {
        if (upgradeUIPanel == null) return;

        WeaponUpgradePanelUI panelUI = upgradeUIPanel.GetComponent<WeaponUpgradePanelUI>();
        if (panelUI != null)
        {
            // ✨ 핵심: 패널을 열 때 'CloseUpgradePanel' 함수를 닫기 콜백으로 같이 넘겨줍니다!
            panelUI.OpenPanel(cachedWeaponManager, CloseUpgradePanel);
        }
        else
        {
            upgradeUIPanel.SetActive(true);
        }

        if (cachedPlayerControl != null) cachedPlayerControl.LockControls();
    }

    // ✨ UI 버튼에서도 콜백을 통해 호출될 수 있도록 public으로 변경
    public void CloseUpgradePanel()
    {
        if (upgradeUIPanel == null) return;

        upgradeUIPanel.SetActive(false);
        if (cachedPlayerControl != null) cachedPlayerControl.UnlockControls();
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerWeaponManager weaponManager = other.GetComponentInParent<PlayerWeaponManager>();
        PlayerGameplayControl playerControl = other.GetComponentInParent<PlayerGameplayControl>();
        if (weaponManager == null || playerControl == null) return;

        playerInside = true;
        cachedWeaponManager = weaponManager;
        cachedPlayerControl = playerControl;
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerWeaponManager weaponManager = other.GetComponentInParent<PlayerWeaponManager>();
        if (weaponManager == null) return;

        playerInside = false;
        cachedWeaponManager = null;
        cachedPlayerControl = null;
    }
}