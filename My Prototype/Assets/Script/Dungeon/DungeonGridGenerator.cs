using System.Collections.Generic; // List 사용을 위해 추가
using UnityEngine;

public class DungeonGridGenerator : MonoBehaviour
{
    [Header("Grid Size")]
    [SerializeField]
    private int gridWidth = 5;

    [SerializeField]
    private int gridHeight = 5;

    [Header("Generation Rules")]
    [Tooltip("생성할 방의 총 개수 (그리드 최대치 초과 불가)")]
    [SerializeField]
    private int numberOfRoomsToGenerate = 10;

    [Header("Room Prefabs")]
    [Tooltip("생성 시 랜덤으로 선택될 방 프리팹 리스트")]
    [SerializeField]
    private GameObject[] roomPrefabs;

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
    [Tooltip("DungeonGenerator 위치를 전체 그리드의 중앙으로 사용할지")]
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
        // 1. 예외 처리: 프리팹 리스트가 비어있는지 확인
        if (roomPrefabs == null || roomPrefabs.Length == 0)
        {
            Debug.LogError("[DungeonGenerator] Room Prefab이 배열에 없습니다. 인스펙터를 확인해주세요.");
            return;
        }

        // 2. 예외 처리: 설정한 방 개수가 전체 그리드 칸 수보다 많은지 확인
        int maxGridCells = gridWidth * gridHeight;
        int roomsToCreate = numberOfRoomsToGenerate;

        if (roomsToCreate > maxGridCells)
        {
            Debug.LogWarning($"[DungeonGenerator] 생성하려는 방의 개수({roomsToCreate})가 최대 그리드 칸({maxGridCells})을 초과했습니다. 최대치로 조정합니다.");
            roomsToCreate = maxGridCells;
        }

        ClearDungeon();

        GameObject root = new GameObject("GeneratedRooms");
        root.transform.SetParent(transform);
        root.transform.localPosition = Vector3.zero;
        generatedRoomRoot = root.transform;

        // 3. 생성 가능한 모든 그리드 좌표(X, Z)를 리스트에 담기
        List<Vector2Int> availableCells = new List<Vector2Int>();
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                availableCells.Add(new Vector2Int(x, z));
            }
        }

        // 4. 리스트 순서 섞기 (랜덤 좌표 추출을 위함)
        for (int i = 0; i < availableCells.Count; i++)
        {
            int randomIndex = Random.Range(i, availableCells.Count);
            Vector2Int temp = availableCells[i];
            availableCells[i] = availableCells[randomIndex];
            availableCells[randomIndex] = temp;
        }

        // 5. 지정된 개수(roomsToCreate)만큼만 방 생성
        for (int i = 0; i < roomsToCreate; i++)
        {
            Vector2Int cell = availableCells[i];
            Vector3 position = GetCellCenter(cell.x, cell.y);

            // 등록된 프리팹 중 무작위로 하나 선택
            GameObject selectedPrefab = roomPrefabs[Random.Range(0, roomPrefabs.Length)];

            GameObject room = Instantiate(
                selectedPrefab,
                position,
                selectedPrefab.transform.rotation,
                generatedRoomRoot
            );

            room.name = $"Room_{cell.x}_{cell.y}";
        }

        Debug.Log($"Dungeon 생성 완료 : 전체 {maxGridCells}칸 중 {roomsToCreate}개의 Room 생성됨");
    }

    // =========================================================
    // 특정 Grid Cell의 중앙 위치
    // =========================================================
    private Vector3 GetCellCenter(int x, int z)
    {
        float xPosition;
        float zPosition;

        if (centerGrid)
        {
            float halfWidth = (gridWidth - 1) * cellSizeX * 0.5f;
            float halfHeight = (gridHeight - 1) * cellSizeZ * 0.5f;

            xPosition = x * cellSizeX - halfWidth;
            zPosition = z * cellSizeZ - halfHeight;
        }
        else
        {
            xPosition = x * cellSizeX;
            zPosition = z * cellSizeZ;
        }

        return transform.position + new Vector3(xPosition, roomY, zPosition);
    }

    // =========================================================
    // 생성된 Dungeon 삭제
    // =========================================================
    public void ClearDungeon()
    {
        Transform oldRoot = transform.Find("GeneratedRooms");
        if (oldRoot == null) return;

        // 에디터에서 실행할 때와 플레이 모드에서 실행할 때를 구분하여 안전하게 삭제
        if (Application.isPlaying)
            Destroy(oldRoot.gameObject);
        else
            DestroyImmediate(oldRoot.gameObject);

        generatedRoomRoot = null;
    }

    // =========================================================
    // Scene에서 Grid 확인
    // =========================================================
    private void OnDrawGizmos()
    {
        if (!drawGrid) return;
        if (gridWidth <= 0 || gridHeight <= 0) return;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                Vector3 center = GetCellCenter(x, z);

                Gizmos.DrawWireCube(
                    center,
                    new Vector3(cellSizeX, 0.1f, cellSizeZ)
                );
            }
        }
    }
}