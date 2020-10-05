using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Globals
{
    public enum MoveDirection
    {
        None = -1,
        Up,
        Right,
        Down,
        Left,
    }
    
    // Up, Right, Down, Left moves
    public static readonly Vector2Int[] Moves =
        {new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(-1, 0), new Vector2Int(0, -1)};
}
