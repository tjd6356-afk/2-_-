using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BossHealthUI : MonoBehaviour
{
    // =========================================================
    // UI
    // =========================================================

    [Header("UI")]

    [SerializeField]
    private GameObject panelRoot;


    [SerializeField]
    private TMP_Text bossNameText;


    [Tooltip("실제 현재 HP")]
    [SerializeField]
    private Image healthFill;


    [Tooltip("데미지를 뒤늦게 따라가는 HP")]
    [SerializeField]
    private Image lateHPFill;


    [Tooltip("Groggy Gauge")]
    [SerializeField]
    private Image groggyFill;


    // =========================================================
    // Boss
    // =========================================================

    [Header("Boss")]

    [SerializeField]
    private string bossName =
        "BOSS";


    // =========================================================
    // Late HP
    // =========================================================

    [Header("Late HP")]

    [Tooltip(
        "첫 데미지를 받은 뒤 Late HP가 " +
        "움직이기 시작할 때까지 기다리는 시간"
    )]
    [SerializeField]
    private float lateHPDelay =
        0.5f;


    [Tooltip("Late HP의 기본 감소 속도")]
    [SerializeField]
    private float lateHPBaseSpeed =
        0.35f;


    [Tooltip(
        "실제 HP와 Late HP의 차이가 클수록 " +
        "얼마나 더 빠르게 따라갈지"
    )]
    [SerializeField]
    private float lateHPGapSpeedMultiplier =
        2.5f;


    [Tooltip(
        "Late HP와 실제 HP 차이가 이 값보다 작으면 " +
        "즉시 동일하게 맞춤"
    )]
    [SerializeField]
    private float lateHPSnapDistance =
        0.001f;


    [Tooltip(
        "Boss 사망 후 Late HP가 0까지 내려간 뒤 " +
        "체력바가 사라지기까지 기다리는 시간"
    )]
    [SerializeField]
    private float deathHideDelay =
        0.3f;


    // =========================================================
    // Runtime
    // =========================================================

    private BossStats currentBoss;


    // Late HP가 최종적으로 따라가야 할 위치
    private float targetHealthFill =
        1f;


    // 첫 피격 후 0.5초 기다리는 중인가
    private bool waitingForLateHP;


    // Late HP가 실제 HP를 따라가는 중인가
    private bool lateHPChasing;


    private bool bossDead;


    private Coroutine lateHPDelayCoroutine;


    private Coroutine deathRoutine;


    // =========================================================
    // Awake
    // =========================================================

    private void Awake()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(
                false
            );
        }
    }


    // =========================================================
    // Update
    // =========================================================

    private void Update()
    {
        UpdateLateHP();
    }


    // =========================================================
    // Late HP 움직임
    // =========================================================

    private void UpdateLateHP()
    {
        if (!lateHPChasing)
            return;


        if (lateHPFill == null)
            return;


        // =====================================================
        // Late HP와 실제 HP 차이
        // =====================================================

        float gap =
            lateHPFill.fillAmount -
            targetHealthFill;


        // =====================================================
        // 목표에 도착
        // =====================================================

        if (gap <=
            lateHPSnapDistance)
        {
            lateHPFill.fillAmount =
                targetHealthFill;


            lateHPChasing =
                false;


            return;
        }


        // =====================================================
        // HP 차이가 클수록 더 빠르게 감소
        // =====================================================

        float speed =
            lateHPBaseSpeed +
            gap *
            lateHPGapSpeedMultiplier;


        lateHPFill.fillAmount =
            Mathf.MoveTowards(
                lateHPFill.fillAmount,
                targetHealthFill,
                speed *
                Time.deltaTime
            );
    }


    // =========================================================
    // 기존 시스템 호환용 Open
    // =========================================================

    public void Open(
        BossStats boss
    )
    {
        ShowBoss(
            boss,
            bossName
        );
    }


    // =========================================================
    // 새로운 방식
    // =========================================================

    public void ShowBoss(
        BossStats boss,
        string displayBossName = null
    )
    {
        if (boss == null)
            return;


        BindBoss(
            boss
        );


        // =====================================================
        // Panel 표시
        // =====================================================

        if (panelRoot != null)
        {
            panelRoot.SetActive(
                true
            );


            panelRoot.transform.SetAsLastSibling();
        }


        // =====================================================
        // Boss Name
        // =====================================================

        if (bossNameText != null)
        {
            if (!string.IsNullOrWhiteSpace(
                    displayBossName))
            {
                bossNameText.text =
                    displayBossName;
            }
            else
            {
                bossNameText.text =
                    bossName;
            }
        }
    }


    // =========================================================
    // Boss 연결
    // =========================================================

    private void BindBoss(
        BossStats boss
    )
    {
        UnbindBoss();


        currentBoss =
            boss;


        if (currentBoss == null)
            return;


        bossDead =
            false;


        // =====================================================
        // Events
        // =====================================================

        currentBoss.OnHealthChanged +=
            UpdateHealth;


        currentBoss.OnGroggyChanged +=
            UpdateGroggy;


        currentBoss.OnDied +=
            HandleBossDeath;


        // =====================================================
        // 현재 HP 동기화
        // =====================================================

        float normalizedHealth =
            Mathf.Clamp01(
                currentBoss.CurrentHealth /
                Mathf.Max(
                    1f,
                    currentBoss.MaxHealth
                )
            );


        targetHealthFill =
            normalizedHealth;


        if (healthFill != null)
        {
            healthFill.fillAmount =
                normalizedHealth;
        }


        if (lateHPFill != null)
        {
            lateHPFill.fillAmount =
                normalizedHealth;
        }


        // =====================================================
        // 현재 Groggy 동기화
        // =====================================================

        UpdateGroggy(
            currentBoss.CurrentGroggy,
            currentBoss.MaxGroggy
        );


        waitingForLateHP =
            false;


        lateHPChasing =
            false;
    }


    // =========================================================
    // Health
    // =========================================================

    private void UpdateHealth(
        float current,
        float max
    )
    {
        if (max <= 0f)
            return;


        float normalizedHealth =
            Mathf.Clamp01(
                current /
                max
            );


        // =====================================================
        // 변경 전 실제 HP
        // =====================================================

        float previousHealth =
            healthFill != null
            ? healthFill.fillAmount
            : normalizedHealth;


        // =====================================================
        // 실제 HP는 즉시 감소
        // =====================================================

        if (healthFill != null)
        {
            healthFill.fillAmount =
                normalizedHealth;
        }


        // Late HP가 따라갈 새로운 목표
        targetHealthFill =
            normalizedHealth;


        // =====================================================
        // 회복
        // =====================================================

        if (normalizedHealth >
            previousHealth)
        {
            // 기다리던 Late HP Coroutine 제거
            if (lateHPDelayCoroutine != null)
            {
                StopCoroutine(
                    lateHPDelayCoroutine
                );


                lateHPDelayCoroutine =
                    null;
            }


            waitingForLateHP =
                false;


            lateHPChasing =
                false;


            // 회복은 Late HP도 즉시 맞춤
            if (lateHPFill != null)
            {
                lateHPFill.fillAmount =
                    normalizedHealth;
            }


            return;
        }


        // =====================================================
        // HP가 변하지 않았다면
        // =====================================================

        if (normalizedHealth >=
            previousHealth)
        {
            return;
        }


        // =====================================================
        // Damage 발생
        //
        // 이미 따라가는 중이라면
        // 목표값만 변경하고 계속 추적
        // =====================================================

        if (lateHPChasing)
        {
            return;
        }


        // =====================================================
        // 첫 피해 후 이미 기다리는 중이면
        //
        // 또 맞더라도 0.5초 타이머 Reset하지 않음
        // =====================================================

        if (waitingForLateHP)
        {
            return;
        }


        // =====================================================
        // 첫 Damage
        // =====================================================

        lateHPDelayCoroutine =
            StartCoroutine(
                LateHPDelayRoutine()
            );
    }


    // =========================================================
    // Late HP Delay
    // =========================================================

    private IEnumerator LateHPDelayRoutine()
    {
        waitingForLateHP =
            true;


        yield return
            new WaitForSeconds(
                lateHPDelay
            );


        waitingForLateHP =
            false;


        lateHPDelayCoroutine =
            null;


        // 실제 HP 추적 시작
        lateHPChasing =
            true;
    }


    // =========================================================
    // Groggy
    // =========================================================

    private void UpdateGroggy(
        float current,
        float max
    )
    {
        if (groggyFill == null)
            return;


        if (max <= 0f)
        {
            groggyFill.fillAmount =
                0f;


            return;
        }


        groggyFill.fillAmount =
            Mathf.Clamp01(
                current /
                max
            );
    }


    // =========================================================
    // Boss Death
    // =========================================================

    private void HandleBossDeath()
    {
        if (bossDead)
            return;


        bossDead =
            true;


        // 실제 HP는 즉시 0
        targetHealthFill =
            0f;


        if (healthFill != null)
        {
            healthFill.fillAmount =
                0f;
        }


        // =====================================================
        // Late HP도 마지막으로 0까지 내려오게 함
        // =====================================================

        if (!waitingForLateHP &&
            !lateHPChasing)
        {
            lateHPDelayCoroutine =
                StartCoroutine(
                    LateHPDelayRoutine()
                );
        }


        // =====================================================
        // Late HP 연출이 끝난 뒤 UI 숨김
        // =====================================================

        if (deathRoutine != null)
        {
            StopCoroutine(
                deathRoutine
            );
        }


        deathRoutine =
            StartCoroutine(
                BossDeathUIRoutine()
            );
    }


    // =========================================================
    // Death UI
    // =========================================================

    private IEnumerator BossDeathUIRoutine()
    {
        // Late HP가 0이 될 때까지 기다림
        while (
            lateHPFill != null &&
            lateHPFill.fillAmount >
            lateHPSnapDistance)
        {
            yield return null;
        }


        if (deathHideDelay >
            0f)
        {
            yield return
                new WaitForSeconds(
                    deathHideDelay
                );
        }


        deathRoutine =
            null;


        Hide();
    }


    // =========================================================
    // 기존 시스템 호환용 Hide
    // =========================================================

    public void Hide()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(
                false
            );
        }


        UnbindBoss();
    }


    // =========================================================
    // 새로운 코드 호환용 HideBoss
    // =========================================================

    public void HideBoss()
    {
        Hide();
    }


    // =========================================================
    // Unbind
    // =========================================================

    private void UnbindBoss()
    {
        if (currentBoss != null)
        {
            currentBoss.OnHealthChanged -=
                UpdateHealth;


            currentBoss.OnGroggyChanged -=
                UpdateGroggy;


            currentBoss.OnDied -=
                HandleBossDeath;
        }


        currentBoss =
            null;


        // =====================================================
        // Coroutine 초기화
        // =====================================================

        if (lateHPDelayCoroutine != null)
        {
            StopCoroutine(
                lateHPDelayCoroutine
            );


            lateHPDelayCoroutine =
                null;
        }


        if (deathRoutine != null)
        {
            StopCoroutine(
                deathRoutine
            );


            deathRoutine =
                null;
        }


        waitingForLateHP =
            false;


        lateHPChasing =
            false;


        bossDead =
            false;
    }


    // =========================================================
    // Inspector 보호
    // =========================================================

    private void OnValidate()
    {
        lateHPDelay =
            Mathf.Max(
                0f,
                lateHPDelay
            );


        lateHPBaseSpeed =
            Mathf.Max(
                0.01f,
                lateHPBaseSpeed
            );


        lateHPGapSpeedMultiplier =
            Mathf.Max(
                0f,
                lateHPGapSpeedMultiplier
            );


        lateHPSnapDistance =
            Mathf.Max(
                0.0001f,
                lateHPSnapDistance
            );


        deathHideDelay =
            Mathf.Max(
                0f,
                deathHideDelay
            );
    }


    // =========================================================
    // Destroy
    // =========================================================

    private void OnDestroy()
    {
        UnbindBoss();
    }
}