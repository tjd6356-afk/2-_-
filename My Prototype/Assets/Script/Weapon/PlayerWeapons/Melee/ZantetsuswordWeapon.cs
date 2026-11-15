using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ZantetsuswordWeapon : WeaponBase
{
    [Header("References")]
    [Tooltip("근접 공격 판정 기준점 (비워두면 플레이어 본체 가슴 높이 전방을 자동 사용)")]
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

    [Header("Fallback Origin Offsets (AttackPoint 미지정 시 기본 발사/타격 위치)")]
    [Tooltip("AttackPoint가 비어 있을 때 플레이어 발밑 기준 높이 오프셋")]
    [SerializeField]
    private float defaultOriginHeight = 1.1f;

    [Tooltip("AttackPoint가 비어 있을 때 플레이어 정면 거리 오프셋")]
    [SerializeField]
    private float defaultOriginForward = 0.5f;

    [Header("Sword Wave Projectile (발도 원거리 검기 설정)")]
    [Tooltip("발도 공격 시 발사할 SwordWaveProjectile 프리팹")]
    [SerializeField]
    private SwordWaveProjectile swordWavePrefab;

    [Tooltip("검기가 생성될 위치 (비워두면 Attack Point 또는 플레이어 가슴 전방에서 발사)")]
    [SerializeField]
    private Transform waveSpawnPoint;

    [Tooltip("검기 발사 시 PlayerStats.AttackPower에 곱해질 원거리 데미지 배율")]
    [Min(0f)]
    [SerializeField]
    private float waveDamageMultiplier = 1.2f;

    [Tooltip("날아가는 검기의 기울기 각도 (체크 해제 시 발도 베기 각도 iaidoSlashTiltAngle을 그대로 따라감)")]
    [SerializeField]
    private bool useCustomWaveTilt = false;

    [Tooltip("useCustomWaveTilt 체크 시 적용할 날아가는 검기의 기울기 각도")]
    [Range(-180f, 180f)]
    [SerializeField]
    private float customWaveTiltAngle = 28f;

    [Tooltip("화면 중앙 크로스헤어 조준 최대 거리")]
    [Min(1f)]
    [SerializeField]
    private float maxAimDistance = 100f;

    [SerializeField]
    private LayerMask aimMask = ~0;

    [Header("Normal Melee Stats (일반 횡베기 설정)")]
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

    [Tooltip("적의 Is Trigger 콜라이더도 근접 공격으로 타격할지 여부")]
    [SerializeField]
    private bool hitTriggerColliders = true;

    [Tooltip("근접 판정 결과를 콘솔에 출력할지 여부 (원인 확인 후 해제 가능)")]
    [SerializeField]
    private bool debugMeleeHitLog = true;

    // =========================================================
    // 납도 준비 자세 & 사선('/') 발도 공격 설정 (우클릭 홀드)
    // =========================================================
    [Header("Sheathing Stance (우클릭 납도 준비 자세)")]
    [Tooltip("납도 자세 중 플레이어 이동 속도 비율 (1.0 = 속도 감소 없음)")]
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
    [Header("Swing Arc (일반 휘두르기 중심 및 궤도)")]
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

    [Header("Follow-Through & Chambering (감속 및 다음 공격 대비 자세)")]
    [Tooltip("타격 지점을 지난 뒤 같은 방향으로 더 미끄러지며 회전하는 추가 각도")]
    [Range(0f, 45f)]
    [SerializeField]
    private float chamberExtraAngle = 16f;

    [Tooltip("다음 휘두르기를 대비해 무기를 뒤로 살짝 내빼는 위치 오프셋")]
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

    [Header("Blade Orientation & Return (칼날 방향 및 무기 눕힘 각도)")]
    [Tooltip("휘두를 때 무기를 눕히는 X축 각도 (90 = 지면과 완전히 수평)")]
    [SerializeField]
    private float bladeLayAngle = 90f;

    [Tooltip("칼날이 옆면이 아닌 베는 방향을 향하도록 칼자루 축을 회전시키는 각도")]
    [SerializeField]
    private float bladeEdgeAngle = 90f;

    [Tooltip("왕복 스윙 시 칼날이 항상 진행 방향을 향하도록 손목을 반전시킬지 여부")]
    [SerializeField]
    private bool flipEdgeOnReturnSwing = true;

    [Tooltip("휘두를 때 칼날 기울기 Z축 각도 (0 = 수평 횡베기)")]
    [SerializeField]
    private float bladeRollAngle = 0f;

    [Tooltip("트레일의 정면이 항상 휘두르는 평면의 위쪽을 향하도록 자동 정렬")]
    [SerializeField]
    private bool alignTrailToSwingPlane = true;

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
    private float iaidoHoldUntilTime;
    private float currentYawAngle;
    private float currentChamberRatio;
    private float currentEdgeRoll;
    private bool isSwungToLeft;
    private bool isSwinging;
    private bool isInAttackPose;
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

        if (Mouse.current.leftButton.isPressed)
        {
            TryNormalAttack();
        }

        if (!isSwinging && weaponVisual != null)
        {
            if (isInAttackPose && Time.time < nextAttackTime)
            {
                float holdAngle = GetChamberedAngle(isSwungToLeft);
                float holdEdgeRoll = GetTargetEdgeRoll(isSwungToLeft);

                currentYawAngle = Mathf.Lerp(currentYawAngle, holdAngle, returnToDefaultSpeed * Time.deltaTime);
                currentChamberRatio = Mathf.Lerp(currentChamberRatio, 1f, returnToDefaultSpeed * Time.deltaTime);
                currentEdgeRoll = Mathf.LerpAngle(currentEdgeRoll, holdEdgeRoll, returnToDefaultSpeed * Time.deltaTime);

                EvaluateArcPose(currentYawAngle, currentChamberRatio, currentEdgeRoll, orbitRadius, bladeRollAngle, 0f, out Vector3 holdPos, out Quaternion holdRot);
                weaponVisual.localPosition = Vector3.Lerp(weaponVisual.localPosition, holdPos, returnToDefaultSpeed * Time.deltaTime);
                weaponVisual.localRotation = Quaternion.Slerp(weaponVisual.localRotation, holdRot, returnToDefaultSpeed * Time.deltaTime);
                AlignTrailTransform(currentYawAngle, bladeRollAngle, 0f);
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

        if (!isSwinging && Time.time >= iaidoHoldUntilTime && weaponVisual != null)
        {
            float pitchAngle = GetAimPitchAngle();
            Quaternion pitchRot = Quaternion.Euler(pitchAngle, 0f, 0f);

            Vector3 targetPos = pitchRot * sheathLocalPosition;
            Quaternion targetRot = pitchRot * Quaternion.Euler(sheathLocalEuler);

            weaponVisual.localPosition = Vector3.Lerp(weaponVisual.localPosition, targetPos, sheathTransitionSpeed * Time.deltaTime);
            weaponVisual.localRotation = Quaternion.Slerp(weaponVisual.localRotation, targetRot, sheathTransitionSpeed * Time.deltaTime);
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
    // 1. 사선('/') 발도 공격 + 조준 위아래 각도 연동 + 원거리 검기 발사
    // =========================================================
    private void TryIaidoDrawAttack()
    {
        if (Time.time < nextAttackTime)
            return;

        float meleeSpeedStat = Mathf.Max(0.1f, PlayerStats.MeleeAttackSpeed);
        float finalInterval = iaidoBaseAttackInterval / meleeSpeedStat;

        isInAttackPose = false;
        isSwungToLeft = false;

        Vector3 spawnOrigin = GetWaveSpawnOrigin();
        Vector3 aimDir = GetAimShootDirection(spawnOrigin);

        LaunchSwordWave(spawnOrigin, aimDir);

        if (iaidoAlsoHitsMelee)
        {
            PerformMeleeBoxAttack(iaidoMeleeDamageMultiplier, aimDir);
        }

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
            float startAngle = -iaidoSwingHalfAngle;
            float strikeEndAngle = iaidoSwingHalfAngle;
            float chamberedEndAngle = iaidoSwingHalfAngle + iaidoChamberExtraAngle;
            float drawEdgeRoll = GetTargetEdgeRoll(false);

            currentYawAngle = startAngle;
            currentChamberRatio = 0f;
            currentEdgeRoll = drawEdgeRoll;

            float pitchAngle = GetAimPitchAngle();

            EvaluateArcPose(startAngle, 0f, drawEdgeRoll, iaidoOrbitRadius, iaidoSlashTiltAngle, pitchAngle, out Vector3 startPos, out Quaternion startRot);
            weaponVisual.localPosition = startPos;
            weaponVisual.localRotation = startRot;
            AlignTrailTransform(startAngle, iaidoSlashTiltAngle, pitchAngle);

            SetTrailEmitting(true, true);

            float elapsed = 0f;
            while (elapsed < mainDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / mainDuration);
                float curvedT = swingCurve.Evaluate(t);

                currentYawAngle = Mathf.LerpUnclamped(startAngle, strikeEndAngle, curvedT);
                currentChamberRatio = 0f;
                pitchAngle = GetAimPitchAngle();

                EvaluateArcPose(currentYawAngle, currentChamberRatio, drawEdgeRoll, iaidoOrbitRadius, iaidoSlashTiltAngle, pitchAngle, out Vector3 pos, out Quaternion rot);
                weaponVisual.localPosition = pos;
                weaponVisual.localRotation = rot;
                AlignTrailTransform(currentYawAngle, iaidoSlashTiltAngle, pitchAngle);

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
                    pitchAngle = GetAimPitchAngle();

                    EvaluateArcPose(currentYawAngle, currentChamberRatio, drawEdgeRoll, iaidoOrbitRadius, iaidoSlashTiltAngle, pitchAngle, out Vector3 chamberPos, out Quaternion chamberRot);
                    weaponVisual.localPosition = chamberPos;
                    weaponVisual.localRotation = chamberRot;
                    AlignTrailTransform(currentYawAngle, iaidoSlashTiltAngle, pitchAngle);

                    yield return null;
                }
            }

            currentYawAngle = chamberedEndAngle;
            currentChamberRatio = 1f;
            pitchAngle = GetAimPitchAngle();

            EvaluateArcPose(currentYawAngle, currentChamberRatio, drawEdgeRoll, iaidoOrbitRadius, iaidoSlashTiltAngle, pitchAngle, out Vector3 finalPos, out Quaternion finalRot);
            weaponVisual.localPosition = finalPos;
            weaponVisual.localRotation = finalRot;
            AlignTrailTransform(currentYawAngle, iaidoSlashTiltAngle, pitchAngle);

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

    private float GetAimPitchAngle()
    {
        if (PlayerCamera == null)
            return 0f;

        Vector3 localCamForward = transform.InverseTransformDirection(PlayerCamera.transform.forward);
        if (localCamForward.sqrMagnitude < 0.0001f)
            return 0f;

        localCamForward.Normalize();
        return -Mathf.Asin(Mathf.Clamp(localCamForward.y, -0.95f, 0.95f)) * Mathf.Rad2Deg;
    }

    private void LaunchSwordWave(Vector3 spawnPos, Vector3 shootDirection)
    {
        if (swordWavePrefab == null)
        {
            Debug.LogWarning($"{WeaponName} : SwordWavePrefab이 연결되지 않았습니다.");
            return;
        }

        float tiltAngle = useCustomWaveTilt ? customWaveTiltAngle : iaidoSlashTiltAngle;

        Quaternion waveRotation = Quaternion.LookRotation(shootDirection, Vector3.up) * Quaternion.Euler(0f, 0f, tiltAngle);
        SwordWaveProjectile wave = Instantiate(swordWavePrefab, spawnPos, waveRotation);

        float waveDamage = PlayerStats.AttackPower * waveDamageMultiplier;
        wave.Launch(shootDirection, waveDamage, PlayerStats, tiltAngle);
    }

    private Vector3 GetAimShootDirection(Vector3 origin)
    {
        if (PlayerCamera != null)
        {
            Ray aimRay = PlayerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            float camToOriginDist = Vector3.Dot(origin - aimRay.origin, aimRay.direction);
            float minValidDistance = Mathf.Max(0.2f, camToOriginDist + 0.2f);

            Vector3 targetPoint = aimRay.GetPoint(maxAimDistance);

            // 1순위: 조준선 반경 0.35m 이내에 적이 있으면 적의 콜라이더 중심을 우선 조준
            RaycastHit[] enemyHits = Physics.SphereCastAll(aimRay, 0.35f, maxAimDistance, enemyMask, QueryTriggerInteraction.Collide);
            float nearestEnemyDist = maxAimDistance;
            bool foundEnemy = false;

            for (int i = 0; i < enemyHits.Length; i++)
            {
                if (enemyHits[i].distance < minValidDistance)
                    continue;

                Collider col = enemyHits[i].collider;
                if (col == null)
                    continue;

                PlayerStats hitPlayer = col.GetComponentInParent<PlayerStats>();
                if (hitPlayer != null && hitPlayer == PlayerStats)
                    continue;

                IHealth health = col.GetComponentInParent<IHealth>();
                if (health == null || health.IsDead)
                    continue;

                if (enemyHits[i].distance < nearestEnemyDist)
                {
                    nearestEnemyDist = enemyHits[i].distance;
                    targetPoint = col.bounds.center;
                    foundEnemy = true;
                }
            }

            // 2순위: 조준선 주변에 적이 없으면 일반 레이캐스트 지점 조준
            if (!foundEnemy)
            {
                RaycastHit[] hits = Physics.RaycastAll(aimRay, maxAimDistance, aimMask, QueryTriggerInteraction.Ignore);
                float nearestDist = maxAimDistance;

                for (int i = 0; i < hits.Length; i++)
                {
                    if (hits[i].distance < minValidDistance)
                        continue;

                    Collider col = hits[i].collider;
                    if (col == null)
                        continue;

                    PlayerStats hitPlayer = col.GetComponentInParent<PlayerStats>();
                    if (hitPlayer != null && hitPlayer == PlayerStats)
                        continue;

                    if (hits[i].distance < nearestDist)
                    {
                        nearestDist = hits[i].distance;
                        targetPoint = hits[i].point;
                    }
                }
            }

            Vector3 toTarget = targetPoint - origin;
            if (toTarget.sqrMagnitude > 0.0001f && Vector3.Dot(toTarget, PlayerCamera.transform.forward) > 0.1f)
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

            EvaluateArcPose(currentYawAngle, currentChamberRatio, currentEdgeRoll, orbitRadius, bladeRollAngle, 0f, out Vector3 forwardStartPos, out Quaternion forwardStartRot);
            weaponVisual.localPosition = forwardStartPos;
            weaponVisual.localRotation = forwardStartRot;
            AlignTrailTransform(currentYawAngle, bladeRollAngle, 0f);
        }

        isInAttackPose = true;

        Vector3 forwardDir = WeaponManager != null ? WeaponManager.transform.forward : transform.forward;
        PerformMeleeBoxAttack(damageMultiplier, forwardDir);

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
        EvaluateArcPose(startAngle, startChamberRatio, currentEdgeRoll, orbitRadius, bladeRollAngle, 0f, out Vector3 alignedStartPos, out Quaternion alignedStartRot);
        weaponVisual.localPosition = alignedStartPos;
        weaponVisual.localRotation = alignedStartRot;
        AlignTrailTransform(startAngle, bladeRollAngle, 0f);

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

                EvaluateArcPose(currentYawAngle, currentChamberRatio, currentEdgeRoll, orbitRadius, bladeRollAngle, 0f, out Vector3 targetPos, out Quaternion targetRot);
                weaponVisual.localPosition = targetPos;
                weaponVisual.localRotation = targetRot;
                AlignTrailTransform(currentYawAngle, bladeRollAngle, 0f);

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

                    EvaluateArcPose(currentYawAngle, currentChamberRatio, currentEdgeRoll, orbitRadius, bladeRollAngle, 0f, out Vector3 chamberPos, out Quaternion chamberRot);
                    weaponVisual.localPosition = chamberPos;
                    weaponVisual.localRotation = chamberRot;
                    AlignTrailTransform(currentYawAngle, bladeRollAngle, 0f);

                    yield return null;
                }
            }

            currentYawAngle = chamberedEndAngle;
            currentChamberRatio = 1f;
            EvaluateArcPose(currentYawAngle, currentChamberRatio, currentEdgeRoll, orbitRadius, bladeRollAngle, 0f, out Vector3 finalPos, out Quaternion finalRot);
            weaponVisual.localPosition = finalPos;
            weaponVisual.localRotation = finalRot;
            AlignTrailTransform(currentYawAngle, bladeRollAngle, 0f);
        }
        finally
        {
            SetTrailEmitting(false, false);
            isSwinging = false;
            swingCoroutine = null;
        }
    }

    // =========================================================
    // 궤도 및 트레일 평면 계산
    // =========================================================
    private void EvaluateArcPose(
        float yawAngle,
        float chamberRatio,
        float edgeRollAngle,
        float radius,
        float planeTiltAngle,
        float pitchAngle,
        out Vector3 pos,
        out Quaternion rot)
    {
        Vector3 pivot = GetSwingCenterLocal();

        float rad = yawAngle * Mathf.Deg2Rad;
        Vector3 horizontalRadial = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));

        Quaternion pitchRot = Quaternion.Euler(pitchAngle, 0f, 0f);
        Quaternion swingSpaceRot = pitchRot * Quaternion.Euler(0f, 0f, planeTiltAngle);

        Vector3 tiltedRadial = swingSpaceRot * horizontalRadial;
        Vector3 pullOffset = pitchRot * (chamberPullOffset * chamberRatio);

        pos = pivot + tiltedRadial * radius + pullOffset;

        Quaternion arcOrientation = swingSpaceRot * Quaternion.Euler(bladeLayAngle, yawAngle, 0f);
        Quaternion bladeTwist = Quaternion.Euler(0f, edgeRollAngle, 0f);

        rot = arcOrientation * bladeTwist;
    }

    private void AlignTrailTransform(float yawAngle, float planeTiltAngle, float pitchAngle)
    {
        if (!alignTrailToSwingPlane || swingTrail == null || swingTrail.transform == weaponVisual)
            return;

        Quaternion swingSpaceRot = Quaternion.Euler(pitchAngle, 0f, 0f) * Quaternion.Euler(0f, 0f, planeTiltAngle);
        Quaternion yawRot = Quaternion.Euler(0f, yawAngle, 0f);

        Vector3 localPlaneNormal = swingSpaceRot * Vector3.up;
        Vector3 localRadialDir = swingSpaceRot * (yawRot * Vector3.forward);

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

    private Vector3 GetMeleeOrigin()
    {
        if (attackPoint != null)
            return attackPoint.position;

        Transform body = WeaponManager != null ? WeaponManager.transform : transform;
        return body.position + body.up * defaultOriginHeight + body.forward * defaultOriginForward;
    }

    private Vector3 GetWaveSpawnOrigin()
    {
        if (waveSpawnPoint != null)
            return waveSpawnPoint.position;

        return GetMeleeOrigin();
    }

    private void PerformMeleeBoxAttack(float dmgMultiplier, Vector3 attackDirection)
    {
        if (PlayerStats == null || dmgMultiplier <= 0f)
            return;

        float damage = PlayerStats.AttackPower * dmgMultiplier;

        Vector3 dir = attackDirection.sqrMagnitude > 0.0001f
            ? attackDirection.normalized
            : (WeaponManager != null ? WeaponManager.transform.forward : transform.forward);

        Vector3 upAxis = WeaponManager != null ? WeaponManager.transform.up : transform.up;

        Quaternion boxRotation = Quaternion.LookRotation(dir, upAxis);
        Vector3 origin = GetMeleeOrigin();
        Vector3 boxCenter = origin + dir * (attackRange * 0.5f);
        Vector3 halfExtents = new Vector3(attackWidth * 0.5f, attackHeight * 0.5f, attackRange * 0.5f);

        QueryTriggerInteraction triggerMode = hitTriggerColliders
            ? QueryTriggerInteraction.Collide
            : QueryTriggerInteraction.Ignore;

        Collider[] hits = Physics.OverlapBox(
            boxCenter,
            halfExtents,
            boxRotation,
            enemyMask,
            triggerMode
        );

        HashSet<IHealth> hitTargets = new HashSet<IHealth>();
        int validHitCount = 0;

        foreach (Collider hit in hits)
        {
            if (hit == null)
                continue;

            PlayerStats hitPlayer = hit.GetComponentInParent<PlayerStats>();
            if (hitPlayer != null && hitPlayer == PlayerStats)
                continue;

            IHealth health = hit.GetComponentInParent<IHealth>();
            if (health == null)
            {
                if (debugMeleeHitLog)
                    Debug.Log($"[근접 판정] 콜라이더 '{hit.name}'(Layer: {LayerMask.LayerToName(hit.gameObject.layer)})는 잡혔지만 IHealth가 없음");
                continue;
            }

            if (!hitTargets.Add(health))
                continue;

            if (health.IsDead)
            {
                if (debugMeleeHitLog)
                    Debug.Log($"[근접 판정] 대상 '{hit.name}'은 이미 IsDead == true 상태임");
                continue;
            }

            health.TakeDamage(damage);
            validHitCount++;

            if (debugMeleeHitLog)
                Debug.Log($"[근접 판정] '{hit.name}'에게 데미지 {damage} 전달 완료");
        }

        if (validHitCount == 0 && debugMeleeHitLog)
        {
            Debug.LogWarning($"[근접 판정 실패] 박스 내 감지된 콜라이더 수: {hits.Length}개 / Origin: {origin}");
        }
    }

    private void OnValidate()
    {
        attackRange = Mathf.Max(0.1f, attackRange);
        attackWidth = Mathf.Max(0.1f, attackWidth);
        attackHeight = Mathf.Max(0.1f, attackHeight);
        damageMultiplier = Mathf.Max(0f, damageMultiplier);
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
        Vector3 origin = GetMeleeOrigin();
        Vector3 forward = WeaponManager != null ? WeaponManager.transform.forward : transform.forward;
        Vector3 up = WeaponManager != null ? WeaponManager.transform.up : transform.up;
        Quaternion boxRot = Quaternion.LookRotation(forward, up);

        DrawBoxGizmo(origin, forward, boxRot, attackRange, attackWidth, attackHeight, Color.red);

        DrawSwingArcGizmo(swingHalfAngle + chamberExtraAngle, orbitRadius, bladeRollAngle, Color.yellow);
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