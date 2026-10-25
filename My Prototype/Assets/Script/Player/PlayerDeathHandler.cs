using UnityEngine;

public class PlayerDeathHandler : MonoBehaviour
{
    [Header("Player")]
    [SerializeField]
    private PlayerStats playerStats;

    [SerializeField]
    private PlayerGameplayControl gameplayControl;


    private GameOverUI gameOverUI;


    private void Awake()
    {
        if (playerStats == null)
        {
            playerStats =
                GetComponent<PlayerStats>();
        }


        if (gameplayControl == null)
        {
            gameplayControl =
                GetComponent<PlayerGameplayControl>();
        }


        Debug.Log(
            $"[PlayerDeathHandler] Awake / " +
            $"Stats={playerStats != null}, " +
            $"GameplayControl={gameplayControl != null}"
        );
    }


    private void OnEnable()
    {
        if (playerStats != null)
        {
            playerStats.OnPlayerDied +=
                HandlePlayerDeath;


            Debug.Log(
                "[PlayerDeathHandler] 사망 이벤트 등록 완료"
            );
        }
    }


    private void Start()
    {
        FindGameOverUI();
    }


    private void OnDisable()
    {
        if (playerStats != null)
        {
            playerStats.OnPlayerDied -=
                HandlePlayerDeath;
        }
    }


    private void FindGameOverUI()
    {
        gameOverUI =
            FindFirstObjectByType<GameOverUI>(
                FindObjectsInactive.Include
            );


        if (gameOverUI != null)
        {
            Debug.Log(
                $"[PlayerDeathHandler] GameOverUI 발견 : " +
                $"{gameOverUI.gameObject.name}"
            );
        }
        else
        {
            Debug.LogError(
                "[PlayerDeathHandler] GameOverUI를 찾지 못했습니다."
            );
        }
    }


    private void HandlePlayerDeath()
    {
        Debug.Log(
            "[PlayerDeathHandler] 사망 이벤트 수신 성공"
        );


        // ==========================================
        // 플레이어 조작 완전 잠금
        // ==========================================

        if (gameplayControl != null)
        {
            gameplayControl.LockForDeath();
        }
        else
        {
            Debug.LogError(
                "[PlayerDeathHandler] PlayerGameplayControl이 없습니다."
            );
        }


        // ==========================================
        // Inventory가 열려있으면 닫기
        // ==========================================

        InventoryUIController inventoryUI =
            FindFirstObjectByType<InventoryUIController>(
                FindObjectsInactive.Include
            );


        if (inventoryUI != null &&
            inventoryUI.IsOpen)
        {
            inventoryUI.CloseInventory();
        }


        // ==========================================
        // GameOver UI
        // ==========================================

        if (gameOverUI == null)
        {
            FindGameOverUI();
        }


        if (gameOverUI != null)
        {
            gameOverUI.Open();
        }
        else
        {
            Debug.LogError(
                "[PlayerDeathHandler] GameOverPanel을 열 수 없습니다."
            );


            Cursor.lockState =
                CursorLockMode.None;

            Cursor.visible =
                true;
        }
    }
}