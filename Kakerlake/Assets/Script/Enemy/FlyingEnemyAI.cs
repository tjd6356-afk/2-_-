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

    // 씬에 활성화된 공중형 적 목록 (개체 간 겹침 방지 계산용)
    private static readonly List<FlyingEnemyAI> activeFliers = new List<FlyingEnemyAI>();

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
    private float attackRange = 8.5f;

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
    // Flight, Hover & Random Spread (공중 부유 및 개체별 랜덤 분산)
    // =========================================================
    [Header("Flight & Hover (개체별 랜덤 거리/높이)")]
    [Tooltip("목표 발밑 기준 유지할 비행 높이 범위 (최소 ~ 최대)")]
    [SerializeField]
    private Vector2 hoverHeightRange = new Vector2(1.7f, 3.1f);

    [Tooltip("돌진 전 목표와 유지할 수평 거리 범위 (최소 ~ 최대)")]
    [SerializeField]
    private Vector2 preferredDistanceRange = new Vector2(3.5f, 6.5f);

    [Tooltip("목표 주위를 좌우로 맴도는 공전 각속도 범위 (도/초, 음수=반시계 / 양수=시계)")]
    [SerializeField]
    private Vector2 orbitAngularSpeedRange = new Vector2(-40f, 40f);

    [Tooltip("돌진이 끝날 때마다 새로운 비행 거리·높이·선회 방향을 다시 뽑을지 여부")]
    [SerializeField]
    private bool rerollPositionAfterDive = true;

    [Tooltip("선회/회전 속도")]
    [SerializeField]
    private float rotationSpeed = 10f;

    [Tooltip("지형 충돌 방지를 위한 적 몸체 반경")]
    [Min(0.1f)]
    [SerializeField]
    private float bodyRadius = 0.45f;

    [Header("Separation (동료 간 겹침 방지)")]
    [Tooltip("다른 공중형 적과 거리를 벌리기 시작하는 반경")]
    [Min(0f)]
    [SerializeField]
    private float separationRadius = 2.0f;

    [Tooltip("서로 가까워졌을 때 밀어내는 힘의 세기")]
    [Min(0f)]
    [SerializeField]
    private float separationStrength = 3.5f;

    [Header("Bat Motion (박쥐 특유의 흔들림 연출)")]
    [Tooltip("제자리/이동 중 위아래로 둥실거리는 폭 범위 (최소 ~ 최대)")]
    [SerializeField]
    private Vector2 bobAmplitudeRange = new Vector2(0.18f, 0.35f);

    [Tooltip("위아래 둥실거림 속도 범위 (최소 ~ 최대)")]
    [SerializeField]
    private Vector2 bobFrequencyRange = new Vector2(2.8f, 4.4f);

    [Tooltip("추적 시 좌우로 불규칙하게 흔들리며 날아오는 폭")]
    [SerializeField]
    private float weaveAmplitude = 0.55f;

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

    [Tooltip("돌진 전 공중에서 멈춰 조준하는 전조 시간 범위 (초, 최소 ~ 최대)")]
    [SerializeField]
    private Vector2 windUpDurationRange = new Vector2(0.38f, 0.58f);

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

    [Tooltip("돌진 공격 후 다음 돌진까지의 랜덤 대기 시간 범위 (초, 최소 ~ 최대)")]
    [SerializeField]
    private Vector2 attackCooldownRange = new Vector2(1.2f, 2.8f);

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

    // 개체별 랜덤 부여 파라미터
    private float personalHoverHeight;
    private float personalPreferredDistance;
    private float personalOrbitSpeed;
    private float personalBobAmplitude;
    private float personalBobFrequency;
    private float currentOrbitAngle;
    private bool hasInitializedOrbitAngle;

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
        personalBobAmplitude = Random.Range(bobAmplitudeRange.x, bobAmplitudeRange.y);
        personalBobFrequency = Random.Range(bobFrequencyRange.x, bobFrequencyRange.y);

        RollPersonalFlightParameters();
    }

    private void Start()
    {
        baseIdleHeight = transform.position.y;

        // 동시 스폰 시 한꺼번에 돌진하지 않도록 초기 공격 타이밍 분산
        nextTargetScanTime = Time.time + Random.Range(0f, targetScanInterval);
        nextAttackTime = Time.time + Random.Range(0.2f, attackCooldownRange.y * 0.75f);

        FindNearestTarget();

        PlayerStats foundPlayer = FindFirstObjectByType<PlayerStats>();
        if (foundPlayer != null)
            playerTransform = foundPlayer.transform;
    }

    private void OnEnable()
    {
        if (!activeFliers.Contains(this))
            activeFliers.Add(this);

        if (enemyStats == null)
            enemyStats = GetComponent<EnemyStats>();

        if (enemyStats != null)
            enemyStats.OnDamaged += HandleDamaged;

        baseIdleHeight = transform.position.y;
    }

    private void OnDisable()
    {
        activeFliers.Remove(this);

        if (enemyStats != null)
            enemyStats.OnDamaged -= HandleDamaged;

        StopAllCombatRoutines();
    }

    private void RollPersonalFlightParameters()
    {
        personalHoverHeight = Random.Range(hoverHeightRange.x, hoverHeightRange.y);
        personalPreferredDistance = Random.Range(preferredDistanceRange.x, preferredDistanceRange.y);

        // 너무 느리게 거의 제자리에 멈추는 각속도는 피하도록 보정
        float rawOrbit = Random.Range(orbitAngularSpeedRange.x, orbitAngularSpeedRange.y);
        if (Mathf.Abs(rawOrbit) < 10f && (Mathf.Abs(orbitAngularSpeedRange.x) >= 10f || Mathf.Abs(orbitAngularSpeedRange.y) >= 10f))
        {
            rawOrbit = (Random.value < 0.5f ? -1f : 1f) * Random.Range(12f, Mathf.Max(15f, Mathf.Abs(orbitAngularSpeedRange.y)));
        }
        personalOrbitSpeed = rawOrbit;
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

        // 공격 사거리 안 + 랜덤 쿨타임 완료 + 시야 확보 시 급강하 돌진 시작
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
        float bobOffset = Mathf.Sin((Time.time * personalBobFrequency) + motionPhaseOffset) * personalBobAmplitude;
        float targetY = baseIdleHeight + bobOffset;

        Vector3 hoverMove = new Vector3(0f, (targetY - transform.position.y) * 4f, 0f);
        Vector3 totalVelocity = hoverMove + ComputeSeparationVector();

        MoveWithCollision(totalVelocity * Time.deltaTime);
    }

    // =========================================================
    // 2. 목표 상공으로 접근 및 개체별 궤도 선회
    // =========================================================
    private void FlyTowardsHoverPosition()
    {
        Vector3 targetPos = currentTarget.position;
        Vector3 toEnemy = transform.position - targetPos;
        toEnemy.y = 0f;

        if (!hasInitializedOrbitAngle)
        {
            currentOrbitAngle = toEnemy.sqrMagnitude > 0.001f
                ? Mathf.Atan2(toEnemy.x, toEnemy.z) * Mathf.Rad2Deg
                : Random.Range(0f, 360f);
            hasInitializedOrbitAngle = true;
        }

        // 개체별 각속도로 목표 주위를 회전하며 접근
        currentOrbitAngle = Mathf.Repeat(currentOrbitAngle + personalOrbitSpeed * Time.deltaTime, 360f);
        float orbitRad = currentOrbitAngle * Mathf.Deg2Rad;
        Vector3 horizontalDir = new Vector3(Mathf.Sin(orbitRad), 0f, Mathf.Cos(orbitRad));

        Vector3 desiredPos = targetPos
            + horizontalDir * personalPreferredDistance
            + Vector3.up * personalHoverHeight;

        float t = Time.time + motionPhaseOffset;
        Vector3 rightAxis = Vector3.Cross(Vector3.up, -horizontalDir).normalized;
        desiredPos += Vector3.up * (Mathf.Sin(t * personalBobFrequency) * personalBobAmplitude);
        desiredPos += rightAxis * (Mathf.Sin(t * weaveFrequency) * weaveAmplitude);

        Vector3 toDesired = desiredPos - transform.position;
        float distToDesired = toDesired.magnitude;

        Vector3 seekVelocity = Vector3.zero;
        if (distToDesired > 0.05f)
        {
            float desiredSpeed = Mathf.Min(enemyStats.MoveSpeed, distToDesired * 4f);
            seekVelocity = (toDesired / distToDesired) * desiredSpeed;
        }

        Vector3 separationVelocity = ComputeSeparationVector();
        Vector3 combinedVelocity = seekVelocity + separationVelocity;

        float maxMoveSpeed = enemyStats.MoveSpeed * 1.25f;
        if (combinedVelocity.magnitude > maxMoveSpeed)
        {
            combinedVelocity = combinedVelocity.normalized * maxMoveSpeed;
        }

        MoveWithCollision(combinedVelocity * Time.deltaTime);
        RotateTowards(GetTargetAimPosition());
    }

    // =========================================================
    // 개체 간 겹침 방지 반발 벡터 계산
    // =========================================================
    private Vector3 ComputeSeparationVector()
    {
        if (separationRadius <= 0.01f || separationStrength <= 0f)
            return Vector3.zero;

        Vector3 push = Vector3.zero;
        float radiusSqr = separationRadius * separationRadius;

        for (int i = 0; i < activeFliers.Count; i++)
        {
            FlyingEnemyAI other = activeFliers[i];
            if (other == null || other == this || !other.isActiveAndEnabled)
                continue;

            if (other.enemyStats != null && other.enemyStats.IsDead)
                continue;

            Vector3 diff = transform.position - other.transform.position;
            float distSqr = diff.sqrMagnitude;

            if (distSqr < radiusSqr)
            {
                float dist = Mathf.Sqrt(distSqr);
                Vector3 awayDir = dist > 0.0001f
                    ? (diff / dist)
                    : Random.onUnitSphere;

                float weight = 1f - (dist / separationRadius);
                push += awayDir * weight;
            }
        }

        return push * separationStrength;
    }

    // =========================================================
    // 3. 키이스 급강하 돌진 시퀀스 (전조 -> 돌진 -> 복귀)
    // =========================================================
    private IEnumerator DiveAttackRoutine()
    {
        hasHitDuringCurrentDive = false;

        // -----------------------------------------------------
        // Phase A: 전조 동작 (랜덤 전조 시간 동안 공중에서 조준)
        // -----------------------------------------------------
        attackState = AttackState.WindUp;
        float currentWindUpDuration = Random.Range(windUpDurationRange.x, windUpDurationRange.y);

        Vector3 windUpStartPos = transform.position;
        Vector3 lockedAimPos = GetTargetAimPosition();

        Vector3 backDir = (transform.position - lockedAimPos);
        backDir.y = 0f;
        backDir = backDir.sqrMagnitude > 0.001f ? backDir.normalized : -transform.forward;

        Vector3 windUpTargetPos = windUpStartPos
            + Vector3.up * windUpOffset.y
            + backDir * Mathf.Abs(windUpOffset.z);

        float elapsed = 0f;
        while (elapsed < currentWindUpDuration)
        {
            if (enemyStats == null || enemyStats.IsDead || currentTarget == null)
            {
                attackState = AttackState.None;
                attackRoutine = null;
                yield break;
            }

            elapsed += Time.deltaTime;
            float ratio = Mathf.Clamp01(elapsed / currentWindUpDuration);
            float ease = 1f - Mathf.Pow(1f - ratio, 2f);

            lockedAimPos = GetTargetAimPosition();
            RotateTowards(lockedAimPos);

            Vector3 nextPos = Vector3.Lerp(windUpStartPos, windUpTargetPos, ease);
            Vector3 separationStep = ComputeSeparationVector() * (0.4f * Time.deltaTime);
            MoveWithCollision((nextPos - transform.position) + separationStep);

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

            Vector3 prevPos = transform.position;
            float step = diveSpeed * Time.deltaTime;
            Vector3 actualMoved = MoveWithCollision(diveDir * step, out bool hitWallOrFloor);
            traveledDistance += actualMoved.magnitude;

            if (!hasHitDuringCurrentDive && TryDealMeleeDiveDamage(prevPos, transform.position))
            {
                hasHitDuringCurrentDive = true;
                break;
            }

            if (hitWallOrFloor)
                break;

            yield return null;
        }

        // -----------------------------------------------------
        // Phase C: 다시 공중 호버링 높이로 복귀 + 다음 위치/쿨타임 랜덤 재설정
        // -----------------------------------------------------
        attackState = AttackState.Recovering;

        if (rerollPositionAfterDive)
        {
            RollPersonalFlightParameters();
        }

        Vector3 recoverStartPos = transform.position;
        float targetHoverY = currentTarget != null
            ? currentTarget.position.y + personalHoverHeight
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
            Vector3 separationStep = ComputeSeparationVector() * (0.6f * Time.deltaTime);
            MoveWithCollision((nextPos - transform.position) + separationStep);

            if (currentTarget != null)
                RotateTowards(GetTargetAimPosition());

            yield return null;
        }

        // 복귀 완료 후 현재 각도에서 이어서 선회하도록 갱신
        hasInitializedOrbitAngle = false;
        baseIdleHeight = transform.position.y;
        nextAttackTime = Time.time + Random.Range(attackCooldownRange.x, attackCooldownRange.y);
        attackState = AttackState.None;
        attackRoutine = null;
    }

    private bool TryDealMeleeDiveDamage(Vector3 startPos, Vector3 endPos)
    {
        if (enemyStats == null || enemyStats.IsDead || enemyStats.AttackPower <= 0f)
            return false;

        Collider[] hits = Physics.OverlapCapsule(
            startPos,
            endPos,
            meleeHitRadius,
            targetMask,
            QueryTriggerInteraction.Collide
        );

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hit = hits[i];
            if (hit == null || hit.transform.IsChildOf(transform))
                continue;

            PlayerStats player = hit.GetComponentInParent<PlayerStats>();
            if (player != null && !player.IsDead)
            {
                player.TakeDamage(enemyStats.AttackPower);
                return true;
            }

            NPCStats npc = hit.GetComponentInParent<NPCStats>();
            if (npc != null && !npc.IsDead)
            {
                IHealth npcHealth = hit.GetComponentInParent<IHealth>();
                if (npcHealth != null)
                {
                    npcHealth.TakeDamage(enemyStats.AttackPower);
                }
                else
                {
                    npc.SendMessage("TakeDamage", enemyStats.AttackPower, SendMessageOptions.DontRequireReceiver);
                }
                return true;
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
            away.y = 0.25f;
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

        hasInitializedOrbitAngle = false;
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

            if (col.GetComponentInParent<PlayerStats>() != null ||
                col.GetComponentInParent<NPCStats>() != null ||
                col.GetComponentInParent<EnemyStats>() != null ||
                col.GetComponentInParent<WeaponBase>() != null ||
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
    // 타겟 탐색 / 시야 / 회전 헬퍼
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

        if (bestTarget != currentTarget)
        {
            hasInitializedOrbitAngle = false;
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
        hasInitializedOrbitAngle = false;
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

    private void OnValidate()
    {
        hoverHeightRange.x = Mathf.Max(0.5f, hoverHeightRange.x);
        hoverHeightRange.y = Mathf.Max(hoverHeightRange.x, hoverHeightRange.y);

        preferredDistanceRange.x = Mathf.Max(1f, preferredDistanceRange.x);
        preferredDistanceRange.y = Mathf.Max(preferredDistanceRange.x, preferredDistanceRange.y);

        if (orbitAngularSpeedRange.y < orbitAngularSpeedRange.x)
            orbitAngularSpeedRange.y = orbitAngularSpeedRange.x;

        bobAmplitudeRange.x = Mathf.Max(0f, bobAmplitudeRange.x);
        bobAmplitudeRange.y = Mathf.Max(bobAmplitudeRange.x, bobAmplitudeRange.y);

        bobFrequencyRange.x = Mathf.Max(0.1f, bobFrequencyRange.x);
        bobFrequencyRange.y = Mathf.Max(bobFrequencyRange.x, bobFrequencyRange.y);

        windUpDurationRange.x = Mathf.Max(0.05f, windUpDurationRange.x);
        windUpDurationRange.y = Mathf.Max(windUpDurationRange.x, windUpDurationRange.y);

        attackCooldownRange.x = Mathf.Max(0.1f, attackCooldownRange.x);
        attackCooldownRange.y = Mathf.Max(attackCooldownRange.x, attackCooldownRange.y);
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

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, separationRadius);

        if (Application.isPlaying && attackState == AttackState.Diving)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(debugDiveStart, debugDiveEnd);
        }
    }
}