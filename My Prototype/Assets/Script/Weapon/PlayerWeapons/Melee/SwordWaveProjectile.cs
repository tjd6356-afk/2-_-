using System.Collections.Generic;
using UnityEngine;

public class SwordWaveProjectile : MonoBehaviour
{
    [Header("Movement & Lifetime (비행 속도 및 수명)")]
    [Tooltip("검기 비행 속도 (m/s)")]
    [Min(0.1f)]
    [SerializeField]
    private float speed = 28f;

    [Tooltip("적에게 맞지 않았을 때 최대 유지 시간 (초)")]
    [Min(0.1f)]
    [SerializeField]
    private float maxLifetime = 2.5f;

    [Header("Wave Tilt & Hitbox (검기 기울기 및 타격 범위)")]
    [Tooltip("검기 기본 기울기 각도 (Launch에서 별도 각도를 넘기지 않을 때 사용)")]
    [Range(-180f, 180f)]
    [SerializeField]
    private float defaultTiltAngle = 28f;

    [Tooltip("발사 시 무기가 넘겨준 기울기 각도를 우선 적용할지 여부")]
    [SerializeField]
    private bool useLauncherTiltAngle = true;

    [Tooltip("검기 판정 박스 크기 (X: 검기 좌우 폭, Y: 상하 두께, Z: 전후 길이)")]
    [SerializeField]
    private Vector3 hitboxSize = new Vector3(2.2f, 0.35f, 0.8f);

    [Header("Post-Hit Follow-Through (맞은 직후 추가 전진 판정)")]
    [Tooltip("첫 적을 맞힌 직후 추가로 더 나아갈 거리 (m) - 이 거리 안의 뒤쪽 적까지 타격 후 소멸")]
    [Min(0f)]
    [SerializeField]
    private float extraTravelAfterFirstHit = 1.5f;

    [Tooltip("첫 적을 맞힌 뒤 추가 전진 구간의 이동 속도 배율")]
    [Range(0.1f, 1.5f)]
    [SerializeField]
    private float postHitSpeedMultiplier = 0.85f;

    [Tooltip("첫 적 이후 뒤쪽 적에게 들어가는 데미지 배율 (1 = 동일 데미지)")]
    [Min(0f)]
    [SerializeField]
    private float postHitDamageMultiplier = 1f;

    [Header("Collision Layers")]
    [Tooltip("타격할 적 레이어")]
    [SerializeField]
    private LayerMask enemyMask = ~0;

    [Tooltip("검기가 부딪히면 소멸할 벽/지형 레이어 (적·플레이어 레이어는 반드시 제외)")]
    [SerializeField]
    private LayerMask obstacleMask = 0;

    [Header("Optional VFX")]
    [Tooltip("(선택) 적중 시 생성할 타격 이펙트 프리팹")]
    [SerializeField]
    private GameObject hitEffectPrefab;

    private Vector3 moveDirection = Vector3.forward;
    private float baseDamage;
    private float appliedTiltAngle;
    private Transform ownerRoot;

    private float spawnTime;
    private bool isLaunched;
    private bool hasTriggeredFirstHit;
    private float remainingDistanceAfterHit;

    private readonly HashSet<IHealth> hitTargets = new HashSet<IHealth>();

    /// <summary>
    /// 프리팹 기본 기울기(defaultTiltAngle)로 검기 발사
    /// </summary>
    public void Launch(Vector3 direction, float damage, GameObject owner)
    {
        Launch(direction, damage, owner, defaultTiltAngle);
    }

    /// <summary>
    /// 지정한 기울기 각도(tiltAngle)를 적용하여 검기 발사
    /// </summary>
    public void Launch(Vector3 direction, float damage, GameObject owner, float tiltAngle)
    {
        moveDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward;
        baseDamage = Mathf.Max(0f, damage);
        ownerRoot = owner != null ? owner.transform.root : null;

        appliedTiltAngle = useLauncherTiltAngle ? tiltAngle : defaultTiltAngle;
        transform.rotation = Quaternion.LookRotation(moveDirection, Vector3.up) * Quaternion.Euler(0f, 0f, appliedTiltAngle);

        spawnTime = Time.time;
        hasTriggeredFirstHit = false;
        remainingDistanceAfterHit = extraTravelAfterFirstHit;
        hitTargets.Clear();
        isLaunched = true;
    }

