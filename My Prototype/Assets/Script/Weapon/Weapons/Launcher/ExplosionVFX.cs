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

    private void Start()
    {
        // 처음 시작할 때 크기를 0으로 만듭니다.
        transform.localScale = Vector3.zero;

        // 구체의 렌더러에서 머티리얼을 가져옵니다. (투명해지게 만들기 위함)
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            myMaterial = rend.material;

            if (myMaterial.HasProperty("_Color"))
                initialColor = myMaterial.color;
            else if (myMaterial.HasProperty("_BaseColor")) // URP 대응
                initialColor = myMaterial.GetColor("_BaseColor");
        }
    }

    private void Update()
    {
        timer += Time.deltaTime;
        float progress = timer / duration;

        // 1. 크기 커지기
        float currentScale = Mathf.Lerp(0f, maxRadius * 2f, progress);
        transform.localScale = new Vector3(currentScale, currentScale, currentScale);

        if (myMaterial != null)
        {
            // 2. 투명도 조절 (기존 코드)
            Color newColor = initialColor;
            newColor.a = Mathf.Lerp(initialColor.a, 0f, progress);

            if (myMaterial.HasProperty("_Color"))
                myMaterial.color = newColor;
            else if (myMaterial.HasProperty("_BaseColor"))
                myMaterial.SetColor("_BaseColor", newColor);

            // 3. ✨ 빛(Emission) 서서히 꺼지게 만들기 추가 ✨
            if (myMaterial.HasProperty("_EmissionColor"))
            {
                // progress가 1에 가까워질수록 Color.black(빛 없음)으로 변함
                Color currentEmission = Color.Lerp(initialColor, Color.black, progress);
                myMaterial.SetColor("_EmissionColor", currentEmission);
            }
        }

        // 시간이 다 되면 파괴
        if (timer >= duration)
        {
            Destroy(gameObject);
        }
    }
}