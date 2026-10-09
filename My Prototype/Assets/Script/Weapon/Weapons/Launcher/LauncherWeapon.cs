using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class LauncherWeapon : WeaponBase, IAmmoWeapon
{
    [Header("Launcher References")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private RocketProjectile rocketPrefab;

    [Header("Stat Multipliers (기본 스탯 비례 설정)")]
    [Tooltip("플레이어 공격력의 몇 배로 데미지를 줄 것인가? (요청하신 4~6배)")]
    [SerializeField, Range(1f, 10f)] private float damageMultiplier = 5f;

    [Tooltip("플레이어 연사속도의 몇 배로 쏠 것인가? (요청하신 0.2~0.3배)")]
    [SerializeField, Range(0.1f, 1f)] private float fireRateMultiplier = 0.25f;

    [Header("Ammo")]
    [SerializeField] private int magazineSize = 3;
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
        NotifyAmmo();
    }

    private void Update()
    {
        if (PlayerCamera == null || PlayerStats == null || Mouse.current == null) return;
        if (isReloading) return;

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

        Shoot();

        // 런처 특유의 느린 연사속도 적용 (기본 연사속도 * 배율)
        float finalFireRate = Mathf.Max(0.01f, PlayerStats.FireRate * fireRateMultiplier);
        nextFireTime = Time.time + (1f / finalFireRate);
    }

    private void Shoot()
    {
        Ray aimRay = PlayerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 targetPoint = Physics.Raycast(aimRay, out RaycastHit hit, maxAimDistance, aimMask, QueryTriggerInteraction.Ignore)
            ? hit.point : aimRay.GetPoint(maxAimDistance);

        Vector3 shootDirection = (targetPoint - firePoint.position).normalized;

        RocketProjectile rocket = Instantiate(rocketPrefab, firePoint.position, Quaternion.LookRotation(shootDirection));

        // 플레이어 기본 공격력에 런처 배율(4~6배)을 곱해서 로켓에 전달
        float finalDamage = PlayerStats.AttackPower * damageMultiplier;

        rocket.Launch(shootDirection, finalDamage, WeaponManager.gameObject);

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
        NotifyAmmo();
        base.Unequip();
    }
}