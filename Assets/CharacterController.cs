using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterController : MonoBehaviour
{
    public Globals.MoveDirection agentMove;
    public bool alive = false;
    public float moveSpeed = 20.0f;
    public float aliveDelay = 0.0f;
    public Vector2Int currentTile;
    public Vector2Int spawnTile;
    public AgentType agentType;
    [SerializeField] public Sprite ghostNormal;
    [SerializeField] public Sprite ghostScared;
    
    private LevelManager _levelManager;
    private Animator _animator;
    private static readonly int IsMoving = Animator.StringToHash("IsMoving");
    private AgentPacmanCharacter _agent;

    public enum AgentType
    {
        Pacman,
        Ghost,
    }

    // Start is called before the first frame update
    void Awake()
    {
        _levelManager = GetComponentInParent<LevelManager>();
        _animator = GetComponent<Animator>();
        _agent = GetComponent<AgentPacmanCharacter>();
        agentMove = Globals.MoveDirection.None;
        if (agentType == AgentType.Pacman)
        {
            alive = true;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (alive)
        {
            if (agentType == AgentType.Pacman)
            {
                _animator.SetBool(IsMoving, agentMove != Globals.MoveDirection.None);
            }
        }
    }

    public void Die()
    {
        alive = false;
        _agent.SetReward(-1.0f);
        if (agentType == AgentType.Pacman)
        {
            _levelManager.livesLeft--;
            if (_levelManager.livesLeft >= 0)
            {
                RespawnAgent();
            }
        }

        if (agentType == AgentType.Ghost)
        {
            RespawnAgent();
        }
    }

    public void TryChangeDirection(Globals.MoveDirection dir)
    {
        if (dir == Globals.MoveDirection.None)
        {
            return;
        }
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
        if (agentType == AgentType.Pacman)
        {
            transform.eulerAngles = new Vector3(0, 0, 90 - (int) dir * 90);
        }
    }
    
    public bool Passable(Vector2Int pos)
    {
        char tile = _levelManager.levelData.levelTiles[pos.x, pos.y];
        switch (agentType)
        {
            case AgentType.Pacman:
                return new List<char>() {'W', 'B', 'G', 'D'}.Contains(tile) == false;
            // Only ghosts can walk through ghost box doors
            case AgentType.Ghost:
                return new List<char>() {'W', 'B', 'G'}.Contains(tile) == false;
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
                float currentSpeed = moveSpeed;
                if (agentType == AgentType.Ghost && _levelManager.levelData.powerPillActive)
                {
                    currentSpeed /= 2;
                }
                transform.localPosition += new Vector3(move.y, move.x, 0) * currentSpeed;
                currentTile = _levelManager.levelData.TileByLocalPosition(transform.localPosition);
            }
        }
        if (aliveDelay > 1e-9)
        {
            aliveDelay -= Time.deltaTime;
        }
        if (aliveDelay < 1e-9)
        {
            aliveDelay = 0.0f;
            alive = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Dot") && agentType == AgentType.Pacman)
        {
            _levelManager.ConsumeDot(other.gameObject);
            Destroy(other.gameObject);
        }
        
        if (other.gameObject.CompareTag("PowerPill") && agentType == AgentType.Pacman)
        {
            _levelManager.ConsumePowerPill(other.gameObject);
            Destroy(other.gameObject);
        }

        if (other.gameObject.CompareTag("Ghost") && agentType == AgentType.Pacman)
        {
            var otherController = other.gameObject.GetComponent<CharacterController>();
            if (_levelManager.levelData.powerPillActive && otherController.alive)
            {
                other.gameObject.GetComponent<CharacterController>().Die();
                _levelManager.score += 100;
            }
            else if (alive)
            {
                Die();
            }
        }
    }

    public void RespawnAgent()
    {
        currentTile = spawnTile;
        transform.localPosition = _levelManager.levelData.LocalPositionByTile(spawnTile);
    }
}
