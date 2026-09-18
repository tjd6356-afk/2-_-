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


    public bool IsOpen => isOpen;


    private void Awake()
    {
        if (moveButton != null)
        {
            moveButton.onClick.AddListener(
                OnMoveButtonClicked
            );
        }


        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(
                OnCancelButtonClicked
            );
        }


        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
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
        targetSceneName =
            sceneName;


        currentPlayerControl =
            playerControl;


        if (destinationText != null)
        {
            destinationText.text =
                $"{displayName}으로 이동하시겠습니까?";
        }


        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }


        isOpen = true;


        if (currentPlayerControl != null)
        {
            currentPlayerControl.LockControls();
        }
        else
        {
            Cursor.lockState =
                CursorLockMode.None;

            Cursor.visible = true;
        }
    }


    // =========================================================
    // 이동하기
    // =========================================================

    private void OnMoveButtonClicked()
    {
        if (string.IsNullOrWhiteSpace(
                targetSceneName))
        {
            return;
        }


        if (SceneTransitionManager.Instance == null)
        {
            Debug.LogError(
                "SceneTransitionManager가 없습니다."
            );

            return;
        }


        moveButton.interactable =
            false;


        SceneTransitionManager.Instance.LoadScene(
            targetSceneName
        );
    }


    // =========================================================
    // 취소
    // =========================================================

    private void OnCancelButtonClicked()
    {
        Close();
    }


    // =========================================================
    // Panel 닫기
    // =========================================================

    public void Close()
    {
        if (!isOpen)
            return;


        isOpen = false;


        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }


        if (moveButton != null)
        {
            moveButton.interactable =
                true;
        }


        if (currentPlayerControl != null)
        {
            currentPlayerControl.UnlockControls();
        }


        currentPlayerControl = null;
    }
}