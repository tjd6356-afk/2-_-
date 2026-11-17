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
    private Vector3 hitboxSize = new Vector3(2.4f, 0.8f, 1.0f);

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

    [Tooltip("검기가 부딪히면 소멸할 벽/지형 레이어 (적·플레이어 레이어는 제외)")]
    [SerializeField]
    private LayerMask obstacleMask = 0;

    [Tooltip("적의 Is Trigger 콜라이더도 검기로 타격할지 여부")]
    [SerializeField]
    private bool hitTriggerColliders = false;

    [Header("Optional VFX")]
    [Tooltip("(선택) 적중 시 생성할 타격 이펙트 프리팹")]
    [SerializeField]
    private GameObject hitEffectPrefab;

    private Vector3 moveDirection = Vector3.forward;
    private float baseDamage;
    private float appliedTiltAngle;
    private PlayerStats ownerStats;

    private float spawnTime;
    private bool isLaunched;
    private bool hasTriggeredFirstHit;
    private float remainingDistanceAfterHit;

    private readonly HashSet<IHealth> hitTargets = new HashSet<IHealth>();

    public void Launch(Vector3 direction, float damage, PlayerStats owner)
    {
        Launch(direction, damage, owner, defaultTiltAngle);
    }

    public void Launch(Vector3 direction, float damage, PlayerStats owner, float tiltAngle)
    {
        moveDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward;
        baseDamage = Mathf.Max(0f, damage);
        ownerStats = owner;

        appliedTiltAngle = useLauncherTiltAngle ? tiltAngle : defaultTiltAngle;
        transform.rotation = Quaternion.LookRotation(moveDirection, Vector3.up) * Quaternion.Euler(0f, 0f, appliedTiltAngle);

        spawnTime = Time.time;
        hasTriggeredFirstHit = false;
        remainingDistanceAfterHit = extraTravelAfterFirstHit;
        hitTargets.Clear();
        isLaunched = true;

        // 발사 직후 영점 거리에 있는 적도 즉시 판정
        ProcessEnemyHits(transform.position, transform.position, 0f, false);
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

                PlayerStats hitPlayer = col.GetComponentInParent<PlayerStats>();
                if (hitPlayer != null && hitPlayer == ownerStats)
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

        QueryTriggerInteraction triggerMode = hitTriggerColliders
            ? QueryTriggerInteraction.Collide
            : QueryTriggerInteraction.Ignore;

        Collider[] hits = Physics.OverlapBox(
            sweepCenter,
            sweptHalfExtents,
            transform.rotation,
            enemyMask,
            triggerMode
        );

        if (!hasTriggeredFirstHit)
        {
            float firstHitForwardDist = float.MaxValue;
            bool foundValidEnemy = false;

            for (int i = 0; i < hits.Length; i++)
            {
                if (!TryGetValidTarget(hits[i], out _))
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

            transform.position = endPos;
            if (hitObstacle)
            {
                Destroy(gameObject);
            }
            return;
        }

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

        PlayerStats hitPlayer = col.GetComponentInParent<PlayerStats>();
        if (hitPlayer != null && hitPlayer == ownerStats)
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

        Gizmos.color = Color.cyan;
        Gizmos.matrix = Matrix4x4.TRS(transform.position, previewRot, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, hitboxSize);

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