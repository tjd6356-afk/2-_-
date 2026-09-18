using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    private bool isLoading;

    public bool IsLoading => isLoading;


    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }


        Instance = this;

        DontDestroyOnLoad(gameObject);
    }


    // =========================================================
    // 씬 전환 요청
    // =========================================================

    public void LoadScene(string sceneName)
    {
        if (isLoading)
            return;


        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError(
                "이동할 Scene 이름이 설정되지 않았습니다."
            );

            return;
        }


        StartCoroutine(
            LoadSceneRoutine(sceneName)
        );
    }


    // =========================================================
    // 실제 씬 로딩
    // =========================================================

    private IEnumerator LoadSceneRoutine(
        string sceneName
    )
    {
        isLoading = true;


        // 혹시 이전 시스템에서 TimeScale을 건드렸을 경우
        Time.timeScale = 1f;


        AsyncOperation operation =
            SceneManager.LoadSceneAsync(
                sceneName
            );


        if (operation == null)
        {
            Debug.LogError(
                $"Scene Load 실패 : {sceneName}"
            );

            isLoading = false;
            yield break;
        }


        while (!operation.isDone)
        {
            yield return null;
        }


        isLoading = false;
    }
}