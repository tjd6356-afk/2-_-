using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BossBattleTrigger : MonoBehaviour
{
    // =========================================================
    // Boss
    // =========================================================

    [Header("Boss")]

    [SerializeField]
    private BossStats bossStats;


    [SerializeField]
    private BossPatternAI bossAI;


    // =========================================================
    // UI
    // =========================================================

    [Header("UI")]

    [SerializeField]
    private BossHealthUI bossHealthUI;


    [Tooltip("보스 체력바에 표시할 이름")]
    [SerializeField]
    private string bossName = "BOSS";


    // =========================================================
    // Runtime
    // =========================================================

    private bool battleStarted;


    // =========================================================
    // Awake
    // =========================================================

    private void Awake()
    {
        Collider trigger =
            GetComponent<Collider>();


        trigger.isTrigger =
            true;


        // ==========================================
        // BossStats 자동 연결
        // ==========================================

        if (bossStats == null)
        {
            bossStats =
                GetComponentInParent<BossStats>();
        }


        // ==========================================
        // Boss AI 자동 연결
        // ==========================================

        if (bossAI == null)
        {
            bossAI =
                GetComponentInParent<BossPatternAI>();
        }


        // ==========================================
        // Boss Health UI 자동 검색
        // ==========================================

        if (bossHealthUI == null)
        {
            bossHealthUI =
                FindFirstObjectByType<BossHealthUI>(
                    FindObjectsInactive.Include
                );
        }
    }


    // =========================================================
    // Start
    // =========================================================

    private void Start()
    {
        // 처음에는 Boss AI 비활성
        if (bossAI != null)
        {
            bossAI.SetBattleActive(
                false
            );
        }


        // 처음에는 Boss HP UI 숨김
        if (bossHealthUI != null)
        {
            bossHealthUI.HideBoss();
        }
    }


    // =========================================================
    // Trigger
    // =========================================================

    private void OnTriggerEnter(
        Collider other
    )
    {
        if (battleStarted)
            return;


        PlayerStats player =
            other.GetComponentInParent<PlayerStats>();


        if (player == null)
            return;


        StartBattle(
            player
        );
    }


    // =========================================================
    // Battle Start
    // =========================================================

    private void StartBattle(
        PlayerStats player
    )
    {
        if (battleStarted)
            return;


        battleStarted =
            true;


        Debug.Log(
            "[BossBattle] Battle Start"
        );


        // ==========================================
        // Boss AI 시작
        // ==========================================

        if (bossAI != null)
        {
            bossAI.SetTarget(
                player.transform
            );


            bossAI.SetBattleActive(
                true
            );
        }


        // ==========================================
        // Boss HP UI 시작
        // ==========================================

        if (bossHealthUI != null &&
            bossStats != null)
        {
            bossHealthUI.ShowBoss(
                bossStats,
                bossName
            );
        }
    }
}