using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleUIController : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField]
    private string lobbySceneName = "LobbyScene";


    [Header("Panels")]
    [SerializeField]
    private GameObject exitPanel;

    [SerializeField]
    private GameObject settingPanel;


    private void Start()
    {
        // ==========================================
        // 게임 시작 시 Panel 숨기기
        // ==========================================

        if (exitPanel != null)
        {
            exitPanel.SetActive(false);
        }


        if (settingPanel != null)
        {
            settingPanel.SetActive(false);
        }


        // 타이틀에서는 마우스 사용 가능
        Cursor.lockState =
            CursorLockMode.None;

        Cursor.visible =
            true;
    }


    // =========================================================
    // PLAY
    // =========================================================

    public void OnPlayButton()
    {
        Debug.Log(
            $"Lobby Scene Load : {lobbySceneName}"
        );


        SceneManager.LoadScene(
            lobbySceneName
        );
    }


    // =========================================================
    // SETTING
    // =========================================================

    public void OnSettingButton()
    {
        if (settingPanel == null)
            return;


        // ExitPanel이 열려있다면 닫기
        if (exitPanel != null)
        {
            exitPanel.SetActive(false);
        }


        settingPanel.SetActive(true);
    }


    public void OnSettingCloseButton()
    {
        if (settingPanel == null)
            return;


        settingPanel.SetActive(false);
    }


    // =========================================================
    // EXIT
    // =========================================================

    public void OnExitButton()
    {
        if (exitPanel == null)
            return;


        // SettingPanel이 열려있다면 닫기
        if (settingPanel != null)
        {
            settingPanel.SetActive(false);
        }


        exitPanel.SetActive(true);
    }


    // =========================================================
    // EXIT - YES
    // =========================================================

    public void OnExitYesButton()
    {
        Debug.Log(
            "Game Exit"
        );


#if UNITY_EDITOR

        // Unity Editor 테스트용
        UnityEditor.EditorApplication.isPlaying =
            false;

#else

        // 실제 Windows Build
        Application.Quit();

#endif
    }


    // =========================================================
    // EXIT - NO
    // =========================================================

    public void OnExitNoButton()
    {
        if (exitPanel == null)
            return;


        exitPanel.SetActive(false);
    }
}