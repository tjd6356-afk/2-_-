using System.Collections;
using UnityEngine;

public class ShopCameraDirector : MonoBehaviour
{
    public static ShopCameraDirector Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Camera mainCamera;

    [Header("Move Settings")]
    [SerializeField] private float moveDuration = 0.45f;
    [SerializeField]
    private AnimationCurve moveCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private ThirdPersonCamera thirdPersonCamera;

    private Vector3 savedPosition;
    private Quaternion savedRotation;

    private bool isShopViewActive;
    private Coroutine moveCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera != null)
        {
            thirdPersonCamera = mainCamera.GetComponent<ThirdPersonCamera>();
        }
    }

    public void EnterShopView(MerchantShopViewPoint viewPoint)
    {
        if (mainCamera == null || viewPoint == null || viewPoint.CameraPoint == null)
            return;

        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
        }

        savedPosition = mainCamera.transform.position;
        savedRotation = mainCamera.transform.rotation;

        if (thirdPersonCamera != null)
        {
            thirdPersonCamera.enabled = false;
        }

        Quaternion targetRotation = GetTargetRotation(viewPoint);

        moveCoroutine = StartCoroutine(
            MoveCameraRoutine(
                viewPoint.CameraPoint.position,
                targetRotation,
                true
            )
        );
    }

    public void ExitShopView()
    {
        if (mainCamera == null)
            return;

        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
        }

        moveCoroutine = StartCoroutine(
            MoveCameraRoutine(
                savedPosition,
                savedRotation,
                false
            )
        );
    }

    private Quaternion GetTargetRotation(MerchantShopViewPoint viewPoint)
    {
        if (viewPoint.LookTarget != null && viewPoint.CameraPoint != null)
        {
            Vector3 direction =
                viewPoint.LookTarget.position -
                viewPoint.CameraPoint.position;

            if (direction.sqrMagnitude > 0.001f)
            {
                return Quaternion.LookRotation(direction.normalized, Vector3.up);
            }
        }

        return viewPoint.CameraPoint.rotation;
    }

    private IEnumerator MoveCameraRoutine(
        Vector3 targetPosition,
        Quaternion targetRotation,
        bool enteringShop
    )
    {
        isShopViewActive = enteringShop;

        Transform cam = mainCamera.transform;

        Vector3 startPosition = cam.position;
        Quaternion startRotation = cam.rotation;

        float time = 0f;

        while (time < moveDuration)
        {
            time += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(time / moveDuration);
            float curved = moveCurve.Evaluate(t);

            cam.position =
                Vector3.Lerp(
                    startPosition,
                    targetPosition,
                    curved
                );

            cam.rotation =
                Quaternion.Slerp(
                    startRotation,
                    targetRotation,
                    curved
                );

            yield return null;
        }

        cam.position = targetPosition;
        cam.rotation = targetRotation;

        if (!enteringShop && thirdPersonCamera != null)
        {
            thirdPersonCamera.enabled = true;
        }

        moveCoroutine = null;
    }
}