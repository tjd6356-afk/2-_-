using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TestWeapon : WeaponBase
{
    // =========================================================
    // Attack Mode
    // =========================================================

    public enum AttackMode
    {
        Horizontal,
        Vertical
    }

    [Header("Attack Mode")]

    [SerializeField]
    private AttackMode currentMode = AttackMode.Horizontal;

    public AttackMode CurrentMode => currentMode;


    // =========================================================
    // References
    // =========================================================

    [Header("References")]

    [SerializeField]
    private Transform attackPoint;

    [SerializeField]
    private Transform weaponVisual;

    [SerializeField]
    private TrailRenderer swingTrail;

    [SerializeField]
    private Transform swingCenterPoint;


    // =========================================================
    // Horizontal Attack
    // =========================================================

    [Header("Horizontal Attack (횡베기)")]

    [SerializeField]
    private float horizontalAttackRange = 4.2f;

    [SerializeField]
    private float horizontalAttackWidth = 6.3f;

    [SerializeField]
    private float horizontalAttackHeight = 1.5f;

    [SerializeField]
    private float horizontalDamageMultiplier = 1f;

    [SerializeField]
    private float horizontalAttackInterval = 0.5f;

    [SerializeField]
    private float horizontalSwingDuration = 0.15f;

    [SerializeField, Range(10f, 150f)]
    private float horizontalSwingAngle = 100f;


    // =========================================================
    // Vertical Attack
    // =========================================================

    [Header("Vertical Attack (내려치기)")]

    [SerializeField]
    private float verticalAttackRange = 4.5f;

    [SerializeField]
    private float verticalAttackWidth = 1.5f;

    [SerializeField]
    private float verticalAttackHeight = 4f;

    [SerializeField]
    private float verticalDamageMultiplier = 1.5f;

    [SerializeField]
    private float verticalAttackInterval = 0.8f;

    [SerializeField]
    private float verticalSwingDuration = 0.2f;

    [Tooltip("내려치기 전에 무기를 들어 올리는 각도")]
    [SerializeField]
    private float verticalStartAngle = -80f;

    [Tooltip("내려치기가 끝나는 각도")]
    [SerializeField]
    private float verticalEndAngle = 65f;


    // =========================================================
    // Swing Animation
    // =========================================================

    [Header("Swing Animation")]

    [SerializeField]
    private Vector3 swingCenterOffset =
        new Vector3(0f, 0.7f, 0.5f);

    [Min(0.1f)]
    [SerializeField]
    private float orbitRadius = 1.4f;

    [Tooltip("무기를 공격 시작 위치로 이동시키는 시간")]
    [SerializeField]
    private float horizontalPrepareDuration = 0.08f;

    [Tooltip("내려치기 전에 무기를 위로 들어 올리는 시간")]
    [SerializeField]
    private float verticalPrepareDuration = 0.2f;

    [Tooltip("모델의 기본 방향 보정")]
    [SerializeField]
    private Vector3 bladeRotationOffset =
        new Vector3(90f, 0f, 0f);

    [SerializeField]
    private float returnToDefaultSpeed = 14f;

    [SerializeField]
    private AnimationCurve swingCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);


    // =========================================================
    // Trail
    // =========================================================

    [Header("Trail Settings")]

    [SerializeField]
    private bool alignTrailToSwingPlane = true;


    // =========================================================
    // Hit Settings
    // =========================================================

    [Header("Hit Settings")]

    [SerializeField]
    private LayerMask enemyMask = ~0;

    [SerializeField]
    private LayerMask doorMask = ~0;

    [SerializeField]
    private float doorImpactSpeed = 30f;


    // =========================================================
    // Runtime
    // =========================================================

    private bool isEquipped;
    private bool isSwinging;
    private bool nextSwingToLeft = true;

    private float nextAttackTime;

    private Coroutine swingCoroutine;

    private Vector3 defaultLocalPosition;
    private Quaternion defaultLocalRotation;
    private Quaternion defaultTrailRotation;

    private bool poseCached;


    // =========================================================
    // Awake
    // =========================================================

    private void Awake()
    {
        CacheDefaultPose();

        SetTrail(false, true);
    }


    private void CacheDefaultPose()
    {
        if (poseCached || weaponVisual == null)
            return;

        defaultLocalPosition =
            weaponVisual.localPosition;

        defaultLocalRotation =
            weaponVisual.localRotation;


        if (swingTrail == null)
        {
            swingTrail =
                weaponVisual.GetComponentInChildren
                    <TrailRenderer>(true);
        }


        if (swingTrail != null)
        {
            defaultTrailRotation =
                swingTrail.transform.localRotation;
        }

        poseCached = true;
    }


    // =========================================================
    // Equip
    // =========================================================

    public override void Equip()
    {
        CacheDefaultPose();

        base.Equip();

        isEquipped = true;

        CancelSwing();

        currentMode = AttackMode.Horizontal;

        nextSwingToLeft = true;

        nextAttackTime = 0f;
    }


    // =========================================================
    // Unequip
    // =========================================================

    public override void Unequip()
    {
        isEquipped = false;

        CancelSwing();

        base.Unequip();
    }


    // =========================================================
    // Update
    // =========================================================

    private void Update()
    {
        if (!isEquipped || PlayerStats == null)
            return;


        // UI가 열려 있거나 플레이어가 사망하면 공격 금지

        if (PlayerGameplayControl.GameplayLocked ||
            PlayerStats.IsDead)
        {
            if (isSwinging)
            {
                CancelSwing();
            }

            return;
        }


        // =====================================================
        // Q : Attack Mode 변경
        // =====================================================

        if (Keyboard.current != null &&
            Keyboard.current.qKey.wasPressedThisFrame &&
            !isSwinging)
        {
            ChangeAttackMode();
        }


        // =====================================================
        // 좌클릭 : 공격
        // =====================================================

        if (Mouse.current != null &&
            Mouse.current.leftButton.isPressed)
        {
            TryAttack();
        }


        // =====================================================
        // 공격이 끝나면 기본 자세로 복귀
        // =====================================================

        if (!isSwinging &&
            Time.time >= nextAttackTime)
        {
            ReturnToDefaultPose();
        }
    }


    // =========================================================
    // Mode Change
    // =========================================================

    private void ChangeAttackMode()
    {
        if (currentMode == AttackMode.Horizontal)
        {
            currentMode = AttackMode.Vertical;
        }
        else
        {
            currentMode = AttackMode.Horizontal;
        }

        nextSwingToLeft = true;

        SetTrail(false, true);

        Debug.Log(
            $"[TestWeapon] 공격 모드 : {currentMode}"
        );
    }


    // =========================================================
    // Attack
    // =========================================================

    private void TryAttack()
    {
        if (isSwinging)
            return;

        if (Time.time < nextAttackTime)
            return;

        if (weaponVisual == null ||
            attackPoint == null)
        {
            return;
        }


        float speedStat =
            Mathf.Max(
                0.1f,
                PlayerStats.MeleeAttackSpeed
            );


        bool vertical =
            currentMode == AttackMode.Vertical;


        float interval =
            vertical
            ? verticalAttackInterval
            : horizontalAttackInterval;


        float prepareDuration =
            vertical
            ? verticalPrepareDuration
            : horizontalPrepareDuration;


        float swingDuration =
            vertical
            ? verticalSwingDuration
            : horizontalSwingDuration;


        prepareDuration =
            Mathf.Max(0.01f, prepareDuration / speedStat);

        swingDuration =
            Mathf.Max(0.02f, swingDuration / speedStat);


        nextAttackTime =
            Time.time +
            Mathf.Max(
                interval / speedStat,
                prepareDuration + swingDuration
            );


        float startAngle;
        float endAngle;

        bool swingToLeft = nextSwingToLeft;


        if (vertical)
        {
            // 위 -> 아래

            startAngle = verticalStartAngle;
            endAngle = verticalEndAngle;
        }
        else
        {
            // 오른쪽 -> 왼쪽
            // 왼쪽 -> 오른쪽

            startAngle =
                swingToLeft
                ? horizontalSwingAngle
                : -horizontalSwingAngle;

            endAngle = -startAngle;

            nextSwingToLeft = !nextSwingToLeft;
        }


        swingCoroutine =
            StartCoroutine(
                SwingRoutine(
                    vertical,
                    swingToLeft,
                    startAngle,
                    endAngle,
                    prepareDuration,
                    swingDuration
                )
            );
    }


    // =========================================================
    // Swing Animation
    // =========================================================

    private IEnumerator SwingRoutine(
        bool vertical,
        bool swingToLeft,
        float startAngle,
        float endAngle,
        float prepareDuration,
        float swingDuration
    )
    {
        isSwinging = true;

        SetTrail(false, true);


        // =====================================================
        // 1. 공격 준비 자세
        // =====================================================

        Vector3 startPosition =
            weaponVisual.localPosition;

        Quaternion startRotation =
            weaponVisual.localRotation;


        EvaluateArcPose(
            vertical,
            startAngle,
            out Vector3 preparePosition,
            out Quaternion prepareRotation
        );


        float elapsed = 0f;


        while (elapsed < prepareDuration)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / prepareDuration
                );

            t = Mathf.SmoothStep(0f, 1f, t);


            weaponVisual.localPosition =
                Vector3.Lerp(
                    startPosition,
                    preparePosition,
                    t
                );

            weaponVisual.localRotation =
                Quaternion.Slerp(
                    startRotation,
                    prepareRotation,
                    t
                );

            yield return null;
        }


        weaponVisual.localPosition =
            preparePosition;

        weaponVisual.localRotation =
            prepareRotation;


        // =====================================================
        // 2. 실제 공격 시작
        // =====================================================

        AlignTrail(vertical, startAngle);

        SetTrail(true, true);

        elapsed = 0f;

        bool damageApplied = false;


        while (elapsed < swingDuration)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / swingDuration
                );


            float curvedT =
                Mathf.Clamp01(
                    swingCurve.Evaluate(t)
                );


            float angle =
                Mathf.Lerp(
                    startAngle,
                    endAngle,
                    curvedT
                );


            EvaluateArcPose(
                vertical,
                angle,
                out Vector3 targetPosition,
                out Quaternion targetRotation
            );


            weaponVisual.localPosition =
                targetPosition;

            weaponVisual.localRotation =
                targetRotation;


            AlignTrail(
                vertical,
                angle
            );


            // 무기를 실제로 휘두르는 도중
            // 중앙 지점에서 데미지 적용

            if (!damageApplied && curvedT >= 0.5f)
            {
                damageApplied = true;

                PerformAttack(
                    vertical,
                    swingToLeft
                );
            }


            yield return null;
        }


        // =====================================================
        // 3. 공격 종료
        // =====================================================

        EvaluateArcPose(
            vertical,
            endAngle,
            out Vector3 endPosition,
            out Quaternion endRotation
        );

        weaponVisual.localPosition = endPosition;
        weaponVisual.localRotation = endRotation;


        if (!damageApplied)
        {
            PerformAttack(
                vertical,
                swingToLeft
            );
        }


        SetTrail(false);

        isSwinging = false;

        swingCoroutine = null;
    }


    // =========================================================
    // Swing Center
    // =========================================================

    private Vector3 GetSwingCenterLocal()
    {
        Vector3 center =
            swingCenterPoint != null
            ? transform.InverseTransformPoint(
                swingCenterPoint.position
            )
            : Vector3.zero;


        return center + swingCenterOffset;
    }


    // =========================================================
    // Swing Arc 계산
    // =========================================================

    private void EvaluateArcPose(
        bool vertical,
        float angle,
        out Vector3 position,
        out Quaternion rotation
    )
    {
        Vector3 center =
            GetSwingCenterLocal();


        // 횡베기 = Y축
        // 내려치기 = X축

        Vector3 axis =
            vertical
            ? Vector3.right
            : Vector3.up;


        Quaternion arcRotation =
            Quaternion.AngleAxis(
                angle,
                axis
            );


        Vector3 radialDirection =
            arcRotation * Vector3.forward;


        // 무기가 원호를 따라 이동

        position =
            center +
            radialDirection * orbitRadius;


        // 무기가 원호를 따라 회전

        rotation =
            arcRotation *
            Quaternion.Euler(
                bladeRotationOffset
            );
    }


    // =========================================================
    // Trail
    // =========================================================

    private void SetTrail(
        bool emit,
        bool clear = false
    )
    {
        if (swingTrail == null)
            return;


        if (clear)
        {
            swingTrail.Clear();
        }

        swingTrail.emitting = emit;
    }


    private void AlignTrail(
        bool vertical,
        float angle
    )
    {
        if (!alignTrailToSwingPlane ||
            swingTrail == null ||
            swingTrail.transform == weaponVisual)
        {
            return;
        }


        Vector3 axis =
            vertical
            ? Vector3.right
            : Vector3.up;


        Quaternion arcRotation =
            Quaternion.AngleAxis(
                angle,
                axis
            );


        Vector3 normal =
            transform.TransformDirection(axis);


        Vector3 radial =
            transform.TransformDirection(
                arcRotation * Vector3.forward
            );


        swingTrail.transform.rotation =
            Quaternion.LookRotation(
                -normal,
                radial
            );
    }


    // =========================================================
    // 실제 Damage
    // =========================================================

    private void PerformAttack(
        bool vertical,
        bool swingToLeft
    )
    {
        if (PlayerStats == null ||
            PlayerStats.IsDead ||
            attackPoint == null)
        {
            return;
        }


        // =====================================================
        // 현재 공격 모드에 따른 범위 및 공격력
        // =====================================================

        float range =
            vertical
            ? verticalAttackRange
            : horizontalAttackRange;

        float width =
            vertical
            ? verticalAttackWidth
            : horizontalAttackWidth;

        float height =
            vertical
            ? verticalAttackHeight
            : horizontalAttackHeight;


        float damageMultiplier =
            vertical
            ? verticalDamageMultiplier
            : horizontalDamageMultiplier;


        float damage =
            PlayerStats.AttackPower *
            damageMultiplier;


        // =====================================================
        // 공격 방향
        // =====================================================

        Transform attackFrame =
            WeaponManager != null
            ? WeaponManager.transform
            : transform;


        Vector3 forward =
            attackFrame.forward;

        Vector3 right =
            attackFrame.right;

        Vector3 up =
            attackFrame.up;


        Quaternion boxRotation =
            Quaternion.LookRotation(
                forward,
                up
            );


        Vector3 boxCenter =
            attackPoint.position +
            forward * (range * 0.5f);


        Vector3 halfExtents =
            new Vector3(
                width * 0.5f,
                height * 0.5f,
                range * 0.5f
            );


        // =====================================================
        // 공격 판정
        // =====================================================

        LayerMask combinedMask =
            enemyMask | doorMask;


        Collider[] hits =
            Physics.OverlapBox(
                boxCenter,
                halfExtents,
                boxRotation,
                combinedMask,
                QueryTriggerInteraction.Ignore
            );


        HashSet<IHealth> hitTargets =
            new HashSet<IHealth>();

        HashSet<DoorController> hitDoors =
            new HashSet<DoorController>();

        HashSet<SlidingDoorController> hitSlidingDoors =
            new HashSet<SlidingDoorController>();


        // =====================================================
        // 문 공격 방향
        // =====================================================

        Vector3 doorHitDirection;

        if (vertical)
        {
            doorHitDirection = forward;
        }
        else
        {
            float sign =
                swingToLeft ? -1f : 1f;

            doorHitDirection =
                (
                    forward +
                    right * sign * 0.45f
                ).normalized;
        }


        foreach (Collider hit in hits)
        {
            if (hit == null)
                continue;


            // 플레이어 자신 제외

            PlayerStats hitPlayer =
                hit.GetComponentInParent<PlayerStats>();

            if (hitPlayer == PlayerStats)
                continue;


            int layerBit =
                1 << hit.gameObject.layer;


            // =================================================
            // 문 공격
            // =================================================

            if ((doorMask.value & layerBit) != 0)
            {
                Vector3 hitPoint =
                    GetClosestPointSafe(
                        hit,
                        boxCenter
                    );


                // 기존 회전문

                DoorController door =
                    hit.GetComponentInParent
                        <DoorController>();

                if (door != null)
                {
                    if (hitDoors.Add(door))
                    {
                        door.HitByMelee(
                            doorHitDirection,
                            hitPoint,
                            doorImpactSpeed
                        );
                    }

                    continue;
                }


                // 미닫이문

                SlidingDoorController slidingDoor =
                    hit.GetComponentInParent
                        <SlidingDoorController>();

                if (slidingDoor != null)
                {
                    if (hitSlidingDoors.Add(slidingDoor))
                    {
                        slidingDoor.HitByProjectile(
                            doorHitDirection * doorImpactSpeed,
                            hitPoint
                        );
                    }

                    continue;
                }
            }


            // =================================================
            // 공용 IHealth 데미지
            // =================================================

            if ((enemyMask.value & layerBit) == 0)
                continue;


            IHealth health =
                hit.GetComponentInParent<IHealth>();


            if (health == null ||
                health.IsDead)
            {
                continue;
            }


            // Collider가 여러 개여도 한 번만 공격

            if (!hitTargets.Add(health))
                continue;


            health.TakeDamage(damage);


            Debug.Log(
                $"[TestWeapon] {currentMode} / " +
                $"{hit.gameObject.name} / Damage : {damage}"
            );
        }
    }


    // =========================================================
    // Collider Hit Point
    // =========================================================

    private Vector3 GetClosestPointSafe(
        Collider col,
        Vector3 point
    )
    {
        if (col is BoxCollider ||
            col is SphereCollider ||
            col is CapsuleCollider ||
            (col is MeshCollider mesh && mesh.convex))
        {
            return col.ClosestPoint(point);
        }

        return col.ClosestPointOnBounds(point);
    }


    // =========================================================
    // Return
    // =========================================================

    private void ReturnToDefaultPose()
    {
        if (weaponVisual == null || !poseCached)
            return;


        weaponVisual.localPosition =
            Vector3.Lerp(
                weaponVisual.localPosition,
                defaultLocalPosition,
                returnToDefaultSpeed * Time.deltaTime
            );


        weaponVisual.localRotation =
            Quaternion.Slerp(
                weaponVisual.localRotation,
                defaultLocalRotation,
                returnToDefaultSpeed * Time.deltaTime
            );
    }


    // =========================================================
    // Cancel
    // =========================================================

    private void CancelSwing()
    {
        if (swingCoroutine != null)
        {
            StopCoroutine(swingCoroutine);

            swingCoroutine = null;
        }


        isSwinging = false;

        SetTrail(false, true);


        if (weaponVisual != null && poseCached)
        {
            weaponVisual.localPosition =
                defaultLocalPosition;

            weaponVisual.localRotation =
                defaultLocalRotation;
        }


        if (swingTrail != null &&
            swingTrail.transform != weaponVisual &&
            poseCached)
        {
            swingTrail.transform.localRotation =
                defaultTrailRotation;
        }
    }


    private void OnDisable()
    {
        CancelSwing();
    }


    // =========================================================
    // Gizmos
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
        {
            bool vertical =
                currentMode == AttackMode.Vertical;


            float range =
                vertical
                ? verticalAttackRange
                : horizontalAttackRange;

            float width =
                vertical
                ? verticalAttackWidth
                : horizontalAttackWidth;

            float height =
                vertical
                ? verticalAttackHeight
                : horizontalAttackHeight;


            Transform attackFrame =
                WeaponManager != null
                ? WeaponManager.transform
                : transform;


            Vector3 center =
                attackPoint.position +
                attackFrame.forward * range * 0.5f;


            Matrix4x4 oldMatrix =
                Gizmos.matrix;


            Gizmos.color = Color.red;

            Gizmos.matrix =
                Matrix4x4.TRS(
                    center,
                    attackFrame.rotation,
                    Vector3.one
                );


            Gizmos.DrawWireCube(
                Vector3.zero,
                new Vector3(width, height, range)
            );


            Gizmos.matrix = oldMatrix;
        }


        // 공격 궤도 표시

        DrawSwingArcGizmo(
            false,
            -horizontalSwingAngle,
            horizontalSwingAngle,
            Color.yellow
        );

        DrawSwingArcGizmo(
            true,
            verticalStartAngle,
            verticalEndAngle,
            Color.cyan
        );
    }


    private void DrawSwingArcGizmo(
        bool vertical,
        float startAngle,
        float endAngle,
        Color color
    )
    {
        Gizmos.color = color;

        Vector3 center =
            GetSwingCenterLocal();


        Vector3 axis =
            vertical
            ? Vector3.right
            : Vector3.up;


        Vector3 previousPoint =
            Vector3.zero;


        const int segments = 30;


        for (int i = 0; i <= segments; i++)
        {
            float t =
                (float)i / segments;


            float angle =
                Mathf.Lerp(
                    startAngle,
                    endAngle,
                    t
                );


            Vector3 direction =
                Quaternion.AngleAxis(
                    angle,
                    axis
                ) * Vector3.forward;


            Vector3 point =
                transform.TransformPoint(
                    center +
                    direction * orbitRadius
                );


            if (i > 0)
            {
                Gizmos.DrawLine(
                    previousPoint,
                    point
                );
            }


            previousPoint = point;
        }
    }
}