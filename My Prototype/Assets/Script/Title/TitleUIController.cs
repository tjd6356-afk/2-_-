using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleUIController : MonoBehaviour
{
    // =========================================================
    // Scene
    // =========================================================

    [Header("Scene")]

    [SerializeField]
    private string lobbySceneName =
        "LobbyScene";


    // =========================================================
    // Panels
    // =========================================================

    [Header("Panels")]

    [SerializeField]
    private GameObject newPlayPanel;


    [SerializeField]
    private GameObject exitPanel;


    [SerializeField]
    private GameObject settingPanel;


    // =========================================================
    // Player Model
    // =========================================================

    [Header("Player Model")]

    [SerializeField]
    private TitlePlayerModelAnimation
        playerModelAnimation;


    // =========================================================
    // Start
    // =========================================================

    private void Start()
    {
        // ==========================================
        // New Play
        // ==========================================

        if (newPlayPanel != null)
        {
            newPlayPanel.SetActive(
                false
            );
        }


        // ==========================================
        // Exit
        // ==========================================

        if (exitPanel != null)
        {
            exitPanel.SetActive(
                false
            );
        }


        // ==========================================
        // Setting
        // ==========================================

        if (settingPanel != null)
        {
            settingPanel.SetActive(
                false
            );
        }


        // ==========================================
        // Player Model
        // ==========================================

        if (playerModelAnimation != null)
        {
            playerModelAnimation
                .MoveToOriginal();
        }


        // ==========================================
        // Cursor
        // ==========================================

        Cursor.lockState =
            CursorLockMode.None;


        Cursor.visible =
            true;
    }


    // =========================================================
    // NEW PLAY
    // =========================================================

    public void OnNewPlayButton()
    {
        if (newPlayPanel == null)
            return;


        // ==========================================
        // 다른 Panel 닫기
        // ==========================================

        if (exitPanel != null)
        {
            exitPanel.SetActive(
                false
            );
        }


        if (settingPanel != null)
        {
            settingPanel.SetActive(
                false
            );
        }


        // ==========================================
        // New Play Panel
        // ==========================================

        newPlayPanel.SetActive(
            true
        );


        newPlayPanel.transform
            .SetAsLastSibling();


        // ==========================================
        // Player Model 살짝 앞으로
        // ==========================================

        if (playerModelAnimation != null)
        {
            playerModelAnimation
                .MoveToPanelOpenPosition();
        }
    }


    // =========================================================
    // NEW PLAY - YES
    // =========================================================

    public void OnNewPlayYesButton()
    {
        Debug.Log(
            $"New Game Start : {lobbySceneName}"
        );


        SceneManager.LoadScene(
            lobbySceneName
        );
    }


    // =========================================================
    // NEW PLAY - NO
    // =========================================================

    public void OnNewPlayNoButton()
    {
        if (newPlayPanel != null)
        {
            newPlayPanel.SetActive(
                false
            );
        }


        // Player Model 원래 위치 복귀
        if (playerModelAnimation != null)
        {
            playerModelAnimation
                .MoveToOriginal();
        }
    }


    // =========================================================
    // 기존 PLAY 호환
    // =========================================================

    public void OnPlayButton()
    {
        OnNewPlayButton();
    }


    // =========================================================
    // SETTING
    // =========================================================

    public void OnSettingButton()
    {
        if (settingPanel == null)
            return;


        if (exitPanel != null)
        {
            exitPanel.SetActive(
                false
            );
        }


        if (newPlayPanel != null)
        {
            newPlayPanel.SetActive(
                false
            );
        }


        if (playerModelAnimation != null)
        {
            playerModelAnimation
                .MoveToOriginal();
        }


        settingPanel.SetActive(
            true
        );
    }


    public void OnSettingCloseButton()
    {
        if (settingPanel == null)
            return;


        settingPanel.SetActive(
            false
        );
    }


    // =========================================================
    // EXIT
    // =========================================================

    public void OnExitButton()
    {
        if (exitPanel == null)
            return;


        if (settingPanel != null)
        {
            settingPanel.SetActive(
                false
            );
        }


        if (newPlayPanel != null)
        {
            newPlayPanel.SetActive(
                false
            );
        }


        if (playerModelAnimation != null)
        {
            playerModelAnimation
                .MoveToOriginal();
        }


        exitPanel.SetActive(
            true
        );
    }


    // =========================================================
    // EXIT YES
    // =========================================================

    public void OnExitYesButton()
    {
        Debug.Log(
            "Game Exit"
        );


#if UNITY_EDITOR

        UnityEditor.EditorApplication.isPlaying =
            false;

#else

        Application.Quit();

#endif
    }


    // =========================================================
    // EXIT NO
    // =========================================================

    public void OnExitNoButton()
    {
        if (exitPanel == null)
            return;


        exitPanel.SetActive(
            false
        );
    }
}