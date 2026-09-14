using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyStats enemyStats;

    [SerializeField] private EnemyShooter enemyShooter;

    [SerializeField] private NavMeshAgent agent;

    [SerializeField] private Transform player;


    [Header("Detection")]
    [Tooltip("플레이어를 발견하는 거리")]
    [SerializeField] private float detectionRange = 20f;

    [Tooltip("이 거리까지 접근하면 멈추고 공격")]
    [SerializeField] private float attackRange = 10f;


    [Header("Aim")]
    [Tooltip("적이 조준할 플레이어 높이")]
    [SerializeField] private float targetHeight = 1.2f;

    [SerializeField] private float rotationSpeed = 8f;


    [Header("Line Of Sight")]
    [SerializeField] private LayerMask sightMask = ~0;


    private bool detectedPlayer;


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


    private void Start()
    {
        // Inspector에 직접 안 넣어도
        // PlayerStats를 가진 객체를 검색
        if (player == null)
        {
            PlayerStats playerStats =
                FindFirstObjectByType<PlayerStats>();


            if (playerStats != null)
            {
                player =
                    playerStats.transform;
            }
        }


        if (agent != null &&
            enemyStats != null)
        {
            agent.speed =
                enemyStats.MoveSpeed;


            // 회전은 직접 처리
            agent.updateRotation =
                false;
        }
    }


    private void Update()
    {
        if (enemyStats == null ||
            enemyStats.IsDead)
        {
            return;
        }


        if (player == null)
            return;


        float distance =
            Vector3.Distance(
                transform.position,
                player.position
            );


        // =====================================================
        // 플레이어 발견
        // =====================================================

        if (!detectedPlayer)
        {
            if (distance <=
                detectionRange)
            {
                detectedPlayer = true;
            }
            else
            {
                StopMoving();

                return;
            }
        }


        // =====================================================
        // 공격 범위 밖
        // → 접근
        // =====================================================

        if (distance >
            attackRange)
        {
            ChasePlayer();

            return;
        }


        // =====================================================
        // 공격 범위
        // =====================================================

        StopMoving();


        RotateTowardsPlayer();


        if (CanSeePlayer())
        {
            Vector3 targetPosition =
                player.position +
                Vector3.up *
                targetHeight;


            enemyShooter.TryShoot(
                targetPosition
            );
        }
    }


    // =========================================================
    // 추적
    // =========================================================

    private void ChasePlayer()
    {
        if (agent == null)
            return;


        agent.isStopped =
            false;


        agent.speed =
            enemyStats.MoveSpeed;


        agent.SetDestination(
            player.position
        );


        // 이동 중에도 플레이어 쪽으로 회전
        Vector3 direction =
            agent.desiredVelocity;


        direction.y = 0f;


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


        if (agent.isOnNavMesh)
        {
            agent.isStopped =
                true;


            agent.ResetPath();
        }
    }


    // =========================================================
    // 플레이어 바라보기
    // =========================================================

    private void RotateTowardsPlayer()
    {
        Vector3 direction =
            player.position -
            transform.position;


        // 위아래로 몸이 기울어지지 않음
        direction.y = 0f;


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
    // 시야 확인
    // =========================================================

    private bool CanSeePlayer()
    {
        Vector3 origin =
            transform.position +
            Vector3.up *
            targetHeight;


        Vector3 target =
            player.position +
            Vector3.up *
            targetHeight;


        Vector3 direction =
            target -
            origin;


        float distance =
            direction.magnitude;


        direction.Normalize();


        if (Physics.Raycast(
                origin,
                direction,
                out RaycastHit hit,
                distance,
                sightMask,
                QueryTriggerInteraction.Ignore))
        {
            PlayerStats hitPlayer =
                hit.collider
                    .GetComponentInParent<PlayerStats>();


            return hitPlayer != null;
        }


        return false;
    }


    // =========================================================
    // Scene에서 거리 확인
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(
            transform.position,
            detectionRange
        );


        Gizmos.DrawWireSphere(
            transform.position,
            attackRange
        );
    }
}