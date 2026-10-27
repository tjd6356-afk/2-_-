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
    [Tooltip("공속이 가장 느릴 때(예열 직후)의 '1초당' 총 반동 강도")]
    [SerializeField] private float minCameraKickPerSec = 2.0f;

    [Tooltip("공속이 가장 빠를 때(예열 100%)의 '1초당' 총 반동 강도")]
    [SerializeField] private float maxCameraKickPerSec = 12.0f;

    [Tooltip("카메라가 좌우로 덜덜 흔들리는 비율")]
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

        // 장착 시 카메라 스크립트 연결
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

        // ✨ 핵심 수정 포인트: 쿨타임 저축 버그 완벽 차단 ✨
        // 타이머가 한 발 쏠 수 있는 딜레이 이상으로 과도하게 쌓여있다면 딱 한 발치로 깎아냅니다.
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

    private void Shoot(
    float delay
)
    {
        // =========================================================
        // Camera Recoil
        // =========================================================

        if (thirdPersonCam != null)
        {
            float currentKickPerSec =
                Mathf.Lerp(
                    minCameraKickPerSec,
                    maxCameraKickPerSec,
                    currentSpool
                );


            float kickThisShot =
                currentKickPerSec *
                delay;


            float kickPitch =
                kickThisShot;


            float kickYaw =
                UnityEngine.Random.Range(
                    -kickThisShot,
                    kickThisShot
                ) *
                horizontalKickRatio;


            thirdPersonCam.AddRecoil(
                kickPitch,
                kickYaw
            );
        }


        // =========================================================
        // 화면 중앙으로 Ray
        // =========================================================

        Ray aimRay =
            PlayerCamera.ViewportPointToRay(
                new Vector3(
                    0.5f,
                    0.5f,
                    0f
                )
            );


        Vector3 targetPoint;


        // =========================================================
        // Hit
        // =========================================================

        if (Physics.Raycast(
                aimRay,
                out RaycastHit hit,
                maxAimDistance,
                aimMask,
                QueryTriggerInteraction.Ignore))
        {
            targetPoint =
                hit.point;


            // =====================================================
            // Enemy / NPC 공통 체력 검색
            // =====================================================

            IHealth health =
                hit.collider
                    .GetComponentInParent<IHealth>();


            if (health != null &&
                !health.IsDead)
            {
                float finalDamage =
                    PlayerStats.AttackPower *
                    damageMultiplier;


                health.TakeDamage(
                    finalDamage
                );


                Debug.Log(
                    $"[Minigun] Hit : " +
                    $"{hit.collider.transform.root.name} / " +
                    $"Damage = {finalDamage}"
                );
            }
        }

        // =========================================================
        // 아무것도 안 맞은 경우
        // =========================================================

        else
        {
            targetPoint =
                aimRay.GetPoint(
                    maxAimDistance
                );
        }


        // =========================================================
        // Trail
        // =========================================================

        MinigunTrailVFX trail =
            Instantiate(
                trailPrefab,
                firePoint.position,
                Quaternion.identity
            );


        trail.DrawTrail(
            firePoint.position,
            targetPoint
        );


        // =========================================================
        // Ammo
        // =========================================================

        currentAmmo--;


        NotifyAmmo();


        if (currentAmmo <= 0)
        {
            if (reserveMagazines > 0)
            {
                LoadNextMagazine();
            }
            else
            {
                TryFullReload();
            }
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