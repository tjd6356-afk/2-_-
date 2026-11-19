using TMPro;
using UnityEngine;

public class WorldInteractionPromptUI : MonoBehaviour
{
    // =========================================================
    // UI
    // =========================================================

    [Header("UI")]

    [SerializeField]
    private TMP_Text actionText;


    // =========================================================
    // Billboard
    // =========================================================

    [Header("Billboard")]

    [SerializeField]
    private bool faceCamera = true;


    [Tooltip(
        "UI가 카메라 반대 방향을 보고 있다면 체크"
    )]
    [SerializeField]
    private bool invertFacing = false;


    // =========================================================
    // Runtime
    // =========================================================

    private Camera targetCamera;


    // =========================================================
    // Awake
    // =========================================================

    private void Awake()
    {
        FindCamera();
    }


    // =========================================================
    // Setup
    // =========================================================

    public void Setup(
        string message
    )
    {
        if (actionText != null)
        {
            actionText.text =
                message;
        }
    }


    // =========================================================
    // Late Update
    // =========================================================

    private void LateUpdate()
    {
        if (!faceCamera)
            return;


        if (targetCamera == null)
        {
            FindCamera();


            if (targetCamera == null)
                return;
        }


        // ==========================================
        // 항상 Camera를 바라보도록
        // ==========================================

        transform.rotation =
            targetCamera.transform.rotation;


        if (invertFacing)
        {
            transform.Rotate(
                0f,
                180f,
                0f
            );
        }
    }


    // =========================================================
    // Camera Search
    // =========================================================

    private void FindCamera()
    {
        targetCamera =
            Camera.main;
    }
}