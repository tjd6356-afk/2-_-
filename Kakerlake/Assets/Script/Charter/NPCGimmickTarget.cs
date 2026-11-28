using System;
using System.Collections.Generic;
using UnityEngine;

public class NPCGimmickTarget : MonoBehaviour, IHealth
{
    // =========================================================
    // References
    // =========================================================

    [Header("Owner NPC")]
    [Tooltip("비워두면 부모 계층에서 NPCInteraction을 자동으로 찾습니다.")]
    [SerializeField]
    private NPCInteraction ownerNPC;


    // =========================================================
    // Gimmick Toggles (기능 ON/OFF)
    // =========================================================

    [Header("Gimmick Settings (기능 ON/OFF)")]
    [Tooltip("이 아이템을 수집(훔치기)했을 때 NPC 훔치기 반응 다이얼로그를 출력할지 여부")]
    [SerializeField]
    private bool enableStealReaction = true;

    [Tooltip("이 아이템/오브젝트를 무기로 타격했을 때 NPC 훼손 반응 다이얼로그를 출력할지 여부")]
    [SerializeField]
    private bool enableDamageReaction = true;

    [Tooltip("체크 시 훼손 다이얼로그는 최초 1회 타격 때만 출력 (해제 시 때릴 때마다 출력)")]
    [SerializeField]
    private bool triggerDamageDialogueOnce = true;

    [Tooltip("ON: 훼손된 이후에도 이 아이템을 수집하면 훔치기 다이얼로그가 출력됨 / OFF: 훼손 후 수집 시 훔치기 다이얼로그 미출력")]
    [SerializeField]
    private bool allowStealDialogueAfterDamage = true;

    [Tooltip("ON: 훼손 후 훔칠 때 전용 대사(Steal After Damage Dialogue) 출력 / OFF: 훼손 후 훔칠 때도 일반 훔치기 대사(Steal Dialogue) 출력")]
    [SerializeField]
    private bool useDifferentStealDialogueAfterDamage = true;


    // =========================================================
    // Dialogues (아이템/오브젝트별 개별 대사)
    // =========================================================

    [Header("Steal Dialogue (일반 훔치기 반응 대사)")]
    [TextArea(2, 5)]
    [SerializeField]
    private List<string> stealDialogue =
        new List<string>();

    [Header("Damage Dialogue (무기 타격 / 훼손 반응 대사)")]
    [TextArea(2, 5)]
    [SerializeField]
    private List<string> damageDialogue =
        new List<string>();

    [Header("Steal After Damage Dialogue (훼손 후 훔치기 전용 대사)")]
    [TextArea(2, 5)]
    [SerializeField]
    private List<string> stealAfterDamageDialogue =
        new List<string>();


    // =========================================================
    // Weapon Hitbox Helper
    // =========================================================

    [Header("Weapon Hitbox")]
    [Tooltip("ItemPickup처럼 Trigger 콜라이더만 있는 경우, 총알/레이캐스트/근접무기 판정을 위해 메쉬 크기의 일반 콜라이더를 자동 추가합니다.")]
    [SerializeField]
    private bool autoCreateHitboxIfOnlyTrigger = true;


    // =========================================================
    // Runtime State
    // =========================================================

    private ItemPickup itemPickup;

    private bool isDamaged;

    private bool hasTriggeredDamageDialogue;


    public bool IsDamaged => isDamaged;


    // =========================================================
    // IHealth 구현 (모든 근접/원거리 무기 타격 감지용)
    // =========================================================

    public float MaxHealth => 999999f;

    public float CurrentHealth => 999999f;

    public bool IsDead => false;

    public event Action<float, float> OnHealthChanged;

    public event Action OnDied;


    private void Awake()
    {
        if (ownerNPC == null)
        {
            ownerNPC =
                GetComponentInParent<NPCInteraction>();
        }


        itemPickup =
            GetComponent<ItemPickup>();


        if (enableDamageReaction && autoCreateHitboxIfOnlyTrigger)
        {
            EnsureWeaponHitboxCollider();
        }
    }


    private void OnEnable()
    {
        if (itemPickup != null)
        {
            itemPickup.OnItemCollected += HandleItemCollected;
        }
    }


    private void OnDisable()
    {
        if (itemPickup != null)
        {
            itemPickup.OnItemCollected -= HandleItemCollected;
        }
    }


    // =========================================================
    // 무기 타격(훼손) 처리
    // =========================================================

    public void TakeDamage(float damage)
    {
        if (!enableDamageReaction)
            return;


        isDamaged = true;


        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);


        if (triggerDamageDialogueOnce && hasTriggeredDamageDialogue)
            return;


        hasTriggeredDamageDialogue = true;


        if (ownerNPC != null &&
            damageDialogue != null &&
            damageDialogue.Count > 0)
        {
            ownerNPC.TriggerReactionDialogue(damageDialogue);
        }
    }


    public void Heal(float amount)
    {
        // 훼손 판정 전용 오브젝트이므로 체력 회복 미사용
    }


    // =========================================================
    // 아이템 수집(훔치기) 처리
    // =========================================================

    private void HandleItemCollected()
    {
        if (!enableStealReaction || ownerNPC == null)
            return;


        // 훼손된 상태일 때 처리
        if (isDamaged)
        {
            // 훼손 후 훔치기 다이얼로그 OFF 상태면 출력하지 않음
            if (!allowStealDialogueAfterDamage)
                return;


            // 훼손 후 훔칠 때 다른 대사 사용 ON 상태면 전용 대사 출력
            if (useDifferentStealDialogueAfterDamage)
            {
                List<string> linesToUse =
                    (stealAfterDamageDialogue != null && stealAfterDamageDialogue.Count > 0)
                    ? stealAfterDamageDialogue
                    : stealDialogue;

                ownerNPC.TriggerReactionDialogue(linesToUse);
                return;
            }
        }


        // 일반 훔치기 대사 출력 (또는 훼손 후 동일 대사 사용 시)
        ownerNPC.TriggerReactionDialogue(stealDialogue);
    }


    // =========================================================
    // Trigger 전용 아이템에도 총알/근접무기가 맞도록 히트박스 보완
    // =========================================================

    private void EnsureWeaponHitboxCollider()
    {
        Collider[] colliders =
            GetComponentsInChildren<Collider>(true);


        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null && !colliders[i].isTrigger)
            {
                return;
            }
        }


        BoxCollider hitbox =
            gameObject.AddComponent<BoxCollider>();

        hitbox.isTrigger = false;


        Renderer meshRenderer =
            GetComponentInChildren<Renderer>();

        if (meshRenderer != null)
        {
            Bounds worldBounds = meshRenderer.bounds;
            hitbox.center = transform.InverseTransformPoint(worldBounds.center);

            Vector3 lossy = transform.lossyScale;
            hitbox.size = new Vector3(
                lossy.x != 0f ? worldBounds.size.x / Mathf.Abs(lossy.x) : 0.5f,
                lossy.y != 0f ? worldBounds.size.y / Mathf.Abs(lossy.y) : 0.5f,
                lossy.z != 0f ? worldBounds.size.z / Mathf.Abs(lossy.z) : 0.5f
            );
        }
        else
        {
            hitbox.size = Vector3.one * 0.5f;
        }
    }
}