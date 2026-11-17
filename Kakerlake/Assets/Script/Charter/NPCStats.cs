using System;
using System.Collections.Generic;
using UnityEngine;

public class NPCStats : MonoBehaviour, IHealth
{
    // =========================================================
    // Data
    // =========================================================

    [Header("NPC Data")]

    [Tooltip("이 NPC가 사용할 NPCData")]
    [SerializeField]
    private NPCData npcData;


    [Header("Health Setting")]

    [Tooltip("NPCData에서 체력으로 사용할 Stat 이름")]
    [SerializeField]
    private string healthStatName = "Health";


    [Header("Death")]

    [Tooltip("죽었을 때 NPC 오브젝트를 삭제할지")]
    [SerializeField]
    private bool destroyOnDeath = false;


    [SerializeField]
    private float destroyDelay = 0f;


    // =========================================================
    // Runtime Stats
    // =========================================================

    private readonly Dictionary<string, float> runtimeStats =
        new Dictionary<string, float>(
            StringComparer.OrdinalIgnoreCase
        );


    private float maxHealth;

    private float currentHealth;

    private bool isDead;

    private bool initialized;


    // =========================================================
    // External
    // =========================================================

    public NPCData Data =>
        npcData;


    public float MaxHealth =>
        maxHealth;


    public float CurrentHealth =>
        currentHealth;


    public bool IsDead =>
        isDead;


    // =========================================================
    // Events
    // =========================================================

    public event Action<float, float> OnHealthChanged;

    public event Action OnDied;


    public event Action<string, float> OnStatChanged;


    // =========================================================
    // Awake
    // =========================================================

    private void Awake()
    {
        if (npcData != null)
        {
            Initialize(
                npcData
            );
        }
        else
        {
            Debug.LogError(
                $"[NPCStats] {gameObject.name}에 NPCData가 없습니다."
            );
        }
    }


    // =========================================================
    // Initialize
    // =========================================================

    public void Initialize(
        NPCData data
    )
    {
        if (data == null)
        {
            Debug.LogError(
                $"[NPCStats] 전달받은 NPCData가 Null입니다 : {gameObject.name}"
            );

            return;
        }


        npcData =
            data;


        runtimeStats.Clear();


        // ==========================================
        // NPCData의 모든 Stat 복사
        // ==========================================

        foreach (NPCStat stat in npcData.Stats)
        {
            if (stat == null)
                continue;


            string statName =
                stat.StatName;


            if (string.IsNullOrWhiteSpace(
                    statName))
            {
                continue;
            }


            // 같은 이름이 있으면 마지막 값을 사용
            if (runtimeStats.ContainsKey(
                    statName))
            {
                Debug.LogWarning(
                    $"[NPCStats] 중복 Stat 발견 : {statName} / " +
                    $"{gameObject.name}"
                );
            }


            runtimeStats[statName] =
                stat.Value;
        }


        // ==========================================
        // Health 가져오기
        // ==========================================

        if (!runtimeStats.TryGetValue(
                healthStatName,
                out maxHealth))
        {
            Debug.LogError(
                $"[NPCStats] {npcData.NPCName}의 NPCData에 " +
                $"'{healthStatName}' Stat이 없습니다."
            );


            initialized =
                false;

            return;
        }


        maxHealth =
            Mathf.Max(
                1f,
                maxHealth
            );


        currentHealth =
            maxHealth;


        runtimeStats[healthStatName] =
            currentHealth;


        isDead =
            false;


        initialized =
            true;


        OnHealthChanged?.Invoke(
            currentHealth,
            maxHealth
        );


        Debug.Log(
            $"[NPCStats] {npcData.NPCName} 초기화 완료 / " +
            $"HP = {currentHealth}/{maxHealth}"
        );
    }


    // =========================================================
    // Get Stat
    // =========================================================

    public float GetStat(
        string statName
    )
    {
        if (string.IsNullOrWhiteSpace(
                statName))
        {
            return 0f;
        }


        if (runtimeStats.TryGetValue(
                statName,
                out float value))
        {
            return value;
        }


        Debug.LogWarning(
            $"[NPCStats] 존재하지 않는 Stat : " +
            $"{statName} / {gameObject.name}"
        );


        return 0f;
    }


    public bool TryGetStat(
        string statName,
        out float value
    )
    {
        return runtimeStats.TryGetValue(
            statName,
            out value
        );
    }


    // =========================================================
    // Set Stat
    // =========================================================

    public void SetStat(
        string statName,
        float value
    )
    {
        if (string.IsNullOrWhiteSpace(
                statName))
        {
            return;
        }


        // Health는 전용 함수 사용
        if (string.Equals(
                statName,
                healthStatName,
                StringComparison.OrdinalIgnoreCase))
        {
            currentHealth =
                Mathf.Clamp(
                    value,
                    0f,
                    maxHealth
                );


            runtimeStats[healthStatName] =
                currentHealth;


            OnHealthChanged?.Invoke(
                currentHealth,
                maxHealth
            );


            if (currentHealth <= 0f)
            {
                Die();
            }


            return;
        }


        runtimeStats[statName] =
            value;


        OnStatChanged?.Invoke(
            statName,
            value
        );
    }


    // =========================================================
    // Add / Subtract Stat
    // =========================================================

    public void ModifyStat(
        string statName,
        float amount
    )
    {
        float current =
            GetStat(
                statName
            );


        SetStat(
            statName,
            current + amount
        );
    }


    // =========================================================
    // Damage
    // =========================================================

    public void TakeDamage(
        float damage
    )
    {
        if (!initialized)
            return;


        if (isDead)
            return;


        if (damage <= 0f)
            return;


        currentHealth -=
            damage;


        currentHealth =
            Mathf.Clamp(
                currentHealth,
                0f,
                maxHealth
            );


        runtimeStats[healthStatName] =
            currentHealth;


        OnHealthChanged?.Invoke(
            currentHealth,
            maxHealth
        );


        Debug.Log(
            $"[NPCStats] {npcData.NPCName} Damage : {damage} / " +
            $"HP {currentHealth}/{maxHealth}"
        );


        if (currentHealth <= 0f)
        {
            Die();
        }
    }


    // =========================================================
    // Heal
    // =========================================================

    public void Heal(
        float amount
    )
    {
        if (!initialized)
            return;


        if (isDead)
            return;


        if (amount <= 0f)
            return;


        currentHealth +=
            amount;


        currentHealth =
            Mathf.Clamp(
                currentHealth,
                0f,
                maxHealth
            );


        runtimeStats[healthStatName] =
            currentHealth;


        OnHealthChanged?.Invoke(
            currentHealth,
            maxHealth
        );
    }


    // =========================================================
    // Restore
    // =========================================================

    public void RestoreFullHealth()
    {
        if (!initialized)
            return;


        if (isDead)
            return;


        currentHealth =
            maxHealth;


        runtimeStats[healthStatName] =
            currentHealth;


        OnHealthChanged?.Invoke(
            currentHealth,
            maxHealth
        );
    }


    // =========================================================
    // Death
    // =========================================================

    private void Die()
    {
        if (isDead)
            return;


        isDead =
            true;


        currentHealth =
            0f;


        runtimeStats[healthStatName] =
            0f;


        OnHealthChanged?.Invoke(
            currentHealth,
            maxHealth
        );


        Debug.Log(
            $"[NPCStats] {npcData.NPCName} Dead"
        );


        OnDied?.Invoke();


        if (destroyOnDeath)
        {
            Destroy(
                gameObject,
                Mathf.Max(
                    0f,
                    destroyDelay
                )
            );
        }
    }
}