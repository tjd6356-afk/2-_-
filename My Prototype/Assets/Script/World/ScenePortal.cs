using UnityEngine;

public class ScenePortal : MonoBehaviour
{
    [Header("Destination")]

    [Tooltip("실제 Unity Scene 이름")]
    [SerializeField]
    private string destinationSceneName;

    [Tooltip("UI에 표시할 이름")]
    [SerializeField]
    private string destinationDisplayName =
        "게임";


    [Header("References")]

    [SerializeField]
    private SceneTransitionPanelUI transitionPanel;


    private PlayerGameplayControl playerControl;

    private bool playerInside;


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


        playerControl =
            playerStats.GetComponent<PlayerGameplayControl>();


        playerInside =
            true;


        if (transitionPanel != null)
        {
            transitionPanel.Open(
                destinationSceneName,
                destinationDisplayName,
                playerControl
            );
        }
    }


    // =========================================================
    // Player 퇴장
    // =========================================================

    private void OnTriggerExit(
        Collider other
    )
    {
        PlayerStats playerStats =
            other.GetComponentInParent<PlayerStats>();


        if (playerStats == null)
            return;


        playerInside =
            false;


        if (transitionPanel != null &&
            transitionPanel.IsOpen)
        {
            transitionPanel.Close();
        }


        playerControl = null;
    }
}