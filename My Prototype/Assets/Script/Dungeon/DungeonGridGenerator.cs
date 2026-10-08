using System.Collections.Generic;
using UnityEngine;

public class DungeonGridGenerator : MonoBehaviour
{
    [Header("Grid Size")]
    [SerializeField] private int gridWidth = 5;
    [SerializeField] private int gridHeight = 5;


    [Header("Generation Rules")]
    [Tooltip("우선 생성할 방 개수")]
    [SerializeField] private int numberOfRoomsToGenerate = 10;

    [Tooltip("무한 시도 방지")]
    [SerializeField] private int maxGenerationAttempts = 1000;


    [Header("Room Prefabs")]
    [Tooltip("DungeonRoom 컴포넌트가 붙어 있는 Prefab")]
    [SerializeField]
    private DungeonRoom[] roomDefinitions;


    [Header("Cell Size")]
    [SerializeField] private float cellSizeX = 30f;
    [SerializeField] private float cellSizeZ = 30f;


    [Header("Room Position")]
    [SerializeField] private float roomY = 0f;


    [Header("Generation")]
    [SerializeField] private bool centerGrid = true;


    [Header("Debug")]
    [SerializeField] private bool drawGrid = true;

    [Header("Closure Room Prefabs")]
    [Tooltip("생성 후 열린 입구를 막는 전용 프리팹들")]
    [SerializeField]
    private DungeonRoom[] closureRoomDefinitions;


    private Transform generatedRoomRoot;


    // =========================================================
    // 현재 생성된 방
    // =========================================================

    private readonly Dictionary<Vector2Int, DungeonRoom>
        generatedRooms =
            new Dictionary<Vector2Int, DungeonRoom>();


    // =========================================================
    // 반드시 이어야 하는 열린 통로들
    // =========================================================

    private readonly List<OpenDoor>
        openDoors =
            new List<OpenDoor>();


    private readonly RoomDirection[] allDirections =
    {
        RoomDirection.North,
        RoomDirection.South,
        RoomDirection.East,
        RoomDirection.West
    };


    // =========================================================
    // 열린 통로 정보
    // =========================================================

    private struct OpenDoor
    {
        public Vector2Int sourceCell;

        public RoomDirection direction;


        public OpenDoor(
            Vector2Int sourceCell,
            RoomDirection direction
        )
        {
            this.sourceCell =
                sourceCell;

            this.direction =
                direction;
        }
    }


    private void Start()
    {
        GenerateDungeon();
    }


    // =========================================================
    // Dungeon 생성
    // =========================================================

