using UnityEngine;
using UnityEngine.InputSystem;


// PlayerMovement보다 먼저 Update되도록 설정
[DefaultExecutionOrder(-50)]
public class WireController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private CharacterController controller;

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


    [Header("Single Wire Swing")]
    [Tooltip("A/D로 회전하는 기본 속도")]
    [SerializeField] private float swingAngularSpeed = 90f;

    [Tooltip("Shift 사용 시 회전 속도 배율")]
    [SerializeField] private float swingBoostMultiplier = 2f;


    [Header("Double Wire Pull")]
    [Tooltip("Q + E 사용 시 목표 지점으로 이동하는 속도")]
    [SerializeField] private float pullSpeed = 15f;

    [Tooltip("Shift 사용 시 직선 이동 속도 배율")]
    [SerializeField] private float pullBoostMultiplier = 2f;

    [SerializeField] private float pullStopDistance = 0.5f;


    // =========================================================
    // 각각의 와이어 상태
    // =========================================================

    private class WireState
    {
        public bool attached;

        public Transform targetTransform;

        public Vector3 localAnchorPoint;

        public Vector3 fallbackWorldPoint;


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
    // 외부 확인용
    // =========================================================

    public bool IsQWireAttached => qWire.attached;

    public bool IsEWireAttached => eWire.attached;

    public bool IsDoubleWire =>
        qWire.attached &&
        eWire.attached;


    /*
     * PlayerMovement가 이 값을 보고
     * 일반 이동 / 대쉬를 중지한다.
     */
    public bool IsControllingMovement =>
        qWire.attached ||
        eWire.attached;


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


        PrepareLine(qWireLine);
        PrepareLine(eWireLine);
    }


    private void Update()
    {
        if (Keyboard.current == null)
            return;


        HandleWireInput();


        // 두 와이어
        if (qWire.attached &&
            eWire.attached)
        {
            DoubleWireMove();
        }

        // Q 와이어만
        else if (qWire.attached)
        {
            SingleWireMove(qWire);
        }

        // E 와이어만
        else if (eWire.attached)
        {
            SingleWireMove(eWire);
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


        // Q를 누르고 있는데 아직 연결되지 않았다면 연결 시도
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
    // 십자선 방향으로 와이어 발사
    // =========================================================

    private void TryAttachWire(
        WireState wire,
        LineRenderer line
    )
    {
        if (playerCamera == null)
            return;


        // 화면 정확히 중앙
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


        // 너무 가까운 위치에는 와이어 사용 금지
        if (distance < minWireDistance)
            return;


        wire.attached = true;

        wire.targetTransform =
            hit.collider.transform;

        wire.localAnchorPoint =
            hit.collider.transform.InverseTransformPoint(
                hit.point
            );

        wire.fallbackWorldPoint =
            hit.point;


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
    // 한쪽 와이어 이동
    //
    // A = 왼쪽 회전
    // D = 오른쪽 회전
    // =========================================================

    private void SingleWireMove(
        WireState wire
    )
    {
        if (controller == null)
            return;


        float horizontalInput = 0f;


        if (Keyboard.current.aKey.isPressed)
        {
            horizontalInput -= 1f;
        }


        if (Keyboard.current.dKey.isPressed)
        {
            horizontalInput += 1f;
        }


        // A/D 입력이 없다면 회전하지 않음
        if (Mathf.Abs(horizontalInput) < 0.01f)
            return;


        bool shiftHeld =
            Keyboard.current.leftShiftKey.isPressed ||
            Keyboard.current.rightShiftKey.isPressed;


        float speedMultiplier =
            shiftHeld
                ? swingBoostMultiplier
                : 1f;


        Vector3 anchorPoint =
            wire.GetAnchorPoint();


        /*
         * 고정 지점 → Player 벡터
         *
         * 이 벡터를 회전시켜서
         * 와이어 길이를 그대로 유지한 상태로
         * 원을 그리며 이동한다.
         */
        Vector3 fromAnchor =
            transform.position -
            anchorPoint;


        float angle =
            horizontalInput *
            swingAngularSpeed *
            speedMultiplier *
            Time.deltaTime;


        /*
         * A/D에 따라
         * 월드 Y축을 중심으로 회전
         */
        Quaternion rotation =
            Quaternion.AngleAxis(
                angle,
                Vector3.up
            );


        Vector3 rotatedOffset =
            rotation *
            fromAnchor;


        Vector3 targetPosition =
            anchorPoint +
            rotatedOffset;


        Vector3 movement =
            targetPosition -
            transform.position;


        controller.Move(movement);
    }


    // =========================================================
    // Q + E
    //
    // 두 와이어 고정점 방향으로 직선 이동
    // =========================================================

    private void DoubleWireMove()
    {
        if (controller == null)
            return;


        Vector3 qAnchor =
            qWire.GetAnchorPoint();

        Vector3 eAnchor =
            eWire.GetAnchorPoint();


        /*
         * 두 와이어가 다른 지점에 연결되어 있다면
         * 두 지점의 중앙으로 이동
         *
         * 같은 지점이라면 그냥 그 지점으로 이동
         */
        Vector3 targetPoint =
            (qAnchor + eAnchor) * 0.5f;


        Vector3 toTarget =
            targetPoint -
            transform.position;


        float distance =
            toTarget.magnitude;


        if (distance <= pullStopDistance)
            return;


        bool shiftHeld =
            Keyboard.current.leftShiftKey.isPressed ||
            Keyboard.current.rightShiftKey.isPressed;


        float speedMultiplier =
            shiftHeld
                ? pullBoostMultiplier
                : 1f;


        float speed =
            pullSpeed *
            speedMultiplier;


        Vector3 direction =
            toTarget.normalized;


        /*
         * 목표 지점을 지나치지 않도록
         * 이번 프레임 이동거리를 제한
         */
        float moveDistance =
            speed *
            Time.deltaTime;


        moveDistance =
            Mathf.Min(
                moveDistance,
                distance - pullStopDistance
            );


        if (moveDistance <= 0f)
            return;


        controller.Move(
            direction *
            moveDistance
        );
    }


    // =========================================================
    // Line Renderer 초기화
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


    // =========================================================
    // 실제 와이어 선 표시
    // =========================================================

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