using System.Collections.Generic;
using UnityEngine;
using static DungeonRoom;

public class DungeonGridGenerator : MonoBehaviour
{
    [Header("Grid Size")]
    [SerializeField]
    private int gridWidth = 5;

    [SerializeField]
    private int gridHeight = 5;


    [Header("Generation Rules")]

    [Tooltip("생성할 방의 총 개수")]
    [SerializeField]
    private int numberOfRoomsToGenerate = 10;

    [Tooltip("생성 시도 최대 횟수")]
    [SerializeField]
    private int maxGenerationAttempts = 500;


    [Header("Room Prefabs")]
    [Tooltip("DungeonRoom 컴포넌트가 붙어 있는 Room Prefab")]
    [SerializeField]
    private DungeonRoom[] roomDefinitions;


    [Header("Cell Size")]

    [SerializeField]
    private float cellSizeX = 30f;

    [SerializeField]
    private float cellSizeZ = 30f;


    [Header("Room Position")]

    [SerializeField]
    private float roomY = 0f;


    [Header("Generation")]

    [SerializeField]
    private bool centerGrid = true;


    [Header("Debug")]

    [SerializeField]
    private bool drawGrid = true;


    private Transform generatedRoomRoot;

    private readonly RoomDirection[] allDirections =
    {
    RoomDirection.North,
    RoomDirection.South,
    RoomDirection.East,
    RoomDirection.West
    };


    // =========================================================
    // 실제 생성된 방 정보
    // =========================================================

    private Dictionary<Vector2Int, DungeonRoom>
        generatedRooms =
            new Dictionary<Vector2Int, DungeonRoom>();


    // 상하좌우
    private readonly Vector2Int[] directions =
    {
        new Vector2Int(0, 1),   // North
        new Vector2Int(0, -1),  // South
        new Vector2Int(1, 0),   // East
        new Vector2Int(-1, 0)   // West
    };


    private void Start()
    {
        GenerateDungeon();
    }


    // =========================================================
    // Dungeon 생성
    // =========================================================

    public void GenerateDungeon()
    {
        // ==========================================
        // 기본 검사
        // ==========================================

        if (roomDefinitions == null ||
            roomDefinitions.Length == 0)
        {
            Debug.LogError(
                "[DungeonGenerator] Room Prefab이 없습니다."
            );

            return;
        }


        foreach (DungeonRoom room in roomDefinitions)
        {
            if (room == null)
            {
                Debug.LogError(
                    "[DungeonGenerator] Room Prefab 배열에 빈 값이 있습니다."
                );

                return;
            }
        }


        int maxGridCells =
            gridWidth *
            gridHeight;


        int targetRoomCount =
            Mathf.Clamp(
                numberOfRoomsToGenerate,
                1,
                maxGridCells
            );


        // ==========================================
        // 기존 Dungeon 제거
        // ==========================================

        ClearDungeon();


        generatedRooms.Clear();


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
        // 첫 번째 방
        //
        // Grid 중앙에서 시작
        // ==========================================

        Vector2Int startCell =
            new Vector2Int(
                gridWidth / 2,
                gridHeight / 2
            );


        DungeonRoom firstPrefab =
            roomDefinitions[
                Random.Range(
                    0,
                    roomDefinitions.Length
                )
            ];


        CreateRoom(
            startCell,
            firstPrefab
        );


        // ==========================================
        // 두 번째 방부터
        // 기존 방 옆으로 확장
        // ==========================================

        int attempts = 0;


        while (generatedRooms.Count <
               targetRoomCount &&
               attempts <
               maxGenerationAttempts)
        {
            attempts++;


            // 현재 방들 주변의 빈 칸 목록
            List<Vector2Int> frontier =
                GetAvailableAdjacentCells();


            // 더 이상 확장할 곳 없음
            if (frontier.Count == 0)
            {
                Debug.LogWarning(
                    "[DungeonGenerator] 더 이상 확장 가능한 Grid Cell이 없습니다."
                );

                break;
            }


            // 랜덤한 인접 칸 선택
            Vector2Int cell =
                frontier[
                    Random.Range(
                        0,
                        frontier.Count
                    )
                ];


            // 해당 위치와 연결 가능한 Room 검색
            List<DungeonRoom> validRooms =
                GetValidRoomsForCell(
                    cell
                );


            // 들어갈 수 있는 방이 없다면
            // 다른 위치를 다음 Loop에서 시도
            if (validRooms.Count == 0)
            {
                continue;
            }


            DungeonRoom selectedPrefab =
                validRooms[
                    Random.Range(
                        0,
                        validRooms.Count
                    )
                ];


            CreateRoom(
                cell,
                selectedPrefab
            );
        }


        Debug.Log(
            $"Dungeon 생성 완료 : " +
            $"{generatedRooms.Count}/{targetRoomCount} Rooms"
        );


        if (generatedRooms.Count <
            targetRoomCount)
        {
            Debug.LogWarning(
                $"목표는 {targetRoomCount}개였지만 " +
                $"{generatedRooms.Count}개만 생성되었습니다. " +
                "Room 통로 조합을 확인해주세요."
            );
        }
    }


    // =========================================================
    // 기존 Room에 붙어있는 빈 Cell 찾기
    // =========================================================

