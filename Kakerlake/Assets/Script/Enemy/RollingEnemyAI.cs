using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(EnemyStats))]
[RequireComponent(typeof(NavMeshAgent))]
public class RollingEnemyAI : MonoBehaviour
{
    // =========================================================
    // References
    // =========================================================

    [Header("References")]

    [SerializeField] private EnemyStats enemyStats;

    [SerializeField] private NavMeshAgent agent;

    [Tooltip("Z축으로 구르는 모델")]
    [SerializeField] private Transform rollingModel;

    [Tooltip("적의 정면 방향을 나타내는 Empty")]
    [SerializeField] private Transform lookingEmpty;


    // =========================================================
    // Detection
    // =========================================================

    [Header("Detection")]

    [SerializeField] private float detectionRange = 20f;


    // =========================================================
    // Distance Control
    // =========================================================

    [Header("Distance Control")]

    [Tooltip("이 거리보다 가까우면 후진")]
    [SerializeField] private float retreatDistance = 2.5f;

    [Tooltip("플레이어와 유지하려는 목표 거리")]
    [SerializeField] private float preferredDistance = 3.5f;

    [Tooltip("이 거리보다 멀어지면 전진")]
    [SerializeField] private float approachDistance = 4.5f;

    [Tooltip("후진 이동 속도 배율")]
    [SerializeField] private float retreatSpeedMultiplier = 0.8f;


    // =========================================================
    // Rotation
    // =========================================================

    [Header("Rotation")]

    [Tooltip("부모 오브젝트의 Y축 회전 속도")]
    [SerializeField] private float rotationSpeed = 240f;

    [Tooltip("플레이어 방향과 이 각도 이내일 때 이동 가능")]
    [SerializeField] private float moveAngleTolerance = 8f;


    // =========================================================
    // Rolling
    // =========================================================

    [Header("Rolling")]

    [Min(0.01f)]
    [SerializeField] private float rollingRadius = 0.5f;

    [SerializeField] private float rollingSpeedMultiplier = 1f;

    [Tooltip("구르는 방향이 반대일 경우 체크")]
    [SerializeField] private bool invertRollingDirection = false;


    // =========================================================
    // Dash Attack
    // =========================================================

    [Header("Dash Attack")]

    [SerializeField] private float attackInterval = 3f;

    [SerializeField] private float attackWarningTime = 0.6f;

    [Min(0.1f)]
    [SerializeField] private float dashDistance = 6f;

    [Min(0.01f)]
    [SerializeField] private float dashSpeed = 12f;

    [SerializeField] private float damageRadius = 0.8f;

    [SerializeField] private float damageHeightOffset = 0f;

    [SerializeField] private LayerMask playerMask = ~0;


    // =========================================================
    // Stun
    // =========================================================

    [Header("Stun")]

    [SerializeField] private float stunDuration = 1.2f;


    // =========================================================
    // Attack Warning
    // =========================================================

    [Header("Attack Warning")]

    [SerializeField] private Color warningColor = Color.red;

    [SerializeField] private Renderer[] modelRenderers;


    // =========================================================
    // State
    // =========================================================

    public enum EnemyState
    {
        Idle,
        Chase,
        AttackWarning,
        Dash,
        Stunned
    }

    public enum MovementState
    {
        Stop,
        Forward,
        Backward
    }


    [Header("Runtime State")]

    [SerializeField]
    private EnemyState currentState = EnemyState.Idle;

    [SerializeField]
    private MovementState movementState = MovementState.Stop;

    [SerializeField]
    private float attackTimer;

    [SerializeField]
    private float currentPlayerDistance;


    public EnemyState CurrentState => currentState;

    public bool IsStunned =>
        currentState == EnemyState.Stunned;


    private PlayerStats targetPlayer;

    private Transform player;

    private float stateTimer;

    private Vector3 dashDirection;

    private float remainingDashDistance;

    private Vector3 previousDashPosition;

    private bool dashFinishPending;


