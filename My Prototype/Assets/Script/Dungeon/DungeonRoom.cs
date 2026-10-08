using UnityEngine;

public enum RoomDirection
{
    North,
    South,
    East,
    West
}

public class DungeonRoom : MonoBehaviour
{
    [Header("Room Connections")]

    [Tooltip("북쪽(+Z)이 뚫려 있는가")]
    [SerializeField] private bool north;

    [Tooltip("남쪽(-Z)이 뚫려 있는가")]
    [SerializeField] private bool south;

    [Tooltip("동쪽(+X)이 뚫려 있는가")]
    [SerializeField] private bool east;

    [Tooltip("서쪽(-X)이 뚫려 있는가")]
    [SerializeField] private bool west;


    public bool North => north;
    public bool South => south;
    public bool East => east;
    public bool West => west;

    // 플레이어 생성 위치, Stage Exit 생성 위치를 지정할 수 있는 시스템

    [Header("Start / Exit Placement")]

    [Tooltip("Player 또는 Stage Exit가 생성될 위치")]
    [SerializeField]
    private Transform placementPoint;

    [Tooltip("이 Room을 시작/도착 지점 후보로 사용할 것인가")]
    [SerializeField]
    private bool allowStartEndPlacement = true;


    public Transform PlacementPoint =>
        placementPoint;

    public bool AllowStartEndPlacement =>
        allowStartEndPlacement;


    public Vector3 GetPlacementPosition()
    {
        if (placementPoint != null)
        {
            return placementPoint.position;
        }

        return transform.position;
    }


    public bool HasConnection(RoomDirection direction)
    {
        switch (direction)
        {
            case RoomDirection.North:
                return north;

            case RoomDirection.South:
                return south;

            case RoomDirection.East:
                return east;

            case RoomDirection.West:
                return west;
        }

        return false;
    }
}