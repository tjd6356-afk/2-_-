using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Stats")]
    [SerializeField] private PlayerStats playerStats;

    [Header("Jump")]
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -20f;

    [Header("Glide (Warlock Strafe Glide)")]
    [SerializeField] private float glideGravity = -4f;        // 활공 중일 때의 가벼운 중력
    [SerializeField] private float maxGlideDuration = 3f;     // 활공 최대 유지 시간
    [SerializeField] private float maxGlideFallSpeed = -2.5f; // 활공 중 최대 낙하 속도 (천천히 떨어지게)

    private bool isGliding = false;
    private float currentGlideTime = 0f;

    [Header("Ground Check")]
    [SerializeField] private float groundCheckDistance = 0.08f;
    [SerializeField] private float groundCheckRadiusMultiplier = 0.9f;

    [Header("Camera")]
    [SerializeField] private Transform cameraTransform;

    [SerializeField] private ThirdPersonCamera thirdPersonCamera;

    [SerializeField] private float aimRotationSpeed = 20f;


    [Header("Dash")]
    [SerializeField] private float dashSpeed = 18f;
    [SerializeField] private float dashDuration = 0.18f;
    [SerializeField] private float dashCooldown = 0.5f;

    [Header("Wire")]
    [SerializeField] private WireController wireController;

    [Header("Wire Release Momentum")]
    [SerializeField] private float wireAirDeceleration = 1.5f;
    [SerializeField] private float wireGroundDeceleration = 12f;

    private Vector3 wireHorizontalVelocity;

    private bool isDashing;
    private float nextDashTime;

    private Vector3 dashDirection;

    private CharacterController controller;

    private float verticalVelocity;

    // 현재 Ground 태그 오브젝트에 닿아있는가?
    private bool isGrounded;


    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (playerStats == null)
        {
            playerStats =
                GetComponent<PlayerStats>();
        }

        if (cameraTransform == null &&
            Camera.main != null)
        {
            cameraTransform =
                Camera.main.transform;
        }

        if (thirdPersonCamera == null &&
            cameraTransform != null)
        {
            thirdPersonCamera =
                cameraTransform.GetComponent<ThirdPersonCamera>();
        }

        if (wireController == null)
        {
            wireController =
                GetComponent<WireController>();
        }
    }

    private void LateUpdate()
    {
        RotateWhileAiming();
    }

    public bool isSwingingByWeapon;

    private void Update()
    {
        // ==========================================
        // 와이어가 현재 Player 이동을 제어 중
        // ==========================================

        if (wireController != null &&
        wireController.IsControllingMovement)
        {
            return;
        }

        if (isSwingingByWeapon) return;

        CheckGround();

        CheckDashInput();


        if (isDashing)
        {
            DashMove();
            return;
        }


        Move();

        JumpAndGravity();

        ApplyWireMomentum();
    }


    // =========================================================
    // Ground 태그 검사
    // =========================================================

    private void CheckGround()
    {
        isGrounded = false;

        // 점프해서 위로 올라가는 중이라면
        // 발이 아직 바닥 근처더라도 Ground로 인정하지 않는다.
        if (verticalVelocity > 0.1f)
        {
            return;
        }


        Vector3 controllerCenter =
            transform.TransformPoint(controller.center);


        float checkRadius =
            controller.radius * groundCheckRadiusMultiplier;


        // CharacterController의 가장 아래쪽 계산
        Vector3 groundCheckPosition =
            controllerCenter +
            Vector3.down *
            (
                controller.height / 2f
                - checkRadius
                + groundCheckDistance
            );


        Collider[] colliders =
            Physics.OverlapSphere(
                groundCheckPosition,
                checkRadius,
                ~0,
                QueryTriggerInteraction.Ignore
            );


        foreach (Collider hit in colliders)
        {
            // 자기 자신의 Collider는 무시
            if (hit.transform == transform ||
                hit.transform.IsChildOf(transform))
            {
                continue;
            }


            // Ground 태그만 땅으로 인정
            if (hit.CompareTag("Ground"))
            {
                isGrounded = true;
                break;
            }
        }
    }


    // =========================================================
    // 이동
    // =========================================================

    private void Move()
    {
        if (cameraTransform == null)
            return;


        Vector2 input = GetMovementInput();


        input = Vector2.ClampMagnitude(input, 1f);


        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;


        cameraForward.y = 0f;
        cameraRight.y = 0f;


        cameraForward.Normalize();
        cameraRight.Normalize();


        Vector3 moveDirection =
            cameraForward * input.y +
            cameraRight * input.x;


        controller.Move(
        moveDirection *
        playerStats.MoveSpeed *
        Time.deltaTime
        );



        // 이동 방향 바라보기
        bool isAiming =
            thirdPersonCamera != null &&
            thirdPersonCamera.IsAiming;


        // 일반 상태일 때만 이동 방향으로 회전
        if (!isAiming &&
            moveDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(moveDirection);

            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
        }
    }


    // =========================================================
    // 점프 + 중력
    // =========================================================

    private void JumpAndGravity()
    {
        // Ground에 있을 때 아래로 계속 떨어지는 현상 방지 및 활공 쿨타임 초기화
        if (isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
            isGliding = false;
            currentGlideTime = maxGlideDuration; // 땅에 닿으면 활공 게이지 100% 회복
        }

        // 스페이스바 입력 처리
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (isGrounded)
            {
                // 1. 땅에 있을 때 : 기본 점프
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                isGrounded = false;
            }
            else
            {
                // 2. 공중에 있을 때 : 활공 ON/OFF 토글 (제어 활공)
                // 남은 시간이 있을 때만 켤 수 있음
                if (currentGlideTime > 0f)
                {
                    isGliding = !isGliding;
                }
            }
        }

        // 활공 지속 시간 깎기
        if (isGliding)
        {
            currentGlideTime -= Time.deltaTime;

            // 시간이 다 되면 강제로 활공 종료
            if (currentGlideTime <= 0f)
            {
                isGliding = false;
            }
        }

        // 중력 계산 (활공 켜짐 vs 꺼짐)
        if (isGliding)
        {
            // 활공 중: 매우 약한 중력 적용 (체공 시간 증가)
            verticalVelocity += glideGravity * Time.deltaTime;

            // 떨어지는 도중 활공을 켰을 때(위로 못 올라감) 천천히 낙하하도록 속도 제한
            if (verticalVelocity < maxGlideFallSpeed)
            {
                verticalVelocity = maxGlideFallSpeed;
            }
        }
        else
        {
            // 평상시: 기본 묵직한 중력 적용
            verticalVelocity += gravity * Time.deltaTime;
        }

        // 최종 수직 이동 적용
        controller.Move(Vector3.up * verticalVelocity * Time.deltaTime);
    }


    // =========================================================
    // Scene에서 Ground 검사 영역 확인
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        CharacterController characterController =
            GetComponent<CharacterController>();


        if (characterController == null)
            return;


        Vector3 controllerCenter =
            transform.TransformPoint(
                characterController.center
            );


        float checkRadius =
            characterController.radius *
            groundCheckRadiusMultiplier;


        Vector3 groundCheckPosition =
            controllerCenter +
            Vector3.down *
            (
                characterController.height / 2f
                - checkRadius
                + groundCheckDistance
            );


        Gizmos.DrawWireSphere(
            groundCheckPosition,
            checkRadius
        );
    }

    private void RotateWhileAiming()
    {
        if (thirdPersonCamera == null)
            return;


        if (!thirdPersonCamera.IsAiming)
            return;


        // 카메라의 좌우 회전값만 가져온다.
        // 위/아래 Pitch는 Player에 적용하지 않는다.
        Quaternion targetRotation =
            Quaternion.Euler(
                0f,
                thirdPersonCamera.CurrentYaw,
                0f
            );


        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                aimRotationSpeed * Time.deltaTime
            );
    }

    private void CheckDashInput()
    {
        if (Keyboard.current == null)
            return;


        if (isDashing)
            return;


        if (Time.time < nextDashTime)
            return;


        // ==========================================
        // 현재 이동 입력 확인
        // ==========================================

        Vector2 input = GetMovementInput();


        // 방향키를 아무것도 안 누르고 있다면 대쉬 불가능
        if (input.sqrMagnitude <= 0.01f)
            return;


        bool shiftPressed =
            Keyboard.current.leftShiftKey.wasPressedThisFrame ||
            Keyboard.current.rightShiftKey.wasPressedThisFrame;


        /*
         * Shift를 먼저 누르고
         * W/A/S/D를 누르는 경우도 처리한다.
         */
        bool directionPressed =
            Keyboard.current.wKey.wasPressedThisFrame ||
            Keyboard.current.aKey.wasPressedThisFrame ||
            Keyboard.current.sKey.wasPressedThisFrame ||
            Keyboard.current.dKey.wasPressedThisFrame;


        bool shiftHeld =
            Keyboard.current.leftShiftKey.isPressed ||
            Keyboard.current.rightShiftKey.isPressed;


        /*
         * 경우 1
         * W를 누르고 있는 상태에서 Shift
         *
         * 경우 2
         * Shift를 누르고 있는 상태에서 W
         */
        if (!shiftPressed &&
            !(shiftHeld && directionPressed))
        {
            return;
        }


        StartDash(input);
    }

    private Vector2 GetMovementInput()
    {
        Vector2 input = Vector2.zero;


        if (Keyboard.current == null)
            return input;


        if (Keyboard.current.wKey.isPressed)
            input.y += 1f;

        if (Keyboard.current.sKey.isPressed)
            input.y -= 1f;

        if (Keyboard.current.dKey.isPressed)
            input.x += 1f;

        if (Keyboard.current.aKey.isPressed)
            input.x -= 1f;


        return Vector2.ClampMagnitude(
            input,
            1f
        );
    }

    private void StartDash(Vector2 input)
    {
        wireHorizontalVelocity =
        Vector3.zero;

        if (cameraTransform == null)
            return;


        // 카메라 기준 앞/오른쪽
        Vector3 cameraForward =
            cameraTransform.forward;

        Vector3 cameraRight =
            cameraTransform.right;


        // 대쉬는 수평 방향으로만
        cameraForward.y = 0f;
        cameraRight.y = 0f;

        cameraForward.Normalize();
        cameraRight.Normalize();


        // ==========================================
        // 입력 방향에 따른 대쉬 방향 계산
        // ==========================================

        dashDirection =
            cameraForward * input.y +
            cameraRight * input.x;


        dashDirection.Normalize();


        isDashing = true;


        nextDashTime =
            Time.time +
            dashCooldown;


        // 일정 시간이 지나면 대쉬 종료
        Invoke(
            nameof(EndDash),
            dashDuration
        );
    }
    private void DashMove()
    {
        controller.Move(
            dashDirection *
            dashSpeed *
            Time.deltaTime
        );
    }
    private void EndDash()
    {
        isDashing = false;
    }

    public void ReceiveWireReleaseVelocity(
    Vector3 releaseVelocity)
    {
        // 수평 속도
        wireHorizontalVelocity =
            new Vector3(
                releaseVelocity.x,
                0f,
                releaseVelocity.z
            );


        // 수직 속도는 기존 점프/중력 시스템에 넘긴다.
        verticalVelocity =
            releaseVelocity.y;
    }


    public void ClearWireMomentum()
    {
        wireHorizontalVelocity =
            Vector3.zero;
    }

    private void ApplyWireMomentum()
    {
        if (wireHorizontalVelocity.sqrMagnitude < 0.001f)
        {
            wireHorizontalVelocity = Vector3.zero;
            return;
        }

        // 1. 관성으로 캐릭터 이동
        controller.Move(wireHorizontalVelocity * Time.deltaTime);

        // 2. 바닥인지 공중인지에 따라 다른 브레이크(마찰력) 방식 적용
        if (isGrounded)
        {
            // 🌟 핵심 해결책: 땅에 닿았을 때는 Lerp(퍼센트 감속)를 사용!
            // 초고속으로 날아와도 속도가 확 깎여서 얼음판처럼 미끄러지지 않습니다.
            wireHorizontalVelocity = Vector3.Lerp(
                wireHorizontalVelocity,
                Vector3.zero,
                wireGroundDeceleration * Time.deltaTime
            );
        }
        else
        {
            // 공중에서는 기존 방식(MoveTowards) 유지 (천천히 자연스럽게 감속)
            wireHorizontalVelocity = Vector3.MoveTowards(
                wireHorizontalVelocity,
                Vector3.zero,
                wireAirDeceleration * Time.deltaTime
            );
        }
    }
}