using System.Collections.Generic;
using UnityEngine;

public class StageProgressManager : MonoBehaviour
{
    public static StageProgressManager Instance
    {
        get;
        private set;
    }


    // =========================================================
    // Progress
    // =========================================================

    [Header("Stage Progress")]

    [SerializeField]
    private int currentStage = 0;

    [SerializeField]
    private int moveCount = 0;


    [Header("Last Selected Route")]

    [SerializeField]
    private string lastRouteId = "";


    // 지나온 Route 기록
    private readonly List<string> routeHistory =
        new List<string>();


    public int CurrentStage =>
        currentStage;

    public int MoveCount =>
        moveCount;

    public string LastRouteId =>
        lastRouteId;

    public IReadOnlyList<string> RouteHistory =>
        routeHistory;


    // =========================================================
    // Singleton
    // =========================================================

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);

            return;
        }


        Instance =
            this;


        DontDestroyOnLoad(
            gameObject
        );


        Debug.Log(
            $"[StageProgress] 시작 Stage : {currentStage}"
        );
    }


    // =========================================================
    // 다음 Stage 이동
    // =========================================================

    public bool TryMoveToStage(
        int targetStage,
        string routeId
    )
    {
        int expectedStage =
            currentStage + 1;


        // 다음 Stage가 아니면 이동 금지
        if (targetStage != expectedStage)
        {
            Debug.LogWarning(
                $"[StageProgress] 이동 불가 : " +
                $"현재 Stage={currentStage}, " +
                $"요청 Stage={targetStage}, " +
                $"가능 Stage={expectedStage}"
            );

            return false;
        }


        currentStage =
            targetStage;


        moveCount++;


        lastRouteId =
            routeId;


        routeHistory.Add(
            routeId
        );


        Debug.Log(
            $"[StageProgress] Stage 이동 성공\n" +
            $"Current Stage : {currentStage}\n" +
            $"Move Count : {moveCount}\n" +
            $"Route : {routeId}"
        );


        return true;
    }


    // =========================================================
    // 나중에 새 게임 시작할 때 사용 가능
    // =========================================================

    public void ResetProgress()
    {
        currentStage =
            0;


        moveCount =
            0;


        lastRouteId =
            "";


        routeHistory.Clear();


        Debug.Log(
            "[StageProgress] 진행도 초기화"
        );
    }
}