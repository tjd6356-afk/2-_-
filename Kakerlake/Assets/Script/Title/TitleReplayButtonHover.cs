using UnityEngine;
using UnityEngine.EventSystems;

public class TitleReplayButtonHover :
    MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("Animation")]

    [SerializeField]
    private TitleReplayHoverAnimation
        replayAnimation;


    // =========================================================
    // Mouse Enter
    // =========================================================

    public void OnPointerEnter(
        PointerEventData eventData
    )
    {
        if (replayAnimation == null)
            return;


        replayAnimation.EnterHover();
    }


    // =========================================================
    // Mouse Exit
    // =========================================================

    public void OnPointerExit(
        PointerEventData eventData
    )
    {
        if (replayAnimation == null)
            return;


        replayAnimation.ExitHover();
    }
}