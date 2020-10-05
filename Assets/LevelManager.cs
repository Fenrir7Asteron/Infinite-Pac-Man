using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public int livesLeft = 2;
    public int livesMax = 2;
    [Range(1, 6)] public int ghostsMax = 4;
    
    [Header("Prefabs")]
    [SerializeField] private GameObject pacman;
    
    [HideInInspector] public LevelData levelData;
    
    private LevelGenerator _levelGenerator;
    
    public void LevelStart()
    {
        _levelGenerator.GenerateLevel();
        SpawnPacMan();
        // SpawnGhosts();
    }

    private void SpawnPacMan()
    {
        for (int i = 0; i < levelData.LevelHeight; ++i)
        {
            for (int j = 0; j < levelData.LevelWidth; ++j)
            {
                if (levelData.Free(new Vector2Int(i, j)))
                {
                    Vector2Int tile = new Vector2Int(i, j);
                    var pacmanInstance = Instantiate(pacman, transform);
                    pacmanInstance.transform.localPosition = levelData.LocalPositionByTile(tile);
                    pacmanInstance.GetComponent<MoveController>().currentTile = tile;
                    return;
                }
            }
        }
    }
    
    // Start is called before the first frame update
    void Start()
    {
        _levelGenerator = gameObject.GetComponent<LevelGenerator>();
        levelData = gameObject.GetComponent<LevelData>();
        LevelStart();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
