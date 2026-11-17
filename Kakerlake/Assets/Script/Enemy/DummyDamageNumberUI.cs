using TMPro;
using UnityEngine;

public class DummyDamageNumberUI : MonoBehaviour
{
    // =========================================================
    // References
    // =========================================================

    [Header("References")]

    [Tooltip("데미지를 측정할 DummyStats")]
    [SerializeField]
    private DummyStats dummyStats;


    [Tooltip("Damage Number UI 전체")]
    [SerializeField]
    private GameObject damageNumberRoot;


    [Tooltip("마지막 한 번의 공격으로 받은 데미지")]
    [SerializeField]
    private TMP_Text currentDamageText;


    [Tooltip("6초 동안 누적해서 받은 데미지")]
    [SerializeField]
    private TMP_Text totalDamageText;


    // =========================================================
    // Settings
    // =========================================================

    [Header("Settings")]

    [Tooltip("이 시간 동안 추가 공격을 받지 않으면 누적 데미지 초기화")]
    [SerializeField]
    private float resetTime = 6f;


    [Tooltip("초기화될 때 Damage Number UI도 숨길지")]
    [SerializeField]
    private bool hideWhenReset = true;


    // =========================================================
    // Runtime
    // =========================================================

    private float previousHealth;


    private float currentDamage;


    private float totalDamage;


    private float resetTimer;


    private bool damageActive;


    private bool initialized;


    // =========================================================
    // Awake
    // =========================================================

    private void Awake()
    {
        // ==========================================
        // DummyStats 자동 탐색
        // ==========================================

        if (dummyStats == null)
        {
            dummyStats =
                GetComponentInParent<DummyStats>();
        }


        ResetDamageUI();
    }


    // =========================================================
    // Start
    // =========================================================

    private void Start()
    {
        if (dummyStats == null)
        {
            Debug.LogError(
                $"[DummyDamageNumberUI] " +
                $"DummyStats를 찾지 못했습니다 : {name}"
            );

            return;
        }


        previousHealth =
            dummyStats.CurrentHealth;


        initialized =
            true;
    }


    // =========================================================
    // Enable
    // =========================================================

    private void OnEnable()
    {
        if (dummyStats == null)
        {
            dummyStats =
                GetComponentInParent<DummyStats>();
        }


        if (dummyStats == null)
            return;


        dummyStats.OnHealthChanged -=
            HandleHealthChanged;


        dummyStats.OnHealthChanged +=
            HandleHealthChanged;


        previousHealth =
            dummyStats.CurrentHealth;


        initialized =
            true;
    }


    // =========================================================
    // Disable
    // =========================================================

    private void OnDisable()
    {
        if (dummyStats == null)
            return;


        dummyStats.OnHealthChanged -=
            HandleHealthChanged;
    }


    // =========================================================
    // Update
    // =========================================================

    private void Update()
    {
        if (!damageActive)
            return;


        resetTimer -=
            Time.deltaTime;


        // 아직 6초가 지나지 않음
        if (resetTimer > 0f)
            return;


        ResetDamageUI();
    }


    // =========================================================
    // HP 변경 감지
    // =========================================================

    private void HandleHealthChanged(
        float currentHealth,
        float maxHealth
    )
    {
        if (!initialized)
        {
            previousHealth =
                currentHealth;


            initialized =
                true;


            return;
        }


        // =====================================================
        // 이전 체력 - 현재 체력
        // =====================================================

        float receivedDamage =
            previousHealth -
            currentHealth;


        // 다음 계산을 위해 현재 체력 기억
        previousHealth =
            currentHealth;


        // =====================================================
        // 회복 / Reset / 변화 없음
        // =====================================================

        if (receivedDamage <= 0f)
        {
            return;
        }


        RegisterDamage(
            receivedDamage
        );
    }


    // =========================================================
    // Damage 등록
    // =========================================================

    private void RegisterDamage(
        float damage
    )
    {
        if (damage <= 0f)
            return;


        // =====================================================
        // 이번 한 번의 Damage
        // =====================================================

        currentDamage =
            damage;


        // =====================================================
        // 누적 Damage
        // =====================================================

        totalDamage +=
            damage;


        // =====================================================
        // 마지막 피격 기준으로 6초 다시 시작
        // =====================================================

        resetTimer =
            resetTime;


        damageActive =
            true;


        // =====================================================
        // UI 표시
        // =====================================================

        if (damageNumberRoot != null)
        {
            damageNumberRoot.SetActive(
                true
            );
        }


        RefreshText();


        Debug.Log(
            $"[Dummy Damage] " +
            $"현재 받은 데미지 : {currentDamage:0.##} / " +
            $"누적 데미지 : {totalDamage:0.##}"
        );
    }


    // =========================================================
    // Text
    // =========================================================

    private void RefreshText()
    {
        if (currentDamageText != null)
        {
            currentDamageText.text =
                $"현재 받은 데미지 : " +
                $"{currentDamage:0.##}";
        }


        if (totalDamageText != null)
        {
            totalDamageText.text =
                $"받은 데미지 : " +
                $"{totalDamage:0.##}";
        }
    }


    // =========================================================
    // Reset
    // =========================================================

    private void ResetDamageUI()
    {
        currentDamage =
            0f;


        totalDamage =
            0f;


        resetTimer =
            0f;


        damageActive =
            false;


        if (currentDamageText != null)
        {
            currentDamageText.text =
                "현재 받은 데미지 : 0";
        }


        if (totalDamageText != null)
        {
            totalDamageText.text =
                "받은 데미지 : 0";
        }


        if (damageNumberRoot != null &&
            hideWhenReset)
        {
            damageNumberRoot.SetActive(
                false
            );
        }
    }


    // =========================================================
    // Inspector 보호
    // =========================================================

    private void OnValidate()
    {
        resetTime =
            Mathf.Max(
                0.1f,
                resetTime
            );
    }
}