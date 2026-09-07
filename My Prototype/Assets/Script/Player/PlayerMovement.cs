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

    [Header("Ground Check")]
    [SerializeField] private float groundCheckDistance = 0.08f;
    [SerializeField] private float groundCheckRadiusMultiplier = 0.9f;

    [Header("Camera")]
    [SerializeField] private Transform cameraTransform;

    [SerializeField] private ThirdPersonCamera thirdPersonCamera;

    [SerializeField] private float aimRotationSpeed = 20f;

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
    }

    private void LateUpdate()
    {
        RotateWhileAiming();
    }


    private void Update()
    {
        CheckGround();

        Move();
        JumpAndGravity();
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


        Vector2 input = Vector2.zero;


        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed)
                input.y += 1f;

            if (Keyboard.current.sKey.isPressed)
                input.y -= 1f;

            if (Keyboard.current.dKey.isPressed)
                input.x += 1f;

            if (Keyboard.current.aKey.isPressed)
                input.x -= 1f;
        }


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
        Time.deltaTime);
                    


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
        // Ground에 있을 때 아래로 계속 떨어지는 현상 방지
        if (isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }


        // Ground 태그 위에 있을 때만 점프 가능
        if (Keyboard.current != null &&
            Keyboard.current.spaceKey.wasPressedThisFrame &&
            isGrounded)
        {
            verticalVelocity =
                Mathf.Sqrt(
                    jumpHeight *
                    -2f *
                    gravity
                );


            // 점프하는 순간 바로 공중 상태로 전환
            isGrounded = false;
        }


        // 중력
        verticalVelocity += gravity * Time.deltaTime;


        controller.Move(
            Vector3.up *
            verticalVelocity *
            Time.deltaTime
        );
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
}