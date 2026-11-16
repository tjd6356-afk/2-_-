using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleDoorTransition : MonoBehaviour
{
    // =========================================================
    // References
    // =========================================================

    [Header("References")]

    [SerializeField]
    private Camera titleCamera;


    [Tooltip("사진의 Door2 오브젝트")]
    [SerializeField]
    private Transform door2;


    [Tooltip("클릭 후 조작을 막을 버튼들")]
    [SerializeField]
    private Button[] titleButtons;


    [Tooltip("RePlay Hover 카메라 연출")]
    [SerializeField]
    private TitleReplayHoverAnimation replayHoverAnimation;


    // =========================================================
    // Camera Alignment
    // =========================================================

    [Header("Camera Alignment")]

    [Tooltip("Door2를 바라보기 전에 카메라가 내려갈 Y 위치")]
    [SerializeField]
    private float cameraTargetY =
        -2.34f;


    [Tooltip("카메라 X도 Door2 중앙에 맞출 것인가")]
    [SerializeField]
    private bool alignCameraXToDoor =
        true;


    [Tooltip("Door 중앙에서 어느 높이를 바라볼지")]
    [SerializeField]
    private float doorLookHeightOffset =
        0f;


    [Tooltip("Door 정면으로 정렬되는 시간")]
    [SerializeField]
    private float cameraAlignDuration =
        0.7f;


    // =========================================================
    // Camera Forward
    // =========================================================

    [Header("Camera Forward")]

    [Tooltip("Door를 통과한 뒤 최종적으로 이동할 추가 Z 거리")]
    [SerializeField]
    private float cameraPassDistance =
        2f;


    [Tooltip("Door까지 전진하는 시간")]
    [SerializeField]
    private float cameraForwardDuration =
        2f;


    [Tooltip("이 Z 좌표를 넘는 순간 Scene 이동")]
    [SerializeField]
    private float sceneLoadZ =
        7.88f;


    // =========================================================
    // Door
    // =========================================================

    [Header("Door Open")]

    [Tooltip("Door2가 오른쪽으로 이동할 거리")]
    [SerializeField]
    private float doorOpenXDistance =
        3f;


    [Tooltip("Door가 완전히 열리기까지 걸리는 시간")]
    [SerializeField]
    private float doorOpenDuration =
        1.3f;


    // =========================================================
    // Animation
    // =========================================================

    [Header("Animation")]

    [SerializeField]
    private AnimationCurve cameraCurve =
        AnimationCurve.EaseInOut(
            0f,
            0f,
            1f,
            1f
        );


    [SerializeField]
    private AnimationCurve doorCurve =
        AnimationCurve.EaseInOut(
            0f,
            0f,
            1f,
            1f
        );


    // =========================================================
    // Runtime
    // =========================================================

    private Vector3 doorClosedLocalPosition;


    private bool isTransitioning;


    public bool IsTransitioning =>
        isTransitioning;


    // =========================================================
    // Awake
    // =========================================================

    private void Awake()
    {
        if (titleCamera == null)
        {
            titleCamera =
                Camera.main;
        }


        if (door2 != null)
        {
            doorClosedLocalPosition =
                door2.localPosition;
        }
    }


    // =========================================================
    // Transition Start
    // =========================================================

    public void PlayTransition(
        string sceneName
    )
    {
        if (isTransitioning)
            return;


        if (titleCamera == null)
        {
            Debug.LogError(
                "[TitleTransition] Camera가 없습니다."
            );

            return;
        }


        if (door2 == null)
        {
            Debug.LogError(
                "[TitleTransition] Door2가 없습니다."
            );

            return;
        }


        if (string.IsNullOrWhiteSpace(
                sceneName))
        {
            Debug.LogError(
                "[TitleTransition] 이동할 Scene 이름이 없습니다."
            );

            return;
        }


        isTransitioning =
            true;


        // ==========================================
        // 버튼 사용 금지
        // ==========================================

        foreach (
            Button button in titleButtons)
        {
            if (button != null)
            {
                button.interactable =
                    false;
            }
        }


        // ==========================================
        // 기존 RePlay Hover 연출 중지
        // ==========================================

        if (replayHoverAnimation != null)
        {
            replayHoverAnimation
                .LockForSceneTransition();
        }


        StartCoroutine(
            TransitionRoutine(
                sceneName
            )
        );
    }


    // =========================================================
    // Main Transition
    // =========================================================

    private IEnumerator TransitionRoutine(
        string sceneName
    )
    {
        Transform cameraTransform =
            titleCamera.transform;


        // =====================================================
        // 1. Camera를 Door 정면에 맞춤
        // =====================================================

        Vector3 startPosition =
            cameraTransform.position;


        Quaternion startRotation =
            cameraTransform.rotation;


        Vector3 alignPosition =
            startPosition;


        // Y를 지정한 높이까지 내림
        alignPosition.y =
            cameraTargetY;


        // Door 중앙 X축에 맞춤
        if (alignCameraXToDoor)
        {
            alignPosition.x =
                door2.position.x;
        }


        // 현재 Z는 유지
        alignPosition.z =
            startPosition.z;


        // =====================================================
        // Door를 정면으로 바라보는 Rotation
        // =====================================================

        Vector3 lookPosition =
            door2.position +
            Vector3.up *
            doorLookHeightOffset;


        Vector3 lookDirection =
            lookPosition -
            alignPosition;


        Quaternion alignRotation;


        if (lookDirection.sqrMagnitude >
            0.001f)
        {
            alignRotation =
                Quaternion.LookRotation(
                    lookDirection.normalized,
                    Vector3.up
                );
        }
        else
        {
            alignRotation =
                startRotation;
        }


        float timer =
            0f;


        while (timer <
               cameraAlignDuration)
        {
            timer +=
                Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    timer /
                    Mathf.Max(
                        0.01f,
                        cameraAlignDuration
                    )
                );


            float curved =
                cameraCurve.Evaluate(
                    t
                );


            cameraTransform.position =
                Vector3.Lerp(
                    startPosition,
                    alignPosition,
                    curved
                );


            cameraTransform.rotation =
                Quaternion.Slerp(
                    startRotation,
                    alignRotation,
                    curved
                );


            yield return null;
        }


        cameraTransform.position =
            alignPosition;


        cameraTransform.rotation =
            alignRotation;


        // =====================================================
        // 2. Camera 전진 + Door Open
        // =====================================================

        Vector3 forwardStart =
            cameraTransform.position;


        // 카메라는 Door보다 Z축으로 더 깊이 들어감
        Vector3 forwardTarget =
            new Vector3(
                door2.position.x,
                cameraTargetY,
                sceneLoadZ +
                cameraPassDistance
            );


        Vector3 doorStart =
            doorClosedLocalPosition;


        Vector3 doorTarget =
            doorClosedLocalPosition +
            Vector3.right *
            doorOpenXDistance;


        timer =
            0f;


        while (true)
        {
            timer +=
                Time.unscaledDeltaTime;


            // =================================================
            // Camera
            // =================================================

            float cameraT =
                Mathf.Clamp01(
                    timer /
                    Mathf.Max(
                        0.01f,
                        cameraForwardDuration
                    )
                );


            float cameraCurved =
                cameraCurve.Evaluate(
                    cameraT
                );


            cameraTransform.position =
                Vector3.Lerp(
                    forwardStart,
                    forwardTarget,
                    cameraCurved
                );


            // Door를 계속 바라보게 함
            Vector3 currentLookDirection =
                lookPosition -
                cameraTransform.position;


            if (currentLookDirection.sqrMagnitude >
                0.001f)
            {
                cameraTransform.rotation =
                    Quaternion.LookRotation(
                        currentLookDirection.normalized,
                        Vector3.up
                    );
            }


            // =================================================
            // Door 오른쪽으로 Open
            // =================================================

            float doorT =
                Mathf.Clamp01(
                    timer /
                    Mathf.Max(
                        0.01f,
                        doorOpenDuration
                    )
                );


            float doorCurved =
                doorCurve.Evaluate(
                    doorT
                );


            door2.localPosition =
                Vector3.Lerp(
                    doorStart,
                    doorTarget,
                    doorCurved
                );


            // =================================================
            // Door Z = 7.88 통과
            // =================================================

            if (cameraTransform.position.z >=
                sceneLoadZ)
            {
                break;
            }


            yield return null;
        }


        // =====================================================
        // 3. Scene 이동
        // =====================================================

        Debug.Log(
            $"[TitleTransition] Door 통과 → {sceneName}"
        );


        SceneManager.LoadScene(
            sceneName
        );
    }
}