    private void Update()
    {
        if (!isLaunched)
            return;

        if (Time.time - spawnTime >= maxLifetime)
        {
            Destroy(gameObject);
            return;
        }

        float currentSpeed = hasTriggeredFirstHit ? (speed * postHitSpeedMultiplier) : speed;
        float stepDistance = currentSpeed * Time.deltaTime;

        if (hasTriggeredFirstHit)
        {
            if (remainingDistanceAfterHit <= 0f)
            {
                Destroy(gameObject);
                return;
            }
            stepDistance = Mathf.Min(stepDistance, remainingDistanceAfterHit);
        }

        Vector3 startPos = transform.position;
        bool hitObstacle = false;

        // 1. 벽/지형 충돌 검사 (장애물에 닿으면 그 지점까지만 판정 후 소멸)
        if (obstacleMask.value != 0 && stepDistance > 0f)
        {
            RaycastHit[] wallHits = Physics.RaycastAll(
                startPos,
                moveDirection,
                stepDistance,
                obstacleMask,
                QueryTriggerInteraction.Ignore
            );

            float nearestWallDist = stepDistance;
            for (int i = 0; i < wallHits.Length; i++)
            {
                Collider col = wallHits[i].collider;
                if (col == null)
                    continue;
                if (ownerRoot != null && col.transform.IsChildOf(ownerRoot))
                    continue;

                if (wallHits[i].distance < nearestWallDist)
                {
                    nearestWallDist = wallHits[i].distance;
                    hitObstacle = true;
                }
            }

            stepDistance = nearestWallDist;
        }

        Vector3 endPos = startPos + moveDirection * stepDistance;

        // 2. 적 타격 판정 (이동 경로 전체를 감싸는 Swept OverlapBox)
        ProcessEnemyHits(startPos, endPos, stepDistance, hitObstacle);
    }

    private void ProcessEnemyHits(Vector3 startPos, Vector3 endPos, float stepDistance, bool hitObstacle)
    {
        Vector3 sweepCenter = (startPos + endPos) * 0.5f;
        Vector3 sweptHalfExtents = new Vector3(
            hitboxSize.x * 0.5f,
            hitboxSize.y * 0.5f,
            (hitboxSize.z + stepDistance) * 0.5f
        );

        Collider[] hits = Physics.OverlapBox(
            sweepCenter,
            sweptHalfExtents,
            transform.rotation,
            enemyMask,
            QueryTriggerInteraction.Ignore
        );

        // 아직 첫 적을 맞히지 않은 상태라면, 이번 프레임에서 가장 먼저 닿은 적 기준으로 추가 전진 거리 계산
        if (!hasTriggeredFirstHit)
        {
            float firstHitForwardDist = float.MaxValue;
            bool foundValidEnemy = false;

            for (int i = 0; i < hits.Length; i++)
            {
                if (!TryGetValidTarget(hits[i], out IHealth health))
                    continue;

                Vector3 closestPoint = hits[i].ClosestPoint(startPos);
                float forwardDist = Mathf.Clamp(Vector3.Dot(closestPoint - startPos, moveDirection), 0f, stepDistance);

                if (forwardDist < firstHitForwardDist)
                {
                    firstHitForwardDist = forwardDist;
                    foundValidEnemy = true;
                }
            }

            if (foundValidEnemy)
            {
                hasTriggeredFirstHit = true;

                // 첫 적 적중 지점 + extraTravelAfterFirstHit까지만 이번 프레임 도달 거리 허용
                float maxReachThisFrame = Mathf.Min(stepDistance, firstHitForwardDist + extraTravelAfterFirstHit);
                float consumedFollowThrough = Mathf.Max(0f, maxReachThisFrame - firstHitForwardDist);
                remainingDistanceAfterHit = Mathf.Max(0f, extraTravelAfterFirstHit - consumedFollowThrough);

                ApplyDamageWithinReach(hits, startPos, maxReachThisFrame);
                transform.position = startPos + moveDirection * maxReachThisFrame;

                if (remainingDistanceAfterHit <= 0f || hitObstacle)
                {
                    Destroy(gameObject);
                }
                return;
            }

            // 아무도 맞지 않았으면 정상 전진
            transform.position = endPos;
            if (hitObstacle)
            {
                Destroy(gameObject);
            }
            return;
        }

        // 이미 첫 적을 맞힌 뒤 추가 전진 중인 상태
        ApplyDamageWithinReach(hits, startPos, stepDistance);
        transform.position = endPos;
        remainingDistanceAfterHit -= stepDistance;

        if (remainingDistanceAfterHit <= 0f || hitObstacle)
        {
            Destroy(gameObject);
        }
    }

