using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
    // =========================================================
    // Target
    // =========================================================

    [Header("Target")]
    [SerializeField]
    private Transform target;


    // =========================================================
    // Normal Camera
    // =========================================================

    [Header("Normal Camera")]

    [SerializeField]
    private float normalDistance = 5f;

    [SerializeField]
    private Vector3 normalOffset =
        new Vector3(
            0f,
            1.5f,
            0f
        );


    // =========================================================
    // Aim Camera
    // =========================================================

    [Header("Aim Camera")]

    [SerializeField]
    private float aimDistance = 3f;

    [SerializeField]
    private Vector3 aimOffset =
        new Vector3(
            0.8f,
            1.5f,
            0f
        );


    // =========================================================
    // Transition
    // =========================================================

    [Header("Camera Transition")]

    [SerializeField]
    private float transitionSpeed = 10f;


    // =========================================================
    // Rotation
    // =========================================================

    [Header("Rotation")]

    [SerializeField]
    private float sensitivity = 0.15f;

    [SerializeField]
    private float minPitch = -70f;

    [SerializeField]
    private float maxPitch = 70f;


    // =========================================================
    // Camera Collision
    // =========================================================

    [Header("Camera Collision")]

    [Tooltip("카메라가 충돌할 벽 / 지형 Layer")]
    [SerializeField]
    private LayerMask collisionMask = ~0;


    [Tooltip("카메라 충돌 검사 구체의 크기")]
    [SerializeField]
    private float collisionRadius = 0.25f;


    [Tooltip("벽에서 카메라를 조금 띄우는 거리")]
    [SerializeField]
    private float collisionPadding = 0.1f;


    [Tooltip("벽이 사라진 뒤 원래 거리로 돌아가는 속도")]
    [SerializeField]
    private float collisionReturnSpeed = 8f;


    [Tooltip("카메라가 Player 중심에 접근할 수 있는 최소 거리")]
    [SerializeField]
    private float minimumCameraDistance = 0.05f;


    // =========================================================
    // References
    // =========================================================

    [Header("References")]

    [SerializeField]
    private PlayerWeaponManager weaponManager;


    private PlayerModelFader modelFader;


    // =========================================================
    // Runtime
    // =========================================================

    private float yaw;

    private float pitch = 15f;


    private float currentDistance;

    private Vector3 currentOffset;


    private float currentCollisionDistance = -1f;


    private bool isAiming;


    public bool IsAiming
    {
        get { return isAiming; }
    }


    public float CurrentYaw
    {
        get { return yaw; }
    }


    // =========================================================
    // 외부에서 Player 등록
    // =========================================================

    public void SetTarget(
        Transform newTarget
    )
    {
        target =
            newTarget;


        if (target == null)
        {
            weaponManager = null;

            modelFader = null;

            return;
        }


        // ==========================================
        // 생성된 Player의 WeaponManager 자동 탐색
        // ==========================================

        weaponManager =
            target.GetComponent<PlayerWeaponManager>();


        if (weaponManager == null)
        {
            weaponManager =
                target.GetComponentInChildren<PlayerWeaponManager>();
        }


        // ==========================================
        // Player Model Fader 자동 탐색
        // ==========================================

        modelFader =
            target.GetComponent<PlayerModelFader>();


        if (modelFader == null)
        {
            modelFader =
                target.GetComponentInChildren<PlayerModelFader>(
                    true
                );
        }


        // 새 Player가 등록되었으므로
        // Camera collision 거리 재설정
        currentCollisionDistance =
            -1f;


        Debug.Log(
            $"[ThirdPersonCamera] Target 연결 : {target.name}"
        );
    }


    // =========================================================
    // Start
    // =========================================================

    private void Start()
    {
        Cursor.lockState =
            CursorLockMode.Locked;

        Cursor.visible =
            false;


        yaw =
            transform.eulerAngles.y;


        currentDistance =
            normalDistance;


        currentOffset =
            normalOffset;


        // Scene에 Player가 처음부터 있는 경우도 지원
        if (target != null)
        {
            SetTarget(
                target
            );
        }
    }


    // =========================================================
    // Update
    // =========================================================

    private void Update()
    {
        if (target == null)
            return;


        CheckAimInput();

        CameraRotation();

        CursorControl();
    }


    // =========================================================
    // Late Update
    // =========================================================

    private void LateUpdate()
    {
        if (target == null)
            return;


        UpdateCameraState();

        CameraPosition();
    }




    // =========================================================
    // Aim
    // =========================================================

    private void CheckAimInput()
    {
        // 근접 무기는 조준 카메라 사용 안 함
        if (weaponManager != null &&
            weaponManager.ActiveWeaponType ==
            WeaponType.Melee)
        {
            isAiming =
                false;

            return;
        }


        if (Mouse.current == null)
        {
            isAiming =
                false;

            return;
        }


        isAiming =
            Mouse.current.rightButton.isPressed;
    }


    // =========================================================
    // Camera Rotation
    // =========================================================

    private void CameraRotation()
    {
        if (Mouse.current == null)
            return;


        if (Cursor.lockState !=
            CursorLockMode.Locked)
        {
            return;
        }


        Vector2 mouseDelta =
            Mouse.current.delta.ReadValue();


        yaw +=
            mouseDelta.x *
            sensitivity;


        pitch -=
            mouseDelta.y *
            sensitivity;


        pitch =
            Mathf.Clamp(
                pitch,
                minPitch,
                maxPitch
            );
    }


    // =========================================================
    // Normal <-> Aim
    // =========================================================

    private void UpdateCameraState()
    {
        float targetDistance;

        Vector3 targetOffset;


        if (isAiming)
        {
            targetDistance =
                aimDistance;

            targetOffset =
                aimOffset;
        }
        else
        {
            targetDistance =
                normalDistance;

            targetOffset =
                normalOffset;
        }


        currentDistance =
            Mathf.Lerp(
                currentDistance,
                targetDistance,
                transitionSpeed *
                Time.deltaTime
            );


        currentOffset =
            Vector3.Lerp(
                currentOffset,
                targetOffset,
                transitionSpeed *
                Time.deltaTime
            );
    }


    // =========================================================
    // 실제 Camera 위치
    // =========================================================

    private void CameraPosition()
    {
        Quaternion rotation =
            Quaternion.Euler(
                pitch,
                yaw,
                0f
            );


        // ==========================================
        // Player를 바라보는 기준점
        // ==========================================

        Vector3 lookTarget =
            target.position +
            Vector3.up *
            currentOffset.y;


        // ==========================================
        // Shoulder Offset
        // ==========================================

        Vector3 shoulderOffset =
            rotation *
            Vector3.right *
            currentOffset.x;


        // ==========================================
        // Camera Back
        // ==========================================

        Vector3 cameraBack =
            rotation *
            Vector3.back *
            currentDistance;


        Vector3 desiredOffset =
            shoulderOffset +
            cameraBack;


        float desiredLength =
            desiredOffset.magnitude;


        if (desiredLength <= 0.001f)
            return;


        Vector3 cameraDirection =
            desiredOffset.normalized;


        // 기본적으로 원하는 Camera 거리
        float targetCameraDistance =
            desiredLength;


        bool obstacleFound =
            false;


        float nearestHitDistance =
            desiredLength;


        // =====================================================
        // 벽 충돌 검사
        // =====================================================

        RaycastHit[] hits =
            Physics.SphereCastAll(
                lookTarget,
                collisionRadius,
                cameraDirection,
                desiredLength +
                collisionPadding,
                collisionMask,
                QueryTriggerInteraction.Ignore
            );


        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null)
                continue;


            // ==========================================
            // Player 자신의 Collider는 무시
            // ==========================================

            if (hit.collider.transform.IsChildOf(
                    target))
            {
                continue;
            }


            if (hit.distance <
                nearestHitDistance)
            {
                nearestHitDistance =
                    hit.distance;

                obstacleFound =
                    true;
            }
        }


        // =====================================================
        // 벽이 Camera 뒤에 있으면
        // Camera를 Player 쪽으로 당김
        // =====================================================

        if (obstacleFound)
        {
            targetCameraDistance =
                Mathf.Max(
                    minimumCameraDistance,
                    nearestHitDistance -
                    collisionPadding
                );
        }


        // =====================================================
        // Collision Distance 처리
        //
        // 벽에 접근할 때는 즉시 앞으로 이동
        // 벽에서 멀어질 때는 부드럽게 뒤로 이동
        // =====================================================

        if (currentCollisionDistance < 0f)
        {
            currentCollisionDistance =
                targetCameraDistance;
        }


        if (targetCameraDistance <
            currentCollisionDistance)
        {
            // 벽에 들어가지 않도록 즉시 줄임
            currentCollisionDistance =
                targetCameraDistance;
        }
        else
        {
            // 다시 뒤로 갈 때는 부드럽게
            currentCollisionDistance =
                Mathf.MoveTowards(
                    currentCollisionDistance,
                    targetCameraDistance,
                    collisionReturnSpeed *
                    Time.deltaTime
                );
        }


        currentCollisionDistance =
            Mathf.Clamp(
                currentCollisionDistance,
                minimumCameraDistance,
                desiredLength
            );


        // =====================================================
        // 최종 Camera Position
        // =====================================================

        Vector3 finalPosition =
            lookTarget +
            cameraDirection *
            currentCollisionDistance;


        transform.position =
            finalPosition;


        transform.rotation =
            rotation;


        // =====================================================
        // Player Model Fade
        // =====================================================

        if (modelFader != null)
        {
            modelFader.SetCameraDistance(
                currentCollisionDistance
            );
        }
    }


    // =========================================================
    // Cursor
    // =========================================================

    private void CursorControl()
    {

        // ==========================================
        // UI 때문에 Gameplay 자체가 잠긴 상태
        // ==========================================

        if (PlayerGameplayControl.GameplayLocked)
        {
            Cursor.lockState =
                CursorLockMode.None;

            Cursor.visible =
                true;

            return;
        }


        // Scene 이동 UI가 열려있다면
        if (SceneTransitionPanelUI.IsTransitionUIOpen)
        {
            Cursor.lockState =
                CursorLockMode.None;

            Cursor.visible =
                true;

            return;
        }


        if (Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState =
                CursorLockMode.None;

            Cursor.visible =
                true;

            return;
        }


        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame &&
            Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState =
                CursorLockMode.Locked;

            Cursor.visible =
                false;
        }
    }


    // =========================================================
    // Collision Debug
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        if (target == null)
            return;


        Gizmos.DrawWireSphere(
            transform.position,
            collisionRadius
        );
    }



}