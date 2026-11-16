using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlyingEnemyAI : MonoBehaviour
{
    private enum AttackState
    {
        None,
        WindUp,     // 돌진 전 잠시 멈춰 조준하는 전조 동작
        Diving,     // 목표를 향해 고속 급강하 돌진
        Recovering  // 돌진 후 다시 공중 호버링 고도로 복귀
    }

    // =========================================================
    // References
    // =========================================================
    [Header("References")]
    [SerializeField]
    private EnemyStats enemyStats;

    // =========================================================
    // Detection & Sight
    // =========================================================
    [Header("Detection")]
    [Tooltip("Player / NPC를 발견하는 최대 거리")]
    [SerializeField]
    private float detectionRange = 25f;

    [Tooltip("급강하 돌진을 시작하는 사거리")]
    [SerializeField]
    private float attackRange = 8f;

    [Tooltip("주변 타겟을 다시 검색하는 주기 (초)")]
    [SerializeField]
    private float targetScanInterval = 0.25f;

    [SerializeField]
    private LayerMask targetMask = ~0;

    [SerializeField]
    private LayerMask sightMask = ~0;

    [Tooltip("비행 및 돌진 시 통과하지 않을 벽/바닥 레이어")]
    [SerializeField]
    private LayerMask obstacleMask = ~0;

    // =========================================================
    // Flight & Hover (공중 부유 및 추적)
    // =========================================================
    [Header("Flight & Hover")]
    [Tooltip("목표 발밑 기준으로 유지하려는 기본 비행 높이")]
    [SerializeField]
    private float hoverHeightAboveTarget = 2.6f;

    [Tooltip("돌진 전 목표와 유지하려는 수평 거리")]
    [SerializeField]
    private float preferredHorizontalDistance = 4.5f;

    [Tooltip("선회/회전 속도")]
    [SerializeField]
    private float rotationSpeed = 10f;

    [Tooltip("지형 충돌 방지를 위한 적 몸체 반경")]
    [Min(0.1f)]
    [SerializeField]
    private float bodyRadius = 0.45f;

    [Header("Bat Motion (박쥐 특유의 흔들림 연출)")]
    [Tooltip("제자리/이동 중 위아래로 둥실거리는 폭")]
    [SerializeField]
    private float bobAmplitude = 0.25f;

    [Tooltip("위아래 둥실거림 속도")]
    [SerializeField]
    private float bobFrequency = 3.5f;

    [Tooltip("추적 시 좌우로 불규칙하게 흔들리며 날아오는 폭")]
    [SerializeField]
    private float weaveAmplitude = 0.6f;

    [Tooltip("좌우 흔들림 속도")]
    [SerializeField]
    private float weaveFrequency = 2.4f;

    // =========================================================
    // Keese Dive Attack (급강하 돌진 공격)
    // =========================================================
    [Header("Dive Attack (키이스 급강하 돌진)")]
    [Tooltip("타겟의 조준 높이 오프셋 (가슴 높이)")]
    [SerializeField]
    private float targetAimHeight = 1.0f;

    [Tooltip("돌진 전 공중에서 멈춰 조준하는 전조 시간 (초)")]
    [Min(0.05f)]
    [SerializeField]
    private float windUpDuration = 0.45f;

    [Tooltip("전조 동작 중 살짝 뒤로 젖히며 고도를 높이는 오프셋")]
    [SerializeField]
    private Vector3 windUpOffset = new Vector3(0f, 0.45f, -0.5f);

    [Tooltip("급강하 돌진 이동 속도")]
    [Min(1f)]
    [SerializeField]
    private float diveSpeed = 16f;

    [Tooltip("조준 지점을 지나쳐 추가로 뚫고 나가는 거리")]
    [Min(0f)]
    [SerializeField]
    private float diveOvershootDistance = 1.8f;

    [Tooltip("돌진 중 근접 타격 판정 반경")]
    [Min(0.1f)]
    [SerializeField]
    private float meleeHitRadius = 0.85f;

    [Tooltip("돌진 종료 후 다시 호버링 높이로 올라가는 시간 (초)")]
    [Min(0.05f)]
    [SerializeField]
    private float recoverDuration = 0.4f;

    [Tooltip("돌진 공격 후 다음 돌진까지의 대기 시간 (초)")]
    [Min(0.1f)]
    [SerializeField]
    private float attackCooldown = 1.6f;

    // =========================================================
    // Hit Reaction (피격 경직 & 공중 넉백)
    // =========================================================
    [Header("Hit Reaction")]
    [SerializeField]
    private float stunDuration = 0.35f;

    [SerializeField]
    private float knockbackDistance = 0.9f;

    [SerializeField]
    private float knockbackDuration = 0.14f;

    [SerializeField]
    private AnimationCurve knockbackCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    // =========================================================
    // Runtime
    // =========================================================
    private Transform currentTarget;
    private PlayerStats targetPlayer;
    private NPCStats targetNPC;
    private Transform playerTransform;

    private float nextTargetScanTime;
    private float nextAttackTime;
    private float baseIdleHeight;
    private float motionPhaseOffset;

    private AttackState attackState = AttackState.None;
    private bool hasHitDuringCurrentDive;
    private bool isStunned;

    private Coroutine attackRoutine;
    private Coroutine stunRoutine;

    // 기즈모 디버그용
    private Vector3 debugDiveStart;
    private Vector3 debugDiveEnd;

    public bool IsStunned => isStunned;
    public Transform CurrentTarget => currentTarget;

    private void Awake()
    {
        if (enemyStats == null)
            enemyStats = GetComponent<EnemyStats>();

        motionPhaseOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    private void Start()
    {
        baseIdleHeight = transform.position.y;
        FindNearestTarget();

        PlayerStats foundPlayer = FindFirstObjectByType<PlayerStats>();
        if (foundPlayer != null)
            playerTransform = foundPlayer.transform;
    }

    private void OnEnable()
    {
        if (enemyStats == null)
            enemyStats = GetComponent<EnemyStats>();

        if (enemyStats != null)
            enemyStats.OnDamaged += HandleDamaged;

        baseIdleHeight = transform.position.y;
    }

    private void OnDisable()
    {
        if (enemyStats != null)
            enemyStats.OnDamaged -= HandleDamaged;

        StopAllCombatRoutines();
    }

    private void StopAllCombatRoutines()
    {
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        if (stunRoutine != null)
        {
            StopCoroutine(stunRoutine);
            stunRoutine = null;
        }

        attackState = AttackState.None;
        hasHitDuringCurrentDive = false;
        isStunned = false;
    }

    private void Update()
    {
        if (enemyStats == null || enemyStats.IsDead)
            return;

        if (isStunned)
            return;

        // 돌진 시퀀스(전조 -> 급강하 -> 복귀) 진행 중에는 코루틴이 이동을 제어
        if (attackState != AttackState.None)
            return;

        // 주기적으로 주변 타겟 탐색
        if (Time.time >= nextTargetScanTime)
        {
            nextTargetScanTime = Time.time + targetScanInterval;
            FindNearestTarget();
        }

        // 타겟이 없으면 제자리 공중 호버링
        if (currentTarget == null)
        {
            HoverInPlace();
            return;
        }

        if (!IsCurrentTargetAlive())
        {
            ClearTarget();
            FindNearestTarget();
            HoverInPlace();
            return;
        }

        float distance = Vector3.Distance(transform.position, GetTargetAimPosition());

        if (distance > detectionRange)
        {
            ClearTarget();
            baseIdleHeight = transform.position.y;
            HoverInPlace();
            return;
        }

        // 공격 사거리 안 + 쿨타임 완료 + 시야 확보 시 급강하 돌진 시작
        if (distance <= attackRange && Time.time >= nextAttackTime && CanSeeTarget())
        {
            attackRoutine = StartCoroutine(DiveAttackRoutine());
            return;
        }

        // 공격 사거리 밖이거나 쿨타임 대기 중이면 목표 상공을 선회하며 접근
        FlyTowardsHoverPosition();
    }

    // =========================================================
    // 1. 제자리 공중 부유 (타겟 없을 때)
    // =========================================================
    private void HoverInPlace()
    {
        float bobOffset = Mathf.Sin((Time.time * bobFrequency) + motionPhaseOffset) * bobAmplitude;
        float targetY = baseIdleHeight + bobOffset;

        Vector3 desiredMove = new Vector3(0f, (targetY - transform.position.y) * 4f * Time.deltaTime, 0f);
        MoveWithCollision(desiredMove);
    }

    // =========================================================
    // 2. 목표 상공으로 접근 및 호버링
    // =========================================================
    private void FlyTowardsHoverPosition()
    {
        Vector3 targetPos = currentTarget.position;
        Vector3 toEnemy = transform.position - targetPos;
        toEnemy.y = 0f;

        Vector3 horizontalDir = toEnemy.sqrMagnitude > 0.001f ? toEnemy.normalized : -transform.forward;

        // 목표 앞쪽 상공의 목표 지점 계산
        Vector3 desiredPos = targetPos
            + horizontalDir * preferredHorizontalDistance
            + Vector3.up * hoverHeightAboveTarget;

        // 박쥐 특유의 위아래/좌우 흔들림 추가
        float t = Time.time + motionPhaseOffset;
        Vector3 rightAxis = Vector3.Cross(Vector3.up, -horizontalDir).normalized;
        desiredPos += Vector3.up * (Mathf.Sin(t * bobFrequency) * bobAmplitude);
        desiredPos += rightAxis * (Mathf.Sin(t * weaveFrequency) * weaveAmplitude);

        Vector3 toDesired = desiredPos - transform.position;
        float distToDesired = toDesired.magnitude;

        if (distToDesired > 0.05f)
        {
            float moveStep = Mathf.Min(distToDesired, enemyStats.MoveSpeed * Time.deltaTime);
            MoveWithCollision(toDesired.normalized * moveStep);
        }

        RotateTowards(GetTargetAimPosition());
    }

    // =========================================================
    // 3. 키이스 급강하 돌진 시퀀스 (전조 -> 돌진 -> 복귀)
    // =========================================================
    private IEnumerator DiveAttackRoutine()
    {
        hasHitDuringCurrentDive = false;

        // -----------------------------------------------------
        // Phase A: 전조 동작 (공중에서 멈춰 뒤로 젖히며 조준)
        // -----------------------------------------------------
        attackState = AttackState.WindUp;
        Vector3 windUpStartPos = transform.position;
        Vector3 lockedAimPos = GetTargetAimPosition();

        Vector3 backDir = (transform.position - lockedAimPos);
        backDir.y = 0f;
        backDir = backDir.sqrMagnitude > 0.001f ? backDir.normalized : -transform.forward;

        Vector3 windUpTargetPos = windUpStartPos
            + Vector3.up * windUpOffset.y
            + backDir * Mathf.Abs(windUpOffset.z);

        float elapsed = 0f;
        while (elapsed < windUpDuration)
        {
            if (enemyStats == null || enemyStats.IsDead || currentTarget == null)
            {
                attackState = AttackState.None;
                attackRoutine = null;
                yield break;
            }

            elapsed += Time.deltaTime;
            float ratio = Mathf.Clamp01(elapsed / windUpDuration);
            float ease = 1f - Mathf.Pow(1f - ratio, 2f);

            // 전조 중에도 플레이어의 최신 위치를 추적해 조준
            lockedAimPos = GetTargetAimPosition();
            RotateTowards(lockedAimPos);

            Vector3 nextPos = Vector3.Lerp(windUpStartPos, windUpTargetPos, ease);
            MoveWithCollision(nextPos - transform.position);

            yield return null;
        }

        // -----------------------------------------------------
        // Phase B: 고속 급강하 돌진
        // -----------------------------------------------------
        attackState = AttackState.Diving;

        Vector3 diveStartPos = transform.position;
        Vector3 diveVector = lockedAimPos - diveStartPos;
        float distToTarget = diveVector.magnitude;

        Vector3 diveDir = distToTarget > 0.001f ? (diveVector / distToTarget) : transform.forward;
        float totalDiveDistance = distToTarget + diveOvershootDistance;

        debugDiveStart = diveStartPos;
        debugDiveEnd = diveStartPos + diveDir * totalDiveDistance;

        float traveledDistance = 0f;
        while (traveledDistance < totalDiveDistance)
        {
            if (enemyStats == null || enemyStats.IsDead)
            {
                attackState = AttackState.None;
                attackRoutine = null;
                yield break;
            }

            float step = diveSpeed * Time.deltaTime;
            Vector3 actualMoved = MoveWithCollision(diveDir * step, out bool hitWallOrFloor);
            traveledDistance += actualMoved.magnitude;

            // 돌진 경로 중 근접 타격 판정
            if (!hasHitDuringCurrentDive && TryDealMeleeDiveDamage())
            {
                hasHitDuringCurrentDive = true;
                break; // 적중 시 바로 상승 복귀 단계로 전환
            }

            // 바닥이나 벽에 부딪히면 돌진 중단
            if (hitWallOrFloor)
                break;

            yield return null;
        }

        // -----------------------------------------------------
        // Phase C: 다시 공중 호버링 높이로 복귀
        // -----------------------------------------------------
        attackState = AttackState.Recovering;

        Vector3 recoverStartPos = transform.position;
        float targetHoverY = currentTarget != null
            ? currentTarget.position.y + hoverHeightAboveTarget
            : Mathf.Max(baseIdleHeight, recoverStartPos.y + 1.5f);

        Vector3 recoverEndPos = new Vector3(
            recoverStartPos.x + diveDir.x * 0.8f,
            Mathf.Max(recoverStartPos.y + 0.6f, targetHoverY),
            recoverStartPos.z + diveDir.z * 0.8f
        );

        float recoverElapsed = 0f;
        while (recoverElapsed < recoverDuration)
        {
            if (enemyStats == null || enemyStats.IsDead)
            {
                attackState = AttackState.None;
                attackRoutine = null;
                yield break;
            }

            recoverElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(recoverElapsed / recoverDuration);
            float smoothT = 1f - Mathf.Pow(1f - t, 2f);

            Vector3 nextPos = Vector3.Lerp(recoverStartPos, recoverEndPos, smoothT);
            MoveWithCollision(nextPos - transform.position);

            if (currentTarget != null)
                RotateTowards(GetTargetAimPosition());

            yield return null;
        }

        baseIdleHeight = transform.position.y;
        nextAttackTime = Time.time + attackCooldown;
        attackState = AttackState.None;
        attackRoutine = null;
    }

    private bool TryDealMeleeDiveDamage()
    {
        if (enemyStats == null || enemyStats.IsDead)
            return false;

        Collider[] hits = Physics.OverlapSphere(transform.position, meleeHitRadius, targetMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hit = hits[i];
            if (hit == null || hit.transform.IsChildOf(transform))
                continue;

            PlayerStats player = hit.GetComponentInParent<PlayerStats>();
            NPCStats npc = hit.GetComponentInParent<NPCStats>();

            if ((player != null && !player.IsDead) || (npc != null && !npc.IsDead))
            {
                IHealth targetHealth = hit.GetComponentInParent<IHealth>();
                if (targetHealth != null && !targetHealth.IsDead)
                {
                    targetHealth.TakeDamage(enemyStats.AttackPower);
                    return true;
                }
            }
        }

        return false;
    }

    // =========================================================
    // 4. 피격 경직 및 공중 넉백
    // =========================================================
    private void HandleDamaged(float damage)
    {
        if (enemyStats == null || enemyStats.IsDead)
            return;

        if (playerTransform == null)
        {
            PlayerStats foundPlayer = FindFirstObjectByType<PlayerStats>();
            if (foundPlayer != null)
                playerTransform = foundPlayer.transform;
        }

        // 돌진 중 맞으면 돌진 즉시 취소
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }
        attackState = AttackState.None;
        hasHitDuringCurrentDive = false;

        if (stunRoutine != null)
            StopCoroutine(stunRoutine);

        stunRoutine = StartCoroutine(StunRoutine());
    }

    private IEnumerator StunRoutine()
    {
        isStunned = true;

        Vector3 knockbackDir = -transform.forward;
        if (playerTransform != null)
        {
            Vector3 away = transform.position - playerTransform.position;
            away.y = 0.25f; // 공중형 적은 뒤로 밀리면서 살짝 위로 튕김
            if (away.sqrMagnitude > 0.001f)
                knockbackDir = away.normalized;
        }

        float elapsed = 0f;
        float prevProgress = 0f;

        while (elapsed < knockbackDuration)
        {
            if (enemyStats == null || enemyStats.IsDead)
                yield break;

            elapsed += Time.deltaTime;
            float norm = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, knockbackDuration));
            float currProgress = knockbackCurve.Evaluate(norm);
            float delta = currProgress - prevProgress;

            MoveWithCollision(knockbackDir * (knockbackDistance * delta));
            prevProgress = currProgress;

            yield return null;
        }

        float remainStun = Mathf.Max(0f, stunDuration - knockbackDuration);
        if (remainStun > 0f)
            yield return new WaitForSeconds(remainStun);

        baseIdleHeight = transform.position.y;
        isStunned = false;
        stunRoutine = null;
    }

    // =========================================================
    // 지형 충돌 보정 이동 (바닥/벽 관통 방지)
    // =========================================================
    private Vector3 MoveWithCollision(Vector3 displacement)
    {
        return MoveWithCollision(displacement, out _);
    }

    private Vector3 MoveWithCollision(Vector3 displacement, out bool hitObstacle)
    {
        hitObstacle = false;
        float distance = displacement.magnitude;
        if (distance <= 0.0001f)
            return Vector3.zero;

        Vector3 direction = displacement / distance;
        RaycastHit[] hits = Physics.SphereCastAll(
            transform.position,
            bodyRadius,
            direction,
            distance,
            obstacleMask,
            QueryTriggerInteraction.Ignore
        );

        float nearestDist = distance;
        for (int i = 0; i < hits.Length; i++)
        {
            Collider col = hits[i].collider;
            if (col == null || col.transform.IsChildOf(transform))
                continue;

            // 캐릭터/투사체는 장애물 정지 대상에서 제외
            if (col.GetComponentInParent<PlayerStats>() != null ||
                col.GetComponentInParent<NPCStats>() != null ||
                col.GetComponentInParent<EnemyStats>() != null ||
                col.GetComponentInParent<Projectile>() != null)
            {
                continue;
            }

            if (hits[i].distance < nearestDist)
            {
                nearestDist = hits[i].distance;
                hitObstacle = true;
            }
        }

        float safeDistance = hitObstacle ? Mathf.Max(0f, nearestDist - 0.02f) : distance;
        Vector3 actualMovement = direction * safeDistance;
        transform.position += actualMovement;
        return actualMovement;
    }

    // =========================================================
    // 타겟 탐색 / 시야 / 회전 헬퍼 (기존 EnemyAI와 동일 규격)
    // =========================================================
    private void FindNearestTarget()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            detectionRange,
            targetMask,
            QueryTriggerInteraction.Ignore
        );

        Transform bestTarget = null;
        PlayerStats bestPlayer = null;
        NPCStats bestNPC = null;
        float bestDistanceSqr = float.MaxValue;

        HashSet<Transform> checkedTargets = new HashSet<Transform>();

        foreach (Collider hit in hits)
        {
            if (hit == null)
                continue;

            PlayerStats player = hit.GetComponentInParent<PlayerStats>();
            if (player != null && !player.IsDead)
            {
                Transform pTrans = player.transform;
                if (checkedTargets.Add(pTrans))
                {
                    float distSqr = (pTrans.position - transform.position).sqrMagnitude;
                    if (distSqr < bestDistanceSqr)
                    {
                        bestDistanceSqr = distSqr;
                        bestTarget = pTrans;
                        bestPlayer = player;
                        bestNPC = null;
                    }
                }
            }

            NPCStats npc = hit.GetComponentInParent<NPCStats>();
            if (npc != null && !npc.IsDead)
            {
                Transform nTrans = npc.transform;
                if (checkedTargets.Add(nTrans))
                {
                    float distSqr = (nTrans.position - transform.position).sqrMagnitude;
                    if (distSqr < bestDistanceSqr)
                    {
                        bestDistanceSqr = distSqr;
                        bestTarget = nTrans;
                        bestNPC = npc;
                        bestPlayer = null;
                    }
                }
            }
        }

        currentTarget = bestTarget;
        targetPlayer = bestPlayer;
        targetNPC = bestNPC;
    }

    private bool IsCurrentTargetAlive()
    {
        if (currentTarget == null)
            return false;

        if (targetPlayer != null)
            return !targetPlayer.IsDead;

        if (targetNPC != null)
            return !targetNPC.IsDead;

        return false;
    }

    private void ClearTarget()
    {
        currentTarget = null;
        targetPlayer = null;
        targetNPC = null;
    }

    private void RotateTowards(Vector3 worldTarget)
    {
        Vector3 dir = worldTarget - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
    }

    private Vector3 GetTargetAimPosition()
    {
        if (currentTarget == null)
            return transform.position;

        Collider targetCollider = currentTarget.GetComponentInChildren<Collider>();
        if (targetCollider != null)
            return targetCollider.bounds.center;

        return currentTarget.position + Vector3.up * targetAimHeight;
    }

    private bool CanSeeTarget()
    {
        if (currentTarget == null)
            return false;

        Vector3 origin = transform.position;
        Vector3 target = GetTargetAimPosition();
        Vector3 direction = target - origin;
        float distance = direction.magnitude;

        if (distance <= 0.01f)
            return true;

        direction.Normalize();

        RaycastHit[] hits = Physics.RaycastAll(origin, direction, distance + 0.5f, sightMask, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            Transform hitTransform = hit.collider.transform;
            if (hitTransform == transform || hitTransform.IsChildOf(transform))
                continue;

            if (targetPlayer != null)
            {
                PlayerStats hitPlayer = hit.collider.GetComponentInParent<PlayerStats>();
                if (hitPlayer == targetPlayer)
                    return true;
            }

            if (targetNPC != null)
            {
                NPCStats hitNPC = hit.collider.GetComponentInParent<NPCStats>();
                if (hitNPC == targetNPC)
                    return true;
            }

            return false;
        }

        return false;
    }

    // =========================================================
    // Scene Debug
    // =========================================================
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, meleeHitRadius);

        if (Application.isPlaying && attackState == AttackState.Diving)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(debugDiveStart, debugDiveEnd);
        }
    }
}