using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public int score = 0;
    public int livesLeft = 2;
    public int livesMax = 2;
    public int powerPillMax = 4;
    public float powerPillDuration = 5.0f;
    [Range(1, 6)] public int ghostsMax = 4;
    [Header("Prefabs")]
    [SerializeField] private GameObject pacman;
    [SerializeField] private GameObject dot;
    [SerializeField] private GameObject powerPill;
    
    [HideInInspector] public LevelData levelData;
    
    private LevelGenerator _levelGenerator;
    private float _powerPillRemainTime = 0.0f;
    private GameObject _pacman;
    private Vector2Int _pacmanSpawnPlace;

    public void GameStart()
    {
        LevelStart();
    }
    public void LevelStart()
    {
        levelData.Reset();
        _levelGenerator.GenerateLevel();
        // levelData.PrintLevel();
        var tiles = GetFreeTiles();
        SpawnPacMan(tiles);
        // SpawnGhosts();
        SpawnConsumables(tiles);
    }
    
    public void ConsumeDot()
    {
        levelData.dotsRemain--;
        Debug.Log(levelData.dotsRemain);
        score += 10;
    }
    
    public void ConsumePowerPill()
    {
        levelData.dotsRemain--;
        score += 100;
        levelData.powerPillActive = true;
        _powerPillRemainTime = powerPillDuration;
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

    private void SpawnConsumables(List<Vector2Int> freeTiles)
    {
        Debug.Log(freeTiles.Count);
        List<int> shuffledIdx = Utils.RandomPermutation(0, freeTiles.Count);
        int idx = 0;
        while (idx < powerPillMax)
        {
            Vector2Int tile = freeTiles[shuffledIdx[idx]];
            var _powerPill = Instantiate(powerPill, transform);
            _powerPill.transform.localPosition = levelData.LocalPositionByTile(tile);
            levelData.dotsRemain++;
            idx++;
        }
        while (idx < shuffledIdx.Count)
        {
            Vector2Int tile = freeTiles[shuffledIdx[idx]];
            var _dot = Instantiate(dot, transform);
            _dot.transform.localPosition = levelData.LocalPositionByTile(tile);
            levelData.dotsRemain++;
            idx++;
        }
    }

    private void SpawnPacMan(List<Vector2Int> freeTiles)
    {
        if (!ReferenceEquals(_pacman, null))
        {
            _pacman.GetComponent<MoveController>().currentTile = _pacmanSpawnPlace;
            _pacman.transform.localPosition = levelData.LocalPositionByTile(_pacmanSpawnPlace);
            return;
        }
        
        Debug.Log(freeTiles.Count);
        List<int> shuffledIdx = Utils.RandomPermutation(0, freeTiles.Count);
        foreach (var idx in shuffledIdx)
        {
            Vector2Int tile = freeTiles[idx];
            _pacman = Instantiate(pacman, transform);
            _pacman.transform.localPosition = levelData.LocalPositionByTile(tile);
            _pacman.GetComponent<MoveController>().currentTile = tile;
            _pacmanSpawnPlace = tile;
            freeTiles.Remove(tile); // PacMan spawn place is not free. We can not spawn anything else there.
            return;
        }
    }
    
    // Start is called before the first frame update
    void Start()
    {
        _levelGenerator = gameObject.GetComponent<LevelGenerator>();
        levelData = gameObject.GetComponent<LevelData>();
        GameStart();
    }

    // Update is called once per frame
    void Update()
    {
        
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
            }
        }
    }
}
