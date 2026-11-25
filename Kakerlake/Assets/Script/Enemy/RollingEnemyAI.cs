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

    [SerializeField] private Transform rollingModel;

    [SerializeField] private Transform lookingEmpty;


    // =========================================================
    // Detection
    // =========================================================

    [Header("Detection")]

    [SerializeField] private float detectionRange = 20f;


    // =========================================================
    // Movement
    // =========================================================

    [Header("Movement")]

    [Tooltip("처음 이동할 때 MoveSpeed 배율")]
    [SerializeField] private float startSpeedMultiplier = 0.6f;

    [Tooltip("자폭 직전 MoveSpeed 배율")]
    [SerializeField] private float maxSpeedMultiplier = 3f;

    [Tooltip("플레이어를 향해 Y축으로 회전하는 속도")]
    [SerializeField] private float rotationSpeed = 180f;


    // =========================================================
    // Rolling
    // =========================================================

    [Header("Rolling")]

    [Min(0.01f)]
    [SerializeField] private float rollingRadius = 0.5f;

    [SerializeField] private float rollingSpeedMultiplier = 1f;

    [SerializeField] private bool invertRollingDirection = false;


    // =========================================================
    // Self Destruction
    // =========================================================

    [Header("Self Destruction")]

    [Tooltip("플레이어를 감지한 후 자폭하기까지 걸리는 시간")]
    [Min(0.1f)]
    [SerializeField] private float explosionTime = 8f;

    [Tooltip("폭발 반경")]
    [Min(0.01f)]
    [SerializeField] private float explosionRadius = 4f;

    [Tooltip("폭발 이펙트 프리팹")]
    [SerializeField] private ExplosionVFX explosionPrefab;

    [SerializeField] private LayerMask playerMask = ~0;


    // =========================================================
    // Warning Blink
    // =========================================================

    [Header("Warning Blink")]

    [SerializeField] private Color warningColor = Color.red;

    [Tooltip("처음 색상이 바뀌는 간격")]
    [Min(0.01f)]
    [SerializeField] private float startBlinkInterval = 0.6f;

    [Tooltip("자폭 직전 색상이 바뀌는 간격")]
    [Min(0.01f)]
    [SerializeField] private float finalBlinkInterval = 0.12f;

    [SerializeField] private Renderer[] modelRenderers;


    // =========================================================
    // State
    // =========================================================

    public enum EnemyState
    {
        Idle,
        Chase,
        Exploded
    }

    [Header("Runtime State")]

    [SerializeField]
    private EnemyState currentState = EnemyState.Idle;

    [SerializeField]
    private float explosionTimer;

    [SerializeField]
    private float remainingTime;

    [SerializeField]
    private float currentMoveSpeed;

    [SerializeField]
    private float currentBlinkInterval;


    public EnemyState CurrentState => currentState;

    // 기존 외부 스크립트 호환용
    public bool IsStunned => false;


    // =========================================================
    // Runtime
    // =========================================================

    private PlayerStats targetPlayer;

    private Transform player;

    private bool hasExploded;

    private float blinkTimer;

    private bool warningActive;


    // =========================================================
    // Rolling Runtime
    // =========================================================

    private Vector3 previousPosition;

    private Quaternion originalModelRotation;

    private float rollingAngle;


    // =========================================================
    // Material Runtime
    // =========================================================

    private MaterialPropertyBlock[] originalBlocks;


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
            enemyStats =
                GetComponent<EnemyStats>();
        }

        if (agent == null)
        {
            agent =
                GetComponent<NavMeshAgent>();
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

        agent.updateRotation = false;
        agent.updatePosition = true;


        // 모델 초기 회전 저장

        originalModelRotation =
            rollingModel.localRotation;


        // Renderer 자동 탐색

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

        explosionTimer = 0f;
        remainingTime = explosionTime;

        currentMoveSpeed = 0f;
        currentBlinkInterval = startBlinkInterval;

        blinkTimer = 0f;
        rollingAngle = 0f;

        hasExploded = false;

        previousPosition =
            transform.position;

        if (rollingModel != null)
        {
            rollingModel.localRotation =
                originalModelRotation;
        }

        SetWarningColor(false);

        StopMoving();
    }


    // =========================================================
    // Update
    // =========================================================

    private void Update()
    {
        if (enemyStats == null ||
            enemyStats.IsDead ||
            hasExploded)
        {
            return;
        }


        switch (currentState)
        {
            case EnemyState.Idle:

                UpdateIdle();

                break;


            case EnemyState.Chase:

                UpdateChase();

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


    private bool HasValidPlayer()
    {
        return targetPlayer != null &&
               player != null &&
               !targetPlayer.IsDead;
    }


    // =========================================================
    // Idle
    // =========================================================

    private void UpdateIdle()
    {
        if (!CanUseAgent)
            return;


        if (!HasValidPlayer())
        {
            FindPlayer();
        }


        if (!HasValidPlayer())
            return;


        float distance =
            Vector3.Distance(
                transform.position,
                player.position
            );


        if (distance <= detectionRange)
        {
            StartChase();
        }
    }


    // =========================================================
    // Chase Start
    // =========================================================

    private void StartChase()
    {
        currentState =
            EnemyState.Chase;

        explosionTimer = 0f;

        remainingTime =
            explosionTime;

        blinkTimer = 0f;

        agent.ResetPath();

        agent.isStopped = false;


        // 자폭 경고 시작

        SetWarningColor(true);


        Debug.Log(
            "[RollingEnemy] 자폭 추격 시작"
        );
    }


    // =========================================================
    // Chase Update
    // =========================================================

    private void UpdateChase()
    {
        // =====================================================
        // 자폭 타이머
        // =====================================================

        explosionTimer += Time.deltaTime;

        remainingTime =
            Mathf.Max(
                0f,
                explosionTime - explosionTimer
            );


        // 제한 시간 종료 → 폭발

        if (explosionTimer >= explosionTime)
        {
            Explode();

            return;
        }


        // =====================================================
        // 자폭 진행률
        //
        // 시작 = 0
        // 자폭 직전 = 1
        // =====================================================

        float progress =
            Mathf.Clamp01(
                explosionTimer /
                Mathf.Max(0.1f, explosionTime)
            );


        // =====================================================
        // 시간이 지날수록 이동 속도 증가
        // =====================================================

        float speedMultiplier =
            Mathf.Lerp(
                startSpeedMultiplier,
                maxSpeedMultiplier,
                progress
            );


        currentMoveSpeed =
            enemyStats.MoveSpeed *
            Mathf.Max(0f, speedMultiplier);


        // =====================================================
        // 시간이 지날수록 붉은색 점멸 속도 증가
        // =====================================================

        UpdateWarningBlink(progress);


        // NavMesh에 올라와 있을 때만 이동

        if (!CanUseAgent)
            return;


        // =====================================================
        // 플레이어 방향으로 부드럽게 회전
        // =====================================================

        if (!HasValidPlayer())
        {
            FindPlayer();
        }


        if (HasValidPlayer())
        {
            Vector3 playerDirection =
                player.position -
                transform.position;

            playerDirection.y = 0f;


            RotateTowardsDirection(
                playerDirection
            );


            Debug.DrawRay(
                transform.position,
                playerDirection,
                Color.yellow
            );
        }


        // =====================================================
        // 바라보는 방향으로 무조건 전진
        //
        // 플레이어를 지나쳐도 멈추지 않음
        // =====================================================

        MoveForward(
            currentMoveSpeed
        );


        Debug.DrawRay(
            transform.position,
            GetForwardDirection() * 3f,
            Color.cyan
        );
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
    // Y축 회전
    // =========================================================

    private void RotateTowardsDirection(
        Vector3 targetDirection
    )
    {
        targetDirection.y = 0f;


        if (targetDirection.sqrMagnitude <= 0.0001f)
            return;


        targetDirection.Normalize();


        float angle =
            Vector3.SignedAngle(
                GetForwardDirection(),
                targetDirection,
                Vector3.up
            );


        float rotateAmount =
            Mathf.Clamp(
                angle,
                -rotationSpeed * Time.deltaTime,
                rotationSpeed * Time.deltaTime
            );


        transform.Rotate(
            Vector3.up,
            rotateAmount,
            Space.World
        );
    }


    // =========================================================
    // Forward Movement
    // =========================================================

    private void MoveForward(float speed)
    {
        if (!CanUseAgent)
            return;


        Vector3 direction =
            GetForwardDirection();


        float moveDistance =
            speed * Time.deltaTime;


        Vector3 startPosition =
            agent.nextPosition;


        Vector3 nextPosition =
            startPosition +
            direction * moveDistance;


        // =====================================================
        // NavMesh 경계 및 장애물 검사
        // =====================================================

        if (agent.Raycast(
                nextPosition,
                out NavMeshHit hit))
        {
            float availableDistance =
                Vector3.Dot(
                    hit.position - startPosition,
                    direction
                );


            moveDistance =
                Mathf.Clamp(
                    availableDistance - 0.03f,
                    0f,
                    moveDistance
                );
        }


        if (moveDistance <= 0.0001f)
            return;


        agent.isStopped = false;


        // 플레이어 위치가 아닌
        // 현재 바라보는 방향으로 계속 이동

        agent.Move(
            direction * moveDistance
        );
    }


    // =========================================================
    // Model X축 Rolling
    // =========================================================

    private void LateUpdate()
    {
        Vector3 movement =
            transform.position -
            previousPosition;


        previousPosition =
            transform.position;


        if (rollingModel == null ||
            currentState != EnemyState.Chase)
        {
            return;
        }


        movement.y = 0f;


        float signedDistance =
            Vector3.Dot(
                movement,
                GetForwardDirection()
            );


        if (Mathf.Abs(signedDistance) <= 0.00001f)
            return;


        float direction =
            invertRollingDirection
            ? -1f
            : 1f;


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


        // 기존에 수정했던 X축 회전 유지

        rollingModel.localRotation =
            originalModelRotation *
            Quaternion.AngleAxis(
                rollingAngle,
                Vector3.right
            );
    }


    // =========================================================
    // Warning Blink
    // =========================================================

    private void UpdateWarningBlink(float progress)
    {
        // 자폭 시간이 가까워질수록
        // 점멸 간격이 짧아짐

        currentBlinkInterval =
            Mathf.Max(
                0.01f,
                Mathf.Lerp(
                    startBlinkInterval,
                    finalBlinkInterval,
                    progress
                )
            );


        blinkTimer += Time.deltaTime;


        if (blinkTimer >= currentBlinkInterval)
        {
            int toggleCount =
                Mathf.FloorToInt(
                    blinkTimer / currentBlinkInterval
                );

            blinkTimer -=
                toggleCount * currentBlinkInterval;


            // 프레임이 느려져도 점멸 횟수 유지

            if (toggleCount % 2 != 0)
            {
                SetWarningColor(
                    !warningActive
                );
            }
        }
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
                // 원래 색상 상태 저장

                modelRenderer.GetPropertyBlock(
                    originalBlocks[i]
                );


                MaterialPropertyBlock warningBlock =
                    new MaterialPropertyBlock();


                modelRenderer.GetPropertyBlock(
                    warningBlock
                );


                // URP

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
                // 원래 Material 상태로 복구

                modelRenderer.SetPropertyBlock(
                    originalBlocks[i].isEmpty
                    ? null
                    : originalBlocks[i]
                );
            }
        }
    }


    // =========================================================
    // Self Destruction
    // =========================================================

    private void Explode()
    {
        if (hasExploded ||
            enemyStats == null ||
            enemyStats.IsDead)
        {
            return;
        }


        hasExploded = true;

        currentState = EnemyState.Exploded;

        currentMoveSpeed = 0f;

        remainingTime = 0f;


        StopMoving();

        SetWarningColor(false);


        // =====================================================
        // 공용 폭발 위치 및 범위
        // =====================================================

        Vector3 explosionCenter =
            transform.position;

        float radius =
            Mathf.Max(0.01f, explosionRadius);


        // =====================================================
        // 1. 폭발 이펙트 생성
        // =====================================================

        if (explosionPrefab != null)
        {
            ExplosionVFX explosion =
                Instantiate(
                    explosionPrefab,
                    explosionCenter,
                    Quaternion.identity
                );


            // RollingEnemyAI의 폭발 범위를
            // ExplosionVFX에 전달

            explosion.SetRadius(radius);
        }


        // =====================================================
        // 2. 실제 폭발 데미지 범위
        // =====================================================

        Collider[] hitColliders =
            Physics.OverlapSphere(
                explosionCenter,
                radius,
                playerMask,
                QueryTriggerInteraction.Ignore
            );


        HashSet<PlayerStats> damagedPlayers =
            new HashSet<PlayerStats>();


        foreach (Collider hit in hitColliders)
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


            // 중복 데미지 방지

            if (!damagedPlayers.Add(hitPlayer))
                continue;


            // 공용 EnemyStats의 공격력 사용

            hitPlayer.TakeDamage(
                enemyStats.AttackPower
            );


            Debug.Log(
                $"[RollingEnemy] 폭발 명중 / " +
                $"Damage : {enemyStats.AttackPower}"
            );
        }


        // =====================================================
        // 3. 자폭한 적 사망
        // =====================================================

        Debug.Log(
            $"[RollingEnemy] 자폭 완료 / " +
            $"Explosion Radius : {radius}"
        );


        enemyStats.TakeDamage(
            enemyStats.CurrentHealth
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
    // Disable
    // =========================================================

    private void OnDisable()
    {
        SetWarningColor(false);

        StopMoving();
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


        // 폭발 범위

        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(
            transform.position,
            explosionRadius
        );


        // 현재 바라보는 방향

        Gizmos.color = Color.cyan;

        Gizmos.DrawLine(
            transform.position,
            transform.position +
            GetForwardDirection() * 3f
        );
    }
}