using UnityEngine;
using UnityEngine.InputSystem;


// Weapon 공격보다 NPC 클릭 먼저 처리
[DefaultExecutionOrder(-100)]
public class NewNPCInteraction : MonoBehaviour
{
    // =========================================================
    // NPC
    // =========================================================

    [Header("Special NPC")]

    [SerializeField]
    private NewNPCData npcData;


    // =========================================================
    // Dialogue
    // =========================================================

    [Header("Dialogue")]

    [SerializeField]
    private DialogueManager dialogueManager;


    // =========================================================
    // Special UI
    // =========================================================

    [Header("Special UI")]

    [SerializeField]
    private SpecialNPCPanelRouter panelRouter;


    // =========================================================
    // Mouse
    // =========================================================

    [Header("Mouse Interaction")]

    [SerializeField]
    private Camera interactionCamera;


    [SerializeField]
    private float clickRayDistance =
        100f;


    [SerializeField]
    private LayerMask clickLayerMask =
        ~0;


    // =========================================================
    // Runtime
    // =========================================================

    private bool playerInside;


    private bool firstDialogueCompleted;


    private PlayerGameplayControl playerControl;


    // =========================================================
    // Awake
    // =========================================================

    private void Awake()
    {
        if (dialogueManager == null)
        {
            dialogueManager =
                FindFirstObjectByType<DialogueManager>();
        }


        if (panelRouter == null)
        {
            panelRouter =
                FindFirstObjectByType<SpecialNPCPanelRouter>();
        }


        if (interactionCamera == null)
        {
            interactionCamera =
                Camera.main;
        }
    }


    // =========================================================
    // Update
    // =========================================================

    private void Update()
    {
        if (!playerInside)
            return;


        if (dialogueManager == null)
            return;


        if (dialogueManager.IsDialogueOpen)
            return;


        // =====================================================
        // E
        // =====================================================

        if (Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            StartConversation();

            return;
        }


        // =====================================================
        // Mouse Click
        // =====================================================

        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryStartDialogueByMouse();
        }
    }


    // =========================================================
    // Mouse Interaction
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
                interactionCamera.ViewportPointToRay(
                    new Vector3(
                        0.5f,
                        0.5f,
                        0f
                    )
                );
        }
        else
        {
            if (Mouse.current == null)
                return;


            Vector2 mousePosition =
                Mouse.current.position.ReadValue();


            ray =
                interactionCamera.ScreenPointToRay(
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


        StartConversation();
    }


    // =========================================================
    // Dialogue Start
    // =========================================================

    private void StartConversation()
    {
        if (npcData == null)
        {
            Debug.LogError(
                $"{gameObject.name}에 NewNPCData가 없습니다."
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


        // =====================================================
        // First Dialogue
        // =====================================================

        if (!firstDialogueCompleted)
        {
            dialogueManager.StartDialogue(
                this,
                npcData,
                npcData.FirstDialogue,
                playerControl
            );
        }

        // =====================================================
        // Repeat Dialogue
        // =====================================================

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
    // Dialogue Finished
    // =========================================================

    public void OnDialogueFinished()
    {
        if (!firstDialogueCompleted)
        {
            firstDialogueCompleted =
                true;
        }


        if (npcData == null)
            return;


        // =====================================================
        // 대화 종료 후 해당 NPC의 UI 실행
        // =====================================================

        if (panelRouter != null)
        {
            panelRouter.OpenPanel(
                npcData.PanelType
            );
        }
        else
        {
            Debug.LogWarning(
                "[NewNPCInteraction] SpecialNPCPanelRouter가 없습니다."
            );
        }
    }


    // =========================================================
    // Trigger Enter
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
            playerStats.GetComponent<PlayerGameplayControl>();


        Debug.Log(
            $"{npcData?.NPCName ?? gameObject.name} 범위 진입"
        );
    }


    // =========================================================
    // Trigger Exit
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
    }
}