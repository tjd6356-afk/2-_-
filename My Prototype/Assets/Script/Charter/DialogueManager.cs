using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField]
    private GameObject dialoguePanel;

    [SerializeField]
    private TMP_Text npcNameText;

    [SerializeField]
    private TMP_Text dialogueText;

    [SerializeField]
    private Image characterImage;


    private NPCInteraction currentNPC;

    private PlayerGameplayControl currentPlayerControl;


    private IReadOnlyList<string> currentDialogue;

    private int dialogueIndex;

    private bool isDialogueOpen;


    public bool IsDialogueOpen =>
        isDialogueOpen;


    private void Start()
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }
    }


    private void Update()
    {
        if (!isDialogueOpen)
            return;


        if (Keyboard.current == null)
            return;


        // ==========================================
        // Space
        // ==========================================

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            NextDialogue();

            return;
        }


        // ==========================================
        // Enter
        // ==========================================

        if (Keyboard.current.enterKey.wasPressedThisFrame ||
            Keyboard.current.numpadEnterKey.wasPressedThisFrame)
        {
            NextDialogue();

            return;
        }
    }


    // =========================================================
    // 대화 시작
    // =========================================================

    public void StartDialogue(
        NPCInteraction npc,
        NPCData npcData,
        IReadOnlyList<string> dialogue,
        PlayerGameplayControl playerControl
    )
    {
        if (isDialogueOpen)
            return;


        if (dialogue == null ||
            dialogue.Count == 0)
        {
            Debug.LogWarning(
                $"{npcData.NPCName}의 대사가 없습니다."
            );

            return;
        }


        currentNPC =
            npc;


        currentDialogue =
            dialogue;


        currentPlayerControl =
            playerControl;


        dialogueIndex =
            0;


        isDialogueOpen =
            true;


        // ==========================================
        // NPC 정보 출력
        // ==========================================

        if (npcNameText != null)
        {
            npcNameText.text =
                npcData.NPCName;
        }


        if (characterImage != null)
        {
            characterImage.sprite =
                npcData.NPCPortrait;


            characterImage.enabled =
                npcData.NPCPortrait != null;
        }


        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
        }


        // ==========================================
        // Player 조작 중지
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
    }


    // =========================================================
    // 현재 대사 출력
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
            currentDialogue[dialogueIndex];
    }


    // =========================================================
    // 다음 대사
    // =========================================================

    public void NextDialogue()
    {
        if (!isDialogueOpen)
            return;


        dialogueIndex++;


        // ==========================================
        // 마지막 대사까지 끝남
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
    // Panel 클릭용
    // =========================================================

    public void OnDialoguePanelClicked()
    {
        NextDialogue();
    }


    // =========================================================
    // 대화 종료
    // =========================================================

    private void EndDialogue()
    {
        isDialogueOpen =
            false;


        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }


        // NPC에게 대화 완료 알림
        if (currentNPC != null)
        {
            currentNPC.OnDialogueFinished();
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


        currentNPC =
            null;

        currentDialogue =
            null;

        currentPlayerControl =
            null;

        dialogueIndex =
            0;
    }
}