using UnityEngine;

public class DungeonRoom : MonoBehaviour
{
    [Header("Room Connections")]

    [Tooltip("북쪽(+Z)에 통로가 있는가")]
    [SerializeField]
    private bool north;

    [Tooltip("남쪽(-Z)에 통로가 있는가")]
    [SerializeField]
    private bool south;

    [Tooltip("동쪽(+X)에 통로가 있는가")]
    [SerializeField]
    private bool east;

    [Tooltip("서쪽(-X)에 통로가 있는가")]
    [SerializeField]
    private bool west;


    public bool North => north;
    public bool South => south;
    public bool East => east;
    public bool West => west;


    // =========================================================
    // 해당 방향에 문이 있는가?
    // =========================================================

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

    public enum RoomDirection // 룸 프리팹의 방향을 나타내는 상태
    {
        North,
        South,
        East,
        West
    }

}