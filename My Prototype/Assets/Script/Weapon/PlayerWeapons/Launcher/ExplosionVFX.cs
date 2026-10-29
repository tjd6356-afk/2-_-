using UnityEngine;

public class ExplosionVFX : MonoBehaviour
{
    [Header("Explosion Settings")]
    [Tooltip("최종적으로 커질 구체의 크기 (반지름)")]
    [SerializeField] private float maxRadius = 4f;

    [Tooltip("이펙트가 지속되는 시간 (초)")]
    [SerializeField] private float duration = 0.3f;

    private float timer = 0f;
    private Material myMaterial;
    private Color initialColor;

    // ✨ 핵심: 무기 쪽에서 생성 직후 이 함수를 불러 폭발 크기를 맞춰줍니다. ✨
    public void SetRadius(float radius)
    {
        maxRadius = radius;
    }

    private void Start()
    {
        transform.localScale = Vector3.zero;

        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            myMaterial = rend.material;

            if (myMaterial.HasProperty("_Color"))
                initialColor = myMaterial.color;
            else if (myMaterial.HasProperty("_BaseColor"))
                initialColor = myMaterial.GetColor("_BaseColor");
        }
    }

    private void Update()
    {
        timer += Time.deltaTime;
        float progress = timer / duration;

        // 1. 크기 커지기 (변경된 maxRadius가 적용됨)
        float currentScale = Mathf.Lerp(0f, maxRadius * 2f, progress);
        transform.localScale = new Vector3(currentScale, currentScale, currentScale);

        if (myMaterial != null)
        {
            // 2. 투명도 조절
            Color newColor = initialColor;
            newColor.a = Mathf.Lerp(initialColor.a, 0f, progress);

            if (myMaterial.HasProperty("_Color"))
                myMaterial.color = newColor;
            else if (myMaterial.HasProperty("_BaseColor"))
                myMaterial.SetColor("_BaseColor", newColor);

            // 3. 빛 서서히 끄기
            if (myMaterial.HasProperty("_EmissionColor"))
            {
                Color currentEmission = Color.Lerp(initialColor, Color.black, progress);
                myMaterial.SetColor("_EmissionColor", currentEmission);
            }
        }

        if (timer >= duration)
        {
            Destroy(gameObject);
        }
    }
}