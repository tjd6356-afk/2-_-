using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BossBattleTrigger : MonoBehaviour
{
    [Header("Boss")]

    [SerializeField]
    private BossStats bossStats;


    [SerializeField]
    private BossPatternAI bossAI;


    [Header("UI")]

    [SerializeField]
    private BossHealthUI bossHealthUI;


    private bool battleStarted;


    private void Awake()
    {
        Collider trigger =
            GetComponent<Collider>();


        trigger.isTrigger =
            true;


        if (bossHealthUI == null)
        {
            bossHealthUI =
                FindFirstObjectByType<BossHealthUI>(
                    FindObjectsInactive.Include
                );
        }
    }


    private void Start()
    {
        if (bossAI != null)
        {
            bossAI.SetBattleActive(
                false
            );
        }


        if (bossHealthUI != null)
        {
            bossHealthUI.Hide();
        }
    }


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


    private void StartBattle(
        PlayerStats player
    )
    {
        battleStarted =
            true;


        Debug.Log(
            "[BossBattle] Battle Start"
        );


        if (bossAI != null)
        {
            bossAI.SetTarget(
                player.transform
            );


            bossAI.SetBattleActive(
                true
            );
        }


        if (bossHealthUI != null &&
            bossStats != null)
        {
            bossHealthUI.Open(
                bossStats
            );
        }
    }
}