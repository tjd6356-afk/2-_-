using UnityEngine;

public class MerchantShopViewPoint : MonoBehaviour
{
    [Header("Shop Camera View")]
    [SerializeField] private Transform cameraPoint;
    [SerializeField] private Transform lookTarget;

    public Transform CameraPoint => cameraPoint;
    public Transform LookTarget => lookTarget != null ? lookTarget : transform;

    private void OnDrawGizmosSelected()
    {
        if (cameraPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(cameraPoint.position, 0.08f);
        }

        if (lookTarget != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(lookTarget.position, 0.08f);

            if (cameraPoint != null)
            {
                Gizmos.DrawLine(cameraPoint.position, lookTarget.position);
            }
        }
    }
}