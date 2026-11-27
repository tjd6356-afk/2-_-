using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class ScenePortal : MonoBehaviour
{
    // =========================================================
    // Destination
    // =========================================================

    [Header("Destination")]

    [Tooltip("실제 Unity Scene 이름")]
    [SerializeField]
    private string destinationSceneName =
        "GameScene";


    [Tooltip("UI에 보여줄 장소 이름")]
    [SerializeField]
    private string destinationDisplayName =
        "전투 지역";


    // =========================================================
    // UI
    // =========================================================

    [Header("References")]

    [SerializeField]
    private SceneTransitionPanelUI transitionPanel;


    // =========================================================
    // Portal Camera
    // =========================================================

    [Header("Portal Camera Transition")]

    [Tooltip("포탈 연출이 시작될 카메라 위치")]
    [SerializeField]
    private Transform cameraStartPoint;


    [Tooltip("포탈 쪽으로 전진한 후 도착할 위치")]
    [SerializeField]
    private Transform cameraEndPoint;


    [Tooltip("현재 Main Camera")]
    [SerializeField]
    private Camera mainCamera;


    [Tooltip("현재 플레이어 ThirdPersonCamera")]
    [SerializeField]
    private ThirdPersonCamera thirdPersonCamera;


    // =========================================================
    // Transition Settings
    // =========================================================

    [Header("Transition Settings")]

    [Tooltip("현재 카메라 위치에서 Portal 정면까지 이동하는 시간")]
    [SerializeField]
    private float alignDuration =
        0.5f;


    [Tooltip("Portal 쪽으로 전진하는 시간")]
    [SerializeField]
    private float moveForwardDuration =
        1.5f;


    [Tooltip("카메라 이동 곡선")]
    [SerializeField]
    private AnimationCurve moveCurve =
        AnimationCurve.EaseInOut(
            0f,
            0f,
            1f,
            1f
        );


    [Tooltip("연출 종료 후 Scene Load 전 잠깐 기다리는 시간")]
    [SerializeField]
    private float loadDelay =
        0.1f;


    // =========================================================
    // Runtime
    // =========================================================

    private PlayerGameplayControl playerControl;

    private bool playerInside;

    private bool transitionPlaying;


    // =========================================================
    // Awake
    // =========================================================

    private void Awake()
    {
        // ==========================================
        // UI 자동 탐색
        // ==========================================

        if (transitionPanel == null)
        {
            transitionPanel =
                FindFirstObjectByType
                <SceneTransitionPanelUI>();
        }


        if (transitionPanel == null)
        {
            Debug.LogError(
                "[ScenePortal] SceneTransitionPanelUI를 " +
                "찾을 수 없습니다."
            );
        }


        // ==========================================
        // Main Camera 자동 탐색
        // ==========================================

        if (mainCamera == null)
        {
            mainCamera =
                Camera.main;
        }


        // ==========================================
        // ThirdPersonCamera 자동 탐색
        // ==========================================

        if (mainCamera != null &&
            thirdPersonCamera == null)
        {
            thirdPersonCamera =
                mainCamera.GetComponent
                <ThirdPersonCamera>();
        }
    }


    // =========================================================
    // Player Interaction
    // =========================================================

    private void Update()
    {
        // 포탈 이동 연출 중에는 입력 금지
        if (transitionPlaying)
            return;


        // 플레이어가 트리거 밖에 있으면 입력 금지
        if (!playerInside)
            return;


        if (transitionPanel == null)
            return;


        // 이미 씬 전환 UI가 열려 있다면 무시
        if (transitionPanel.IsOpen)
            return;


        // 인벤토리, NPC 대화, 상점 등의
        // 다른 UI가 열려 있으면 입력 금지
        if (PlayerGameplayControl.GameplayLocked)
            return;


        if (Keyboard.current == null)
            return;


        // =====================================================
        // E키 입력
        // =====================================================

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            transitionPanel.Open(
                destinationSceneName,
                destinationDisplayName,
                playerControl,
                this
            );


            Debug.Log(
                "[ScenePortal] E키 입력 / 씬 전환 UI 열기"
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
        if (transitionPlaying)
            return;


        PlayerStats playerStats =
            other.GetComponentInParent<PlayerStats>();


        if (playerStats == null)
            return;


        if (playerInside)
            return;


        playerInside =
            true;


        playerControl =
            playerStats.GetComponent<PlayerGameplayControl>();


        Debug.Log(
            "[ScenePortal] Player 진입 / E키 입력 대기"
        );
    }


    // =========================================================
    // Player 퇴장
    // =========================================================

    private void OnTriggerExit(
        Collider other
    )
    {
        if (transitionPlaying)
            return;


        PlayerStats playerStats =
            other.GetComponentInParent
            <PlayerStats>();


        if (playerStats == null)
            return;


        playerInside =
            false;


        if (transitionPanel != null &&
            transitionPanel.IsOpen)
        {
            transitionPanel.Close();
        }


        playerControl =
            null;
    }


    // =========================================================
    // UI 이동 버튼에서 호출
    // =========================================================

    public void StartPortalTransition()
    {
        if (transitionPlaying)
            return;


        if (!playerInside)
            return;


        StartCoroutine(
            PortalTransitionRoutine()
        );
    }


    // =========================================================
    // Portal 연출
    // =========================================================

    private IEnumerator PortalTransitionRoutine()
    {
        transitionPlaying =
            true;


        // =====================================================
        // Panel 숨김
        // =====================================================

        if (transitionPanel != null)
        {
            transitionPanel.CloseForTransition();
        }


        // =====================================================
        // Player 조작 잠금
        // =====================================================

        if (playerControl != null)
        {
            playerControl.LockControls();
        }


        // =====================================================
        // Camera 확인
        // =====================================================

        if (mainCamera == null)
        {
            mainCamera =
                Camera.main;
        }


        if (mainCamera == null)
        {
            Debug.LogError(
                "[ScenePortal] Main Camera가 없습니다."
            );


            LoadDestinationScene();

            yield break;
        }


        if (thirdPersonCamera == null)
        {
            thirdPersonCamera =
                mainCamera.GetComponent
                <ThirdPersonCamera>();
        }


        // 평소 카메라 움직임 정지
        if (thirdPersonCamera != null)
        {
            thirdPersonCamera.enabled =
                false;
        }


        // =====================================================
        // 1단계
        // 현재 카메라 → Portal 정면 위치
        // =====================================================

        if (cameraStartPoint != null)
        {
            yield return
                MoveCamera(
                    mainCamera.transform.position,
                    mainCamera.transform.rotation,
                    cameraStartPoint.position,
                    cameraStartPoint.rotation,
                    alignDuration
                );
        }


        // =====================================================
        // 2단계
        // Portal 방향으로 천천히 전진
        // =====================================================

        if (cameraEndPoint != null)
        {
            yield return
                MoveCamera(
                    mainCamera.transform.position,
                    mainCamera.transform.rotation,
                    cameraEndPoint.position,
                    cameraEndPoint.rotation,
                    moveForwardDuration
                );
        }


        // =====================================================
        // Scene 전환 직전 짧은 대기
        // =====================================================

        if (loadDelay > 0f)
        {
            yield return
                new WaitForSecondsRealtime(
                    loadDelay
                );
        }


        // =====================================================
        // Scene 이동
        // =====================================================

        LoadDestinationScene();
    }


    // =========================================================
    // Camera Movement
    // =========================================================

    private IEnumerator MoveCamera(
        Vector3 startPosition,
        Quaternion startRotation,
        Vector3 targetPosition,
        Quaternion targetRotation,
        float duration
    )
    {
        if (duration <= 0f)
        {
            mainCamera.transform.position =
                targetPosition;


            mainCamera.transform.rotation =
                targetRotation;


            yield break;
        }


        float elapsed =
            0f;


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


            mainCamera.transform.position =
                Vector3.Lerp(
                    startPosition,
                    targetPosition,
                    curvedT
                );


            mainCamera.transform.rotation =
                Quaternion.Slerp(
                    startRotation,
                    targetRotation,
                    curvedT
                );


            yield return null;
        }


        mainCamera.transform.position =
            targetPosition;


        mainCamera.transform.rotation =
            targetRotation;
    }


    // =========================================================
    // Scene Load
    // =========================================================

    private void LoadDestinationScene()
    {
        if (string.IsNullOrWhiteSpace(
                destinationSceneName))
        {
            Debug.LogError(
                "[ScenePortal] Destination Scene Name이 비어 있습니다."
            );


            return;
        }


        SceneManager.LoadScene(
            destinationSceneName
        );
    }
}