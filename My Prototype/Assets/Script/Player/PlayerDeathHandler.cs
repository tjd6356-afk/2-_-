using UnityEngine;

public class PlayerDeathHandler : MonoBehaviour
{
    // =========================================================
    // References
    // =========================================================

    [Header("Player")]

    [SerializeField]
    private PlayerStats playerStats;


    [SerializeField]
    private PlayerGameplayControl gameplayControl;


    private GameOverUI gameOverUI;


    // =========================================================
    // Awake
    // =========================================================

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
    }


    // =========================================================
    // Subscribe
    // =========================================================

    private void OnEnable()
    {
        if (playerStats != null)
        {
            playerStats.OnPlayerDied +=
                HandlePlayerDeath;
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


    // =========================================================
    // GameOver UI 자동 탐색
    // =========================================================

    private void FindGameOverUI()
    {
        gameOverUI =
            FindFirstObjectByType<GameOverUI>(
                FindObjectsInactive.Include
            );


        if (gameOverUI == null)
        {
            Debug.LogWarning(
                "[PlayerDeath] GameOverUI를 찾지 못했습니다."
            );
        }
    }


    // =========================================================
    // Death
    // =========================================================

    private void HandlePlayerDeath()
    {
        Debug.Log(
            "[PlayerDeath] 사망 처리 시작"
        );


        // ==========================================
        // 가장 먼저 Death Lock
        // ==========================================

        if (gameplayControl != null)
        {
            gameplayControl.LockForDeath();
        }


        // ==========================================
        // 열려있는 Inventory가 있다면 닫음
        // UnlockControls가 호출되어도
        // DeathLock 때문에 조작은 복구되지 않음
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
        // GameOver
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
            // UI가 없어도 Mouse는 확보
            Cursor.lockState =
                CursorLockMode.None;

            Cursor.visible =
                true;
        }
    }
}