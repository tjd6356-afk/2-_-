using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ZantetsuswordWeapon : WeaponBase
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

    [Tooltip("(선택) 휘두르기 중심 기준으로 삼을 빈 오브젝트. 비워두면 무기 프리팹 루트(0,0,0) 기준")]
    [SerializeField]
    private Transform swingCenterPoint;

    [Header("Sword Wave Projectile (발도 원거리 검기 설정)")]
    [Tooltip("발도 공격 시 발사할 검기 Projectile 프리팹")]
    [SerializeField]
    private Projectile swordWavePrefab;

    [Tooltip("검기가 생성될 위치 (비워두면 Attack Point 위치에서 발사)")]
    [SerializeField]
    private Transform waveSpawnPoint;

    [Tooltip("검기 발사 시 PlayerStats.AttackPower에 곱해질 원거리 데미지 배율")]
    [Min(0f)]
    [SerializeField]
    private float waveDamageMultiplier = 1.2f;

    [Tooltip("화면 중앙 크로스헤어 조준 최대 거리")]
    [Min(1f)]
    [SerializeField]
    private float maxAimDistance = 100f;

    [SerializeField]
    private LayerMask aimMask = ~0;

    [Header("Normal Melee Stats (일반 횡베기 설정)")]
    [Min(0.1f)]
    [SerializeField]
    private float attackRange = 2.2f;

    [Min(0.1f)]
    [SerializeField]
    private float attackWidth = 2.8f;

    [Min(0.1f)]
    [SerializeField]
    private float attackHeight = 1.2f;

    [Min(0f)]
    [SerializeField]
    private float normalDamageMultiplier = 1f;

    [Min(0.01f)]
    [SerializeField]
    private float baseAttackInterval = 0.5f;

    [SerializeField]
    private LayerMask enemyMask = ~0;

    // =========================================================
    // 납도 준비 자세 & 사선('/') 발도 공격 설정 (우클릭 홀드)
    // =========================================================
    [Header("Sheathing Stance (우클릭 납도 준비 자세)")]
    [Tooltip("납도 자세 중 이동 속도 비율 (1.0 = 속도 감소 없음)")]
    [Range(0f, 1f)]
    [SerializeField]
    private float sheathMoveSpeedMultiplier = 0.9f;

    [Tooltip("납도 준비 시 무기를 왼쪽 허리에 거두는 로컬 위치")]
    [SerializeField]
    private Vector3 sheathLocalPosition = new Vector3(-0.45f, 0.78f, 0.22f);

    [Tooltip("납도 준비 시 칼자루가 왼쪽 아래, 칼끝이 뒤쪽을 향하도록 기울이는 로컬 회전각")]
    [SerializeField]
    private Vector3 sheathLocalEuler = new Vector3(35f, 15f, -45f);

    [Tooltip("납도 자세로 전환되는 속도")]
    [Min(1f)]
    [SerializeField]
    private float sheathTransitionSpeed = 18f;

    [Header("Iaido Draw Slash (사선 '/' 발도 베기 설정)")]
    [Tooltip("발도 공격 시 근접 판정도 동시에 들어갈지 여부")]
    [SerializeField]
    private bool iaidoAlsoHitsMelee = true;

    [Tooltip("발도 공격 시 근접 타격 데미지 배율")]
    [Min(0f)]
    [SerializeField]
    private float iaidoMeleeDamageMultiplier = 1.3f;

    [Tooltip("발도 공격 기본 공격 간격 (초)")]
    [Min(0.01f)]
    [SerializeField]
    private float iaidoBaseAttackInterval = 0.65f;

    [Tooltip("발도 사선('/') 기울기 각도 (양수 28° = 왼쪽 아래 -> 오른쪽 위 '/' 대각선 베기)")]
    [Range(-80f, 80f)]
    [SerializeField]
    private float iaidoSlashTiltAngle = 28f;

    [Tooltip("발도 베기 좌우 반각 (80이면 왼쪽 아래 -80°에서 오른쪽 위 +80°까지 크게 횡단)")]
    [Range(20f, 140f)]
    [SerializeField]
    private float iaidoSwingHalfAngle = 80f;

    [Tooltip("발도 베기 원호 반경")]
    [Min(0.1f)]
    [SerializeField]
    private float iaidoOrbitRadius = 1.25f;

    [Tooltip("발도 베기 후 같은 방향으로 감속하며 미끄러지는 여분 각도")]
    [Range(0f, 45f)]
    [SerializeField]
    private float iaidoChamberExtraAngle = 16f;

    [Tooltip("공속 1.0 기준 발도 실제 타격(트레일 ON) 소요 시간 (초)")]
    [Min(0.02f)]
    [SerializeField]
    private float iaidoSwingDuration = 0.14f;

    [Tooltip("공속 1.0 기준 발도 후 감속 여운 시간 (초)")]
    [Min(0.01f)]
    [SerializeField]
    private float iaidoChamberDuration = 0.13f;

    [Tooltip("우클릭 유지 중 발도 베기 끝 자세를 잠깐 유지한 뒤 재납도하기까지의 여운 시간 (초)")]
    [Min(0f)]
    [SerializeField]
    private float iaidoPostSlashHoldDuration = 0.10f;

    // =========================================================
    // 일반 횡베기 궤도 및 감속 연출 설정
    // =========================================================
    [Header("Normal Swing Arc (일반 수평 횡베기 궤도)")]
    [SerializeField]
    private Vector3 swingCenterOffset = new Vector3(0f, 1.15f, 0.25f);

    [Range(10f, 150f)]
    [SerializeField]
    private float swingHalfAngle = 80f;

    [Min(0.1f)]
    [SerializeField]
    private float orbitRadius = 1.25f;

    [Range(0f, 45f)]
    [SerializeField]
    private float chamberExtraAngle = 16f;

    [SerializeField]
    private Vector3 chamberPullOffset = new Vector3(0f, 0.05f, -0.14f);

    [Min(0.02f)]
    [SerializeField]
    private float baseSwingDuration = 0.13f;

    [Min(0.01f)]
    [SerializeField]
    private float baseChamberDuration = 0.14f;

    [Header("Blade Orientation & Return (칼날 방향 및 복귀)")]
    [SerializeField]
    private float bladeLayAngle = 90f;

    [SerializeField]
    private float bladeEdgeAngle = 90f;

    [SerializeField]
    private bool flipEdgeOnReturnSwing = true;

    [SerializeField]
    private bool alignTrailToSwingPlane = true;

    [SerializeField]
    private bool snapToForwardStartOnFirstSwing = true;

    [SerializeField]
    private AnimationCurve swingCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Min(1f)]
    [SerializeField]
    private float returnToDefaultSpeed = 14f;

    private ThirdPersonCamera thirdPersonCam;
    private float nextAttackTime;
    private float iaidoHoldUntilTime;
    private float currentYawAngle;
    private float currentChamberRatio;
    private float currentEdgeRoll;
    private bool isSwungToLeft;
    private bool isSwinging;
    private bool isInAttackPose; // 일반 평타 여운 유지 여부 (발도 공격 시에는 false 유지)
    private bool isSheathing;
    private bool isEquipped;
    private Coroutine swingCoroutine;

    private Vector3 defaultLocalPosition;
    private Quaternion defaultLocalRotation;
    private bool hasCachedDefaultPose;

    public bool IsSheathing => isSheathing;

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

        if (PlayerCamera != null)
        {
            thirdPersonCam = PlayerCamera.GetComponent<ThirdPersonCamera>();
        }

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
        if (isSheathing && PlayerStats != null)
        {
            PlayerStats.ResetGuardModifiers();
        }
        isSheathing = false;
    }

    private void ResetWeaponState()
    {
        if (swingCoroutine != null)
        {
            StopCoroutine(swingCoroutine);
            swingCoroutine = null;
        }

        if (isSheathing && PlayerStats != null)
        {
            PlayerStats.ResetGuardModifiers();
        }

        isSheathing = false;
        isSwinging = false;
        isSwungToLeft = false;
        isInAttackPose = false;
        nextAttackTime = 0f;
        iaidoHoldUntilTime = 0f;
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

        bool wantSheath = Mouse.current.rightButton.isPressed && !PlayerStats.IsDead;

        if (wantSheath)
        {
            EnterOrMaintainSheath();

            // 납도 준비 자세 중 좌클릭 시 사선('/') 발도 공격 + 원거리 검기 발사
            if (Mouse.current.leftButton.isPressed)
            {
                TryIaidoDrawAttack();
            }
            return;
        }

        if (isSheathing)
        {
            ExitSheath();
        }

        // 비조준 상태에서는 일반 근접 횡베기
        if (Mouse.current.leftButton.isPressed)
        {
            TryNormalAttack();
        }

        if (!isSwinging && weaponVisual != null)
        {
            // 일반 평타를 친 직후(isInAttackPose == true)에만 다음 콤보 대기 자세 유지
            if (isInAttackPose && Time.time < nextAttackTime)
            {
                float holdAngle = GetChamberedAngle(isSwungToLeft);
                float holdEdgeRoll = GetTargetEdgeRoll(isSwungToLeft);

                currentYawAngle = Mathf.Lerp(currentYawAngle, holdAngle, returnToDefaultSpeed * Time.deltaTime);
                currentChamberRatio = Mathf.Lerp(currentChamberRatio, 1f, returnToDefaultSpeed * Time.deltaTime);
                currentEdgeRoll = Mathf.LerpAngle(currentEdgeRoll, holdEdgeRoll, returnToDefaultSpeed * Time.deltaTime);

                EvaluateArcPose(currentYawAngle, currentChamberRatio, currentEdgeRoll, orbitRadius, 0f, out Vector3 holdPos, out Quaternion holdRot);
                weaponVisual.localPosition = Vector3.Lerp(weaponVisual.localPosition, holdPos, returnToDefaultSpeed * Time.deltaTime);
                weaponVisual.localRotation = Quaternion.Slerp(weaponVisual.localRotation, holdRot, returnToDefaultSpeed * Time.deltaTime);
                AlignTrailTransform(currentYawAngle, 0f);
            }
            // 발도 공격 직후 조준을 풀었거나 평타 대기 시간이 끝나면 곧바로 기본 대기 자세로 복귀
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

    // =========================================================
    // 납도 준비 자세 진입 / 유지
    // =========================================================
    private void EnterOrMaintainSheath()
    {
        if (!isSheathing)
        {
            isSheathing = true;

            if (swingCoroutine != null)
            {
                StopCoroutine(swingCoroutine);
                swingCoroutine = null;
            }
            isSwinging = false;
            isSwungToLeft = false;
            isInAttackPose = false;
            iaidoHoldUntilTime = 0f;
            SetTrailEmitting(false, true);

            PlayerStats.SetGuardModifiers(sheathMoveSpeedMultiplier, 1f);
        }

        // 발도 베기 모션 중이 아니고, 베기 직후 여운 시간이 지났을 때만 납도 자세로 거두어 들임
        if (!isSwinging && Time.time >= iaidoHoldUntilTime && weaponVisual != null)
        {
            Quaternion targetSheathRot = Quaternion.Euler(sheathLocalEuler);
            weaponVisual.localPosition = Vector3.Lerp(weaponVisual.localPosition, sheathLocalPosition, sheathTransitionSpeed * Time.deltaTime);
            weaponVisual.localRotation = Quaternion.Slerp(weaponVisual.localRotation, targetSheathRot, sheathTransitionSpeed * Time.deltaTime);
        }
    }

    private void ExitSheath()
    {
        isSheathing = false;
        isInAttackPose = false;
        iaidoHoldUntilTime = 0f;

        if (PlayerStats != null)
        {
            PlayerStats.ResetGuardModifiers();
        }
    }

    // =========================================================
    // 1. 사선('/') 발도 공격 + 원거리 검기 발사
    // =========================================================
    private void TryIaidoDrawAttack()
    {
        if (Time.time < nextAttackTime)
            return;

        float meleeSpeedStat = Mathf.Max(0.1f, PlayerStats.MeleeAttackSpeed);
        float finalInterval = iaidoBaseAttackInterval / meleeSpeedStat;

        // 평타 여운 상태 비활성화 (조준 해제 시 수평 평타 자세로 꺾이는 현상 방지)
        isInAttackPose = false;
        isSwungToLeft = false;

        // 원거리 검기 투사체 발사
        LaunchSwordWave();

        // 근접 사거리 내 적도 함께 타격
        if (iaidoAlsoHitsMelee)
        {
            PerformMeleeBoxAttack(iaidoMeleeDamageMultiplier);
        }

        // 사선('/') 발도 애니메이션 실행 (평타처럼 크게 한 번 베고 감속 여운 연결)
        if (weaponVisual != null)
        {
            if (swingCoroutine != null)
            {
                StopCoroutine(swingCoroutine);
            }

            float rawMain = iaidoSwingDuration / meleeSpeedStat;
            float rawChamber = iaidoChamberDuration / meleeSpeedStat;
            float totalRaw = Mathf.Max(0.04f, rawMain + rawChamber);

            float maxTotal = Mathf.Max(0.04f, finalInterval * 0.85f);
            float scale = totalRaw > maxTotal ? (maxTotal / totalRaw) : 1f;

            float mainDuration = Mathf.Max(0.03f, rawMain * scale);
            float chamberDuration = Mathf.Max(0f, rawChamber * scale);

            swingCoroutine = StartCoroutine(IaidoDrawRoutine(mainDuration, chamberDuration));
        }

        nextAttackTime = Time.time + finalInterval;
    }

    private IEnumerator IaidoDrawRoutine(float mainDuration, float chamberDuration)
    {
        isSwinging = true;
        SetTrailEmitting(false, true);

        try
        {
            // 왼쪽 아래(-iaidoSwingHalfAngle)에서 시작해 오른쪽 위(+iaidoSwingHalfAngle)로 '/' 크게 횡단
            float startAngle = -iaidoSwingHalfAngle;
            float strikeEndAngle = iaidoSwingHalfAngle;
            float chamberedEndAngle = iaidoSwingHalfAngle + iaidoChamberExtraAngle;
            float drawEdgeRoll = GetTargetEdgeRoll(false);

            currentYawAngle = startAngle;
            currentChamberRatio = 0f;
            currentEdgeRoll = drawEdgeRoll;

            EvaluateArcPose(startAngle, 0f, drawEdgeRoll, iaidoOrbitRadius, iaidoSlashTiltAngle, out Vector3 startPos, out Quaternion startRot);
            weaponVisual.localPosition = startPos;
            weaponVisual.localRotation = startRot;
            AlignTrailTransform(startAngle, iaidoSlashTiltAngle);

            SetTrailEmitting(true, true);

            // 1단계: 사선('/') 방향으로 크게 한 번 베어내기 (트레일 ON)
            float elapsed = 0f;
            while (elapsed < mainDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / mainDuration);
                float curvedT = swingCurve.Evaluate(t);

                currentYawAngle = Mathf.LerpUnclamped(startAngle, strikeEndAngle, curvedT);
                currentChamberRatio = 0f;

                EvaluateArcPose(currentYawAngle, currentChamberRatio, drawEdgeRoll, iaidoOrbitRadius, iaidoSlashTiltAngle, out Vector3 pos, out Quaternion rot);
                weaponVisual.localPosition = pos;
                weaponVisual.localRotation = rot;
                AlignTrailTransform(currentYawAngle, iaidoSlashTiltAngle);

                yield return null;
            }

            SetTrailEmitting(false, false);

            // 2단계: 평타처럼 같은 방향으로 부드럽게 감속하며 마무리
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

                    EvaluateArcPose(currentYawAngle, currentChamberRatio, drawEdgeRoll, iaidoOrbitRadius, iaidoSlashTiltAngle, out Vector3 chamberPos, out Quaternion chamberRot);
                    weaponVisual.localPosition = chamberPos;
                    weaponVisual.localRotation = chamberRot;
                    AlignTrailTransform(currentYawAngle, iaidoSlashTiltAngle);

                    yield return null;
                }
            }

            currentYawAngle = chamberedEndAngle;
            currentChamberRatio = 1f;
            EvaluateArcPose(currentYawAngle, currentChamberRatio, drawEdgeRoll, iaidoOrbitRadius, iaidoSlashTiltAngle, out Vector3 finalPos, out Quaternion finalRot);
            weaponVisual.localPosition = finalPos;
            weaponVisual.localRotation = finalRot;
            AlignTrailTransform(currentYawAngle, iaidoSlashTiltAngle);

            // 우클릭 유지 시에만 베어낸 끝 자세 여운을 잠깐 남긴 뒤 재납도
            iaidoHoldUntilTime = isSheathing ? (Time.time + iaidoPostSlashHoldDuration) : 0f;
        }
        finally
        {
            SetTrailEmitting(false, false);
            isSwinging = false;
            isInAttackPose = false;
            isSwungToLeft = false;
            swingCoroutine = null;
        }
    }

    private void LaunchSwordWave()
    {
        if (swordWavePrefab == null)
        {
            Debug.LogWarning($"{WeaponName} : SwordWavePrefab이 연결되지 않았습니다.");
            return;
        }

        Transform spawnTransform = waveSpawnPoint != null
            ? waveSpawnPoint
            : (attackPoint != null ? attackPoint : transform);

        Vector3 spawnPos = spawnTransform.position;
        Vector3 shootDirection = GetAimShootDirection(spawnPos);

        // 검기 프리팹도 발도 베기('/') 각도만큼 기울여서 생성
        Quaternion waveRotation = Quaternion.LookRotation(shootDirection) * Quaternion.Euler(0f, 0f, iaidoSlashTiltAngle);

        Projectile wave = Instantiate(swordWavePrefab, spawnPos, waveRotation);

        float waveDamage = PlayerStats.AttackPower * waveDamageMultiplier;
        GameObject owner = WeaponManager != null ? WeaponManager.gameObject : gameObject;

        wave.Launch(shootDirection, waveDamage, owner);
    }

    private Vector3 GetAimShootDirection(Vector3 origin)
    {
        if (PlayerCamera != null)
        {
            Ray aimRay = PlayerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            Vector3 targetPoint = Physics.Raycast(aimRay, out RaycastHit hit, maxAimDistance, aimMask, QueryTriggerInteraction.Ignore)
                ? hit.point
                : aimRay.GetPoint(maxAimDistance);

            Vector3 toTarget = targetPoint - origin;
            if (Vector3.Dot(toTarget, PlayerCamera.transform.forward) > 0.1f)
            {
                return toTarget.normalized;
            }

            return PlayerCamera.transform.forward;
        }

        return WeaponManager != null ? WeaponManager.transform.forward : transform.forward;
    }

    // =========================================================
    // 2. 일반 근접 횡베기 공격
    // =========================================================
    private void TryNormalAttack()
    {
        if (isSheathing || Time.time < nextAttackTime)
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

            EvaluateArcPose(currentYawAngle, currentChamberRatio, currentEdgeRoll, orbitRadius, 0f, out Vector3 forwardStartPos, out Quaternion forwardStartRot);
            weaponVisual.localPosition = forwardStartPos;
            weaponVisual.localRotation = forwardStartRot;
            AlignTrailTransform(currentYawAngle, 0f);
        }

        isInAttackPose = true;

        PerformMeleeBoxAttack(normalDamageMultiplier);

        isSwungToLeft = !isSwungToLeft;
        StartNormalSwingAnimation(isSwungToLeft, meleeSpeedStat, finalInterval);

        nextAttackTime = Time.time + finalInterval;
    }

    private void StartNormalSwingAnimation(bool toLeft, float speedStat, float maxAllowedDuration)
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

        swingCoroutine = StartCoroutine(NormalSwingRoutine(toLeft, mainDuration, chamberDuration));
    }

    private IEnumerator NormalSwingRoutine(bool toLeft, float mainDuration, float chamberDuration)
    {
        isSwinging = true;

        float startAngle = currentYawAngle;
        float startChamberRatio = currentChamberRatio;

        float strikeEndAngle = GetStrikeEndAngle(toLeft);
        float chamberedEndAngle = GetChamberedAngle(toLeft);
        float targetEdgeRoll = GetTargetEdgeRoll(toLeft);

        currentEdgeRoll = targetEdgeRoll;
        EvaluateArcPose(startAngle, startChamberRatio, currentEdgeRoll, orbitRadius, 0f, out Vector3 alignedStartPos, out Quaternion alignedStartRot);
        weaponVisual.localPosition = alignedStartPos;
        weaponVisual.localRotation = alignedStartRot;
        AlignTrailTransform(startAngle, 0f);

        SetTrailEmitting(true, true);

        try
        {
            float elapsed = 0f;
            while (elapsed < mainDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / mainDuration);
                float curvedT = swingCurve.Evaluate(t);

                currentYawAngle = Mathf.LerpUnclamped(startAngle, strikeEndAngle, curvedT);
                currentChamberRatio = Mathf.Lerp(startChamberRatio, 0f, curvedT);

                EvaluateArcPose(currentYawAngle, currentChamberRatio, currentEdgeRoll, orbitRadius, 0f, out Vector3 targetPos, out Quaternion targetRot);
                weaponVisual.localPosition = targetPos;
                weaponVisual.localRotation = targetRot;
                AlignTrailTransform(currentYawAngle, 0f);

                yield return null;
            }

            SetTrailEmitting(false, false);

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

                    EvaluateArcPose(currentYawAngle, currentChamberRatio, currentEdgeRoll, orbitRadius, 0f, out Vector3 chamberPos, out Quaternion chamberRot);
                    weaponVisual.localPosition = chamberPos;
                    weaponVisual.localRotation = chamberRot;
                    AlignTrailTransform(currentYawAngle, 0f);

                    yield return null;
                }
            }

            currentYawAngle = chamberedEndAngle;
            currentChamberRatio = 1f;
            EvaluateArcPose(currentYawAngle, currentChamberRatio, currentEdgeRoll, orbitRadius, 0f, out Vector3 finalPos, out Quaternion finalRot);
            weaponVisual.localPosition = finalPos;
            weaponVisual.localRotation = finalRot;
            AlignTrailTransform(currentYawAngle, 0f);
        }
        finally
        {
            SetTrailEmitting(false, false);
            isSwinging = false;
            swingCoroutine = null;
        }
    }

    // =========================================================
    // 궤도 및 트레일 평면 계산 (수평 0° / 사선 '/' tiltAngle 지원)
    // =========================================================
    private void EvaluateArcPose(float yawAngle, float chamberRatio, float edgeRollAngle, float radius, float planeTiltAngle, out Vector3 pos, out Quaternion rot)
    {
        Vector3 pivot = GetSwingCenterLocal();

        float rad = yawAngle * Mathf.Deg2Rad;
        Vector3 horizontalRadial = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));

        Quaternion planeTilt = Quaternion.Euler(0f, 0f, planeTiltAngle);
        Vector3 tiltedRadial = planeTilt * horizontalRadial;
        Vector3 pullOffset = chamberPullOffset * chamberRatio;

        pos = pivot + tiltedRadial * radius + pullOffset;

        Quaternion arcOrientation = planeTilt * Quaternion.Euler(bladeLayAngle, yawAngle, 0f);
        Quaternion bladeTwist = Quaternion.Euler(0f, edgeRollAngle, 0f);

        rot = arcOrientation * bladeTwist;
    }

    private void AlignTrailTransform(float yawAngle, float planeTiltAngle)
    {
        if (!alignTrailToSwingPlane || swingTrail == null || swingTrail.transform == weaponVisual)
            return;

        Quaternion planeTilt = Quaternion.Euler(0f, 0f, planeTiltAngle);
        Quaternion yawRot = Quaternion.Euler(0f, yawAngle, 0f);

        Vector3 localPlaneNormal = planeTilt * Vector3.up;
        Vector3 localRadialDir = planeTilt * (yawRot * Vector3.forward);

        Vector3 worldPlaneNormal = transform.TransformDirection(localPlaneNormal);
        Vector3 worldRadialDir = transform.TransformDirection(localRadialDir);

        swingTrail.transform.rotation = Quaternion.LookRotation(-worldPlaneNormal, worldRadialDir);
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

    private void PerformMeleeBoxAttack(float dmgMultiplier)
    {
        if (attackPoint == null || PlayerStats == null || dmgMultiplier <= 0f)
            return;

        float damage = PlayerStats.AttackPower * dmgMultiplier;

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
        normalDamageMultiplier = Mathf.Max(0f, normalDamageMultiplier);
        waveDamageMultiplier = Mathf.Max(0f, waveDamageMultiplier);
        iaidoMeleeDamageMultiplier = Mathf.Max(0f, iaidoMeleeDamageMultiplier);

        baseAttackInterval = Mathf.Max(0.01f, baseAttackInterval);
        iaidoBaseAttackInterval = Mathf.Max(0.01f, iaidoBaseAttackInterval);

        orbitRadius = Mathf.Max(0.1f, orbitRadius);
        iaidoOrbitRadius = Mathf.Max(0.1f, iaidoOrbitRadius);
        baseSwingDuration = Mathf.Max(0.02f, baseSwingDuration);
        baseChamberDuration = Mathf.Max(0.01f, baseChamberDuration);
        iaidoSwingDuration = Mathf.Max(0.02f, iaidoSwingDuration);
        iaidoChamberDuration = Mathf.Max(0.01f, iaidoChamberDuration);

        maxAimDistance = Mathf.Max(1f, maxAimDistance);
        returnToDefaultSpeed = Mathf.Max(1f, returnToDefaultSpeed);
        sheathTransitionSpeed = Mathf.Max(1f, sheathTransitionSpeed);
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

        // 노란색: 일반 수평 횡베기 궤도 / 하늘색: '/' 사선 발도 베기 궤도
        DrawSwingArcGizmo(swingHalfAngle + chamberExtraAngle, orbitRadius, 0f, Color.yellow);
        DrawSwingArcGizmo(iaidoSwingHalfAngle + iaidoChamberExtraAngle, iaidoOrbitRadius, iaidoSlashTiltAngle, Color.cyan);
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

    private void DrawSwingArcGizmo(float halfAngle, float radius, float planeTiltAngle, Color color)
    {
        Gizmos.color = color;

        Vector3 localCenter = GetSwingCenterLocal();
        Vector3 worldCenter = transform.TransformPoint(localCenter);
        Quaternion planeTilt = Quaternion.Euler(0f, 0f, planeTiltAngle);

        Gizmos.DrawWireSphere(worldCenter, 0.08f);

        int segments = 20;
        Vector3 prevPoint = Vector3.zero;

        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float angle = Mathf.Lerp(-halfAngle, halfAngle, t);
            float rad = angle * Mathf.Deg2Rad;

            Vector3 horizontalRadial = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
            Vector3 localPoint = localCenter + (planeTilt * horizontalRadial) * radius;
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