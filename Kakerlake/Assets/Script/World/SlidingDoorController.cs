using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(BoxCollider))]
public class SlidingDoorController : MonoBehaviour
{
    // =========================================================
    // References
    // =========================================================

    [Header("References")]

    [Tooltip("실제로 X축으로 이동할 문 모델")]
    [SerializeField]
    private Transform doorModel;


    // =========================================================
    // Normal Interaction
    // =========================================================

    [Header("Normal Door")]

    [Tooltip("문이 X축으로 이동할 거리")]
    [SerializeField]
    private float openDistance = 3f;


    [Tooltip("E키로 열고 닫을 때 이동 속도")]
    [Min(0.01f)]
    [SerializeField]
    private float moveSpeed = 2f;


    [Tooltip("반대 방향으로 문을 열고 싶다면 체크")]
    [SerializeField]
    private bool invertOpenDirection = false;


    // =========================================================
    // Projectile Reaction
    // =========================================================

    [Header("Projectile Reaction")]

    [SerializeField]
    private bool reactToProjectile = true;


    [Tooltip("총알 속도를 문 이동 속도로 변환하는 배율")]
    [SerializeField]
    private float projectileSpeedMultiplier = 0.15f;


    [Tooltip("총알에 맞았을 때 최소 이동 속도")]
    [SerializeField]
    private float minimumShotSpeed = 4f;


    [Tooltip("총알에 맞았을 때 최대 이동 속도")]
    [SerializeField]
    private float maximumShotSpeed = 15f;


    // =========================================================
    // Explosion Reaction
    // =========================================================

    [Header("Explosion Reaction")]

    [SerializeField]
    private bool reactToExplosion = true;


    [SerializeField]
    private float minimumExplosionSpeed = 5f;


    [SerializeField]
    private float maximumExplosionSpeed = 20f;


    // =========================================================
    // Interaction
    // =========================================================

    [Header("Interaction")]

    [SerializeField]
    private bool allowPlayerInteraction = true;


    // =========================================================
    // Runtime
    // =========================================================

    private Vector3 closedPosition;

    private Vector3 openPosition;

    private Vector3 targetPosition;


    private float currentMoveSpeed;


    private bool isOpen;

    private bool initialized;


    private readonly HashSet<Collider> playerColliders =
        new HashSet<Collider>();


    public bool IsOpen =>
        isOpen;


    // =========================================================
    // Awake
    // =========================================================

    private void Awake()
    {
        if (doorModel == null)
        {
            Debug.LogError(
                $"[SlidingDoor] {name} : Door Model이 없습니다."
            );

            enabled = false;

            return;
        }


        // 처음 위치를 닫힌 위치로 저장
        closedPosition =
            doorModel.localPosition;


        // 문이 열릴 방향
        float direction =
            invertOpenDirection
            ? -1f
            : 1f;


        // X축으로 이동할 최종 위치
        openPosition =
            closedPosition +
            Vector3.right *
            Mathf.Abs(openDistance) *
            direction;


        targetPosition =
            closedPosition;


        currentMoveSpeed =
            moveSpeed;


        isOpen =
            false;


        initialized =
            true;
    }


    // =========================================================
    // Update
    // =========================================================

