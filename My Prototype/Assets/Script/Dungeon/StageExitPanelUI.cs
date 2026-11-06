using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum StageExitType
{
    NormalStage,
    BossRoom
}

public class StageExitPanelUI : MonoBehaviour
{
    private PlayerGameplayControl currentPlayerControl;
    private bool merchantRoutePrepared;
    public static StageExitPanelUI Instance
    {
        get;
        private set;
    }

    // =========================================================
    // Button Data
    // =========================================================

    [System.Serializable]
    public class StageChoice
    {
        [Header("Button")]
        public Button button;


        [Header("Exit Type")]
        [Tooltip("일반 스테이지 이동인지 Boss Room 이동인지")]
        public StageExitType exitType =
            StageExitType.NormalStage;


        // =========================================================
        // 일반 Stage용
        // =========================================================

        [Header("Normal Stage")]

        [Tooltip("이 버튼이 이동할 Stage 번호")]
        public int targetStage = 1;

        [Tooltip("예: 1_01, 1_02")]
        public string routeId;

        [Tooltip("상점, 강화 재료 등의 기획 설명")]
        public string description;


        // =========================================================
        // Boss Room용
        // =========================================================

        [Header("Boss Room")]

        [Tooltip("Boss Room으로 사용할 Scene 이름")]
        public string bossSceneName;

        [Tooltip("이 Stage 이상일 때 Boss 버튼 사용 가능")]
        public int bossUnlockStage = 0;

        [Header("Merchant")]

        [SerializeField]
        private GameObject merchantMarker;


        [SerializeField]
        private bool bossRoute;


        [System.NonSerialized]
        private bool hasMerchant;


        public GameObject MerchantMarker =>
            merchantMarker;


        public bool HasMerchant =>
            hasMerchant;


        public void SetMerchant(
            bool value
        )
        {
            hasMerchant =
                value;


            if (merchantMarker != null)
            {
                merchantMarker.SetActive(
                    value
                );
            }
        }
    }


    // =========================================================
    // UI
    // =========================================================

    [Header("UI")]

    [SerializeField]
    private GameObject panelRoot;


    [Header("Stage Buttons")]

    [SerializeField]
    private List<StageChoice> stageChoices =
        new List<StageChoice>();


    // =========================================================
    // Reload
    // =========================================================

    [Header("Scene Reload")]

    [Tooltip("클릭 중복 방지")]
    [SerializeField]
    private float reloadDelay = 0.1f;


    private bool isMovingStage;


    // =========================================================
    // Awake
    // =========================================================

    private void Awake()
    {
        // ==========================================
        // 각 Button 자동 연결
        // ==========================================

        Instance = this;

        foreach (
            StageChoice choice in stageChoices)
        {
            if (choice == null ||
                choice.button == null)
            {
                continue;
            }


            // foreach closure 문제 방지
            StageChoice capturedChoice =
                choice;


            capturedChoice.button.onClick.AddListener(
                () =>
                {
                    OnStageButtonClicked(
                        capturedChoice
                    );
                }
            );
        }


        if (panelRoot != null)
        {
            panelRoot.SetActive(
                false
            );
        }
    }


    private void Start()
    {
        RefreshButtons();
    }


    // =========================================================
    // Panel Open
    // =========================================================

    public void Open(
    PlayerGameplayControl playerControl
)
    {
        currentPlayerControl =
            playerControl;


        if (panelRoot == null)
        {
            Debug.LogError(
                "[StageExitPanelUI] Panel Root가 없습니다."
            );

            return;
        }


        // ==========================================
        // 플레이어 조작 잠금
        // ==========================================

        if (currentPlayerControl != null)
        {
            currentPlayerControl.LockControls();
        }
        else
        {
            // 안전장치
            Cursor.lockState =
                CursorLockMode.None;

            Cursor.visible =
                true;


            Debug.LogWarning(
                "[StageExitPanelUI] PlayerGameplayControl을 찾지 못했습니다."
            );
        }


        // ==========================================
        // UI 열기
        // ==========================================

        panelRoot.SetActive(
            true
        );


        panelRoot.transform.SetAsLastSibling();


        RefreshButtons();

        PrepareMerchantRoute();

        Debug.Log(
            "[StageExitPanelUI] Open"
        );
    }


    // =========================================================
    // Close
    // =========================================================

