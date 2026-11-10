using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MerchantDialogueUI : MonoBehaviour
{
    // =========================================================
    // UI
    // =========================================================

    [Header("Dialogue UI")]

    [SerializeField]
    private GameObject dialoguePanel;


    [SerializeField]
    private TMP_Text merchantNameText;


    [SerializeField]
    private TMP_Text dialogueText;


    [SerializeField]
    private Image merchantPortrait;


    // =========================================================
    // Choices
    // =========================================================

    [Header("Choices")]

    [SerializeField]
    private GameObject choiceRoot;


    [SerializeField]
    private Button buyButton;


    [SerializeField]
    private Button aboutButton;


    [SerializeField]
    private Button quitButton;

    // =========================================================
    // Typing
    // =========================================================

    [Header("Typing")]

    [Tooltip("글자가 하나씩 나타나는 속도")]
    [SerializeField]
    private float typingSpeed = 0.03f;


    private bool isTyping;

    private Coroutine typingCoroutine;

    // =========================================================
    // Shop
    // =========================================================

    [Header("Shop")]

    [SerializeField]
    private ShopUIController shopUI;


    // =========================================================
    // Runtime
    // =========================================================

    private MerchantNPCData currentData;

    private PlayerGameplayControl playerControl;


    private IReadOnlyList<string> currentLines;

    private int lineIndex;


    private DialogueMode currentMode;


    private bool isOpen;


    public bool IsOpen =>
        isOpen;


    private enum DialogueMode
    {
        None,

        Start,

        About,

        Ending
    }


    // =========================================================
    // Awake
    // =========================================================

    private void Awake()
    {
        if (buyButton != null)
        {
            buyButton.onClick.AddListener(
                SelectBuy
            );
        }


        if (aboutButton != null)
        {
            aboutButton.onClick.AddListener(
                SelectAbout
            );
        }


        if (quitButton != null)
        {
            quitButton.onClick.AddListener(
                SelectQuit
            );
        }
    }


    // =========================================================
    // Start
    // =========================================================

    private void Start()
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(
                false
            );
        }


        if (choiceRoot != null)
        {
            choiceRoot.SetActive(
                false
            );
        }
    }


    // =========================================================
    // Update
    // =========================================================

    private void Update()
    {
        if (!isOpen)
            return;


        // =========================================================
        // 선택지가 표시 중이면
        // Space / Enter / Click으로 대사를 넘기지 않음
        // =========================================================

        if (choiceRoot != null &&
            choiceRoot.activeSelf)
        {
            return;
        }


        // =========================================================
        // Shop이 열려 있으면
        // 대화 진행 입력 중지
        // =========================================================

        if (shopUI != null &&
            shopUI.IsOpen)
        {
            return;
        }


        bool nextPressed =
            false;


        // =========================================================
        // Keyboard
        // =========================================================

        if (Keyboard.current != null)
        {
            if (Keyboard.current.spaceKey.wasPressedThisFrame ||
                Keyboard.current.enterKey.wasPressedThisFrame ||
                Keyboard.current.numpadEnterKey.wasPressedThisFrame)
            {
                nextPressed =
                    true;
            }
        }


        // =========================================================
        // Mouse Left Click
        // =========================================================

        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            nextPressed =
                true;
        }


        if (nextPressed)
        {
            NextDialogue();
        }
    }


    // =========================================================
    // Conversation Start
    // =========================================================

    public void BeginConversation(
        MerchantNPCData data,
        PlayerGameplayControl control
    )
    {
        if (isOpen)
            return;

        isTyping =
        false;


        if (typingCoroutine != null)
        {
            StopCoroutine(
                typingCoroutine
            );


            typingCoroutine =
                null;
        }

        if (data == null)
            return;


        currentData =
            data;


        playerControl =
            control;


        isOpen =
            true;


        if (merchantNameText != null)
        {
            merchantNameText.text =
                currentData.MerchantName;
        }


        if (merchantPortrait != null)
        {
            merchantPortrait.sprite =
                currentData.MerchantPortrait;


            merchantPortrait.enabled =
                currentData.MerchantPortrait != null;
        }


        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(
                true
            );


            dialoguePanel.transform.SetAsLastSibling();
        }


        if (choiceRoot != null)
        {
            choiceRoot.SetActive(
                false
            );
        }


        // Player 조작 중지
        if (playerControl != null)
        {
            playerControl.LockControls();
        }
        else
        {
            Cursor.lockState =
                CursorLockMode.None;

            Cursor.visible =
                true;
        }


        StartDialogueSequence(
            currentData.StartDialogue,
            DialogueMode.Start
        );
    }


    // =========================================================
    // Sequence
    // =========================================================

    private void StartDialogueSequence(
        IReadOnlyList<string> lines,
        DialogueMode mode
    )
    {
        currentMode =
            mode;


        currentLines =
            lines;


        lineIndex =
            0;


        if (currentLines == null ||
            currentLines.Count == 0)
        {
            FinishCurrentSequence();

            return;
        }


        ShowCurrentLine();
    }


    private void ShowCurrentLine()
    {
        if (dialogueText == null)
            return;


        if (currentLines == null)
            return;


        if (lineIndex < 0 ||
            lineIndex >= currentLines.Count)
        {
            return;
        }


        // 기존 타이핑 중지
        if (typingCoroutine != null)
        {
            StopCoroutine(
                typingCoroutine
            );


            typingCoroutine =
                null;
        }


        typingCoroutine =
            StartCoroutine(
                TypeDialogue(
                    currentLines[lineIndex]
                )
            );
    }

    // =========================================================
    // 한 글자씩 대사 표시
    // =========================================================

    private IEnumerator TypeDialogue(
        string line
    )
    {
        isTyping =
            true;


        dialogueText.text =
            "";


        foreach (
            char letter
            in line.ToCharArray())
        {
            dialogueText.text +=
                letter;


            yield return
                new WaitForSeconds(
                    typingSpeed
                );
        }


        isTyping =
            false;


        typingCoroutine =
            null;
    }

    public void NextDialogue()
    {
        if (!isOpen)
            return;


        if (currentLines == null)
            return;


        // =========================================================
        // 1. 현재 글자가 출력 중
        //
        // → 다음 대사로 넘어가지 않고
        //   현재 문장을 즉시 완성
        // =========================================================

        if (isTyping)
        {
            if (typingCoroutine != null)
            {
                StopCoroutine(
                    typingCoroutine
                );


                typingCoroutine =
                    null;
            }


            if (lineIndex >= 0 &&
                lineIndex < currentLines.Count)
            {
                dialogueText.text =
                    currentLines[lineIndex];
            }


            isTyping =
                false;


            return;
        }


        // =========================================================
        // 2. 현재 문장이 이미 전부 나옴
        //
        // → 다음 문장
        // =========================================================

        lineIndex++;


        if (lineIndex >=
            currentLines.Count)
        {
            FinishCurrentSequence();

            return;
        }


        ShowCurrentLine();
    }


    // =========================================================
    // Sequence Finished
    // =========================================================

    private void FinishCurrentSequence()
    {
        switch (currentMode)
        {
            // 처음 대화 끝
            case DialogueMode.Start:

                ShowChoices();

                break;


            // 상인 소개가 끝나면 다시 선택지
            case DialogueMode.About:

                ShowChoices();

                break;


            // 거래 종료 대사가 끝나면 완전히 종료
            case DialogueMode.Ending:

                EndConversation();

                break;
        }
    }


    // =========================================================
    // Choices
    // =========================================================

    private void ShowChoices()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(
                typingCoroutine
            );


            typingCoroutine =
                null;
        }


        isTyping =
            false;

        currentLines =
            null;


        currentMode =
            DialogueMode.None;


        if (dialogueText != null)
        {
            dialogueText.text =
                "";
        }


        if (choiceRoot != null)
        {
            choiceRoot.SetActive(
                true
            );
        }
    }


    // =========================================================
    // 산다
    // =========================================================

    private void SelectBuy()
    {
        if (currentData == null)
            return;


        if (choiceRoot != null)
        {
            choiceRoot.SetActive(
                false
            );
        }


        // 대화창을 잠시 숨김
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(
                false
            );
        }


        if (shopUI != null)
        {
            shopUI.Open(
                currentData,
                OnShopClosed
            );
        }
    }


    // =========================================================
    // 너에 대해서
    // =========================================================

    private void SelectAbout()
    {
        if (choiceRoot != null)
        {
            choiceRoot.SetActive(
                false
            );
        }


        StartDialogueSequence(
            currentData.AboutDialogue,
            DialogueMode.About
        );
    }


    // =========================================================
    // 그만 두기
    // =========================================================

    private void SelectQuit()
    {
        if (choiceRoot != null)
        {
            choiceRoot.SetActive(
                false
            );
        }


        StartDialogueSequence(
            currentData.NoPurchaseDialogue,
            DialogueMode.Ending
        );
    }


    // =========================================================
    // Shop Close
    // =========================================================

    private void OnShopClosed(
        bool purchasedAnyItem
    )
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(
                true
            );
        }


        if (purchasedAnyItem)
        {
            StartDialogueSequence(
                currentData.PurchaseDialogue,
                DialogueMode.Ending
            );
        }
        else
        {
            StartDialogueSequence(
                currentData.NoPurchaseDialogue,
                DialogueMode.Ending
            );
        }
    }


    // =========================================================
    // End
    // =========================================================

    private void EndConversation()
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(
                false
            );
        }


        if (choiceRoot != null)
        {
            choiceRoot.SetActive(
                false
            );
        }


        isOpen =
            false;

        isTyping =
        false;


        if (typingCoroutine != null)
        {
            StopCoroutine(
                typingCoroutine
            );


            typingCoroutine =
                null;
        }

        currentData =
            null;


        currentLines =
            null;


        currentMode =
            DialogueMode.None;


        lineIndex =
            0;


        if (playerControl != null)
        {
            playerControl.UnlockControls();
        }
        else
        {
            Cursor.lockState =
                CursorLockMode.Locked;

            Cursor.visible =
                false;
        }


        playerControl =
            null;
    }
}