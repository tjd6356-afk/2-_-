using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;


    [Header("Normal Camera")]
    [SerializeField] private float normalDistance = 5f;

    [SerializeField]
    private Vector3 normalOffset =
        new Vector3(0f, 1.5f, 0f);


    [Header("Aim Camera")]
    [SerializeField] private float aimDistance = 3f;

    // X가 +이면 카메라가 플레이어 오른쪽으로 이동
    // 그래서 플레이어는 화면 왼쪽에 보이게 된다.
    [SerializeField]
    private Vector3 aimOffset =
        new Vector3(0.8f, 1.5f, 0f);


    [Header("Camera Transition")]
    [SerializeField] private float transitionSpeed = 10f;


    [Header("Rotation")]
    [SerializeField] private float sensitivity = 0.15f;

    [SerializeField] private float minPitch = -30f;
    [SerializeField] private float maxPitch = 70f;


    private float yaw;
    private float pitch = 15f;


    private float currentDistance;
    private Vector3 currentOffset;

    private bool isAiming;


    public bool IsAiming
    {
        get { return isAiming; }
    }

    public float CurrentYaw
    {
        get { return yaw; }
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;


        yaw = transform.eulerAngles.y;


        // 처음에는 일반 카메라 상태
        currentDistance = normalDistance;
        currentOffset = normalOffset;
    }


    private void Update()
    {
        if (target == null)
            return;

        CheckAimInput();

        CameraRotation();

        CursorControl();
    }


    private void LateUpdate()
    {
        if (target == null)
            return;

        UpdateCameraState();

        CameraPosition();
    }


    // =========================================================
    // 우클릭 조준
    // =========================================================

    private void CheckAimInput()
    {
        if (Mouse.current == null)
        {
            isAiming = false;
            return;
        }


        // 우클릭을 누르고 있는 동안 조준
        isAiming =
            Mouse.current.rightButton.isPressed;
    }


    // =========================================================
    // 카메라 회전
    // =========================================================

    private void CameraRotation()
    {
        if (Mouse.current == null)
            return;


        if (Cursor.lockState != CursorLockMode.Locked)
            return;


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
    // 일반 ↔ 조준 카메라 전환
    // =========================================================

    private void UpdateCameraState()
    {
        float targetDistance;
        Vector3 targetCameraOffset;


        if (isAiming)
        {
            targetDistance = aimDistance;
            targetCameraOffset = aimOffset;
        }
        else
        {
            targetDistance = normalDistance;
            targetCameraOffset = normalOffset;
        }


        // 거리 부드럽게 변경
        currentDistance =
            Mathf.Lerp(
                currentDistance,
                targetDistance,
                transitionSpeed *
                Time.deltaTime
            );


        // 위치 부드럽게 변경
        currentOffset =
            Vector3.Lerp(
                currentOffset,
                targetCameraOffset,
                transitionSpeed *
                Time.deltaTime
            );
    }


    // =========================================================
    // 실제 카메라 위치
    // =========================================================

    private void CameraPosition()
    {
        Quaternion rotation =
            Quaternion.Euler(
                pitch,
                yaw,
                0f
            );


        /*
         * Y Offset
         *
         * 플레이어 기준으로
         * 카메라가 바라보는 높이
         */
        Vector3 lookTarget =
            target.position +
            Vector3.up *
            currentOffset.y;


        /*
         * 카메라의 오른쪽 방향
         *
         * aimOffset.x가 양수라면
         * 카메라가 오른쪽으로 이동한다.
         */
        Vector3 shoulderOffset =
            rotation *
            Vector3.right *
            currentOffset.x;


        /*
         * 카메라 뒤쪽 거리
         */
        Vector3 cameraBack =
            rotation *
            Vector3.back *
            currentDistance;


        /*
         * 최종 카메라 위치
         */
        Vector3 cameraPosition =
            lookTarget +
            shoulderOffset +
            cameraBack;


        transform.position =
            cameraPosition;


        /*
         * 카메라는 현재 마우스 회전 방향을 바라본다.
         */
        transform.rotation =
            rotation;
    }


    // =========================================================
    // Cursor
    // =========================================================

    private void CursorControl()
    {
        if (Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState =
                CursorLockMode.None;

            Cursor.visible = true;
        }


        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame &&
            Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState =
                CursorLockMode.Locked;

            Cursor.visible = false;
        }
    }
}