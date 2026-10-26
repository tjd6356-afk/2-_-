using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class ItemEffectMessageUI : MonoBehaviour
{
    // =========================================================
    // UI
    // =========================================================

    [Header("UI")]

    [SerializeField]
    private GameObject effectPanel;


    [SerializeField]
    private TMP_Text effectText;


    // =========================================================
    // Runtime
    // =========================================================

    private Action onClosed;

    private bool isOpen;

    private int openedFrame;


    public bool IsOpen =>
        isOpen;


    // =========================================================
    // Start
    // =========================================================

    private void Start()
    {
        if (effectPanel != null)
        {
            effectPanel.SetActive(
                false
            );
        }


        isOpen =
            false;
    }


    // =========================================================
    // Update
    // =========================================================

    private void Update()
    {
        if (!isOpen)
            return;


        // 패널을 연 바로 그 클릭으로
        // 즉시 닫히는 것을 방지
        if (Time.frameCount ==
            openedFrame)
        {
            return;
        }


        bool closeInput =
            false;


        // ==========================================
        // Keyboard
        // ==========================================

        if (Keyboard.current != null)
        {
            if (Keyboard.current.spaceKey.wasPressedThisFrame ||
                Keyboard.current.enterKey.wasPressedThisFrame ||
                Keyboard.current.numpadEnterKey.wasPressedThisFrame)
            {
                closeInput =
                    true;
            }
        }


        // ==========================================
        // Mouse
        // ==========================================

        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            closeInput =
                true;
        }


        if (closeInput)
        {
            Close();
        }
    }


    // =========================================================
    // ItemData 표시
    // =========================================================

    public void Show(
        ItemData itemData,
        Action closeCallback = null
    )
    {
        if (itemData == null)
            return;


        Show(
            itemData.UseEffectText,
            closeCallback
        );
    }


    // =========================================================
    // Text 직접 표시
    // =========================================================

    public void Show(
        string message,
        Action closeCallback = null
    )
    {
        if (effectPanel == null)
        {
            Debug.LogError(
                "[ItemEffectUI] Effect Panel이 없습니다."
            );

            return;
        }


        onClosed =
            closeCallback;


        if (effectText != null)
        {
            effectText.text =
                message;
        }


        effectPanel.SetActive(
            true
        );


        // Canvas에서 가장 앞으로
        effectPanel.transform.SetAsLastSibling();


        isOpen =
            true;


        openedFrame =
            Time.frameCount;


        Debug.Log(
            $"[ItemEffectUI] Open : {message}"
        );
    }


    // =========================================================
    // Close
    // =========================================================

    public void Close()
    {
        if (!isOpen)
            return;


        isOpen =
            false;


        if (effectPanel != null)
        {
            effectPanel.SetActive(
                false
            );
        }


        // Callback 중복 호출 방지
        Action callback =
            onClosed;


        onClosed =
            null;


        callback?.Invoke();


        Debug.Log(
            "[ItemEffectUI] Close"
        );
    }
}