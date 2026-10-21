using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using static Readme;

public class RangedGunWeapon : WeaponBase, IAmmoWeapon
{
    // =========================================================
    // Prefab 내부 References
    // =========================================================

    [Header("Weapon References")]

    [Tooltip("이 무기 Prefab 내부의 FirePoint")]
    [SerializeField]
    private Transform firePoint;

    [Tooltip("발사할 총알 Prefab")]
    [SerializeField]
    private Projectile projectilePrefab;


    // =========================================================
    // Ammo
    // =========================================================

    [Header("Ammo")]

    [SerializeField]
    private int magazineSize = 5;

    [SerializeField]
    private int maxReserveMagazines = 5;

    [Tooltip("모든 탄약을 다 사용한 뒤 전체 회복 시간")]
    [SerializeField]
    private float baseFullReloadTime = 2f;

    [SerializeField]
    private bool autoFullReloadWhenEmpty = true;


    // =========================================================
    // Aim
    // =========================================================

    [Header("Aim")]

    [SerializeField]
    private float maxAimDistance = 100f;

    [SerializeField]
    private LayerMask aimMask = ~0;


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

    public int CurrentAmmo =>
        currentAmmo;

    public int MagazineSize =>
        magazineSize;

    public int ReserveMagazines =>
        reserveMagazines;

    public bool IsReloading =>
        isReloading;


    public event Action OnAmmoStateChanged;


    // =========================================================
    // 장착
    // =========================================================

    public override void Equip()
    {
        base.Equip();


        // 처음 생성된 무기라면 탄약 초기화
        if (!ammoInitialized)
        {
            currentAmmo =
                magazineSize;

            reserveMagazines =
                maxReserveMagazines;

            ammoInitialized =
                true;
        }


        NotifyAmmo();
    }


    private void Update()
    {
        /*
         * 중요한 점:
         *
         * PlayerCamera와 PlayerStats는
         * WeaponBase.Initialize()에서
         * PlayerWeaponManager가 전달해준다.
         *
         * Prefab Inspector에서 연결할 필요 없음.
         */

        if (PlayerCamera == null ||
            PlayerStats == null)
        {
            return;
        }


        if (Mouse.current == null)
            return;


        // 재장전 중에는 공격 불가
        if (isReloading)
            return;


        // ==========================================
        // RMB + LMB
        // ==========================================

        if (Mouse.current.rightButton.isPressed &&
            Mouse.current.leftButton.isPressed)
        {
            TryShoot();
        }
    }


    // =========================================================
    // 발사 시도
    // =========================================================

    private void TryShoot()
    {
        if (Time.time <
            nextFireTime)
        {
            return;
        }


        // ==========================================
        // 현재 5발을 다 사용함
        // ==========================================

        if (currentAmmo <= 0)
        {
            // 예비 탄창이 있다면
            // 기다리지 않고 바로 다음 탄창 사용
            if (reserveMagazines > 0)
            {
                LoadNextMagazine();
            }
            else
            {
                // 예비 탄창까지 전부 소모
                if (autoFullReloadWhenEmpty)
                {
                    TryFullReload();
                }


                return;
            }
        }


        if (firePoint == null)
        {
            Debug.LogError(
                $"{WeaponName} : FirePoint가 없습니다."
            );

            return;
        }


        if (projectilePrefab == null)
        {
            Debug.LogError(
                $"{WeaponName} : Projectile Prefab이 없습니다."
            );

            return;
        }


        Shoot();


        nextFireTime =
            Time.time +
            (
                1f /
                Mathf.Max(
                    0.01f,
                    PlayerStats.FireRate
                )
            );
    }


    // =========================================================
    // 실제 총알 발사
    // =========================================================

    private void Shoot()
    {
        // 화면 중앙 십자선
        Ray aimRay =
            PlayerCamera.ViewportPointToRay(
                new Vector3(
                    0.5f,
                    0.5f,
                    0f
                )
            );


        Vector3 targetPoint;


        if (Physics.Raycast(
                aimRay,
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
                aimRay.GetPoint(
                    maxAimDistance
                );
        }


        // ==========================================
        // Prefab 안의 FirePoint에서 발사
        // ==========================================

        Vector3 shootDirection =
            (
                targetPoint -
                firePoint.position
            ).normalized;


        Projectile projectile =
            Instantiate(
                projectilePrefab,
                firePoint.position,
                Quaternion.LookRotation(
                    shootDirection
                )
            );


        /*
         * Owner는 무기 Prefab이 아니라 Player여야 한다.
         *
         * WeaponManager가 Player에 붙어 있으므로
         * WeaponManager.gameObject == Player
         */
        projectile.Launch(
            shootDirection,
            PlayerStats.AttackPower,
            WeaponManager.gameObject
        );


        currentAmmo--;


        NotifyAmmo();


        // ==========================================
        // 방금 마지막 총알을 쐈음
        // ==========================================

        if (currentAmmo <= 0)
        {
            if (reserveMagazines > 0)
            {
                LoadNextMagazine();
            }
            else if (
                autoFullReloadWhenEmpty)
            {
                TryFullReload();
            }
        }
    }


    // =========================================================
    // 예비 탄창 즉시 사용
    // =========================================================

    private void LoadNextMagazine()
    {
        if (reserveMagazines <= 0)
            return;


        reserveMagazines--;


        currentAmmo =
            magazineSize;


        NotifyAmmo();


        Debug.Log(
            $"{WeaponName} 다음 탄창 사용 " +
            $"| 탄창 {reserveMagazines} " +
            $"| 탄약 {currentAmmo}/{magazineSize}"
        );
    }


    // =========================================================
    // 전부 소진 후 전체 재장전
    // =========================================================

    private void TryFullReload()
    {
        if (isReloading)
            return;


        // 아직 사용할 탄약이 있다면 실행하지 않음
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
        isReloading =
            true;


        NotifyAmmo();


        float reloadTime =
            PlayerStats.GetReloadTime(
                baseFullReloadTime
            );


        Debug.Log(
            $"{WeaponName} 전체 재장전 시작"
        );


        yield return
            new WaitForSeconds(
                reloadTime
            );


        // ==========================================
        // 완전히 최대치 회복
        // ==========================================

        currentAmmo =
            magazineSize;


        reserveMagazines =
            maxReserveMagazines;


        isReloading =
            false;


        NotifyAmmo();


        Debug.Log(
            $"{WeaponName} 전체 재장전 완료"
        );
    }


    // =========================================================
    // UI 갱신
    // =========================================================

    private void NotifyAmmo()
    {
        OnAmmoStateChanged?.Invoke();
    }


    // =========================================================
    // 무기 해제
    // =========================================================

    public override void Unequip()
    {
        StopAllCoroutines();


        isReloading =
            false;


        NotifyAmmo();


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