    // =========================================================
    // Rolling Runtime
    // =========================================================

    private Vector3 previousPosition;

    private Quaternion originalModelRotation;

    private float rollingAngle;


    // =========================================================
    // Damage Runtime
    // =========================================================

    private readonly HashSet<PlayerStats> damagedPlayers =
        new HashSet<PlayerStats>();


    // =========================================================
    // Warning Runtime
    // =========================================================

    private MaterialPropertyBlock[] originalBlocks;

    private bool warningActive;


    private bool CanUseAgent =>
        agent != null &&
        agent.isActiveAndEnabled &&
        agent.isOnNavMesh;


    // =========================================================
    // Awake
    // =========================================================

    private void Awake()
    {
        if (enemyStats == null)
        {
            enemyStats = GetComponent<EnemyStats>();
        }

        if (agent == null)
        {
            agent = GetComponent<NavMeshAgent>();
        }

        if (rollingModel == null ||
            rollingModel == transform)
        {
            Debug.LogError(
                "[RollingEnemy] Rolling Model을 연결하세요.",
                this
            );

            enabled = false;
            return;
        }

        if (lookingEmpty == null)
        {
            Debug.LogWarning(
                "[RollingEnemy] Looking Empty가 없습니다. " +
                "기본 Forward 방향을 사용합니다.",
                this
            );
        }

        agent.updateRotation = false;
        agent.updatePosition = true;


        // 모델의 초기 회전값 저장

        originalModelRotation =
            rollingModel.localRotation;

        rollingAngle = 0f;


        if (modelRenderers == null ||
            modelRenderers.Length == 0)
        {
            modelRenderers =
                rollingModel.GetComponentsInChildren<Renderer>();
        }


        originalBlocks =
            new MaterialPropertyBlock[modelRenderers.Length];

        for (int i = 0; i < originalBlocks.Length; i++)
        {
            originalBlocks[i] =
                new MaterialPropertyBlock();
        }
    }


    // =========================================================
    // Enable
    // =========================================================

    private void OnEnable()
    {
        currentState = EnemyState.Idle;

        movementState = MovementState.Stop;

        previousPosition = transform.position;

        attackTimer = 0f;

        stateTimer = 0f;

        dashFinishPending = false;

        damagedPlayers.Clear();

        StopMoving();
    }


    // =========================================================
    // Update
    // =========================================================

    private void Update()
    {
        if (enemyStats == null ||
            enemyStats.IsDead)
        {
            StopMoving();
            SetWarningColor(false);
            return;
        }

        if (!CanUseAgent)
            return;


        switch (currentState)
        {
            case EnemyState.Idle:
                UpdateIdle();
                break;

            case EnemyState.Chase:
                UpdateChase();
                break;

            case EnemyState.AttackWarning:
                UpdateAttackWarning();
                break;

            case EnemyState.Dash:
                UpdateDash();
                break;

            case EnemyState.Stunned:
                UpdateStunned();
                break;
        }
    }


    // =========================================================
    // Player Search
    // =========================================================

    private void FindPlayer()
    {
        targetPlayer =
            FindFirstObjectByType<PlayerStats>();

        player =
            targetPlayer != null
            ? targetPlayer.transform
            : null;
    }


    private bool CanDetectPlayer()
    {
        if (targetPlayer == null ||
            player == null ||
            targetPlayer.IsDead)
        {
            FindPlayer();
        }

        if (targetPlayer == null ||
            player == null ||
            targetPlayer.IsDead)
        {
            return false;
        }

        currentPlayerDistance =
            GetPlayerDirection().magnitude;

        return currentPlayerDistance <= detectionRange;
    }


    // =========================================================
    // Player 방향
    // =========================================================

    private Vector3 GetPlayerDirection()
    {
        if (player == null)
            return Vector3.zero;


        Vector3 direction =
            player.position - transform.position;

        direction.y = 0f;

        return direction;
    }


    // =========================================================
    // Looking Empty 방향
    // =========================================================

