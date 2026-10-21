using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class MinigunWeapon : WeaponBase, IAmmoWeapon
{
    [Header("Minigun References")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private MinigunTrailVFX trailPrefab;

    [Header("Stat Multipliers (스탯 비례 배율)")]
    [Tooltip("플레이어 공격력의 몇 배로 데미지를 줄 것인가? (보통 미니건은 낮음: 0.3~0.5배)")]
    [SerializeField, Range(0.1f, 3f)] private float damageMultiplier = 0.5f;

    [Tooltip("플레이어 연사속도의 몇 배로 쏠 것인가? (미니건이므로 높음: 3~5배)")]
    [SerializeField, Range(1f, 10f)] private float fireRateMultiplier = 4f;

    [Header("Spooling (예열 시스템)")]
    [Tooltip("최대 연사속도에 도달하기까지 걸리는 조준 시간 (초)")]
    [SerializeField] private float windUpTime = 1.5f;

    [Tooltip("조준 해제 시 총열이 완전히 멈추기까지 걸리는 시간 (초)")]
    [SerializeField] private float windDownTime = 2.0f;

    [Tooltip("최소 이 정도 예열되어야 발사가 시작됨 (0.1 ~ 1.0)")]
    [SerializeField, Range(0f, 1f)] private float minSpoolToFire = 0.2f;

    [Header("Ammo")]
    [SerializeField] private int magazineSize = 200;
    [SerializeField] private int maxReserveMagazines = 3;
    [SerializeField] private float baseFullReloadTime = 4.5f;

    [Header("Aim")]
    [SerializeField] private float maxAimDistance = 150f;
    [SerializeField] private LayerMask aimMask = ~0;
    [SerializeField] private LayerMask enemyMask = ~0; // 적만 맞추기 위한 마스크 추가 권장

    // Runtime variables
    private int currentAmmo;
    private int reserveMagazines;
    private bool isReloading;
    private bool ammoInitialized;

    private float nextFireTime;

    // 현재 예열 상태 (0f = 정지, 1f = 최대 속도)
    private float currentSpool = 0f;

    // IAmmoWeapon 구현
    public int CurrentAmmo => currentAmmo;
    public int MagazineSize => magazineSize;
    public int ReserveMagazines => reserveMagazines;
    public bool IsReloading => isReloading;
    public event Action OnAmmoStateChanged;

    public override void Equip()
    {
        base.Equip();
        if (!ammoInitialized)
        {
            currentAmmo = magazineSize;
            reserveMagazines = maxReserveMagazines;
            ammoInitialized = true;
        }
        currentSpool = 0f; // 장착 시 총열 정지 상태
        NotifyAmmo();
    }

    private void Update()
    {
        if (PlayerCamera == null || PlayerStats == null || Mouse.current == null) return;
        if (isReloading) return;

        bool isAiming = Mouse.current.rightButton.isPressed;
        bool isShooting = Mouse.current.leftButton.isPressed;

        // ==========================================
        // 예열(Spooling) 로직
        // ==========================================
        if (isAiming)
        {
            // 우클릭 중이면 점점 속도 증가
            currentSpool = Mathf.MoveTowards(currentSpool, 1f, Time.deltaTime / windUpTime);
        }
        else
        {
            // 우클릭을 떼면 서서히 속도 감소
            currentSpool = Mathf.MoveTowards(currentSpool, 0f, Time.deltaTime / windDownTime);
        }

        // ==========================================
        // 발사 로직
        // ==========================================
        if (isAiming && isShooting)
        {
            TryShoot();
        }
    }

    private void TryShoot()
    {
        // 1. 일정 수준 이상 예열되지 않으면 쏘지 못함
        if (currentSpool < minSpoolToFire) return;

        // 2. 쿨타임 체크
        if (Time.time < nextFireTime) return;

        // 3. 탄약 체크
        if (currentAmmo <= 0)
        {
            if (reserveMagazines > 0) LoadNextMagazine();
            else TryFullReload();
            return;
        }

        if (firePoint == null || trailPrefab == null) return;

        Shoot();

        // 4. 예열 상태에 비례하여 연사 딜레이 적용
        // currentSpool이 1에 가까울수록 finalFireRate가 최대치가 됨
        float maxFireRate = PlayerStats.FireRate * fireRateMultiplier;
        float finalFireRate = Mathf.Max(0.01f, maxFireRate * currentSpool);

        nextFireTime = Time.time + (1f / finalFireRate);
    }

    private void Shoot()
    {
        // 1. 카메라 중앙에서 레이저 발사
        Ray aimRay = PlayerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 targetPoint;

        // 2. 충돌 체크 (히트스캔)
        if (Physics.Raycast(aimRay, out RaycastHit hit, maxAimDistance, aimMask, QueryTriggerInteraction.Ignore))
        {
            targetPoint = hit.point;

            // 적중한 대상이 적이라면 데미지 입힘
            EnemyStats enemy = hit.collider.GetComponentInParent<EnemyStats>();
            if (enemy != null && !enemy.IsDead)
            {
                float finalDamage = PlayerStats.AttackPower * damageMultiplier;
                enemy.TakeDamage(finalDamage);
            }
        }
        else
        {
            // 아무것도 안 맞았다면 최대 사거리 지점
            targetPoint = aimRay.GetPoint(maxAimDistance);
        }

        // 3. 발사 시각 효과 (노란 궤적 선) 생성
        MinigunTrailVFX trail = Instantiate(trailPrefab, firePoint.position, Quaternion.identity);
        trail.DrawTrail(firePoint.position, targetPoint);

        // 4. 탄약 차감
        currentAmmo--;
        NotifyAmmo();

        if (currentAmmo <= 0)
        {
            if (reserveMagazines > 0) LoadNextMagazine();
            else TryFullReload();
        }
    }

    private void LoadNextMagazine()
    {
        if (reserveMagazines <= 0) return;
        reserveMagazines--;
        currentAmmo = magazineSize;
        NotifyAmmo();
    }

    private void TryFullReload()
    {
        if (isReloading || (currentAmmo > 0 || reserveMagazines > 0)) return;
        StartCoroutine(FullReloadRoutine());
    }

    private IEnumerator FullReloadRoutine()
    {
        isReloading = true;
        currentSpool = 0f; // 재장전 시 총열 정지
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