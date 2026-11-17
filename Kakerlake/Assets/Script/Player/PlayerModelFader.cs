using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class PlayerModelFader : MonoBehaviour
{
    // =========================================================
    // References
    // =========================================================

    [Header("References")]

    [Tooltip("투명하게 만들 Player의 실제 Model")]
    [SerializeField]
    private Transform visualRoot;


    // =========================================================
    // Fade
    // =========================================================

    [Header("Fade Settings")]

    [Tooltip("Camera가 이 거리 안으로 들어오면 투명화 시작")]
    [SerializeField]
    private float fadeStartDistance = 1.7f;


    [Tooltip("이 거리에서는 거의 1인칭 상태")]
    [SerializeField]
    private float firstPersonDistance = 0.35f;


    [Range(0f, 1f)]
    [Tooltip("가장 가까울 때 Player 최소 불투명도")]
    [SerializeField]
    private float minimumAlpha = 0.05f;


    [Tooltip("투명도 전환 속도")]
    [SerializeField]
    private float fadeSpeed = 8f;


    [Tooltip("URP Material을 런타임에 Transparent로 변경")]
    [SerializeField]
    private bool forceTransparentMaterials = true;


    // =========================================================
    // Runtime
    // =========================================================

    private float targetAlpha = 1f;

    private float currentAlpha = 1f;


    private readonly List<MaterialData>
        materials =
            new List<MaterialData>();


    private class MaterialData
    {
        public Material material;

        public string colorProperty;

        public Color originalColor;
    }


    // =========================================================
    // Awake
    // =========================================================

    private void Awake()
    {
        if (visualRoot == null)
        {
            visualRoot =
                transform;
        }


        CacheMaterials();
    }


    // =========================================================
    // Material 저장
    // =========================================================

    private void CacheMaterials()
    {
        materials.Clear();


        Renderer[] renderers =
            visualRoot.GetComponentsInChildren<Renderer>(
                true
            );


        foreach (Renderer renderer in renderers)
        {
            // .materials 사용:
            // 해당 Player만 사용하는 Runtime Material 복사본 생성
            Material[] rendererMaterials =
                renderer.materials;


            foreach (Material material in rendererMaterials)
            {
                if (material == null)
                    continue;


                string colorProperty = null;


                if (material.HasProperty(
                        "_BaseColor"))
                {
                    colorProperty =
                        "_BaseColor";
                }
                else if (
                    material.HasProperty(
                        "_Color"))
                {
                    colorProperty =
                        "_Color";
                }


                if (string.IsNullOrEmpty(
                        colorProperty))
                {
                    continue;
                }


                MaterialData data =
                    new MaterialData();


                data.material =
                    material;


                data.colorProperty =
                    colorProperty;


                data.originalColor =
                    material.GetColor(
                        colorProperty
                    );


                if (forceTransparentMaterials)
                {
                    MakeMaterialTransparent(
                        material
                    );
                }


                materials.Add(
                    data
                );
            }
        }
    }


    // =========================================================
    // URP Transparent
    // =========================================================

    private void MakeMaterialTransparent(
        Material material
    )
    {
        if (material == null)
            return;


        // URP Lit / Simple Lit 계열
        if (material.HasProperty(
                "_Surface"))
        {
            material.SetFloat(
                "_Surface",
                1f
            );
        }


        if (material.HasProperty(
                "_SrcBlend"))
        {
            material.SetFloat(
                "_SrcBlend",
                (float)BlendMode.SrcAlpha
            );
        }


        if (material.HasProperty(
                "_DstBlend"))
        {
            material.SetFloat(
                "_DstBlend",
                (float)BlendMode.OneMinusSrcAlpha
            );
        }


        if (material.HasProperty(
                "_ZWrite"))
        {
            material.SetFloat(
                "_ZWrite",
                0f
            );
        }


        material.EnableKeyword(
            "_SURFACE_TYPE_TRANSPARENT"
        );


        material.renderQueue =
            (int)RenderQueue.Transparent;
    }


    // =========================================================
    // Camera에서 호출
    // =========================================================

    public void SetCameraDistance(
        float cameraDistance
    )
    {
        float normalized =
            Mathf.InverseLerp(
                firstPersonDistance,
                fadeStartDistance,
                cameraDistance
            );


        targetAlpha =
            Mathf.Lerp(
                minimumAlpha,
                1f,
                normalized
            );
    }


    // =========================================================
    // Fade
    // =========================================================

    private void LateUpdate()
    {
        currentAlpha =
            Mathf.MoveTowards(
                currentAlpha,
                targetAlpha,
                fadeSpeed *
                Time.deltaTime
            );


        ApplyAlpha(
            currentAlpha
        );
    }


    private void ApplyAlpha(
        float alpha
    )
    {
        foreach (
            MaterialData data in materials)
        {
            if (data.material == null)
                continue;


            Color color =
                data.originalColor;


            color.a =
                data.originalColor.a *
                alpha;


            data.material.SetColor(
                data.colorProperty,
                color
            );
        }
    }


    // =========================================================
    // Disable
    // =========================================================

    private void OnDisable()
    {
        ApplyAlpha(
            1f
        );
    }
}