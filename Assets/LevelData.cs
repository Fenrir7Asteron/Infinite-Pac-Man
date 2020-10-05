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
    [Header("Prefabs")] 
    [SerializeField] private GameObject tilemapPrefab;
    [SerializeField] private Material tilemapMaterial;
    [Header("References")]
    [SerializeField] private Grid grid;
    public int LevelHeight => levelTiles.GetLength(0);
    public int LevelWidth => levelTiles.GetLength(1);
    
    public bool Free(Vector2Int pos)
    {
        return levelTiles[pos.x, pos.y] != 'W' && levelTiles[pos.x, pos.y] != 'B' && levelTiles[pos.x, pos.y] != 'G';
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
