using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShooter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform firePoint;
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private ThirdPersonCamera thirdPersonCamera;
    [SerializeField] private PlayerStats playerStats;


    [Header("Aim")]
    [SerializeField] private float maxAimDistance = 100f;
    [SerializeField] private LayerMask aimLayerMask = ~0;


    [Header("Ammo")]
    [Tooltip("한 탄창에 들어가는 총알 수")]
    [SerializeField] private int magazineSize = 30;

    [Tooltip("현재 가지고 있는 예비 탄창 수")]
    [SerializeField] private int reserveMagazines = 3;

    [Tooltip("Reload Speed가 1일 때 걸리는 기본 재장전 시간")]
    [SerializeField] private float baseReloadTime = 2.5f;

    [Tooltip("탄약이 0이 되면 자동으로 재장전")]
    [SerializeField] private bool autoReloadWhenEmpty = true;


    private int currentAmmo;

    private float nextFireTime;

    private bool isReloading;


    // =========================================================
    // 외부에서 확인할 수 있는 값
    // =========================================================

    public int CurrentAmmo => currentAmmo;

    public int MagazineSize => magazineSize;

    public int ReserveMagazines => reserveMagazines;

    public bool IsReloading => isReloading;


    // UI가 사용할 이벤트
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
    }


    private void Start()
    {
        // 게임 시작 시 현재 탄창을 가득 채움
        currentAmmo = magazineSize;

        NotifyAmmoChanged();
    }


    private void Update()
    {
        // =====================================================
        // R키 재장전
        // =====================================================

        if (Keyboard.current != null &&
            Keyboard.current.rKey.wasPressedThisFrame)
        {
            TryReload();
        }


        // 재장전 중에는 총 발사 불가능
        if (isReloading)
            return;


        if (Mouse.current == null)
            return;


        // 우클릭 조준 중에만 발사
        if (thirdPersonCamera != null &&
            !thirdPersonCamera.IsAiming)
        {
            return;
        }


        // =====================================================
        // 좌클릭 연사
        // =====================================================

        if (Mouse.current.leftButton.isPressed)
        {
            TryShoot();
        }
    }


    // =========================================================
    // 총을 쏠 수 있는지 검사
    // =========================================================

    private void TryShoot()
    {
        // 발사속도 제한
        if (Time.time < nextFireTime)
            return;


        // 탄약이 없음
        if (currentAmmo <= 0)
        {
            if (autoReloadWhenEmpty)
            {
                TryReload();
            }

            return;
        }


        // 필수 오브젝트가 없으면 발사 X
        if (playerCamera == null ||
            firePoint == null ||
            projectilePrefab == null)
        {
            return;
        }


        Shoot();


        float fireRate =
            playerStats != null
                ? playerStats.FireRate
                : 1f;


        nextFireTime =
            Time.time +
            (1f / Mathf.Max(0.01f, fireRate));
    }


    // =========================================================
    // 실제 발사
    // =========================================================

    private void Shoot()
    {
        // 총알 한 발 소비
        currentAmmo--;

        NotifyAmmoChanged();


        // =====================================================
        // 화면 정중앙 십자선 Ray
        // =====================================================

        Ray aimRay =
            playerCamera.ViewportPointToRay(
                new Vector3(
                    0.5f,
                    0.5f,
                    0f
                )
            );


        Vector3 targetPoint;


        // =====================================================
        // 십자선이 가리키는 지점
        // =====================================================

        if (Physics.Raycast(
                aimRay,
                out RaycastHit hit,
                maxAimDistance,
                aimLayerMask,
                QueryTriggerInteraction.Ignore))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint =
                aimRay.GetPoint(maxAimDistance);
        }


        // =====================================================
        // FirePoint → 목표 지점
        // =====================================================

        Vector3 shootDirection =
            (
                targetPoint -
                firePoint.position
            ).normalized;


        Projectile projectile =
            Instantiate(
                projectilePrefab,
                firePoint.position,
                Quaternion.LookRotation(shootDirection)
            );


        float damage =
            playerStats != null
                ? playerStats.AttackPower
                : 1f;


        projectile.Launch(
            shootDirection,
            damage
        );


        // 마지막 총알을 사용했다면 자동 재장전
        if (currentAmmo <= 0 &&
            autoReloadWhenEmpty)
        {
            TryReload();
        }
    }


    // =========================================================
    // 재장전 시도
    // =========================================================

    private void TryReload()
    {
        // 이미 재장전 중
        if (isReloading)
            return;


        // 탄창이 이미 가득 참
        if (currentAmmo >= magazineSize)
            return;


        // 남은 예비 탄창 없음
        if (reserveMagazines <= 0)
            return;


        StartCoroutine(
            ReloadRoutine()
        );
    }


    // =========================================================
    // 재장전
    // =========================================================

    private IEnumerator ReloadRoutine()
    {
        isReloading = true;

        OnReloadStateChanged?.Invoke(true);


        float reloadTime = baseReloadTime;


        // PlayerStats의 ReloadSpeed 적용
        if (playerStats != null)
        {
            reloadTime =
                playerStats.GetReloadTime(
                    baseReloadTime
                );
        }


        Debug.Log(
            $"Reload Start : {reloadTime:F2}초"
        );


        yield return new WaitForSeconds(
            reloadTime
        );


        // 예비 탄창 하나 소비
        reserveMagazines--;


        // 현재 탄창을 완전히 채움
        currentAmmo = magazineSize;


        isReloading = false;


        NotifyAmmoChanged();

        OnReloadStateChanged?.Invoke(false);


        Debug.Log("Reload Complete");
    }


    // =========================================================
    // UI 갱신
    // =========================================================

    private void NotifyAmmoChanged()
    {
        OnAmmoChanged?.Invoke(
            currentAmmo,
            magazineSize,
            reserveMagazines
        );
    }


    // =========================================================
    // Inspector 값 보호
    // =========================================================

    private void OnValidate()
    {
        magazineSize =
            Mathf.Max(1, magazineSize);

        reserveMagazines =
            Mathf.Max(0, reserveMagazines);

        baseReloadTime =
            Mathf.Max(0.01f, baseReloadTime);
    }
}