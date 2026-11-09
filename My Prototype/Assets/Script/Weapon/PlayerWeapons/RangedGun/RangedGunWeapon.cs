using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class RangedGunWeapon : WeaponBase, IAmmoWeapon
{
    // =========================================================
    // Prefab 내부 References
    // =========================================================
    [Header("Weapon References")]
    [Tooltip("이 무기 Prefab 내부의 FirePoint")]
    [SerializeField] private Transform firePoint;

    [Tooltip("발사할 총알 Prefab")]
    [SerializeField] private Projectile projectilePrefab;

    // =========================================================
    // 스탯 배율 (공격력 / 공속)
    // =========================================================
    [Header("Stat Multipliers")]
    [Tooltip("플레이어 기본 공격력의 몇 배로 데미지를 줄 것인가?")]
    [SerializeField, Range(0.1f, 10f)] private float damageMultiplier = 1.0f;

    [Tooltip("플레이어 기본 연사속도의 몇 배로 쏠 것인가?")]
    [SerializeField, Range(0.1f, 10f)] private float fireRateMultiplier = 1.0f;

    // =========================================================
    // ✨ 수정됨: Smooth Camera Recoil (시간에 걸쳐 올라가는 부드러운 반동)
    // =========================================================
    [Header("Smooth Camera Recoil (카메라 반동)")]
    [Tooltip("최종적으로 도달할 위쪽 반동(Pitch) 목표치")]
    [SerializeField] private float targetKickPitch = 1.0f;

    [Tooltip("목표 반동까지 밀려 올라가는 데 걸리는 시간 (초) - 0.05초 정도로 짧게 주면 경쾌한 총기 반동이 됩니다")]
    [SerializeField] private float recoilDuration = 0.05f;

    [Tooltip("좌우 흔들림 비율")]
    [SerializeField] private float horizontalKickRatio = 0.5f;

    private ThirdPersonCamera thirdPersonCam;

    // =========================================================
    // Ammo
    // =========================================================
    [Header("Ammo")]
    [SerializeField] private int magazineSize = 5;
    [SerializeField] private int maxReserveMagazines = 5;
    [Tooltip("모든 탄약을 다 사용한 뒤 전체 회복 시간")]
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

    // =========================================================
    // IAmmoWeapon
    // =========================================================
    public int CurrentAmmo => currentAmmo;
    public int MagazineSize => magazineSize;
    public int ReserveMagazines => reserveMagazines;
    public bool IsReloading => isReloading;
    public event Action OnAmmoStateChanged;

    // =========================================================
    // 장착
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

        if (PlayerCamera != null)
        {
            thirdPersonCam = PlayerCamera.GetComponent<ThirdPersonCamera>();
        }

        NotifyAmmo();
    }

    private void Update()
    {
        if (PlayerCamera == null || PlayerStats == null) return;
        if (Mouse.current == null) return;
        if (isReloading) return;

        // ==========================================
        // RMB + LMB
        // ==========================================
        if (Mouse.current.rightButton.isPressed && Mouse.current.leftButton.isPressed)
        {
            TryShoot();
        }
    }

    // =========================================================
    // 발사 시도
    // =========================================================
    private void TryShoot()
    {
        if (Time.time < nextFireTime) return;

        if (currentAmmo <= 0)
        {
            if (reserveMagazines > 0)
            {
                LoadNextMagazine();
            }
            else
            {
                if (autoFullReloadWhenEmpty) TryFullReload();
                return;
            }
        }

        if (firePoint == null)
        {
            Debug.LogError($"{WeaponName} : FirePoint가 없습니다.");
            return;
        }

        if (projectilePrefab == null)
        {
            Debug.LogError($"{WeaponName} : Projectile Prefab이 없습니다.");
            return;
        }

        Shoot();

        float finalFireRate = Mathf.Max(0.01f, PlayerStats.FireRate * fireRateMultiplier);
        nextFireTime = Time.time + (1f / finalFireRate);
    }

    // =========================================================
    // 실제 총알 발사
    // =========================================================
    private void Shoot()
    {
        // ✨ 수정됨: 코루틴을 통해 지정된 시간(recoilDuration) 동안 반동을 쪼개서 적용
        if (thirdPersonCam != null)
        {
            float targetYaw = UnityEngine.Random.Range(-targetKickPitch, targetKickPitch) * horizontalKickRatio;
            StartCoroutine(SmoothRecoilRoutine(targetKickPitch, targetYaw, recoilDuration));
        }

        Ray aimRay = PlayerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 targetPoint;

        if (Physics.Raycast(aimRay, out RaycastHit hit, maxAimDistance, aimMask, QueryTriggerInteraction.Ignore))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = aimRay.GetPoint(maxAimDistance);
        }

        Vector3 shootDirection = (targetPoint - firePoint.position).normalized;

        Projectile projectile = Instantiate(
            projectilePrefab,
            firePoint.position,
            Quaternion.LookRotation(shootDirection)
        );

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

    private void LoadNextMagazine()
    {
        if (reserveMagazines <= 0) return;

        reserveMagazines--;
        currentAmmo = magazineSize;
        NotifyAmmo();

        Debug.Log($"{WeaponName} 다음 탄창 사용 | 탄창 {reserveMagazines} | 탄약 {currentAmmo}/{magazineSize}");
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
        Debug.Log($"{WeaponName} 전체 재장전 시작");

        yield return new WaitForSeconds(reloadTime);

        currentAmmo = magazineSize;
        reserveMagazines = maxReserveMagazines;
        isReloading = false;
        NotifyAmmo();

        Debug.Log($"{WeaponName} 전체 재장전 완료");
    }

    private void NotifyAmmo()
    {
        OnAmmoStateChanged?.Invoke();
    }

    public override void Unequip()
    {
        StopAllCoroutines();
        isReloading = false;
        NotifyAmmo();
        base.Unequip();
    }

    // ✨ 목표 반동량(n)을 시간(duration)에 걸쳐 부드럽게 주입하는 코루틴
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

            // 마지막 프레임 보정 (오버해서 반동이 들어가지 않도록)
            if (elapsed + dt > duration)
                dt = duration - elapsed;

            // 1프레임당 들어가야 할 반동량 계산
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