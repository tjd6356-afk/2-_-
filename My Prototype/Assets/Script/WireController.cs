using UnityEngine;
using UnityEngine.InputSystem;


// PlayerMovement보다 먼저 실행
[DefaultExecutionOrder(-50)]
public class WireController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private CharacterController controller;
    [SerializeField] private PlayerMovement playerMovement;


    [Header("Wire Origins")]
    [SerializeField] private Transform qWireOrigin;
    [SerializeField] private Transform eWireOrigin;


    [Header("Wire Lines")]
    [SerializeField] private LineRenderer qWireLine;
    [SerializeField] private LineRenderer eWireLine;


    [Header("Wire Target")]
    [SerializeField] private LayerMask wireableMask;
    [SerializeField] private float maxWireDistance = 50f;
    [SerializeField] private float minWireDistance = 1f;


    // =========================================================
    // 진자 스윙
    // =========================================================

    [Header("Pendulum Swing")]

    [Tooltip("중력의 세기. 1 = 기본 중력")]
    [SerializeField] private float gravityScale = 1f;

    [Tooltip("A/D가 스윙에 추가하는 가속력")]
    [SerializeField] private float swingAcceleration = 18f;

    [Tooltip("Shift 사용 시 A/D 가속력 배율")]
    [SerializeField] private float swingBoostMultiplier = 2f;

    [Tooltip("일반 상태의 최대 스윙 속도")]
    [SerializeField] private float maxSwingSpeed = 25f;

    [Tooltip("공기 저항. 작을수록 오래 속도를 유지")]
    [SerializeField] private float swingDrag = 0.15f;


    // =========================================================
    // Q + E 당기기
    // =========================================================

    [Header("Double Wire Pull")]

    [Tooltip("두 와이어 사용 시 가속력")]
    [SerializeField] private float pullAcceleration = 35f;

    [Tooltip("두 와이어 사용 시 최대 속도")]
    [SerializeField] private float maxPullSpeed = 22f;

    [Tooltip("Shift 사용 시 가속/최대속도 배율")]
    [SerializeField] private float pullBoostMultiplier = 1.8f;

    [SerializeField] private float pullStopDistance = 0.8f;


    // =========================================================
    // Runtime
    // =========================================================

    private Vector3 wireVelocity;

    // 와이어를 걸기 직전 플레이어 속도 추정
    private Vector3 freeMovementVelocity;

    private Vector3 previousPosition;


    // =========================================================
    // 와이어 하나의 상태
    // =========================================================

    private class WireState
    {
        public bool attached;

        public Transform targetTransform;

        public Vector3 localAnchorPoint;

        public Vector3 fallbackWorldPoint;

        // 와이어를 처음 걸었을 때 길이
        public float ropeLength;


        public Vector3 GetAnchorPoint()
        {
            if (targetTransform != null)
            {
                return targetTransform.TransformPoint(
                    localAnchorPoint
                );
            }

            return fallbackWorldPoint;
        }
    }


    private WireState qWire = new WireState();
    private WireState eWire = new WireState();


    // =========================================================
    // 외부 접근
    // =========================================================

    public bool IsQWireAttached =>
        qWire.attached;

    public bool IsEWireAttached =>
        eWire.attached;

    public bool IsDoubleWire =>
        qWire.attached &&
        eWire.attached;

    public bool IsControllingMovement =>
        qWire.attached ||
        eWire.attached;

    public Vector3 CurrentWireVelocity =>
        wireVelocity;


    private void Awake()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }


        if (controller == null)
        {
            controller =
                GetComponent<CharacterController>();
        }


        if (playerMovement == null)
        {
            playerMovement =
                GetComponent<PlayerMovement>();
        }


        PrepareLine(qWireLine);
        PrepareLine(eWireLine);


        previousPosition =
            transform.position;
    }


    private void Update()
    {
        if (Keyboard.current == null)
            return;


        bool hadWire =
            IsControllingMovement;


        HandleWireInput();


        bool hasWire =
            IsControllingMovement;


        // =====================================================
        // 와이어를 처음 건 순간
        // =====================================================

        if (!hadWire &&
            hasWire)
        {
            BeginWireMovement();
        }


        // =====================================================
        // 모든 와이어를 놓은 순간
        // =====================================================

        if (hadWire &&
            !hasWire)
        {
            EndWireMovement();

            return;
        }


        if (!hasWire)
            return;


        // =====================================================
        // Q + E
        // =====================================================

        if (IsDoubleWire)
        {
            DoubleWirePull();
        }

        // =====================================================
        // 한쪽 와이어
        // =====================================================

        else if (qWire.attached)
        {
            SingleWireSwing(qWire);
        }

        else if (eWire.attached)
        {
            SingleWireSwing(eWire);
        }
    }


    private void LateUpdate()
    {
        UpdateWireLine(
            qWire,
            qWireOrigin,
            qWireLine
        );


        UpdateWireLine(
            eWire,
            eWireOrigin,
            eWireLine
        );


        // =====================================================
        // 와이어를 사용하지 않을 때
        // 플레이어의 실제 이동 속도 추정
        // =====================================================

        if (!IsControllingMovement &&
            Time.deltaTime > 0f)
        {
            freeMovementVelocity =
                (
                    transform.position -
                    previousPosition
                )
                / Time.deltaTime;
        }


        previousPosition =
            transform.position;
    }


    // =========================================================
    // 입력
    // =========================================================

    private void HandleWireInput()
    {
        bool qHeld =
            Keyboard.current.qKey.isPressed;

        bool eHeld =
            Keyboard.current.eKey.isPressed;


        // Q
        if (qHeld)
        {
            if (!qWire.attached)
            {
                TryAttachWire(
                    qWire,
                    qWireLine
                );
            }
        }
        else
        {
            DetachWire(
                qWire,
                qWireLine
            );
        }


        // E
        if (eHeld)
        {
            if (!eWire.attached)
            {
                TryAttachWire(
                    eWire,
                    eWireLine
                );
            }
        }
        else
        {
            DetachWire(
                eWire,
                eWireLine
            );
        }
    }


    // =========================================================
    // 십자선 방향 와이어 발사
    // =========================================================

    private void TryAttachWire(
        WireState wire,
        LineRenderer line
    )
    {
        if (playerCamera == null)
            return;


        Ray ray =
            playerCamera.ViewportPointToRay(
                new Vector3(
                    0.5f,
                    0.5f,
                    0f
                )
            );


        if (!Physics.Raycast(
                ray,
                out RaycastHit hit,
                maxWireDistance,
                wireableMask,
                QueryTriggerInteraction.Ignore))
        {
            return;
        }


        float distance =
            Vector3.Distance(
                transform.position,
                hit.point
            );


        if (distance <
            minWireDistance)
        {
            return;
        }


        wire.attached = true;


        wire.targetTransform =
            hit.collider.transform;


        wire.localAnchorPoint =
            hit.collider.transform
                .InverseTransformPoint(
                    hit.point
                );


        wire.fallbackWorldPoint =
            hit.point;


        // 중요:
        // 이 길이보다 멀리 갈 수 없다.
        wire.ropeLength =
            distance;


        if (line != null)
        {
            line.enabled = true;
        }
    }


    // =========================================================
    // 와이어 해제
    // =========================================================

    private void DetachWire(
        WireState wire,
        LineRenderer line
    )
    {
        if (!wire.attached)
            return;


        wire.attached = false;

        wire.targetTransform = null;


        if (line != null)
        {
            line.enabled = false;
        }
    }


    // =========================================================
    // 와이어를 처음 건 순간
    // =========================================================

    private void BeginWireMovement()
    {
        /*
         * 기존에 달리거나 점프하던 속도를
         * 그대로 와이어 속도로 가져온다.
         *
         * 이것 때문에 달리다가 와이어를 걸면
         * 기존 관성이 사라지지 않는다.
         */

        wireVelocity =
            freeMovementVelocity;


        if (playerMovement != null)
        {
            playerMovement.ClearWireMomentum();
        }
    }


    // =========================================================
    // 모든 와이어를 놓은 순간
    // =========================================================

    private void EndWireMovement()
    {
        /*
         * 스윙 중 얻은 속도를
         * PlayerMovement에게 돌려준다.
         *
         * 그래서 와이어를 놓아도
         * 갑자기 멈추지 않고 날아간다.
         */

        if (playerMovement != null)
        {
            playerMovement.ReceiveWireReleaseVelocity(
                wireVelocity
            );
        }


        wireVelocity =
            Vector3.zero;
    }


    // =========================================================
    // 한쪽 와이어
    //
    // 실제 진자형 스윙
    // =========================================================

    private void SingleWireSwing(
        WireState wire
    )
    {
        if (controller == null)
            return;


        float deltaTime =
            Time.deltaTime;


        Vector3 anchorPoint =
            wire.GetAnchorPoint();


        Vector3 playerPosition =
            transform.position;


        Vector3 anchorToPlayer =
            playerPosition -
            anchorPoint;


        float distance =
            anchorToPlayer.magnitude;


        if (distance <= 0.001f)
            return;


        Vector3 ropeDirection =
            anchorToPlayer /
            distance;


        // =====================================================
        // 1. 중력
        // =====================================================

        wireVelocity +=
            Physics.gravity *
            gravityScale *
            deltaTime;


        // =====================================================
        // 2. A / D 입력
        // =====================================================

        float horizontalInput = 0f;


        if (Keyboard.current.aKey.isPressed)
        {
            horizontalInput -= 1f;
        }


        if (Keyboard.current.dKey.isPressed)
        {
            horizontalInput += 1f;
        }


        if (Mathf.Abs(horizontalInput) >
            0.01f)
        {
            bool shiftHeld =
                Keyboard.current
                    .leftShiftKey
                    .isPressed
                ||
                Keyboard.current
                    .rightShiftKey
                    .isPressed;


            float boost =
                shiftHeld
                    ? swingBoostMultiplier
                    : 1f;


            /*
             * 카메라의 오른쪽 방향을
             * 로프 방향에 수직인 평면에 투영한다.
             *
             * 그래서 A/D가 실제 진자의
             * 접선 방향 힘이 된다.
             */

            Vector3 desiredDirection =
                playerCamera.transform.right *
                horizontalInput;


            Vector3 tangentDirection =
                Vector3.ProjectOnPlane(
                    desiredDirection,
                    ropeDirection
                );


            if (tangentDirection.sqrMagnitude >
                0.001f)
            {
                tangentDirection.Normalize();


                wireVelocity +=
                    tangentDirection *
                    swingAcceleration *
                    boost *
                    deltaTime;
            }
        }


        // =====================================================
        // 3. 공기 저항
        // =====================================================

        float dragAmount =
            1f /
            (
                1f +
                swingDrag *
                deltaTime
            );


        wireVelocity *=
            dragAmount;


        // =====================================================
        // 4. 최고 속도 제한
        // =====================================================

        bool boosting =
            Keyboard.current
                .leftShiftKey
                .isPressed
            ||
            Keyboard.current
                .rightShiftKey
                .isPressed;


        float maxSpeed =
            maxSwingSpeed;


        if (boosting)
        {
            maxSpeed *=
                swingBoostMultiplier;
        }


        if (wireVelocity.magnitude >
            maxSpeed)
        {
            wireVelocity =
                wireVelocity.normalized *
                maxSpeed;
        }


        // =====================================================
        // 5. 속도로 다음 위치 예측
        // =====================================================

        Vector3 predictedPosition =
            playerPosition +
            wireVelocity *
            deltaTime;


        Vector3 predictedFromAnchor =
            predictedPosition -
            anchorPoint;


        float predictedDistance =
            predictedFromAnchor.magnitude;


        // =====================================================
        // 6. 로프 길이 제한
        //
        // Player가 로프보다 멀리 나가려 하면
        // 구 표면으로 되돌린다.
        // =====================================================

        if (predictedDistance >
            wire.ropeLength)
        {
            Vector3 constrainedDirection =
                predictedFromAnchor.normalized;


            predictedPosition =
                anchorPoint +
                constrainedDirection *
                wire.ropeLength;


            /*
             * 바깥쪽으로 빠져나가려는 속도 제거.
             *
             * 접선 속도는 남기기 때문에
             * 진자 운동이 만들어진다.
             */

            float outwardVelocity =
                Vector3.Dot(
                    wireVelocity,
                    constrainedDirection
                );


            if (outwardVelocity > 0f)
            {
                wireVelocity -=
                    constrainedDirection *
                    outwardVelocity;
            }
        }


        // =====================================================
        // 7. CharacterController 이동
        // =====================================================

        Vector3 movement =
            predictedPosition -
            playerPosition;


        CollisionFlags flags =
            controller.Move(
                movement
            );


        // 땅에 충돌했다면
        // 아래쪽 속도 제거
        if ((flags & CollisionFlags.Below) != 0 &&
            wireVelocity.y < 0f)
        {
            wireVelocity.y = 0f;
        }


        // =====================================================
        // 8. 충돌 때문에 로프 밖으로 밀렸다면 재보정
        // =====================================================

        Vector3 actualFromAnchor =
            transform.position -
            anchorPoint;


        float actualDistance =
            actualFromAnchor.magnitude;


        if (actualDistance >
            wire.ropeLength + 0.01f)
        {
            Vector3 correctedPosition =
                anchorPoint +
                actualFromAnchor.normalized *
                wire.ropeLength;


            controller.Move(
                correctedPosition -
                transform.position
            );
        }
    }


    // =========================================================
    // Q + E
    //
    // 두 와이어 고정점 방향으로 직접 가속
    // =========================================================

    private void DoubleWirePull()
    {
        if (controller == null)
            return;


        Vector3 qAnchor =
            qWire.GetAnchorPoint();


        Vector3 eAnchor =
            eWire.GetAnchorPoint();


        Vector3 targetPoint =
            (
                qAnchor +
                eAnchor
            )
            * 0.5f;


        Vector3 toTarget =
            targetPoint -
            transform.position;


        float distance =
            toTarget.magnitude;


        if (distance <=
            pullStopDistance)
        {
            wireVelocity =
                Vector3.zero;

            return;
        }


        Vector3 direction =
            toTarget.normalized;


        bool shiftHeld =
            Keyboard.current
                .leftShiftKey
                .isPressed
            ||
            Keyboard.current
                .rightShiftKey
                .isPressed;


        float boost =
            shiftHeld
                ? pullBoostMultiplier
                : 1f;


        // 현재 목표 방향 속도
        float currentForwardSpeed =
            Vector3.Dot(
                wireVelocity,
                direction
            );


        currentForwardSpeed =
            Mathf.Max(
                0f,
                currentForwardSpeed
            );


        // 순간 최고속도가 아니라
        // 실제로 가속
        currentForwardSpeed +=
            pullAcceleration *
            boost *
            Time.deltaTime;


        float maximumSpeed =
            maxPullSpeed *
            boost;


        currentForwardSpeed =
            Mathf.Min(
                currentForwardSpeed,
                maximumSpeed
            );


        /*
         * 두 와이어 모드는
         * 사용자가 요구한 "직선 이동"을
         * 유지하기 위해 목표 방향 속도만 사용한다.
         */

        wireVelocity =
            direction *
            currentForwardSpeed;


        float moveDistance =
            currentForwardSpeed *
            Time.deltaTime;


        moveDistance =
            Mathf.Min(
                moveDistance,
                distance -
                pullStopDistance
            );


        if (moveDistance <= 0f)
            return;


        controller.Move(
            direction *
            moveDistance
        );
    }


    // =========================================================
    // CharacterController 벽 충돌
    // =========================================================

    private void OnControllerColliderHit(
        ControllerColliderHit hit
    )
    {
        if (!IsControllingMovement)
            return;


        /*
         * 벽 안쪽을 향하는 속도 제거.
         *
         * 벽에 충돌했을 때 계속
         * 벽을 뚫으려고 가속되는 현상 방지.
         */

        float intoSurface =
            Vector3.Dot(
                wireVelocity,
                hit.normal
            );


        if (intoSurface < 0f)
        {
            wireVelocity -=
                hit.normal *
                intoSurface;
        }
    }


    // =========================================================
    // Line Renderer
    // =========================================================

    private void PrepareLine(
        LineRenderer line
    )
    {
        if (line == null)
            return;


        line.positionCount = 2;

        line.useWorldSpace = true;

        line.enabled = false;
    }


    private void UpdateWireLine(
        WireState wire,
        Transform origin,
        LineRenderer line
    )
    {
        if (line == null)
            return;


        if (!wire.attached)
        {
            line.enabled = false;

            return;
        }


        if (origin == null)
            return;


        line.enabled = true;


        line.SetPosition(
            0,
            origin.position
        );


        line.SetPosition(
            1,
            wire.GetAnchorPoint()
        );
    }
}