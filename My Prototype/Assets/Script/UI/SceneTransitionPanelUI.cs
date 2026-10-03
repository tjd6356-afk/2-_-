using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SceneTransitionPanelUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField]
    private GameObject panelRoot;

    [SerializeField]
    private TMP_Text destinationText;

    [SerializeField]
    private Button moveButton;

    [SerializeField]
    private Button cancelButton;


    private string targetSceneName;

    private PlayerGameplayControl currentPlayerControl;

    private bool isOpen;


    public bool IsOpen =>
        isOpen;

    public static bool IsTransitionUIOpen
    {
        get;
        private set;
    }

    private void Awake()
    {
        // ==========================================
        // 버튼 자동 연결
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
                "[SceneTransitionPanelUI] Move Button이 연결되지 않았습니다."
            );
        }


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
                "[SceneTransitionPanelUI] Cancel Button이 연결되지 않았습니다."
            );
        }


        if (panelRoot != null)
        {
            panelRoot.SetActive(
                false
            );
        }
    }


    // =========================================================
    // Panel 열기
    // =========================================================

    public void Open(
        string sceneName,
        string displayName,
        PlayerGameplayControl playerControl
    )
    {
        targetSceneName = sceneName;

        currentPlayerControl = playerControl;

        isOpen = true;

        IsTransitionUIOpen = true;


        // ==========================================
        // 다른 UI보다 가장 앞으로 가져오기
        // ==========================================

        if (panelRoot != null)
        {
            panelRoot.SetActive(true);

            panelRoot.transform.SetAsLastSibling();
        }


        // ==========================================
        // 반드시 마우스 잠금 해제
        // ==========================================

        Cursor.lockState =
            CursorLockMode.None;

        Cursor.visible =
            true;


        if (destinationText != null)
        {
            destinationText.text =
                $"{displayName}으로 이동하시겠습니까?";
        }


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


        if (currentPlayerControl != null)
        {
            currentPlayerControl.LockControls();
        }


        Debug.Log(
            $"[SceneTransitionPanel] Open : {targetSceneName}"
        );
    }


    // =========================================================
    // 이동 버튼
    // =========================================================

    public void OnMoveButtonClicked()
    {
        Debug.Log(
            $"[SceneTransitionPanel] Move Click : {targetSceneName}"
        );


        if (string.IsNullOrWhiteSpace(
                targetSceneName))
        {
            Debug.LogError(
                "[SceneTransitionPanel] Target Scene이 없습니다."
            );

            return;
        }


        if (SceneTransitionManager.Instance == null)
        {
            Debug.LogError(
                "[SceneTransitionPanel] SceneTransitionManager가 없습니다."
            );

            return;
        }


        // ==========================================
        // UI를 즉시 숨긴다.
        // ==========================================

        isOpen =
            false;

        IsTransitionUIOpen = false;

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


        // ==========================================
        // Scene 이동
        // ==========================================

        SceneTransitionManager
            .Instance
            .LoadScene(
                targetSceneName
            );
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
    // Panel 닫기
    // =========================================================

    public void Close()
    {
        if (!isOpen)
            return;


        isOpen =
            false;

        IsTransitionUIOpen =
        false;

        if (panelRoot != null)
        {
            panelRoot.SetActive(
                false
            );
        }


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


        if (currentPlayerControl != null)
        {
            currentPlayerControl
                .UnlockControls();
        }


        currentPlayerControl =
            null;
    }
}