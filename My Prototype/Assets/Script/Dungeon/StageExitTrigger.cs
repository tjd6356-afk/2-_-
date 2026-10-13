using UnityEngine;

public class StageExitTrigger : MonoBehaviour
{
    [Header("UI")]

    [SerializeField]
    private StageExitPanelUI stageExitPanel;


    private void Start()
    {
        // Prefab은 Scene의 Canvas를 미리 참조할 수 없으므로
        // 런타임에 찾는다.
        if (stageExitPanel == null)
        {
            stageExitPanel =
                FindFirstObjectByType<StageExitPanelUI>();
        }


        if (stageExitPanel == null)
        {
            Debug.LogError(
                "[StageExitTrigger] StageExitPanelUI를 찾을 수 없습니다."
            );
        }
    }


    private void OnTriggerEnter(
        Collider other
    )
    {
        PlayerStats player =
            other.GetComponentInParent<PlayerStats>();


        if (player == null)
            return;


        if (stageExitPanel == null)
            return;


        Debug.Log(
            "[StageExitTrigger] Player 도착"
        );


        stageExitPanel.Open();
    }
}