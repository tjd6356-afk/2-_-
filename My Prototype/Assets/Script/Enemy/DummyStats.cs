using System;
using UnityEngine;

public class DummyStats : MonoBehaviour, IHealth
{
    // =========================================================
    // Health
    // =========================================================

    [Header("Health")]

    [Tooltip("더미 최대 체력")]
    [SerializeField]
    private float maxHealth =
        10000f;


    [Tooltip("체력이 0이 되었을 때 오브젝트를 삭제할지")]
    [SerializeField]
    private bool destroyOnDeath =
        false;


    [Tooltip("죽은 더미가 자동으로 다시 회복될지")]
    [SerializeField]
    private bool autoResetAfterDeath =
        true;


    [Tooltip("사망 후 다시 체력이 차기까지 시간")]
    [SerializeField]
    private float resetDelay =
        1f;


    // =========================================================
    // Runtime
    // =========================================================

    private float currentHealth;


    private bool isDead;


    // =========================================================
    // IHealth
    // =========================================================

    public float MaxHealth =>
        maxHealth;


    public float CurrentHealth =>
        currentHealth;


    public bool IsDead =>
        isDead;


    public event Action<float, float>
        OnHealthChanged;


    public event Action
        OnDied;


    // =========================================================
    // Awake
    // =========================================================

    private void Awake()
    {
        currentHealth =
            maxHealth;


        isDead =
            false;
    }


    // =========================================================
    // Damage
    // =========================================================

    public void TakeDamage(
        float damage
    )
    {
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


        // ==========================================
        // HP 변경 알림
        // ==========================================

        OnHealthChanged?.Invoke(
            currentHealth,
            maxHealth
        );


        Debug.Log(
            $"[Dummy] Damage : {damage} / " +
            $"HP : {currentHealth}/{maxHealth}"
        );


        // ==========================================
        // Death
        // ==========================================

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


        OnHealthChanged?.Invoke(
            currentHealth,
            maxHealth
        );
    }


    // =========================================================
    // Full Heal
    // =========================================================

    public void RestoreFullHealth()
    {
        currentHealth =
            maxHealth;


        isDead =
            false;


        OnHealthChanged?.Invoke(
            currentHealth,
            maxHealth
        );


        Debug.Log(
            "[Dummy] HP Reset"
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


        OnHealthChanged?.Invoke(
            currentHealth,
            maxHealth
        );


        OnDied?.Invoke();


        Debug.Log(
            $"[Dummy] {gameObject.name} Dead"
        );


        // ==========================================
        // 죽으면 삭제
        // ==========================================

        if (destroyOnDeath)
        {
            Destroy(
                gameObject
            );

            return;
        }


        // ==========================================
        // 자동 체력 복구
        // ==========================================

        if (autoResetAfterDeath)
        {
            Invoke(
                nameof(RestoreFullHealth),
                resetDelay
            );
        }
    }


    // =========================================================
    // Inspector 보호
    // =========================================================

    private void OnValidate()
    {
        maxHealth =
            Mathf.Max(
                1f,
                maxHealth
            );


        resetDelay =
            Mathf.Max(
                0f,
                resetDelay
            );
    }
}