using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    // =========================================================
    // UI
    // =========================================================

    [Header("UI")]

    [SerializeField]
    private GameObject gameOverPanel;


    [SerializeField]
    private Button lobbyButton;


    // =========================================================
    // Scene
    // =========================================================

    [Header("Scene")]

    [Tooltip("돌아갈 Lobby Scene 이름")]
    [SerializeField]
    private string lobbySceneName =
        "LobbyScene";


    // =========================================================
    // Game
    // =========================================================

    [Header("Game Over")]

    [SerializeField]
    private bool pauseGameOnDeath = true;


    private bool opened;


    // =========================================================
    // Awake
    // =========================================================

    private void Awake()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(
                false
            );
        }


        if (lobbyButton != null)
        {
            lobbyButton.onClick.AddListener(
                ReturnToLobby
            );
        }
    }


    // =========================================================
    // Open
    // =========================================================

    public void Open()
    {
        if (opened)
            return;


        opened =
            true;


        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(
                true
            );


            // 다른 UI보다 앞으로
            gameOverPanel.transform.SetAsLastSibling();
        }


        // ==========================================
        // Mouse
        // ==========================================

        Cursor.lockState =
            CursorLockMode.None;

        Cursor.visible =
            true;


        // ==========================================
        // 전체 Game 정지
        // ==========================================

        if (pauseGameOnDeath)
        {
            Time.timeScale =
                0f;
        }


        Debug.Log(
            "[GameOver] GameOver Panel Open"
        );
    }


    // =========================================================
    // Lobby
    // =========================================================

    public void ReturnToLobby()
    {
        Debug.Log(
            "[GameOver] Return To Lobby"
        );


        // 정지 상태 해제
        Time.timeScale =
            1f;


        SceneManager.LoadScene(
            lobbySceneName
        );
    }
}