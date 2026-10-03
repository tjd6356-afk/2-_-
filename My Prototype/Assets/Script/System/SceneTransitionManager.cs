using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance
    {
        get;
        private set;
    }


    private bool isLoading;

    public bool IsLoading => isLoading;


    private void Awake()
    {
        // 이미 Manager가 있다면
        // 새 Scene의 중복 Manager는 제거
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }


        Instance = this;


        DontDestroyOnLoad(
            gameObject
        );


        // Scene이 로드되면
        // Loading 상태를 확실하게 초기화
        SceneManager.sceneLoaded +=
            OnSceneLoaded;
    }


    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -=
                OnSceneLoaded;
        }
    }


    // =========================================================
    // Scene 로딩
    // =========================================================

    public void LoadScene(
        string sceneName
    )
    {
        if (isLoading)
        {
            Debug.LogWarning(
                $"[SceneTransition] 이미 Scene을 로딩 중입니다. : {sceneName}"
            );

            return;
        }


        if (string.IsNullOrWhiteSpace(
                sceneName))
        {
            Debug.LogError(
                "[SceneTransition] Scene 이름이 비어 있습니다."
            );

            return;
        }


        // Build Profile에 등록된 Scene인지 검사
        if (!Application.CanStreamedLevelBeLoaded(
                sceneName))
        {
            Debug.LogError(
                $"[SceneTransition] Scene을 찾을 수 없습니다 : {sceneName}\n" +
                "Build Profiles의 Scene List와 Scene 이름을 확인하세요."
            );

            return;
        }


        Debug.Log(
            $"[SceneTransition] Load Start : {sceneName}"
        );


        StartCoroutine(
            LoadSceneRoutine(
                sceneName
            )
        );
    }


    private IEnumerator LoadSceneRoutine(
        string sceneName
    )
    {
        isLoading =
            true;


        Time.timeScale =
            1f;


        AsyncOperation operation =
            SceneManager.LoadSceneAsync(
                sceneName
            );


        if (operation == null)
        {
            Debug.LogError(
                $"[SceneTransition] LoadSceneAsync 실패 : {sceneName}"
            );

            isLoading =
                false;

            yield break;
        }


        while (!operation.isDone)
        {
            yield return null;
        }
    }


    // =========================================================
    // Scene 로딩 완료
    // =========================================================

    private void OnSceneLoaded(
        Scene scene,
        LoadSceneMode mode
    )
    {
        isLoading =
            false;


        Debug.Log(
            $"[SceneTransition] Load Complete : {scene.name}"
        );
    }
}