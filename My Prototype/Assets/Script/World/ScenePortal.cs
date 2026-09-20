using UnityEngine;

public class ScenePortal : MonoBehaviour
{
    [Header("Destination")]

    [Tooltip("실제 Unity Scene 이름")]
    [SerializeField]
    private string destinationSceneName = "GameScene";

    [Tooltip("UI에 보여줄 장소 이름")]
    [SerializeField]
    private string destinationDisplayName = "전투 지역";


    [Header("References")]

    [SerializeField]
    private SceneTransitionPanelUI transitionPanel;


    private PlayerGameplayControl playerControl;

    private bool playerInside;


    private void Awake()
    {
        // 직접 연결하지 않았으면 자동 검색
        if (transitionPanel == null)
        {
            transitionPanel =
                FindFirstObjectByType<SceneTransitionPanelUI>();
        }


        if (transitionPanel == null)
        {
            Debug.LogError(
                "[ScenePortal] SceneTransitionPanelUI를 찾을 수 없습니다."
            );
        }
    }


    // =========================================================
    // Player 진입
    // =========================================================

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log(
            $"[ScenePortal] Trigger Enter : {other.gameObject.name}"
        );


        PlayerStats playerStats =
            other.GetComponentInParent<PlayerStats>();


        if (playerStats == null)
        {
            Debug.Log(
                "[ScenePortal] 들어온 오브젝트는 Player가 아닙니다."
            );

            return;
        }


        if (playerInside)
            return;


        playerInside = true;


        playerControl =
            playerStats.GetComponent<PlayerGameplayControl>();


        Debug.Log(
            "[ScenePortal] Player 감지 성공. UI를 엽니다."
        );


        if (transitionPanel == null)
        {
            Debug.LogError(
                "[ScenePortal] Transition Panel이 없습니다."
            );

            return;
        }


        transitionPanel.Open(
            destinationSceneName,
            destinationDisplayName,
            playerControl
        );
    }


    // =========================================================
    // Player 퇴장
    // =========================================================

    private void OnTriggerExit(Collider other)
    {
        PlayerStats playerStats =
            other.GetComponentInParent<PlayerStats>();


        if (playerStats == null)
            return;


        playerInside = false;


        if (transitionPanel != null &&
            transitionPanel.IsOpen)
        {
            transitionPanel.Close();
        }


        playerControl = null;


        Debug.Log(
            "[ScenePortal] Player가 Portal에서 나갔습니다."
        );
    }
}