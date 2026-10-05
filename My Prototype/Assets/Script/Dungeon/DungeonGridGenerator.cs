using UnityEngine;

public class DungeonGridGenerator : MonoBehaviour
{
    [Header("Grid Size")]
    [SerializeField]
    private int gridWidth = 5;

    [SerializeField]
    private int gridHeight = 5;


    [Header("Room")]
    [SerializeField]
    private GameObject roomPrefab;


    [Header("Cell Size")]
    [Tooltip("방 하나가 차지하는 X축 크기")]
    [SerializeField]
    private float cellSizeX = 20f;

    [Tooltip("방 하나가 차지하는 Z축 크기")]
    [SerializeField]
    private float cellSizeZ = 20f;


    [Header("Room Position")]
    [SerializeField]
    private float roomY = 0f;


    [Header("Generation")]
    [Tooltip("DungeonGenerator 위치를 전체 5x5 맵의 중앙으로 사용할지")]
    [SerializeField]
    private bool centerGrid = true;


    [Header("Debug")]
    [SerializeField]
    private bool drawGrid = true;


    private Transform generatedRoomRoot;


    private void Start()
    {
        GenerateDungeon();
    }


    // =========================================================
    // Dungeon 생성
    // =========================================================

    public void GenerateDungeon()
    {
        if (roomPrefab == null)
        {
            Debug.LogError(
                "[DungeonGenerator] Room Prefab이 없습니다."
            );

            return;
        }


        // 혹시 이전에 생성한 방이 있으면 제거
        ClearDungeon();


        // 생성된 Room들을 정리할 부모
        GameObject root =
            new GameObject(
                "GeneratedRooms"
            );


        root.transform.SetParent(
            transform
        );


        root.transform.localPosition =
            Vector3.zero;


        generatedRoomRoot =
            root.transform;


        // ==========================================
        // 5 × 5 생성
        // ==========================================

        for (int x = 0;
             x < gridWidth;
             x++)
        {
            for (int z = 0;
                 z < gridHeight;
                 z++)
            {
                Vector3 position =
                    GetCellCenter(
                        x,
                        z
                    );


                GameObject room =
                    Instantiate(
                        roomPrefab,
                        position,
                        roomPrefab.transform.rotation,
                        generatedRoomRoot
                    );


                room.name =
                    $"Room_{x}_{z}";
            }
        }


        Debug.Log(
            $"Dungeon 생성 완료 : {gridWidth} x {gridHeight} = " +
            $"{gridWidth * gridHeight} Rooms"
        );
    }


    // =========================================================
    // 특정 Grid Cell의 중앙 위치
    // =========================================================

    private Vector3 GetCellCenter(
        int x,
        int z
    )
    {
        float xPosition;
        float zPosition;


        if (centerGrid)
        {
            /*
             * DungeonGenerator 자체를
             * 전체 Grid의 중앙으로 사용한다.
             *
             * 5x5라면:
             *
             * -2 -1 0 1 2
             *
             * 형태로 배치됨.
             */

            float halfWidth =
                (gridWidth - 1) *
                cellSizeX *
                0.5f;


            float halfHeight =
                (gridHeight - 1) *
                cellSizeZ *
                0.5f;


            xPosition =
                x *
                cellSizeX -
                halfWidth;


            zPosition =
                z *
                cellSizeZ -
                halfHeight;
        }
        else
        {
            /*
             * DungeonGenerator를
             * 왼쪽 아래 시작점으로 사용
             */

            xPosition =
                x *
                cellSizeX;


            zPosition =
                z *
                cellSizeZ;
        }


        return
            transform.position +
            new Vector3(
                xPosition,
                roomY,
                zPosition
            );
    }


    // =========================================================
    // 생성된 Dungeon 삭제
    // =========================================================

    public void ClearDungeon()
    {
        Transform oldRoot =
            transform.Find(
                "GeneratedRooms"
            );


        if (oldRoot == null)
            return;


        Destroy(
            oldRoot.gameObject
        );


        generatedRoomRoot =
            null;
    }


    // =========================================================
    // Scene에서 Grid 확인
    // =========================================================

    private void OnDrawGizmos()
    {
        if (!drawGrid)
            return;


        if (gridWidth <= 0 ||
            gridHeight <= 0)
        {
            return;
        }


        for (int x = 0;
             x < gridWidth;
             x++)
        {
            for (int z = 0;
                 z < gridHeight;
                 z++)
            {
                Vector3 center =
                    GetCellCenter(
                        x,
                        z
                    );


                Gizmos.DrawWireCube(
                    center,
                    new Vector3(
                        cellSizeX,
                        0.1f,
                        cellSizeZ
                    )
                );
            }
        }
    }
}