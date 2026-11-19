using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class InteractionPromptTrigger : MonoBehaviour
{
    // =========================================================
    // Type
    // =========================================================

    public enum PromptType
    {
        NPC,
        Farm,
        Door,
        DungeonMove,
        ScenePortal,
        Custom
    }


    // =========================================================
    // Settings
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


    [Tooltip(
        "여러 상호작용 Trigger가 겹쳤을 때 높은 숫자가 우선"
    )]
    [SerializeField]
    private int priority =
        0;


    // =========================================================
    // Runtime
    // =========================================================

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
                $"{name}의 Collider가 Trigger가 아닙니다."
            );
        }
    }


    // =========================================================
    // Player Enter
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


        if (InteractionPromptUI.Instance ==
            null)
        {
            return;
        }


        InteractionPromptUI.Instance
            .ShowPrompt(
                this,
                GetPromptText(),
                priority
            );
    }


    // =========================================================
    // Player Exit
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


        // Player의 다른 Collider가
        // 아직 Trigger 안에 있을 수 있음
        if (playerColliders.Count >
            0)
        {
            return;
        }


        HidePrompt();
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


            case PromptType.Custom:

                return customText;
        }


        return "상호작용";
    }


    // =========================================================
    // Runtime Text 갱신용
    // =========================================================

    public void RefreshPrompt()
    {
        if (playerColliders.Count ==
            0)
        {
            return;
        }


        if (InteractionPromptUI.Instance ==
            null)
        {
            return;
        }


        InteractionPromptUI.Instance
            .ShowPrompt(
                this,
                GetPromptText(),
                priority
            );
    }


    // =========================================================
    // Hide
    // =========================================================

    private void HidePrompt()
    {
        if (InteractionPromptUI.Instance !=
            null)
        {
            InteractionPromptUI.Instance
                .HidePrompt(
                    this
                );
        }
    }


    // =========================================================
    // Disable
    // =========================================================

    private void OnDisable()
    {
        playerColliders.Clear();

        HidePrompt();
    }
}