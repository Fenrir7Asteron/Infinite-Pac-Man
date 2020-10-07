using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class PlayerState
{
    public int playerIndex;
    public Vector3 startingPos;
    public AgentPacmanCharacter agentScript;
}

public class LevelManager : MonoBehaviour
{
    [Header("Game Parameters")]
    public int livesMax = 2;
    public int powerPillMax = 4;
    public float powerPillDuration = 5.0f;
    public float ghostDelay = 5.0f;
    public List<PlayerState> playerStates = new List<PlayerState>();
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI gameOverText;
    [SerializeField] private GameObject fade;
    [SerializeField] private GameObject next;
    [SerializeField] private GameObject retry;
    [Header("Prefabs")]
    [SerializeField] private GameObject pacman;
    [SerializeField] private GameObject[] ghosts;
    [SerializeField] private GameObject dot;
    [SerializeField] private GameObject powerPill;
    
    [HideInInspector] public int score;
    [HideInInspector] public int livesLeft = 2;
    [HideInInspector] public LevelData levelData;
    
    private LevelGenerator _levelGenerator;
    private int _dotsMax;
    private float _powerPillRemainTime;
    private GameObject _pacmanInstance;
    private List<GameObject> _ghostInstances;
    private bool _playing;
    private bool _tryAgain;

    public Vector2 GetClosestDot(bool isPowerPill = false)
    {
        return Bfs(_pacmanInstance.GetComponent<CharacterController>().currentTile, isPowerPill);
        // int minDist = -1;
        // Vector2 closestDot = Vector2.zero;
        // foreach (var dot in levelData.dotInstances)
        // {
        //     if (isPowerPill && !dot.name.Contains("Power"))
        //     {
        //         continue;
        //     }
        //     var tile = levelData.TileByLocalPosition(dot.transform.localPosition);
        //     if (minDist == -1 || minDist > distances[tile.x, tile.y])
        //     {
        //         minDist = distances[tile.x, tile.y];
        //         closestDot = tile;
        //     }
        // }
        //
        // return closestDot;
    }

    private Vector2Int Bfs(Vector2Int startTile, bool isPowerPill)
    {
        bool[,] visited = new bool[levelData.LevelHeight, levelData.LevelWidth];
        Queue<Vector3Int> tiles = new Queue<Vector3Int>();
        tiles.Enqueue(new Vector3Int(startTile.x, startTile.y, 0));
        while (tiles.Count > 0)
        {
            var tileDist = tiles.Dequeue();
            var tile = new Vector2Int(tileDist.x, tileDist.y);
            if (!isPowerPill && levelData.levelTiles[tile.x, tile.y] == 'd')
            {
                return tile;
            }
            if (isPowerPill && levelData.levelTiles[tile.x, tile.y] == 'p')
            {
                return tile;
            }
            var dist = tileDist.z;
            visited[tile.x, tile.y] = true;
            foreach (var move in Globals.Moves)
            {
                var nextTile = tile + move;
                if (!visited[nextTile.x, nextTile.y] && levelData.Free(nextTile))
                {
                    tiles.Enqueue(new Vector3Int(nextTile.x, nextTile.y, dist + 1));
                }
            }
        }
        
        return Vector2Int.zero;
    }

    public void OnClickNext()
    {
        _tryAgain = false;
        next.SetActive(false);
        fade.SetActive(false);
        gameOverText.enabled = false;
        foreach (var ps in playerStates)
        {
            if (ps.agentScript.type == AgentPacmanCharacter.Type.Pacman)
            {
                ps.agentScript.SetReward(1.0f);
            }
            ps.agentScript.EndEpisode();  //all agents need to be reset
        }
    }
    
    public void OnClickRetry()
    {
        _tryAgain = true;
        retry.SetActive(false);
        fade.SetActive(false);
        gameOverText.enabled = false;
        foreach (var ps in playerStates)
        {
            if (ps.agentScript.type == AgentPacmanCharacter.Type.Ghost)
            {
                ps.agentScript.SetReward(1.0f);
            }
            ps.agentScript.EndEpisode();  //all agents need to be reset
        }
    }

