using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class MultipleLauncherWeapon : WeaponBase, IAmmoWeapon
{
    [Header("Launcher References")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private RocketProjectile rocketPrefab;

    [Header("Burst Settings (점사 설정)")]
    [Tooltip("한 번에 점사로 발사할 로켓의 개수")]
    [SerializeField] private int burstCount = 3;

    [Tooltip("점사 시 로켓과 로켓 사이의 발사 간격 (초)")]
    [SerializeField] private float burstInterval = 0.08f;

    [Header("Stat Multipliers (기본 스탯 비례 설정)")]
    [Tooltip("플레이어 공격력의 몇 배로 데미지를 줄 것인가?")]
    [SerializeField, Range(1f, 10f)] private float damageMultiplier = 5f;

    [Tooltip("점사가 모두 끝난 후, 다음 점사까지의 쿨타임을 결정하는 연사 배율")]
    [SerializeField, Range(0.1f, 1f)] private float fireRateMultiplier = 0.25f;

    [Header("Ammo")]
    [SerializeField] private int magazineSize = 12; // 점사 무기이므로 탄창을 좀 더 늘리는 것을 추천합니다
    [SerializeField] private int maxReserveMagazines = 3;
    [SerializeField] private float baseFullReloadTime = 3.5f;

    [Header("Aim")]
    [SerializeField] private float maxAimDistance = 100f;
    [SerializeField] private LayerMask aimMask = ~0;

    // Runtime variables
    private int currentAmmo;
    private int reserveMagazines;
    private bool isReloading;
    private bool ammoInitialized;
    private float nextFireTime;

    // 점사 진행 중인지 체크하는 변수
    private bool isBursting;

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

        isBursting = false; // 장착 시 점사 상태 초기화
        NotifyAmmo();
    }

    private void Update()
    {
        if (PlayerCamera == null || PlayerStats == null || Mouse.current == null) return;
        if (isReloading || isBursting) return; // 장전 중이거나 점사 발사 중이면 추가 입력 무시

        // 마우스를 누르고 있으면 쿨타임마다 자동으로 점사 발사
        if (Mouse.current.rightButton.isPressed && Mouse.current.leftButton.isPressed)
        {
            TryShoot();
        }
    }

    private void TryShoot()
    {
        if (Time.time < nextFireTime) return;

        if (currentAmmo <= 0)
        {
            if (reserveMagazines > 0) LoadNextMagazine();
            else TryFullReload();
            return;
        }

        if (firePoint == null || rocketPrefab == null) return;

        // 점사 코루틴 시작
        StartCoroutine(BurstShootRoutine());
    }

    // ✨ 핵심: 지정된 개수(burstCount)만큼 간격(burstInterval)을 두고 발사하는 코루틴
    private IEnumerator BurstShootRoutine()
    {
        isBursting = true; // 점사 시작

        for (int i = 0; i < burstCount; i++)
        {
            if (currentAmmo <= 0) break; // 점사 도중 탄약이 0이 되면 즉시 중단

            ShootSingleRocket();

            // 마지막 발사가 아니고 탄약이 남아있다면 간격만큼 대기
            if (i < burstCount - 1 && currentAmmo > 0)
            {
                yield return new WaitForSeconds(burstInterval);
            }
        }

        // 점사가 모두 끝난 후 런처 특유의 느린 쿨타임 적용
        float finalFireRate = Mathf.Max(0.01f, PlayerStats.FireRate * fireRateMultiplier);
        nextFireTime = Time.time + (1f / finalFireRate);

        isBursting = false; // 점사 종료

        // 점사 종료 후 탄약이 비어있다면 자동 장전
        if (currentAmmo <= 0)
        {
            if (reserveMagazines > 0) LoadNextMagazine();
            else TryFullReload();
        }
    }

    // 단일 로켓 1발을 쏘는 로직 (기존 Shoot과 동일하되 이름만 변경)
    private void ShootSingleRocket()
    {
        // 매 발사마다 화면 중앙을 다시 계산합니다 (점사 도중 마우스를 움직여도 따라가도록)
        Ray aimRay = PlayerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 targetPoint = Physics.Raycast(aimRay, out RaycastHit hit, maxAimDistance, aimMask, QueryTriggerInteraction.Ignore)
            ? hit.point : aimRay.GetPoint(maxAimDistance);

        Vector3 shootDirection = (targetPoint - firePoint.position).normalized;

        RocketProjectile rocket = Instantiate(rocketPrefab, firePoint.position, Quaternion.LookRotation(shootDirection));

        float finalDamage = PlayerStats.AttackPower * damageMultiplier;

        rocket.Launch(shootDirection, finalDamage, WeaponManager.gameObject);

        currentAmmo--;
        NotifyAmmo();
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
        isBursting = false; // 무기 스왑 시 점사 초기화
        NotifyAmmo();
        base.Unequip();
    }
}