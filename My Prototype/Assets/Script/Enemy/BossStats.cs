using System;
using System.Collections;
using UnityEngine;

public class BossStats : MonoBehaviour, IHealth
{
    // =========================================================
    // Health
    // =========================================================

    [Header("Health")]

    [SerializeField]
    private float maxHealth = 2000f;


    // =========================================================
    // Combat
    // =========================================================

    [Header("Combat")]

    [SerializeField]
    private float attackPower = 30f;


    // =========================================================
    // Movement
    // =========================================================

    [Header("Movement")]

    [SerializeField]
    private float moveSpeed = 3.5f;


    // =========================================================
    // Groggy
    // =========================================================

    [Header("Groggy")]

    [Tooltip("Groggy 상태가 되기 위해 필요한 누적 피해")]
    [SerializeField]
    private float maxGroggy = 100f;


    [Tooltip("이 시간 안에 Max Groggy만큼 피해를 받아야 Groggy 발생")]
    [SerializeField]
    private float groggyDamageWindow = 3f;


    [Tooltip("받은 피해가 Groggy에 얼마나 반영되는지")]
    [SerializeField]
    private float groggyGainMultiplier = 1f;


    [Tooltip("Groggy 상태 유지 시간")]
    [SerializeField]
    private float groggyDuration = 2f;


    [Tooltip("Groggy가 끝난 뒤 다시 Groggy가 쌓이기까지의 보호 시간")]
    [SerializeField]
    private float groggyRecoveryCooldown = 4f;


    // =========================================================
    // Runtime
    // =========================================================

    private float currentHealth;

    private float currentGroggy;

    private float groggyWindowEndTime;


    private bool isDead;

    private bool isGroggy;

    private bool groggyImmune;


    private Coroutine groggyRoutine;


    // =========================================================
    // Properties
    // =========================================================

    public float MaxHealth =>
        maxHealth;


    public float CurrentHealth =>
        currentHealth;


    public float AttackPower =>
        attackPower;


    public float MoveSpeed =>
        moveSpeed;


    public float MaxGroggy =>
        maxGroggy;


    public float CurrentGroggy =>
        currentGroggy;


    public bool IsDead =>
        isDead;


    public bool IsGroggy =>
        isGroggy;


    public bool IsGroggyImmune =>
        groggyImmune;


    // =========================================================
    // Events
    // =========================================================

    public event Action<float, float> OnHealthChanged;

    public event Action OnDied;


    public event Action<float, float> OnGroggyChanged;

    public event Action OnGroggyStarted;

    public event Action OnGroggyEnded;


    // =========================================================
    // Awake
    // =========================================================

    private void Awake()
    {
        currentHealth =
            maxHealth;


        currentGroggy =
            0f;


        isDead =
            false;


        isGroggy =
            false;


        groggyImmune =
            false;
    }


    // =========================================================
    // Update
    // =========================================================

    private void Update()
    {
        if (isDead ||
            isGroggy ||
            groggyImmune)
        {
            return;
        }


        // 제한 시간 안에 Groggy를 채우지 못했다면 초기화
        if (currentGroggy > 0f &&
            Time.time >
            groggyWindowEndTime)
        {
            currentGroggy =
                0f;


            OnGroggyChanged?.Invoke(
                currentGroggy,
                maxGroggy
            );
        }
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


        OnHealthChanged?.Invoke(
            currentHealth,
            maxHealth
        );


        // 죽었다면 Groggy 계산 불필요
        if (currentHealth <= 0f)
        {
            Die();

            return;
        }


        AddGroggyDamage(
            damage
        );
    }


    // =========================================================
    // Groggy Damage
    // =========================================================

    private void AddGroggyDamage(
        float damage
    )
    {
        // Groggy 중에는 또 Groggy 불가능
        if (isGroggy)
            return;


        // Groggy 종료 직후 보호 시간
        if (groggyImmune)
            return;


        // 새 Groggy 누적 시작
        if (currentGroggy <= 0f ||
            Time.time > groggyWindowEndTime)
        {
            currentGroggy =
                0f;


            groggyWindowEndTime =
                Time.time +
                groggyDamageWindow;
        }


        currentGroggy +=
            damage *
            groggyGainMultiplier;


        currentGroggy =
            Mathf.Clamp(
                currentGroggy,
                0f,
                maxGroggy
            );


        OnGroggyChanged?.Invoke(
            currentGroggy,
            maxGroggy
        );


        if (currentGroggy >=
            maxGroggy)
        {
            StartGroggy();
        }
    }


    // =========================================================
    // Start Groggy
    // =========================================================

    private void StartGroggy()
    {
        if (isDead ||
            isGroggy ||
            groggyImmune)
        {
            return;
        }


        if (groggyRoutine != null)
        {
            StopCoroutine(
                groggyRoutine
            );
        }


        groggyRoutine =
            StartCoroutine(
                GroggyRoutine()
            );
    }


    // =========================================================
    // Groggy Routine
    // =========================================================

    private IEnumerator GroggyRoutine()
    {
        isGroggy =
            true;


        currentGroggy =
            maxGroggy;


        OnGroggyChanged?.Invoke(
            currentGroggy,
            maxGroggy
        );


        OnGroggyStarted?.Invoke();


        Debug.Log(
            "[Boss] Groggy Start"
        );


        // ==========================================
        // Groggy 상태
        // ==========================================

        yield return new WaitForSeconds(
            groggyDuration
        );


        if (isDead)
            yield break;


        isGroggy =
            false;


        currentGroggy =
            0f;


        groggyImmune =
            true;


        OnGroggyChanged?.Invoke(
            currentGroggy,
            maxGroggy
        );


        OnGroggyEnded?.Invoke();


        Debug.Log(
            "[Boss] Groggy End / Immunity Start"
        );


        // ==========================================
        // Groggy 보호 시간
        // ==========================================

        yield return new WaitForSeconds(
            groggyRecoveryCooldown
        );


        groggyImmune =
            false;


        groggyRoutine =
            null;


        Debug.Log(
            "[Boss] Groggy Immunity End"
        );
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


        if (groggyRoutine != null)
        {
            StopCoroutine(
                groggyRoutine
            );
        }


        OnHealthChanged?.Invoke(
            currentHealth,
            maxHealth
        );


        Debug.Log(
            "[Boss] Dead"
        );


        OnDied?.Invoke();
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


        attackPower =
            Mathf.Max(
                0f,
                attackPower
            );


        moveSpeed =
            Mathf.Max(
                0f,
                moveSpeed
            );


        maxGroggy =
            Mathf.Max(
                1f,
                maxGroggy
            );


        groggyDamageWindow =
            Mathf.Max(
                0.1f,
                groggyDamageWindow
            );


        groggyDuration =
            Mathf.Max(
                0.1f,
                groggyDuration
            );


        groggyRecoveryCooldown =
            Mathf.Max(
                0f,
                groggyRecoveryCooldown
            );
    }
}