    private void Update()
    {
        if (!initialized)
            return;


        // 문 이동
        UpdateDoorMovement();


        // 제거되거나 비활성화된 Collider 정리
        playerColliders.RemoveWhere(
            col => col == null ||
                   !col.enabled ||
                   !col.gameObject.activeInHierarchy
        );


        // =====================================================
        // Player Interaction
        // =====================================================

        if (!allowPlayerInteraction)
            return;


        if (PlayerGameplayControl.GameplayLocked)
            return;


        if (playerColliders.Count == 0)
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
        if (!initialized)
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
    // Normal Open
    // =========================================================

    public void OpenDoor()
    {
        if (!initialized)
            return;


        StartDoorMovement(
            openPosition,
            moveSpeed,
            true
        );
    }


    // =========================================================
    // Normal Close
    // =========================================================

    public void CloseDoor()
    {
        if (!initialized)
            return;


        StartDoorMovement(
            closedPosition,
            moveSpeed,
            false
        );
    }


    // =========================================================
    // Projectile Hit
    //
    // 기존 DoorController와 동일한 함수 형식
    // =========================================================

    public void HitByProjectile(
        Vector3 projectileVelocity,
        Vector3 hitPoint
    )
    {
        if (!initialized ||
            !reactToProjectile)
        {
            return;
        }


        float projectileSpeed =
            projectileVelocity.magnitude;


        if (projectileSpeed <= 0.01f)
            return;


        // 총알 속도를 문 이동 속도로 변환
        float impactSpeed =
            projectileSpeed *
            projectileSpeedMultiplier;


        impactSpeed =
            Mathf.Clamp(
                impactSpeed,
                minimumShotSpeed,
                maximumShotSpeed
            );


        OpenFromImpact(
            impactSpeed
        );


        Debug.Log(
            $"[SlidingDoor] Projectile Hit / " +
            $"Bullet Speed = {projectileSpeed:F1} / " +
            $"Door Speed = {impactSpeed:F1}"
        );
    }


    // =========================================================
    // Explosion Hit
    //
    // 기존 로켓 / 수류탄과 호환되는 함수 형식
    // =========================================================

    public void HitByExplosion(
        Vector3 explosionCenter,
        Vector3 hitPoint,
        float normalizedStrength
    )
    {
        if (!initialized ||
            !reactToExplosion)
        {
            return;
        }


        normalizedStrength =
            Mathf.Clamp01(
                normalizedStrength
            );


        if (normalizedStrength <= 0.001f)
            return;


        float impactSpeed =
            Mathf.Lerp(
                minimumExplosionSpeed,
                maximumExplosionSpeed,
                normalizedStrength
            );


        OpenFromImpact(
            impactSpeed
        );
    }


    // =========================================================
    // 충격으로 문 열기
    // =========================================================

    private void OpenFromImpact(
        float impactSpeed
    )
    {
        // 이미 완전히 열렸으면 추가 이동하지 않음
        if (
            (
                doorModel.localPosition -
                openPosition
            ).sqrMagnitude <= 0.000001f
        )
        {
            return;
        }


        // 열리는 도중에 또 맞으면
        // 기존 속도보다 느려지지 않게 처리
        if (isOpen)
        {
            impactSpeed =
                Mathf.Max(
                    currentMoveSpeed,
                    impactSpeed
                );
        }


        StartDoorMovement(
            openPosition,
            impactSpeed,
            true
        );
    }


    // =========================================================
    // 이동 설정
    // =========================================================

    private void StartDoorMovement(
        Vector3 position,
        float speed,
        bool opening
    )
    {
        targetPosition =
            position;


        currentMoveSpeed =
            Mathf.Max(
                0.01f,
                speed
            );


        isOpen =
            opening;
    }


    // =========================================================
    // 실제 문 이동
    // =========================================================

    private void UpdateDoorMovement()
    {
        if (doorModel == null)
            return;


        doorModel.localPosition =
            Vector3.MoveTowards(
                doorModel.localPosition,
                targetPosition,
                currentMoveSpeed *
                Time.deltaTime
            );
    }


    // =========================================================
    // Player Trigger Enter
    // =========================================================

    private void OnTriggerEnter(
        Collider other
    )
    {
        PlayerStats player =
            other.GetComponentInParent<PlayerStats>();


        if (player == null)
            return;


        playerColliders.Add(
            other
        );
    }


    // =========================================================
    // Player Trigger Exit
    // =========================================================

    private void OnTriggerExit(
        Collider other
    )
    {
        playerColliders.Remove(
            other
        );
    }


    private void OnDisable()
    {
        playerColliders.Clear();
    }
}