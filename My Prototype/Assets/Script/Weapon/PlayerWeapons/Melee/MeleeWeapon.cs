using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MeleeWeapon : WeaponBase
{
    [Header("References")]
    [SerializeField]
    private Transform attackPoint;

    [Tooltip("실제로 회전/이동할 무기 메쉬(모델) 자식 오브젝트")]
    [SerializeField]
    private Transform weaponVisual;

    [Tooltip("휘두를 때만 켜질 검기 Trail Renderer (비워두면 Weapon Visual 하위에서 자동 탐색)")]
    [SerializeField]
    private TrailRenderer swingTrail;

    [Tooltip("(선택) 휘두르기 중심 기준으로 삼을 빈 오브젝트. 비워두면 무기 프리팹 루트(0,0,0)를 기준으로 오프셋만 적용합니다.")]
    [SerializeField]
    private Transform swingCenterPoint;

    [Header("Attack Stats (공격 설정)")]
    [Tooltip("전방 공격 사거리 (Z 길이)")]
    [Min(0.1f)]
    [SerializeField]
    private float attackRange = 2.0f;

    [Tooltip("공격 판정의 좌우 전체 폭 (X 길이)")]
    [Min(0.1f)]
    [SerializeField]
    private float attackWidth = 2.6f;

    [Tooltip("공격 판정의 상하 전체 높이 (Y 길이)")]
    [Min(0.1f)]
    [SerializeField]
    private float attackHeight = 1.2f;

    [Min(0f)]
    [SerializeField]
    private float damageMultiplier = 1f;

    [Min(0.01f)]
    [SerializeField]
    private float baseAttackInterval = 0.5f;

    [SerializeField]
    private LayerMask enemyMask = ~0;

    // =========================================================
    // 가드 설정 (우클릭 홀드)
    // =========================================================
    [Header("Guard Settings (우클릭 가드 설정)")]
    [Tooltip("가드 중 플레이어 이동 속도 비율 (0.5 = 기본 이동 속도의 50%로 감소)")]
    [Range(0f, 1f)]
    [SerializeField]
    private float guardMoveSpeedMultiplier = 0.5f;

    [Tooltip("가드 중 받는 피해량 감소율 (0.6 = 받는 피해 60% 감소, 40%만 입음)")]
    [Range(0f, 1f)]
    [SerializeField]
    private float guardDamageReduction = 0.6f;

    [Tooltip("가드 시 무기를 가로로 들 로컬 위치")]
    [SerializeField]
    private Vector3 guardLocalPosition = new Vector3(0.35f, 1.15f, 0.55f);

    [Tooltip("가드 시 무기를 가로로 눕힐 로컬 회전각 (Z = 80~90으로 가로 배치)")]
    [SerializeField]
    private Vector3 guardLocalEuler = new Vector3(15f, 0f, 80f);

    [Tooltip("가드 자세로 전환되는 속도")]
    [Min(1f)]
    [SerializeField]
    private float guardTransitionSpeed = 16f;

    // =========================================================
    // 휘두르기 중심 위치 및 원호(부채꼴) 궤도 설정
    // =========================================================
    [Header("Swing Arc (휘두르기 중심 및 궤도)")]
    [Tooltip("휘두르기 중심 위치 오프셋 (X: 좌우, Y: 높이, Z: 앞뒤)")]
    [SerializeField]
    private Vector3 swingCenterOffset = new Vector3(0f, 1.15f, 0.25f);

    [Tooltip("좌우로 휘두르는 반각 (80이면 오른쪽 +80° ~ 왼쪽 -80°, 총 160° 반원 회전)")]
    [Range(10f, 150f)]
    [SerializeField]
    private float swingHalfAngle = 80f;

    [Tooltip("휘두르기 중심으로부터 무기가 떨어져 도는 원호 반경")]
    [Min(0.1f)]
    [SerializeField]
    private float orbitRadius = 1.25f;

    [Header("Blade Orientation & Timing (무기 눕힘 각도 및 속도)")]
    [Tooltip("휘두를 때 무기를 눕히는 X축 각도 (90 = 지면과 완전히 수평)")]
    [SerializeField]
    private float bladeLayAngle = 90f;

    [Tooltip("휘두를 때 칼날 기울기 Z축 각도 (0 = 수평 횡베기)")]
    [SerializeField]
    private float bladeRollAngle = 0f;

    [Tooltip("대기 상태에서 첫 공격 시 전방 시작 위치로 즉시 전환한 뒤 휘두를지 여부")]
    [SerializeField]
    private bool snapToForwardStartOnFirstSwing = true;

    [Min(0.02f)]
    [SerializeField]
    private float baseSwingDuration = 0.18f;

    [SerializeField]
    private AnimationCurve swingCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("공격 종료 후 원래 기본 자세로 되돌아가는 속도")]
    [Min(1f)]
    [SerializeField]
    private float returnToDefaultSpeed = 14f;

    private float nextAttackTime;
    private bool isSwungToLeft;
    private bool isSwinging;
    private bool isInAttackPose;
    private bool isGuarding;
    private bool isEquipped;
    private Coroutine swingCoroutine;

    // 무기에 위치/회전값을 부여하기 전의 원본 기본 자세
    private Vector3 defaultLocalPosition;
    private Quaternion defaultLocalRotation;
    private bool hasCachedDefaultPose;

    public bool IsGuarding => isGuarding;

    private void Awake()
    {
        CacheDefaultPose();
        FindSwingTrailIfNeeded();
        SetTrailEmitting(false, true);
    }

    private void FindSwingTrailIfNeeded()
    {
        if (swingTrail == null && weaponVisual != null)
        {
            swingTrail = weaponVisual.GetComponentInChildren<TrailRenderer>(true);
        }
    }

    private void SetTrailEmitting(bool emit, bool clear = false)
    {
        if (swingTrail == null)
            return;

        if (clear)
            swingTrail.Clear();

        swingTrail.emitting = emit;
    }

    private void CacheDefaultPose()
    {
        if (hasCachedDefaultPose || weaponVisual == null)
            return;

        defaultLocalPosition = weaponVisual.localPosition;
        defaultLocalRotation = weaponVisual.localRotation;
        hasCachedDefaultPose = true;
    }

    public override void Equip()
    {
        CacheDefaultPose();
        FindSwingTrailIfNeeded();
        base.Equip();

        isEquipped = true;
        ResetWeaponState();
    }

    public override void Unequip()
    {
        isEquipped = false;
        ResetWeaponState();
        base.Unequip();
    }

    private void OnDisable()
    {
        if (isGuarding && PlayerStats != null)
        {
            PlayerStats.ResetGuardModifiers();
        }
        isGuarding = false;
    }

    private void ResetWeaponState()
    {
        if (swingCoroutine != null)
        {
            StopCoroutine(swingCoroutine);
            swingCoroutine = null;
        }

        if (isGuarding && PlayerStats != null)
        {
            PlayerStats.ResetGuardModifiers();
        }

        isGuarding = false;
        isSwinging = false;
        isSwungToLeft = false;
        isInAttackPose = false;
        nextAttackTime = 0f;

        SetTrailEmitting(false, true);

        if (weaponVisual != null && hasCachedDefaultPose)
        {
            weaponVisual.localPosition = defaultLocalPosition;
            weaponVisual.localRotation = defaultLocalRotation;
        }
    }

    private void Update()
    {
        if (!isEquipped || PlayerStats == null || Mouse.current == null)
            return;

        bool wantGuard = Mouse.current.rightButton.isPressed && !PlayerStats.IsDead;

        if (wantGuard)
        {
            EnterOrMaintainGuard();
            return;
        }

        if (isGuarding)
        {
            ExitGuard();
        }

        // 가드 중이 아닐 때만 좌클릭 공격 허용
        if (Mouse.current.leftButton.isPressed)
        {
            TryAttack();
        }

        if (!isSwinging && weaponVisual != null)
        {
            if (Time.time < nextAttackTime)
            {
                float holdAngle = GetEndAngle(isSwungToLeft);
                EvaluateArcPose(holdAngle, out Vector3 holdPos, out Quaternion holdRot);
                weaponVisual.localPosition = Vector3.Lerp(weaponVisual.localPosition, holdPos, returnToDefaultSpeed * Time.deltaTime);
                weaponVisual.localRotation = Quaternion.Slerp(weaponVisual.localRotation, holdRot, returnToDefaultSpeed * Time.deltaTime);
            }
            else if (hasCachedDefaultPose)
            {
                isSwungToLeft = false;
                isInAttackPose = false;
                weaponVisual.localPosition = Vector3.Lerp(weaponVisual.localPosition, defaultLocalPosition, returnToDefaultSpeed * Time.deltaTime);
                weaponVisual.localRotation = Quaternion.Slerp(weaponVisual.localRotation, defaultLocalRotation, returnToDefaultSpeed * Time.deltaTime);
            }
        }
    }

    private void EnterOrMaintainGuard()
    {
        if (!isGuarding)
        {
            isGuarding = true;

            // 휘두르는 중이었다면 즉시 중단하고 트레일 끄기
            if (swingCoroutine != null)
            {
                StopCoroutine(swingCoroutine);
                swingCoroutine = null;
            }
            isSwinging = false;
            isSwungToLeft = false;
            isInAttackPose = false;
            SetTrailEmitting(false, true);

            // 이동 속도 감소 및 받는 피해량 감소 적용
            float incomingDamageMultiplier = 1f - guardDamageReduction;
            PlayerStats.SetGuardModifiers(guardMoveSpeedMultiplier, incomingDamageMultiplier);
        }

        if (weaponVisual != null)
        {
            Quaternion targetGuardRot = Quaternion.Euler(guardLocalEuler);
            weaponVisual.localPosition = Vector3.Lerp(weaponVisual.localPosition, guardLocalPosition, guardTransitionSpeed * Time.deltaTime);
            weaponVisual.localRotation = Quaternion.Slerp(weaponVisual.localRotation, targetGuardRot, guardTransitionSpeed * Time.deltaTime);
        }
    }

    private void ExitGuard()
    {
        isGuarding = false;

        if (PlayerStats != null)
        {
            PlayerStats.ResetGuardModifiers();
        }
    }

    private void TryAttack()
    {
        if (isGuarding || Time.time < nextAttackTime)
            return;

        float meleeSpeedStat = Mathf.Max(0.1f, PlayerStats.MeleeAttackSpeed);
        float finalInterval = baseAttackInterval / meleeSpeedStat;

        if (!isInAttackPose && snapToForwardStartOnFirstSwing && weaponVisual != null)
        {
            SetTrailEmitting(false, true);

            isSwungToLeft = false;
            float startRightAngle = GetEndAngle(false);
            EvaluateArcPose(startRightAngle, out Vector3 forwardStartPos, out Quaternion forwardStartRot);
            weaponVisual.localPosition = forwardStartPos;
            weaponVisual.localRotation = forwardStartRot;
        }

        isInAttackPose = true;

        PerformAttack();

        isSwungToLeft = !isSwungToLeft;
        StartSwingAnimation(isSwungToLeft, meleeSpeedStat, finalInterval);

        nextAttackTime = Time.time + finalInterval;
    }

    private void StartSwingAnimation(bool toLeft, float speedStat, float maxAllowedDuration)
    {
        if (weaponVisual == null)
            return;

        if (swingCoroutine != null)
        {
            StopCoroutine(swingCoroutine);
        }

        float duration = Mathf.Min(baseSwingDuration / speedStat, maxAllowedDuration * 0.85f);
        duration = Mathf.Max(0.03f, duration);

        swingCoroutine = StartCoroutine(SwingRoutine(toLeft, duration));
    }

    private IEnumerator SwingRoutine(bool toLeft, float duration)
    {
        isSwinging = true;
        SetTrailEmitting(true, true);

        try
        {
            float startAngle = GetEndAngle(!toLeft);
            float endAngle = GetEndAngle(toLeft);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float curvedT = swingCurve.Evaluate(t);

                float currentAngle = Mathf.LerpUnclamped(startAngle, endAngle, curvedT);

                EvaluateArcPose(currentAngle, out Vector3 targetPos, out Quaternion targetRot);
                weaponVisual.localPosition = targetPos;
                weaponVisual.localRotation = targetRot;

                yield return null;
            }

            EvaluateArcPose(endAngle, out Vector3 finalPos, out Quaternion finalRot);
            weaponVisual.localPosition = finalPos;
            weaponVisual.localRotation = finalRot;
        }
        finally
        {
            SetTrailEmitting(false, false);
            isSwinging = false;
            swingCoroutine = null;
        }
    }

    private float GetEndAngle(bool isLeft)
    {
        return isLeft ? -swingHalfAngle : swingHalfAngle;
    }

    private Vector3 GetSwingCenterLocal()
    {
        Vector3 baseCenter = swingCenterPoint != null
            ? transform.InverseTransformPoint(swingCenterPoint.position)
            : Vector3.zero;

        return baseCenter + swingCenterOffset;
    }

    private void EvaluateArcPose(float yawAngle, out Vector3 pos, out Quaternion rot)
    {
        Vector3 pivot = GetSwingCenterLocal();

        float rad = yawAngle * Mathf.Deg2Rad;
        Vector3 radialDir = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));

        pos = pivot + radialDir * orbitRadius;
        rot = Quaternion.Euler(bladeLayAngle, yawAngle, bladeRollAngle);
    }

    private void PerformAttack()
    {
        if (attackPoint == null || PlayerStats == null)
            return;

        float damage = PlayerStats.AttackPower * damageMultiplier;

        Vector3 attackDirection = WeaponManager != null ? WeaponManager.transform.forward : transform.forward;
        Vector3 upAxis = WeaponManager != null ? WeaponManager.transform.up : transform.up;

        Quaternion boxRotation = Quaternion.LookRotation(attackDirection, upAxis);
        Vector3 boxCenter = attackPoint.position + attackDirection * (attackRange * 0.5f);
        Vector3 halfExtents = new Vector3(attackWidth * 0.5f, attackHeight * 0.5f, attackRange * 0.5f);

        Collider[] hits = Physics.OverlapBox(
            boxCenter,
            halfExtents,
            boxRotation,
            enemyMask,
            QueryTriggerInteraction.Ignore
        );

        HashSet<IHealth> hitTargets = new HashSet<IHealth>();

        foreach (Collider hit in hits)
        {
            if (hit == null)
                continue;

            IHealth health = hit.GetComponentInParent<IHealth>();
            if (health == null)
                continue;

            PlayerStats hitPlayer = hit.GetComponentInParent<PlayerStats>();
            if (hitPlayer == PlayerStats)
                continue;

            if (!hitTargets.Add(health))
                continue;

            if (health.IsDead)
                continue;

            health.TakeDamage(damage);
        }
    }

    private void OnValidate()
    {
        attackRange = Mathf.Max(0.1f, attackRange);
        attackWidth = Mathf.Max(0.1f, attackWidth);
        attackHeight = Mathf.Max(0.1f, attackHeight);
        damageMultiplier = Mathf.Max(0f, damageMultiplier);
        baseAttackInterval = Mathf.Max(0.01f, baseAttackInterval);

        orbitRadius = Mathf.Max(0.1f, orbitRadius);
        baseSwingDuration = Mathf.Max(0.02f, baseSwingDuration);
        returnToDefaultSpeed = Mathf.Max(1f, returnToDefaultSpeed);
        guardTransitionSpeed = Mathf.Max(1f, guardTransitionSpeed);
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
        {
            Vector3 forward = WeaponManager != null ? WeaponManager.transform.forward : transform.forward;
            Vector3 up = WeaponManager != null ? WeaponManager.transform.up : transform.up;
            Quaternion boxRot = Quaternion.LookRotation(forward, up);

            DrawBoxGizmo(attackPoint.position, forward, boxRot, attackRange, attackWidth, attackHeight, Color.red);
        }

        DrawSwingArcGizmo(Color.yellow);
    }

    private void DrawBoxGizmo(Vector3 start, Vector3 forward, Quaternion rotation, float range, float width, float height, Color color)
    {
        Matrix4x4 prevMatrix = Gizmos.matrix;
        Gizmos.color = color;

        Vector3 center = start + forward * (range * 0.5f);
        Vector3 size = new Vector3(width, height, range);

        Gizmos.matrix = Matrix4x4.TRS(center, rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, size);

        Gizmos.matrix = prevMatrix;
    }

    private void DrawSwingArcGizmo(Color color)
    {
        Gizmos.color = color;

        Vector3 localCenter = GetSwingCenterLocal();
        Vector3 worldCenter = transform.TransformPoint(localCenter);

        Gizmos.DrawWireSphere(worldCenter, 0.08f);

        int segments = 20;
        Vector3 prevPoint = Vector3.zero;

        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float angle = Mathf.Lerp(-swingHalfAngle, swingHalfAngle, t);
            float rad = angle * Mathf.Deg2Rad;

            Vector3 localPoint = localCenter + new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad)) * orbitRadius;
            Vector3 worldPoint = transform.TransformPoint(localPoint);

            if (i == 0 || i == segments)
            {
                Gizmos.DrawLine(worldCenter, worldPoint);
            }

            if (i > 0)
            {
                Gizmos.DrawLine(prevPoint, worldPoint);
            }

            prevPoint = worldPoint;
        }
    }
}