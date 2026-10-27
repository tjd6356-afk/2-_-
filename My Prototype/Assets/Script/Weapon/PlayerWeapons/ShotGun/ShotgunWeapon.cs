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

    [Tooltip("히트스캔 궤적을 그려줄 LineRenderer 프리팹 (미니건에서 쓰던 것 그대로 사용 가능)")]
    [SerializeField] private MinigunTrailVFX trailPrefab;

    [Header("Stat Multipliers (스탯 비례 배율)")]
    [Tooltip("플레이어 공격력에 곱해질 샷건의 '총' 데미지 배율 (전체 산탄이 다 맞았을 때의 데미지)")]
    [SerializeField, Range(1f, 10f)] private float damageMultiplier = 3.0f;

    [Tooltip("플레이어 연사속도에 곱해질 배율 (샷건은 펌프 액션이므로 0.2~0.5배 추천)")]
    [SerializeField, Range(0.1f, 2f)] private float fireRateMultiplier = 0.3f;

    [Header("Pellet & Spread (산탄 설정)")]
    [Tooltip("한 번 발사 시 쪼개져 나가는 산탄의 개수")]
    [SerializeField, Range(2, 30)] private int pelletCount = 8;

    [Tooltip("탄퍼짐 원의 반지름 (값이 클수록 더 넓게 퍼짐)")]
    [SerializeField, Range(0.1f, 5f)] private float spreadRadius = 1.5f;

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
    [SerializeField] private float maxAimDistance = 50f; // 샷건이므로 사거리를 좀 짧게
    [SerializeField] private LayerMask aimMask = ~0;

    // Runtime Variables
    private int currentAmmo;
    private int reserveMagazines;
    private bool isReloading;
    private bool ammoInitialized;
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

        // 샷건은 꾹 누르고 있는 것(isPressed)보다 클릭(wasPressedThisFrame)마다 쏘는 펌프액션 느낌이 좋습니다.
        // 하지만 기존 시스템과 동일하게 꾹 눌러도 연사가 되도록 하되, 타이머로 제어합니다.
        bool isShooting = Mouse.current.leftButton.isPressed;

        if (isAiming && isShooting)
        {
            TryShoot();
        }
    }

    private void TryShoot()
    {
        float finalFireRate = Mathf.Max(0.01f, PlayerStats.FireRate * fireRateMultiplier);
        float currentRequiredDelay = 1f / finalFireRate;

        // 쿨타임 저축 버그 방지
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
        // 1. 카메라 반동 (강력한 단발 킥)
        if (thirdPersonCam != null)
        {
            float randomYaw = UnityEngine.Random.Range(-kickYaw, kickYaw);
            thirdPersonCam.AddRecoil(kickPitch, randomYaw);
        }

        // 2. 전체 데미지를 산탄 개수만큼 분할 (예: 총 데미지 100, 10발이면 1발당 10데미지)
        float totalDamage = PlayerStats.AttackPower * damageMultiplier;
        float damagePerPellet = totalDamage / pelletCount;

        // 화면 중앙 조준선
        Ray centerRay = PlayerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        // 뭉침 방지 알고리즘을 통해 생성된 산탄의 분포 오프셋들
        List<Vector2> pelletOffsets = GenerateAntiClumpOffsets();

        // 3. 각각의 산탄 발사 처리
        for (int i = 0; i < pelletCount; i++)
        {
            Vector2 offset = pelletOffsets[i];

            // 카메라의 Right와 Up 벡터를 사용해 2D 오프셋을 3D 조준 방향으로 변환
            Vector3 spreadDirection = centerRay.direction
                                      + (PlayerCamera.transform.right * offset.x)
                                      + (PlayerCamera.transform.up * offset.y);
            spreadDirection.Normalize();

            Ray pelletRay = new Ray(centerRay.origin, spreadDirection);
            Vector3 targetPoint;

            if (Physics.Raycast(pelletRay, out RaycastHit hit, maxAimDistance, aimMask, QueryTriggerInteraction.Ignore))
            {
                targetPoint = hit.point;

                // ✨ 새로운 IHealth 인터페이스 적용 ✨
                // 상대가 EnemyStats인지 나무상자인지 상관없이 IHealth만 있으면 데미지를 줌
                IHealth targetHealth = hit.collider.GetComponentInParent<IHealth>();
                if (targetHealth != null && !targetHealth.IsDead)
                {
                    targetHealth.TakeDamage(damagePerPellet);
                }
            }
            else
            {
                targetPoint = pelletRay.GetPoint(maxAimDistance);
            }

            // 시각 효과 생성 (산탄 하나당 궤적 1개씩)
            MinigunTrailVFX trail = Instantiate(trailPrefab, firePoint.position, Quaternion.identity);
            trail.DrawTrail(firePoint.position, targetPoint);
        }

        // 4. 탄약 차감
        currentAmmo--;
        NotifyAmmo();

        if (currentAmmo <= 0)
        {
            if (reserveMagazines > 0) LoadNextMagazine();
            else TryFullReload();
        }
    }

    // =========================================================
    // 뭉침 방지 (Anti-Clumping) 오프셋 생성 알고리즘 (Poisson-like)
    // =========================================================
    private List<Vector2> GenerateAntiClumpOffsets()
    {
        List<Vector2> offsets = new List<Vector2>();

        // 산탄 개수와 퍼짐 반경에 따라 최소 요구 간격을 계산
        // antiClumpFactor가 높을수록 펠릿끼리 강제로 멀어집니다.
        float minDistance = (spreadRadius / Mathf.Sqrt(pelletCount)) * antiClumpFactor;

        for (int i = 0; i < pelletCount; i++)
        {
            Vector2 candidate = Vector2.zero;
            bool valid = false;

            // 뭉침을 피하기 위해 최대 15번까지 랜덤 위치를 다시 뽑습니다.
            for (int attempt = 0; attempt < 15; attempt++)
            {
                // 원 안의 랜덤한 점 생성
                candidate = UnityEngine.Random.insideUnitCircle * spreadRadius;
                valid = true;

                // 기존에 뽑힌 산탄들과 거리를 비교
                foreach (Vector2 existingOffset in offsets)
                {
                    if (Vector2.Distance(candidate, existingOffset) < minDistance)
                    {
                        valid = false; // 너무 가까우면 실패, 다시 뽑기
                        break;
                    }
                }

                if (valid) break; // 충분히 멀면 합격
            }

            // 15번을 시도해도 자리가 없으면 그냥 마지막에 뽑힌 걸 씁니다 (무한루프 방지)
            offsets.Add(candidate);
        }

        return offsets;
    }

    // =========================================================
    // 재장전 로직
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