using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShooter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;

    [SerializeField] private Transform firePoint;

    [SerializeField] private Projectile projectilePrefab;

    [SerializeField] private ThirdPersonCamera thirdPersonCamera;


    [Header("Aim")]
    [SerializeField] private float maxAimDistance = 100f;

    [SerializeField] private LayerMask aimLayerMask = ~0;


    private void Awake()
    {
        // 직접 연결하지 않았으면 Main Camera 자동 검색
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
    }


    private void Update()
    {
        if (Mouse.current == null)
            return;


        // 우클릭 조준 상태가 아니면 발사 불가능
        if (thirdPersonCamera != null &&
            !thirdPersonCamera.IsAiming)
        {
            return;
        }


        // 좌클릭 한 번
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Shoot();
        }
    }


    private void Shoot()
    {
        if (playerCamera == null ||
            firePoint == null ||
            projectilePrefab == null)
        {
            return;
        }


        // =====================================================
        // 1. 화면 정중앙 십자선 방향으로 Ray 생성
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
        // 2. 십자선이 바라보는 지점 찾기
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
            // 아무것도 안 맞았다면
            // 카메라 앞쪽 100m 지점을 목표로 설정
            targetPoint =
                aimRay.GetPoint(maxAimDistance);
        }


        // =====================================================
        // 3. 총구 → 십자선 목표 지점 방향 계산
        // =====================================================

        Vector3 shootDirection =
            (targetPoint - firePoint.position).normalized;


        // =====================================================
        // 4. 총알 생성
        // =====================================================

        Projectile projectile =
            Instantiate(
                projectilePrefab,
                firePoint.position,
                Quaternion.LookRotation(shootDirection)
            );


        // =====================================================
        // 5. 총알 발사
        // =====================================================

        projectile.Launch(shootDirection);
    }
}