    public void GenerateDungeon()
    {
        // -----------------------------------------------------
        // 기본 검사
        // -----------------------------------------------------

        if (roomDefinitions == null ||
            roomDefinitions.Length == 0)
        {
            Debug.LogError(
                "[DungeonGenerator] Room Definitions가 비어있습니다."
            );

            return;
        }


        foreach (DungeonRoom room in roomDefinitions)
        {
            if (room == null)
            {
                Debug.LogError(
                    "[DungeonGenerator] Room Definitions에 None이 있습니다."
                );

                return;
            }
        }


        int maxCells =
            gridWidth *
            gridHeight;


        int targetCount =
            Mathf.Clamp(
                numberOfRoomsToGenerate,
                1,
                maxCells
            );


        // -----------------------------------------------------
        // 초기화
        // -----------------------------------------------------

        ClearDungeon();

        generatedRooms.Clear();

        openDoors.Clear();


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


        // =====================================================
        // 첫 번째 방
        // =====================================================

        Vector2Int startCell =
            new Vector2Int(
                gridWidth / 2,
                gridHeight / 2
            );


        DungeonRoom startPrefab =
            GetRandomValidStartRoom(
                startCell
            );


        if (startPrefab == null)
        {
            Debug.LogError(
                "[DungeonGenerator] 시작점에 배치 가능한 Room이 없습니다."
            );

            return;
        }


        DungeonRoom firstRoom =
            CreateRoom(
                startCell,
                startPrefab
            );


        // 첫 방의 모든 열린 통로 등록
        RegisterOpenDoors(
            startCell,
            firstRoom
        );


        // =====================================================
        // 열린 통로를 따라 방 확장
        // =====================================================

        int attempts = 0;


        while (generatedRooms.Count <
               targetCount &&
               openDoors.Count > 0 &&
               attempts <
               maxGenerationAttempts)
        {
            attempts++;


            int doorIndex =
                Random.Range(
                    0,
                    openDoors.Count
                );


            OpenDoor request =
                openDoors[doorIndex];


            // 이 요청은 이번에 처리
            openDoors.RemoveAt(
                doorIndex
            );


            Vector2Int targetCell =
                request.sourceCell +
                DirectionToVector(
                    request.direction
                );


            // Grid 밖
            if (!IsInsideGrid(
                    targetCell))
            {
                continue;
            }


            // =================================================
            // 이미 방이 있는 경우
            //
            // 양쪽이 연결되어 있는지만 확인
            // =================================================

            if (generatedRooms.TryGetValue(
                    targetCell,
                    out DungeonRoom existingRoom))
            {
                DungeonRoom sourceRoom =
                    generatedRooms[
                        request.sourceCell
                    ];


                RoomDirection opposite =
                    GetOppositeDirection(
                        request.direction
                    );


                if (!sourceRoom.HasConnection(
                        request.direction) ||
                    !existingRoom.HasConnection(
                        opposite))
                {
                    Debug.LogError(
                        $"[Dungeon] 잘못된 연결 발견 : " +
                        $"{request.sourceCell} → {targetCell}"
                    );
                }


                continue;
            }


            // =================================================
            // 이 통로에 연결될 수 있는 방 검색
            // =================================================

            List<DungeonRoom> candidates =
                GetValidRoomsForDoor(
                    targetCell,
                    request.direction
                );


            if (candidates.Count == 0)
            {
                Debug.LogWarning(
                    $"[Dungeon] {targetCell}에 " +
                    $"{request.direction} 방향과 연결 가능한 Room이 없습니다."
                );

                continue;
            }


            DungeonRoom selectedPrefab =
                candidates[
                    Random.Range(
                        0,
                        candidates.Count
                    )
                ];


            DungeonRoom newRoom =
                CreateRoom(
                    targetCell,
                    selectedPrefab
                );


            // =================================================
            // 새 Room의 모든 출구를
            // 다음 생성 요청으로 등록
            // =================================================

            RegisterOpenDoors(
                targetCell,
                newRoom
            );
        }


        // =====================================================
        // 메인 Dungeon 생성 완료
        // 남아있는 열린 입구 마감
        // =====================================================

        FillRemainingOpenings();

        Debug.Log(
            $"Dungeon 생성 완료 : " +
            $"{generatedRooms.Count}/{targetCount} Rooms"
        );
    }


    // =========================================================
    // 특정 출구와 연결 가능한 Room들
    // =========================================================

    private List<DungeonRoom> GetValidRoomsForDoor(
        Vector2Int targetCell,
        RoomDirection sourceDirection
    )
    {
        List<DungeonRoom> result =
            new List<DungeonRoom>();


        /*
         * 기존 방 East에서 넘어왔다면
         * 새 방은 West가 필요하다.
         */
        RoomDirection requiredDirection =
            GetOppositeDirection(
                sourceDirection
            );


        foreach (DungeonRoom candidate in roomDefinitions)
        {
            // =============================================
            // 1. 들어오는 방향이 반드시 열려 있어야 함
            // =============================================

            if (!candidate.HasConnection(
                    requiredDirection))
            {
                continue;
            }


            // =============================================
            // 2. 주변에 이미 존재하는 모든 Room과
            //    문 ↔ 문이어야 함
            // =============================================

            if (!MatchesAllExistingNeighbors(
                    targetCell,
                    candidate))
            {
                continue;
            }


            // =============================================
            // 3. Candidate의 열린 문이
            //    Grid 바깥을 향하면 배치하지 않음
            // =============================================

            if (HasDoorOutsideGrid(
                    targetCell,
                    candidate))
            {
                continue;
            }


            result.Add(
                candidate
            );
        }


        return result;
    }


    // =========================================================
    // 핵심 검사
    //
    // Candidate와 맞닿은 기존 방은
    // 무조건 양쪽 모두 문이 있어야 한다.
    // =========================================================

    private bool MatchesAllExistingNeighbors(
        Vector2Int cell,
        DungeonRoom candidate
    )
    {
        foreach (
            RoomDirection direction in allDirections)
        {
            Vector2Int neighborCell =
                cell +
                DirectionToVector(
                    direction
                );


            if (!generatedRooms.TryGetValue(
                    neighborCell,
                    out DungeonRoom neighbor))
            {
                continue;
            }


            bool candidateOpen =
                candidate.HasConnection(
                    direction
                );


            bool neighborOpen =
                neighbor.HasConnection(
                    GetOppositeDirection(
                        direction
                    )
                );


            // ==========================================
            // 닿아있는 방이라면
            // 양쪽 모두 반드시 열림
            // ==========================================

            if (!candidateOpen ||
                !neighborOpen)
            {
                return false;
            }
        }


        return true;
    }


