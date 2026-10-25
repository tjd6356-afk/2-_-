using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    // =========================================================
    // UI
    // =========================================================

    [Header("UI")]

    [SerializeField]
    private GameObject dialoguePanel;


    [SerializeField]
    private TMP_Text npcNameText;


    [SerializeField]
    private TMP_Text dialogueText;


    [SerializeField]
    private Image characterImage;


    // =========================================================
    // Runtime
    // =========================================================

    private PlayerGameplayControl currentPlayerControl;


    private IReadOnlyList<string> currentDialogue;


    private int dialogueIndex;


    private bool isDialogueOpen;


    // =========================================================
    // Dialogue 종료 Callback
    //
    // 일반 NPC
    // → NPCInteraction.OnDialogueFinished
    //
    // 특수 NPC
    // → NewNPCInteraction.OnDialogueFinished
    // =========================================================

    private Action dialogueFinishedCallback;


    public bool IsDialogueOpen =>
        isDialogueOpen;


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
    }


    // =========================================================
    // Update
    // =========================================================

    private void Update()
    {
        if (!isDialogueOpen)
            return;


        if (Keyboard.current == null)
            return;


        // =====================================================
        // Space
        // =====================================================

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            NextDialogue();

            return;
        }


        // =====================================================
        // Enter
        // =====================================================

        if (Keyboard.current.enterKey.wasPressedThisFrame ||
            Keyboard.current.numpadEnterKey.wasPressedThisFrame)
        {
            NextDialogue();

            return;
        }
    }


    // =========================================================
    // 일반 NPC용 Dialogue 시작
    //
    // 기존 NPCInteraction은 이 함수를 그대로 사용
    // =========================================================

    public void StartDialogue(
        NPCInteraction npc,
        NPCData npcData,
        IReadOnlyList<string> dialogue,
        PlayerGameplayControl playerControl
    )
    {
        if (npcData == null)
        {
            Debug.LogError(
                "[DialogueManager] NPCData가 없습니다."
            );

            return;
        }


        StartDialogueInternal(
            npcData.NPCName,
            npcData.NPCPortrait,
            dialogue,
            playerControl,
            npc != null
                ? npc.OnDialogueFinished
                : null
        );
    }


    // =========================================================
    // 특수 NPC용 Dialogue 시작
    //
    // NewNPCInteraction이 이 함수를 사용
    // =========================================================

    public void StartDialogue(
        NewNPCInteraction npc,
        NewNPCData npcData,
        IReadOnlyList<string> dialogue,
        PlayerGameplayControl playerControl
    )
    {
        if (npcData == null)
        {
            Debug.LogError(
                "[DialogueManager] NewNPCData가 없습니다."
            );

            return;
        }


        StartDialogueInternal(
            npcData.NPCName,
            npcData.NPCPortrait,
            dialogue,
            playerControl,
            npc != null
                ? npc.OnDialogueFinished
                : null
        );
    }


    // =========================================================
    // 실제 공통 Dialogue 시작
    // =========================================================

    private void StartDialogueInternal(
        string npcName,
        Sprite npcPortrait,
        IReadOnlyList<string> dialogue,
        PlayerGameplayControl playerControl,
        Action onFinished
    )
    {
        // 이미 대화 중이면 새로운 Dialogue 시작 금지
        if (isDialogueOpen)
            return;


        if (dialogue == null ||
            dialogue.Count == 0)
        {
            Debug.LogWarning(
                $"{npcName}의 대사가 없습니다."
            );

            return;
        }


        // ==========================================
        // Runtime 정보 저장
        // ==========================================

        currentDialogue =
            dialogue;


        currentPlayerControl =
            playerControl;


        dialogueFinishedCallback =
            onFinished;


        dialogueIndex =
            0;


        isDialogueOpen =
            true;


        // ==========================================
        // NPC 이름
        // ==========================================

        if (npcNameText != null)
        {
            npcNameText.text =
                npcName;
        }


        // ==========================================
        // NPC 이미지
        // ==========================================

        if (characterImage != null)
        {
            characterImage.sprite =
                npcPortrait;


            characterImage.enabled =
                npcPortrait != null;
        }


        // ==========================================
        // Dialogue Panel
        // ==========================================

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(
                true
            );


            dialoguePanel.transform.SetAsLastSibling();
        }


        // ==========================================
        // Player 조작 잠금
        // ==========================================

        if (currentPlayerControl != null)
        {
            currentPlayerControl.LockControls();
        }
        else
        {
            Cursor.lockState =
                CursorLockMode.None;


            Cursor.visible =
                true;
        }


        ShowCurrentDialogue();


        Debug.Log(
            $"[DialogueManager] Dialogue Start : {npcName}"
        );
    }


    // =========================================================
    // 현재 Dialogue 출력
    // =========================================================

    private void ShowCurrentDialogue()
    {
        if (dialogueText == null)
            return;


        if (currentDialogue == null)
            return;


        if (dialogueIndex < 0 ||
            dialogueIndex >= currentDialogue.Count)
        {
            return;
        }


        dialogueText.text =
            currentDialogue[
                dialogueIndex
            ];
    }


    // =========================================================
    // 다음 Dialogue
    // =========================================================

    public void NextDialogue()
    {
        if (!isDialogueOpen)
            return;


        dialogueIndex++;


        // ==========================================
        // 마지막 대사 종료
        // ==========================================

        if (dialogueIndex >=
            currentDialogue.Count)
        {
            EndDialogue();

            return;
        }


        ShowCurrentDialogue();
    }


    // =========================================================
    // Dialogue Panel 클릭
    // =========================================================

    public void OnDialoguePanelClicked()
    {
        NextDialogue();
    }


    // =========================================================
    // Dialogue 종료
    // =========================================================

    private void EndDialogue()
    {
        if (!isDialogueOpen)
            return;


        isDialogueOpen =
            false;


        // ==========================================
        // Panel OFF
        // ==========================================

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(
                false
            );
        }


        // ==========================================
        // Player 조작 복구
        // ==========================================

        if (currentPlayerControl != null)
        {
            currentPlayerControl.UnlockControls();
        }
        else
        {
            Cursor.lockState =
                CursorLockMode.Locked;


            Cursor.visible =
                false;
        }


        // ==========================================
        // Callback을 임시 변수에 보관
        // ==========================================

        Action callback =
            dialogueFinishedCallback;


        // ==========================================
        // Dialogue 정보 초기화
        // ==========================================

        currentDialogue =
            null;


        currentPlayerControl =
            null;


        dialogueFinishedCallback =
            null;


        dialogueIndex =
            0;


        Debug.Log(
            "[DialogueManager] Dialogue End"
        );


        // ==========================================
        // 가장 마지막에 Dialogue 종료 알림
        //
        // 일반 NPC라면
        // NPCInteraction.OnDialogueFinished()
        //
        // 특수 NPC라면
        // NewNPCInteraction.OnDialogueFinished()
        // ==========================================

        callback?.Invoke();
    }
}