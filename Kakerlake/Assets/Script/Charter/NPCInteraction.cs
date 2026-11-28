using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


// 무기 공격보다 NPC 클릭 판정을 먼저 처리
[DefaultExecutionOrder(-100)]
public class NPCInteraction : MonoBehaviour
{
    [Header("NPC")]
    [SerializeField]
    private NPCData npcData;


    [Header("Dialogue")]
    [SerializeField]
    private DialogueManager dialogueManager;


    [Header("Mouse Interaction")]
    [Tooltip("NPC를 클릭할 때 사용할 카메라")]
    [SerializeField]
    private Camera interactionCamera;

    [Tooltip("NPC 클릭 Raycast 최대 거리")]
    [SerializeField]
    private float clickRayDistance = 100f;

    [Tooltip("NPC 클릭 판정에 사용할 Layer")]
    [SerializeField]
    private LayerMask clickLayerMask = ~0;


    private bool playerInside;

    private bool firstDialogueCompleted;

    private bool isPlayingReactionDialogue;

    private PlayerGameplayControl playerControl;

    private IHealth npcHealth;


    private void Awake()
    {
        if (dialogueManager == null)
        {
            dialogueManager =
                FindFirstObjectByType<DialogueManager>();
        }


        if (interactionCamera == null)
        {
            interactionCamera =
                Camera.main;
        }


        npcHealth =
            GetComponent<IHealth>();
    }


    private void Update()
    {
        if (IsNPCDead())
            return;


        // ==========================================
        // Player가 NPC 범위 안에 있어야 함
        // ==========================================

        if (!playerInside)
            return;


        if (dialogueManager == null)
            return;


        // 이미 대화 중이라면
        // 새로운 대화를 시작하지 않는다.
        if (dialogueManager.IsDialogueOpen)
            return;


        // ==========================================
        // E키
        // ==========================================

        if (Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            StartConversation();

            return;
        }


        // ==========================================
        // NPC 좌클릭
        // ==========================================

        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryStartDialogueByMouse();
        }
    }


    // =========================================================
    // 마우스로 NPC 클릭 확인
    // =========================================================

    private void TryStartDialogueByMouse()
    {
        if (interactionCamera == null)
            return;


        Ray ray;


        if (Cursor.lockState ==
            CursorLockMode.Locked)
        {
            ray =
                interactionCamera
                    .ViewportPointToRay(
                        new Vector3(
                            0.5f,
                            0.5f,
                            0f
                        )
                    );
        }
        else
        {
            Vector2 mousePosition =
                Mouse.current
                    .position
                    .ReadValue();


            ray =
                interactionCamera
                    .ScreenPointToRay(
                        mousePosition
                    );
        }


        if (!Physics.Raycast(
                ray,
                out RaycastHit hit,
                clickRayDistance,
                clickLayerMask,
                QueryTriggerInteraction.Ignore))
        {
            return;
        }


        Transform hitTransform =
            hit.collider.transform;


        bool clickedThisNPC =
            hitTransform == transform ||
            hitTransform.IsChildOf(transform);


        if (!clickedThisNPC)
            return;


        Debug.Log(
            $"{npcData?.NPCName ?? gameObject.name} 좌클릭 대화 시작"
        );


        StartConversation();
    }


    // =========================================================
    // 일반 대화 시작
    // =========================================================

    private void StartConversation()
    {
        if (IsNPCDead())
            return;


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


        isPlayingReactionDialogue = false;


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
    // 아이템 훔치기 / 오브젝트 훼손 반응 다이얼로그 출력
    // =========================================================

    public void TriggerReactionDialogue(
        IReadOnlyList<string> reactionDialogue
    )
    {
        if (IsNPCDead())
            return;


        if (npcData == null || dialogueManager == null)
            return;


        if (reactionDialogue == null || reactionDialogue.Count == 0)
            return;


        if (dialogueManager.IsDialogueOpen)
            return;


        PlayerGameplayControl targetControl =
            playerControl != null
            ? playerControl
            : FindFirstObjectByType<PlayerGameplayControl>();


        isPlayingReactionDialogue = true;


        dialogueManager.StartDialogue(
            this,
            npcData,
            reactionDialogue,
            targetControl
        );
    }


    // =========================================================
    // 대화 완료
    // =========================================================

    public void OnDialogueFinished()
    {
        // 기믹 반응 다이얼로그 종료 시에는 일반 첫 대화 완료 상태를 건드리지 않음
        if (isPlayingReactionDialogue)
        {
            isPlayingReactionDialogue = false;
            return;
        }


        if (!firstDialogueCompleted)
        {
            firstDialogueCompleted =
                true;
        }
    }


    private bool IsNPCDead()
    {
        return npcHealth != null && npcHealth.IsDead;
    }


    // =========================================================
    // Player가 NPC 범위 안으로 들어옴
    // =========================================================

    private void OnTriggerEnter(
        Collider other
    )
    {
        PlayerStats playerStats =
            other.GetComponentInParent<PlayerStats>();


        if (playerStats == null)
            return;


        playerInside =
            true;


        playerControl =
            playerStats
                .GetComponent<PlayerGameplayControl>();


        Debug.Log(
            $"{npcData?.NPCName ?? gameObject.name} 대화 범위 진입"
        );
    }


    // =========================================================
    // Player가 NPC 범위 밖으로 나감
    // =========================================================

    private void OnTriggerExit(
        Collider other
    )
    {
        PlayerStats playerStats =
            other.GetComponentInParent<PlayerStats>();


        if (playerStats == null)
            return;


        playerInside =
            false;


        playerControl =
            null;


        Debug.Log(
            $"{npcData?.NPCName ?? gameObject.name} 대화 범위 이탈"
        );
    }
}