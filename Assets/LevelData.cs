using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// Data about level that is accessible for outside scripts
public class LevelData : MonoBehaviour
{
    [HideInInspector] public char[,] levelTiles;
    [HideInInspector] public Tilemap tilemap;
    [HideInInspector] public int dotsRemain;
    [HideInInspector] public int powerPillsRemain;
    [HideInInspector] public bool powerPillActive = false;
    [Header("Prefabs")] 
    [SerializeField] private GameObject tilemapPrefab;
    [SerializeField] private Material tilemapMaterial;
    [Header("References")]
    [SerializeField] private Grid grid;
    public int LevelHeight => levelTiles.GetLength(0);
    public int LevelWidth => levelTiles.GetLength(1);

    public void Reset()
    {
        dotsRemain = 0;
        powerPillsRemain = 0;
        powerPillActive = false;
    }

    public bool Free(Vector2Int pos)
    {
        char tile = levelTiles[pos.x, pos.y];
        return new List<char>() {'W', 'B', 'G', 'D'}.Contains(tile) == false;
    }
    
    public void PrintLevel()
    {
        Debug.Log("Level Table");
        for (int i = 0; i < LevelHeight; ++i)
        {
            String tiles = "";
            for (int j = 0; j < LevelWidth; ++j)
            {
                tiles += levelTiles[i, j];
            }

            Debug.Log(tiles);
        }
    }

    public Vector3 LocalPositionByTile(Vector2Int pos)
    {
        return tilemap.GetCellCenterLocal(new Vector3Int(pos.y - LevelWidth / 2, pos.x - LevelHeight / 2, 0));
    }

    public Vector2Int TileByLocalPosition(Vector3 localPos)
    {
        Vector3Int cell = tilemap.LocalToCell(localPos);
        return new Vector2Int(cell.y + LevelHeight / 2, cell.x + LevelWidth / 2);
    }
    
    private void Awake()
    {
        tilemap = Instantiate(tilemapPrefab, transform.position, Quaternion.identity, grid.transform).GetComponent<Tilemap>();
        InitTilemap(tilemap, "Foreground");
    }
    
    private void InitTilemap(Tilemap tilemap, String name)
    {
        tilemap.name = name;
        var renderer = tilemap.GetComponent<TilemapRenderer>();
        renderer.material = tilemapMaterial;
        renderer.sortingLayerName = name;
    }
}
