using UnityEngine;
using UnityEngine.InputSystem;

public class NPCInteraction : MonoBehaviour
{
    [Header("NPC")]
    [SerializeField]
    private NPCData npcData;


    [Header("Dialogue")]
    [SerializeField]
    private DialogueManager dialogueManager;


    private bool playerInside;

    private bool firstDialogueCompleted;

    private PlayerGameplayControl playerControl;


    private void Awake()
    {
        if (dialogueManager == null)
        {
            dialogueManager =
                FindFirstObjectByType<DialogueManager>();
        }
    }


    private void Update()
    {
        if (!playerInside)
            return;


        if (Keyboard.current == null)
            return;


        // 대화 중이면 E로 새 대화를 또 시작하지 않는다.
        if (dialogueManager != null &&
            dialogueManager.IsDialogueOpen)
        {
            return;
        }


        // ==========================================
        // E키로 대화 시작
        // ==========================================

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            StartConversation();
        }
    }


    private void StartConversation()
    {
        if (npcData == null)
        {
            Debug.LogError(
                $"{gameObject.name}에 NPC Data가 없습니다."
            );

            return;
        }


        if (dialogueManager == null)
        {
            Debug.LogError(
                "DialogueManager가 없습니다."
            );

            return;
        }


        // ==========================================
        // 처음 대화 / 반복 대화 결정
        // ==========================================

        if (!firstDialogueCompleted)
        {
            dialogueManager.StartDialogue(
                this,
                npcData,
                npcData.FirstDialogue,
                playerControl
            );
        }
        else
        {
            // 반복 대사가 없다면 첫 대사를 대신 사용
            if (npcData.RepeatDialogue.Count > 0)
            {
                dialogueManager.StartDialogue(
                    this,
                    npcData,
                    npcData.RepeatDialogue,
                    playerControl
                );
            }
            else
            {
                dialogueManager.StartDialogue(
                    this,
                    npcData,
                    npcData.FirstDialogue,
                    playerControl
                );
            }
        }
    }


    // =========================================================
    // DialogueManager가 대화 종료 시 호출
    // =========================================================

    public void OnDialogueFinished()
    {
        if (!firstDialogueCompleted)
        {
            firstDialogueCompleted = true;
        }
    }


    // =========================================================
    // Player 진입
    // =========================================================

    private void OnTriggerEnter(Collider other)
    {
        PlayerStats playerStats =
            other.GetComponentInParent<PlayerStats>();


        if (playerStats == null)
            return;


        playerInside = true;


        playerControl =
            playerStats.GetComponent<PlayerGameplayControl>();


        Debug.Log(
            $"{npcData?.NPCName ?? gameObject.name} 대화 범위 진입"
        );
    }


    // =========================================================
    // Player 퇴장
    // =========================================================

    private void OnTriggerExit(Collider other)
    {
        PlayerStats playerStats =
            other.GetComponentInParent<PlayerStats>();


        if (playerStats == null)
            return;


        playerInside = false;

        playerControl = null;
    }
}