    // =========================================================
    // Room의 열린 방향을 모두 다음 생성 요청에 등록
    // =========================================================

    private void RegisterOpenDoors(
        Vector2Int cell,
        DungeonRoom room
    )
    {
        foreach (
            RoomDirection direction in allDirections)
        {
            if (!room.HasConnection(
                    direction))
            {
                continue;
            }


            Vector2Int targetCell =
                cell +
                DirectionToVector(
                    direction
                );


            // Grid 바깥쪽 문은 등록하지 않음
            if (!IsInsideGrid(
                    targetCell))
            {
                continue;
            }


            // 이미 방이 있다면
            // 연결 상태 검사만 하면 됨
            if (generatedRooms.TryGetValue(
                    targetCell,
                    out DungeonRoom neighbor))
            {
                bool neighborOpen =
                    neighbor.HasConnection(
                        GetOppositeDirection(
                            direction
                        )
                    );


                if (!neighborOpen)
                {
                    Debug.LogError(
                        $"[Dungeon] 문 ↔ 벽 충돌 : " +
                        $"{cell} / {direction}"
                    );
                }


                continue;
            }


            // 같은 출구를 중복 등록하지 않는다.
            if (!ContainsOpenDoor(
                    cell,
                    direction))
            {
                openDoors.Add(
                    new OpenDoor(
                        cell,
                        direction
                    )
                );
            }
        }
    }


    // =========================================================
    // 중복 OpenDoor 확인
    // =========================================================

    private bool ContainsOpenDoor(
        Vector2Int sourceCell,
        RoomDirection direction
    )
    {
        foreach (
            OpenDoor door in openDoors)
        {
            if (door.sourceCell ==
                    sourceCell &&
                door.direction ==
                    direction)
            {
                return true;
            }
        }


        return false;
    }


    // =========================================================
    // Grid 바깥을 향하는 문 검사
    // =========================================================

    private bool HasDoorOutsideGrid(
        Vector2Int cell,
        DungeonRoom room
    )
    {
        foreach (
            RoomDirection direction in allDirections)
        {
            if (!room.HasConnection(
                    direction))
            {
                continue;
            }


            Vector2Int next =
                cell +
                DirectionToVector(
                    direction
                );


            if (!IsInsideGrid(next))
            {
                return true;
            }
        }


        return false;
    }


    // =========================================================
    // 시작 방
    // =========================================================

    private DungeonRoom GetRandomValidStartRoom(
        Vector2Int startCell
    )
    {
        List<DungeonRoom> candidates =
            new List<DungeonRoom>();


        foreach (DungeonRoom room in roomDefinitions)
        {
            if (!HasDoorOutsideGrid(
                    startCell,
                    room))
            {
                candidates.Add(
                    room
                );
            }
        }


        if (candidates.Count == 0)
            return null;


        return candidates[
            Random.Range(
                0,
                candidates.Count
            )
        ];
    }


    // =========================================================
    // 실제 Room 생성
    // =========================================================

    private DungeonRoom CreateRoom(
        Vector2Int cell,
        DungeonRoom prefab
    )
    {
        Vector3 worldPosition =
            GetCellCenter(
                cell.x,
                cell.y
            );


        DungeonRoom room =
            Instantiate(
                prefab,
                worldPosition,
                prefab.transform.rotation,
                generatedRoomRoot
            );


        room.name =
            $"Room_{cell.x}_{cell.y}_{prefab.name}";


        generatedRooms.Add(
            cell,
            room
        );


        return room;
    }


    // =========================================================
    // 방향 → Grid
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


    // =========================================================
    // Grid 내부 검사
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
    // Cell World Position
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
    // 삭제
    // =========================================================

