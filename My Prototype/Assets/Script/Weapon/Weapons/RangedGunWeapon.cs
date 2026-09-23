using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class RangedGunWeapon : WeaponBase
{
    [Header("References")]
    [SerializeField]
    private Camera playerCamera;

    [SerializeField]
    private Transform firePoint;

    [SerializeField]
    private Projectile projectilePrefab;

    [SerializeField]
    private PlayerStats playerStats;


    [Header("Ammo")]
    [SerializeField]
    private int magazineSize = 5;

    [SerializeField]
    private int maxReserveMagazines = 5;

    [SerializeField]
    private float baseReloadTime = 2f;


    [Tooltip(
        "켜면 R 재장전 완료 시 탄약과 예비 탄창을 전부 최대치로 복구"
    )]
    [SerializeField]
    private bool refillEverythingOnReload = false;


    [Header("Aim")]
    [SerializeField]
    private float maxAimDistance = 100f;

    [SerializeField]
    private LayerMask aimMask = ~0;


    private int currentAmmo;

    private int reserveMagazines;

    private bool isReloading;

    private float nextFireTime;


    public int CurrentAmmo =>
        currentAmmo;

    public int ReserveMagazines =>
        reserveMagazines;

    public int MagazineSize =>
        magazineSize;


    public event Action<int, int, int>
        OnAmmoChanged;


    private void Awake()
    {
        if (playerCamera == null)
        {
            playerCamera =
                Camera.main;
        }


        if (playerStats == null)
        {
            playerStats =
                GetComponent<PlayerStats>();
        }


        currentAmmo =
            magazineSize;

        reserveMagazines =
            maxReserveMagazines;
    }


    private void Update()
    {
        if (Mouse.current == null ||
            Keyboard.current == null)
        {
            return;
        }


        if (Keyboard.current
            .rKey
            .wasPressedThisFrame)
        {
            TryReload();
        }


        if (isReloading)
            return;


        // 우클릭 중 + 좌클릭
        if (Mouse.current
                .rightButton
                .isPressed &&
            Mouse.current
                .leftButton
                .isPressed)
        {
            TryShoot();
        }
    }


    private void TryShoot()
    {
        if (Time.time <
            nextFireTime)
        {
            return;
        }


        if (currentAmmo <= 0)
        {
            TryReload();
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


        currentAmmo--;


        NotifyAmmo();
    }


    private void TryReload()
    {
        if (isReloading)
            return;


        if (refillEverythingOnReload)
        {
            if (currentAmmo ==
                    magazineSize &&
                reserveMagazines ==
                    maxReserveMagazines)
            {
                return;
            }
        }
        else
        {
            if (currentAmmo >=
                magazineSize)
            {
                return;
            }


            if (reserveMagazines <= 0)
                return;
        }


        StartCoroutine(
            ReloadRoutine()
        );
    }


    private IEnumerator ReloadRoutine()
    {
        isReloading = true;


        float reloadTime =
            playerStats.GetReloadTime(
                baseReloadTime
            );


        yield return
            new WaitForSeconds(
                reloadTime
            );


        if (refillEverythingOnReload)
        {
            // 특수 모드
            currentAmmo =
                magazineSize;

            reserveMagazines =
                maxReserveMagazines;
        }
        else
        {
            // 일반적인 탄창 교체
            reserveMagazines--;

            currentAmmo =
                magazineSize;
        }


        isReloading = false;


        NotifyAmmo();
    }


    private void NotifyAmmo()
    {
        OnAmmoChanged?.Invoke(
            currentAmmo,
            magazineSize,
            reserveMagazines
        );
    }


    public override void Unequip()
    {
        StopAllCoroutines();

        isReloading = false;

        base.Unequip();
    }
}