    public void GameOver(AgentPacmanCharacter.Type whoWon)
    {
        _playing = false;
        fade.SetActive(true);
        if (whoWon == AgentPacmanCharacter.Type.Pacman)
        {
            gameOverText.text = "Level Complete!";
            gameOverText.enabled = true;
            next.SetActive(true);
        }
        else
        {
            gameOverText.text = "Game Over";
            gameOverText.enabled = true;
            retry.SetActive(true);
        }
    }

    public void GameStart()
    {
        if (_tryAgain)
        {
            score = 0;
        }
        livesLeft = livesMax;
        LevelStart();
    }
    public void LevelStart()
    {
        Debug.Log("Level Start!");
        Reset();
        // levelData.PrintLevel();
        if (!_tryAgain)
        {
            _levelGenerator.GenerateLevel();
        }
        var freeTiles = GetFreeTiles();
        SpawnPacMan(freeTiles);
        SpawnGhosts();
        SpawnConsumables(freeTiles);
        _playing = true;
    }
    
    public void ConsumeDot(GameObject consumedDot)
    {
        var tile = levelData.TileByLocalPosition(consumedDot.transform.localPosition);
        levelData.levelTiles[tile.x, tile.y] = '.';
        levelData.dotsRemain--;
        score += 10;
        foreach (var ps in playerStates)
        {
            if (ps.agentScript.type == AgentPacmanCharacter.Type.Pacman)
            {
                ps.agentScript.AddReward(1.0f / _dotsMax);
            }
        }
    }
    
    public void ConsumePowerPill(GameObject consumedDot)
    {
        ConsumeDot(consumedDot);
        score += 90;
        levelData.powerPillActive = true;
        _powerPillRemainTime = powerPillDuration;
        foreach (var ghost in _ghostInstances)
        {
            Debug.Log(ghost.GetComponent<CharacterController>().ghostScared);
            ghost.GetComponent<SpriteRenderer>().sprite = ghost.GetComponent<CharacterController>().ghostScared;
        }
    }

    private List<Vector2Int> GetFreeTiles()
    {
        List<Vector2Int> freeTiles = new List<Vector2Int>();
        for (int i = 0; i < levelData.LevelHeight; ++i)
        {
            for (int j = 0; j < levelData.LevelWidth; ++j)
            {
                Vector2Int tile = new Vector2Int(i, j);
                if (levelData.Free(tile))
                {
                    freeTiles.Add(tile);
                }
            }
        }

        return freeTiles;
    }
    
    private List<Vector2Int> GetGhostTiles()
    {
        List<Vector2Int> ghostTiles = new List<Vector2Int>();
        int startX = levelData.LevelHeight / 2 - _levelGenerator.ghostBoxHeight / 2 + 1;
        int startY = levelData.LevelWidth / 2 - _levelGenerator.ghostBoxWidth / 2 + 1;
        int endX = levelData.LevelHeight / 2 + _levelGenerator.ghostBoxHeight / 2 - 1;
        int endY = levelData.LevelWidth / 2 + _levelGenerator.ghostBoxWidth / 2 - 1;
        for (int i = startX; i < endX; ++i)
        {
            for (int j = startY; j < endY; ++j)
            {
                Vector2Int tile = new Vector2Int(i, j);
                ghostTiles.Add(tile);
            }
        }

        return ghostTiles;
    }

    private void SpawnConsumables(List<Vector2Int> freeTiles)
    {
        List<int> shuffledIdx = Utils.RandomPermutation(0, freeTiles.Count);
        int idx = 0;
        while (idx < powerPillMax)
        {
            Vector2Int tile = freeTiles[shuffledIdx[idx]];
            var _powerPill = Instantiate(powerPill, transform);
            _powerPill.transform.localPosition = levelData.LocalPositionByTile(tile);
            _powerPill.transform.SetAsFirstSibling();
            levelData.dotsRemain++;
            _dotsMax++;
            levelData.levelTiles[tile.x, tile.y] = 'p';
            idx++;
        }
        while (idx < shuffledIdx.Count)
        {
            Vector2Int tile = freeTiles[shuffledIdx[idx]];
            var _dot = Instantiate(dot, transform);
            _dot.transform.localPosition = levelData.LocalPositionByTile(tile);
            _dot.transform.SetAsFirstSibling();
            levelData.dotsRemain++;
            _dotsMax++;
            levelData.levelTiles[tile.x, tile.y] = 'd';
            idx++;
        }
    }

