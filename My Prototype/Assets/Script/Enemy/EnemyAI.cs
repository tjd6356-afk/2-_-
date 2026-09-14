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
                    Vector3.up * 0.5f;
            }


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
        if (player == null)
            return false;


        // ==========================================
        // 1. Ray 시작 위치
        // FirePoint가 있으면 총구에서 시작
        // ==========================================

        Vector3 origin;

        if (enemyShooter != null)
        {
            // 우선 Enemy 중심 정도에서 시작
            origin = transform.position + Vector3.up * 0.5f;
        }
        else
        {
            origin = transform.position + Vector3.up * 0.5f;
        }


        // ==========================================
        // 2. Player의 실제 Collider 중심 찾기
        // ==========================================

        Collider playerCollider =
            player.GetComponentInChildren<Collider>();


        Vector3 target;


        if (playerCollider != null)
        {
            // 실제 Collider 정중앙
            target =
                playerCollider.bounds.center;
        }
        else
        {
            // Collider가 없을 경우 예비값
            target =
                player.position +
                Vector3.up * 0.5f;
        }


        // ==========================================
        // 3. 방향 계산
        // ==========================================

        Vector3 direction =
            target - origin;


        float distance =
            direction.magnitude;


        if (distance <= 0.01f)
            return true;


        direction.Normalize();


        // ==========================================
        // 4. RaycastAll
        // ==========================================

        RaycastHit[] hits =
            Physics.RaycastAll(
                origin,
                direction,
                distance + 0.5f,
                sightMask,
                QueryTriggerInteraction.Ignore
            );


        // 가까운 순서로 정렬
        System.Array.Sort(
            hits,
            (a, b) =>
                a.distance.CompareTo(b.distance)
        );


        foreach (RaycastHit hit in hits)
        {
            Transform hitTransform =
                hit.collider.transform;


            // ======================================
            // Enemy 자기 자신은 무시
            // ======================================

            if (hitTransform == transform ||
                hitTransform.IsChildOf(transform))
            {
                continue;
            }


            // ======================================
            // Player인지 검사
            // ======================================

            PlayerStats hitPlayer =
                hit.collider
                    .GetComponentInParent<PlayerStats>();


            if (hitPlayer != null)
            {
                Debug.DrawLine(
                    origin,
                    hit.point,
                    Color.green
                );

                return true;
            }


            // ======================================
            // Player보다 먼저 벽 등을 만남
            // ======================================

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