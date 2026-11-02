using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class BossPatternAI : MonoBehaviour
{
    // =========================================================
    // References
    // =========================================================

    [Header("References")]

    [SerializeField]
    private BossStats bossStats;


    [SerializeField]
    private NavMeshAgent agent;


    [SerializeField]
    private Transform player;


    // =========================================================
    // Detection
    // =========================================================

    [Header("Detection")]

    [SerializeField]
    private float attackRange = 5f;


    [SerializeField]
    private float rotationSpeed = 8f;


    [SerializeField]
    private float sightHeight = 1.5f;


    [SerializeField]
    private LayerMask sightMask = ~0;


    // =========================================================
    // Attack
    // =========================================================

    [Header("Attack")]

    [Tooltip("붉은 공격 범위가 나온 뒤 실제 공격까지 걸리는 시간")]
    [SerializeField]
    private float attackWindUpTime = 0.8f;


    [Tooltip("공격 후 다음 공격까지 대기 시간")]
    [SerializeField]
    private float attackCooldown = 1.5f;


    [Tooltip("Boss 앞쪽 공격 중심 거리")]
    [SerializeField]
    private float attackForwardOffset = 2.5f;


    [Tooltip("공격 Box 실제 크기")]
    [SerializeField]
    private Vector3 attackBoxSize =
        new Vector3(
            4f,
            2f,
            5f
        );


    [SerializeField]
    private LayerMask playerMask = ~0;

    [Tooltip("공격 순간 앞으로 이동하는 거리")]
    [SerializeField]
    private float attackLungeDistance = 1.5f;


    [Tooltip("공격 전진에 걸리는 시간")]
    [SerializeField]
    private float attackLungeDuration = 0.18f;


    [Tooltip("공격 전진 속도 변화")]
    [SerializeField]
    private AnimationCurve attackLungeCurve =
        AnimationCurve.EaseInOut(
            0f,
            0f,
            1f,
            1f
        );


    // =========================================================
    // Telegraph
    // =========================================================

    [Header("Attack Warning")]

    [Tooltip("붉고 투명한 Cube Prefab")]
    [SerializeField]
    private GameObject attackWarningPrefab;

    [Tooltip("바닥에 표시되는 경고의 두께")]
    [SerializeField]
    private float warningHeight = 0.05f;


    [Tooltip("바닥과 겹쳐 깜빡이는 것을 방지하기 위한 높이")]
    [SerializeField]
    private float warningGroundOffset = 0.03f;


    [Tooltip("바닥을 찾기 위해 위에서 Ray를 시작하는 높이")]
    [SerializeField]
    private float groundSearchHeight = 5f;


    [Tooltip("바닥 탐색 최대 거리")]
    [SerializeField]
    private float groundSearchDistance = 20f;


    [Tooltip("바닥으로 사용할 Layer")]
    [SerializeField]
    private LayerMask groundMask = ~0;

    // =========================================================
    // Pattern Selection
    // =========================================================

    [Header("Pattern Selection")]

    [Tooltip("원형 공격을 사용할 확률. 0.5 = 50%")]
    [SerializeField, Range(0f, 1f)]
    private float circleAttackChance = 0.5f;


    // =========================================================
    // Circle Attack
    // =========================================================

    [Header("Circle Attack")]

    [Tooltip("원형 공격 최대 반경")]
    [SerializeField]
    private float circleAttackRadius = 5f;


    [Tooltip("원형 공격 전체 예고 시간")]
    [SerializeField]
    private float circleWarningTotalTime = 1.4f;


    [Tooltip("원이 최대 크기까지 커지는 시간")]
    [SerializeField]
    private float circleWarningGrowTime = 0.85f;


    [Tooltip("실제 공격 판정 높이")]
    [SerializeField]
    private float circleAttackHeight = 2f;


    [Tooltip("바닥 경고의 두께")]
    [SerializeField]
    private float circleWarningHeight = 0.04f;


    [Tooltip("바닥과 겹치지 않도록 살짝 띄우는 값")]
    [SerializeField]
    private float circleGroundOffset = 0.03f;


    [Tooltip("원형 공격 경고용 Cylinder Prefab")]
    [SerializeField]
    private GameObject circleWarningPrefab;

    [Header("Circle Dash")]

    [Tooltip("원형 공격 전에 플레이어 쪽으로 빠르게 이동")]
    [SerializeField]
    private bool useCircleDash = true;


    [Tooltip("한 번에 접근할 수 있는 최대 거리")]
    [SerializeField]
    private float circleDashMaxDistance = 6f;


    [Tooltip("플레이어와 이 정도 거리를 남겨두고 정지")]
    [SerializeField]
    private float circleDashStopDistance = 1.5f;


    [Tooltip("빠른 접근에 걸리는 시간")]
    [SerializeField]
    private float circleDashDuration = 0.15f;


    [Tooltip("접근 움직임 곡선")]
    [SerializeField]
    private AnimationCurve circleDashCurve =
        AnimationCurve.EaseInOut(
            0f,
            0f,
            1f,
            1f
        );

    // =========================================================
    // Runtime
    // =========================================================

    private PlayerStats playerStats;


    private bool battleActive;

    private bool isAttacking;


    private Coroutine attackRoutine;

    private GameObject currentWarning;


    // =========================================================
    // Awake
    // =========================================================

    private void Awake()
    {
        if (bossStats == null)
        {
            bossStats =
                GetComponent<BossStats>();
        }


        if (agent == null)
        {
            agent =
                GetComponent<NavMeshAgent>();
        }
    }


    // =========================================================
    // Start
    // =========================================================

    private void Start()
    {
        if (agent != null &&
            bossStats != null)
        {
            agent.speed =
                bossStats.MoveSpeed;


            agent.updateRotation =
                false;
        }


        FindPlayer();
    }


    // =========================================================
    // Events
    // =========================================================

    private void OnEnable()
    {
        if (bossStats == null)
        {
            bossStats =
                GetComponent<BossStats>();
        }


        if (bossStats != null)
        {
            bossStats.OnGroggyStarted +=
                HandleGroggyStarted;


            bossStats.OnDied +=
                HandleBossDeath;
        }
    }


    private void OnDisable()
    {
        if (bossStats != null)
        {
            bossStats.OnGroggyStarted -=
                HandleGroggyStarted;


            bossStats.OnDied -=
                HandleBossDeath;
        }
    }


    // =========================================================
    // Battle
    // =========================================================

    public void SetBattleActive(
        bool active
    )
    {
        battleActive =
            active;


        if (!active)
        {
            CancelCurrentAttack();

            StopMoving();
        }
    }


    public void SetTarget(
        Transform target
    )
    {
        player =
            target;


        if (player != null)
        {
            playerStats =
                player.GetComponentInParent<PlayerStats>();
        }
    }


    // =========================================================
    // Update
    // =========================================================

    private void Update()
    {
        if (!battleActive)
            return;


        if (bossStats == null ||
            bossStats.IsDead)
        {
            return;
        }


        // ==========================================
        // Groggy 중
        // ==========================================

        if (bossStats.IsGroggy)
        {
            StopMoving();

            return;
        }


        // ==========================================
        // Player 확인
        // ==========================================

        if (player == null ||
            playerStats == null ||
            playerStats.IsDead)
        {
            FindPlayer();


            if (player == null)
            {
                StopMoving();

                return;
            }
        }


        if (isAttacking)
        {
            return;
        }


        float distance =
            Vector3.Distance(
                transform.position,
                player.position
            );


        // ==========================================
        // 공격 범위 밖
        // ==========================================

        if (distance >
            attackRange)
        {
            ChasePlayer();

            return;
        }


        // ==========================================
        // 사거리 안이지만 벽 뒤
        // ==========================================

        if (!CanSeePlayer())
        {
            ChasePlayer();

            return;
        }


        // ==========================================
        // 공격 준비
        // ==========================================

        StopMoving();

        RotateTowardsPlayer();


        StartBossAttack();
    }


    // =========================================================
    // Player Search
    // =========================================================

    private void FindPlayer()
    {
        PlayerStats foundPlayer =
            FindFirstObjectByType<PlayerStats>();


        if (foundPlayer == null)
        {
            player =
                null;

            playerStats =
                null;

            return;
        }


        playerStats =
            foundPlayer;


        player =
            foundPlayer.transform;
    }


    // =========================================================
    // Chase
    // =========================================================

    private void ChasePlayer()
    {
        if (agent == null ||
            player == null)
        {
            return;
        }


        if (!agent.isOnNavMesh)
            return;


        agent.speed =
            bossStats.MoveSpeed;


        agent.isStopped =
            false;


        agent.SetDestination(
            player.position
        );


        Vector3 moveDirection =
            agent.desiredVelocity;


        moveDirection.y =
            0f;


        if (moveDirection.sqrMagnitude >
            0.01f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(
                    moveDirection
                );


            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed *
                    Time.deltaTime
                );
        }
    }


    // =========================================================
    // Stop
    // =========================================================

    private void StopMoving()
    {
        if (agent == null ||
            !agent.isOnNavMesh)
        {
            return;
        }


        agent.isStopped =
            true;


        agent.ResetPath();
    }


    // =========================================================
    // Rotate
    // =========================================================

    private void RotateTowardsPlayer()
    {
        if (player == null)
            return;


        Vector3 direction =
            player.position -
            transform.position;


        direction.y =
            0f;


        if (direction.sqrMagnitude <
            0.001f)
        {
            return;
        }


        Quaternion targetRotation =
            Quaternion.LookRotation(
                direction
            );


        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed *
                Time.deltaTime
            );
    }


    // =========================================================
    // Ray
    // =========================================================

    private bool CanSeePlayer()
    {
        if (player == null)
            return false;


        Vector3 origin =
            transform.position +
            Vector3.up *
            sightHeight;


        Collider playerCollider =
            player.GetComponentInChildren<Collider>();


        Vector3 targetPosition;


        if (playerCollider != null)
        {
            targetPosition =
                playerCollider.bounds.center;
        }
        else
        {
            targetPosition =
                player.position +
                Vector3.up;
        }


        Vector3 direction =
            targetPosition -
            origin;


        float distance =
            direction.magnitude;


        if (distance <= 0.01f)
            return true;


        direction.Normalize();


        RaycastHit[] hits =
            Physics.RaycastAll(
                origin,
                direction,
                distance + 0.5f,
                sightMask,
                QueryTriggerInteraction.Ignore
            );


        System.Array.Sort(
            hits,
            (a, b) =>
                a.distance.CompareTo(
                    b.distance
                )
        );


        foreach (RaycastHit hit in hits)
        {
            // Boss 자기 자신의 Collider 무시
            if (hit.transform == transform ||
                hit.transform.IsChildOf(
                    transform
                ))
            {
                continue;
            }


            PlayerStats hitPlayer =
                hit.collider
                    .GetComponentInParent<PlayerStats>();


            if (hitPlayer ==
                playerStats)
            {
                Debug.DrawLine(
                    origin,
                    hit.point,
                    Color.green
                );


                return true;
            }


            Debug.DrawLine(
                origin,
                hit.point,
                Color.red
            );


            return false;
        }


        return false;
    }


    // =========================================================
    // Attack Start
    // =========================================================

    private void StartBossAttack()
    {
        if (isAttacking)
            return;


        // ==========================================
        // 패턴 선택
        // ==========================================

        bool useCircleAttack =
            Random.value <
            circleAttackChance;


        if (useCircleAttack)
        {
            attackRoutine =
                StartCoroutine(
                    CircleAttackRoutine()
                );
        }
        else
        {
            attackRoutine =
                StartCoroutine(
                    AttackRoutine()
                );
        }
    }


    // =========================================================
    // Attack Pattern
    // =========================================================

    private IEnumerator AttackRoutine()
    {
        isAttacking =
            true;


        StopMoving();


        // ==========================================
        // 공격 방향 고정
        // ==========================================

        Vector3 attackDirection =
            player.position -
            transform.position;


        attackDirection.y =
            0f;


        if (attackDirection.sqrMagnitude <
            0.001f)
        {
            attackDirection =
                transform.forward;
        }
        else
        {
            attackDirection.Normalize();
        }


        Quaternion attackRotation =
            Quaternion.LookRotation(
                attackDirection
            );


        transform.rotation =
            attackRotation;


        // ==========================================
        // Boss 앞쪽의 공격 예정 X/Z 위치
        // ==========================================

        Vector3 attackBasePosition =
            transform.position +
            attackDirection *
            attackForwardOffset;


        // ==========================================
        // 실제 바닥 위치 검색
        // ==========================================

        Vector3 groundPosition =
            FindGroundPosition(
                attackBasePosition
            );


        // ==========================================
        // 실제 공격 판정 중심
        //
        // OverlapBox는 중심점을 기준으로 하기 때문에
        // 공격 높이 절반만큼 위로 올림
        // ==========================================

        Vector3 attackCenter =
            groundPosition +
            Vector3.up *
            (
                attackBoxSize.y *
                0.5f
            );


        // ==========================================
        // 바닥 공격 Warning
        // ==========================================

        if (attackWarningPrefab != null)
        {
            // ======================================
            // Warning은 실제 공격 Box와 달리
            // 바닥 바로 위에 위치
            // ======================================

            Vector3 warningPosition =
                groundPosition +
                Vector3.up *
                (
                    warningGroundOffset +
                    warningHeight *
                    0.5f
                );


            currentWarning =
                Instantiate(
                    attackWarningPrefab,
                    warningPosition,
                    attackRotation
                );


            // ======================================
            // X/Z는 실제 공격 범위와 동일
            // Y는 아주 얇게
            // ======================================

            currentWarning.transform.localScale =
                new Vector3(
                    attackBoxSize.x,
                    warningHeight,
                    attackBoxSize.z
                );


            // Warning은 시각 표시일 뿐
            // 공격 Collider가 아님
            Collider[] warningColliders =
                currentWarning
                    .GetComponentsInChildren<Collider>();


            foreach (
                Collider warningCollider
                in warningColliders)
            {
                warningCollider.enabled =
                    false;
            }
        }


        // ==========================================
        // 공격 예고 시간
        // ==========================================

        yield return new WaitForSeconds(
            attackWindUpTime
        );


        // 예고 중 Groggy/사망 가능
        if (!battleActive ||
            bossStats.IsDead ||
            bossStats.IsGroggy)
        {
            CleanupWarning();


            isAttacking =
                false;


            attackRoutine =
                null;


            yield break;
        }


        // ==========================================
        // 실제 공격 순간 앞으로 짧게 전진
        // ==========================================

        yield return StartCoroutine(
            AttackLungeRoutine(
                attackDirection
            )
        );


        // 돌진 도중 Groggy / 사망 가능
        if (!battleActive ||
            bossStats.IsDead ||
            bossStats.IsGroggy)
        {
            CleanupWarning();


            isAttacking =
                false;


            attackRoutine =
                null;


            yield break;
        }


        // ==========================================
        // 실제 Box 공격
        // ==========================================

        Collider[] hits =
            Physics.OverlapBox(
                attackCenter,
                attackBoxSize * 0.5f,
                attackRotation,
                playerMask,
                QueryTriggerInteraction.Ignore
            );


        HashSet<PlayerStats> damagedPlayers =
            new HashSet<PlayerStats>();


        foreach (Collider hit in hits)
        {
            PlayerStats hitPlayer =
                hit.GetComponentInParent<PlayerStats>();


            if (hitPlayer == null ||
                hitPlayer.IsDead)
            {
                continue;
            }


            if (!damagedPlayers.Add(
                    hitPlayer))
            {
                continue;
            }


            hitPlayer.TakeDamage(
                bossStats.AttackPower
            );


            Debug.Log(
                $"[Boss] Attack Hit / " +
                $"Damage = {bossStats.AttackPower}"
            );
        }


        CleanupWarning();


        // ==========================================
        // 공격 후딜
        // ==========================================

        yield return new WaitForSeconds(
            attackCooldown
        );


        isAttacking =
            false;


        attackRoutine =
            null;
    }


    // =========================================================
    // Groggy
    // =========================================================

    private void HandleGroggyStarted()
    {
        CancelCurrentAttack();

        StopMoving();


        Debug.Log(
            "[BossAI] Groggy 때문에 패턴 중단"
        );
    }


    // =========================================================
    // Death
    // =========================================================

    private void HandleBossDeath()
    {
        SetBattleActive(
            false
        );
    }


    // =========================================================
    // Cancel
    // =========================================================

    private void CancelCurrentAttack()
    {
        if (attackRoutine != null)
        {
            StopCoroutine(
                attackRoutine
            );


            attackRoutine =
                null;
        }


        CleanupWarning();


        isAttacking =
            false;
    }


    private void CleanupWarning()
    {
        if (currentWarning != null)
        {
            Destroy(
                currentWarning
            );


            currentWarning =
                null;
        }
    }


    // =========================================================
    // Gizmos
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(
            transform.position,
            attackRange
        );
    }

    // =========================================================
    // 공격 위치의 실제 바닥 찾기
    // =========================================================

    private Vector3 FindGroundPosition(
        Vector3 position
    )
    {
        Vector3 rayOrigin =
            position +
            Vector3.up *
            groundSearchHeight;


        RaycastHit[] hits =
            Physics.RaycastAll(
                rayOrigin,
                Vector3.down,
                groundSearchDistance,
                groundMask,
                QueryTriggerInteraction.Ignore
            );


        // 가까운 것부터 검사
        System.Array.Sort(
            hits,
            (a, b) =>
                a.distance.CompareTo(
                    b.distance
                )
        );


        foreach (RaycastHit hit in hits)
        {
            // ==========================================
            // Boss 자신의 Collider는 바닥으로 인정하지 않음
            // ==========================================

            if (hit.transform == transform ||
                hit.transform.IsChildOf(transform))
            {
                continue;
            }


            return hit.point;
        }


        // 바닥을 못 찾으면 기존 Y 사용
        return position;
    }

    // =========================================================
    // 실제 공격 순간 짧게 전진
    // =========================================================

    private IEnumerator AttackLungeRoutine(
        Vector3 attackDirection
    )
    {
        if (attackLungeDistance <= 0f ||
            attackLungeDuration <= 0f)
        {
            yield break;
        }


        attackDirection.y =
            0f;


        if (attackDirection.sqrMagnitude <
            0.001f)
        {
            yield break;
        }


        attackDirection.Normalize();


        float elapsed =
            0f;


        float previousProgress =
            0f;


        while (elapsed <
               attackLungeDuration)
        {
            // Groggy나 사망 시 돌진 즉시 중단
            if (!battleActive ||
                bossStats == null ||
                bossStats.IsDead ||
                bossStats.IsGroggy)
            {
                yield break;
            }


            elapsed +=
                Time.deltaTime;


            float normalizedTime =
                Mathf.Clamp01(
                    elapsed /
                    attackLungeDuration
                );


            float currentProgress =
                attackLungeCurve.Evaluate(
                    normalizedTime
                );


            float progressDelta =
                currentProgress -
                previousProgress;


            float moveDistance =
                attackLungeDistance *
                progressDelta;


            Vector3 movement =
                attackDirection *
                moveDistance;


            // ==========================================
            // NavMeshAgent가 정상적으로 NavMesh 위에 있음
            // ==========================================

            if (agent != null &&
                agent.isOnNavMesh)
            {
                agent.Move(
                    movement
                );
            }

            // NavMeshAgent가 없는 테스트 상황
            else
            {
                transform.position +=
                    movement;
            }


            previousProgress =
                currentProgress;


            yield return null;
        }
    }

    // =========================================================
    // Circle Attack Pattern
    // =========================================================

    private IEnumerator CircleAttackRoutine()
    {
        isAttacking =
            true;


        StopMoving();


        // =====================================================
        // 1. Player 방향 바라보기
        // =====================================================

        RotateTowardsPlayer();


        // =====================================================
        // 2. 공격 전에 Player 쪽으로 빠르게 접근
        // =====================================================

        yield return StartCoroutine(
            CircleDashToPlayerRoutine()
        );


        // =====================================================
        // Dash 도중 Groggy / 사망 가능
        // =====================================================

        if (!battleActive ||
            bossStats == null ||
            bossStats.IsDead ||
            bossStats.IsGroggy)
        {
            CleanupWarning();


            isAttacking =
                false;


            attackRoutine =
                null;


            yield break;
        }


        // =====================================================
        // 3. Dash가 끝난 위치를 공격 중심으로 확정
        // =====================================================

        Vector3 circleCenter =
            transform.position;


        Vector3 groundPosition =
            FindGroundPosition(
                circleCenter
            );


        // =====================================================
        // 여기부터 기존 Warning 생성 코드
        // =====================================================

        if (circleWarningPrefab != null)
        {
            Vector3 warningPosition =
                groundPosition +
                Vector3.up *
                (
                    circleGroundOffset +
                    circleWarningHeight *
                    0.5f
                );


            currentWarning =
                Instantiate(
                    circleWarningPrefab,
                    warningPosition,
                    Quaternion.identity
                );


            Collider[] warningColliders =
                currentWarning
                    .GetComponentsInChildren<Collider>();


            foreach (Collider col in warningColliders)
            {
                col.enabled =
                    false;
            }
        }


        // ==========================================
        // Warning 생성
        // ==========================================

        if (circleWarningPrefab != null)
        {
            Vector3 warningPosition =
                groundPosition +
                Vector3.up *
                (
                    circleGroundOffset +
                    circleWarningHeight *
                    0.5f
                );


            currentWarning =
                Instantiate(
                    circleWarningPrefab,
                    warningPosition,
                    Quaternion.identity
                );


            // 혹시 Collider가 있으면 비활성화
            Collider[] warningColliders =
                currentWarning
                    .GetComponentsInChildren<Collider>();


            foreach (Collider col in warningColliders)
            {
                col.enabled =
                    false;
            }
        }


        // ==========================================
        // 성장 시간
        // ==========================================

        float growTime =
            Mathf.Clamp(
                circleWarningGrowTime,
                0.01f,
                circleWarningTotalTime
            );


        float elapsed =
            0f;


        // ==========================================
        // 0 → 최대 크기
        // ==========================================

        while (elapsed <
               growTime)
        {
            // Groggy / 사망 / 전투 종료
            if (!battleActive ||
                bossStats.IsDead ||
                bossStats.IsGroggy)
            {
                CleanupWarning();


                isAttacking =
                    false;


                attackRoutine =
                    null;


                yield break;
            }


            elapsed +=
                Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    elapsed /
                    growTime
                );


            // 조금 더 자연스럽게 커지게
            float smoothT =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );


            float diameter =
                Mathf.Lerp(
                    0.05f,
                    circleAttackRadius * 2f,
                    smoothT
                );


            if (currentWarning != null)
            {
                // Unity 기본 Cylinder 기준
                // X/Z = 지름
                // Y = 매우 얇게
                currentWarning.transform.localScale =
                    new Vector3(
                        diameter,
                        circleWarningHeight * 0.5f,
                        diameter
                    );
            }


            yield return null;
        }


        // ==========================================
        // 정확히 최대 크기로 고정
        // ==========================================

        if (currentWarning != null)
        {
            float maxDiameter =
                circleAttackRadius *
                2f;


            currentWarning.transform.localScale =
                new Vector3(
                    maxDiameter,
                    circleWarningHeight * 0.5f,
                    maxDiameter
                );
        }


        // ==========================================
        // 남은 예고 시간
        //
        // 예:
        // Total = 1.4
        // Grow  = 0.85
        //
        // Wait = 0.55
        // ==========================================

        float remainingWarningTime =
            Mathf.Max(
                0f,
                circleWarningTotalTime -
                growTime
            );


        if (remainingWarningTime > 0f)
        {
            yield return new WaitForSeconds(
                remainingWarningTime
            );
        }


        // ==========================================
        // 공격 직전 상태 재확인
        // ==========================================

        if (!battleActive ||
            bossStats.IsDead ||
            bossStats.IsGroggy)
        {
            CleanupWarning();


            isAttacking =
                false;


            attackRoutine =
                null;


            yield break;
        }


        // ==========================================
        // 실제 원형 공격
        // ==========================================

        PerformCircleAttack(
            groundPosition
        );


        // ==========================================
        // Warning 제거
        // ==========================================

        CleanupWarning();


        // ==========================================
        // 후딜
        // ==========================================

        yield return new WaitForSeconds(
            attackCooldown
        );


        isAttacking =
            false;


        attackRoutine =
            null;
    }

    // =========================================================
    // Circle Damage
    // =========================================================

    private void PerformCircleAttack(
        Vector3 groundPosition
    )
    {
        Vector3 overlapCenter =
            groundPosition +
            Vector3.up *
            (
                circleAttackHeight *
                0.5f
            );


        // 우선 주변 대상 검색
        Collider[] hits =
            Physics.OverlapSphere(
                overlapCenter,
                circleAttackRadius,
                playerMask,
                QueryTriggerInteraction.Ignore
            );


        HashSet<PlayerStats> damagedPlayers =
            new HashSet<PlayerStats>();


        foreach (Collider hit in hits)
        {
            PlayerStats hitPlayer =
                hit.GetComponentInParent<PlayerStats>();


            if (hitPlayer == null ||
                hitPlayer.IsDead)
            {
                continue;
            }


            if (!damagedPlayers.Add(
                    hitPlayer))
            {
                continue;
            }


            // ==========================================
            // 중요한 부분:
            // 구형 범위가 아니라 바닥 기준 원형 거리 확인
            // ==========================================

            Vector3 playerOffset =
                hitPlayer.transform.position -
                groundPosition;


            playerOffset.y =
                0f;


            float horizontalDistanceSqr =
                playerOffset.sqrMagnitude;


            if (horizontalDistanceSqr >
                circleAttackRadius *
                circleAttackRadius)
            {
                continue;
            }


            // ==========================================
            // Damage
            // ==========================================

            hitPlayer.TakeDamage(
                bossStats.AttackPower
            );


            Debug.Log(
                $"[Boss] Circle Attack Hit / " +
                $"Damage = {bossStats.AttackPower}"
            );
        }
    }

    // =========================================================
    // Circle Attack - Player 쪽으로 빠르게 접근
    // =========================================================

    private IEnumerator CircleDashToPlayerRoutine()
    {
        if (!useCircleDash)
            yield break;


        if (player == null)
            yield break;


        if (circleDashDuration <= 0f)
            yield break;


        StopMoving();


        // =====================================================
        // Dash 시작 순간의 Player 위치를 기준으로 함
        //
        // Dash 도중 Player를 계속 추적하지 않음.
        // =====================================================

        Vector3 targetPosition =
            player.position;


        Vector3 direction =
            targetPosition -
            transform.position;


        direction.y =
            0f;


        float distanceToPlayer =
            direction.magnitude;


        if (distanceToPlayer <=
            circleDashStopDistance)
        {
            yield break;
        }


        if (direction.sqrMagnitude <
            0.001f)
        {
            yield break;
        }


        direction.Normalize();


        // =====================================================
        // Player를 뚫고 지나가지 않도록
        // StopDistance만큼 남김
        // =====================================================

        float availableDistance =
            distanceToPlayer -
            circleDashStopDistance;


        float dashDistance =
            Mathf.Min(
                availableDistance,
                circleDashMaxDistance
            );


        // =====================================================
        // Dash 방향 바라보기
        // =====================================================

        transform.rotation =
            Quaternion.LookRotation(
                direction
            );


        float elapsed =
            0f;


        float previousProgress =
            0f;


        // =====================================================
        // 실제 Dash
        // =====================================================

        while (elapsed <
               circleDashDuration)
        {
            // 전투 종료 / 사망 / Groggy면 중단
            if (!battleActive ||
                bossStats == null ||
                bossStats.IsDead ||
                bossStats.IsGroggy)
            {
                yield break;
            }


            elapsed +=
                Time.deltaTime;


            float normalizedTime =
                Mathf.Clamp01(
                    elapsed /
                    circleDashDuration
                );


            float currentProgress =
                circleDashCurve.Evaluate(
                    normalizedTime
                );


            float progressDelta =
                currentProgress -
                previousProgress;


            Vector3 movement =
                direction *
                dashDistance *
                progressDelta;


            // ==========================================
            // NavMesh를 유지하면서 이동
            // ==========================================

            if (agent != null &&
                agent.isOnNavMesh)
            {
                agent.Move(
                    movement
                );
            }
            else
            {
                transform.position +=
                    movement;
            }


            previousProgress =
                currentProgress;


            yield return null;
        }
    }
}