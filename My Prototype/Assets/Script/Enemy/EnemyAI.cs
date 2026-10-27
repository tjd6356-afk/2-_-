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


    public Transform CurrentTarget =>
        currentTarget;


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
}