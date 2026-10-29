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

    // 나중에 업그레이드 패널로 무기 정보를 넘겨주기 위해 플레이어의 무기 매니저를 저장해 둡니다.
    private PlayerWeaponManager cachedWeaponManager;


    private void Awake()
    {
        if (interactionCamera == null)
        {
            interactionCamera = Camera.main;
        }

        // 시작할 때 업그레이드 패널이 켜져있다면 꺼줍니다.
        if (upgradeUIPanel != null && upgradeUIPanel.activeSelf)
        {
            upgradeUIPanel.SetActive(false);
        }
    }


    private void Update()
    {
        if (!playerInside)
            return;


        // 이미 업그레이드 창이 열려 있다면 입력을 무시합니다.
        if (upgradeUIPanel != null && upgradeUIPanel.activeSelf)
            return;


        // ==========================================
        // E키로 상호작용
        // ==========================================

        if (Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            OpenUpgradePanel();
            return;
        }


        // ==========================================
        // 마우스 좌클릭으로 상호작용
        // ==========================================

        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryOpenPanelByMouse();
        }
    }


    // =========================================================
    // 마우스 클릭 레이캐스트 판정
    // =========================================================

    private void TryOpenPanelByMouse()
    {
        if (interactionCamera == null)
            return;

        Ray ray;

        if (Cursor.lockState == CursorLockMode.Locked)
        {
            // 조준 중일 때는 화면 중앙 십자선 기준
            ray = interactionCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        }
        else
        {
            // 마우스 커서가 자유로울 때는 실제 마우스 커서 위치 기준
            Vector2 mousePosition = Mouse.current.position.ReadValue();
            ray = interactionCamera.ScreenPointToRay(mousePosition);
        }


        if (!Physics.Raycast(
                ray,
                out RaycastHit hit,
                clickRayDistance,
                clickLayerMask,
                QueryTriggerInteraction.Ignore))
        {
            return;
        }


        Transform hitTransform = hit.collider.transform;

        // 클릭한 오브젝트가 이 모루(또는 자식 오브젝트)인지 확인
        bool clickedThisAnvil = hitTransform == transform || hitTransform.IsChildOf(transform);

        if (!clickedThisAnvil)
            return;


        OpenUpgradePanel();
    }


    // =========================================================
    // 업그레이드 패널 띄우기
    // =========================================================

    private void OpenUpgradePanel()
    {
        if (upgradeUIPanel == null)
        {
            Debug.LogError("연결된 Upgrade UI Panel이 없습니다!");
            return;
        }

        Debug.Log("모루 상호작용: 무기 업그레이드 패널 열기");

        // 1. UI 패널 활성화
        upgradeUIPanel.SetActive(true);

        // [이후 구현할 부분]
        // 2. 패널 스크립트에 cachedWeaponManager를 넘겨주어 현재 무기 정보 띄우기
        // 3. PlayerGameplayControl.GameplayLocked = true; 등을 호출하여 캐릭터 조작 잠그고 마우스 풀기
    }


    // =========================================================
    // Player 범위 진입 / 이탈 감지
    // =========================================================

    private void OnTriggerEnter(Collider other)
    {
        // 무기 매니저를 찾아 플레이어인지 확실하게 검증하고 캐싱합니다.
        PlayerWeaponManager weaponManager = other.GetComponentInParent<PlayerWeaponManager>();

        if (weaponManager == null)
            return;

        playerInside = true;
        cachedWeaponManager = weaponManager;

        Debug.Log("모루 범위 내로 플레이어 진입");
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerWeaponManager weaponManager = other.GetComponentInParent<PlayerWeaponManager>();

        if (weaponManager == null)
            return;

        playerInside = false;
        cachedWeaponManager = null;

        Debug.Log("모루 범위 밖으로 플레이어 이탈");
    }
}