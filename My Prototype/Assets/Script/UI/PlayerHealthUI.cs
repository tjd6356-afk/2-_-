using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerStats playerStats;

    [SerializeField] private Image healthFillImage;


    private void Awake()
    {
        // PlayerStats가 연결되지 않았다면 자동 검색
        if (playerStats == null)
        {
            playerStats =
                FindFirstObjectByType<PlayerStats>();
        }
    }


    private void Start()
    {
        UpdateHealthBar();
    }


    private void Update()
    {
        // 매 프레임 현재 체력을 확인
        UpdateHealthBar();
    }


    private void UpdateHealthBar()
    {
        if (playerStats == null)
            return;


        if (healthFillImage == null)
            return;


        if (playerStats.MaxHealth <= 0f)
            return;


        // 현재 체력 비율
        float healthPercent =
            playerStats.CurrentHealth /
            playerStats.MaxHealth;


        // 0 ~ 1 사이로 제한
        healthPercent =
            Mathf.Clamp01(
                healthPercent
            );


        // HP바 즉시 갱신
        healthFillImage.fillAmount =
            healthPercent;
    }
}