using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class AgentPacmanCharacter : Agent
{
    public enum Type
    {
        Pacman = 0,
        Ghost = 1
    }

    [HideInInspector] public Type type;
    [HideInInspector] public float timePenalty;
    [HideInInspector] public CharacterController characterController;
    
    private float m_Existential;
    private int m_PlayerIndex;
    private BehaviorParameters m_BehaviorParameters;
    private LevelManager _levelManager;
    
    public override void Initialize()
    {
        m_Existential = 1f / MaxStep;
        m_BehaviorParameters = gameObject.GetComponent<BehaviorParameters>();
        characterController = gameObject.GetComponent<CharacterController>();
        _levelManager = GetComponentInParent<LevelManager>();
        type = (Type) m_BehaviorParameters.TeamId;

        var playerState = new PlayerState
        {
            startingPos = transform.position,
            agentScript = this,
        };
        _levelManager.playerStates.Add(playerState);
        m_PlayerIndex = _levelManager.playerStates.IndexOf(playerState);
        playerState.playerIndex = m_PlayerIndex;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Load map
        var tile = _levelManager.levelData.TileByLocalPosition(transform.localPosition);
        int startX = tile.x - 4;
        int endX = tile.x + 4;
        int startY = tile.y - 4;
        int endY = tile.y + 4;
        var tiles = _levelManager.levelData.levelTiles;
        for (int i = startX; i < endX; ++i)
        {
            for (int j = startY; j < endY; ++j)
            {
                if (i < 0 || i >= _levelManager.levelData.LevelHeight || j < 0 ||
                    j >= _levelManager.levelData.LevelWidth)
                {
                    sensor.AddObservation(0);
                }
                else if (!characterController.Passable(new Vector2Int(i, j)))
                {
                    sensor.AddObservation(0);
                }
                else if (type == Type.Pacman && new List<char> {'d', 'p'}.Contains(_levelManager.levelData.levelTiles[i, j]))
                {
                    sensor.AddObservation(2);
                }
                else
                {
                    sensor.AddObservation(1);
                }
            }
        }
        
        // First add position and direction of self
        sensor.AddObservation(transform.localPosition);
        sensor.AddObservation((int) characterController.agentMove);
        // Then add positions of other agents of same type. Ghosts should know where their buddies are.
        foreach (var ps in _levelManager.playerStates)
        {
            if (ps.playerIndex != m_PlayerIndex && ps.agentScript.type == type)
            {
                sensor.AddObservation(ps.agentScript.gameObject.transform.localPosition);
                sensor.AddObservation((int) ps.agentScript.characterController.agentMove);
            }
        }
        // Then add positions of enemy agents. 
        foreach (var ps in _levelManager.playerStates)
        {
            if (ps.playerIndex != m_PlayerIndex && ps.agentScript.type != type)
            {
                sensor.AddObservation(ps.agentScript.gameObject.transform.localPosition);
                sensor.AddObservation((int) ps.agentScript.characterController.agentMove);
            }
        }
        // Whether power pill is active or not
        sensor.AddObservation(_levelManager.levelData.powerPillActive);
        // if (type == Type.Pacman)
        // {
        //     sensor.AddObservation(_levelManager.GetClosestDot(false));
        //     sensor.AddObservation(_levelManager.GetClosestDot(true));
        // }
        // else
        // {
        //     // Ghosts don't need dot locations
        //     sensor.AddObservation(Vector2.zero);
        //     sensor.AddObservation(Vector2.zero);
        // }
    }

    public override void OnEpisodeBegin()
    {
        timePenalty = 0;
        _levelManager.GameStart();
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        MoveAgent(actionBuffers.DiscreteActions);
    }
    
    public void MoveAgent(ActionSegment<int> act)
    {
        var moveDir = (Globals.MoveDirection) (act[0] - 1);
        characterController.TryChangeDirection(moveDir);
    }
    
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        discreteActionsOut.Clear();
        if (Input.GetButton("Up"))
        {
            discreteActionsOut[0] = 1;
        }

        if (Input.GetButton("Right"))
        {
            discreteActionsOut[0] = 2;
        }

        if (Input.GetButton("Down"))
        {
            discreteActionsOut[0] = 3;
        }

        if (Input.GetButton("Left"))
        {
            discreteActionsOut[0] = 4;
        }
    }
}
