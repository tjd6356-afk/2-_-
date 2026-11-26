using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(EnemyStats))]
[RequireComponent(typeof(NavMeshAgent))]
public class RollingEnemyAI : MonoBehaviour
{
    // =========================================================
    // References
    // =========================================================

    [Header("References")]

    [SerializeField] private EnemyStats enemyStats;

    [SerializeField] private NavMeshAgent agent;

    [SerializeField] private Transform rollingModel;

    [SerializeField] private Transform lookingEmpty;


    // =========================================================
    // Detection
    // =========================================================

    [Header("Detection")]

    [SerializeField] private float detectionRange = 20f;


    // =========================================================
    // Movement
    // =========================================================

    [Header("Movement")]

    [Tooltip("처음 이동할 때 MoveSpeed 배율")]
    [SerializeField] private float startSpeedMultiplier = 0.6f;

    [Tooltip("자폭 직전 MoveSpeed 배율")]
    [SerializeField] private float maxSpeedMultiplier = 3f;

    [Tooltip("플레이어를 향해 Y축으로 회전하는 속도")]
    [SerializeField] private float rotationSpeed = 180f;


    // =========================================================
    // Rolling
    // =========================================================

    [Header("Rolling")]

    [Min(0.01f)]
    [SerializeField] private float rollingRadius = 0.5f;

    [SerializeField] private float rollingSpeedMultiplier = 1f;

    [SerializeField] private bool invertRollingDirection = false;


    // =========================================================
    // Self Destruction
    // =========================================================

    [Header("Self Destruction")]

    [Tooltip("플레이어를 감지한 후 자폭하기까지 걸리는 시간")]
    [Min(0.1f)]
    [SerializeField] private float explosionTime = 8f;

    [Tooltip("플레이어와 접촉 시 즉시 자폭할지 여부")]
    [SerializeField] private bool explodeOnPlayerContact = true;

    [Tooltip("적 중심으로부터 플레이어 콜라이더 표면을 감지할 접촉 폭발 반경")]
    [Min(0.1f)]
    [SerializeField] private float contactExplosionRadius = 0.9f;

    [Tooltip("폭발 반경")]
    [Min(0.01f)]
    [SerializeField] private float explosionRadius = 4f;

    [Tooltip("폭발 이펙트 프리팹")]
    [SerializeField] private ExplosionVFX explosionPrefab;

    [SerializeField] private LayerMask playerMask = ~0;


    // =========================================================
    // Warning Blink
    // =========================================================

    [Header("Warning Blink")]

    [SerializeField] private Color warningColor = Color.red;

    [Tooltip("처음 경고 신호가 발생하는 간격 (초)")]
    [Min(0.01f)]
    [SerializeField] private float startBlinkInterval = 0.6f;

    [Tooltip("자폭 직전 경고 신호가 발생하는 간격 (초)")]
    [Min(0.01f)]
    [SerializeField] private float finalBlinkInterval = 0.12f;

    [Tooltip("한 번 색이 변한 뒤 원래 색으로 서서히 페이드 아웃되는 시간 (초)")]
    [Min(0.02f)]
    [SerializeField] private float warningFadeDuration = 0.35f;

    [SerializeField] private Renderer[] modelRenderers;


    // =========================================================
    // State
    // =========================================================

    public enum EnemyState
    {
        Idle,
        Chase,
        Exploded
    }

    [Header("Runtime State")]

    [SerializeField]
    private EnemyState currentState = EnemyState.Idle;

    [SerializeField]
    private float explosionTimer;

    [SerializeField]
    private float remainingTime;

    [SerializeField]
    private float currentMoveSpeed;

    [SerializeField]
    private float currentBlinkInterval;


    public EnemyState CurrentState => currentState;

    // 기존 외부 스크립트 호환용
    public bool IsStunned => false;


    // =========================================================
    // Runtime
    // =========================================================

    private PlayerStats targetPlayer;

    private Transform player;

    private bool hasExploded;

    private float blinkTimer;

    private float fadeTimer;

    private float warningIntensity;


    // =========================================================
    // Rolling Runtime
    // =========================================================

    private Vector3 previousPosition;

    private Quaternion originalModelRotation;

    private float rollingAngle;


    // =========================================================
    // Material Runtime
    // =========================================================

    private MaterialPropertyBlock[] originalBlocks;

    private Color[] originalBaseColors;

    private Color[] originalColors;

    private MaterialPropertyBlock workingBlock;


    private bool CanUseAgent =>
        agent != null &&
        agent.isActiveAndEnabled &&
        agent.isOnNavMesh;


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

