using System.Collections;
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


    [Header("Settings")]
    [Tooltip("글자가 하나씩 나타나는 속도")]
    [SerializeField]
    private float typingSpeed = 0.03f;


    private NPCInteraction currentNPC;
    private PlayerGameplayControl currentPlayerControl;

    private IReadOnlyList<string> currentDialogue;
    private int dialogueIndex;
    private bool isDialogueOpen;

    // 타이핑 효과를 위한 변수
    private bool isTyping;
    private Coroutine typingCoroutine;

    public bool IsDialogueOpen => isDialogueOpen;


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

        // Space 또는 Enter 입력 시
        if (Keyboard.current.spaceKey.wasPressedThisFrame ||
            Keyboard.current.enterKey.wasPressedThisFrame ||
            Keyboard.current.numpadEnterKey.wasPressedThisFrame)
        {
            NextDialogue();
        }
    }


    // =========================================================
    // 대화 시작
    // =========================================================
    public void StartDialogue(
        NPCInteraction npc,
        NPCData npcData,
        IReadOnlyList<string> dialogue,
        PlayerGameplayControl playerControl)
    {
        if (isDialogueOpen)
            return;

        if (dialogue == null || dialogue.Count == 0)
        {
            Debug.LogWarning($"{npcData.NPCName}의 대사가 없습니다.");
            return;
        }

        currentNPC = npc;
        currentDialogue = dialogue;
        currentPlayerControl = playerControl;
        dialogueIndex = 0;
        isDialogueOpen = true;
        isTyping = false;

        // NPC 정보 출력
        if (npcNameText != null)
            npcNameText.text = npcData.NPCName;

        if (characterImage != null)
        {
            characterImage.sprite = npcData.NPCPortrait;
            characterImage.enabled = npcData.NPCPortrait != null;
        }

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        // Player 조작 중지
        if (currentPlayerControl != null)
        {
            currentPlayerControl.LockControls();
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        ShowCurrentDialogue();
    }


    // =========================================================
    // 현재 대사 출력 (코루틴 시작)
    // =========================================================
    private void ShowCurrentDialogue()
    {
        if (dialogueText == null || currentDialogue == null) return;
        if (dialogueIndex < 0 || dialogueIndex >= currentDialogue.Count) return;

        // 기존에 실행 중인 타이핑 코루틴이 있다면 정지
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        typingCoroutine = StartCoroutine(TypeDialogue(currentDialogue[dialogueIndex]));
    }


    // =========================================================
    // 한 글자씩 출력하는 코루틴
    // =========================================================
    private IEnumerator TypeDialogue(string line)
    {
        isTyping = true;
        dialogueText.text = "";

        // 문자열을 한 글자씩 쪼개서 출력
        foreach (char letter in line.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
    }


    // =========================================================
    // 다음 대사 (또는 타이핑 스킵)
    // =========================================================
    public void NextDialogue()
    {
        if (!isDialogueOpen)
            return;

        // 1. 아직 타이핑 중이라면 코루틴을 멈추고 전체 문장을 한 번에 띄움
        if (isTyping)
        {
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
            }
            dialogueText.text = currentDialogue[dialogueIndex];
            isTyping = false;
            return;
        }

        // 2. 타이핑이 끝난 상태라면 다음 줄로 넘어감
        dialogueIndex++;

        if (dialogueIndex >= currentDialogue.Count)
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
        isDialogueOpen = false;
        isTyping = false;

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        // NPC에게 대화 완료 알림
        if (currentNPC != null)
        {
            currentNPC.OnDialogueFinished();
        }

        // Player 조작 복구
        if (currentPlayerControl != null)
        {
            currentPlayerControl.UnlockControls();
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        currentNPC = null;
        currentDialogue = null;
        currentPlayerControl = null;
        dialogueIndex = 0;
    }
}