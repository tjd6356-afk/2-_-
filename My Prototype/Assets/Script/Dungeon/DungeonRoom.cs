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