using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class MinigunTrailVFX : MonoBehaviour
{
    [Tooltip("궤적이 화면에 남아있는 시간 (초)")]
    [SerializeField] private float duration = 0.05f;

    private LineRenderer lineRenderer;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    // 총구부터 목표물까지 선을 그리고 지정된 시간 후 삭제
    public void DrawTrail(Vector3 startPoint, Vector3 endPoint)
    {
        // 2개의 점으로 직선을 만듦
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, startPoint);
        lineRenderer.SetPosition(1, endPoint);

        Destroy(gameObject, duration);
    }
}