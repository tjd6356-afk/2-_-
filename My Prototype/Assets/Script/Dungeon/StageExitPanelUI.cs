using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

    [Serializable]
    public class StageChoice
    {
        [Tooltip("UI Button")]
        public Button button;


        [Tooltip("이 버튼이 이동하는 Stage 번호")]
        public int targetStage;


        [Tooltip("경로 식별자. 예: 1_01")]
        public string routeId;


        [Tooltip("디버그/기획용 설명")]
        public string description;
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


        foreach (
            StageChoice choice in stageChoices)
        {
            if (choice == null ||
                choice.button == null)
            {
                continue;
            }


            // =================================================
            // 오직 다음 Stage 버튼만 활성화
            // =================================================

            bool canSelect =
                choice.targetStage ==
                nextStage;


            choice.button.interactable =
                canSelect;
        }


        Debug.Log(
            $"[StageExitPanel] Current={currentStage} / " +
            $"Next={nextStage}"
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
            $"[StageExitPanel] 선택 : " +
            $"{choice.routeId} / " +
            $"{choice.description}"
        );


        isMovingStage =
            true;


        // 모든 버튼 즉시 막음
        SetAllButtonsInteractable(
            false
        );


        StartCoroutine(
            ReloadDungeonRoutine()
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