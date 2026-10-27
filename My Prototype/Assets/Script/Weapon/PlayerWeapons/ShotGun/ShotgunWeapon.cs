using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ShotgunWeapon : WeaponBase, IAmmoWeapon
{
    [Header("Shotgun References")]
    [Tooltip("총알(히트스캔 궤적)이 시작될 총구 위치")]
    [SerializeField] private Transform firePoint;

    [Tooltip("히트스캔 궤적을 그려줄 LineRenderer 프리팹")]
    [SerializeField] private MinigunTrailVFX trailPrefab;


    [Header("Stat Multipliers (스탯 비례 배율)")]
    [Tooltip("플레이어 공격력에 곱해질 샷건의 '총' 데미지 배율 (전체 산탄이 다 맞았을 때의 데미지)")]
    [SerializeField, Range(1f, 10f)] private float damageMultiplier = 3.0f;

    [Tooltip("플레이어 연사속도에 곱해질 배율 (펌프 액션이므로 0.2~0.5배 추천)")]
    [SerializeField, Range(0.1f, 2f)] private float fireRateMultiplier = 0.3f;


    [Header("Pellet & Spread (산탄 및 탄퍼짐)")]
    [Tooltip("한 번 발사 시 쪼개져 나가는 산탄의 개수")]
    [SerializeField, Range(2, 30)] private int pelletCount = 8;

    [Tooltip("기본(최대) 탄퍼짐 원의 반지름 (비조준 상태 또는 발사 직후 넓어진 상태)")]
    [SerializeField, Range(0.1f, 5f)] private float maxSpreadRadius = 2.0f;

    [Tooltip("조준을 끝까지 유지했을 때 최대로 좁아지는 탄퍼짐 원의 반지름")]
    [SerializeField, Range(0.01f, 5f)] private float minSpreadRadius = 0.5f;

    [Tooltip("최대 퍼짐에서 최소 퍼짐까지 좁아지는데 걸리는 조준 시간 (초)")]
    [SerializeField, Range(0.1f, 5f)] private float aimTightenTime = 2.0f;

    [Tooltip("산탄끼리 뭉치는 것을 방지하기 위한 최소 간격 비율 (0 = 뭉침 허용, 1 = 꽉 차게 밀어냄)")]
    [SerializeField, Range(0f, 1f)] private float antiClumpFactor = 0.4f;


    [Header("Camera Recoil (카메라 반동)")]
    [Tooltip("발사 시 카메라가 위로 강하게 튀는 힘")]
    [SerializeField] private float kickPitch = 3.0f;

    [Tooltip("발사 시 카메라가 좌우로 무작위로 흔들리는 힘")]
    [SerializeField] private float kickYaw = 1.0f;


    [Header("Ammo & Aim")]
    [SerializeField] private int magazineSize = 6;
    [SerializeField] private int maxReserveMagazines = 5;
    [SerializeField] private float baseFullReloadTime = 3.0f;
    [SerializeField] private float maxAimDistance = 50f;
    [SerializeField] private LayerMask aimMask = ~0;


    // =========================================================
    // Runtime Variables
    // =========================================================
    private int currentAmmo;
    private int reserveMagazines;
    private bool isReloading;
    private bool ammoInitialized;
    private float fireCooldownTimer = 100f;

    // 조준에 따른 탄퍼짐 진행도 (0 = 최대 넓음, 1 = 최소로 좁아짐)
    private float currentAimProgress = 0f;

    private ThirdPersonCamera thirdPersonCam;


    // =========================================================
    // IAmmoWeapon Implementation
    // =========================================================
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

        currentAimProgress = 0f;

        if (PlayerCamera != null)
            thirdPersonCam = PlayerCamera.GetComponent<ThirdPersonCamera>();

        NotifyAmmo();
    }

    private void Update()
    {
        if (PlayerCamera == null || PlayerStats == null || Mouse.current == null) return;

        fireCooldownTimer += Time.deltaTime;

        if (isReloading) return;

        bool isAiming = Mouse.current.rightButton.isPressed;
        bool isShooting = Mouse.current.leftButton.isPressed;

        // ==========================================
        // 조준(우클릭) 유지 시 탄퍼짐 좁히기
        // ==========================================
        if (isAiming)
        {
            currentAimProgress = Mathf.MoveTowards(currentAimProgress, 1f, Time.deltaTime / aimTightenTime);
        }
        else
        {
            // 우클릭을 떼면 즉시 조준이 풀리며 탄퍼짐 최대치로 복구
            currentAimProgress = 0f;
        }

        if (isAiming && isShooting)
        {
            TryShoot();
        }
    }

    private void TryShoot()
    {
        float finalFireRate = Mathf.Max(0.01f, PlayerStats.FireRate * fireRateMultiplier);
        float currentRequiredDelay = 1f / finalFireRate;

        // 쿨타임 저축 버그 차단
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

        Shoot();

        fireCooldownTimer -= currentRequiredDelay;
    }

    private void Shoot()
    {
        // 1. 카메라 반동
        if (thirdPersonCam != null)
        {
            float randomYaw = UnityEngine.Random.Range(-kickYaw, kickYaw);
            thirdPersonCam.AddRecoil(kickPitch, randomYaw);
        }

        // 2. 조준 진행도를 기반으로 현재 탄퍼짐 범위 계산
        float currentSpreadRadius = Mathf.Lerp(maxSpreadRadius, minSpreadRadius, currentAimProgress);
        float totalDamage = PlayerStats.AttackPower * damageMultiplier;
        float damagePerPellet = totalDamage / pelletCount;

        Ray centerRay = PlayerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        List<Vector2> pelletOffsets = GenerateAntiClumpOffsets(currentSpreadRadius);

        // ✨ 데미지 합산용 딕셔너리 (이번 발사에서 맞은 대상과 누적 데미지 기록) ✨
        Dictionary<IHealth, float> damageMap = new Dictionary<IHealth, float>();

        // 3. 산탄 발사 (레이캐스트 계산)
        for (int i = 0; i < pelletCount; i++)
        {
            Vector2 offset = pelletOffsets[i];

            Vector3 spreadDirection = centerRay.direction
                                      + (PlayerCamera.transform.right * offset.x)
                                      + (PlayerCamera.transform.up * offset.y);
            spreadDirection.Normalize();

            Ray pelletRay = new Ray(centerRay.origin, spreadDirection);
            Vector3 targetPoint;

            if (Physics.Raycast(pelletRay, out RaycastHit hit, maxAimDistance, aimMask, QueryTriggerInteraction.Ignore))
            {
                targetPoint = hit.point;

                IHealth targetHealth = hit.collider.GetComponentInParent<IHealth>();
                if (targetHealth != null && !targetHealth.IsDead)
                {
                    // 즉시 데미지를 주지 않고, 딕셔너리에 대상과 데미지를 누적시킵니다.
                    if (damageMap.ContainsKey(targetHealth))
                    {
                        damageMap[targetHealth] += damagePerPellet;
                    }
                    else
                    {
                        damageMap[targetHealth] = damagePerPellet;
                    }
                }
            }
            else
            {
                targetPoint = pelletRay.GetPoint(maxAimDistance);
            }

            // 시각 효과 생성
            MinigunTrailVFX trail = Instantiate(trailPrefab, firePoint.position, Quaternion.identity);
            trail.DrawTrail(firePoint.position, targetPoint);
        }

        // ✨ 4. 누적된 데미지를 대상별로 딱 한 번씩만 일괄 적용 ✨
        foreach (var kvp in damageMap)
        {
            kvp.Key.TakeDamage(kvp.Value);
        }

        // 5. 발사 즉시 조준 진행도 초기화
        currentAimProgress = 0f;

        // 6. 탄약 차감
        currentAmmo--;
        NotifyAmmo();

        if (currentAmmo <= 0)
        {
            if (reserveMagazines > 0) LoadNextMagazine();
            else TryFullReload();
        }
    }

    // =========================================================
    // 뭉침 방지 알고리즘 (현재 탄퍼짐 범위를 받아서 처리)
    // =========================================================
    private List<Vector2> GenerateAntiClumpOffsets(float currentRadius)
    {
        List<Vector2> offsets = new List<Vector2>();

        // 현재 탄퍼짐 범위에 비례하여 펠릿 간 최소 간격 조정
        float minDistance = (currentRadius / Mathf.Sqrt(pelletCount)) * antiClumpFactor;

        for (int i = 0; i < pelletCount; i++)
        {
            Vector2 candidate = Vector2.zero;
            bool valid = false;

            for (int attempt = 0; attempt < 15; attempt++)
            {
                // 현재 좁혀진 반경(currentRadius) 안에서 랜덤 점 생성
                candidate = UnityEngine.Random.insideUnitCircle * currentRadius;
                valid = true;

                foreach (Vector2 existingOffset in offsets)
                {
                    if (Vector2.Distance(candidate, existingOffset) < minDistance)
                    {
                        valid = false;
                        break;
                    }
                }

                if (valid) break;
            }

            offsets.Add(candidate);
        }

        return offsets;
    }

    // =========================================================
    // 재장전 & 해제
    // =========================================================
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
        currentAimProgress = 0f;
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
        currentAimProgress = 0f;
        NotifyAmmo();
        base.Unequip();
    }
}