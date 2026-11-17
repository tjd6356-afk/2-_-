using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField]
    private Image healthFillImage;


    [Header("Player")]
    [SerializeField]
    private PlayerStats playerStats;


    private Coroutine findPlayerRoutine;


    private void Start()
    {
        // Inspector에 Player가 연결되어 있으면 바로 사용
        if (playerStats != null)
        {
            RefreshHealthBar();
        }
        else
        {
            BeginFindPlayer();
        }
    }


    private void Update()
    {
        // ==========================================
        // 실시간 HP 표시
        // ==========================================

        if (playerStats == null)
            return;


        RefreshHealthBar();
    }


    // =========================================================
    // Player 자동 검색 시작
    // =========================================================

    private void BeginFindPlayer()
    {
        if (findPlayerRoutine != null)
        {
            StopCoroutine(
                findPlayerRoutine
            );
        }


        findPlayerRoutine =
            StartCoroutine(
                FindPlayerRoutine()
            );
    }


    // =========================================================
    // GeneratedPlayer가 생성될 때까지 기다림
    // =========================================================

    private IEnumerator FindPlayerRoutine()
    {
        while (playerStats == null)
        {
            playerStats =
                FindFirstObjectByType<PlayerStats>();


            if (playerStats == null)
            {
                yield return null;
            }
        }


        Debug.Log(
            $"[HPUI] Player 연결 성공 : {playerStats.gameObject.name}"
        );


        RefreshHealthBar();


        findPlayerRoutine =
            null;
    }


    // =========================================================
    // DungeonGenerator에서 직접 Player 전달 가능
    // =========================================================

    public void SetPlayer(
        GameObject player
    )
    {
        if (player == null)
            return;


        PlayerStats newStats =
            player.GetComponent<PlayerStats>();


        if (newStats == null)
        {
            newStats =
                player.GetComponentInChildren<PlayerStats>();
        }


        if (newStats == null)
        {
            Debug.LogWarning(
                "[HPUI] 생성된 Player에서 PlayerStats를 찾지 못했습니다."
            );

            return;
        }


        playerStats =
            newStats;


        Debug.Log(
            $"[HPUI] Player 직접 연결 성공 : {player.name}"
        );


        RefreshHealthBar();
    }


    // =========================================================
    // HP Bar 갱신
    // =========================================================

    private void RefreshHealthBar()
    {
        if (playerStats == null ||
            healthFillImage == null)
        {
            return;
        }


        if (playerStats.MaxHealth <= 0f)
        {
            healthFillImage.fillAmount =
                0f;

            return;
        }


        float healthPercent =
            playerStats.CurrentHealth /
            playerStats.MaxHealth;


        healthFillImage.fillAmount =
            Mathf.Clamp01(
                healthPercent
            );
    }


    private void OnDisable()
    {
        if (findPlayerRoutine != null)
        {
            StopCoroutine(
                findPlayerRoutine
            );


            findPlayerRoutine =
                null;
        }
    }
}