using System.Collections.Generic;
using UnityEngine;

public class TitleCockroachRainbowAnimation : MonoBehaviour
{
    // =========================================================
    // Renderer
    // =========================================================

    [Header("Renderers")]

    [Tooltip("비워두면 자식 Renderer를 자동으로 찾습니다.")]
    [SerializeField]
    private List<Renderer> targetRenderers =
        new List<Renderer>();


    // =========================================================
    // Rotation
    // =========================================================

    [Header("Rotation")]

    [Tooltip("연출 시작 시 고정할 X축 각도")]
    [SerializeField]
    private float fixedTiltX =
        -65f;


    [Tooltip("Y축 초당 회전 속도")]
    [SerializeField]
    private float yRotationSpeed =
        360f;


    // =========================================================
    // Rainbow
    // =========================================================

    [Header("Rainbow")]

    [Tooltip("색상 변화 속도")]
    [SerializeField]
    private float colorCycleSpeed =
        0.5f;


    [Range(0f, 1f)]
    [SerializeField]
    private float saturation =
        1f;


    [Range(0f, 1f)]
    [SerializeField]
    private float brightness =
        1f;

    // =========================================================
    // Up / Down Movement
    // =========================================================

    [Header("Up Down Movement")]

    [Tooltip("위아래로 움직이는 최대 거리")]
    [SerializeField]
    private float moveHeight =
        0.4f;


    [Tooltip("위아래 왕복 속도")]
    [SerializeField]
    private float moveSpeed =
        2f;

    // =========================================================
    // Runtime
    // =========================================================

    private bool isPlaying;


    // 연출 시작 전 원래 회전
    private Quaternion originalRotation;


    // 원래 Euler 값
    private Vector3 originalEuler;


    // Y축 누적 회전값
    private float currentYRotation;


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


        originalEuler =
            transform.localEulerAngles;


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
    }


    // =========================================================
    // Update
    // =========================================================

    private void Update()
    {
        if (!isPlaying)
            return;


        // =====================================================
        // Y축 회전값만 증가
        // =====================================================

        currentYRotation +=
            yRotationSpeed *
            Time.unscaledDeltaTime;


        currentYRotation =
            Mathf.Repeat(
                currentYRotation,
                360f
            );


        // =====================================================
        // X축은 항상 -65도 고정
        //
        // Y축만 계속 회전
        // Z축은 원래 값 유지
        // =====================================================

        transform.localRotation =
            Quaternion.Euler(
                fixedTiltX,
                currentYRotation,
                originalEuler.z
            );


        // =====================================================
        // Rainbow
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
        if (isPlaying)
            return;


        isPlaying =
            true;


        // ==========================================
        // 현재 Y축을 시작점으로 사용
        // ==========================================

        currentYRotation =
            originalEuler.y;


        // ==========================================
        // 연출 시작하는 순간 X = -65도로 눕힘
        // ==========================================

        transform.localRotation =
            Quaternion.Euler(
                fixedTiltX,
                currentYRotation,
                originalEuler.z
            );
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


        currentYRotation =
            originalEuler.y;


        // 원래 자세 복구
        transform.localRotation =
            originalRotation;


        // 원래 Material 색 복구
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


            // URP
            block.SetColor(
                "_BaseColor",
                color
            );


            // Standard Shader
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
    // Material 복구
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
        yRotationSpeed =
            Mathf.Max(
                0f,
                yRotationSpeed
            );


        colorCycleSpeed =
            Mathf.Max(
                0f,
                colorCycleSpeed
            );
    }
}