    private void SpawnPacMan(List<Vector2Int> freeTiles)
    {
        List<int> shuffledIdx = Utils.RandomPermutation(0, freeTiles.Count);
        foreach (var idx in shuffledIdx)
        {
            Vector2Int tile = freeTiles[idx];
            if (ReferenceEquals(_pacmanInstance, null))
            {
                _pacmanInstance = Instantiate(pacman, transform);
            }
            _pacmanInstance.transform.localPosition = levelData.LocalPositionByTile(tile);
            var controller = _pacmanInstance.GetComponent<CharacterController>();
            controller.currentTile = tile;
            controller.spawnTile = tile;
            freeTiles.Remove(tile); // Pacman spawn place is not free. We can not spawn anything else there.
            Debug.Log("Pacman spawned ");
            return;
        }
    }
    
    private void SpawnGhosts()
    {
        var freeTiles = GetGhostTiles();
        List<int> shuffledIdx = Utils.RandomPermutation(0, freeTiles.Count);
        int idx = 0;
        while (idx < ghosts.Length)
        {
            Vector2Int tile = freeTiles[shuffledIdx[idx]];
            GameObject ghost;
            if (_ghostInstances.Count > idx)
            {
                ghost = _ghostInstances[idx];
            }
            else
            {
                ghost = Instantiate(ghosts[idx], transform);
                _ghostInstances.Add(ghost);
            }
            ghost.transform.localPosition = levelData.LocalPositionByTile(tile);
            var controller = ghost.GetComponent<CharacterController>();
            controller.currentTile = tile;
            controller.spawnTile = tile;
            controller.aliveDelay = ghostDelay * idx;
            controller.alive = false;
            Debug.Log("Ghost spawned " + ghost.name);
            idx++;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        score = 0;
        _tryAgain = false; 
        _levelGenerator = gameObject.GetComponent<LevelGenerator>();
        _ghostInstances = new List<GameObject>();
        levelData = gameObject.GetComponent<LevelData>();
        GameStart();
    }

    // Update is called once per frame
    void Update()
    {
        if (_playing)
        {
            if (levelData.dotsRemain <= 0)
            {
                GameOver(AgentPacmanCharacter.Type.Pacman);
            }

            if (livesLeft < 0)
            {
                GameOver(AgentPacmanCharacter.Type.Ghost);
            }

            scoreText.text = score.ToString();
        }
    }

    private void FixedUpdate()
    {
        if (levelData.powerPillActive)
        {
            _powerPillRemainTime -= Time.deltaTime;
            if (_powerPillRemainTime < 1e-9)
            {
                _powerPillRemainTime = 0.0f;
                levelData.powerPillActive = false;
                foreach (var ghost in _ghostInstances)
                {
                    ghost.GetComponent<SpriteRenderer>().sprite = ghost.GetComponent<CharacterController>().ghostNormal;
                }
            }
        }
    }

    private void Reset()
    {
        foreach (Transform child in transform)
        {
            if (child.CompareTag("Dot"))
            {
                Destroy(child.gameObject);
            }
            if (child.CompareTag("PowerPill"))
            {
                Destroy(child.gameObject);
            }
        }
        foreach (var ghost in _ghostInstances)
        {
            ghost.GetComponent<SpriteRenderer>().sprite = ghost.GetComponent<CharacterController>().ghostNormal;
        }
        _dotsMax = 0;
        _powerPillRemainTime = 0.0f;
        levelData.Reset();
    }
}
