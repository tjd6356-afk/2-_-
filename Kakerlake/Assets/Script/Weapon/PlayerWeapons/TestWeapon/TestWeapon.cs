using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TestWeapon : WeaponBase
{
    public enum AttackMode { Horizontal, Vertical }

    [Header("Attack Mode")]
    [SerializeField] private AttackMode currentMode = AttackMode.Horizontal;
    public AttackMode CurrentMode => currentMode;
    public bool IsGuarding => isGuarding;
    public bool IsSwinging => isSwinging;
    public int CurrentComboStep => comboStep;

    [Header("References")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private Transform weaponVisual;
    [SerializeField] private TrailRenderer swingTrail;
    [SerializeField] private Transform swingCenterPoint;

    [Header("Horizontal Attack (횡베기)")]
    [SerializeField, Min(0.1f)] private float horizontalAttackRange = 4.2f;
    [SerializeField, Min(0.1f)] private float horizontalAttackWidth = 6.3f;
    [SerializeField, Min(0.1f)] private float horizontalAttackHeight = 1.5f;
    [SerializeField, Min(0f)] private float horizontalDamageMultiplier = 1f;
    [SerializeField, Min(0.01f)] private float horizontalAttackInterval = 0.5f;
    [SerializeField, Min(0.02f)] private float horizontalSwingDuration = 0.15f;
    [SerializeField, Range(10f, 150f)] private float horizontalSwingAngle = 100f;

    [Header("Vertical Attack (내려치기)")]
    [SerializeField, Min(0.1f)] private float verticalAttackRange = 4.5f;
    [SerializeField, Min(0.1f)] private float verticalAttackWidth = 1.5f;
    [SerializeField, Min(0.1f)] private float verticalAttackHeight = 4f;
    [SerializeField, Min(0f)] private float verticalDamageMultiplier = 1.5f;
    [SerializeField, Min(0.02f)] private float verticalSwingDuration = 0.2f;
    [SerializeField] private float verticalStartAngle = -80f;
    [SerializeField] private float verticalEndAngle = 65f;

    [Header("Swing Animation")]
    [SerializeField] private Vector3 swingCenterOffset = new Vector3(0f, 0.7f, 0.5f);
    [SerializeField, Min(0.1f)] private float orbitRadius = 1.4f;
    [SerializeField, Min(0.01f)] private float horizontalPrepareDuration = 0.08f;
    [SerializeField, Min(0.01f)] private float verticalPrepareDuration = 0.2f;
    [SerializeField] private Vector3 bladeRotationOffset = new Vector3(90f, 0f, 0f);
    [SerializeField, Min(1f)] private float returnToDefaultSpeed = 14f;
    [SerializeField] private AnimationCurve swingCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Vertical Combo (3단 콤보)")]
    [SerializeField, Min(0.01f)] private float comboInputWindow = 0.4f;
    [SerializeField, Min(0f)] private float firstAttackPause = 0.15f;
    [SerializeField, Min(0f)] private float firstAdvanceDistance = 1.2f;
    [SerializeField, Min(0f)] private float secondAttackPause = 0.10f;
    [SerializeField, Min(0.01f)] private float secondRaiseDuration = 0.12f;
    [SerializeField, Min(0.02f)] private float secondSwingDuration = 0.17f;
    [SerializeField, Min(0f)] private float secondAdvanceDistance = 1.6f;
    [SerializeField, Min(0f)] private float secondDamageMultiplier = 1.8f;
    [SerializeField, Min(0.01f)] private float finisherPrepareDuration = 0.12f;
    [SerializeField, Min(0.02f)] private float finisherSpinDuration = 0.42f;
    [SerializeField, Min(0.1f)] private float finisherRadius = 3.5f;
    [SerializeField, Min(0.1f)] private float finisherHeight = 2f;
    [SerializeField, Min(0f)] private float finisherDamageMultiplier = 2.5f;
    [SerializeField, Min(0f)] private float finisherRecovery = 0.25f;

    [Header("Guard Settings (우클릭 가드)")]
    [SerializeField, Range(0f, 1f)] private float guardMoveSpeedMultiplier = 0.5f;
    [SerializeField, Range(0f, 1f)] private float guardDamageReduction = 0.6f;
    [SerializeField] private Vector3 guardLocalPosition = new Vector3(0.35f, 1.15f, 0.55f);
    [SerializeField] private Vector3 guardLocalEuler = new Vector3(15f, 0f, 80f);
    [SerializeField, Min(1f)] private float guardTransitionSpeed = 16f;

    [Header("Trail Settings")]
    [SerializeField] private bool alignTrailToSwingPlane = true;
    [Header("Hit Settings")]
    [SerializeField] private LayerMask enemyMask = ~0;
    [SerializeField] private LayerMask doorMask = ~0;
    [SerializeField, Min(0f)] private float doorImpactSpeed = 30f;

    [Header("Runtime Combo")]
    [SerializeField] private int comboStep;
    [SerializeField] private bool queuedComboAttack;
    [SerializeField] private float comboTimeRemaining;
    private float comboDeadline;
    private float nextAttackTime;
    private bool isEquipped, isSwinging, isGuarding;
    private bool nextSwingToLeft = true;
    private Coroutine swingCoroutine;
    private Vector3 defaultLocalPosition;
    private Quaternion defaultLocalRotation, defaultTrailRotation;
    private bool poseCached;
    private MeleeAttackMotion attackMotion;

    private void Awake()
    {
        CacheDefaultPose();
        SetTrail(false, true);
    }

    private void CacheDefaultPose()
    {
        if (poseCached || weaponVisual == null) return;
        defaultLocalPosition = weaponVisual.localPosition;
        defaultLocalRotation = weaponVisual.localRotation;
        if (swingTrail == null)
            swingTrail = weaponVisual.GetComponentInChildren<TrailRenderer>(true);
        if (swingTrail != null)
            defaultTrailRotation = swingTrail.transform.localRotation;
        poseCached = true;
    }

    public override void Equip()
    {
        CacheDefaultPose();
        base.Equip();
        CancelSwing();
        ExitGuard();
        isEquipped = true;
        currentMode = AttackMode.Horizontal;
        nextSwingToLeft = true;
        nextAttackTime = 0f;
        attackMotion = PlayerStats != null ? PlayerStats.GetComponentInParent<MeleeAttackMotion>() : null;
        if (attackMotion == null && WeaponManager != null)
            attackMotion = WeaponManager.GetComponentInParent<MeleeAttackMotion>();
        if (attackMotion == null)
            Debug.LogWarning("[TestWeapon] MeleeAttackMotion이 없어 전진/플레이어 모델 회전은 적용되지 않습니다.", this);
    }

    public override void Unequip()
    {
        isEquipped = false;
        CancelSwing();
        ExitGuard();
        base.Unequip();
    }

    private void OnDisable()
    {
        CancelSwing();
        ExitGuard();
    }

    private void Update()
    {
        if (!isEquipped || PlayerStats == null) return;
        if (PlayerGameplayControl.GameplayLocked || PlayerStats.IsDead || Mouse.current == null)
        {
            if (isSwinging || comboStep != 0 || isGuarding)
            {
                CancelSwing();
                ExitGuard();
            }
            return;
        }

        // 가드가 공격보다 우선한다.
        if (Mouse.current.rightButton.isPressed)
        {
            EnterOrMaintainGuard();
            return;
        }
        ExitGuard();

        if (!isSwinging && Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame)
        {
            ResetCombo();
            currentMode = currentMode == AttackMode.Horizontal ? AttackMode.Vertical : AttackMode.Horizontal;
            nextSwingToLeft = true;
            SetTrail(false, true);
            Debug.Log($"[TestWeapon] 공격 모드 : {currentMode}", this);
        }

        if (currentMode == AttackMode.Horizontal)
        {
            if (!isSwinging && Time.time >= nextAttackTime && Mouse.current.leftButton.isPressed)
                StartHorizontalAttack();
        }
        else
        {
            // 공격이 끝난 뒤에만 콤보 제한시간이 흐른다.
            if (!isSwinging && comboStep != 0)
            {
                comboTimeRemaining = Mathf.Max(0f, comboDeadline - Time.time);
                if (Time.time > comboDeadline) ResetCombo();
            }

            // 홀드가 아니라 새 클릭만 콤보 입력으로 인정한다.
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (isSwinging)
                {
                    // 한 공격에 한 번만 다음 타격을 예약한다.
                    if (comboStep == 1 || comboStep == 2)
                        queuedComboAttack = true;
                }
                else if (Time.time >= nextAttackTime)
                {
                    StartVerticalAttack(comboStep + 1);
                }
            }
        }

        // 1, 2타 직후 콤보 대기 중에는 마지막 자세 유지.
        if (!isSwinging && comboStep == 0 && Time.time >= nextAttackTime)
            ReturnToDefaultPose();
    }

    private bool CanAttack() => weaponVisual != null && attackPoint != null && PlayerStats != null;
    private float AttackSpeed => Mathf.Max(0.1f, PlayerStats.MeleeAttackSpeed);
    private static float Duration(float baseTime, float speed) => Mathf.Max(0.01f, baseTime / speed);

    private void StartHorizontalAttack()
    {
        if (!CanAttack()) return;
        bool toLeft = nextSwingToLeft;
        nextSwingToLeft = !nextSwingToLeft;
        float speed = AttackSpeed;
        float prep = Duration(horizontalPrepareDuration, speed);
        float strike = Duration(horizontalSwingDuration, speed);
        nextAttackTime = Time.time + Mathf.Max(horizontalAttackInterval / speed, prep + strike);
        isSwinging = true;
        swingCoroutine = StartCoroutine(HorizontalRoutine(toLeft, prep, strike));
    }

    private IEnumerator HorizontalRoutine(bool toLeft, float prep, float strike)
    {
        float start = toLeft ? horizontalSwingAngle : -horizontalSwingAngle;
        float end = -start;
        SetTrail(false, true);
        yield return MoveToArcPose(false, start, prep);
        AlignTrail(false, start);
        SetTrail(true, true);
        yield return Sweep(false, start, end, strike, () => PerformAttack(false, toLeft, horizontalDamageMultiplier));
        SetTrail(false);
        isSwinging = false;
        swingCoroutine = null;
    }

    private void StartVerticalAttack(int step)
    {
        if (!CanAttack() || step < 1 || step > 3) return;
        comboStep = step;
        queuedComboAttack = false;
        comboTimeRemaining = 0f;
        comboDeadline = 0f;
        isSwinging = true;
        swingCoroutine = StartCoroutine(VerticalComboRoutine(step, AttackSpeed));
    }

    private IEnumerator VerticalComboRoutine(int step, float speed)
    {
        SetTrail(false, true);
        // 평상시 이동 입력의 수평 성분을 잠시 대체한다. 중력/수직 이동은 기존 코드가 담당한다.
        if (attackMotion != null) attackMotion.Hold(this);

        if (step == 1)
        {
            yield return MoveToArcPose(true, verticalStartAngle, Duration(verticalPrepareDuration, speed));
            if (firstAttackPause > 0f) yield return new WaitForSeconds(firstAttackPause / speed);
            float strike = Duration(verticalSwingDuration, speed);
            BeginAdvance(firstAdvanceDistance, strike);
            AlignTrail(true, verticalStartAngle);
            SetTrail(true, true);
            yield return Sweep(true, verticalStartAngle, verticalEndAngle, strike,
                () => PerformAttack(true, false, verticalDamageMultiplier));
        }
        else if (step == 2)
        {
            // 1타 마지막 자세에서 잠시 정지한 후 빠르게 다시 들어올린다.
            if (secondAttackPause > 0f) yield return new WaitForSeconds(secondAttackPause / speed);
            yield return MoveToArcPose(true, verticalStartAngle, Duration(secondRaiseDuration, speed));
            float strike = Duration(secondSwingDuration, speed);
            BeginAdvance(secondAdvanceDistance, strike);
            AlignTrail(true, verticalStartAngle);
            SetTrail(true, true);
            yield return Sweep(true, verticalStartAngle, verticalEndAngle, strike,
                () => PerformAttack(true, false, secondDamageMultiplier));
        }
        else
        {
            yield return MoveToArcPose(false, 0f, Duration(finisherPrepareDuration, speed));
            AlignTrail(false, 0f);
            SetTrail(true, true);
            yield return SpinRoutine(Duration(finisherSpinDuration, speed));
        }

        SetTrail(false);
        if (attackMotion != null) attackMotion.Release(this);
        isSwinging = false;
        swingCoroutine = null;

        if (step == 3)
        {
            ResetCombo();
            nextSwingToLeft = true;
            nextAttackTime = Time.time + finisherRecovery / speed;
            yield break;
        }

        // 새 타격 입력이 이미 예약되었다면 즉시 다음 단계 실행.
        bool buffered = queuedComboAttack;
        queuedComboAttack = false;
        comboDeadline = Time.time + comboInputWindow;
        comboTimeRemaining = comboInputWindow;
        if (buffered) StartVerticalAttack(step + 1);
    }

    private void BeginAdvance(float distance, float duration)
    {
        if (attackMotion != null)
            attackMotion.BeginAdvance(this, AttackFrame.forward, distance, duration);
    }

    private IEnumerator MoveToArcPose(bool vertical, float angle, float duration)
    {
        Vector3 fromPos = weaponVisual.localPosition;
        Quaternion fromRot = weaponVisual.localRotation;
        EvaluateArcPose(vertical, angle, out Vector3 toPos, out Quaternion toRot);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            weaponVisual.localPosition = Vector3.Lerp(fromPos, toPos, t);
            weaponVisual.localRotation = Quaternion.Slerp(fromRot, toRot, t);
            yield return null;
        }
        ApplyArcPose(vertical, angle);
    }

    private IEnumerator Sweep(bool vertical, float start, float end, float duration, System.Action onHit)
    {
        float elapsed = 0f;
        bool applied = false;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float curved = Mathf.Clamp01(swingCurve.Evaluate(t));
            float angle = Mathf.Lerp(start, end, curved);
            ApplyArcPose(vertical, angle);
            if (!applied && curved >= 0.5f)
            {
                applied = true;
                onHit();
            }
            yield return null;
        }
        ApplyArcPose(vertical, end);
        if (!applied) onHit();
    }

    private IEnumerator SpinRoutine(float duration)
    {
        float elapsed = 0f;
        // 매 타격마다 하나의 HashSet: 같은 적/문은 회전 중 한 번만 맞는다.
        var targets = new HashSet<IHealth>();
        var doors = new HashSet<DoorController>();
        var slidingDoors = new HashSet<SlidingDoorController>();
        if (attackMotion != null) attackMotion.BeginVisualSpin(this);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float angle = t * 360f;
            ApplyArcPose(false, angle);
            if (attackMotion != null) attackMotion.SetVisualSpin(this, angle);
            PerformFinisher(targets, doors, slidingDoors);
            yield return null;
        }
        ApplyArcPose(false, 360f);
        if (attackMotion != null) attackMotion.SetVisualSpin(this, 360f);
    }

    private Transform AttackFrame => WeaponManager != null ? WeaponManager.transform : transform;

    private Vector3 GetSwingCenterLocal()
    {
        return (swingCenterPoint != null ? transform.InverseTransformPoint(swingCenterPoint.position) : Vector3.zero) + swingCenterOffset;
    }

    private void EvaluateArcPose(bool vertical, float angle, out Vector3 pos, out Quaternion rot)
    {
        Quaternion arc = Quaternion.AngleAxis(angle, vertical ? Vector3.right : Vector3.up);
        pos = GetSwingCenterLocal() + arc * Vector3.forward * orbitRadius;
        rot = arc * Quaternion.Euler(bladeRotationOffset);
    }

    private void ApplyArcPose(bool vertical, float angle)
    {
        EvaluateArcPose(vertical, angle, out Vector3 pos, out Quaternion rot);
        weaponVisual.localPosition = pos;
        weaponVisual.localRotation = rot;
        AlignTrail(vertical, angle);
    }

    private void SetTrail(bool emit, bool clear = false)
    {
        if (swingTrail == null) return;
        if (clear) swingTrail.Clear();
        swingTrail.emitting = emit;
    }

    private void AlignTrail(bool vertical, float angle)
    {
        if (!alignTrailToSwingPlane || swingTrail == null || swingTrail.transform == weaponVisual) return;
        Vector3 axis = vertical ? Vector3.right : Vector3.up;
        Vector3 normal = transform.TransformDirection(axis);
        Vector3 radial = transform.TransformDirection(Quaternion.AngleAxis(angle, axis) * Vector3.forward);
        swingTrail.transform.rotation = Quaternion.LookRotation(-normal, radial);
    }

    private void PerformAttack(bool vertical, bool toLeft, float multiplier)
    {
        if (!CanAttack() || PlayerStats.IsDead || PlayerGameplayControl.GameplayLocked) return;
        float range = vertical ? verticalAttackRange : horizontalAttackRange;
        float width = vertical ? verticalAttackWidth : horizontalAttackWidth;
        float height = vertical ? verticalAttackHeight : horizontalAttackHeight;
        Transform frame = AttackFrame;
        Vector3 forward = frame.forward;
        Vector3 center = attackPoint.position + forward * (range * 0.5f);
        Vector3 half = new Vector3(width, height, range) * 0.5f;
        Vector3 direction = vertical ? forward : (forward + frame.right * (toLeft ? -0.45f : 0.45f)).normalized;
        Collider[] hits = Physics.OverlapBox(center, half, frame.rotation, enemyMask.value | doorMask.value, QueryTriggerInteraction.Ignore);
        ApplyHits(hits, center, direction, multiplier, false, new HashSet<IHealth>(), new HashSet<DoorController>(), new HashSet<SlidingDoorController>());
    }

    private Vector3 FinisherCenter => (PlayerStats != null ? PlayerStats.transform.position : AttackFrame.position)
        + Vector3.up * Mathf.Max(0f, swingCenterOffset.y);

    private void PerformFinisher(HashSet<IHealth> targets, HashSet<DoorController> doors, HashSet<SlidingDoorController> slidingDoors)
    {
        if (!CanAttack() || PlayerStats.IsDead || PlayerGameplayControl.GameplayLocked) return;
        Vector3 center = FinisherCenter;
        // 원기둥 범위 판정: 넓게 탐색한 후 수평 거리와 높이를 따로 확인.
        Collider[] hits = Physics.OverlapBox(center, new Vector3(finisherRadius, finisherHeight * 0.5f, finisherRadius), Quaternion.identity,
            enemyMask.value | doorMask.value, QueryTriggerInteraction.Ignore);
        ApplyHits(hits, center, AttackFrame.forward, finisherDamageMultiplier, true, targets, doors, slidingDoors);
    }

    private void ApplyHits(Collider[] hits, Vector3 center, Vector3 direction, float multiplier, bool radial,
        HashSet<IHealth> targets, HashSet<DoorController> doors, HashSet<SlidingDoorController> slidingDoors)
    {
        float damage = PlayerStats.AttackPower * Mathf.Max(0f, multiplier);
        foreach (Collider hit in hits)
        {
            if (hit == null || hit.GetComponentInParent<PlayerStats>() == PlayerStats) continue;
            Vector3 point = GetClosestPointSafe(hit, center);
            Vector3 impactDirection = direction;
            if (radial)
            {
                Vector3 flat = point - center;
                flat.y = 0f;
                if (flat.sqrMagnitude > finisherRadius * finisherRadius) continue;
                if (flat.sqrMagnitude > 0.0001f) impactDirection = flat.normalized;
            }
            int layerBit = 1 << hit.gameObject.layer;
            if ((doorMask.value & layerBit) != 0)
            {
                DoorController door = hit.GetComponentInParent<DoorController>();
                if (door != null)
                {
                    if (doors.Add(door)) door.HitByMelee(impactDirection, point, doorImpactSpeed);
                    continue;
                }
                SlidingDoorController sliding = hit.GetComponentInParent<SlidingDoorController>();
                if (sliding != null)
                {
                    if (slidingDoors.Add(sliding)) sliding.HitByProjectile(impactDirection * doorImpactSpeed, point);
                    continue;
                }
            }
            if ((enemyMask.value & layerBit) == 0) continue;
            IHealth health = hit.GetComponentInParent<IHealth>();
            if (health == null || health.IsDead || !targets.Add(health)) continue;
            health.TakeDamage(damage);
        }
    }

    private Vector3 GetClosestPointSafe(Collider col, Vector3 point)
    {
        if (col is BoxCollider || col is SphereCollider || col is CapsuleCollider || (col is MeshCollider mesh && mesh.convex))
            return col.ClosestPoint(point);
        return col.ClosestPointOnBounds(point);
    }

    private void EnterOrMaintainGuard()
    {
        if (!isGuarding)
        {
            CancelSwing();
            isGuarding = true;
            nextSwingToLeft = true;
            PlayerStats.SetGuardModifiers(guardMoveSpeedMultiplier, 1f - guardDamageReduction);
        }
        if (weaponVisual == null) return;
        float t = 1f - Mathf.Exp(-guardTransitionSpeed * Time.deltaTime);
        weaponVisual.localPosition = Vector3.Lerp(weaponVisual.localPosition, guardLocalPosition, t);
        weaponVisual.localRotation = Quaternion.Slerp(weaponVisual.localRotation, Quaternion.Euler(guardLocalEuler), t);
    }

    private void ExitGuard()
    {
        if (!isGuarding) return;
        isGuarding = false;
        if (PlayerStats != null) PlayerStats.ResetGuardModifiers();
    }

    private void ResetCombo()
    {
        comboStep = 0;
        queuedComboAttack = false;
        comboDeadline = 0f;
        comboTimeRemaining = 0f;
    }

    private void ReturnToDefaultPose()
    {
        if (!poseCached || weaponVisual == null) return;
        float t = 1f - Mathf.Exp(-returnToDefaultSpeed * Time.deltaTime);
        weaponVisual.localPosition = Vector3.Lerp(weaponVisual.localPosition, defaultLocalPosition, t);
        weaponVisual.localRotation = Quaternion.Slerp(weaponVisual.localRotation, defaultLocalRotation, t);
    }

    private void CancelSwing()
    {
        if (swingCoroutine != null) StopCoroutine(swingCoroutine);
        swingCoroutine = null;
        isSwinging = false;
        ResetCombo();
        if (attackMotion != null) attackMotion.Release(this);
        SetTrail(false, true);
        if (poseCached && weaponVisual != null)
        {
            weaponVisual.localPosition = defaultLocalPosition;
            weaponVisual.localRotation = defaultLocalRotation;
        }
        if (poseCached && swingTrail != null && swingTrail.transform != weaponVisual)
            swingTrail.transform.localRotation = defaultTrailRotation;
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
        {
            bool vertical = currentMode == AttackMode.Vertical;
            float range = vertical ? verticalAttackRange : horizontalAttackRange;
            float width = vertical ? verticalAttackWidth : horizontalAttackWidth;
            float height = vertical ? verticalAttackHeight : horizontalAttackHeight;
            Transform frame = AttackFrame;
            Matrix4x4 old = Gizmos.matrix;
            Gizmos.color = Color.red;
            Gizmos.matrix = Matrix4x4.TRS(attackPoint.position + frame.forward * range * 0.5f, frame.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(width, height, range));
            Gizmos.matrix = old;
        }
        DrawArc(false, -horizontalSwingAngle, horizontalSwingAngle, Color.yellow);
        DrawArc(true, verticalStartAngle, verticalEndAngle, Color.cyan);
        Gizmos.color = Color.magenta;
        Vector3 center = FinisherCenter;
        DrawRing(center + Vector3.up * finisherHeight * 0.5f, finisherRadius);
        DrawRing(center - Vector3.up * finisherHeight * 0.5f, finisherRadius);
    }

    private void DrawArc(bool vertical, float start, float end, Color color)
    {
        Gizmos.color = color;
        Vector3 prev = Vector3.zero;
        for (int i = 0; i <= 30; i++)
        {
            EvaluateArcPose(vertical, Mathf.Lerp(start, end, i / 30f), out Vector3 pos, out _);
            Vector3 p = transform.TransformPoint(pos);
            if (i > 0) Gizmos.DrawLine(prev, p);
            prev = p;
        }
    }

    private void DrawRing(Vector3 center, float radius)
    {
        Vector3 previous = center + Vector3.right * radius;
        for (int i = 1; i <= 48; i++)
        {
            float a = i * Mathf.PI * 2f / 48f;
            Vector3 p = center + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius;
            Gizmos.DrawLine(previous, p);
            previous = p;
        }
    }
}
