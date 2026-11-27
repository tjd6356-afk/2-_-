using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
public class StageExitTrigger : MonoBehaviour
{
    [Header("UI")]

    [SerializeField]
    private StageExitPanelUI stageExitPanel;


    // =========================================================
    // Runtime
    // =========================================================

    private PlayerGameplayControl playerControl;

    private bool playerInside;

    // 플레이어에게 Collider가 여러 개 있는 경우 대응
    private readonly HashSet<Collider> playerColliders =
        new HashSet<Collider>();


    private void Awake()
    {
        Collider trigger =
            GetComponent<Collider>();

        trigger.isTrigger = true;


        if (stageExitPanel == null)
        {
            stageExitPanel =
                FindFirstObjectByType<StageExitPanelUI>(
                    FindObjectsInactive.Include
                );
        }
    }


    // =========================================================
    // DungeonGridGenerator 호환
    // =========================================================

    public void SetStageExitPanel(
        StageExitPanelUI panel
    )
    {
        stageExitPanel = panel;
    }


    // =========================================================
    // E키 상호작용
    // =========================================================

    private void Update()
    {
        if (!playerInside)
            return;


        if (stageExitPanel == null)
            return;


        if (stageExitPanel.IsOpen ||
            stageExitPanel.IsMovingStage)
        {
            return;
        }


        // 다른 UI 사용 중이면 상호작용 금지

        if (PlayerGameplayControl.GameplayLocked)
            return;


        if (Keyboard.current == null)
            return;


        // 트리거 내부에서 E키를 눌렀을 때만 Open

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            stageExitPanel.Open(
                playerControl
            );


            Debug.Log(
                "[StageExit] E키 입력 / UI 열기"
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
        PlayerStats playerStats =
            other.GetComponentInParent<PlayerStats>();


        if (playerStats == null)
            return;


        playerColliders.Add(other);

        playerInside = true;


        playerControl =
            playerStats.GetComponent<PlayerGameplayControl>();


        // 여기에서는 UI를 열지 않는다.

        Debug.Log(
            "[StageExit] Player 진입 / E키 입력 대기"
        );
    }


    // =========================================================
    // Player 퇴장
    // =========================================================

    private void OnTriggerExit(
        Collider other
    )
    {
        if (!playerColliders.Remove(other))
            return;


        // 플레이어의 다른 Collider가 아직 내부에 있음

        if (playerColliders.Count > 0)
            return;


        playerInside = false;


        if (stageExitPanel != null &&
            stageExitPanel.IsOpen &&
            !stageExitPanel.IsMovingStage)
        {
            stageExitPanel.Close();
        }


        playerControl = null;


        Debug.Log(
            "[StageExit] Player 퇴장"
        );
    }
}