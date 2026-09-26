using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class RangedGunWeapon : WeaponBase
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform firePoint;
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private PlayerStats playerStats;


    [Header("Ammo")]
    [Tooltip("현재 장전되는 총알 수")]
    [SerializeField] private int magazineSize = 5;

    [Tooltip("가지고 시작하는 예비 탄창 수")]
    [SerializeField] private int maxReserveMagazines = 5;

    [Tooltip("모든 탄약을 소모한 뒤 전체 보급에 걸리는 기본 시간")]
    [SerializeField] private float baseFullReloadTime = 2f;

    [Tooltip("모든 탄약을 소모하면 자동으로 전체 재장전")]
    [SerializeField] private bool autoFullReloadWhenEmpty = true;


    [Header("Aim")]
    [SerializeField] private float maxAimDistance = 100f;
    [SerializeField] private LayerMask aimMask = ~0;


    private int currentAmmo;
    private int reserveMagazines;

    private bool isReloading;
    private bool initialized;

    private float nextFireTime;


    // =========================================================
    // 외부 접근
    // =========================================================

    public int CurrentAmmo => currentAmmo;

    public int MagazineSize => magazineSize;

    public int ReserveMagazines => reserveMagazines;

    public bool IsReloading => isReloading;


    // =========================================================
    // UI Events
    // =========================================================

    public event Action<int, int, int> OnAmmoChanged;

    public event Action<bool> OnReloadStateChanged;


    private void Awake()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }


        if (playerStats == null)
        {
            playerStats =
                GetComponent<PlayerStats>();
        }


        InitializeAmmo();
    }


    private void InitializeAmmo()
    {
        if (initialized)
            return;


        initialized = true;

        currentAmmo =
            magazineSize;

        reserveMagazines =
            maxReserveMagazines;
    }


    public override void Equip()
    {
        InitializeAmmo();

        base.Equip();

        NotifyAmmoChanged();
    }


    private void Update()
    {
        if (Mouse.current == null ||
            Keyboard.current == null)
        {
            return;
        }


        // ==========================================
        // 전부 소모한 상태에서 R을 눌러도 전체 재장전 가능
        // ==========================================

        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            if (currentAmmo <= 0 &&
                reserveMagazines <= 0)
            {
                TryFullReload();
            }
        }


        if (isReloading)
            return;


        // ==========================================
        // 우클릭 + 좌클릭
        // ==========================================

        if (Mouse.current.rightButton.isPressed &&
            Mouse.current.leftButton.isPressed)
        {
            TryShoot();
        }
    }


    // =========================================================
    // 사격
    // =========================================================

    private void TryShoot()
    {
        if (Time.time < nextFireTime)
            return;


        // 현재 5발을 다 썼다면
        if (currentAmmo <= 0)
        {
            // 예비 탄창이 있으면 재장전 시간 없이
            // 즉시 다음 탄창으로 교체
            if (reserveMagazines > 0)
            {
                LoadNextMagazine();
            }
            else
            {
                if (autoFullReloadWhenEmpty)
                {
                    TryFullReload();
                }

                return;
            }
        }


        if (playerCamera == null ||
            firePoint == null ||
            projectilePrefab == null ||
            playerStats == null)
        {
            return;
        }


        Shoot();


        nextFireTime =
            Time.time +
            (
                1f /
                Mathf.Max(
                    0.01f,
                    playerStats.FireRate
                )
            );
    }


    // =========================================================
    // 실제 총알 발사
    // =========================================================

    private void Shoot()
    {
        Ray ray =
            playerCamera.ViewportPointToRay(
                new Vector3(
                    0.5f,
                    0.5f,
                    0f
                )
            );


        Vector3 targetPoint;


        if (Physics.Raycast(
                ray,
                out RaycastHit hit,
                maxAimDistance,
                aimMask,
                QueryTriggerInteraction.Ignore))
        {
            targetPoint =
                hit.point;
        }
        else
        {
            targetPoint =
                ray.GetPoint(
                    maxAimDistance
                );
        }


        Vector3 direction =
            (
                targetPoint -
                firePoint.position
            ).normalized;


        Projectile projectile =
            Instantiate(
                projectilePrefab,
                firePoint.position,
                Quaternion.LookRotation(
                    direction
                )
            );


        projectile.Launch(
            direction,
            playerStats.AttackPower,
            gameObject
        );


        // 한 발 소비
        currentAmmo--;


        NotifyAmmoChanged();


        // ==========================================
        // 지금 쏜 게 마지막 총알
        // ==========================================

        if (currentAmmo <= 0)
        {
            // 예비 탄창이 있으면 즉시 다음 탄창 사용
            if (reserveMagazines > 0)
            {
                LoadNextMagazine();
            }

            // 예비 탄창까지 전부 소모
            else if (autoFullReloadWhenEmpty)
            {
                TryFullReload();
            }
        }
    }


    // =========================================================
    // 다음 탄창으로 즉시 교체
    //
    // 재장전 시간 없음
    // =========================================================

    private void LoadNextMagazine()
    {
        if (reserveMagazines <= 0)
            return;


        reserveMagazines--;


        currentAmmo =
            magazineSize;


        Debug.Log(
            $"다음 탄창 사용 / 탄창 {reserveMagazines} / 탄약 {currentAmmo}"
        );


        NotifyAmmoChanged();
    }


    // =========================================================
    // 모든 탄약을 다 쓴 후 전체 재장전
    // =========================================================

    private void TryFullReload()
    {
        if (isReloading)
            return;


        // 아직 사용할 탄약이 남아있다면 전체 재장전 불가
        if (currentAmmo > 0 ||
            reserveMagazines > 0)
        {
            return;
        }


        StartCoroutine(
            FullReloadRoutine()
        );
    }


    private IEnumerator FullReloadRoutine()
    {
        isReloading = true;


        OnReloadStateChanged?.Invoke(
            true
        );


        float reloadTime =
            baseFullReloadTime;


        if (playerStats != null)
        {
            reloadTime =
                playerStats.GetReloadTime(
                    baseFullReloadTime
                );
        }


        Debug.Log(
            $"원거리 무기 전체 재장전 시작 : {reloadTime:F2}초"
        );


        yield return new WaitForSeconds(
            reloadTime
        );


        // ==========================================
        // 탄창 + 총알 전부 최대치 복원
        // ==========================================

        reserveMagazines =
            maxReserveMagazines;


        currentAmmo =
            magazineSize;


        isReloading = false;


        NotifyAmmoChanged();


        OnReloadStateChanged?.Invoke(
            false
        );


        Debug.Log(
            "원거리 무기 전체 재장전 완료"
        );
    }


    private void NotifyAmmoChanged()
    {
        OnAmmoChanged?.Invoke(
            currentAmmo,
            magazineSize,
            reserveMagazines
        );
    }


    public override void Unequip()
    {
        if (isReloading)
        {
            StopAllCoroutines();

            isReloading = false;


            OnReloadStateChanged?.Invoke(
                false
            );
        }


        base.Unequip();
    }


    private void OnValidate()
    {
        magazineSize =
            Mathf.Max(
                1,
                magazineSize
            );


        maxReserveMagazines =
            Mathf.Max(
                0,
                maxReserveMagazines
            );


        baseFullReloadTime =
            Mathf.Max(
                0.01f,
                baseFullReloadTime
            );
    }
}