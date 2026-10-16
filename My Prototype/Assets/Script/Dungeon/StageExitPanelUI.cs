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
}