using System.Collections.Generic;
using UnityEngine;

public class TitleCockroachRainbowAnimation : MonoBehaviour
{
    // =========================================================
    // Renderer
    // =========================================================

    [Header("Renderers")]

    [Tooltip("비워두면 이 오브젝트와 자식의 Renderer를 자동으로 찾습니다.")]
    [SerializeField]
    private List<Renderer> targetRenderers =
        new List<Renderer>();


    // =========================================================
    // Rotation
    // =========================================================

    [Header("Rotation")]

    [Tooltip("회전시킬 로컬 축")]
    [SerializeField]
    private Vector3 rotationAxis =
        Vector3.up;


    [Tooltip("초당 회전 각도")]
    [SerializeField]
    private float rotationSpeed =
        360f;


    // =========================================================
    // Rainbow
    // =========================================================

    [Header("Rainbow")]

    [Tooltip("색상이 한 바퀴 순환하는 속도")]
    [SerializeField]
    private float colorCycleSpeed =
        0.5f;


    [Tooltip("색상의 채도")]
    [SerializeField, Range(0f, 1f)]
    private float saturation =
        1f;


    [Tooltip("색상의 밝기")]
    [SerializeField, Range(0f, 1f)]
    private float brightness =
        1f;


    // =========================================================
    // Runtime
    // =========================================================

    private bool isPlaying;


    private Quaternion originalRotation;


    private float hue;


    private readonly List<MaterialPropertyBlock>
        propertyBlocks =
        new List<MaterialPropertyBlock>();


    // =========================================================
    // Awake
    // =========================================================

    private void Awake()
    {
        originalRotation =
            transform.localRotation;


        // ==========================================
        // Renderer 자동 탐색
        // ==========================================

        if (targetRenderers.Count == 0)
        {
            Renderer[] renderers =
                GetComponentsInChildren<Renderer>(
                    true
                );


            targetRenderers.AddRange(
                renderers
            );
        }


        propertyBlocks.Clear();


        for (int i = 0;
             i < targetRenderers.Count;
             i++)
        {
            propertyBlocks.Add(
                new MaterialPropertyBlock()
            );
        }


        isPlaying =
            false;
    }


    // =========================================================
    // Update
    // =========================================================

    private void Update()
    {
        if (!isPlaying)
            return;


        // =====================================================
        // 회전
        // =====================================================

        transform.Rotate(
            rotationAxis,
            rotationSpeed *
            Time.unscaledDeltaTime,
            Space.Self
        );


        // =====================================================
        // Rainbow Color
        // =====================================================

        hue +=
            colorCycleSpeed *
            Time.unscaledDeltaTime;


        hue =
            Mathf.Repeat(
                hue,
                1f
            );


        Color rainbowColor =
            Color.HSVToRGB(
                hue,
                saturation,
                brightness
            );


        ApplyColor(
            rainbowColor
        );
    }


    // =========================================================
    // Play
    // =========================================================

    public void Play()
    {
        isPlaying =
            true;
    }


    // =========================================================
    // Stop
    // =========================================================

    public void StopAndReset()
    {
        isPlaying =
            false;


        hue =
            0f;


        transform.localRotation =
            originalRotation;


        ClearColorOverride();
    }


    // =========================================================
    // Material Color
    // =========================================================

    private void ApplyColor(
        Color color
    )
    {
        for (int i = 0;
             i < targetRenderers.Count;
             i++)
        {
            Renderer targetRenderer =
                targetRenderers[i];


            if (targetRenderer == null)
                continue;


            MaterialPropertyBlock block =
                propertyBlocks[i];


            targetRenderer.GetPropertyBlock(
                block
            );


            // URP Lit
            block.SetColor(
                "_BaseColor",
                color
            );


            // Built-in Standard 대응
            block.SetColor(
                "_Color",
                color
            );


            targetRenderer.SetPropertyBlock(
                block
            );
        }
    }


    // =========================================================
    // 원래 Material 상태로 복구
    // =========================================================

    private void ClearColorOverride()
    {
        foreach (
            Renderer targetRenderer
            in targetRenderers)
        {
            if (targetRenderer == null)
                continue;


            targetRenderer.SetPropertyBlock(
                null
            );
        }
    }


    // =========================================================
    // Inspector
    // =========================================================

    private void OnValidate()
    {
        rotationSpeed =
            Mathf.Max(
                0f,
                rotationSpeed
            );


        colorCycleSpeed =
            Mathf.Max(
                0f,
                colorCycleSpeed
            );
    }
}