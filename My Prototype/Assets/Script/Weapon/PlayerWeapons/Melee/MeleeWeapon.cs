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

    [Tooltip("좌우로 휘두르는 기본 반각 (80이면 오른쪽 +80° ~ 왼쪽 -80°까지 베기)")]
    [Range(10f, 150f)]
    [SerializeField]
    private float swingHalfAngle = 80f;

    [Tooltip("휘두르기 중심으로부터 무기가 떨어져 도는 원호 반경")]
    [Min(0.1f)]
    [SerializeField]
    private float orbitRadius = 1.25f;

    // =========================================================
    // 다음 공격 대비 감속 및 뒤로 내빼기 설정
    // =========================================================
    [Header("Follow-Through & Chambering (감속 및 다음 공격 대비 자세)")]
    [Tooltip("타격 지점을 지난 뒤 같은 방향으로 더 미끄러지며 회전하는 추가 각도 (되돌아오지 않음)")]
    [Range(0f, 45f)]
    [SerializeField]
    private float chamberExtraAngle = 16f;

    [Tooltip("다음 휘두르기를 대비해 무기를 뒤로 살짝 내빼는 위치 오프셋 (Z를 음수로 두면 뒤쪽으로 당김)")]
    [SerializeField]
    private Vector3 chamberPullOffset = new Vector3(0f, 0.05f, -0.14f);

    [Tooltip("공속 1.0 기준 실제 타격(트레일 ON)에 걸리는 시간 (초)")]
    [Min(0.02f)]
    [SerializeField]
    private float baseSwingDuration = 0.13f;

    [Tooltip("공속 1.0 기준 타격 후 감속하며 뒤로 내빼는 데 걸리는 시간 (초)")]
    [Min(0.01f)]
    [SerializeField]
    private float baseChamberDuration = 0.14f;

    // =========================================================
    // 칼날 방향 및 무기 눕힘 각도 설정
    // =========================================================
    [Header("Blade Orientation & Return (칼날 방향 및 무기 눕힘 각도)")]
    [Tooltip("휘두를 때 무기를 눕히는 X축 각도 (90 = 지면과 완전히 수평)")]
    [SerializeField]
    private float bladeLayAngle = 90f;

    [Tooltip("칼날이 옆면이 아닌 베는 방향을 향하도록 칼자루 축을 회전시키는 각도 (기본 90, 칼날이 반대로 향하면 -90, 이미 옆면이 날이면 0)")]
    [SerializeField]
    private float bladeEdgeAngle = 90f;

    [Tooltip("왕복 스윙(좌->우 / 우->좌) 시 칼날이 항상 진행 방향을 향하도록 손목을 반전시킬지 여부")]
    [SerializeField]
    private bool flipEdgeOnReturnSwing = true;

    [Tooltip("휘두를 때 칼날 기울기 Z축 각도 (0 = 수평 횡베기)")]
    [SerializeField]
    private float bladeRollAngle = 0f;

    [Tooltip("대기 상태에서 첫 공격 시 전방 시작 위치로 즉시 전환한 뒤 휘두를지 여부")]
    [SerializeField]
    private bool snapToForwardStartOnFirstSwing = true;

    [SerializeField]
    private AnimationCurve swingCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("공격 종료 후 원래 기본 자세로 되돌아가는 속도")]
    [Min(1f)]
    [SerializeField]
    private float returnToDefaultSpeed = 14f;

    private float nextAttackTime;
    private float currentYawAngle;
    private float currentChamberRatio;
    private float currentEdgeRoll;
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
        currentYawAngle = GetChamberedAngle(false);
        currentChamberRatio = 1f;
        currentEdgeRoll = GetTargetEdgeRoll(true);

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

        if (Mouse.current.leftButton.isPressed)
        {
            TryAttack();
        }

        if (!isSwinging && weaponVisual != null)
        {
            if (Time.time < nextAttackTime)
            {
                float holdAngle = GetChamberedAngle(isSwungToLeft);
                float holdEdgeRoll = GetTargetEdgeRoll(isSwungToLeft);

                currentYawAngle = Mathf.Lerp(currentYawAngle, holdAngle, returnToDefaultSpeed * Time.deltaTime);
                currentChamberRatio = Mathf.Lerp(currentChamberRatio, 1f, returnToDefaultSpeed * Time.deltaTime);
                currentEdgeRoll = Mathf.LerpAngle(currentEdgeRoll, holdEdgeRoll, returnToDefaultSpeed * Time.deltaTime);

                EvaluateArcPose(currentYawAngle, currentChamberRatio, currentEdgeRoll, out Vector3 holdPos, out Quaternion holdRot);
                weaponVisual.localPosition = Vector3.Lerp(weaponVisual.localPosition, holdPos, returnToDefaultSpeed * Time.deltaTime);
                weaponVisual.localRotation = Quaternion.Slerp(weaponVisual.localRotation, holdRot, returnToDefaultSpeed * Time.deltaTime);
            }
            else if (hasCachedDefaultPose)
            {
                isSwungToLeft = false;
                isInAttackPose = false;
                currentYawAngle = GetChamberedAngle(false);
                currentChamberRatio = 1f;
                currentEdgeRoll = GetTargetEdgeRoll(true);

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

            if (swingCoroutine != null)
            {
                StopCoroutine(swingCoroutine);
                swingCoroutine = null;
            }
            isSwinging = false;
            isSwungToLeft = false;
            isInAttackPose = false;
            currentYawAngle = GetChamberedAngle(false);
            currentChamberRatio = 1f;
            currentEdgeRoll = GetTargetEdgeRoll(true);
            SetTrailEmitting(false, true);

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
            currentYawAngle = GetChamberedAngle(false);
            currentChamberRatio = 1f;
            currentEdgeRoll = GetTargetEdgeRoll(true);

            EvaluateArcPose(currentYawAngle, currentChamberRatio, currentEdgeRoll, out Vector3 forwardStartPos, out Quaternion forwardStartRot);
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

        float rawMain = baseSwingDuration / speedStat;
        float rawChamber = baseChamberDuration / speedStat;
        float totalRaw = Mathf.Max(0.03f, rawMain + rawChamber);

        float maxTotal = Mathf.Max(0.03f, maxAllowedDuration * 0.92f);
        float scale = totalRaw > maxTotal ? (maxTotal / totalRaw) : 1f;

        float mainDuration = Mathf.Max(0.02f, rawMain * scale);
        float chamberDuration = Mathf.Max(0f, rawChamber * scale);

        swingCoroutine = StartCoroutine(SwingRoutine(toLeft, mainDuration, chamberDuration));
    }

    private IEnumerator SwingRoutine(bool toLeft, float mainDuration, float chamberDuration)
    {
        isSwinging = true;
        SetTrailEmitting(true, true);

        try
        {
            float startAngle = currentYawAngle;
            float startChamberRatio = currentChamberRatio;
            float startEdgeRoll = currentEdgeRoll;

            float strikeEndAngle = GetStrikeEndAngle(toLeft);
            float chamberedEndAngle = GetChamberedAngle(toLeft);
            float targetEdgeRoll = GetTargetEdgeRoll(toLeft);

            // =====================================================
            // 1단계: 칼날을 베는 방향으로 돌리며 타격 끝 각도까지 베기 (트레일 ON)
            // =====================================================
            float elapsed = 0f;
            while (elapsed < mainDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / mainDuration);
                float curvedT = swingCurve.Evaluate(t);

                // 스윙 초반(첫 30% 구간)에 손목을 빠르게 돌려 칼날이 베는 방향을 정면으로 향하게 정렬
                float edgeTurnT = Mathf.Clamp01(t * 3.5f);
                currentEdgeRoll = Mathf.LerpAngle(startEdgeRoll, targetEdgeRoll, edgeTurnT);

                currentYawAngle = Mathf.LerpUnclamped(startAngle, strikeEndAngle, curvedT);
                currentChamberRatio = Mathf.Lerp(startChamberRatio, 0f, curvedT);

                EvaluateArcPose(currentYawAngle, currentChamberRatio, currentEdgeRoll, out Vector3 targetPos, out Quaternion targetRot);
                weaponVisual.localPosition = targetPos;
                weaponVisual.localRotation = targetRot;

                yield return null;
            }

            currentEdgeRoll = targetEdgeRoll;
            SetTrailEmitting(false, false);

            // =====================================================
            // 2단계: 같은 방향으로 감속하며 무기를 뒤로 살짝 내빼기
            // =====================================================
            if (chamberDuration > 0.001f)
            {
                float chamberElapsed = 0f;
                while (chamberElapsed < chamberDuration)
                {
                    chamberElapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(chamberElapsed / chamberDuration);

                    float glideT = 1f - Mathf.Pow(1f - t, 2.5f);

                    currentYawAngle = Mathf.LerpUnclamped(strikeEndAngle, chamberedEndAngle, glideT);
                    currentChamberRatio = Mathf.Lerp(0f, 1f, glideT);

                    EvaluateArcPose(currentYawAngle, currentChamberRatio, currentEdgeRoll, out Vector3 chamberPos, out Quaternion chamberRot);
                    weaponVisual.localPosition = chamberPos;
                    weaponVisual.localRotation = chamberRot;

                    yield return null;
                }
            }

            currentYawAngle = chamberedEndAngle;
            currentChamberRatio = 1f;
            EvaluateArcPose(currentYawAngle, currentChamberRatio, currentEdgeRoll, out Vector3 finalPos, out Quaternion finalRot);
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

    private float GetTargetEdgeRoll(bool isLeft)
    {
        if (!flipEdgeOnReturnSwing)
            return bladeEdgeAngle;

        return isLeft ? -bladeEdgeAngle : bladeEdgeAngle;
    }

    private float GetStrikeEndAngle(bool isLeft)
    {
        return isLeft ? -swingHalfAngle : swingHalfAngle;
    }

    private float GetChamberedAngle(bool isLeft)
    {
        float totalAngle = swingHalfAngle + chamberExtraAngle;
        return isLeft ? -totalAngle : totalAngle;
    }

    private Vector3 GetSwingCenterLocal()
    {
        Vector3 baseCenter = swingCenterPoint != null
            ? transform.InverseTransformPoint(swingCenterPoint.position)
            : Vector3.zero;

        return baseCenter + swingCenterOffset;
    }

    private void EvaluateArcPose(float yawAngle, float chamberRatio, float edgeRollAngle, out Vector3 pos, out Quaternion rot)
    {
        Vector3 pivot = GetSwingCenterLocal();

        float rad = yawAngle * Mathf.Deg2Rad;
        Vector3 radialDir = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));

        Vector3 pullOffset = chamberPullOffset * chamberRatio;

        pos = pivot + radialDir * orbitRadius + pullOffset;

        // 1. 무기를 수평으로 눕히고 원호 바깥쪽(yawAngle)을 향하게 배치
        Quaternion arcOrientation = Quaternion.Euler(bladeLayAngle, yawAngle, bladeRollAngle);
        // 2. 칼자루(로컬 Y축)를 기준으로 칼날이 베는 방향을 향하도록 자전 회전
        Quaternion bladeTwist = Quaternion.Euler(0f, edgeRollAngle, 0f);

        rot = arcOrientation * bladeTwist;
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
        baseChamberDuration = Mathf.Max(0.01f, baseChamberDuration);
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

        float totalHalfAngle = swingHalfAngle + chamberExtraAngle;
        int segments = 20;
        Vector3 prevPoint = Vector3.zero;

        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float angle = Mathf.Lerp(-totalHalfAngle, totalHalfAngle, t);
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