    public void ClearDungeon()
    {
        Transform oldRoot =
            transform.Find(
                "GeneratedRooms"
            );


        if (oldRoot != null)
        {
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
        }


        generatedRooms.Clear();

        openDoors.Clear();

        generatedRoomRoot =
            null;
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

    private void GetRequiredOpeningsForEmptyCell(
    Vector2Int cell,
    out bool north,
    out bool south,
    out bool east,
    out bool west
)
    {
        north = false;
        south = false;
        east = false;
        west = false;

        // 위쪽에 방이 있고, 그 방이 South로 열려 있으면
        // 현재 빈 칸은 North가 필요
        if (generatedRooms.TryGetValue(cell + Vector2Int.up, out DungeonRoom northNeighbor))
        {
            if (northNeighbor.South)
                north = true;
        }

        // 아래쪽에 방이 있고, 그 방이 North로 열려 있으면
        // 현재 빈 칸은 South가 필요
        if (generatedRooms.TryGetValue(cell + Vector2Int.down, out DungeonRoom southNeighbor))
        {
            if (southNeighbor.North)
                south = true;
        }

        // 오른쪽에 방이 있고, 그 방이 West로 열려 있으면
        // 현재 빈 칸은 East가 필요
        if (generatedRooms.TryGetValue(cell + Vector2Int.right, out DungeonRoom eastNeighbor))
        {
            if (eastNeighbor.West)
                east = true;
        }

        // 왼쪽에 방이 있고, 그 방이 East로 열려 있으면
        // 현재 빈 칸은 West가 필요
        if (generatedRooms.TryGetValue(cell + Vector2Int.left, out DungeonRoom westNeighbor))
        {
            if (westNeighbor.East)
                west = true;
        }
    }

    private DungeonRoom FindExactClosureRoom(
    bool north,
    bool south,
    bool east,
    bool west
)
    {
        if (closureRoomDefinitions == null ||
            closureRoomDefinitions.Length == 0)
        {
            return null;
        }

        List<DungeonRoom> matches =
            new List<DungeonRoom>();

        foreach (DungeonRoom room in closureRoomDefinitions)
        {
            if (room == null)
                continue;

            if (room.North == north &&
                room.South == south &&
                room.East == east &&
                room.West == west)
            {
                matches.Add(room);
            }
        }

        if (matches.Count == 0)
            return null;

        return matches[Random.Range(0, matches.Count)];
    }

    private int CountOpenings(
    bool north,
    bool south,
    bool east,
    bool west
)
    {
        int count = 0;

        if (north) count++;
        if (south) count++;
        if (east) count++;
        if (west) count++;

        return count;
    }

    private void FillRemainingOpenSpaces()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                Vector2Int cell =
                    new Vector2Int(x, z);

                // 이미 방이 있으면 넘어감
                if (generatedRooms.ContainsKey(cell))
                    continue;

                GetRequiredOpeningsForEmptyCell(
                    cell,
                    out bool north,
                    out bool south,
                    out bool east,
                    out bool west
                );

                int openingCount =
                    CountOpenings(
                        north,
                        south,
                        east,
                        west
                    );

                // 주변에서 요구하는 연결이 없으면 패스
                if (openingCount == 0)
                    continue;

                bool shouldFill = false;

                // 1개 열림 -> 막힌 방 배치
                if (openingCount == 1)
                {
                    shouldFill = true;
                }
                // 2개 열림인데 서로 마주보는 경우만 직선 통로 배치
                else if (openingCount == 2)
                {
                    bool eastWest =
                        east && west &&
                        !north && !south;

                    bool northSouth =
                        north && south &&
                        !east && !west;

                    if (eastWest || northSouth)
                    {
                        shouldFill = true;
                    }
                }

                if (!shouldFill)
                    continue;

                DungeonRoom closurePrefab =
                    FindExactClosureRoom(
                        north,
                        south,
                        east,
                        west
                    );

                if (closurePrefab == null)
                {
                    Debug.LogWarning(
                        $"[Dungeon] 마감용 프리팹 없음 : " +
                        $"Cell={cell}, N={north}, S={south}, E={east}, W={west}"
                    );

                    continue;
                }

                CreateRoom(cell, closurePrefab);
            }
        }
    }
    private struct ClosureRequest
    {
        public Vector2Int cell;

        public bool north;
        public bool south;
        public bool east;
        public bool west;


        public ClosureRequest(
            Vector2Int cell,
            bool north,
            bool south,
            bool east,
            bool west
        )
        {
            this.cell = cell;
            this.north = north;
            this.south = south;
            this.east = east;
            this.west = west;
        }
    }

    private void GetRequiredOpenings(
    Vector2Int cell,
    out bool north,
    out bool south,
    out bool east,
    out bool west
)
    {
        north = false;
        south = false;
        east = false;
        west = false;


        // ==========================================
        // 북쪽에 있는 Room
        //
        // 그 Room의 South가 열려있다면
        // 현재 Cell은 North가 열려야 함
        // ==========================================

        Vector2Int northCell =
            cell + Vector2Int.up;


        if (generatedRooms.TryGetValue(
                northCell,
                out DungeonRoom northRoom))
        {
            if (northRoom.South)
            {
                north = true;
            }
        }


        // ==========================================
        // 남쪽 Room
        // ==========================================

        Vector2Int southCell =
            cell + Vector2Int.down;


        if (generatedRooms.TryGetValue(
                southCell,
                out DungeonRoom southRoom))
        {
            if (southRoom.North)
            {
                south = true;
            }
        }


        // ==========================================
        // 동쪽 Room
        // ==========================================

        Vector2Int eastCell =
            cell + Vector2Int.right;


        if (generatedRooms.TryGetValue(
                eastCell,
                out DungeonRoom eastRoom))
        {
            if (eastRoom.West)
            {
                east = true;
            }
        }


        // ==========================================
        // 서쪽 Room
        // ==========================================

        Vector2Int westCell =
            cell + Vector2Int.left;


        if (generatedRooms.TryGetValue(
                westCell,
                out DungeonRoom westRoom))
        {
            if (westRoom.East)
            {
                west = true;
            }
        }
    }

    private int CountRequiredOpenings(
    bool north,
    bool south,
    bool east,
    bool west
)
    {
        int count = 0;


        if (north)
            count++;

        if (south)
            count++;

        if (east)
            count++;

        if (west)
            count++;


        return count;
    }
    private DungeonRoom FindClosureRoom(
    bool north,
    bool south,
    bool east,
    bool west
)
    {
        if (closureRoomDefinitions == null)
            return null;


        List<DungeonRoom> matches =
            new List<DungeonRoom>();


        foreach (
            DungeonRoom room in closureRoomDefinitions)
        {
            if (room == null)
                continue;


            // 정확히 같은 출구 조합만 인정
            if (room.North == north &&
                room.South == south &&
                room.East == east &&
                room.West == west)
            {
                matches.Add(
                    room
                );
            }
        }


        if (matches.Count == 0)
            return null;


        return matches[
            Random.Range(
                0,
                matches.Count
            )
        ];
    }

    private void FillRemainingOpenings()
    {
        List<ClosureRequest> requests =
            new List<ClosureRequest>();


        // =========================================================
        // 1단계
        // 모든 빈 Cell 조사
        // =========================================================

        for (int x = 0;
             x < gridWidth;
             x++)
        {
            for (int z = 0;
                 z < gridHeight;
                 z++)
            {
                Vector2Int cell =
                    new Vector2Int(
                        x,
                        z
                    );


                // 이미 방이 있으면 무시
                if (generatedRooms.ContainsKey(
                        cell))
                {
                    continue;
                }


                // 주변 방들이 현재 빈 Cell에
                // 어떤 방향의 연결을 요구하는지 조사
                GetRequiredOpenings(
                    cell,
                    out bool north,
                    out bool south,
                    out bool east,
                    out bool west
                );


                int openingCount =
                    CountRequiredOpenings(
                        north,
                        south,
                        east,
                        west
                    );


                // 주변에 열린 통로가 하나도 없음
                if (openingCount == 0)
                {
                    continue;
                }


                // =================================================
                // 이제 방향 개수에 제한을 두지 않는다.
                //
                // 1방향
                // 2방향
                // 3방향
                // 4방향
                //
                // 전부 처리 가능
                // =================================================

                requests.Add(
                    new ClosureRequest(
                        cell,
                        north,
                        south,
                        east,
                        west
                    )
                );
            }
        }


        // =========================================================
        // 2단계
        // 필요한 모양과 정확히 같은 Closure Room 찾기
        // =========================================================

        int createdCount = 0;


        foreach (
            ClosureRequest request in requests)
        {
            if (generatedRooms.ContainsKey(
                    request.cell))
            {
                continue;
            }


            DungeonRoom closurePrefab =
                FindClosureRoom(
                    request.north,
                    request.south,
                    request.east,
                    request.west
                );


            // =====================================================
            // 맞는 프리팹이 없는 경우
            // =====================================================

            if (closurePrefab == null)
            {
                Debug.LogWarning(
                    $"[Dungeon Closure] 필요한 마감 Room이 없습니다.\n" +
                    $"Cell : {request.cell}\n" +
                    $"North : {request.north}\n" +
                    $"South : {request.south}\n" +
                    $"East : {request.east}\n" +
                    $"West : {request.west}"
                );


                continue;
            }


            DungeonRoom room =
                CreateRoom(
                    request.cell,
                    closurePrefab
                );


            room.name =
                $"Closure_{request.cell.x}_{request.cell.y}_{closurePrefab.name}";


            createdCount++;
        }


        Debug.Log(
            $"[Dungeon Closure] 후처리 완료 : {createdCount}개 Room 생성"
        );
    }
}