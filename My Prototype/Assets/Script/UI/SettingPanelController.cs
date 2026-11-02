using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingPanelController : MonoBehaviour
{
    // =========================================================
    // Buttons
    // =========================================================

    [Header("Buttons")]

    [SerializeField]
    private Button titleButton;


    [SerializeField]
    private Button exitButton;


    // =========================================================
    // Scene
    // =========================================================

    [Header("Scene")]

    [Tooltip("돌아갈 타이틀 Scene 이름")]
    [SerializeField]
    private string titleSceneName =
        "Title";


    // =========================================================
    // Awake
    // =========================================================

    private void Awake()
    {
        // ==========================================
        // Title Button
        // ==========================================

        if (titleButton != null)
        {
            titleButton.onClick.AddListener(
                ReturnToTitle
            );
        }


        // ==========================================
        // Exit Button
        // ==========================================

        if (exitButton != null)
        {
            exitButton.onClick.AddListener(
                ExitGame
            );
        }
    }


    // =========================================================
    // Title
    // =========================================================

    public void ReturnToTitle()
    {
        Debug.Log(
            "[Setting] 타이틀 화면으로 이동"
        );


        // ==========================================
        // Inventory 등에서 게임을 정지했다면
        // 반드시 원상복구
        // ==========================================

        Time.timeScale =
            1f;


        // ==========================================
        // Cursor
        // ==========================================

        Cursor.lockState =
            CursorLockMode.None;


        Cursor.visible =
            true;


        // ==========================================
        // Scene Load
        // ==========================================

        SceneManager.LoadScene(
            titleSceneName
        );
    }


    // =========================================================
    // Exit
    // =========================================================

    public void ExitGame()
    {
        Debug.Log(
            "[Setting] 게임 종료"
        );


        // Build된 게임 종료
        Application.Quit();


        // Unity Editor에서 테스트할 때도
        // Play Mode 종료
#if UNITY_EDITOR

        UnityEditor.EditorApplication.isPlaying =
            false;

#endif
    }
}