    private Vector3 GetForwardDirection()
    {
        Vector3 direction =
            lookingEmpty != null
            ? lookingEmpty.position - transform.position
            : transform.forward;

        direction.y = 0f;


        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = transform.forward;
            direction.y = 0f;
        }

        return direction.normalized;
    }


    // =========================================================
    // Idle
    // =========================================================

    private void UpdateIdle()
    {
        if (!CanDetectPlayer())
            return;

        currentState = EnemyState.Chase;

        movementState = MovementState.Stop;

        attackTimer = 0f;
    }


    // =========================================================
    // Chase
    // =========================================================

    private void UpdateChase()
    {
        if (!CanDetectPlayer())
        {
            StopMoving();

            currentState = EnemyState.Idle;

            movementState = MovementState.Stop;

            attackTimer = 0f;

            return;
        }


        Vector3 playerDirection =
            GetPlayerDirection();


        float distance =
            playerDirection.magnitude;


        // =====================================================
        // 돌진 사거리 내부에서만 공격 Timer 작동
        // =====================================================

        if (distance <= dashDistance)
        {
            attackTimer += Time.deltaTime;
        }
        else
        {
            attackTimer = 0f;
        }


        // 공격 Timer 완료

        if (distance <= dashDistance &&
            attackTimer >= attackInterval)
        {
            StartAttackWarning();
            return;
        }


        // =====================================================
        // Player 방향으로 Y축 회전
        // =====================================================

        RotateTowardsDirection(
            playerDirection
        );


        // Debug Ray

        Debug.DrawRay(
            transform.position,
            playerDirection,
            Color.yellow
        );

        Debug.DrawRay(
            transform.position,
            GetForwardDirection() * 3f,
            Color.cyan
        );


        // =====================================================
        // 플레이어와 거리 유지
        // =====================================================

        UpdateDistanceMovement(distance);


        // =====================================================
        // 바라보는 방향이 충분히 맞지 않았다면
        // 이동하지 않고 회전만 진행
        // =====================================================

        if (playerDirection.sqrMagnitude > 0.0001f)
        {
            float angle =
                Vector3.Angle(
                    GetForwardDirection(),
                    playerDirection
                );


            if (angle > moveAngleTolerance)
            {
                StopMoving();
                return;
            }
        }


        // =====================================================
        // 전진 / 후진
        // =====================================================

        switch (movementState)
        {
            case MovementState.Forward:

                float forwardDistance =
                    Mathf.Min(
                        enemyStats.MoveSpeed * Time.deltaTime,
                        Mathf.Max(
                            0f,
                            distance - preferredDistance
                        )
                    );

                MoveStraight(
                    GetForwardDirection(),
                    forwardDistance
                );

                break;


            case MovementState.Backward:

                float backwardDistance =
                    Mathf.Min(
                        enemyStats.MoveSpeed *
                        retreatSpeedMultiplier *
                        Time.deltaTime,

                        Mathf.Max(
                            0f,
                            preferredDistance - distance
                        )
                    );

                MoveStraight(
                    -GetForwardDirection(),
                    backwardDistance
                );

                break;


            case MovementState.Stop:

                StopMoving();

                break;
        }
    }


    // =========================================================
    // Distance Control
    // =========================================================

    private void UpdateDistanceMovement(float distance)
    {
        // 너무 가까우면 후진

        if (distance < retreatDistance)
        {
            movementState =
                MovementState.Backward;

            return;
        }


        // 너무 멀어지면 전진

        if (distance > approachDistance)
        {
            movementState =
                MovementState.Forward;

            return;
        }


        // =====================================================
        // 후진 중이라면 목표 거리까지 계속 후진
        // =====================================================

        if (movementState == MovementState.Backward)
        {
            if (distance >= preferredDistance - 0.03f)
            {
                movementState =
                    MovementState.Stop;
            }

            return;
        }


        // =====================================================
        // 전진 중이라면 목표 거리까지 계속 전진
        // =====================================================

        if (movementState == MovementState.Forward)
        {
            if (distance <= preferredDistance + 0.03f)
            {
                movementState =
                    MovementState.Stop;
            }
        }
    }


    // =========================================================
    // Y축 방향 전환
    // =========================================================

    private void RotateTowardsDirection(
        Vector3 targetDirection
    )
    {
        targetDirection.y = 0f;


        if (targetDirection.sqrMagnitude <= 0.0001f)
            return;


        targetDirection.Normalize();


        Vector3 forward =
            GetForwardDirection();


        float angle =
            Vector3.SignedAngle(
                forward,
                targetDirection,
                Vector3.up
            );


        float rotateAmount =
            Mathf.Clamp(
                angle,
                -rotationSpeed * Time.deltaTime,
                rotationSpeed * Time.deltaTime
            );


        // 부모는 Y축으로만 방향 전환

        transform.Rotate(
            Vector3.up,
            rotateAmount,
            Space.World
        );
    }


    // =========================================================
    // 직선 이동
    // =========================================================

    private float MoveStraight(
        Vector3 direction,
        float distance
    )
    {
        if (!CanUseAgent)
            return 0f;


        if (distance <= 0.0001f)
        {
            StopMoving();
            return 0f;
        }


        direction.y = 0f;
        direction.Normalize();


        Vector3 startPosition =
            agent.nextPosition;


        Vector3 targetPosition =
            startPosition +
            direction * distance;


        // NavMesh 경계 또는 장애물 확인

        if (agent.Raycast(
                targetPosition,
                out NavMeshHit hit))
        {
            float availableDistance =
                Vector3.Dot(
                    hit.position - startPosition,
                    direction
                );


            distance =
                Mathf.Clamp(
                    availableDistance - 0.03f,
                    0f,
                    distance
                );
        }


        if (distance <= 0.0001f)
        {
            StopMoving();
            return 0f;
        }


        agent.isStopped = false;


        // LookingEmpty 기준 직선 이동
        // SetDestination을 사용하지 않음

        agent.Move(
            direction * distance
        );


        return distance;
    }


    // =========================================================
    // 모델 Z축 구르기
    // =========================================================

    private void LateUpdate()
    {
        Vector3 movement =
            transform.position - previousPosition;


        previousPosition =
            transform.position;


        if (rollingModel == null ||
            !CanUseAgent)
        {
            return;
        }


        movement.y = 0f;


        // =====================================================
        // LookingEmpty 방향 기준 이동량
        //
        // 전진 = 양수
        // 후진 = 음수
        // =====================================================

        float signedDistance =
            Vector3.Dot(
                movement,
                GetForwardDirection()
            );


        if (Mathf.Abs(signedDistance) <= 0.00001f)
            return;


        float direction =
            invertRollingDirection ? -1f : 1f;


        float rotationAmount =
            signedDistance /
            Mathf.Max(0.01f, rollingRadius) *
            Mathf.Rad2Deg *
            rollingSpeedMultiplier *
            direction;


        rollingAngle =
            Mathf.Repeat(
                rollingAngle + rotationAmount,
                360f
            );


        // =====================================================
        // 모델의 초기 회전값 유지
        // Local X축 회전만 추가
        // =====================================================

        rollingModel.localRotation =
        originalModelRotation *
        Quaternion.AngleAxis(
        rollingAngle,
        Vector3.right
        );
    }


    // =========================================================
    // Attack Warning
    // =========================================================

    private void StartAttackWarning()
    {
        StopMoving();

        movementState =
            MovementState.Stop;


        // 공격 시작 시 Player 방향 기억

        dashDirection =
            GetPlayerDirection();


        if (dashDirection.sqrMagnitude <= 0.0001f)
        {
            dashDirection =
                GetForwardDirection();
        }


        dashDirection.Normalize();


        currentState =
            EnemyState.AttackWarning;


        stateTimer =
            Mathf.Max(0f, attackWarningTime);


        attackTimer = 0f;


        SetWarningColor(true);


        Debug.Log(
            "[RollingEnemy] 돌진 공격 준비"
        );
    }


    private void UpdateAttackWarning()
    {
        // 저장된 공격 방향으로만 회전
        // Player의 새로운 위치는 반영하지 않음

        RotateTowardsDirection(
            dashDirection
        );


        stateTimer -= Time.deltaTime;


        float angle =
            Vector3.Angle(
                GetForwardDirection(),
                dashDirection
            );


        // 공격 예고가 끝났고 방향까지 맞으면 돌진

        if (stateTimer <= 0f &&
            angle <= 0.5f)
        {
            StartDash();
        }
    }


    // =========================================================
    // Dash Start
    // =========================================================

    private void StartDash()
    {
        SetWarningColor(false);


        currentState =
            EnemyState.Dash;


        // 현재 LookingEmpty 방향으로 고정

        dashDirection =
            GetForwardDirection();


        remainingDashDistance =
            dashDistance;


        previousDashPosition =
            transform.position;


        dashFinishPending =
            false;


        damagedPlayers.Clear();


        agent.ResetPath();

        agent.isStopped = false;


        Debug.Log(
            "[RollingEnemy] 돌진 시작"
        );
    }


    // =========================================================
    // Dash Movement
    // =========================================================

    private void UpdateDash()
    {
        // 이전 위치부터 현재 위치까지 공격 판정

        CheckDashDamage(
            previousDashPosition,
            transform.position
        );


        previousDashPosition =
            transform.position;


        // 마지막 공격 판정까지 끝났다면 경직

        if (dashFinishPending ||
            remainingDashDistance <= 0.001f)
        {
            StartStun();
            return;
        }


        float requestedDistance =
            Mathf.Min(
                dashSpeed * Time.deltaTime,
                remainingDashDistance
            );


        float movedDistance =
            MoveStraight(
                dashDirection,
                requestedDistance
            );


        remainingDashDistance -=
            movedDistance;


        // 벽에 막히거나 최대 거리 이동 시 종료

        if (movedDistance <= 0.0001f ||
            movedDistance < requestedDistance - 0.001f ||
            remainingDashDistance <= 0.001f)
        {
            dashFinishPending = true;
        }


        Debug.DrawRay(
            transform.position,
            dashDirection * remainingDashDistance,
            Color.red
        );
    }


    // =========================================================
    // Dash Damage
    // =========================================================

    private void CheckDashDamage(
        Vector3 startPosition,
        Vector3 endPosition
    )
    {
        Vector3 offset =
            Vector3.up * damageHeightOffset;


        Vector3 start =
            startPosition + offset;


        Vector3 end =
            endPosition + offset;


        Collider[] hits;


        if ((end - start).sqrMagnitude <= 0.000001f)
        {
            hits = Physics.OverlapSphere(
                start,
                damageRadius,
                playerMask,
                QueryTriggerInteraction.Ignore
            );
        }
        else
        {
            hits = Physics.OverlapCapsule(
                start,
                end,
                damageRadius,
                playerMask,
                QueryTriggerInteraction.Ignore
            );
        }


        foreach (Collider hit in hits)
        {
            if (hit == null)
                continue;


            PlayerStats hitPlayer =
                hit.GetComponentInParent<PlayerStats>();


            if (hitPlayer == null ||
                hitPlayer.IsDead)
            {
                continue;
            }


            // 한 번의 돌진당 한 번만 데미지

            if (!damagedPlayers.Add(hitPlayer))
                continue;


            hitPlayer.TakeDamage(
                enemyStats.AttackPower
            );


            Debug.Log(
                $"[RollingEnemy] 돌진 명중 / " +
                $"Damage = {enemyStats.AttackPower}"
            );
        }
    }


    // =========================================================
    // Stun
    // =========================================================

    private void StartStun()
    {
        StopMoving();

        SetWarningColor(false);


        currentState =
            EnemyState.Stunned;


        movementState =
            MovementState.Stop;


        stateTimer =
            Mathf.Max(0f, stunDuration);


        attackTimer = 0f;


        Debug.Log(
            "[RollingEnemy] 경직 시작"
        );
    }


    private void UpdateStunned()
    {
        stateTimer -= Time.deltaTime;


        if (stateTimer > 0f)
            return;


        currentState =
            EnemyState.Idle;


        Debug.Log(
            "[RollingEnemy] 경직 종료"
        );
    }


    // =========================================================
    // Stop
    // =========================================================

    private void StopMoving()
    {
        if (!CanUseAgent)
            return;


        agent.isStopped = true;

        agent.ResetPath();

        agent.velocity = Vector3.zero;
    }


    // =========================================================
    // Warning Color
    // =========================================================

    private void SetWarningColor(bool active)
    {
        if (modelRenderers == null ||
            originalBlocks == null)
        {
            return;
        }


        if (warningActive == active)
            return;


        warningActive = active;


        for (int i = 0; i < modelRenderers.Length; i++)
        {
            Renderer modelRenderer =
                modelRenderers[i];


            if (modelRenderer == null)
                continue;


            if (active)
            {
                modelRenderer.GetPropertyBlock(
                    originalBlocks[i]
                );


                MaterialPropertyBlock warningBlock =
                    new MaterialPropertyBlock();


                modelRenderer.GetPropertyBlock(
                    warningBlock
                );


                warningBlock.SetColor(
                    "_BaseColor",
                    warningColor
                );


                warningBlock.SetColor(
                    "_Color",
                    warningColor
                );


                modelRenderer.SetPropertyBlock(
                    warningBlock
                );
            }
            else
            {
                modelRenderer.SetPropertyBlock(
                    originalBlocks[i].isEmpty
                    ? null
                    : originalBlocks[i]
                );
            }
        }
    }


    // =========================================================
    // Disable
    // =========================================================

    private void OnDisable()
    {
        SetWarningColor(false);

        StopMoving();

        damagedPlayers.Clear();
    }


    // =========================================================
    // Inspector Protection
    // =========================================================

    private void OnValidate()
    {
        retreatDistance =
            Mathf.Max(0.1f, retreatDistance);

        preferredDistance =
            Mathf.Max(retreatDistance + 0.1f, preferredDistance);

        approachDistance =
            Mathf.Max(preferredDistance + 0.1f, approachDistance);

        dashDistance =
            Mathf.Max(approachDistance, dashDistance);

        detectionRange =
            Mathf.Max(dashDistance, detectionRange);

        rotationSpeed =
            Mathf.Max(1f, rotationSpeed);

        retreatSpeedMultiplier =
            Mathf.Max(0f, retreatSpeedMultiplier);

        moveAngleTolerance =
            Mathf.Clamp(moveAngleTolerance, 1f, 30f);

        rollingRadius =
            Mathf.Max(0.01f, rollingRadius);

        attackInterval =
            Mathf.Max(0f, attackInterval);

        attackWarningTime =
            Mathf.Max(0f, attackWarningTime);

        stunDuration =
            Mathf.Max(0f, stunDuration);
    }


    // =========================================================
    // Gizmos
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        // Detection Range

        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(
            transform.position,
            detectionRange
        );


        // Retreat Distance

        Gizmos.color = new Color(1f, 0.5f, 0f);

        Gizmos.DrawWireSphere(
            transform.position,
            retreatDistance
        );


        // Preferred Distance

        Gizmos.color = Color.green;

        Gizmos.DrawWireSphere(
            transform.position,
            preferredDistance
        );


        // Approach Distance

        Gizmos.color = Color.blue;

        Gizmos.DrawWireSphere(
            transform.position,
            approachDistance
        );


        // Dash Range

        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(
            transform.position,
            dashDistance
        );


        // Looking Empty 방향

        Gizmos.color = Color.cyan;

        Gizmos.DrawLine(
            transform.position,
            transform.position +
            GetForwardDirection() * dashDistance
        );
    }
}