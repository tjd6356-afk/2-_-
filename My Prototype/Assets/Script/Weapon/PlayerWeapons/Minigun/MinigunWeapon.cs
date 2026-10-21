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
    [Tooltip("플레이어 기본 공격력(AttackPower)에 곱해질 데미지 배율")]
    [SerializeField, Range(0.1f, 3f)] private float damageMultiplier = 0.5f;

    [Tooltip("플레이어 기본 연사속도(FireRate)에 곱해질 최대 연사 배율")]
    [SerializeField, Range(1f, 10f)] private float fireRateMultiplier = 4f;

    [Header("Spooling (예열 시스템)")]
    [Tooltip("최대 연사속도에 도달하기까지 걸리는 조준 시간 (초)")]
    [SerializeField] private float windUpTime = 1.5f;

    [Tooltip("조준 해제 시 총열 회전이 완전히 멈추기까지 걸리는 시간 (초)")]
    [SerializeField] private float windDownTime = 2.0f;

    [Tooltip("최소 이 정도 비율(0.0~1.0) 이상 예열되어야 발사가 시작됨")]
    [SerializeField, Range(0f, 1f)] private float minSpoolToFire = 0.2f;

    [Tooltip("예열 진행도(0~1)에 따른 실제 연사속도 가속 곡선")]
    [SerializeField] private AnimationCurve windUpCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f));

    [Header("Camera Recoil (카메라 반동/흔들림)")]
    [Tooltip("첫 발사 시 카메라가 튀는 최대 반동 강도")]
    [SerializeField] private float maxCameraKick = 1.5f;

    [Tooltip("연사를 지속하여 완전히 안정화되었을 때의 최소 카메라 반동 강도")]
    [SerializeField] private float minCameraKick = 0.2f;

    [Tooltip("최대 반동에서 최소 반동으로 카메라가 안정화되는 데 걸리는 시간 (초)")]
    [SerializeField] private float recoilStabilizeTime = 2.0f;

    [Header("Ammo & Aim")]
    [SerializeField] private int magazineSize = 200;
    [SerializeField] private int maxReserveMagazines = 3;
    [SerializeField] private float baseFullReloadTime = 4.5f;
    [SerializeField] private float maxAimDistance = 150f;
    [SerializeField] private LayerMask aimMask = ~0;

    // Runtime Variables
    private int currentAmmo;
    private int reserveMagazines;
    private bool isReloading;
    private bool ammoInitialized;

    private float currentSpool = 0f;
    private float fireCooldownTimer = 100f;
    private float currentSustainedFireTime = 0f;

    // 카메라 연동용 레퍼런스
    private ThirdPersonCamera thirdPersonCam;

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

        currentSpool = 0f;
        currentSustainedFireTime = 0f;

        // 장착 시 PlayerCamera에서 ThirdPersonCamera 컴포넌트를 가져옴
        if (PlayerCamera != null)
        {
            thirdPersonCam = PlayerCamera.GetComponent<ThirdPersonCamera>();
        }

        NotifyAmmo();
    }

    private void Update()
    {
        if (PlayerCamera == null || PlayerStats == null || Mouse.current == null) return;

        fireCooldownTimer += Time.deltaTime;

        if (isReloading) return;

        bool isAiming = Mouse.current.rightButton.isPressed;
        bool isShooting = Mouse.current.leftButton.isPressed;

        if (isAiming)
            currentSpool = Mathf.MoveTowards(currentSpool, 1f, Time.deltaTime / windUpTime);
        else
            currentSpool = Mathf.MoveTowards(currentSpool, 0f, Time.deltaTime / windDownTime);

        if (isAiming && isShooting)
        {
            currentSustainedFireTime += Time.deltaTime;
            TryShoot();
        }
        else
        {
            currentSustainedFireTime = Mathf.Max(0f, currentSustainedFireTime - Time.deltaTime * 2f);
        }
    }

    private void TryShoot()
    {
        if (currentSpool < minSpoolToFire) return;

        float spoolMultiplier = windUpCurve.Evaluate(currentSpool);
        float maxFireRate = PlayerStats.FireRate * fireRateMultiplier;
        float finalFireRate = Mathf.Max(0.01f, maxFireRate * spoolMultiplier);

        float currentRequiredDelay = 1f / finalFireRate;

        if (fireCooldownTimer < currentRequiredDelay) return;

        if (currentAmmo <= 0)
        {
            if (reserveMagazines > 0) LoadNextMagazine();
            else TryFullReload();
            return;
        }

        if (firePoint == null || trailPrefab == null) return;

        Shoot();

        fireCooldownTimer -= currentRequiredDelay;
    }

    private void Shoot()
    {
        // ==========================================
        // 1. ThirdPersonCamera 전용 반동(Kick) 전송
        // ==========================================
        if (thirdPersonCam != null)
        {
            float stabilizeProgress = Mathf.Clamp01(currentSustainedFireTime / recoilStabilizeTime);
            float currentKick = Mathf.Lerp(maxCameraKick, minCameraKick, stabilizeProgress);

            // 위로 튀는 힘(Pitch)과 좌우 무작위 떨림(Yaw) 계산
            float kickPitch = currentKick;
            float kickYaw = UnityEngine.Random.Range(-currentKick, currentKick) * 0.5f;

            // 카메라 스크립트에 반동 주입
            thirdPersonCam.AddRecoil(kickPitch, kickYaw);
        }

        // ==========================================
        // 2. 정확하게 화면 정중앙으로 발사 (탄퍼짐 없음)
        // ==========================================
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

        MinigunTrailVFX trail = Instantiate(trailPrefab, firePoint.position, Quaternion.identity);
        trail.DrawTrail(firePoint.position, targetPoint);

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
        currentSpool = 0f;
        currentSustainedFireTime = 0f;
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
        currentSustainedFireTime = 0f;
        NotifyAmmo();
        base.Unequip();
    }
}