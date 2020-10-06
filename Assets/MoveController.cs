using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveController : MonoBehaviour
{
    public Globals.MoveDirection agentMove;
    public bool alive;
    public float moveSpeed = 20.0f;
    public Vector2Int currentTile;
    public AgentType agentType;

    private LevelManager _levelManager;
    private Animator _animator;
    private static readonly int IsMoving = Animator.StringToHash("IsMoving");

    public enum AgentType
    {
        PacMan,
        Ghost,
    }

    // Start is called before the first frame update
    void Start()
    {
        _levelManager = FindObjectOfType<LevelManager>();
        _animator = GetComponent<Animator>();
        agentMove = Globals.MoveDirection.None;
        alive = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (alive)
        {
            if (Input.GetButton("Up"))
            {
                TryChangeDirection(Globals.MoveDirection.Up);
            }

            if (Input.GetButton("Right"))
            {
                TryChangeDirection(Globals.MoveDirection.Right);
            }

            if (Input.GetButton("Down"))
            {
                TryChangeDirection(Globals.MoveDirection.Down);
            }

            if (Input.GetButton("Left"))
            {
                TryChangeDirection(Globals.MoveDirection.Left);
            }

            _animator.SetBool(IsMoving, agentMove != Globals.MoveDirection.None);
        }
    }

    public void TryChangeDirection(Globals.MoveDirection dir)
    {
        var move = Globals.Moves[(int) dir];
        if (agentMove != dir && Passable(currentTile + move))
        {
            // If current and target directions are opposite (Up & Down, Left & Right)
            if (Math.Abs(agentMove - dir) == 2)
            {
                ChangeDirection(dir);
            }
            else 
            {
                var localPosNext = _levelManager.levelData.LocalPositionByTile(currentTile + move);
                var localPosCurrent = _levelManager.levelData.LocalPositionByTile(currentTile);
                var agentToNext = (localPosNext - transform.localPosition).magnitude;
                var cellToCell = (localPosNext - localPosCurrent).magnitude;
                if (Math.Abs(agentToNext - cellToCell) < cellToCell / 16)
                {
                    // Place agent in the center of current tile
                    transform.localPosition = _levelManager.levelData.LocalPositionByTile(currentTile);
                    ChangeDirection(dir);
                }
            }
        }
    }

    private void ChangeDirection(Globals.MoveDirection dir)
    {
        agentMove = dir;
        transform.eulerAngles = new Vector3(0, 0, 90 - (int) dir * 90);
    }
    
    private bool Passable(Vector2Int pos)
    {
        char tile = _levelManager.levelData.levelTiles[pos.x, pos.y];
        switch (agentType)
        {
            case AgentType.PacMan:
                return new List<char>() {'W', 'B', 'G', 'D'}.Contains(tile) == false;
            // Only ghosts can walk inside ghost box and through doors
            case AgentType.Ghost:
                return new List<char>() {'W', 'B'}.Contains(tile) == false;
        }

        return false;
    }

    private void FixedUpdate()
    {
        if (alive && agentMove != Globals.MoveDirection.None)
        {
            var move = Globals.Moves[(int) agentMove];
            var localPosNext = _levelManager.levelData.LocalPositionByTile(currentTile + move);
            var localPosCurrent = _levelManager.levelData.LocalPositionByTile(currentTile);
            if ((localPosNext - transform.localPosition).magnitude < (localPosNext - localPosCurrent).magnitude)
            {
                // Stop if going in the wall
                if (!Passable(currentTile + move))
                {
                    transform.localPosition = _levelManager.levelData.LocalPositionByTile(currentTile);
                    agentMove = Globals.MoveDirection.None;
                }
            }

            if (agentMove != Globals.MoveDirection.None)
            {
                transform.localPosition += new Vector3(move.y, move.x, 0) * moveSpeed;
                currentTile = _levelManager.levelData.TileByLocalPosition(transform.localPosition);
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Dot") && agentType == AgentType.PacMan)
        {
            _levelManager.ConsumeDot();
            Destroy(other.gameObject);
        }
        if (other.gameObject.CompareTag("PowerPill") && agentType == AgentType.PacMan)
        {
            _levelManager.ConsumePowerPill();
            Destroy(other.gameObject);
        }
    }
}
