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

    // =========================================================
    // 스윙 애니메이션 포즈 (비조준 / 조준 각각 좌·우 포즈 지정)
    // =========================================================
    [Header("Normal Swing Poses (비조준 좌·우 포즈)")]
    [Tooltip("기본(오른쪽) 대기 및 짝수 공격 도착 위치")]
    [SerializeField]
    private Vector3 normalRightPosition = new Vector3(0.35f, -0.1f, 0.4f);

    [Tooltip("기본(오른쪽) 대기 및 짝수 공격 도착 각도")]
    [SerializeField]
    private Vector3 normalRightEuler = new Vector3(15f, 45f, -20f);

    [Tooltip("홀수 공격(1타) 후 멈춰 있을 왼쪽 위치")]
    [SerializeField]
    private Vector3 normalLeftPosition = new Vector3(-0.35f, -0.15f, 0.45f);

    [Tooltip("홀수 공격(1타) 후 멈춰 있을 왼쪽 각도")]
    [SerializeField]
    private Vector3 normalLeftEuler = new Vector3(20f, -65f, 35f);

    [Header("Aimed Swing Poses (조준 시 좌·우 포즈)")]
    [Tooltip("조준 시 오른쪽 대기 위치")]
    [SerializeField]
    private Vector3 aimedRightPosition = new Vector3(0.2f, -0.05f, 0.5f);

    [Tooltip("조준 시 오른쪽 대기 각도")]
    [SerializeField]
    private Vector3 aimedRightEuler = new Vector3(70f, 15f, 0f);

    [Tooltip("조준 시 1타 공격 후 멈춰 있을 위치 (찌르기 연출 시 Z값을 높게 설정)")]
    [SerializeField]
    private Vector3 aimedLeftPosition = new Vector3(0.15f, -0.05f, 0.95f);

    [Tooltip("조준 시 1타 공격 후 멈춰 있을 각도")]
    [SerializeField]
    private Vector3 aimedLeftEuler = new Vector3(85f, -15f, 0f);

    [Header("Swing Timing & Curve")]
    [Tooltip("공속 1.0 기준 휘두르는 데 걸리는 시간 (초)")]
    [Min(0.02f)]
    [SerializeField]
    private float baseSwingDuration = 0.16f;

    [Tooltip("스윙 가속/감속 곡선 (기본: 빠르게 휘두르고 끝에 절도 있게 멈춤)")]
    [SerializeField]
    private AnimationCurve swingCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("우클릭 조준 전환 시 자세가 바뀌는 속도")]
    [Min(1f)]
    [SerializeField]
    private float aimPoseTransitionSpeed = 14f;

    [Header("Layer Masks")]
    [SerializeField]
    private LayerMask enemyMask = ~0;

    [SerializeField]
    private LayerMask aimMask = ~0;

    private float nextAttackTime;
    private bool isSwungToLeft; // false: 오른쪽 자세, true: 왼쪽 자세
    private bool isSwinging;
    private Coroutine swingCoroutine;

    public bool IsAiming => Mouse.current != null && Mouse.current.rightButton.isPressed;

    public override void Equip()
    {
        base.Equip();
        nextAttackTime = 0f;
        isSwungToLeft = false;
        isSwinging = false;

        // 장착 즉시 오른쪽 기본 자세로 초기화
        if (weaponVisual != null)
        {
            weaponVisual.localPosition = normalRightPosition;
            weaponVisual.localRotation = Quaternion.Euler(normalRightEuler);
        }
    }

    public override void Unequip()
    {
        if (swingCoroutine != null)
        {
            StopCoroutine(swingCoroutine);
            swingCoroutine = null;
        }

        isSwinging = false;
        isSwungToLeft = false;
        nextAttackTime = 0f;
        base.Unequip();
    }

    private void Update()
    {
        if (PlayerStats == null || Mouse.current == null)
            return;

        if (Mouse.current.leftButton.isPressed)
        {
            TryAttack(IsAiming);
        }

        // 휘두르는 중이 아닐 때, 현재 스윙 방향(좌/우)을 유지한 채 비조준 <-> 조준 자세를 부드럽게 전환
        if (!isSwinging && weaponVisual != null)
        {
            GetTargetPose(IsAiming, isSwungToLeft, out Vector3 targetPos, out Quaternion targetRot);
            weaponVisual.localPosition = Vector3.Lerp(weaponVisual.localPosition, targetPos, aimPoseTransitionSpeed * Time.deltaTime);
            weaponVisual.localRotation = Quaternion.Slerp(weaponVisual.localRotation, targetRot, aimPoseTransitionSpeed * Time.deltaTime);
        }
    }

    private void TryAttack(bool isAiming)
    {
        if (Time.time < nextAttackTime)
            return;

        float meleeSpeedStat = Mathf.Max(0.1f, PlayerStats.MeleeAttackSpeed);
        float currentBaseInterval = isAiming ? aimedBaseAttackInterval : baseAttackInterval;
        float finalInterval = currentBaseInterval / meleeSpeedStat;

        // 1. 데미지 판정 실행
        PerformAttack(isAiming);

        // 2. 좌 <-> 우 스윙 방향 반전 후 해당 방향으로 스윙 애니메이션 실행
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

        // 공속에 비례해 스윙 시간을 줄이되, 다음 공격 간격(finalInterval)의 85%를 넘지 않도록 보정
        float duration = Mathf.Min(baseSwingDuration / speedStat, maxAllowedDuration * 0.85f);
        duration = Mathf.Max(0.03f, duration);

        swingCoroutine = StartCoroutine(SwingRoutine(isAiming, toLeft, duration));
    }

    private IEnumerator SwingRoutine(bool isAiming, bool toLeft, float duration)
    {
        isSwinging = true;

        Vector3 startPos = weaponVisual.localPosition;
        Quaternion startRot = weaponVisual.localRotation;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float curvedT = swingCurve.Evaluate(t);

            // 스윙 도중 우클릭 조준을 누르거나 떼어도 목표 좌/우 방향을 유지하며 자연스럽게 연결
            GetTargetPose(IsAiming, toLeft, out Vector3 targetPos, out Quaternion targetRot);

            weaponVisual.localPosition = Vector3.LerpUnclamped(startPos, targetPos, curvedT);
            weaponVisual.localRotation = Quaternion.SlerpUnclamped(startRot, targetRot, curvedT);

            yield return null;
        }

        // 도착 자세 고정 (되돌아가지 않고 멈춤)
        GetTargetPose(IsAiming, toLeft, out Vector3 finalPos, out Quaternion finalRot);
        weaponVisual.localPosition = finalPos;
        weaponVisual.localRotation = finalRot;

        isSwinging = false;
        swingCoroutine = null;
    }

    private void GetTargetPose(bool isAiming, bool isLeft, out Vector3 pos, out Quaternion rot)
    {
        if (isAiming)
        {
            pos = isLeft ? aimedLeftPosition : aimedRightPosition;
            rot = Quaternion.Euler(isLeft ? aimedLeftEuler : aimedRightEuler);
        }
        else
        {
            pos = isLeft ? normalLeftPosition : normalRightPosition;
            rot = Quaternion.Euler(isLeft ? normalLeftEuler : normalRightEuler);
        }
    }

    private void PerformAttack(bool isAiming)
    {
        if (attackPoint == null || PlayerStats == null)
            return;

        float currentDamageMultiplier = isAiming ? aimedDamageMultiplier : damageMultiplier;
        float damage = PlayerStats.AttackPower * currentDamageMultiplier;

        float currentRange = isAiming ? aimedAttackRange : attackRange;
        float currentRadius = isAiming ? aimedAttackRadius : attackRadius;

        Vector3 attackDirection = GetAttackDirection(isAiming, currentRange);
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

    private Vector3 GetAttackDirection(bool isAiming, float range)
    {
        if (isAiming && PlayerCamera != null)
        {
            float checkDistance = Mathf.Max(range + 5f, 20f);
            Ray aimRay = PlayerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            Vector3 targetPoint = Physics.Raycast(aimRay, out RaycastHit hit, checkDistance, aimMask, QueryTriggerInteraction.Ignore)
                ? hit.point
                : aimRay.GetPoint(checkDistance);

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

        baseSwingDuration = Mathf.Max(0.02f, baseSwingDuration);
        aimPoseTransitionSpeed = Mathf.Max(1f, aimPoseTransitionSpeed);
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null)
            return;

        Vector3 forward = WeaponManager != null ? WeaponManager.transform.forward : transform.forward;
        Vector3 up = WeaponManager != null ? WeaponManager.transform.up : transform.up;
        Vector3 right = WeaponManager != null ? WeaponManager.transform.right : transform.right;

        DrawCapsuleGizmo(attackPoint.position, forward, up, right, attackRange, attackRadius, Color.red);
        DrawCapsuleGizmo(attackPoint.position, forward, up, right, aimedAttackRange, aimedAttackRadius, Color.cyan);
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