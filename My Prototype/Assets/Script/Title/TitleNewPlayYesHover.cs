using UnityEngine;
using UnityEngine.EventSystems;

public class TitleNewPlayYesHover :
    MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("Player Model")]

    [SerializeField]
    private TitlePlayerModelAnimation playerModelAnimation;


    // =========================================================
    // Mouse Enter
    // =========================================================

    public void OnPointerEnter(
        PointerEventData eventData
    )
    {
        if (playerModelAnimation == null)
            return;


        playerModelAnimation
            .MoveToYesHoverPosition();
    }


    // =========================================================
    // Mouse Exit
    // =========================================================

    public void OnPointerExit(
        PointerEventData eventData
    )
    {
        if (playerModelAnimation == null)
            return;


        // Yes에서 마우스가 빠지면
        // New Play Panel이 열린 기본 위치로 복귀
        playerModelAnimation
            .MoveToPanelOpenPosition();
    }
}