        if (agent == null)
        {
            agent =
                GetComponent<NavMeshAgent>();
        }

        if (rollingModel == null ||
            rollingModel == transform)
        {
            Debug.LogError(
                "[RollingEnemy] Rolling Model을 연결하세요.",
                this
            );

            enabled = false;
            return;
        }

        agent.updateRotation = false;
        agent.updatePosition = true;


        // 모델 초기 회전 저장
        originalModelRotation =
            rollingModel.localRotation;


        // Renderer 자동 탐색
        if (modelRenderers == null ||
            modelRenderers.Length == 0)
        {
            modelRenderers =
                rollingModel.GetComponentsInChildren<Renderer>();
        }


        originalBlocks =
            new MaterialPropertyBlock[modelRenderers.Length];

        originalBaseColors =
            new Color[modelRenderers.Length];

        originalColors =
            new Color[modelRenderers.Length];

        workingBlock =
            new MaterialPropertyBlock();

        for (int i = 0; i < modelRenderers.Length; i++)
        {
            originalBlocks[i] =
                new MaterialPropertyBlock();

            Renderer modelRenderer =
                modelRenderers[i];

            if (modelRenderer == null)
                continue;

            modelRenderer.GetPropertyBlock(
                originalBlocks[i]
            );

            Material sharedMat =
                modelRenderer.sharedMaterial;

            originalBaseColors[i] =
                (sharedMat != null && sharedMat.HasProperty("_BaseColor"))
                ? sharedMat.GetColor("_BaseColor")
                : Color.white;

            originalColors[i] =
                (sharedMat != null && sharedMat.HasProperty("_Color"))
                ? sharedMat.GetColor("_Color")
                : Color.white;
        }
    }


    // =========================================================
    // Enable
    // =========================================================

    private void OnEnable()
    {
        currentState = EnemyState.Idle;

        explosionTimer = 0f;
        remainingTime = explosionTime;

        currentMoveSpeed = 0f;
        currentBlinkInterval = startBlinkInterval;

        blinkTimer = 0f;
        fadeTimer = 0f;
        warningIntensity = 0f;
        rollingAngle = 0f;

        hasExploded = false;

        previousPosition =
            transform.position;

        if (rollingModel != null)
        {
            rollingModel.localRotation =
                originalModelRotation;
        }

        ResetWarningColor();

        StopMoving();
    }


    // =========================================================
    // Update
    // =========================================================

    private void Update()
    {
        if (enemyStats == null ||
            enemyStats.IsDead ||
            hasExploded)
        {
            return;
        }


        switch (currentState)
        {
            case EnemyState.Idle:

                UpdateIdle();

                break;


            case EnemyState.Chase:

                UpdateChase();

                break;
        }
    }


    // =========================================================
    // Player Search
    // =========================================================

    private void FindPlayer()
    {
        targetPlayer =
            FindFirstObjectByType<PlayerStats>();

        player =
            targetPlayer != null
            ? targetPlayer.transform
            : null;
    }


    private bool HasValidPlayer()
    {
        return targetPlayer != null &&
               player != null &&
               !targetPlayer.IsDead;
    }


    // =========================================================
    // Idle
    // =========================================================

    private void UpdateIdle()
    {
        if (!CanUseAgent)
            return;


        if (!HasValidPlayer())
        {
            FindPlayer();
        }


        if (!HasValidPlayer())
            return;


        float distance =
            Vector3.Distance(
                transform.position,
                player.position
            );


        if (distance <= detectionRange)
        {
            StartChase();

            if (explodeOnPlayerContact &&
                IsTouchingPlayer())
            {
                Explode();
            }
        }
    }


    // =========================================================
    // Chase Start
    // =========================================================

    private void StartChase()
    {
        currentState =
            EnemyState.Chase;

        explosionTimer = 0f;

        remainingTime =
            explosionTime;

        blinkTimer = 0f;

        agent.ResetPath();

        agent.isStopped = false;


        // 자폭 경고 첫 펄스 즉시 발생
        TriggerWarningPulse();


        Debug.Log(
            "[RollingEnemy] 자폭 추격 시작"
        );
    }


    // =========================================================
    // Chase Update
    // =========================================================

    private void UpdateChase()
    {
        // =====================================================
        // 자폭 타이머
        // =====================================================

        explosionTimer += Time.deltaTime;

        remainingTime =
            Mathf.Max(
                0f,
                explosionTime - explosionTimer
            );


        // 제한 시간 종료 → 폭발

        if (explosionTimer >= explosionTime)
        {
            Explode();

            return;
        }


        // =====================================================
        // 플레이어와 몸통 콜라이더 접촉 검사 → 즉시 폭발
        // =====================================================

        if (explodeOnPlayerContact &&
            IsTouchingPlayer())
        {
            Explode();

            return;
        }


        // =====================================================
        // 자폭 진행률
        //
        // 시작 = 0
        // 자폭 직전 = 1
        // =====================================================

        float progress =
            Mathf.Clamp01(
                explosionTimer /
                Mathf.Max(0.1f, explosionTime)
            );


        // =====================================================
        // 시간이 지날수록 이동 속도 증가
        // =====================================================

        float speedMultiplier =
            Mathf.Lerp(
                startSpeedMultiplier,
                maxSpeedMultiplier,
                progress
            );


        currentMoveSpeed =
            enemyStats.MoveSpeed *
            Mathf.Max(0f, speedMultiplier);


        // =====================================================
        // 시간이 지날수록 경고 펄스 빈도 증가 + 서서히 페이드 아웃
        // =====================================================

        UpdateWarningBlink(progress);


        // NavMesh에 올라와 있을 때만 이동

        if (!CanUseAgent)
            return;


        // =====================================================
        // 플레이어 방향으로 부드럽게 회전
        // =====================================================

        if (!HasValidPlayer())
        {
            FindPlayer();
        }


        if (HasValidPlayer())
        {
            Vector3 playerDirection =
                player.position -
                transform.position;

            playerDirection.y = 0f;


            RotateTowardsDirection(
                playerDirection
            );


            Debug.DrawRay(
                transform.position,
                playerDirection,
                Color.yellow
            );
        }


        // =====================================================
        // 바라보는 방향으로 무조건 전진
        //
        // 플레이어를 지나쳐도 멈추지 않음
        // =====================================================

        MoveForward(
            currentMoveSpeed
        );


        // 이동 직후에도 접촉 판정 한 번 더 수행
        if (explodeOnPlayerContact &&
            IsTouchingPlayer())
        {
            Explode();

            return;
        }


        Debug.DrawRay(
            transform.position,
            GetForwardDirection() * 3f,
            Color.cyan
        );
    }


    // =========================================================
    // Player Contact Check (Collider Surface Overlap)
    // =========================================================

    private Vector3 GetContactCheckCenter()
    {
        return rollingModel != null
            ? rollingModel.position
            : transform.position + Vector3.up * rollingRadius;
    }

    private bool IsTouchingPlayer()
    {
        Vector3 checkCenter =
            GetContactCheckCenter();

        float checkRadius =
            Mathf.Max(0.1f, contactExplosionRadius);

        Collider[] hits =
            Physics.OverlapSphere(
                checkCenter,
                checkRadius,
                playerMask,
                QueryTriggerInteraction.Ignore
            );

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hit = hits[i];
            if (hit == null)
                continue;

            PlayerStats hitPlayer =
                hit.GetComponentInParent<PlayerStats>();

            if (hitPlayer != null &&
                !hitPlayer.IsDead)
            {
                return true;
            }
        }

        return false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null || other.isTrigger)
            return;

        TryExplodeByContact(other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision == null || collision.gameObject == null)
            return;

        TryExplodeByContact(collision.gameObject);
    }

    private void TryExplodeByContact(GameObject hitObject)
    {
        if (!explodeOnPlayerContact ||
            hasExploded ||
            enemyStats == null ||
            enemyStats.IsDead)
        {
            return;
        }

        if (currentState != EnemyState.Chase &&
            currentState != EnemyState.Idle)
        {
            return;
        }

        PlayerStats hitPlayer =
            hitObject.GetComponentInParent<PlayerStats>();

        if (hitPlayer == null ||
            hitPlayer.IsDead)
        {
            return;
        }

        Explode();
    }


    // =========================================================
    // Looking Empty 방향
    // =========================================================

    private Vector3 GetForwardDirection()
    {
        Vector3 direction =
            lookingEmpty != null
            ? lookingEmpty.position - transform.position
            : transform.forward;


        direction.y = 0f;


        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = transform.forward;
            direction.y = 0f;
        }


        return direction.normalized;
    }


    // =========================================================
    // Y축 회전
    // =========================================================

    private void RotateTowardsDirection(
        Vector3 targetDirection
    )
    {
        targetDirection.y = 0f;


        if (targetDirection.sqrMagnitude <= 0.0001f)
            return;


        targetDirection.Normalize();


        float angle =
            Vector3.SignedAngle(
                GetForwardDirection(),
                targetDirection,
                Vector3.up
            );


        float rotateAmount =
            Mathf.Clamp(
                angle,
                -rotationSpeed * Time.deltaTime,
                rotationSpeed * Time.deltaTime
            );


        transform.Rotate(
            Vector3.up,
            rotateAmount,
            Space.World
        );
    }


    // =========================================================
    // Forward Movement
    // =========================================================

    private void MoveForward(float speed)
    {
        if (!CanUseAgent)
            return;


        Vector3 direction =
            GetForwardDirection();


        float moveDistance =
            speed * Time.deltaTime;


        Vector3 startPosition =
            agent.nextPosition;


        Vector3 nextPosition =
            startPosition +
            direction * moveDistance;


        // =====================================================
        // NavMesh 경계 및 장애물 검사
        // =====================================================

        if (agent.Raycast(
                nextPosition,
                out NavMeshHit hit))
        {
            float availableDistance =
                Vector3.Dot(
                    hit.position - startPosition,
                    direction
                );


            moveDistance =
                Mathf.Clamp(
                    availableDistance - 0.03f,
                    0f,
                    moveDistance
                );
        }


        if (moveDistance <= 0.0001f)
            return;


        agent.isStopped = false;


        // 플레이어 위치가 아닌
        // 현재 바라보는 방향으로 계속 이동

        agent.Move(
            direction * moveDistance
        );
    }


    // =========================================================
    // Model X축 Rolling
    // =========================================================

    private void LateUpdate()
    {
        Vector3 movement =
            transform.position -
            previousPosition;


        previousPosition =
            transform.position;


        if (rollingModel == null ||
            currentState != EnemyState.Chase)
        {
            return;
        }


        movement.y = 0f;


        float signedDistance =
            Vector3.Dot(
                movement,
                GetForwardDirection()
            );


        if (Mathf.Abs(signedDistance) <= 0.00001f)
            return;


        float direction =
            invertRollingDirection
            ? -1f
            : 1f;


        float rotationAmount =
            signedDistance /
            Mathf.Max(0.01f, rollingRadius) *
            Mathf.Rad2Deg *
            rollingSpeedMultiplier *
            direction;


        rollingAngle =
            Mathf.Repeat(
                rollingAngle + rotationAmount,
                360f
            );


        // 기존에 수정했던 X축 회전 유지

        rollingModel.localRotation =
            originalModelRotation *
            Quaternion.AngleAxis(
                rollingAngle,
                Vector3.right
            );
    }


    // =========================================================
    // Warning Pulse & Fade Out
    // =========================================================

    private void UpdateWarningBlink(float progress)
    {
        // 자폭 시간이 가까워질수록 신호 간격이 짧아짐
        currentBlinkInterval =
            Mathf.Max(
                0.02f,
                Mathf.Lerp(
                    startBlinkInterval,
                    finalBlinkInterval,
                    progress
                )
            );

        blinkTimer += Time.deltaTime;

        if (blinkTimer >= currentBlinkInterval)
        {
            blinkTimer %= currentBlinkInterval;
            TriggerWarningPulse();
        }

        // 단발성으로 켜진 경고색을 서서히 원래 색으로 페이드 아웃
        if (warningIntensity > 0f)
        {
            float effectiveFadeDuration =
                Mathf.Max(
                    0.02f,
                    Mathf.Min(warningFadeDuration, currentBlinkInterval)
                );

            fadeTimer += Time.deltaTime;
            float t = Mathf.Clamp01(fadeTimer / effectiveFadeDuration);

            // 초반에 선명하게 빛난 뒤 부드럽게 빠지는 감쇠 곡선
            warningIntensity =
                t >= 1f
                ? 0f
                : Mathf.Pow(1f - t, 1.4f);

            ApplyWarningColor(warningIntensity);
        }
    }

    private void TriggerWarningPulse()
    {
        fadeTimer = 0f;
        warningIntensity = 1f;
        ApplyWarningColor(1f);
    }

    private void ResetWarningColor()
    {
        fadeTimer = 0f;
        warningIntensity = 0f;
        ApplyWarningColor(0f);
    }


    // =========================================================
    // Warning Color
    // =========================================================

    private void ApplyWarningColor(float intensity)
    {
        if (modelRenderers == null ||
            originalBlocks == null)
        {
            return;
        }

        intensity = Mathf.Clamp01(intensity);

        for (int i = 0; i < modelRenderers.Length; i++)
        {
            Renderer modelRenderer =
                modelRenderers[i];

            if (modelRenderer == null)
                continue;

            if (intensity <= 0.001f)
            {
                // 원래 Material 상태로 완전 복구
                modelRenderer.SetPropertyBlock(
                    originalBlocks[i].isEmpty
                    ? null
                    : originalBlocks[i]
                );
            }
            else
            {
                // 원본 PropertyBlock 유지 후 색상만 보간
                modelRenderer.SetPropertyBlock(
                    originalBlocks[i].isEmpty
                    ? null
                    : originalBlocks[i]
                );

                if (workingBlock == null)
                {
                    workingBlock = new MaterialPropertyBlock();
                }

                modelRenderer.GetPropertyBlock(workingBlock);

                Color blendedBaseColor =
                    Color.Lerp(
                        originalBaseColors[i],
                        warningColor,
                        intensity
                    );

                Color blendedColor =
                    Color.Lerp(
                        originalColors[i],
                        warningColor,
                        intensity
                    );

                // URP
                workingBlock.SetColor(
                    "_BaseColor",
                    blendedBaseColor
                );

                // 일반 Material
                workingBlock.SetColor(
                    "_Color",
                    blendedColor
                );

                modelRenderer.SetPropertyBlock(
                    workingBlock
                );
            }
        }
    }


    // =========================================================
    // Self Destruction
    // =========================================================

    private void Explode()
    {
        if (hasExploded ||
            enemyStats == null ||
            enemyStats.IsDead)
        {
            return;
        }


        hasExploded = true;

        currentState = EnemyState.Exploded;

        currentMoveSpeed = 0f;

        remainingTime = 0f;


        StopMoving();

        ResetWarningColor();


        // =====================================================
        // 공용 폭발 위치 및 범위
        // =====================================================

        Vector3 explosionCenter =
            transform.position;

        float radius =
            Mathf.Max(0.01f, explosionRadius);


        // =====================================================
        // 1. 폭발 이펙트 생성
        // =====================================================

        if (explosionPrefab != null)
        {
            ExplosionVFX explosion =
                Instantiate(
                    explosionPrefab,
                    explosionCenter,
                    Quaternion.identity
                );


            // RollingEnemyAI의 폭발 범위를
            // ExplosionVFX에 전달

            explosion.SetRadius(radius);
        }


        // =====================================================
        // 2. 실제 폭발 데미지 범위
        // =====================================================

        Collider[] hitColliders =
            Physics.OverlapSphere(
                explosionCenter,
                radius,
                playerMask,
                QueryTriggerInteraction.Ignore
            );


        HashSet<PlayerStats> damagedPlayers =
            new HashSet<PlayerStats>();


        foreach (Collider hit in hitColliders)
        {
            if (hit == null)
                continue;


            PlayerStats hitPlayer =
                hit.GetComponentInParent<PlayerStats>();


            if (hitPlayer == null ||
                hitPlayer.IsDead)
            {
                continue;
            }


            // 중복 데미지 방지

            if (!damagedPlayers.Add(hitPlayer))
                continue;


            // 공용 EnemyStats의 공격력 사용

            hitPlayer.TakeDamage(
                enemyStats.AttackPower
            );


            Debug.Log(
                $"[RollingEnemy] 폭발 명중 / " +
                $"Damage : {enemyStats.AttackPower}"
            );
        }


        // =====================================================
        // 3. 자폭한 적 사망
        // =====================================================

        Debug.Log(
            $"[RollingEnemy] 자폭 완료 / " +
            $"Explosion Radius : {radius}"
        );


        enemyStats.TakeDamage(
            enemyStats.CurrentHealth
        );
    }


    // =========================================================
    // Stop Movement
    // =========================================================

    private void StopMoving()
    {
        if (!CanUseAgent)
            return;


        agent.isStopped = true;

        agent.ResetPath();

        agent.velocity = Vector3.zero;
    }


    // =========================================================
    // Disable
    // =========================================================

    private void OnDisable()
    {
        ResetWarningColor();

        StopMoving();
    }


    // =========================================================
    // Gizmos
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        // 플레이어 감지 범위

        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(
            transform.position,
            detectionRange
        );


        // 접촉 즉시 폭발 반경 (모델 중심 기준)

        if (explodeOnPlayerContact)
        {
            Gizmos.color = Color.magenta;

            Gizmos.DrawWireSphere(
                GetContactCheckCenter(),
                contactExplosionRadius
            );
        }


        // 폭발 범위

        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(
            transform.position,
            explosionRadius
        );


        // 현재 바라보는 방향

        Gizmos.color = Color.cyan;

        Gizmos.DrawLine(
            transform.position,
            transform.position +
            GetForwardDirection() * 3f
        );
    }
}