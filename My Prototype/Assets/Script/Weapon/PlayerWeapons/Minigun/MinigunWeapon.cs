using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class MinigunWeapon : WeaponBase, IAmmoWeapon
{
    [Header("Minigun References")]
    [Tooltip("총알 및 히트스캔 궤적이 시작될 총구 위치 Transform")]
    [SerializeField] private Transform firePoint;

    [Tooltip("히트스캔 궤적을 그려줄 LineRenderer 프리팹")]
    [SerializeField] private MinigunTrailVFX trailPrefab;


    [Header("Stat Multipliers (스탯 비례 배율)")]
    [Tooltip("플레이어 기본 공격력(AttackPower)에 곱해질 데미지 배율 (미니건은 연사력이 빠르므로 보통 0.3~0.5배 추천)")]
    [SerializeField, Range(0.1f, 3f)] private float damageMultiplier = 0.5f;

    [Tooltip("플레이어 기본 연사속도(FireRate)에 곱해질 최대 연사 배율 (미니건이므로 3~5배 추천)")]
    [SerializeField, Range(1f, 10f)] private float fireRateMultiplier = 4f;


    [Header("Spooling (예열 시스템)")]
    [Tooltip("최대 연사속도에 도달하기까지 걸리는 조준 시간 (초)")]
    [SerializeField] private float windUpTime = 1.5f;

    [Tooltip("조준 해제 시 총열 회전이 완전히 멈추기까지 걸리는 시간 (초)")]
    [SerializeField] private float windDownTime = 2.0f;

    [Tooltip("최소 이 정도 비율(0.0~1.0) 이상 예열되어야 발사가 시작됨 (예: 0.2 = 20% 예열 완료 시 발사 가능)")]
    [SerializeField, Range(0f, 1f)] private float minSpoolToFire = 0.2f;

    [Tooltip("예열 진행도(0~1)에 따른 실제 연사속도 가속 곡선 (클릭하여 J자 모양처럼 후반에 치솟는 곡선으로 설정하세요)")]
    [SerializeField] private AnimationCurve windUpCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f));


    [Header("Ammo")]
    [Tooltip("한 탄창에 들어가는 총알 수")]
    [SerializeField] private int magazineSize = 200;

    [Tooltip("소지 가능한 최대 예비 탄창 수")]
    [SerializeField] private int maxReserveMagazines = 3;

    [Tooltip("모든 탄약을 다 소모했을 때 전체 재장전에 걸리는 기본 시간 (초)")]
    [SerializeField] private float baseFullReloadTime = 4.5f;


    [Header("Aim & Hitscan")]
    [Tooltip("히트스캔 레이캐스트 감지 최대 사거리")]
    [SerializeField] private float maxAimDistance = 150f;

    [Tooltip("조준 및 탄착점을 감지할 레이어 마스크")]
    [SerializeField] private LayerMask aimMask = ~0;

    [Tooltip("데미지를 입힐 적 오브젝트의 레이어 마스크")]
    [SerializeField] private LayerMask enemyMask = ~0;


    // =========================================================
    // Runtime Variables
    // =========================================================

    private int currentAmmo;
    private int reserveMagazines;
    private bool isReloading;
    private bool ammoInitialized;

    private float fireCooldownTimer = 100f;

    // 현재 예열 진행도 (시간 비례로 0f ~ 1f)
    private float currentSpool = 0f;


    // =========================================================
    // IAmmoWeapon Implementation
    // =========================================================

    public int CurrentAmmo => currentAmmo;
    public int MagazineSize => magazineSize;
    public int ReserveMagazines => reserveMagazines;
    public bool IsReloading => isReloading;

    public event Action OnAmmoStateChanged;


    // =========================================================
    // Weapon Lifecycle
    // =========================================================

    public override void Equip()
    {
        base.Equip();

        if (!ammoInitialized)
        {
            currentAmmo = magazineSize;
            reserveMagazines = maxReserveMagazines;
            ammoInitialized = true;
        }

        currentSpool = 0f;
        NotifyAmmo();
    }

    private void Update()
    {
        if (PlayerCamera == null || PlayerStats == null || Mouse.current == null)
            return;

        fireCooldownTimer += Time.deltaTime;

        if (isReloading)
            return;

        bool isAiming = Mouse.current.rightButton.isPressed;
        bool isShooting = Mouse.current.leftButton.isPressed;

        // ==========================================
        // 예열(Spooling) 진행도 수치 계산 (0 ~ 1)
        // ==========================================
        if (isAiming)
        {
            currentSpool = Mathf.MoveTowards(currentSpool, 1f, Time.deltaTime / windUpTime);
        }
        else
        {
            currentSpool = Mathf.MoveTowards(currentSpool, 0f, Time.deltaTime / windDownTime);
        }

        // ==========================================
        // 발사 시도
        // ==========================================
        if (isAiming && isShooting)
        {
            TryShoot();
        }
    }


    // =========================================================
    // Logic
    // =========================================================

    private void TryShoot()
    {
        if (currentSpool < minSpoolToFire)
            return;

        //  실시간 예열 상태를 바탕으로 '지금 당장 필요한 발사 딜레이' 계산
        float spoolMultiplier = windUpCurve.Evaluate(currentSpool);
        float maxFireRate = PlayerStats.FireRate * fireRateMultiplier;
        float finalFireRate = Mathf.Max(0.01f, maxFireRate * spoolMultiplier);

        float currentRequiredDelay = 1f / finalFireRate;

        //  누적된 스톱워치가 현재 필요한 딜레이보다 짧으면 발사하지 않음
        if (fireCooldownTimer < currentRequiredDelay)
            return;

        if (currentAmmo <= 0)
        {
            if (reserveMagazines > 0)
                LoadNextMagazine();
            else
                TryFullReload();

            return;
        }

        if (firePoint == null || trailPrefab == null)
            return;

        Shoot();

        // ✅ 3. 발사 후 스톱워치 차감 
        // (0으로 초기화하지 않고 요구 딜레이만큼만 빼주면 초고속 연사 시 미세한 프레임 손실을 막아줍니다)
        fireCooldownTimer -= currentRequiredDelay;
    }

    private void Shoot()
    {
        // 카메라 중앙 레이캐스트
        Ray aimRay = PlayerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 targetPoint;

        if (Physics.Raycast(aimRay, out RaycastHit hit, maxAimDistance, aimMask, QueryTriggerInteraction.Ignore))
        {
            targetPoint = hit.point;

            EnemyStats enemy = hit.collider.GetComponentInParent<EnemyStats>();
            if (enemy != null && !enemy.IsDead)
            {
                float finalDamage = PlayerStats.AttackPower * damageMultiplier;
                enemy.TakeDamage(finalDamage);
            }
        }
        else
        {
            targetPoint = aimRay.GetPoint(maxAimDistance);
        }

        // 궤적 이펙트 생성
        MinigunTrailVFX trail = Instantiate(trailPrefab, firePoint.position, Quaternion.identity);
        trail.DrawTrail(firePoint.position, targetPoint);

        currentAmmo--;
        NotifyAmmo();

        if (currentAmmo <= 0)
        {
            if (reserveMagazines > 0)
                LoadNextMagazine();
            else
                TryFullReload();
        }
    }

    private void LoadNextMagazine()
    {
        if (reserveMagazines <= 0)
            return;

        reserveMagazines--;
        currentAmmo = magazineSize;
        NotifyAmmo();
    }

    private void TryFullReload()
    {
        if (isReloading || (currentAmmo > 0 || reserveMagazines > 0))
            return;

        StartCoroutine(FullReloadRoutine());
    }

    private IEnumerator FullReloadRoutine()
    {
        isReloading = true;
        currentSpool = 0f;
        NotifyAmmo();

        float reloadTime = PlayerStats.GetReloadTime(baseFullReloadTime);
        yield return new WaitForSeconds(reloadTime);

        currentAmmo = magazineSize;
        reserveMagazines = maxReserveMagazines;
        isReloading = false;
        NotifyAmmo();
    }

    private void NotifyAmmo() => OnAmmoStateChanged?.Invoke();

    public override void Unequip()
    {
        StopAllCoroutines();
        isReloading = false;
        currentSpool = 0f;
        NotifyAmmo();
        base.Unequip();
    }
}