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

    private PlayerGameplayControl playerControl;


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
    }


    private void Update()
    {
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


        // ==========================================
        // 마우스가 게임에 잠겨있는 상태
        //
        // TPS에서는 마우스가 중앙에 잠겨 있으므로
        // 화면 중앙 십자선 방향으로 검사
        // ==========================================

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

        // ==========================================
        // 마우스가 자유로운 상태
        //
        // 실제 마우스 위치로 검사
        // ==========================================

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


        // ==========================================
        // Raycast
        // ==========================================

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


        // ==========================================
        // 클릭한 Collider가 이 NPC인가?
        //
        // NPC 루트 또는 NPC의 자식 Collider만 인정
        // ==========================================

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
    // 대화 시작
    // =========================================================

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
        // 처음 대화
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

        // ==========================================
        // 재대화
        // ==========================================

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
                // 반복 대사가 없으면
                // 첫 대사를 다시 사용
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
    // 첫 대화 완료
    // =========================================================

    public void OnDialogueFinished()
    {
        if (!firstDialogueCompleted)
        {
            firstDialogueCompleted =
                true;
        }
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