    public void Close()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(
                false
            );
        }


        if (currentPlayerControl != null)
        {
            currentPlayerControl.UnlockControls();
        }


        currentPlayerControl =
            null;


        Debug.Log(
            "[StageExitPanelUI] Close"
        );
    }


    // =========================================================
    // 현재 Stage에 따라 Button 잠금
    // =========================================================

    public void RefreshButtons()
    {
        if (StageProgressManager.Instance == null)
        {
            Debug.LogWarning(
                "[StageExitPanel] StageProgressManager가 없습니다."
            );

            return;
        }


        int currentStage =
            StageProgressManager.Instance.CurrentStage;


        int nextStage =
            currentStage + 1;


        foreach (StageChoice choice in stageChoices)
        {
            if (choice == null ||
                choice.button == null)
            {
                continue;
            }


            bool canSelect = false;


            // =====================================================
            // 일반 Stage
            // =====================================================

            if (choice.exitType ==
                StageExitType.NormalStage)
            {
                canSelect =
                    choice.targetStage ==
                    nextStage;
            }


            // =====================================================
            // Boss Room
            // =====================================================

            else if (choice.exitType ==
                     StageExitType.BossRoom)
            {
                canSelect =
                    currentStage >=
                    choice.bossUnlockStage;
            }


            choice.button.interactable =
                canSelect;
        }


        Debug.Log(
            $"[StageExitPanel] Current Stage = {currentStage}"
        );
    }


    // =========================================================
    // Stage Button Click
    // =========================================================

    private void OnStageButtonClicked(
    StageChoice choice
)
    {
        if (isMovingStage)
            return;


        if (choice == null)
            return;


        // =========================================================
        // 일반 Stage
        // =========================================================

        if (choice.exitType ==
            StageExitType.NormalStage)
        {
            MoveToNormalStage(
                choice
            );

            return;
        }


        // =========================================================
        // Boss Room
        // =========================================================

        if (choice.exitType ==
            StageExitType.BossRoom)
        {
            MoveToBossRoom(
                choice
            );
        }
    }

    private void MoveToBossRoom(
    StageChoice choice
)
    {
        if (string.IsNullOrWhiteSpace(
                choice.bossSceneName))
        {
            Debug.LogError(
                "[StageExitPanel] Boss Scene 이름이 비어있습니다."
            );

            return;
        }


        Debug.Log(
            $"[StageExitPanel] Boss Room 이동 : " +
            $"{choice.bossSceneName}"
        );


        isMovingStage =
            true;


        SetAllButtonsInteractable(
            false
        );


        Time.timeScale =
            1f;

        MerchantStageSpawnState
        .Instance
        .SetNextDungeonMerchant(
        false);

        SceneManager.LoadScene(
            choice.bossSceneName
        );
    }


    // =========================================================
    // Dungeon Scene Reload
    // =========================================================

    private IEnumerator ReloadDungeonRoutine()
    {
        if (reloadDelay > 0f)
        {
            yield return
                new WaitForSecondsRealtime(
                    reloadDelay
                );
        }


        Time.timeScale =
            1f;


        Scene currentScene =
            SceneManager.GetActiveScene();


        Debug.Log(
            $"[StageExitPanel] Scene Reload : {currentScene.name}"
        );


        SceneManager.LoadScene(
            currentScene.buildIndex
        );
    }

    private void MoveToNormalStage(
    StageChoice choice
)
    {
        if (StageProgressManager.Instance == null)
        {
            Debug.LogError(
                "[StageExitPanel] StageProgressManager가 없습니다."
            );

            return;
        }


        bool success =
            StageProgressManager.Instance.TryMoveToStage(
                choice.targetStage,
                choice.routeId
            );


        if (!success)
            return;


        Debug.Log(
            $"[StageExitPanel] 일반 Stage 선택 : " +
            $"{choice.routeId}"
        );

        // =========================================================
        // 선택한 Route에 Merchant가 있으면
        // 다음 Dungeon에서 상인 확정 생성
        // =========================================================

        MerchantStageSpawnState
            .Instance
            .SetNextDungeonMerchant(
                choice.HasMerchant
            );

        isMovingStage =
            true;


        SetAllButtonsInteractable(
            false
        );


        StartCoroutine(
            ReloadDungeonRoutine()
        );
    }

    private void SetAllButtonsInteractable(
        bool value
    )
    {
        foreach (
            StageChoice choice in stageChoices)
        {
            if (choice?.button != null)
            {
                choice.button.interactable =
                    value;
            }
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // =========================================================
    // 이번 Stage 선택지 중
    // Merchant가 등장할 Route 하나 선정
    // =========================================================

    private void PrepareMerchantRoute()
    {
        if (merchantRoutePrepared)
            return;


        merchantRoutePrepared =
            true;


        List<StageChoice> candidates =
            new List<StageChoice>();


        // =====================================================
        // 우선 모든 Marker 초기화
        // =====================================================

        foreach (
            StageChoice choice
            in stageChoices)
        {
            if (choice == null)
                continue;


            choice.SetMerchant(
                false
            );
        }


        // =====================================================
        // 현재 실제 선택 가능한 일반 Route만 후보
        // =====================================================

        foreach (
            StageChoice choice
            in stageChoices)
        {
            if (choice == null)
                continue;


            if (choice.exitType ==
            StageExitType.BossRoom)
            {
                continue;
            }

            if (choice.button == null)
                continue;


            // 현재 Stage에서 잠겨있는 버튼 제외
            if (!choice.button.interactable)
                continue;


            candidates.Add(
                choice
            );
        }


        if (candidates.Count == 0)
        {
            Debug.Log(
                "[StageExit] Merchant Route 후보 없음"
            );


            return;
        }


        // =====================================================
        // 정확히 하나만 선택
        // =====================================================

        StageChoice selected =
        candidates[
        UnityEngine.Random.Range(
            0,
            candidates.Count
        )
        ];


        selected.SetMerchant(
            true
        );


        Debug.Log(
            "[StageExit] Merchant 확정 Route 생성"
        );
    }
}