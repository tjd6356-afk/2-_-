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


        attackRoutine =
            StartCoroutine(
                AttackRoutine()
            );
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
}