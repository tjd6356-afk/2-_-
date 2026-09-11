using System;
using UnityEngine;

public class EnemyStats : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;


    [Header("Combat")]
    [Tooltip("초당 발사 횟수")]
    [SerializeField] private float fireRate = 2f;

    [Tooltip("1 = 기본 속도, 2 = 두 배 빠른 재장전")]
    [SerializeField] private float reloadSpeed = 1f;

    [Tooltip("총알 한 발의 데미지")]
    [SerializeField] private float attackPower = 10f;


    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3.5f;


    // =========================================================
    // Runtime
    // =========================================================

    private float currentHealth;

    private bool isDead;


    // =========================================================
    // 외부 접근용
    // =========================================================

    public float MaxHealth => maxHealth;

    public float CurrentHealth => currentHealth;

    public float FireRate => fireRate;

    public float ReloadSpeed => reloadSpeed;

    public float AttackPower => attackPower;

    public float BulletDamage => attackPower;

    public float MoveSpeed => moveSpeed;

    public bool IsDead => isDead;


    // =========================================================
    // Events
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
    // 전체 회복
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
    // 재장전 시간 계산
    // =========================================================

    public float GetReloadTime(float baseReloadTime)
    {
        return baseReloadTime /
               Mathf.Max(0.01f, reloadSpeed);
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


        Debug.Log($"{gameObject.name} Dead");


        OnDied?.Invoke();
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

        attackPower =
            Mathf.Max(0f, attackPower);

        moveSpeed =
            Mathf.Max(0f, moveSpeed);
    }
}