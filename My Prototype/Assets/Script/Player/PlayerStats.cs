using System;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;

    [Header("Combat")]
    [SerializeField] private float fireRate = 5f;
    [SerializeField] private float reloadSpeed = 1f;
    [SerializeField] private float attackPower = 20f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;


    // 현재 체력
    private float currentHealth;

    // 사망 여부
    private bool isDead;


    // =========================================================
    // 외부에서 읽을 수 있는 스탯
    // =========================================================

    public float MaxHealth => maxHealth;

    public float CurrentHealth => currentHealth;

    public float FireRate => fireRate;

    public float ReloadSpeed => reloadSpeed;

    public float MoveSpeed => moveSpeed;

    public float AttackPower => attackPower;

    public bool IsDead => isDead;


    // =========================================================
    // 체력 변경 이벤트
    // =========================================================

    public event Action<float, float> OnHealthChanged;

    public event Action OnDied;


    private void Awake()
    {
        currentHealth = maxHealth;
        isDead = false;
    }


    // =========================================================
    // 데미지
    // =========================================================

    public void TakeDamage(float damage)
    {
        if (isDead)
            return;

        if (damage <= 0f)
            return;


        currentHealth -= damage;

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


        if (currentHealth <= 0f)
        {
            Die();
        }
    }


    // =========================================================
    // 회복
    // =========================================================

    public void Heal(float amount)
    {
        if (isDead)
            return;

        if (amount <= 0f)
            return;


        currentHealth += amount;

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
    // 체력 완전 회복
    // =========================================================

    public void RestoreFullHealth()
    {
        if (isDead)
            return;


        currentHealth = maxHealth;


        OnHealthChanged?.Invoke(
            currentHealth,
            maxHealth
        );
    }


    // =========================================================
    // 사망
    // =========================================================

    private void Die()
    {
        if (isDead)
            return;


        isDead = true;

        currentHealth = 0f;


        Debug.Log("Player Dead");

        OnDied?.Invoke();
    }


    // =========================================================
    // 재장전 시간 계산
    // =========================================================

    public float GetReloadTime(float baseReloadTime)
    {
        return baseReloadTime / reloadSpeed;
    }


    // =========================================================
    // Inspector 값 보호
    // =========================================================

    private void OnValidate()
    {
        maxHealth =
            Mathf.Max(1f, maxHealth);

        fireRate =
            Mathf.Max(0.01f, fireRate);

        reloadSpeed =
            Mathf.Max(0.01f, reloadSpeed);

        moveSpeed =
            Mathf.Max(0f, moveSpeed);

        attackPower =
            Mathf.Max(0f, attackPower);
    }
}