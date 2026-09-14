using System.Collections;
using UnityEngine;

public class EnemyShooter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyStats enemyStats;

    [SerializeField] private Transform firePoint;

    [SerializeField] private Projectile projectilePrefab;


    [Header("Ammo")]
    [SerializeField] private int magazineSize = 10;

    [SerializeField] private float baseReloadTime = 2f;


    private int currentAmmo;

    private float nextFireTime;

    private bool isReloading;


    public bool IsReloading =>
        isReloading;


    private void Awake()
    {
        if (enemyStats == null)
        {
            enemyStats =
                GetComponent<EnemyStats>();
        }
    }


    private void Start()
    {
        currentAmmo =
            magazineSize;
    }


    // =========================================================
    // AI가 호출
    // =========================================================

    public void TryShoot(
        Vector3 targetPosition
    )
    {
        if (enemyStats == null)
            return;


        if (enemyStats.IsDead)
            return;


        if (isReloading)
            return;


        // =====================================================
        // 탄약 없음
        // =====================================================

        if (currentAmmo <= 0)
        {
            StartCoroutine(
                ReloadRoutine()
            );

            return;
        }


        // =====================================================
        // 발사속도
        // =====================================================

        if (Time.time <
            nextFireTime)
        {
            return;
        }


        Shoot(
            targetPosition
        );


        nextFireTime =
            Time.time +
            (
                1f /
                Mathf.Max(
                    0.01f,
                    enemyStats.FireRate
                )
            );
    }


    // =========================================================
    // 실제 발사
    // =========================================================

    private void Shoot(
        Vector3 targetPosition
    )
    {
        if (firePoint == null ||
            projectilePrefab == null)
        {
            return;
        }


        Vector3 direction =
            (
                targetPosition -
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
            enemyStats.AttackPower,
            gameObject
        );


        currentAmmo--;


        // 마지막 총알
        if (currentAmmo <= 0)
        {
            StartCoroutine(
                ReloadRoutine()
            );
        }
    }


    // =========================================================
    // 재장전
    // =========================================================

    private IEnumerator ReloadRoutine()
    {
        if (isReloading)
            yield break;


        isReloading = true;


        float reloadTime =
            enemyStats.GetReloadTime(
                baseReloadTime
            );


        yield return new WaitForSeconds(
            reloadTime
        );


        currentAmmo =
            magazineSize;


        isReloading = false;
    }
}