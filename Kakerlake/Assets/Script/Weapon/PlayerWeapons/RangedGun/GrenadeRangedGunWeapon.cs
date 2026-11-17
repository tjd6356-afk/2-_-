using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class GrenageRangedGunWeapon : WeaponBase, IAmmoWeapon
{
    // =========================================================
    // Prefab 내부 References
    // =========================================================
    [Header("Weapon References")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private Projectile projectilePrefab;

    // =========================================================
    // 스탯 배율
    // =========================================================
    [Header("Stat Multipliers")]
    [SerializeField, Range(0.1f, 10f)] private float damageMultiplier = 1.0f;
    [SerializeField, Range(0.1f, 10f)] private float fireRateMultiplier = 1.0f;

    // =========================================================
    // ✨ 추가됨: Grenade (수류탄 투척 시스템)
    // =========================================================
    [Header("Grenade Settings (Q키 투척)")]
    [Tooltip("수류탄 프리팹 (비워두면 투척 불가)")]
    [SerializeField] private GrenadeProjectile grenadePrefab;

    [Tooltip("플레이어 공격력의 몇 배로 수류탄 데미지를 줄 것인가?")]
    [SerializeField, Range(0.1f, 10f)] private float grenadeDamageMultiplier = 3.0f;

    [Tooltip("투척하는 힘 (멀리 날아가는 정도)")]
    [SerializeField] private float grenadeThrowForce = 20f;

    [Tooltip("포물선 각도 (0: 정면, 값이 클수록 위로 높게 던짐)")]
    [SerializeField, Range(0f, 1f)] private float grenadeUpwardAngle = 0.3f;

    [Tooltip("수류탄 투척 쿨타임 (초)")]
    [SerializeField] private float grenadeCooldown = 5f;

    private float nextGrenadeTime;

    // =========================================================
    // Smooth Camera Recoil 
    // =========================================================
    [Header("Smooth Camera Recoil (카메라 반동)")]
    [SerializeField] private float targetKickPitch = 1.0f;
    [SerializeField] private float recoilDuration = 0.05f;
    [SerializeField] private float horizontalKickRatio = 0.5f;

    private ThirdPersonCamera thirdPersonCam;

    // =========================================================
    // Ammo
    // =========================================================
    [Header("Ammo")]
    [SerializeField] private int magazineSize = 5;
    [SerializeField] private int maxReserveMagazines = 5;
    [SerializeField] private float baseFullReloadTime = 2f;
    [SerializeField] private bool autoFullReloadWhenEmpty = true;

    // =========================================================
    // Aim
    // =========================================================
    [Header("Aim")]
    [SerializeField] private float maxAimDistance = 100f;
    [SerializeField] private LayerMask aimMask = ~0;

    // =========================================================
    // Runtime
    // =========================================================
    private int currentAmmo;
    private int reserveMagazines;
    private bool isReloading;
    private bool ammoInitialized;
    private float nextFireTime;

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

        if (PlayerCamera != null)
        {
            thirdPersonCam = PlayerCamera.GetComponent<ThirdPersonCamera>();
        }

        NotifyAmmo();
    }

    private void Update()
    {
        if (PlayerCamera == null || PlayerStats == null || Mouse.current == null || Keyboard.current == null) return;

        // ✨ 핵심: 총알 장전 중이든 아니든, 수류탄은 쿨타임만 맞으면 독립적으로 던질 수 있습니다.
        if (grenadePrefab != null && Keyboard.current.qKey.wasPressedThisFrame)
        {
            TryThrowGrenade();
        }

        if (isReloading) return;

        if (Mouse.current.rightButton.isPressed && Mouse.current.leftButton.isPressed)
        {
            TryShoot();
        }
    }

    // =========================================================
    // ✨ 수류탄 투척 로직
    // =========================================================
    private void TryThrowGrenade()
    {
        if (Time.time < nextGrenadeTime) return;
        if (firePoint == null) return;

        // 조준점(십자선) 기반 타겟 포인트 계산
        Ray aimRay = PlayerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 targetPoint = Physics.Raycast(aimRay, out RaycastHit hit, maxAimDistance, aimMask, QueryTriggerInteraction.Ignore)
            ? hit.point : aimRay.GetPoint(maxAimDistance);

        // 기본 방향 계산
        Vector3 throwDirection = (targetPoint - firePoint.position).normalized;

        // 포물선을 그리기 위해 위쪽 방향 벡터를 더해주어 각도를 올립니다.
        throwDirection = (throwDirection + Vector3.up * grenadeUpwardAngle).normalized;

        GrenadeProjectile grenade = Instantiate(grenadePrefab, firePoint.position, Quaternion.identity);

        // 무기 배율이 아닌 수류탄 고유의 고정 데미지 + 플레이어 기본 공격력 사용
        float finalGrenadeDamage = PlayerStats.AttackPower * grenadeDamageMultiplier;

        grenade.Launch(throwDirection, grenadeThrowForce, finalGrenadeDamage, WeaponManager.gameObject);
        
        nextGrenadeTime = Time.time + grenadeCooldown;
        Debug.Log($"[수류탄] 투척 완료! (다음 투척까지 {grenadeCooldown}초)");
    }

    // =========================================================
    // 사격 로직 (기존과 동일)
    // =========================================================
    private void TryShoot()
    {
        if (Time.time < nextFireTime) return;

        if (currentAmmo <= 0)
        {
            if (reserveMagazines > 0) LoadNextMagazine();
            else if (autoFullReloadWhenEmpty) TryFullReload();
            return;
        }

        if (firePoint == null || projectilePrefab == null) return;

        Shoot();

        float finalFireRate = Mathf.Max(0.01f, PlayerStats.FireRate * fireRateMultiplier);
        nextFireTime = Time.time + (1f / finalFireRate);
    }

    private void Shoot()
    {
        if (thirdPersonCam != null)
        {
            float targetYaw = UnityEngine.Random.Range(-targetKickPitch, targetKickPitch) * horizontalKickRatio;
            StartCoroutine(SmoothRecoilRoutine(targetKickPitch, targetYaw, recoilDuration));
        }

        Ray aimRay = PlayerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 targetPoint = Physics.Raycast(aimRay, out RaycastHit hit, maxAimDistance, aimMask, QueryTriggerInteraction.Ignore)
            ? hit.point : aimRay.GetPoint(maxAimDistance);

        Vector3 shootDirection = (targetPoint - firePoint.position).normalized;

        Projectile projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(shootDirection));

        float finalDamage = PlayerStats.AttackPower * damageMultiplier;
        projectile.Launch(shootDirection, finalDamage, WeaponManager.gameObject);

        currentAmmo--;
        NotifyAmmo();

        if (currentAmmo <= 0)
        {
            if (reserveMagazines > 0) LoadNextMagazine();
            else if (autoFullReloadWhenEmpty) TryFullReload();
        }
    }

    // (장전, 코루틴 등 아래쪽 코드는 동일하게 유지됩니다)
    private void LoadNextMagazine()
    {
        if (reserveMagazines <= 0) return;
        reserveMagazines--;
        currentAmmo = magazineSize;
        NotifyAmmo();
    }

    private void TryFullReload()
    {
        if (isReloading) return;
        if (currentAmmo > 0 || reserveMagazines > 0) return;
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

    private IEnumerator SmoothRecoilRoutine(float totalPitch, float totalYaw, float duration)
    {
        if (duration <= 0f)
        {
            thirdPersonCam.AddRecoil(totalPitch, totalYaw);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float dt = Time.deltaTime;
            if (elapsed + dt > duration) dt = duration - elapsed;
            float framePitch = (totalPitch / duration) * dt;
            float frameYaw = (totalYaw / duration) * dt;
            thirdPersonCam.AddRecoil(framePitch, frameYaw);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private void OnValidate()
    {
        magazineSize = Mathf.Max(1, magazineSize);
        maxReserveMagazines = Mathf.Max(0, maxReserveMagazines);
        baseFullReloadTime = Mathf.Max(0.01f, baseFullReloadTime);
    }
}