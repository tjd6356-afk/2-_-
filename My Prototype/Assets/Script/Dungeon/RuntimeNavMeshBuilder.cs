using UnityEngine;
using Unity.AI.Navigation;

public class RuntimeNavMeshBuilder : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private NavMeshSurface navMeshSurface;


    public bool IsBuilt
    {
        get;
        private set;
    }


    private void Awake()
    {
        if (navMeshSurface == null)
        {
            navMeshSurface =
                GetComponent<NavMeshSurface>();
        }
    }


    // =========================================================
    // Runtime NavMesh 생성
    // =========================================================

    public void BuildNavMesh()
    {
        if (navMeshSurface == null)
        {
            Debug.LogError(
                "[RuntimeNavMesh] NavMeshSurface가 없습니다."
            );

            return;
        }


        IsBuilt = false;


        // 이전 Dungeon의 NavMesh 데이터 제거
        navMeshSurface.RemoveData();


        Debug.Log(
            "[RuntimeNavMesh] NavMesh 생성 시작"
        );


        // 현재 생성되어 있는 Dungeon을 기준으로 Bake
        navMeshSurface.BuildNavMesh();


        IsBuilt = true;


        Debug.Log(
            "[RuntimeNavMesh] NavMesh 생성 완료"
        );
    }


    // =========================================================
    // NavMesh 제거
    // =========================================================

    public void ClearNavMesh()
    {
        if (navMeshSurface == null)
            return;


        navMeshSurface.RemoveData();

        IsBuilt = false;
    }
}