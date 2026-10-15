using UnityEngine;

public class StageExitTrigger : MonoBehaviour
{
    [Header("Runtime UI")]

    [SerializeField]
    private StageExitPanelUI stageExitPanel;


    private bool playerInside;


    private void Start()
    {
        FindStageExitPanel();
    }


    // =========================================================
    // StageExitPanel 자동 탐색
    // =========================================================

    private void FindStageExitPanel()
    {
        // 1순위: Instance
        if (StageExitPanelUI.Instance != null)
        {
            stageExitPanel =
                StageExitPanelUI.Instance;


            Debug.Log(
                "[StageExitTrigger] StageExitPanelUI 자동 연결 성공"
            );

            return;
        }


        // ==========================================
        // 혹시 비활성화된 오브젝트에 붙어있더라도 검색
        // ==========================================

        stageExitPanel =
            FindFirstObjectByType<StageExitPanelUI>(
                FindObjectsInactive.Include
            );


        if (stageExitPanel != null)
        {
            Debug.Log(
                "[StageExitTrigger] 비활성 UI까지 검색해서 연결 성공"
            );
        }
        else
        {
            Debug.LogError(
                "[StageExitTrigger] StageExitPanelUI를 찾지 못했습니다."
            );
        }
    }


    // =========================================================
    // DungeonGenerator에서 직접 전달할 수도 있음
    // =========================================================

    public void SetStageExitPanel(
        StageExitPanelUI panel
    )
    {
        stageExitPanel =
            panel;


        if (stageExitPanel != null)
        {
            Debug.Log(
                "[StageExitTrigger] StageExitPanelUI 직접 등록 성공"
            );
        }
    }


    // =========================================================
    // Player 진입
    // =========================================================

    private void OnTriggerEnter(
    Collider other
)
    {
        if (playerInside)
            return;


        PlayerStats playerStats =
            other.GetComponentInParent<PlayerStats>();


        if (playerStats == null)
            return;


        playerInside =
            true;


        if (stageExitPanel == null)
        {
            FindStageExitPanel();
        }


        if (stageExitPanel == null)
        {
            Debug.LogError(
                "[StageExitTrigger] StageExitPanelUI가 없습니다."
            );

            return;
        }


        // ==========================================
        // Player 조작 관리자 찾기
        // ==========================================

        PlayerGameplayControl gameplayControl =
            playerStats.GetComponent<PlayerGameplayControl>();


        if (gameplayControl == null)
        {
            gameplayControl =
                playerStats.GetComponentInParent<PlayerGameplayControl>();
        }


        Debug.Log(
            "[StageExitTrigger] Stage Exit Panel Open"
        );


        stageExitPanel.Open(
            gameplayControl
        );
    }


    private void OnTriggerExit(
        Collider other
    )
    {
        PlayerStats player =
            other.GetComponentInParent<PlayerStats>();


        if (player == null)
            return;


        playerInside =
            false;
    }
}