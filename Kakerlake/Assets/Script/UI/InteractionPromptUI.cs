using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InteractionPromptUI : MonoBehaviour
{
    // =========================================================
    // Singleton
    // =========================================================

    public static InteractionPromptUI Instance
    {
        get;
        private set;
    }


    // =========================================================
    // UI
    // =========================================================

    [Header("UI")]

    [SerializeField]
    private GameObject promptRoot;


    [SerializeField]
    private Image keyImage;


    [SerializeField]
    private TMP_Text actionText;


    // =========================================================
    // Runtime Request
    // =========================================================

    private class PromptRequest
    {
        public Component owner;

        public string text;

        public int priority;

        public int order;
    }


    private readonly List<PromptRequest> requests =
        new List<PromptRequest>();


    private int orderCounter;


    // =========================================================
    // Awake
    // =========================================================

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(
                gameObject
            );

            return;
        }


        Instance =
            this;


        if (promptRoot != null)
        {
            promptRoot.SetActive(
                false
            );
        }
    }


    // =========================================================
    // Update
    // =========================================================

    private void LateUpdate()
    {
        CleanupDestroyedRequests();

        RefreshPrompt();
    }


    // =========================================================
    // Prompt 표시 요청
    // =========================================================

    public void ShowPrompt(
        Component owner,
        string message,
        int priority = 0
    )
    {
        if (owner == null)
            return;


        // ==========================================
        // 이미 등록된 Owner인지 검사
        // ==========================================

        for (int i = 0;
             i < requests.Count;
             i++)
        {
            if (requests[i].owner != owner)
                continue;


            requests[i].text =
                message;


            requests[i].priority =
                priority;


            requests[i].order =
                ++orderCounter;


            RefreshPrompt();

            return;
        }


        // ==========================================
        // 새 Prompt 등록
        // ==========================================

        PromptRequest request =
            new PromptRequest
            {
                owner =
                    owner,

                text =
                    message,

                priority =
                    priority,

                order =
                    ++orderCounter
            };


        requests.Add(
            request
        );


        RefreshPrompt();
    }


    // =========================================================
    // Prompt 해제
    // =========================================================

    public void HidePrompt(
        Component owner
    )
    {
        if (owner == null)
            return;


        for (int i =
                 requests.Count - 1;
             i >= 0;
             i--)
        {
            if (requests[i].owner ==
                owner)
            {
                requests.RemoveAt(
                    i
                );
            }
        }


        RefreshPrompt();
    }


    // =========================================================
    // 모든 Prompt 제거
    // =========================================================

    public void ClearAll()
    {
        requests.Clear();


        if (promptRoot != null)
        {
            promptRoot.SetActive(
                false
            );
        }
    }


    // =========================================================
    // 현재 표시할 Prompt 결정
    // =========================================================

    private void RefreshPrompt()
    {
        if (promptRoot == null)
            return;


        // =====================================================
        // 다른 UI가 열렸거나 조작 잠금 상태
        // =====================================================

        if (PlayerGameplayControl.GameplayLocked)
        {
            promptRoot.SetActive(
                false
            );

            return;
        }


        // =====================================================
        // 등록된 Prompt 없음
        // =====================================================

        if (requests.Count == 0)
        {
            promptRoot.SetActive(
                false
            );

            return;
        }


        // =====================================================
        // Priority가 가장 높은 Prompt 선택
        //
        // 같은 Priority라면 가장 최근 Trigger 우선
        // =====================================================

        PromptRequest bestRequest =
            null;


        foreach (
            PromptRequest request in requests)
        {
            if (request.owner == null)
                continue;


            if (bestRequest == null)
            {
                bestRequest =
                    request;

                continue;
            }


            if (request.priority >
                bestRequest.priority)
            {
                bestRequest =
                    request;

                continue;
            }


            if (request.priority ==
                    bestRequest.priority &&
                request.order >
                    bestRequest.order)
            {
                bestRequest =
                    request;
            }
        }


        if (bestRequest == null)
        {
            promptRoot.SetActive(
                false
            );

            return;
        }


        // =====================================================
        // Text
        // =====================================================

        if (actionText != null)
        {
            actionText.text =
                bestRequest.text;
        }


        promptRoot.SetActive(
            true
        );
    }


    // =========================================================
    // Destroy된 Trigger 정리
    // =========================================================

    private void CleanupDestroyedRequests()
    {
        for (int i =
                 requests.Count - 1;
             i >= 0;
             i--)
        {
            if (requests[i].owner ==
                null)
            {
                requests.RemoveAt(
                    i
                );
            }
        }
    }


    // =========================================================
    // Destroy
    // =========================================================

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance =
                null;
        }
    }
}