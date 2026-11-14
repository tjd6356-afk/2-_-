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

    [Tooltip("(선택) 휘두르기 중심 기준으로 삼을 빈 오브젝트. 비워두면 무기 프리팹 루트(0,0,0)를 기준으로 오프셋만 적용합니다.")]
    [SerializeField]
    private Transform swingCenterPoint;

    [Header("Normal Stats (비조준 공격 설정)")]
    [Min(0f)]
    [SerializeField]
    private float attackRange = 1.5f;

    [Min(0.01f)]
    [SerializeField]
    private float attackRadius = 1.0f;

    [Min(0f)]
    [SerializeField]
    private float damageMultiplier = 1f;

    [Min(0.01f)]
    [SerializeField]
    private float baseAttackInterval = 0.5f;

    [Header("Aimed Stats (우클릭 조준 시 공격 설정)")]
    [Min(0f)]
    [SerializeField]
    private float aimedAttackRange = 2.5f;

    [Min(0.01f)]
    [SerializeField]
    private float aimedAttackRadius = 0.6f;

    [Min(0f)]
    [SerializeField]
    private float aimedDamageMultiplier = 1.2f;

    [Min(0.01f)]
    [SerializeField]
    private float aimedBaseAttackInterval = 0.7f;

    [Header("Aim")]
    [SerializeField]
    private float maxAimDistance = 100f;

    [SerializeField]
    private LayerMask aimMask = ~0;

    [SerializeField]
    private LayerMask enemyMask = ~0;

    // =========================================================
    // 휘두르기 중심 위치 및 원호(부채꼴) 궤도 설정
    // =========================================================
    [Header("Normal Swing Arc (비조준 휘두르기 중심 및 궤도)")]
    [Tooltip("비조준 시 휘두르기 중심 위치 오프셋 (X: 좌우, Y: 높이, Z: 앞뒤)")]
    [SerializeField]
    private Vector3 normalSwingCenterOffset = new Vector3(0f, 1.15f, 0.25f);

    [Tooltip("좌우로 휘두르는 반각 (80이면 오른쪽 +80° ~ 왼쪽 -80°, 총 160° 반원 회전)")]
    [Range(10f, 150f)]
    [SerializeField]
    private float normalSwingHalfAngle = 80f;

    [Tooltip("휘두르기 중심으로부터 무기가 떨어져 도는 원호 반경")]
    [Min(0.1f)]
    [SerializeField]
    private float normalOrbitRadius = 1.25f;

    [Header("Aimed Swing Arc (조준 시 휘두르기 중심 및 궤도)")]
    [Tooltip("조준 시 휘두르기 중심 위치 오프셋 (X: 좌우, Y: 높이, Z: 앞뒤)")]
    [SerializeField]
    private Vector3 aimedSwingCenterOffset = new Vector3(0f, 1.15f, 0.35f);

    [Tooltip("조준 시 좌우로 휘두르는 반각")]
    [Range(10f, 150f)]
    [SerializeField]
    private float aimedSwingHalfAngle = 60f;

    [Tooltip("조준 시 원호 반경")]
    [Min(0.1f)]
    [SerializeField]
    private float aimedOrbitRadius = 1.35f;

    [Header("Blade Orientation & Timing (무기 눕힘 각도 및 속도)")]
    [Tooltip("무기를 눕히는 X축 각도 (90 = 지면과 완전히 수평)")]
    [SerializeField]
    private float bladeLayAngle = 90f;

    [Tooltip("칼날 기울기 Z축 각도 (0 = 수평 횡베기, 값을 주면 대각선 베기)")]
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

    private ThirdPersonCamera thirdPersonCam;
    private float nextAttackTime;
    private bool isSwungToLeft;
    private bool isSwinging;
    private bool isInAttackPose;
    private Coroutine swingCoroutine;

    // 무기에 위치/회전값을 부여하기 전의 원본 기본 자세
    private Vector3 defaultLocalPosition;
    private Quaternion defaultLocalRotation;
    private bool hasCachedDefaultPose;

    public bool IsAiming =>
        (thirdPersonCam != null && thirdPersonCam.IsAiming) ||
        (Mouse.current != null && Mouse.current.rightButton.isPressed);

    private void Awake()
    {
        CacheDefaultPose();
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
        base.Equip();

        if (PlayerCamera != null)
        {
            thirdPersonCam = PlayerCamera.GetComponent<ThirdPersonCamera>();
        }

        ResetWeaponPoseState();
    }

    public override void Unequip()
    {
        if (swingCoroutine != null)
        {
            StopCoroutine(swingCoroutine);
            swingCoroutine = null;
        }

        ResetWeaponPoseState();
        base.Unequip();
    }

    private void ResetWeaponPoseState()
    {
        isSwinging = false;
        isSwungToLeft = false;
        isInAttackPose = false;
        nextAttackTime = 0f;

        if (weaponVisual != null && hasCachedDefaultPose)
        {
            weaponVisual.localPosition = defaultLocalPosition;
            weaponVisual.localRotation = defaultLocalRotation;
        }
    }

    private void Update()
    {
        if (PlayerCamera == null || PlayerStats == null || Mouse.current == null)
            return;

        if (Mouse.current.leftButton.isPressed)
        {
            TryAttack(IsAiming);
        }

        if (!isSwinging && weaponVisual != null)
        {
            // 공격 간격(쿨타임) 동안에는 직전 스윙 끝 각도를 유지해 연속 공격 시 좌·우 교차 스윙
            if (Time.time < nextAttackTime)
            {
                float holdAngle = GetEndAngle(IsAiming, isSwungToLeft);
                EvaluateArcPose(IsAiming, holdAngle, out Vector3 holdPos, out Quaternion holdRot);
                weaponVisual.localPosition = Vector3.Lerp(weaponVisual.localPosition, holdPos, returnToDefaultSpeed * Time.deltaTime);
                weaponVisual.localRotation = Quaternion.Slerp(weaponVisual.localRotation, holdRot, returnToDefaultSpeed * Time.deltaTime);
            }
            // 공격을 멈춘 상태에서는 프리팹 원래의 기본 위치/회전값으로 복귀
            else if (hasCachedDefaultPose)
            {
                isSwungToLeft = false;
                isInAttackPose = false;
                weaponVisual.localPosition = Vector3.Lerp(weaponVisual.localPosition, defaultLocalPosition, returnToDefaultSpeed * Time.deltaTime);
                weaponVisual.localRotation = Quaternion.Slerp(weaponVisual.localRotation, defaultLocalRotation, returnToDefaultSpeed * Time.deltaTime);
            }
        }
    }

    private void TryAttack(bool isAiming)
    {
        if (Time.time < nextAttackTime)
            return;

        float meleeSpeedStat = Mathf.Max(0.1f, PlayerStats.MeleeAttackSpeed);
        float currentBaseInterval = isAiming ? aimedBaseAttackInterval : baseAttackInterval;
        float finalInterval = currentBaseInterval / meleeSpeedStat;

        // 대기 자세에서 첫 휘두르기를 시작할 때 전방 오른쪽 시작 위치로 먼저 배치
        if (!isInAttackPose && snapToForwardStartOnFirstSwing && weaponVisual != null)
        {
            isSwungToLeft = false;
            float startRightAngle = GetEndAngle(isAiming, false);
            EvaluateArcPose(isAiming, startRightAngle, out Vector3 forwardStartPos, out Quaternion forwardStartRot);
            weaponVisual.localPosition = forwardStartPos;
            weaponVisual.localRotation = forwardStartRot;
        }

        isInAttackPose = true;

        PerformAttack(isAiming);

        isSwungToLeft = !isSwungToLeft;
        StartSwingAnimation(isAiming, isSwungToLeft, meleeSpeedStat, finalInterval);

        nextAttackTime = Time.time + finalInterval;
    }

    private void StartSwingAnimation(bool isAiming, bool toLeft, float speedStat, float maxAllowedDuration)
    {
        if (weaponVisual == null)
            return;

        if (swingCoroutine != null)
        {
            StopCoroutine(swingCoroutine);
        }

        float duration = Mathf.Min(baseSwingDuration / speedStat, maxAllowedDuration * 0.85f);
        duration = Mathf.Max(0.03f, duration);

        swingCoroutine = StartCoroutine(SwingRoutine(isAiming, toLeft, duration));
    }

    private IEnumerator SwingRoutine(bool initialAiming, bool toLeft, float duration)
    {
        isSwinging = true;

        float startAngle = GetEndAngle(initialAiming, !toLeft);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float curvedT = swingCurve.Evaluate(t);

            bool currentAiming = IsAiming;
            float endAngle = GetEndAngle(currentAiming, toLeft);
            float currentAngle = Mathf.LerpUnclamped(startAngle, endAngle, curvedT);

            EvaluateArcPose(currentAiming, currentAngle, out Vector3 targetPos, out Quaternion targetRot);
            weaponVisual.localPosition = targetPos;
            weaponVisual.localRotation = targetRot;

            yield return null;
        }

        bool finalAiming = IsAiming;
        float finalAngle = GetEndAngle(finalAiming, toLeft);
        EvaluateArcPose(finalAiming, finalAngle, out Vector3 finalPos, out Quaternion finalRot);
        weaponVisual.localPosition = finalPos;
        weaponVisual.localRotation = finalRot;

        isSwinging = false;
        swingCoroutine = null;
    }

    private float GetEndAngle(bool isAiming, bool isLeft)
    {
        float halfAngle = isAiming ? aimedSwingHalfAngle : normalSwingHalfAngle;
        return isLeft ? -halfAngle : halfAngle;
    }

    private Vector3 GetSwingCenterLocal(bool isAiming)
    {
        Vector3 baseCenter = swingCenterPoint != null
            ? transform.InverseTransformPoint(swingCenterPoint.position)
            : Vector3.zero;

        return baseCenter + (isAiming ? aimedSwingCenterOffset : normalSwingCenterOffset);
    }

    // =========================================================
    // 극좌표 기반 원호 위치 및 방사형 회전 계산
    // =========================================================
    private void EvaluateArcPose(bool isAiming, float yawAngle, out Vector3 pos, out Quaternion rot)
    {
        float radius = isAiming ? aimedOrbitRadius : normalOrbitRadius;
        Vector3 pivot = GetSwingCenterLocal(isAiming);

        float rad = yawAngle * Mathf.Deg2Rad;
        Vector3 radialDir = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));

        pos = pivot + radialDir * radius;
        rot = Quaternion.Euler(bladeLayAngle, yawAngle, bladeRollAngle);
    }

    private void PerformAttack(bool isAiming)
    {
        if (attackPoint == null || PlayerStats == null)
            return;

        float currentDamageMultiplier = isAiming ? aimedDamageMultiplier : damageMultiplier;
        float damage = PlayerStats.AttackPower * currentDamageMultiplier;

        float currentRange = isAiming ? aimedAttackRange : attackRange;
        float currentRadius = isAiming ? aimedAttackRadius : attackRadius;

        Vector3 attackDirection = GetAttackDirection(isAiming);
        Vector3 startPoint = attackPoint.position;
        Vector3 endPoint = startPoint + attackDirection * currentRange;

        Collider[] hits = Physics.OverlapCapsule(
            startPoint,
            endPoint,
            currentRadius,
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

    private Vector3 GetAttackDirection(bool isAiming)
    {
        if (isAiming && PlayerCamera != null)
        {
            Ray aimRay = PlayerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            Vector3 targetPoint;

            if (Physics.Raycast(aimRay, out RaycastHit hit, maxAimDistance, aimMask, QueryTriggerInteraction.Ignore))
            {
                targetPoint = hit.point;
            }
            else
            {
                targetPoint = aimRay.GetPoint(maxAimDistance);
            }

            Vector3 toTarget = targetPoint - attackPoint.position;

            if (Vector3.Dot(toTarget, PlayerCamera.transform.forward) > 0.1f)
            {
                return toTarget.normalized;
            }

            return PlayerCamera.transform.forward;
        }

        return WeaponManager != null ? WeaponManager.transform.forward : transform.forward;
    }

    private void OnValidate()
    {
        attackRange = Mathf.Max(0f, attackRange);
        attackRadius = Mathf.Max(0.01f, attackRadius);
        damageMultiplier = Mathf.Max(0f, damageMultiplier);
        baseAttackInterval = Mathf.Max(0.01f, baseAttackInterval);

        aimedAttackRange = Mathf.Max(0f, aimedAttackRange);
        aimedAttackRadius = Mathf.Max(0.01f, aimedAttackRadius);
        aimedDamageMultiplier = Mathf.Max(0f, aimedDamageMultiplier);
        aimedBaseAttackInterval = Mathf.Max(0.01f, aimedBaseAttackInterval);

        normalOrbitRadius = Mathf.Max(0.1f, normalOrbitRadius);
        aimedOrbitRadius = Mathf.Max(0.1f, aimedOrbitRadius);

        maxAimDistance = Mathf.Max(1f, maxAimDistance);
        baseSwingDuration = Mathf.Max(0.02f, baseSwingDuration);
        returnToDefaultSpeed = Mathf.Max(1f, returnToDefaultSpeed);
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
        {
            Vector3 forward = WeaponManager != null ? WeaponManager.transform.forward : transform.forward;
            Vector3 up = WeaponManager != null ? WeaponManager.transform.up : transform.up;
            Vector3 right = WeaponManager != null ? WeaponManager.transform.right : transform.right;

            DrawCapsuleGizmo(attackPoint.position, forward, up, right, attackRange, attackRadius, Color.red);
            DrawCapsuleGizmo(attackPoint.position, forward, up, right, aimedAttackRange, aimedAttackRadius, Color.cyan);
        }

        // 휘두르기 중심점 및 부채꼴 원호 궤적 시각화
        DrawSwingArcGizmo(false, Color.yellow);
        DrawSwingArcGizmo(true, Color.green);
    }

    private void DrawSwingArcGizmo(bool isAiming, Color color)
    {
        Gizmos.color = color;

        Vector3 localCenter = GetSwingCenterLocal(isAiming);
        Vector3 worldCenter = transform.TransformPoint(localCenter);

        float halfAngle = isAiming ? aimedSwingHalfAngle : normalSwingHalfAngle;
        float radius = isAiming ? aimedOrbitRadius : normalOrbitRadius;

        // 중심점 표시
        Gizmos.DrawWireSphere(worldCenter, 0.08f);

        int segments = 20;
        Vector3 prevPoint = Vector3.zero;

        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float angle = Mathf.Lerp(-halfAngle, halfAngle, t);
            float rad = angle * Mathf.Deg2Rad;

            Vector3 localPoint = localCenter + new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad)) * radius;
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

    private void DrawCapsuleGizmo(Vector3 start, Vector3 forward, Vector3 up, Vector3 right, float range, float radius, Color color)
    {
        Gizmos.color = color;
        Vector3 end = start + forward * range;

        Gizmos.DrawWireSphere(start, radius);

        if (range > 0f)
        {
            Gizmos.DrawWireSphere(end, radius);
            Gizmos.DrawLine(start + up * radius, end + up * radius);
            Gizmos.DrawLine(start - up * radius, end - up * radius);
            Gizmos.DrawLine(start + right * radius, end + right * radius);
            Gizmos.DrawLine(start - right * radius, end - right * radius);
        }
    }
}