    private void ApplyDamageWithinReach(Collider[] hits, Vector3 startPos, float maxForwardReach)
    {
        // 박스 앞쪽 여유분(hitboxSize.z * 0.5f)까지 포함해 도달 범위 내 적 타격
        float reachLimit = maxForwardReach + (hitboxSize.z * 0.5f);

        for (int i = 0; i < hits.Length; i++)
        {
            if (!TryGetValidTarget(hits[i], out IHealth health))
                continue;

            Vector3 closestPoint = hits[i].ClosestPoint(startPos);
            float forwardDist = Vector3.Dot(closestPoint - startPos, moveDirection);

            if (forwardDist > reachLimit)
                continue;

            if (!hitTargets.Add(health))
                continue;

            bool isFirstTarget = hitTargets.Count == 1;
            float finalDamage = isFirstTarget ? baseDamage : (baseDamage * postHitDamageMultiplier);

            if (finalDamage > 0f)
            {
                health.TakeDamage(finalDamage);
            }

            if (hitEffectPrefab != null)
            {
                Instantiate(hitEffectPrefab, closestPoint, Quaternion.LookRotation(-moveDirection));
            }
        }
    }

    private bool TryGetValidTarget(Collider col, out IHealth health)
    {
        health = null;
        if (col == null)
            return false;

        if (ownerRoot != null && col.transform.IsChildOf(ownerRoot))
            return false;

        health = col.GetComponentInParent<IHealth>();
        if (health == null || health.IsDead)
            return false;

        return true;
    }

    private void OnValidate()
    {
        speed = Mathf.Max(0.1f, speed);
        maxLifetime = Mathf.Max(0.1f, maxLifetime);
        hitboxSize.x = Mathf.Max(0.05f, hitboxSize.x);
        hitboxSize.y = Mathf.Max(0.05f, hitboxSize.y);
        hitboxSize.z = Mathf.Max(0.05f, hitboxSize.z);
        extraTravelAfterFirstHit = Mathf.Max(0f, extraTravelAfterFirstHit);
        postHitDamageMultiplier = Mathf.Max(0f, postHitDamageMultiplier);
    }

    private void OnDrawGizmosSelected()
    {
        Matrix4x4 prevMatrix = Gizmos.matrix;

        Quaternion previewRot = Application.isPlaying
            ? transform.rotation
            : Quaternion.LookRotation(transform.forward, Vector3.up) * Quaternion.Euler(0f, 0f, defaultTiltAngle);

        // 1. 하늘색: 실제 검기 판정 박스 (기울기 반영)
        Gizmos.color = Color.cyan;
        Gizmos.matrix = Matrix4x4.TRS(transform.position, previewRot, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, hitboxSize);

        // 2. 노란색: 첫 적 적중 후 추가로 전진하는 거리 표시
        if (extraTravelAfterFirstHit > 0f)
        {
            Gizmos.color = Color.yellow;
            Vector3 followCenter = Vector3.forward * (extraTravelAfterFirstHit * 0.5f);
            Vector3 followSize = new Vector3(hitboxSize.x, hitboxSize.y, hitboxSize.z + extraTravelAfterFirstHit);
            Gizmos.DrawWireCube(followCenter, followSize);
        }

        Gizmos.matrix = prevMatrix;
    }
}