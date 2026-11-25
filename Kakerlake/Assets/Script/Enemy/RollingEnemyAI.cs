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

    [SerializeField]
    private EnemyStats enemyStats;

    [SerializeField]
    private NavMeshAgent agent;

    [Tooltip("실제로 구르면서 회전할 모델")]
    [SerializeField]
    private Transform rollingModel;


    // =========================================================
    // Detection
    // =========================================================

    [Header("Detection")]

    [SerializeField]
    private float detectionRange = 20f;


    // =========================================================
    // Rolling
    // =========================================================

    [Header("Rolling")]

    [Tooltip("적 모델의 실제 반지름")]
    [Min(0.01f)]
    [SerializeField]
    private float rollingRadius = 0.5f;

    [Tooltip("구르는 회전 속도 배율")]
    [SerializeField]
    private float rollingSpeedMultiplier = 1f;


    // =========================================================
    // Attack
    // =========================================================

    [Header("Dash Attack")]

    [Tooltip("추적을 시작한 후 공격하기까지 걸리는 시간")]
    [SerializeField]
    private float attackInterval = 3f;

    [Tooltip("공격 전 정지 및 붉은색 예고 시간")]
    [SerializeField]
    private float attackWarningTime = 0.6f;

    [Tooltip("돌진하는 최대 거리")]
    [SerializeField]
    private float dashDistance = 6f;

    [Tooltip("돌진 이동 속도")]
    [SerializeField]
    private float dashSpeed = 12f;

    [Tooltip("돌진 공격 판정 반지름")]
    [SerializeField]
    private float damageRadius = 0.8f;

    [Tooltip("오브젝트 중심 기준 공격 판정 높이")]
    [SerializeField]
    private float damageHeightOffset = 0f;

    [SerializeField]
    private LayerMask playerMask = ~0;


    // =========================================================
    // Stun
    // =========================================================

    [Header("Stun")]

    [Tooltip("돌진이 끝난 후 행동 불가 시간")]
    [SerializeField]
    private float stunDuration = 1.2f;


    // =========================================================
    // Warning Color
    // =========================================================

    [Header("Attack Warning")]

    [SerializeField]
    private Color warningColor = Color.red;

    [Tooltip("공격 준비 시 색상이 변경될 Renderer")]
    [SerializeField]
    private Renderer[] modelRenderers;


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

    [Header("Runtime State")]

    [SerializeField]
    private EnemyState currentState = EnemyState.Idle;


    public EnemyState CurrentState => currentState;

    public bool IsStunned =>
        currentState == EnemyState.Stunned;


    private PlayerStats targetPlayer;

    private Transform player;

    private float stateTimer;

    private Vector3 dashDirection;

    private Vector3 lastMoveDirection = Vector3.forward;

    private float remainingDashDistance;

    private Vector3 previousDashPosition;

    private bool dashFinishPending;

    private Vector3 previousPosition;


    // 돌진 한 번에 같은 플레이어에게
    // 데미지가 여러 번 들어가는 것 방지

    private readonly HashSet<PlayerStats> damagedPlayers =
        new HashSet<PlayerStats>();


    // 모델 색상 복구용

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
                "[RollingEnemy] 자식 Rolling Model을 연결하세요.",
                this
            );

            enabled = false;
            return;
        }

        agent.updateRotation = false;
        agent.updatePosition = true;

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

        previousPosition = transform.position;

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

        float distance =
            Vector3.Distance(
                transform.position,
                player.position
            );

        return distance <= detectionRange;
    }


    // =========================================================
    // Idle
    // =========================================================

    private void UpdateIdle()
    {
        if (!CanDetectPlayer())
            return;

        StartChase();
    }


    // =========================================================
    // Chase
    // =========================================================

    private void StartChase()
    {
        currentState = EnemyState.Chase;

        stateTimer = Mathf.Max(0f, attackInterval);

        agent.isStopped = false;

        agent.speed = enemyStats.MoveSpeed;
    }


    private void UpdateChase()
    {
        if (!CanDetectPlayer())
        {
            StopMoving();

            currentState = EnemyState.Idle;

            return;
        }

        agent.speed = enemyStats.MoveSpeed;

        agent.isStopped = false;

        agent.SetDestination(
            player.position
        );


        // 공격까지 남은 시간

        stateTimer -= Time.deltaTime;


        if (stateTimer <= 0f)
        {
            StartAttackWarning();
        }
    }


    // =========================================================
    // Rolling Visual
    // =========================================================

    private void LateUpdate()
    {
        Vector3 movement =
            transform.position -
            previousPosition;

        previousPosition =
            transform.position;

        movement.y = 0f;


        if (rollingModel == null)
            return;


        if (currentState != EnemyState.Chase &&
            currentState != EnemyState.Dash)
        {
            return;
        }


        float distance =
            movement.magnitude;


        if (distance <= 0.0001f)
            return;


        Vector3 moveDirection =
            movement.normalized;

        lastMoveDirection =
            moveDirection;


        // 이동 방향에 따라 구르는 회전축 계산

        Vector3 rotationAxis =
            Vector3.Cross(
                Vector3.up,
                moveDirection
            );


        // 실제 이동 거리에 비례한 회전량

        float rotationAmount =
            distance /
            Mathf.Max(0.01f, rollingRadius) *
            Mathf.Rad2Deg *
            rollingSpeedMultiplier;


        rollingModel.Rotate(
            rotationAxis,
            rotationAmount,
            Space.World
        );
    }


    // =========================================================
    // Attack Warning
    // =========================================================

    private void StartAttackWarning()
    {
        StopMoving();


        // 공격을 준비하는 순간의
        // 플레이어 방향을 기억

        dashDirection =
            player.position -
            transform.position;

        dashDirection.y = 0f;


        if (dashDirection.sqrMagnitude <= 0.001f)
        {
            dashDirection =
                lastMoveDirection;
        }

        dashDirection.Normalize();


        currentState =
            EnemyState.AttackWarning;


        stateTimer =
            Mathf.Max(0f, attackWarningTime);


        // 모델 붉은색 변경

        SetWarningColor(true);


        Debug.Log(
            "[RollingEnemy] 돌진 공격 준비"
        );
    }


    private void UpdateAttackWarning()
    {
        // 이 상태에서는 이동하지 않고
        // 플레이어 방향도 다시 계산하지 않음

        stateTimer -= Time.deltaTime;


        if (stateTimer <= 0f)
        {
            StartDash();
        }
    }


    // =========================================================
    // Dash Start
    // =========================================================

    private void StartDash()
    {
        // 기존 모델 색상으로 복구

        SetWarningColor(false);


        currentState =
            EnemyState.Dash;


        remainingDashDistance =
            Mathf.Max(0f, dashDistance);


        previousDashPosition =
            transform.position;


        dashFinishPending =
            false;


        damagedPlayers.Clear();


        // 기존 플레이어 추적 경로 제거

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
        // 이전 프레임부터 현재 프레임까지
        // 실제로 이동한 구간의 공격 판정

        CheckDashDamage(
            previousDashPosition,
            transform.position
        );

        previousDashPosition =
            transform.position;


        // 마지막 이동 구간의 공격 판정까지
        // 처리한 후 경직 시작

        if (dashFinishPending ||
            remainingDashDistance <= 0.001f)
        {
            StartStun();

            return;
        }


        float moveDistance =
            Mathf.Min(
                Mathf.Max(0.01f, dashSpeed) *
                Time.deltaTime,
                remainingDashDistance
            );


        if (moveDistance <= 0f)
            return;


        Vector3 startPosition =
            agent.nextPosition;


        Vector3 nextPosition =
            startPosition +
            dashDirection * moveDistance;


        // =====================================================
        // NavMesh 끝 또는 장애물 확인
        // =====================================================

        if (agent.Raycast(
                nextPosition,
                out NavMeshHit navHit))
        {
            float availableDistance =
                Vector3.Dot(
                    navHit.position - startPosition,
                    dashDirection
                );


            moveDistance =
                Mathf.Clamp(
                    availableDistance - 0.03f,
                    0f,
                    moveDistance
                );


            // 벽 또는 이동 불가능한 영역에 도달하면
            // 이번 이동 후 돌진 종료

            dashFinishPending = true;
        }


        if (moveDistance <= 0.001f)
        {
            StartStun();

            return;
        }


        // 고정된 방향으로 직선 이동

        agent.Move(
            dashDirection * moveDistance
        );


        remainingDashDistance -=
            moveDistance;


        if (remainingDashDistance <= 0.001f)
        {
            dashFinishPending = true;
        }
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


        // 이동하지 않은 순간에는 구형 공격 판정

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
            // 빠른 돌진 중 플레이어를 통과하더라도
            // 이전 위치와 현재 위치 사이를 검사

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


            // 돌진 한 번당 한 대상에게
            // 데미지 한 번만 적용

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
    // Stun Start
    // =========================================================

    private void StartStun()
    {
        StopMoving();

        SetWarningColor(false);


        currentState =
            EnemyState.Stunned;


        stateTimer =
            Mathf.Max(0f, stunDuration);


        Debug.Log(
            "[RollingEnemy] 돌진 종료 / 경직 시작"
        );
    }


    private void UpdateStunned()
    {
        stateTimer -= Time.deltaTime;


        if (stateTimer > 0f)
            return;


        // 경직이 끝나면 플레이어를 다시 감지

        currentState =
            EnemyState.Idle;


        Debug.Log(
            "[RollingEnemy] 경직 종료"
        );
    }


    // =========================================================
    // Stop Movement
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
                // 기존 색상 및 MaterialPropertyBlock 기억

                modelRenderer.GetPropertyBlock(
                    originalBlocks[i]
                );


                MaterialPropertyBlock warningBlock =
                    new MaterialPropertyBlock();


                modelRenderer.GetPropertyBlock(
                    warningBlock
                );


                // URP Material

                warningBlock.SetColor(
                    "_BaseColor",
                    warningColor
                );


                // 일반 Material

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
                // 기존 Material 상태로 복구

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
    // Gizmos
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        // 플레이어 감지 범위

        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(
            transform.position,
            detectionRange
        );


        // 돌진 데미지 범위

        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(
            transform.position +
            Vector3.up * damageHeightOffset,
            damageRadius
        );


        // 돌진 방향 및 거리

        Vector3 direction =
            Application.isPlaying &&
            dashDirection.sqrMagnitude > 0.001f
            ? dashDirection
            : transform.forward;


        Gizmos.color = Color.cyan;

        Gizmos.DrawLine(
            transform.position,
            transform.position +
            direction * dashDistance
        );
    }
}