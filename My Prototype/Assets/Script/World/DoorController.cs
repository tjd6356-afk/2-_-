using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class DoorController : MonoBehaviour
{
    // =========================================================
    // References
    // =========================================================

    [Header("References")]

    [Tooltip("실제로 회전할 문 또는 HingePivot")]
    [SerializeField]
    private Transform doorPivot;


    // =========================================================
    // Rotation
    // =========================================================

    [Header("Door Rotation")]

    [Tooltip("닫힌 상태의 Local Y 회전")]
    [SerializeField]
    private float closedAngle = 0f;


    [Tooltip("열린 상태의 Local Y 회전")]
    [SerializeField]
    private float openAngle = 90f;


    [Tooltip("문 회전 속도")]
    [SerializeField]
    private float rotationSpeed = 180f;


    // =========================================================
    // Settings
    // =========================================================

    [Header("Settings")]

    [Tooltip("플레이어가 E키로 문을 사용할 수 있는가")]
    [SerializeField]
    private bool allowPlayerInteraction = true;


    [Tooltip("총알에 맞으면 문이 열리는가")]
    [SerializeField]
    private bool openWhenShot = true;


    // =========================================================
    // Runtime
    // =========================================================

    private bool playerInside;

    private bool isOpen;

    private bool isMoving;


    public bool IsOpen =>
        isOpen;


    // =========================================================
    // Start
    // =========================================================

    private void Start()
    {
        if (doorPivot == null)
        {
            Debug.LogError(
                $"[Door] {gameObject.name} : Door Pivot이 없습니다."
            );

            return;
        }


        SetDoorRotation(
            closedAngle
        );


        isOpen =
            false;
    }


    // =========================================================
    // Input
    // =========================================================

    private void Update()
    {
        if (!allowPlayerInteraction)
            return;


        if (!playerInside)
            return;


        if (Keyboard.current == null)
            return;


        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            ToggleDoor();
        }
    }


    // =========================================================
    // Toggle
    // =========================================================

    public void ToggleDoor()
    {
        if (isMoving)
            return;


        if (isOpen)
        {
            CloseDoor();
        }
        else
        {
            OpenDoor();
        }
    }


    // =========================================================
    // Open
    // =========================================================

    public void OpenDoor()
    {
        if (isOpen ||
            isMoving)
        {
            return;
        }


        StartCoroutine(
            RotateDoorRoutine(
                openAngle,
                true
            )
        );
    }


    // =========================================================
    // Close
    // =========================================================

    public void CloseDoor()
    {
        if (!isOpen ||
            isMoving)
        {
            return;
        }


        StartCoroutine(
            RotateDoorRoutine(
                closedAngle,
                false
            )
        );
    }


    // =========================================================
    // Shot
    // =========================================================

    public void OpenFromShot()
    {
        if (!openWhenShot)
            return;


        if (isOpen ||
            isMoving)
        {
            return;
        }


        OpenDoor();
    }


    // =========================================================
    // Rotation
    // =========================================================

    private IEnumerator RotateDoorRoutine(
        float targetAngle,
        bool opening
    )
    {
        if (doorPivot == null)
            yield break;


        isMoving =
            true;


        while (true)
        {
            Vector3 currentEuler =
                doorPivot.localEulerAngles;


            float currentY =
                NormalizeAngle(
                    currentEuler.y
                );


            float newY =
                Mathf.MoveTowardsAngle(
                    currentY,
                    targetAngle,
                    rotationSpeed *
                    Time.deltaTime
                );


            doorPivot.localRotation =
                Quaternion.Euler(
                    currentEuler.x,
                    newY,
                    currentEuler.z
                );


            if (Mathf.Abs(
                    Mathf.DeltaAngle(
                        newY,
                        targetAngle
                    )
                ) < 0.1f)
            {
                break;
            }


            yield return null;
        }


        SetDoorRotation(
            targetAngle
        );


        isOpen =
            opening;


        isMoving =
            false;
    }


    // =========================================================
    // Trigger
    // =========================================================

    private void OnTriggerEnter(
        Collider other
    )
    {
        PlayerStats player =
            other.GetComponentInParent<PlayerStats>();


        if (player == null)
            return;


        playerInside =
            true;
    }


    private void OnTriggerExit(
        Collider other
    )
    {
        PlayerStats player =
            other.GetComponentInParent<PlayerStats>();


        if (player == null)
            return;


        playerInside =
            false;
    }


    // =========================================================
    // Utility
    // =========================================================

    private void SetDoorRotation(
        float angle
    )
    {
        if (doorPivot == null)
            return;


        Vector3 euler =
            doorPivot.localEulerAngles;


        doorPivot.localRotation =
            Quaternion.Euler(
                euler.x,
                angle,
                euler.z
            );
    }


    private float NormalizeAngle(
        float angle
    )
    {
        while (angle > 180f)
        {
            angle -= 360f;
        }


        while (angle < -180f)
        {
            angle += 360f;
        }


        return angle;
    }
}