using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class DoorController : MonoBehaviour
{
    // =========================================================
    // References
    // =========================================================

    [Header("References")]

    [Tooltip("실제로 회전할 Hinge Pivot")]
    [SerializeField]
    private Transform doorPivot;


    // =========================================================
    // Normal Interaction
    // =========================================================

    [Header("Normal Door")]

    [Tooltip("닫힌 상태 각도")]
    [SerializeField]
    private float closedAngle = 0f;

    [Tooltip("문 기준 건물 내부 방향을 알려주는 Transform")]
    [SerializeField]
    private Transform insideReference;

    [Tooltip("안/밖 열림 방향이 반대로 나온다면 체크")]
    [SerializeField]
    private bool invertManualOpenDirection = false;

    [Tooltip("E키로 열었을 때 각도")]
    [SerializeField]
    private float openAngle = 90f;


    [Tooltip("E키로 열고 닫는 회전 속도")]
    [SerializeField]
    private float rotationSpeed = 180f;

    [Tooltip("총알 충격으로 열리는 방향이 반대일 경우 체크")]
    [SerializeField]
    private bool invertProjectileDirection = false;

    // =========================================================
    // Shot Physics
    // =========================================================

    [Header("Shot Reaction")]

    [Tooltip("총알 속도를 문의 회전속도로 바꾸는 배율")]
    [SerializeField]
    private float shotAngularVelocityMultiplier = 8f;


    [Tooltip("총알에 맞았을 때 최소 회전속도")]
    [SerializeField]
    private float minimumShotAngularVelocity = 120f;


    [Tooltip("총알에 의한 최대 회전속도")]
    [SerializeField]
    private float maximumShotAngularVelocity = 500f;


    [Tooltip("문이 총알 충격 이후 감속되는 정도")]
    [SerializeField]
    private float shotAngularDrag = 3f;


    [Tooltip("문이 최대 각도에 도달했을 때 남는 반동 비율")]
    [SerializeField, Range(0f, 1f)]
    private float shotBounce = 0.35f;


    [Tooltip("총알에 의해 문이 돌아갈 수 있는 최소 각도")]
    [SerializeField]
    private float minimumShotAngle = -105f;


    [Tooltip("총알에 의해 문이 돌아갈 수 있는 최대 각도")]
    [SerializeField]
    private float maximumShotAngle = 105f;


    [Tooltip("이 속도보다 느려지면 총알 반응 종료")]
    [SerializeField]
    private float settleAngularVelocity = 2f;


    [Tooltip(
        "문의 경첩에서 이 거리 정도 떨어진 곳을 맞으면 " +
        "충격이 100% 전달됨"
    )]
    [SerializeField]
    private float fullForceLeverDistance = 0.8f;

    // =========================================================
    // Explosion Reaction
    // =========================================================

    [Header("Explosion Reaction")]

    [Tooltip("폭발에 의해 문이 움직일 수 있는가")]
    [SerializeField]
    private bool reactToExplosion = true;


    [Tooltip("폭발 중심에 매우 가까울 때 문의 최대 회전속도")]
    [SerializeField]
    private float maximumExplosionAngularVelocity = 650f;


    [Tooltip("약한 폭발에도 적용되는 최소 회전속도")]
    [SerializeField]
    private float minimumExplosionAngularVelocity = 60f;


    [Tooltip("폭발 힘 전체 배율")]
    [SerializeField]
    private float explosionForceMultiplier = 1f;

    // =========================================================
    // Interaction
    // =========================================================

    [Header("Interaction")]

    [SerializeField]
    private bool allowPlayerInteraction = true;


    [SerializeField]
    private bool reactToProjectile = true;


    // =========================================================
    // Runtime
    // =========================================================

    private bool playerInside;

    private bool isOpen;

    private bool isManualMoving;

    private bool isShotSwinging;

    private Transform currentPlayer;


    // 현재 총알로 인해 남아있는 회전속도
    // 단위 : degree / second
    private float shotAngularVelocity;


    private Coroutine manualRoutine;


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
                $"[Door] {name} : Door Pivot이 없습니다."
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
    // Update
    // =========================================================

    private void Update()
    {
        // =====================================================
        // 총알에 의한 물리 회전
        // =====================================================

        UpdateShotSwing();


        // =====================================================
        // Player Interaction
        // =====================================================

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
    // Player Toggle
    // =========================================================

    public void ToggleDoor()
    {
        if (doorPivot == null)
            return;


        // 현재 실제 각도를 보고
        // 열렸는지 판단
        float currentAngle =
            NormalizeAngle(
                doorPivot.localEulerAngles.y
            );


        float distanceFromClosed =
            Mathf.Abs(
                Mathf.DeltaAngle(
                    currentAngle,
                    closedAngle
                )
            );


        if (distanceFromClosed > 20f)
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
        if (doorPivot == null)
            return;


        // =====================================================
        // 기본값
        //
        // 플레이어가 밖에 있을 때는
        // 지금까지 사용하던 기존 Open Angle 그대로 사용
        // =====================================================

        float targetAngle =
            openAngle;


        // =====================================================
        // 플레이어가 건물 안쪽에 있는지 검사
        // =====================================================

        if (currentPlayer != null &&
            insideReference != null)
        {
            Vector3 insideDirection =
                insideReference.position -
                doorPivot.position;


            Vector3 playerDirection =
                currentPlayer.position -
                doorPivot.position;


            // 높이 무시
            insideDirection.y =
                0f;


            playerDirection.y =
                0f;


            if (insideDirection.sqrMagnitude >
                    0.001f &&
                playerDirection.sqrMagnitude >
                    0.001f)
            {
                insideDirection.Normalize();

                playerDirection.Normalize();


                // =============================================
                // 같은 방향이면 Player가 건물 내부에 있음
                // =============================================

                float side =
                    Vector3.Dot(
                        insideDirection,
                        playerDirection
                    );


                bool playerIsInside =
                    side > 0f;


                // =============================================
                // 실내에서 사용하면
                // 기존 열림 방향의 반대로 열기
                // =============================================

                if (playerIsInside)
                {
                    targetAngle =
                        -openAngle;
                }


                Debug.Log(
                    $"[Door] Player Inside = {playerIsInside} / " +
                    $"Target Angle = {targetAngle}"
                );
            }
        }


        // =====================================================
        // Prefab 자체 축이 반대인 경우 보정
        // =====================================================

        if (invertManualOpenDirection)
        {
            targetAngle *=
                -1f;
        }


        StartManualRotation(
            targetAngle,
            true
        );
    }


    // =========================================================
    // Normal Close
    // =========================================================

    public void CloseDoor()
    {
        StartManualRotation(
            closedAngle,
            false
        );
    }


    private void StartManualRotation(
        float targetAngle,
        bool opening
    )
    {
        // 총알 반응 중지
        isShotSwinging =
            false;


        shotAngularVelocity =
            0f;


        if (manualRoutine != null)
        {
            StopCoroutine(
                manualRoutine
            );
        }


        manualRoutine =
            StartCoroutine(
                RotateDoorRoutine(
                    targetAngle,
                    opening
                )
            );
    }


    // =========================================================
    // Projectile Hit
    // =========================================================

    public void HitByProjectile(
        Vector3 projectileVelocity,
        Vector3 hitPoint
    )
    {
        if (!reactToProjectile)
            return;


        if (doorPivot == null)
            return;


        // ==========================================
        // E키 애니메이션이 진행 중이라면 중단
        // ==========================================

        if (manualRoutine != null)
        {
            StopCoroutine(
                manualRoutine
            );


            manualRoutine =
                null;
        }


        isManualMoving =
            false;


        // ==========================================
        // 총알 속도
        // ==========================================

        float projectileSpeed =
            projectileVelocity.magnitude;


        if (projectileSpeed <= 0.01f)
            return;


        Vector3 forceDirection =
            projectileVelocity.normalized;


        // 수직 성분은 문의 좌우 회전에 필요 없음
        forceDirection.y =
            0f;


        if (forceDirection.sqrMagnitude <= 0.001f)
            return;


        forceDirection.Normalize();


        // ==========================================
        // 경첩 → 총알 맞은 위치
        // ==========================================

        Vector3 lever =
            hitPoint -
            doorPivot.position;


        lever.y =
            0f;


        // ==========================================
        // 경첩에서 먼 곳을 맞을수록 더 강하게
        // ==========================================

        float leverStrength =
            Mathf.Clamp01(
                lever.magnitude /
                Mathf.Max(
                    0.01f,
                    fullForceLeverDistance
                )
            );


        // =====================================================
        // Door Pivot의 Local 좌표 기준으로 회전 방향 계산
        // =====================================================

        Vector3 localLever =
            doorPivot.InverseTransformDirection(
                lever
            );


        Vector3 localForceDirection =
            doorPivot.InverseTransformDirection(
                forceDirection
            );


        float torque =
            Vector3.Cross(
                localLever,
                localForceDirection
            ).y;


        float directionSign;


        if (Mathf.Abs(torque) >
            0.001f)
        {
            directionSign =
                Mathf.Sign(
                    torque
                );
        }
        else
        {
            directionSign =
                1f;
        }


        // =====================================================
        // 모델의 회전축 방향이 반대일 경우 보정
        // =====================================================

        if (invertProjectileDirection)
        {
            directionSign *=
                -1f;
        }


        // ==========================================
        // 탄속 → 문 각속도
        // ==========================================

        float impactAngularVelocity =
            projectileSpeed *
            shotAngularVelocityMultiplier *
            Mathf.Max(
                0.25f,
                leverStrength
            );


        impactAngularVelocity =
            Mathf.Clamp(
                impactAngularVelocity,
                minimumShotAngularVelocity,
                maximumShotAngularVelocity
            );


        // 기존에 문이 움직이고 있었다면
        // 기존 힘에 새 총알 힘을 추가
        shotAngularVelocity +=
            directionSign *
            impactAngularVelocity;


        shotAngularVelocity =
            Mathf.Clamp(
                shotAngularVelocity,
                -maximumShotAngularVelocity,
                maximumShotAngularVelocity
            );


        isShotSwinging =
            true;


        Debug.Log(
            $"[Door] Projectile Hit / " +
            $"Speed={projectileSpeed:F1} / " +
            $"Angular={shotAngularVelocity:F1}"
        );
    }


    // =========================================================
    // Explosion Hit
    // =========================================================

    public void HitByExplosion(
        Vector3 explosionCenter,
        Vector3 hitPoint,
        float normalizedStrength
    )
    {
        if (!reactToExplosion)
            return;


        if (doorPivot == null)
            return;


        normalizedStrength =
            Mathf.Clamp01(
                normalizedStrength
            );


        if (normalizedStrength <= 0.001f)
            return;


        // =====================================================
        // E키로 열리고 있었다면 중단
        // =====================================================

        if (manualRoutine != null)
        {
            StopCoroutine(
                manualRoutine
            );


            manualRoutine =
                null;
        }


        isManualMoving =
            false;


        // =====================================================
        // 폭발 중심 → 문 방향
        //
        // 폭발에서 바깥쪽으로 밀려남
        // =====================================================

        Vector3 forceDirection =
            hitPoint -
            explosionCenter;


        forceDirection.y =
            0f;


        if (forceDirection.sqrMagnitude <
            0.001f)
        {
            forceDirection =
                -doorPivot.forward;
        }
        else
        {
            forceDirection.Normalize();
        }


        // =====================================================
        // 경첩 → 충격 위치
        // =====================================================

        Vector3 lever =
            hitPoint -
            doorPivot.position;


        lever.y =
            0f;


        float leverStrength =
            Mathf.Clamp01(
                lever.magnitude /
                Mathf.Max(
                    0.01f,
                    fullForceLeverDistance
                )
            );


        // =====================================================
        // 어느 방향으로 문을 돌릴 것인가
        // =====================================================

        float torque =
            Vector3.Cross(
                lever,
                forceDirection
            ).y;


        float directionSign;


        if (Mathf.Abs(torque) >
            0.001f)
        {
            directionSign =
                Mathf.Sign(
                    torque
                );
        }
        else
        {
            // 특수한 경우:
            // 충격 방향과 경첩 위치가 거의 일직선
            float side =
                Vector3.Dot(
                    forceDirection,
                    doorPivot.forward
                );


            directionSign =
                side >= 0f
                ? 1f
                : -1f;
        }


        // =====================================================
        // 폭발 거리 기반 회전력
        // =====================================================

        float angularVelocity =
            Mathf.Lerp(
                minimumExplosionAngularVelocity,
                maximumExplosionAngularVelocity,
                normalizedStrength
            );


        angularVelocity *=
            explosionForceMultiplier;


        // 문의 끝부분을 맞을수록 강함
        angularVelocity *=
            Mathf.Max(
                0.25f,
                leverStrength
            );


        // =====================================================
        // 기존에 남아있는 회전력에 폭발 힘 추가
        // =====================================================

        shotAngularVelocity +=
            directionSign *
            angularVelocity;


        shotAngularVelocity =
            Mathf.Clamp(
                shotAngularVelocity,
                -maximumExplosionAngularVelocity,
                maximumExplosionAngularVelocity
            );


        isShotSwinging =
            true;


        Debug.Log(
            $"[Door] Explosion Hit / " +
            $"Strength={normalizedStrength:F2} / " +
            $"Angular={shotAngularVelocity:F1}"
        );
    }

    // =========================================================
    // Projectile Physics Update
    // =========================================================

    private void UpdateShotSwing()
    {
        if (!isShotSwinging)
            return;


        if (doorPivot == null)
            return;


        float currentAngle =
            NormalizeAngle(
                doorPivot.localEulerAngles.y
            );


        // ==========================================
        // 현재 힘만큼 회전
        // ==========================================

        float nextAngle =
            currentAngle +
            shotAngularVelocity *
            Time.deltaTime;


        // ==========================================
        // 최대 각도 충돌
        //
        // 남아있는 힘으로 반대쪽으로 튕김
        // ==========================================

        if (nextAngle >
            maximumShotAngle)
        {
            nextAngle =
                maximumShotAngle;


            if (shotAngularVelocity > 0f)
            {
                shotAngularVelocity =
                    -shotAngularVelocity *
                    shotBounce;
            }
        }


        // ==========================================
        // 최소 각도 충돌
        // ==========================================

        if (nextAngle <
            minimumShotAngle)
        {
            nextAngle =
                minimumShotAngle;


            if (shotAngularVelocity < 0f)
            {
                shotAngularVelocity =
                    -shotAngularVelocity *
                    shotBounce;
            }
        }


        SetDoorRotation(
            nextAngle
        );


        // ==========================================
        // 마찰
        // ==========================================

        shotAngularVelocity *=
            Mathf.Exp(
                -shotAngularDrag *
                Time.deltaTime
            );


        // ==========================================
        // 충분히 느려지면 정지
        // ==========================================

        if (Mathf.Abs(
                shotAngularVelocity
            ) <= settleAngularVelocity)
        {
            shotAngularVelocity =
                0f;


            isShotSwinging =
                false;


            float finalAngle =
                NormalizeAngle(
                    doorPivot.localEulerAngles.y
                );


            isOpen =
                Mathf.Abs(
                    Mathf.DeltaAngle(
                        finalAngle,
                        closedAngle
                    )
                ) > 20f;
        }
    }


    // =========================================================
    // Normal Rotation
    // =========================================================

    private IEnumerator RotateDoorRoutine(
        float targetAngle,
        bool opening
    )
    {
        isManualMoving =
            true;


        while (true)
        {
            float currentAngle =
                NormalizeAngle(
                    doorPivot.localEulerAngles.y
                );


            float nextAngle =
                Mathf.MoveTowardsAngle(
                    currentAngle,
                    targetAngle,
                    rotationSpeed *
                    Time.deltaTime
                );


            SetDoorRotation(
                nextAngle
            );


            if (Mathf.Abs(
                    Mathf.DeltaAngle(
                        nextAngle,
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


        isManualMoving =
            false;


        manualRoutine =
            null;
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


        currentPlayer =
            player.transform;
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


        if (currentPlayer ==
            player.transform)
        {
            currentPlayer =
                null;
        }
    }


    // =========================================================
    // Rotation Utility
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