using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class ClusterLauncherWeapon : WeaponBase, IAmmoWeapon
{
    [Header("Launcher References")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private ClusterRocketProjectile rocketPrefab;

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

    [Header("Smooth Camera Recoil (묵직한 반동)")]
    [Tooltip("최종적으로 도달할 위쪽 반동(Pitch) 목표치")]
    [SerializeField] private float targetKickPitch = 5.0f;

    [Tooltip("목표 반동까지 밀려 올라가는 데 걸리는 시간 (초) - 이 값이 속도를 결정합니다")]
    [SerializeField] private float recoilDuration = 0.15f;

    [Tooltip("좌우 흔들림 비율")]
    [SerializeField] private float horizontalKickRatio = 0.5f;

    [Header("Player Knockback (발사 반동 밀림)")]
    [Tooltip("발사 시 플레이어가 반대 방향으로 밀려나는 힘 (0이면 안 밀림)")]
    [SerializeField] private float playerKnockbackForce = 5f;

    [Tooltip("위(Y축)로 솟아오르는 힘의 비율 (1 = 100%, 0.3 = 30%만 적용)")]
    [SerializeField, Range(0f, 1f)] private float verticalKnockbackMultiplier = 0.2f;

    [Tooltip("공중에 있을 때 수평(X, Z축)으로 밀리는 힘의 비율 (0.5 = 50%만 밀림)")]
    [SerializeField, Range(0f, 1f)] private float airborneHorizontalMultiplier = 0.4f;


    // Runtime variables
    private int currentAmmo;
    private int reserveMagazines;
    private bool isReloading;
    private bool ammoInitialized;
    private float nextFireTime;
    private ThirdPersonCamera thirdPersonCam;

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

        if (PlayerCamera != null)
        {
            thirdPersonCam = PlayerCamera.GetComponent<ThirdPersonCamera>();
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
        if (thirdPersonCam != null)
        {
            float targetYaw = UnityEngine.Random.Range(-targetKickPitch, targetKickPitch) * horizontalKickRatio;
            StartCoroutine(SmoothRecoilRoutine(targetKickPitch, targetYaw, recoilDuration));
        }

        Ray aimRay = PlayerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 targetPoint = Physics.Raycast(aimRay, out RaycastHit hit, maxAimDistance, aimMask, QueryTriggerInteraction.Ignore)
            ? hit.point : aimRay.GetPoint(maxAimDistance);

        Vector3 shootDirection = (targetPoint - firePoint.position).normalized;

        if (playerKnockbackForce > 0f)
        {
            Vector3 knockbackDir = -shootDirection;

            // 공중인지 체크
            bool isAirborne = PlayerController != null && !PlayerController.isGrounded;

            if (isAirborne)
            {
                knockbackDir.x *= airborneHorizontalMultiplier;
                knockbackDir.z *= airborneHorizontalMultiplier;
            }

            if (knockbackDir.y > 0)
            {
                knockbackDir.y *= verticalKnockbackMultiplier;
            }

            PlayerMovement movement = WeaponManager.GetComponent<PlayerMovement>();
            if (movement != null)
            {
                // ✨ 핵심: 공중(isAirborne)일 경우 기존 속도를 지워버리라고 2번째 인자로 전달
                movement.AddKnockback(knockbackDir * playerKnockbackForce, isAirborne);
            }
        }

        ClusterRocketProjectile rocket = Instantiate(rocketPrefab, firePoint.position, Quaternion.LookRotation(shootDirection));

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
}