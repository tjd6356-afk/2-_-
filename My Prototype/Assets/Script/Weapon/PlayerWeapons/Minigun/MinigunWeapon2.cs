using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MinigunWeapon2 : WeaponBase, IAmmoWeapon
{
    [Header("Minigun References")]
    [Tooltip("총알 및 히트스캔 궤적이 시작될 총구 위치 Transform")]
    [SerializeField] private Transform firePoint;

    [Tooltip("히트스캔 궤적을 그려줄 LineRenderer 프리팹")]
    [SerializeField] private MinigunTrailVFX trailPrefab;


    [Header("Explosive Rounds (폭발탄 세팅)")]
    [Tooltip("탄착 지점에 생성될 폭발 이펙트 프리팹")]
    [SerializeField] private ExplosionVFX explosionPrefab;

    [Tooltip("폭발 데미지가 들어가는 광역 범위 (반지름)")]
    [SerializeField] private float explosionRadius = 1.5f;

    // ✨ 직격과 폭발 배율 분리 ✨
    [Tooltip("총알에 직접 맞은 적에게 들어가는 '직격' 데미지 배율 (예: 1.0 = 100%)")]
    [SerializeField, Range(0.1f, 5f)] private float directDamageMultiplier = 1.0f;

    [Tooltip("폭발 범위 내의 모든 적(직격 대상 포함)에게 들어가는 '스플래시' 데미지 배율 (예: 0.5 = 50%)")]
    [SerializeField, Range(0.1f, 5f)] private float explosionDamageMultiplier = 0.5f;


    [Header("Stat Multipliers (스탯 비례 배율)")]
    [Tooltip("플레이어 기본 연사속도(FireRate)에 곱해질 최대 연사 배율")]
    [SerializeField, Range(1f, 10f)] private float fireRateMultiplier = 4f;


    [Header("Spooling (예열 시스템)")]
    [SerializeField] private float windUpTime = 1.5f;
    [SerializeField] private float windDownTime = 2.0f;
    [SerializeField, Range(0f, 1f)] private float minSpoolToFire = 0.2f;
    [SerializeField] private AnimationCurve windUpCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f));

    [Header("Barrel Rotation (총열 회전)")]
    [Tooltip("회전시킬 총열 모델의 Transform (몸통 말고 총열 부품)")]
    [SerializeField] private Transform barrelTransform;

    [Tooltip("최대 예열 상태(100%)일 때의 1초당 회전 속도")]
    [SerializeField] private float maxBarrelRotationSpeed = 1000f;

    [Tooltip("총열이 회전할 로컬 축 (모델에 따라 X, Y, Z 중 하나를 1로 설정)")]
    [SerializeField] private Vector3 barrelRotationAxis = Vector3.forward;

    [Header("Camera Recoil (카메라 반동/흔들림)")]
    [SerializeField] private float minCameraKickPerSec = 2.0f;
    [SerializeField] private float maxCameraKickPerSec = 12.0f;
    [SerializeField] private float horizontalKickRatio = 0.5f;

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

        if (PlayerCamera != null)
        {
            thirdPersonCam = PlayerCamera.GetComponent<ThirdPersonCamera>();
        }

        NotifyAmmo();
    }

    private void Update()
    {
        if (PlayerGameplayControl.GameplayLocked) return;
        if (PlayerCamera == null || PlayerStats == null || Mouse.current == null) return;

        fireCooldownTimer += Time.deltaTime;

        if (isReloading) return;

        bool isAiming = Mouse.current.rightButton.isPressed;
        bool isShooting = Mouse.current.leftButton.isPressed;

        if (isAiming)
            currentSpool = Mathf.MoveTowards(currentSpool, 1f, Time.deltaTime / windUpTime);
        else
            currentSpool = Mathf.MoveTowards(currentSpool, 0f, Time.deltaTime / windDownTime);


        //예열 진행도에 비례한 총열 회전 적용
        if (barrelTransform != null)
        {
            float currentRotationSpeed = maxBarrelRotationSpeed * currentSpool;
            barrelTransform.Rotate(barrelRotationAxis * currentRotationSpeed * Time.deltaTime, Space.Self);
        }

        if (isAiming && isShooting)
        {
            TryShoot();
        }
    }

    private void TryShoot()
    {
        if (currentSpool < minSpoolToFire) return;

        float spoolMultiplier = windUpCurve.Evaluate(currentSpool);
        float maxFireRate = PlayerStats.FireRate * fireRateMultiplier;
        float finalFireRate = Mathf.Max(0.01f, maxFireRate * spoolMultiplier);

        float currentRequiredDelay = 1f / finalFireRate;

        if (fireCooldownTimer > currentRequiredDelay)
        {
            fireCooldownTimer = currentRequiredDelay;
        }

        if (fireCooldownTimer < currentRequiredDelay) return;

        if (currentAmmo <= 0)
        {
            if (reserveMagazines > 0) LoadNextMagazine();
            else TryFullReload();
            return;
        }

        if (firePoint == null || trailPrefab == null) return;

        Shoot(currentRequiredDelay);

        fireCooldownTimer -= currentRequiredDelay;
    }

    private void Shoot(float delay)
    {
        // 1. Camera Recoil
        if (thirdPersonCam != null)
        {
            float currentKickPerSec = Mathf.Lerp(minCameraKickPerSec, maxCameraKickPerSec, currentSpool);
            float kickThisShot = currentKickPerSec * delay;

            float kickPitch = kickThisShot;
            float kickYaw = UnityEngine.Random.Range(-kickThisShot, kickThisShot) * horizontalKickRatio;

            thirdPersonCam.AddRecoil(kickPitch, kickYaw);
        }

        Ray aimRay = PlayerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 targetPoint;

        // =========================================================
        // ✨ 2. 직격(Direct Hit) 판정 및 데미지 적용
        // =========================================================
        if (Physics.Raycast(aimRay, out RaycastHit hit, maxAimDistance, aimMask, QueryTriggerInteraction.Ignore))
        {
            targetPoint = hit.point;

            IHealth directHitTarget = hit.collider.GetComponentInParent<IHealth>();
            if (directHitTarget != null && !directHitTarget.IsDead)
            {
                // 직격 데미지 배율(directDamageMultiplier) 적용
                float directDamage = PlayerStats.AttackPower * directDamageMultiplier;
                directHitTarget.TakeDamage(directDamage);

                // Debug.Log($"[Explosive Minigun] Direct Hit! Damage: {directDamage}");
            }
        }
        else
        {
            targetPoint = aimRay.GetPoint(maxAimDistance);
        }


        // =========================================================
        // ✨ 3. 폭발(Splash) 이펙트 생성 및 광역 데미지 적용
        // =========================================================
        if (explosionPrefab != null)
        {
            ExplosionVFX explosion = Instantiate(explosionPrefab, targetPoint, Quaternion.identity);
            explosion.SetRadius(explosionRadius);
        }

        // 스플래시 데미지 배율(explosionDamageMultiplier) 적용
        float splashDamage = PlayerStats.AttackPower * explosionDamageMultiplier;

        Collider[] hitColliders = Physics.OverlapSphere(targetPoint, explosionRadius, aimMask, QueryTriggerInteraction.Ignore);
        HashSet<IHealth> damagedTargets = new HashSet<IHealth>();

        foreach (Collider col in hitColliders)
        {
            IHealth health = col.GetComponentInParent<IHealth>();
            if (health != null && !health.IsDead)
            {
                if (damagedTargets.Add(health))
                {
                    health.TakeDamage(splashDamage);

                    // Debug.Log($"[Explosive Minigun] Splash Hit! Damage: {splashDamage}");
                }
            }
        }


        // 4. Trail 및 Ammo
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