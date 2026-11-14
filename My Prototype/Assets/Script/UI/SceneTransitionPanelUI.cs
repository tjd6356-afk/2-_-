using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SceneTransitionPanelUI : MonoBehaviour
{
    // =========================================================
    // UI
    // =========================================================

    [Header("UI")]

    [SerializeField]
    private GameObject panelRoot;


    [SerializeField]
    private TMP_Text destinationText;


    [SerializeField]
    private Button moveButton;


    [SerializeField]
    private Button cancelButton;


    // =========================================================
    // Runtime
    // =========================================================

    private string targetSceneName;


    private PlayerGameplayControl currentPlayerControl;


    // 현재 이 UI를 연 Portal
    private ScenePortal currentPortal;


    private bool isOpen;


    public bool IsOpen =>
        isOpen;


    public static bool IsTransitionUIOpen
    {
        get;
        private set;
    }


    // =========================================================
    // Awake
    // =========================================================

    private void Awake()
    {
        // ==========================================
        // Move Button
        // ==========================================

        if (moveButton != null)
        {
            moveButton.onClick.RemoveListener(
                OnMoveButtonClicked
            );


            moveButton.onClick.AddListener(
                OnMoveButtonClicked
            );
        }
        else
        {
            Debug.LogError(
                "[SceneTransitionPanelUI] " +
                "Move Button이 연결되지 않았습니다."
            );
        }


        // ==========================================
        // Cancel Button
        // ==========================================

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveListener(
                OnCancelButtonClicked
            );


            cancelButton.onClick.AddListener(
                OnCancelButtonClicked
            );
        }
        else
        {
            Debug.LogError(
                "[SceneTransitionPanelUI] " +
                "Cancel Button이 연결되지 않았습니다."
            );
        }


        // ==========================================
        // 처음에는 Panel 숨김
        // ==========================================

        if (panelRoot != null)
        {
            panelRoot.SetActive(
                false
            );
        }


        isOpen =
            false;


        IsTransitionUIOpen =
            false;
    }


    // =========================================================
    // 기존 코드 호환용 Open
    // =========================================================

    public void Open(
        string sceneName,
        string displayName,
        PlayerGameplayControl playerControl
    )
    {
        Open(
            sceneName,
            displayName,
            playerControl,
            null
        );
    }


    // =========================================================
    // Panel 열기
    // =========================================================

    public void Open(
        string sceneName,
        string displayName,
        PlayerGameplayControl playerControl,
        ScenePortal portal
    )
    {
        targetSceneName =
            sceneName;


        currentPlayerControl =
            playerControl;


        // ★ 현재 Portal 기억
        currentPortal =
            portal;


        isOpen =
            true;


        IsTransitionUIOpen =
            true;


        // ==========================================
        // Panel 활성화
        // ==========================================

        if (panelRoot != null)
        {
            panelRoot.SetActive(
                true
            );


            panelRoot.transform.SetAsLastSibling();
        }


        // ==========================================
        // Mouse
        // ==========================================

        Cursor.lockState =
            CursorLockMode.None;


        Cursor.visible =
            true;


        // ==========================================
        // Destination Text
        // ==========================================

        if (destinationText != null)
        {
            destinationText.text =
                $"{displayName}으로 이동하시겠습니까?";
        }


        // ==========================================
        // Buttons
        // ==========================================

        if (moveButton != null)
        {
            moveButton.interactable =
                true;
        }


        if (cancelButton != null)
        {
            cancelButton.interactable =
                true;
        }


        // ==========================================
        // Player 조작 정지
        // ==========================================

        if (currentPlayerControl != null)
        {
            currentPlayerControl.LockControls();
        }


        Debug.Log(
            $"[SceneTransitionPanel] Open : " +
            $"{targetSceneName}"
        );
    }


    // =========================================================
    // 이동 버튼
    // =========================================================

    public void OnMoveButtonClicked()
    {
        Debug.Log(
            $"[SceneTransitionPanel] Move Click : " +
            $"{targetSceneName}"
        );


        // 중복 클릭 방지
        if (moveButton != null)
        {
            moveButton.interactable =
                false;
        }


        if (cancelButton != null)
        {
            cancelButton.interactable =
                false;
        }


        // =====================================================
        // 새로운 Portal 연출 시스템
        // =====================================================

        if (currentPortal != null)
        {
            currentPortal.StartPortalTransition();

            return;
        }


        // =====================================================
        // Portal 정보가 없다면 오류
        // =====================================================

        Debug.LogError(
            "[SceneTransitionPanel] " +
            "현재 ScenePortal이 등록되어 있지 않습니다."
        );


        // 다시 누를 수 있게 복구
        if (moveButton != null)
        {
            moveButton.interactable =
                true;
        }


        if (cancelButton != null)
        {
            cancelButton.interactable =
                true;
        }
    }


    // =========================================================
    // 취소 버튼
    // =========================================================

    public void OnCancelButtonClicked()
    {
        Debug.Log(
            "[SceneTransitionPanel] Cancel Click"
        );


        Close();
    }


    // =========================================================
    // 일반 Close
    //
    // 취소했을 때 사용
    // Player 조작도 다시 켬
    // =========================================================

    public void Close()
    {
        if (!isOpen)
            return;


        isOpen =
            false;


        IsTransitionUIOpen =
            false;


        // ==========================================
        // Panel
        // ==========================================

        if (panelRoot != null)
        {
            panelRoot.SetActive(
                false
            );
        }


        // ==========================================
        // Buttons
        // ==========================================

        if (moveButton != null)
        {
            moveButton.interactable =
                true;
        }


        if (cancelButton != null)
        {
            cancelButton.interactable =
                true;
        }


        // ==========================================
        // Player 조작 복구
        // ==========================================

        if (currentPlayerControl != null)
        {
            currentPlayerControl.UnlockControls();
        }


        // ==========================================
        // Runtime 초기화
        // ==========================================

        currentPlayerControl =
            null;


        currentPortal =
            null;


        targetSceneName =
            null;
    }


    // =========================================================
    // Portal 연출 시작용 Close
    //
    // 중요:
    // Player 조작을 다시 켜지 않음
    // =========================================================

    public void CloseForTransition()
    {
        isOpen =
            false;


        IsTransitionUIOpen =
            false;


        // ==========================================
        // Panel만 숨김
        // ==========================================

        if (panelRoot != null)
        {
            panelRoot.SetActive(
                false
            );
        }


        if (moveButton != null)
        {
            moveButton.interactable =
                false;
        }


        if (cancelButton != null)
        {
            cancelButton.interactable =
                false;
        }


        // =====================================================
        // 매우 중요
        //
        // currentPlayerControl.UnlockControls()
        // 호출하지 않는다.
        //
        // Portal 연출 중에는 Player가 움직이면 안 됨.
        // =====================================================

        Debug.Log(
            "[SceneTransitionPanel] " +
            "Portal Transition용으로 UI 숨김"
        );
    }


    // =========================================================
    // Destroy
    // =========================================================

    private void OnDestroy()
    {
        IsTransitionUIOpen =
            false;
    }
}