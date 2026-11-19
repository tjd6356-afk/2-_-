using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class InteractionPromptTrigger : MonoBehaviour
{
    // =========================================================
    // Prompt Type
    // =========================================================

    public enum PromptType
    {
        NPC,
        Farm,
        Door,
        DungeonMove,
        ScenePortal,
        Custom,
        Upgrad,
        Item
    }


    // =========================================================
    // Prompt
    // =========================================================

    [Header("Interaction Prompt")]

    [SerializeField]
    private PromptType promptType =
        PromptType.Custom;


    [Tooltip(
        "Prompt Type이 Custom일 때 사용할 문구"
    )]
    [SerializeField]
    private string customText =
        "상호작용";


    // =========================================================
    // World UI
    // =========================================================

    [Header("World UI")]

    [Tooltip(
        "World Space로 만든 E키 UI Prefab"
    )]
    [SerializeField]
    private WorldInteractionPromptUI promptPrefab;


    [Tooltip(
        "상호작용 UI가 생성될 위치"
    )]
    [SerializeField]
    private Transform promptSpawnPoint;


    // =========================================================
    // Runtime
    // =========================================================

    private WorldInteractionPromptUI
        currentPrompt;


    private readonly HashSet<Collider>
        playerColliders =
            new HashSet<Collider>();


    // =========================================================
    // Awake
    // =========================================================

    private void Awake()
    {
        Collider trigger =
            GetComponent<Collider>();


        if (trigger != null &&
            !trigger.isTrigger)
        {
            Debug.LogWarning(
                $"[InteractionPrompt] " +
                $"{name} Collider의 Is Trigger를 켜주세요."
            );
        }


        if (promptSpawnPoint == null)
        {
            Debug.LogWarning(
                $"[InteractionPrompt] " +
                $"{name} : Prompt Spawn Point가 없습니다."
            );
        }
    }


    // =========================================================
    // Update
    // =========================================================

    private void Update()
    {
        if (currentPrompt == null)
            return;


        // =====================================================
        // Dialogue / Inventory 등의 UI가 열렸다면
        // World Prompt 잠시 숨김
        // =====================================================

        bool shouldShow =
            playerColliders.Count > 0 &&
            !PlayerGameplayControl.GameplayLocked;


        if (currentPrompt.gameObject.activeSelf !=
            shouldShow)
        {
            currentPrompt.gameObject.SetActive(
                shouldShow
            );
        }
    }


    // =========================================================
    // Enter
    // =========================================================

    private void OnTriggerEnter(
        Collider other
    )
    {
        PlayerStats player =
            other.GetComponentInParent<PlayerStats>();


        if (player == null)
            return;


        playerColliders.Add(
            other
        );


        // 이미 생성되어 있음
        if (currentPrompt != null)
            return;


        CreatePrompt();
    }


    // =========================================================
    // Exit
    // =========================================================

    private void OnTriggerExit(
        Collider other
    )
    {
        PlayerStats player =
            other.GetComponentInParent<PlayerStats>();


        if (player == null)
            return;


        playerColliders.Remove(
            other
        );


        // ==========================================
        // Player에 Collider가 여러 개 있으면
        // 아직 하나가 Trigger 안에 있을 수 있음
        // ==========================================

        if (playerColliders.Count >
            0)
        {
            return;
        }


        DestroyPrompt();
    }


    // =========================================================
    // Create
    // =========================================================

    private void CreatePrompt()
    {
        if (promptPrefab == null)
        {
            Debug.LogWarning(
                $"[InteractionPrompt] " +
                $"{name} : Prompt Prefab이 없습니다."
            );

            return;
        }


        if (promptSpawnPoint == null)
        {
            Debug.LogWarning(
                $"[InteractionPrompt] " +
                $"{name} : Spawn Point가 없습니다."
            );

            return;
        }


        currentPrompt =
            Instantiate(
                promptPrefab,
                promptSpawnPoint.position,
                promptSpawnPoint.rotation,
                promptSpawnPoint
            );


        // ==========================================
        // Spawn Point 기준 위치 초기화
        // ==========================================

        currentPrompt.transform.localPosition =
            Vector3.zero;


        currentPrompt.transform.localRotation =
            Quaternion.identity;


        // ==========================================
        // Text 설정
        // ==========================================

        currentPrompt.Setup(
            GetPromptText()
        );


        Debug.Log(
            $"[InteractionPrompt] 생성 : " +
            $"{GetPromptText()}"
        );
    }


    // =========================================================
    // Destroy
    // =========================================================

    private void DestroyPrompt()
    {
        if (currentPrompt == null)
            return;


        Destroy(
            currentPrompt.gameObject
        );


        currentPrompt =
            null;
    }


    // =========================================================
    // Text
    // =========================================================

    private string GetPromptText()
    {
        switch (promptType)
        {
            case PromptType.NPC:

                return "대화하기";


            case PromptType.Farm:

                return "손에 든 아이템 심기";


            case PromptType.Door:

                return "문열기";


            case PromptType.DungeonMove:

                return "스테이지 이동하기";


            case PromptType.ScenePortal:

                return "이동하기";

            case PromptType.Upgrad:

                return "강화하기";

            case PromptType.Item:

                return "아이템 줍기";

            case PromptType.Custom:

                return customText;
        }


        return "상호작용";
    }


    // =========================================================
    // Disable / Destroy
    // =========================================================

    private void OnDisable()
    {
        playerColliders.Clear();

        DestroyPrompt();
    }
}