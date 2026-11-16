using System.Collections;
using UnityEngine;

public class TitleReplayHoverAnimation : MonoBehaviour
{
    // =========================================================
    // References
    // =========================================================

    [Header("References")]

    [Tooltip("회전시킬 Main Camera")]
    [SerializeField]
    private Camera targetCamera;


    [Tooltip("카메라 회전 후 X축으로 움직일 Box")]
    [SerializeField]
    private Transform boxObject;


    // =========================================================
    // Camera
    // =========================================================

    [Header("Camera Rotation")]

    [Tooltip("현재 회전에서 추가로 회전할 각도")]
    [SerializeField]
    private Vector3 cameraRotationOffset =
        new Vector3(
            0f,
            -25f,
            0f
        );


    [Tooltip("카메라 회전 시간")]
    [SerializeField]
    private float cameraRotationDuration =
        0.35f;


    // =========================================================
    // Box
    // =========================================================

    [Header("Box Movement")]

    [Tooltip("카메라 회전 후 Box가 X축으로 움직일 거리")]
    [SerializeField]
    private float boxXOffset =
        1.5f;


    [Tooltip("Box 이동 시간")]
    [SerializeField]
    private float boxMoveDuration =
        0.3f;


    // =========================================================
    // Animation
    // =========================================================

    [Header("Animation")]

    [SerializeField]
    private AnimationCurve animationCurve =
        AnimationCurve.EaseInOut(
            0f,
            0f,
            1f,
            1f
        );


    // =========================================================
    // Runtime
    // =========================================================

    private Vector3 originalCameraPosition;

    private Quaternion originalCameraRotation;


    private Vector3 originalBoxLocalPosition;


    private Coroutine animationRoutine;


    private bool isHovered;


    // =========================================================
    // Awake
    // =========================================================

    private void Awake()
    {
        // Camera 자동 탐색
        if (targetCamera == null)
        {
            targetCamera =
                Camera.main;
        }


        if (targetCamera != null)
        {
            originalCameraPosition =
                targetCamera.transform.position;


            originalCameraRotation =
                targetCamera.transform.rotation;
        }


        if (boxObject != null)
        {
            originalBoxLocalPosition =
                boxObject.localPosition;
        }
    }


    // =========================================================
    // Hover Enter
    // =========================================================

    public void EnterHover()
    {
        if (isHovered)
            return;


        isHovered =
            true;


        if (animationRoutine != null)
        {
            StopCoroutine(
                animationRoutine
            );
        }


        animationRoutine =
            StartCoroutine(
                PlayEnterAnimation()
            );
    }


    // =========================================================
    // Hover Exit
    // =========================================================

    public void ExitHover()
    {
        if (!isHovered)
            return;


        isHovered =
            false;


        if (animationRoutine != null)
        {
            StopCoroutine(
                animationRoutine
            );
        }


        animationRoutine =
            StartCoroutine(
                PlayExitAnimation()
            );
    }


    // =========================================================
    // Enter Animation
    // =========================================================

    private IEnumerator PlayEnterAnimation()
    {
        if (targetCamera == null)
            yield break;


        // =====================================================
        // 1. Camera 회전
        // =====================================================

        Quaternion startRotation =
            targetCamera.transform.rotation;


        Quaternion targetRotation =
            originalCameraRotation *
            Quaternion.Euler(
                cameraRotationOffset
            );


        float timer =
            0f;


        while (timer <
               cameraRotationDuration)
        {
            // Hover가 끝났으면 중단
            if (!isHovered)
                yield break;


            timer +=
                Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    timer /
                    Mathf.Max(
                        0.01f,
                        cameraRotationDuration
                    )
                );


            float curved =
                animationCurve.Evaluate(
                    t
                );


            targetCamera.transform.rotation =
                Quaternion.Slerp(
                    startRotation,
                    targetRotation,
                    curved
                );


            // =================================================
            // Camera 위치는 절대 움직이지 않음
            // =================================================

            targetCamera.transform.position =
                originalCameraPosition;


            yield return null;
        }


        targetCamera.transform.rotation =
            targetRotation;


        targetCamera.transform.position =
            originalCameraPosition;


        // Hover가 이미 끝났다면 Box 이동 안 함
        if (!isHovered)
            yield break;


        // =====================================================
        // 2. Box X축 이동
        // =====================================================

        if (boxObject != null)
        {
            Vector3 startPosition =
                boxObject.localPosition;


            Vector3 targetPosition =
                originalBoxLocalPosition +
                Vector3.right *
                boxXOffset;


            timer =
                0f;


            while (timer <
                   boxMoveDuration)
            {
                if (!isHovered)
                    yield break;


                timer +=
                    Time.unscaledDeltaTime;


                float t =
                    Mathf.Clamp01(
                        timer /
                        Mathf.Max(
                            0.01f,
                            boxMoveDuration
                        )
                    );


                float curved =
                    animationCurve.Evaluate(
                        t
                    );


                boxObject.localPosition =
                    Vector3.Lerp(
                        startPosition,
                        targetPosition,
                        curved
                    );


                yield return null;
            }


            boxObject.localPosition =
                targetPosition;
        }


        animationRoutine =
            null;
    }


    // =========================================================
    // Exit Animation
    // =========================================================

    private IEnumerator PlayExitAnimation()
    {
        // =====================================================
        // 1. Box 먼저 원래 위치
        // =====================================================

        if (boxObject != null)
        {
            Vector3 startPosition =
                boxObject.localPosition;


            float timer =
                0f;


            while (timer <
                   boxMoveDuration)
            {
                timer +=
                    Time.unscaledDeltaTime;


                float t =
                    Mathf.Clamp01(
                        timer /
                        Mathf.Max(
                            0.01f,
                            boxMoveDuration
                        )
                    );


                float curved =
                    animationCurve.Evaluate(
                        t
                    );


                boxObject.localPosition =
                    Vector3.Lerp(
                        startPosition,
                        originalBoxLocalPosition,
                        curved
                    );


                yield return null;
            }


            boxObject.localPosition =
                originalBoxLocalPosition;
        }


        // =====================================================
        // 2. Camera 원래 방향으로 복귀
        // =====================================================

        if (targetCamera != null)
        {
            Quaternion startRotation =
                targetCamera.transform.rotation;


            float timer =
                0f;


            while (timer <
                   cameraRotationDuration)
            {
                timer +=
                    Time.unscaledDeltaTime;


                float t =
                    Mathf.Clamp01(
                        timer /
                        Mathf.Max(
                            0.01f,
                            cameraRotationDuration
                        )
                    );


                float curved =
                    animationCurve.Evaluate(
                        t
                    );


                targetCamera.transform.rotation =
                    Quaternion.Slerp(
                        startRotation,
                        originalCameraRotation,
                        curved
                    );


                // 위치 유지
                targetCamera.transform.position =
                    originalCameraPosition;


                yield return null;
            }


            targetCamera.transform.rotation =
                originalCameraRotation;


            targetCamera.transform.position =
                originalCameraPosition;
        }


        animationRoutine =
            null;
    }
}