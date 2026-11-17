using System.Collections;
using UnityEngine;

public class TitlePlayerModelAnimation : MonoBehaviour
{
    // =========================================================
    // Settings
    // =========================================================

    [Header("Z Position")]

    [Tooltip("New Play Panel이 열렸을 때 Z축으로 이동할 거리")]
    [SerializeField]
    private float panelOpenZOffset = 0.5f;


    [Tooltip("Yes 버튼 Hover 시 Z축으로 이동할 거리")]
    [SerializeField]
    private float yesHoverZOffset = 1.5f;


    [Header("Animation")]

    [Tooltip("일반 이동 시간")]
    [SerializeField]
    private float moveDuration = 0.25f;


    [Tooltip("Yes Hover 때 튀어나오는 시간")]
    [SerializeField]
    private float hoverMoveDuration = 0.12f;


    [SerializeField]
    private AnimationCurve moveCurve =
        AnimationCurve.EaseInOut(
            0f,
            0f,
            1f,
            1f
        );


    // =========================================================
    // Runtime
    // =========================================================

    private Vector3 originalLocalPosition;


    private Coroutine moveRoutine;


    // =========================================================
    // Awake
    // =========================================================

    private void Awake()
    {
        originalLocalPosition =
            transform.localPosition;
    }


    // =========================================================
    // 기본 위치
    // =========================================================

    public void MoveToOriginal()
    {
        Vector3 target =
            originalLocalPosition;


        StartMove(
            target,
            moveDuration
        );
    }


    // =========================================================
    // New Play Panel Open
    // =========================================================

    public void MoveToPanelOpenPosition()
    {
        Vector3 target =
            originalLocalPosition;


        target.z +=
            panelOpenZOffset;


        StartMove(
            target,
            moveDuration
        );
    }


    // =========================================================
    // Yes Hover
    // =========================================================

    public void MoveToYesHoverPosition()
    {
        Vector3 target =
            originalLocalPosition;


        target.z +=
            yesHoverZOffset;


        StartMove(
            target,
            hoverMoveDuration
        );
    }


    // =========================================================
    // Move
    // =========================================================

    private void StartMove(
        Vector3 targetPosition,
        float duration
    )
    {
        if (moveRoutine != null)
        {
            StopCoroutine(
                moveRoutine
            );
        }


        moveRoutine =
            StartCoroutine(
                MoveRoutine(
                    targetPosition,
                    duration
                )
            );
    }


    private IEnumerator MoveRoutine(
        Vector3 targetPosition,
        float duration
    )
    {
        Vector3 startPosition =
            transform.localPosition;


        float elapsed =
            0f;


        duration =
            Mathf.Max(
                0.01f,
                duration
            );


        while (elapsed <
               duration)
        {
            elapsed +=
                Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    elapsed /
                    duration
                );


            float curvedT =
                moveCurve.Evaluate(
                    t
                );


            transform.localPosition =
                Vector3.Lerp(
                    startPosition,
                    targetPosition,
                    curvedT
                );


            yield return null;
        }


        transform.localPosition =
            targetPosition;


        moveRoutine =
            null;
    }
}