using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BossHealthUI : MonoBehaviour
{
    [Header("UI")]

    [SerializeField]
    private GameObject panelRoot;


    [SerializeField]
    private TMP_Text bossNameText;


    [SerializeField]
    private Image healthFill;


    [SerializeField]
    private Image groggyFill;


    [Header("Boss")]

    [SerializeField]
    private string bossName =
        "BOSS";


    private BossStats currentBoss;


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
    // Open
    // =========================================================

    public void Open(
        BossStats boss
    )
    {
        BindBoss(
            boss
        );


        if (panelRoot != null)
        {
            panelRoot.SetActive(
                true
            );


            panelRoot.transform.SetAsLastSibling();
        }


        if (bossNameText != null)
        {
            bossNameText.text =
                bossName;
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


        currentBoss.OnHealthChanged +=
            UpdateHealth;


        currentBoss.OnGroggyChanged +=
            UpdateGroggy;


        currentBoss.OnDied +=
            HandleBossDeath;


        UpdateHealth(
            currentBoss.CurrentHealth,
            currentBoss.MaxHealth
        );


        UpdateGroggy(
            currentBoss.CurrentGroggy,
            currentBoss.MaxGroggy
        );
    }


    // =========================================================
    // Health
    // =========================================================

    private void UpdateHealth(
        float current,
        float max
    )
    {
        if (healthFill == null ||
            max <= 0f)
        {
            return;
        }


        healthFill.fillAmount =
            Mathf.Clamp01(
                current /
                max
            );
    }


    // =========================================================
    // Groggy
    // =========================================================

    private void UpdateGroggy(
        float current,
        float max
    )
    {
        if (groggyFill == null ||
            max <= 0f)
        {
            return;
        }


        groggyFill.fillAmount =
            Mathf.Clamp01(
                current /
                max
            );
    }


    // =========================================================
    // Death
    // =========================================================

    private void HandleBossDeath()
    {
        Hide();
    }


    // =========================================================
    // Hide
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
    // Unbind
    // =========================================================

    private void UnbindBoss()
    {
        if (currentBoss == null)
            return;


        currentBoss.OnHealthChanged -=
            UpdateHealth;


        currentBoss.OnGroggyChanged -=
            UpdateGroggy;


        currentBoss.OnDied -=
            HandleBossDeath;


        currentBoss =
            null;
    }


    private void OnDestroy()
    {
        UnbindBoss();
    }
}