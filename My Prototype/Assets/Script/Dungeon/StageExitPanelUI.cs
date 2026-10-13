using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StageExitPanelUI : MonoBehaviour
{
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

    public void Open()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(
                true
            );


            // 다른 UI 위에 표시
            panelRoot.transform.SetAsLastSibling();
        }


        Cursor.lockState =
            CursorLockMode.None;

        Cursor.visible =
            true;


        RefreshButtons();


        Debug.Log(
            "[StageExitPanel] Open"
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


        Cursor.lockState =
            CursorLockMode.Locked;

        Cursor.visible =
            false;
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
}