    private List<Vector2Int> GetAvailableAdjacentCells()
    {
        HashSet<Vector2Int> result =
            new HashSet<Vector2Int>();


        foreach (
            KeyValuePair<Vector2Int, DungeonRoom>
            pair in generatedRooms)
        {
            Vector2Int roomCell =
                pair.Key;

            DungeonRoom room =
                pair.Value;


            foreach (
                RoomDirection direction in allDirections)
            {
                // =====================================
                // 이 방향에 문이 없다면
                // 새 방 생성 후보조차 만들지 않는다.
                // =====================================

                if (!room.HasConnection(direction))
                    continue;


                Vector2Int nextCell =
                    roomCell +
                    DirectionToVector(direction);


                // Grid 밖
                if (!IsInsideGrid(nextCell))
                    continue;


                // 이미 방이 있음
                if (generatedRooms.ContainsKey(nextCell))
                    continue;


                result.Add(nextCell);
            }
        }


        return new List<Vector2Int>(
            result
        );
    }


    // =========================================================
    // 해당 Cell에 들어갈 수 있는 Room 검색
    // =========================================================

    private List<DungeonRoom>
        GetValidRoomsForCell(
            Vector2Int cell
        )
    {
        List<DungeonRoom> validRooms =
            new List<DungeonRoom>();


        foreach (
            DungeonRoom roomPrefab in roomDefinitions)
        {
            if (CanPlaceRoom(
                    cell,
                    roomPrefab))
            {
                validRooms.Add(
                    roomPrefab
                );
            }
        }


        return validRooms;
    }


    // =========================================================
    // Room 배치 가능 여부
    //
    // 핵심 연결 검사
    // =========================================================

    private bool CanPlaceRoom(
    Vector2Int cell,
    DungeonRoom candidate
)
    {
        bool connectedToAtLeastOneRoom =
            false;


        foreach (
            RoomDirection direction in allDirections)
        {
            Vector2Int neighborCell =
                cell +
                DirectionToVector(direction);


            // ==========================================
            // 이 방향에 기존 방이 없는 경우
            // ==========================================

            if (!generatedRooms.TryGetValue(
                    neighborCell,
                    out DungeonRoom neighborRoom))
            {
                continue;
            }


            /*
             * Candidate 기준 direction 방향에
             * Neighbor가 존재한다.
             *
             * 예:
             *
             * Candidate의 North
             * ↕
             * Neighbor의 South
             */


            bool candidateDoor =
                candidate.HasConnection(
                    direction
                );


            RoomDirection opposite =
                GetOppositeDirection(
                    direction
                );


            bool neighborDoor =
                neighborRoom.HasConnection(
                    opposite
                );


            // ==========================================
            // 핵심 조건
            //
            // 닿아 있다면
            // 두 방 모두 반드시 뚫려 있어야 함.
            // ==========================================

            if (!candidateDoor ||
                !neighborDoor)
            {
                return false;
            }


            // 양쪽 모두 통로가 있음
            connectedToAtLeastOneRoom =
                true;
        }


        // ==========================================
        // 최소 1개 기존 Room과 연결되어야 함
        // ==========================================

        return connectedToAtLeastOneRoom;
    }


    // =========================================================
    // Room 실제 생성
    // =========================================================

    private void CreateRoom(
        Vector2Int cell,
        DungeonRoom prefab
    )
    {
        Vector3 position =
            GetCellCenter(
                cell.x,
                cell.y
            );


        DungeonRoom room =
            Instantiate(
                prefab,
                position,
                prefab.transform.rotation,
                generatedRoomRoot
            );


        room.name =
            $"Room_{cell.x}_{cell.y}_{prefab.name}";


        generatedRooms.Add(
            cell,
            room
        );
    }


    // =========================================================
    // Grid 안인지 확인
    // =========================================================

    private bool IsInsideGrid(
        Vector2Int cell
    )
    {
        return
            cell.x >= 0 &&
            cell.x < gridWidth &&
            cell.y >= 0 &&
            cell.y < gridHeight;
    }


    // =========================================================
    // Cell 중앙
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
    // 기존 Dungeon 삭제
    // =========================================================

    public void ClearDungeon()
    {
        Transform oldRoot =
            transform.Find(
                "GeneratedRooms"
            );


        if (oldRoot == null)
            return;


        if (Application.isPlaying)
        {
            Destroy(
                oldRoot.gameObject
            );
        }
        else
        {
            DestroyImmediate(
                oldRoot.gameObject
            );
        }


        generatedRoomRoot =
            null;


        generatedRooms.Clear();
    }


    // =========================================================
    // Grid Gizmo
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

    // =========================================================
    // Direction → Grid 이동값
    // =========================================================

    private Vector2Int DirectionToVector(
        RoomDirection direction
    )
    {
        switch (direction)
        {
            case RoomDirection.North:
                return Vector2Int.up;

            case RoomDirection.South:
                return Vector2Int.down;

            case RoomDirection.East:
                return Vector2Int.right;

            case RoomDirection.West:
                return Vector2Int.left;
        }

        return Vector2Int.zero;
    }


    // =========================================================
    // 반대 방향
    // =========================================================

    private RoomDirection GetOppositeDirection(
        RoomDirection direction
    )
    {
        switch (direction)
        {
            case RoomDirection.North:
                return RoomDirection.South;

            case RoomDirection.South:
                return RoomDirection.North;

            case RoomDirection.East:
                return RoomDirection.West;

            case RoomDirection.West:
                return RoomDirection.East;
        }

        return RoomDirection.North;
    }

}