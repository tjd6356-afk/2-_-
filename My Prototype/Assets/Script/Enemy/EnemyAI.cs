using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    // =========================================================
    // References
    // =========================================================

    [Header("References")]

    [SerializeField]
    private EnemyStats enemyStats;


    [SerializeField]
    private EnemyShooter enemyShooter;


    [SerializeField]
    private NavMeshAgent agent;


    // =========================================================
    // Detection
    // =========================================================

    [Header("Detection")]

    [Tooltip("Player / NPC를 발견하는 최대 거리")]
    [SerializeField]
    private float detectionRange = 20f;


    [Tooltip("이 거리까지 접근하면 멈추고 공격")]
    [SerializeField]
    private float attackRange = 10f;


    [Tooltip("주변 타겟을 다시 검색하는 주기")]
    [SerializeField]
    private float targetScanInterval = 0.25f;


    [Tooltip("탐색할 Layer. 처음에는 Everything 권장")]
    [SerializeField]
    private LayerMask targetMask = ~0;


    // =========================================================
    // Aim
    // =========================================================

    [Header("Aim")]

    [SerializeField]
    private float targetHeight = 1.2f;


    [SerializeField]
    private float rotationSpeed = 8f;


    // =========================================================
    // Line Of Sight
    // =========================================================

    [Header("Line Of Sight")]

    [SerializeField]
    private LayerMask sightMask = ~0;


    // =========================================================
    // Runtime Target
    // =========================================================

    private Transform currentTarget;


    private PlayerStats targetPlayer;

    private NPCStats targetNPC;


    private float nextTargetScanTime;

    private bool isStunned;

    private Coroutine stunRoutine;

    private Transform playerTransform;


    public bool IsStunned =>
        isStunned;

    public Transform CurrentTarget =>
        currentTarget;

    // =========================================================
    // Hit Reaction
    // =========================================================

    [Header("Hit Reaction")]

    [Tooltip("공격을 맞았을 때 행동 불가 시간")]
    [SerializeField]
    private float stunDuration = 0.3f;


    [Tooltip("피격 시 뒤로 밀리는 거리")]
    [SerializeField]
    private float knockbackDistance = 0.5f;


    [Tooltip("뒤로 밀려나는 데 걸리는 시간")]
    [SerializeField]
    private float knockbackDuration = 0.12f;


    [Tooltip("넉백 움직임 곡선")]
    [SerializeField]
    private AnimationCurve knockbackCurve =
        AnimationCurve.EaseInOut(
            0f,
            0f,
            1f,
            1f
        );


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


        if (enemyShooter == null)
        {
            enemyShooter =
                GetComponent<EnemyShooter>();
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
            enemyStats != null)
        {
            agent.speed =
                enemyStats.MoveSpeed;


            // 회전은 직접 처리
            agent.updateRotation =
                false;
        }


        // 게임 시작 직후 한 번 탐색
        FindNearestTarget();

        PlayerStats foundPlayer =
        FindFirstObjectByType<PlayerStats>();


        if (foundPlayer != null)
        {
            playerTransform =
                foundPlayer.transform;
        }
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

            return;
        }

        if (isStunned)
        {
            return;
        }

        // =====================================================
        // 일정 주기로 주변 타겟 검색
        // =====================================================

        if (Time.time >=
            nextTargetScanTime)
        {
            nextTargetScanTime =
                Time.time +
                targetScanInterval;


            FindNearestTarget();
        }


        // =====================================================
        // 현재 타겟 없음
        // =====================================================

        if (currentTarget == null)
        {
            StopMoving();

            return;
        }


        // =====================================================
        // 현재 타겟 사망 확인
        // =====================================================

        if (!IsCurrentTargetAlive())
        {
            ClearTarget();

            FindNearestTarget();

            StopMoving();

            return;
        }


        float distance =
            Vector3.Distance(
                transform.position,
                currentTarget.position
            );


        // =====================================================
        // 타겟이 Detection Range 밖으로 나감
        // =====================================================

        if (distance >
            detectionRange)
        {
            ClearTarget();

            StopMoving();

            return;
        }


        // =====================================================
        // 공격 범위 밖
        // → 추적
        // =====================================================

        if (distance >
            attackRange)
        {
            ChaseTarget();

            return;
        }


        // =====================================================
        // 공격 범위 안
        // =====================================================

        StopMoving();


        RotateTowardsTarget();


        if (CanSeeTarget())
        {
            Vector3 targetPosition =
                GetTargetAimPosition();


            if (enemyShooter != null)
            {
                enemyShooter.TryShoot(
                    targetPosition
                );
            }
        }
    }


    // =========================================================
    // 가장 가까운 Player / NPC 탐색
    // =========================================================

    private void FindNearestTarget()
    {
        Collider[] hits =
            Physics.OverlapSphere(
                transform.position,
                detectionRange,
                targetMask,
                QueryTriggerInteraction.Ignore
            );


        Transform bestTarget =
            null;


        PlayerStats bestPlayer =
            null;


        NPCStats bestNPC =
            null;


        float bestDistanceSqr =
            float.MaxValue;


        // 같은 캐릭터에 Collider가 여러 개 있을 수 있으므로
        // 중복 검사 방지
        HashSet<Transform> checkedTargets =
            new HashSet<Transform>();


        foreach (Collider hit in hits)
        {
            if (hit == null)
                continue;


            // =================================================
            // Player 탐색
            // =================================================

            PlayerStats player =
                hit.GetComponentInParent<PlayerStats>();


            if (player != null &&
                !player.IsDead)
            {
                Transform playerTransform =
                    player.transform;


                if (checkedTargets.Add(
                        playerTransform))
                {
                    float distanceSqr =
                        (
                            playerTransform.position -
                            transform.position
                        ).sqrMagnitude;


                    if (distanceSqr <
                        bestDistanceSqr)
                    {
                        bestDistanceSqr =
                            distanceSqr;


                        bestTarget =
                            playerTransform;


                        bestPlayer =
                            player;


                        bestNPC =
                            null;
                    }
                }
            }


            // =================================================
            // NPC 탐색
            // =================================================

            NPCStats npc =
                hit.GetComponentInParent<NPCStats>();


            if (npc != null &&
                !npc.IsDead)
            {
                Transform npcTransform =
                    npc.transform;


                if (checkedTargets.Add(
                        npcTransform))
                {
                    float distanceSqr =
                        (
                            npcTransform.position -
                            transform.position
                        ).sqrMagnitude;


                    if (distanceSqr <
                        bestDistanceSqr)
                    {
                        bestDistanceSqr =
                            distanceSqr;


                        bestTarget =
                            npcTransform;


                        bestNPC =
                            npc;


                        bestPlayer =
                            null;
                    }
                }
            }
        }


        // =====================================================
        // 가장 가까운 대상 적용
        // =====================================================

        currentTarget =
            bestTarget;


        targetPlayer =
            bestPlayer;


        targetNPC =
            bestNPC;
    }


    // =========================================================
    // 현재 Target 생존 여부
    // =========================================================

    private bool IsCurrentTargetAlive()
    {
        if (currentTarget == null)
            return false;


        if (targetPlayer != null)
        {
            return !targetPlayer.IsDead;
        }


        if (targetNPC != null)
        {
            return !targetNPC.IsDead;
        }


        return false;
    }


    // =========================================================
    // Target 제거
    // =========================================================

    private void ClearTarget()
    {
        currentTarget =
            null;


        targetPlayer =
            null;


        targetNPC =
            null;
    }


    // =========================================================
    // Target 추적
    // =========================================================

    private void ChaseTarget()
    {
        if (agent == null ||
            currentTarget == null)
        {
            return;
        }


        // Runtime NavMesh가 아직 없으면
        // SetDestination 호출하지 않음
        if (!agent.isOnNavMesh)
        {
            return;
        }


        agent.isStopped =
            false;


        agent.speed =
            enemyStats.MoveSpeed;


        agent.SetDestination(
            currentTarget.position
        );


        // =====================================================
        // 이동 방향으로 회전
        // =====================================================

        Vector3 direction =
            agent.desiredVelocity;


        direction.y =
            0f;


        if (direction.sqrMagnitude >
            0.01f)
        {
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
    }


    // =========================================================
    // 정지
    // =========================================================

    private void StopMoving()
    {
        if (agent == null)
            return;


        if (!agent.isOnNavMesh)
            return;


        agent.isStopped =
            true;


        agent.ResetPath();
    }


    // =========================================================
    // Target 바라보기
    // =========================================================

    private void RotateTowardsTarget()
    {
        if (currentTarget == null)
            return;


        Vector3 direction =
            currentTarget.position -
            transform.position;


        // 위아래로 몸이 기울어지지 않음
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
    // 조준 위치
    // =========================================================

    private Vector3 GetTargetAimPosition()
    {
        if (currentTarget == null)
        {
            return transform.position;
        }


        Collider targetCollider =
            currentTarget.GetComponentInChildren<Collider>();


        if (targetCollider != null)
        {
            return targetCollider.bounds.center;
        }


        return currentTarget.position +
               Vector3.up *
               targetHeight;
    }


    // =========================================================
    // Target 시야 확인
    // =========================================================

    private bool CanSeeTarget()
    {
        if (currentTarget == null)
            return false;


        // =====================================================
        // Ray 시작점
        // =====================================================

        Vector3 origin =
            transform.position +
            Vector3.up *
            0.5f;


        // =====================================================
        // Target 중심
        // =====================================================

        Vector3 target =
            GetTargetAimPosition();


        Vector3 direction =
            target -
            origin;


        float distance =
            direction.magnitude;


        if (distance <=
            0.01f)
        {
            return true;
        }


        direction.Normalize();


        // =====================================================
        // Raycast
        // =====================================================

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
            Transform hitTransform =
                hit.collider.transform;


            // =================================================
            // Enemy 자기 자신 무시
            // =================================================

            if (hitTransform == transform ||
                hitTransform.IsChildOf(transform))
            {
                continue;
            }


            // =================================================
            // 현재 Player Target인지 확인
            // =================================================

            if (targetPlayer != null)
            {
                PlayerStats hitPlayer =
                    hit.collider
                        .GetComponentInParent<PlayerStats>();


                if (hitPlayer ==
                    targetPlayer)
                {
                    Debug.DrawLine(
                        origin,
                        hit.point,
                        Color.green
                    );


                    return true;
                }
            }


            // =================================================
            // 현재 NPC Target인지 확인
            // =================================================

            if (targetNPC != null)
            {
                NPCStats hitNPC =
                    hit.collider
                        .GetComponentInParent<NPCStats>();


                if (hitNPC ==
                    targetNPC)
                {
                    Debug.DrawLine(
                        origin,
                        hit.point,
                        Color.green
                    );


                    return true;
                }
            }


            // =================================================
            // 목표 전에 벽 또는 다른 물체가 있음
            // =================================================

            Debug.DrawLine(
                origin,
                hit.point,
                Color.red
            );


            return false;
        }


        // 아무것도 안 맞음
        Debug.DrawLine(
            origin,
            target,
            Color.yellow
        );


        return false;
    }


    // =========================================================
    // Scene Debug
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        // Detection
        Gizmos.DrawWireSphere(
            transform.position,
            detectionRange
        );


        // Attack
        Gizmos.DrawWireSphere(
            transform.position,
            attackRange
        );


        // 현재 선택된 Target
        if (currentTarget != null)
        {
            Gizmos.DrawLine(
                transform.position,
                currentTarget.position
            );
        }
    }

    private void OnEnable()
    {
        if (enemyStats == null)
        {
            enemyStats =
                GetComponent<EnemyStats>();
        }


        if (enemyStats != null)
        {
            enemyStats.OnDamaged +=
                HandleDamaged;
        }
    }


    private void OnDisable()
    {
        if (enemyStats != null)
        {
            enemyStats.OnDamaged -=
                HandleDamaged;
        }
    }

    // =========================================================
    // 피격
    // =========================================================

    private void HandleDamaged(
        float damage
    )
    {
        if (enemyStats == null ||
            enemyStats.IsDead)
        {
            return;
        }


        // Runtime 생성 Player라서 아직 못 찾았다면 재탐색
        if (playerTransform == null)
        {
            PlayerStats foundPlayer =
                FindFirstObjectByType<PlayerStats>();


            if (foundPlayer != null)
            {
                playerTransform =
                    foundPlayer.transform;
            }
        }


        // 이미 경직 중에 또 맞으면
        // 기존 경직을 끊고 처음부터 다시 경직
        if (stunRoutine != null)
        {
            StopCoroutine(
                stunRoutine
            );
        }


        stunRoutine =
            StartCoroutine(
                StunRoutine()
            );
    }

    // =========================================================
    // Stun
    // =========================================================

    private IEnumerator StunRoutine()
    {
        isStunned =
            true;


        // ==========================================
        // 이동 즉시 정지
        // ==========================================

        StopMoving();


        // ==========================================
        // 사격 금지
        // ==========================================

        if (enemyShooter != null)
        {
            enemyShooter.SetStunned(
                true
            );
        }


        // ==========================================
        // Player 반대 방향 계산
        // ==========================================

        Vector3 knockbackDirection =
            -transform.forward;


        if (playerTransform != null)
        {
            knockbackDirection =
                transform.position -
                playerTransform.position;


            knockbackDirection.y =
                0f;


            if (knockbackDirection.sqrMagnitude >
                0.001f)
            {
                knockbackDirection.Normalize();
            }
            else
            {
                knockbackDirection =
                    -transform.forward;
            }
        }


        // ==========================================
        // 짧은 Knockback
        // ==========================================

        float elapsed =
            0f;


        float previousProgress =
            0f;


        while (elapsed <
               knockbackDuration)
        {
            if (enemyStats == null ||
                enemyStats.IsDead)
            {
                yield break;
            }


            elapsed +=
                Time.deltaTime;


            float normalizedTime =
                Mathf.Clamp01(
                    elapsed /
                    Mathf.Max(
                        0.01f,
                        knockbackDuration
                    )
                );


            float currentProgress =
                knockbackCurve.Evaluate(
                    normalizedTime
                );


            float progressDelta =
                currentProgress -
                previousProgress;


            Vector3 movement =
                knockbackDirection *
                knockbackDistance *
                progressDelta;


            // ======================================
            // NavMesh 위에서 밀기
            // ======================================

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


        // ==========================================
        // 남은 경직 시간
        // ==========================================

        float remainingStunTime =
            Mathf.Max(
                0f,
                stunDuration -
                knockbackDuration
            );


        if (remainingStunTime > 0f)
        {
            yield return new WaitForSeconds(
                remainingStunTime
            );
        }


        // ==========================================
        // 경직 해제
        // ==========================================

        isStunned =
            false;


        if (enemyShooter != null)
        {
            enemyShooter.SetStunned(
                false
            );
        }


        stunRoutine =
            null;
    }
}