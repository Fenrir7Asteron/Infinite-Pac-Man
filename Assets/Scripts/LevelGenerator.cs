using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

public class LevelGenerator : MonoBehaviour
{
    [Header("Level parameters")]
    [SerializeField] [Range(8, 30)] private int halfWidthInTiles = 14;
    [SerializeField] [Range(8, 30)] private int halfHeightInTiles = 15;
    [SerializeField] [Range(6, 10)] private int ghostBoxWidth = 6;
    [SerializeField] [Range(4, 10)] private int ghostBoxHeight = 4;
    [SerializeField] [Range(0.45f, 1.0f)] private float maxEmptySpaces = 0.45f;
    
    [Header("Controls")]
    [SerializeField] private bool generateLevel = false;
    [SerializeField] private String exportName = "";
    [SerializeField] private bool exportLevel = false;
    [SerializeField] private String importName = "";
    [SerializeField] private bool importLevel = false;

    [Header("Level tiles")]
    [SerializeField] private Tile[] obstacle;
    [SerializeField] private Tile[] ghostBox;
    [SerializeField] private Tile[] levelBorder;
    
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    
    private char[,] _halfLevelTiles;
    private LevelData _levelData;

    private readonly Tuple<int, int, Vector2Int>[] _wallBlueprints =
    {
        // Horizontal bars
        new Tuple<int, int, Vector2Int>(3, 2, new Vector2Int(0, 1)),
        new Tuple<int, int, Vector2Int>(5, 2, new Vector2Int(0, 2)),
        new Tuple<int, int, Vector2Int>(7, 2, new Vector2Int(0, 3)),
        // Vertical bars
        new Tuple<int, int, Vector2Int>(2, 3, new Vector2Int(1, 0)),
        new Tuple<int, int, Vector2Int>(2, 5, new Vector2Int(2, 0)),
        new Tuple<int, int, Vector2Int>(2, 7, new Vector2Int(3, 0)),
        // Rectangles
        new Tuple<int, int, Vector2Int>(3, 5, new Vector2Int(2, 1)),
        new Tuple<int, int, Vector2Int>(5, 3, new Vector2Int(1, 2)),
        new Tuple<int, int, Vector2Int>(5, 5, new Vector2Int(2, 2)),
    };


    public void GenerateLevel()
    {
        while (true)
        {
            _levelData.tilemap.ClearAllTiles();
            _halfLevelTiles = new char[halfHeightInTiles, halfWidthInTiles];
            mainCamera.orthographicSize = Math.Max(halfHeightInTiles, (int) Math.Ceiling((double) halfWidthInTiles / 4 * 3)) + 3;
            for (int i = 0; i < halfHeightInTiles - 1; ++i)
            {
                for (int j = 0; j < halfWidthInTiles - 1; ++j)
                {
                    _halfLevelTiles[i, j] = '.';
                }
            }

            Build(halfWidthInTiles, 1, new Vector2Int(halfHeightInTiles - 1, 0), 'B');
            Build(1, halfHeightInTiles, new Vector2Int(0, halfWidthInTiles - 1), 'B');
            // PrintLevel();
            var diggerPos = new Vector2Int(0, 0);

            bool canBuild = true, canMove = true;
            while (canBuild || canMove)
            {
                canBuild = TryBuild(diggerPos);
                canMove = TryDigCorridor(ref diggerPos);
                // PrintLevel();
            }

            MirrorLevel();
            AddGhostBox();
            
            if (ValidateLevel())
            {
                RenderLevel();
                return;
            }
        }
    }

    private void AddGhostBox()
    {
        int startX = _levelData.levelTiles.GetLength(0) / 2 - ghostBoxHeight / 2;
        int endX = _levelData.levelTiles.GetLength(0) / 2 + ghostBoxHeight / 2;
        int startY = _levelData.levelTiles.GetLength(1) / 2 - ghostBoxWidth / 2;
        int endY = _levelData.levelTiles.GetLength(1) / 2 + ghostBoxWidth / 2;
        int width = _levelData.levelTiles.GetLength(1);
        // Mark tiles as Ghost, removing all obstacles.
        // -1, +1 are because I want to have box padded with 1 tile-wide empty space.
        for (int i = startX - 1; i < endX + 1; ++i)
        {
            for (int j = startY - 1; j < endY + 1; ++j)
            {
                if ((width / 2 - j  == 0 || width / 2 - j  == 1) && i == endX - 1)
                {
                    _levelData.levelTiles[i, j] = 'D';
                }
                else if (i < startX || i >= endX || j < startY || j >= endY)
                {
                    _levelData.levelTiles[i, j] = '.';
                }
                else
                {
                    _levelData.levelTiles[i, j] = 'G';
                }
            }
        }
    }

    private bool Free(char[,] tiles, Vector2Int pos)
    {
        return tiles[pos.x, pos.y] != 'W' && tiles[pos.x, pos.y] != 'B';
    }

    private void MirrorLevel()
    {
        // _levelData.levelTiles is _halfLevelTiles reflected horizontally and vertically - making symmetric level.
        _levelData.levelTiles = new char[halfHeightInTiles * 2, halfWidthInTiles * 2];
        // Copy generated tiles
        for (int i = halfHeightInTiles; i < halfHeightInTiles * 2; ++i)
        {
            for (int j = halfWidthInTiles; j < halfWidthInTiles * 2; ++j)
            {
                
                _levelData.levelTiles[i, j] = _halfLevelTiles[i - halfHeightInTiles, j - halfWidthInTiles];
                // After level is generated we do not need information about corridors.
                if (_levelData.levelTiles[i, j] == 'C')
                {
                    _levelData.levelTiles[i, j] = '.';
                }
            }
        }
        // Reflect horizontally
        for (int i = halfHeightInTiles; i < halfHeightInTiles * 2; ++i)
        {
            for (int j = halfWidthInTiles - 1; j >= 0; --j)
            {
                _levelData.levelTiles[i, j] = _levelData.levelTiles[i, halfWidthInTiles + (halfWidthInTiles - j - 1)];
            }
        }
        // Reflect vertically
        for (int i = halfHeightInTiles - 1; i >= 0; --i)
        {
            for (int j = 0; j < halfWidthInTiles * 2; ++j)
            {
                _levelData.levelTiles[i, j] = _levelData.levelTiles[halfHeightInTiles + (halfHeightInTiles - i - 1), j];
            }
        }
    }

    private bool ValidateLevel()
    {
        int height = _levelData.levelTiles.GetLength(0);
        int width = _levelData.levelTiles.GetLength(1);
        int countEmptySpaces = 0;
        Vector2Int start = new Vector2Int(0, 0);
        bool[,] visited = new bool[height, width];
        for (int i = 0; i < height; ++i)
        {
            for (int j = 0; j < width; ++j)
            {
                if (Free(_levelData.levelTiles, new Vector2Int(i, j)))
                {
                    ++countEmptySpaces;
                    start = new Vector2Int(i, j);
                    visited[i, j] = false;
                }
            }
        }

        double percentage = (double) countEmptySpaces / height / width;
        if (percentage < 0.05f || percentage > maxEmptySpaces)
        {
            return false;
        }
        dfs(start, _levelData.levelTiles, visited);

        int countVisited = 0;
        for (int i = 0; i < height; ++i)
        {
            for (int j = 0; j < width; ++j)
            {
                if (visited[i, j])
                {
                    ++countVisited;
                }
            }
        }
        if (countVisited < countEmptySpaces)
        {
            return false;
        }

        return true;
    }

    private void dfs(Vector2Int currentPos, char[,] tiles, bool[,] visited)
    {
        visited[currentPos.x, currentPos.y] = true;
        foreach (var move in Globals.Moves)
        {
            var nextPos = currentPos + move;
            if (nextPos.x < 0 || nextPos.x >= visited.GetLength(0) || nextPos.y < 0 || nextPos.y >= visited.GetLength(1))
            {
                continue;
            }
            if (Free(tiles, nextPos) && !visited[nextPos.x, nextPos.y])
            {
                dfs(nextPos, tiles, visited);
            }
        }
    }

    private void RenderLevel()
    {
        _levelData.tilemap.ClearAllTiles();
        int height = _levelData.levelTiles.GetLength(0);
        int width = _levelData.levelTiles.GetLength(1);
        
        // Render ghost box
        int startX = _levelData.levelTiles.GetLength(0) / 2 - ghostBoxHeight / 2;
        int endX = _levelData.levelTiles.GetLength(0) / 2 + ghostBoxHeight / 2;
        int startY = _levelData.levelTiles.GetLength(1) / 2 - ghostBoxWidth / 2;
        int endY = _levelData.levelTiles.GetLength(1) / 2 + ghostBoxWidth / 2;
        // Render left and right parts of box
        for (int i = startX + 1; i < endX - 1; ++i)
        {
            if (_levelData.levelTiles[i, startY] == 'D')
            {
                RenderTile(i, startY, _levelData.levelTiles);
            }
            else
            {
                RenderTile(i, startY, _levelData.levelTiles, (int) TileDirection.Left);
            }
            if (_levelData.levelTiles[i, endY - 1] == 'D')
            {
                RenderTile(i, endY - 1, _levelData.levelTiles);
            }
            else
            {
                RenderTile(i, endY - 1, _levelData.levelTiles, (int) TileDirection.Right);
            }
        }
        // Render top and bottom parts of box
        for (int j = startY + 1; j < endY - 1; ++j)
        {
            if (_levelData.levelTiles[endX - 1, j] == 'D')
            {
                RenderTile(endX - 1, j, _levelData.levelTiles);
            }
            else
            {
                RenderTile(endX - 1, j, _levelData.levelTiles, (int) TileDirection.Top);
            }
            if (_levelData.levelTiles[startX, j] == 'D')
            {
                RenderTile(startX, j, _levelData.levelTiles);
            }
            else
            {
                RenderTile(startX, j, _levelData.levelTiles, (int) TileDirection.Bottom);
            }
        }
        RenderTile(startX, startY, _levelData.levelTiles, (int) TileDirection.BottomLeft);
        RenderTile(startX, endY - 1, _levelData.levelTiles, (int) TileDirection.BottomRight);
        RenderTile(endX - 1, startY, _levelData.levelTiles, (int) TileDirection.TopLeft);
        RenderTile(endX - 1, endY - 1, _levelData.levelTiles, (int) TileDirection.TopRight);
        
        // Render walls
        for (int i = 1; i < height - 1; ++i)
        {
            for (int j = 1; j < width - 1; ++j)
            {
                if (_levelData.levelTiles[i, j] != 'W')
                {
                    continue;
                }
                var current = new Vector2Int(i, j);
                var up = current + Globals.Moves[(int) Globals.MoveDirection.Up];
                var right = current + Globals.Moves[(int) Globals.MoveDirection.Right];
                var down = current + Globals.Moves[(int) Globals.MoveDirection.Down];
                var left = current + Globals.Moves[(int) Globals.MoveDirection.Left];
                
                if (Occupied(_levelData.levelTiles, new []{up, right, down, left}))
                {
                    RenderTile(i, j, _levelData.levelTiles, (int) TileDirection.Other);
                }
                else if (Occupied(_levelData.levelTiles, new []{right, down, left}))
                {
                    RenderTile(i, j, _levelData.levelTiles, (int) TileDirection.Top);
                }
                else if (Occupied(_levelData.levelTiles, new []{up, down, left}))
                {
                    RenderTile(i, j, _levelData.levelTiles, (int) TileDirection.Right);
                } 
                else if (Occupied(_levelData.levelTiles, new []{up, right, left}))
                {
                    RenderTile(i, j, _levelData.levelTiles, (int) TileDirection.Bottom);
                }
                else if (Occupied(_levelData.levelTiles, new []{up, right, down}))
                {
                    RenderTile(i, j, _levelData.levelTiles, (int) TileDirection.Left);
                }
                else if (Occupied(_levelData.levelTiles, new []{right, down}))
                {
                    RenderTile(i, j, _levelData.levelTiles, (int) TileDirection.TopLeft);
                }
                else if (Occupied(_levelData.levelTiles, new []{left, down}))
                {
                    RenderTile(i, j, _levelData.levelTiles, (int) TileDirection.TopRight);
                }
                else if (Occupied(_levelData.levelTiles, new []{up, left}))
                {
                    RenderTile(i, j, _levelData.levelTiles, (int) TileDirection.BottomRight);
                }
                else if (Occupied(_levelData.levelTiles, new []{right, up}))
                {
                    RenderTile(i, j, _levelData.levelTiles, (int) TileDirection.BottomLeft);
                }
            }
        }

        // Render borders
        for (int i = 1; i < height - 1; ++i)
        {
            RenderTile(i, 0, _levelData.levelTiles, (int) TileDirection.Left);
            RenderTile(i, width - 1, _levelData.levelTiles, (int) TileDirection.Right);
        }
        for (int j = 1; j < width - 1; ++j)
        {
            RenderTile(0, j, _levelData.levelTiles, (int) TileDirection.Bottom);
            RenderTile(height - 1,j, _levelData.levelTiles, (int) TileDirection.Top);
        }
        RenderTile(0, 0, _levelData.levelTiles, (int) TileDirection.BottomLeft);
        RenderTile(0, width - 1, _levelData.levelTiles, (int) TileDirection.BottomRight);
        RenderTile(height - 1, width - 1, _levelData.levelTiles, (int) TileDirection.TopRight);
        RenderTile(height - 1, 0, _levelData.levelTiles, (int) TileDirection.TopLeft);
    }

    private void RenderTile(int x, int y, char[,] tileTable, int tile = -1)
    {
        switch (tileTable[x, y])
        {
            case 'W':
                _levelData.tilemap.SetTile(new Vector3Int(y - halfWidthInTiles, x - halfHeightInTiles, 0), obstacle[tile]);
                break;
            case 'B':
                _levelData.tilemap.SetTile(new Vector3Int(y - halfWidthInTiles, x - halfHeightInTiles, 0), levelBorder[tile]);
                break;
            case 'G':
                _levelData.tilemap.SetTile(new Vector3Int(y - halfWidthInTiles, x - halfHeightInTiles, 0), ghostBox[tile]);
                break;
            case 'D':
                _levelData.tilemap.SetTile(new Vector3Int(y - halfWidthInTiles, x - halfHeightInTiles, 0), ghostBox[(int) TileDirection.Other]);
                break;
        }
    }

    private bool Occupied(char[,] tiles, Vector2Int[] cells)
    {
        int count = 0;
        foreach (var cell in cells)
        {
            if (cell.x < 0 || cell.x >= halfHeightInTiles * 2 || cell.y < 0 || cell.y >= halfWidthInTiles * 2)
            {
                count++;
            }
            else if (!Free(tiles, cell))
            {
                count++;
            }
        }
        
        
        return count == cells.Length;
    }

    private void PrintLevel()
    {
        Debug.Log("Level Table");
        for (int i = 0; i < halfHeightInTiles; ++i)
        {
            String tiles = "";
            for (int j = 0; j < halfWidthInTiles; ++j)
            {
                tiles += _halfLevelTiles[i, j];
            }

            Debug.Log(tiles);
        }
    }

    private bool TryBuild(Vector2Int diggerPos)
    {
        List<int> shuffledIdx = Enumerable.Range(0, _wallBlueprints.Length).ToList();
        RandomShuffle(shuffledIdx);
        foreach (var idx in shuffledIdx)
        {
            var blueprint = _wallBlueprints[idx];
            int width = blueprint.Item1;
            int height = blueprint.Item2;
            Vector2Int centerPos = blueprint.Item3;
            if (CanBuild(width, height, diggerPos - centerPos))
            {
                Build(width, height, diggerPos - centerPos, 'W');
                return true; // Return true if managed to build any wall type
            }
        }
        
        return false; // Return false if failed to build any wall type
    }

    private bool TryDigCorridor(ref Vector2Int diggerPos)
    {
        List<int> shuffledIdx = Enumerable.Range(0, Globals.Moves.Length).ToList();
        RandomShuffle(shuffledIdx);
        List<int> moveLength = Enumerable.Range(3, 8).ToList();
        RandomShuffle(moveLength);
        
        foreach (var idx in shuffledIdx)
        {
            var dir = Globals.Moves[idx];
            foreach (var length in moveLength)
            {
                if (CanDigCorridor(ref diggerPos, dir, length))
                {
                    DigCorridor(ref diggerPos, dir, length);
                    return true; // Return true if managed to dig somewhere
                }
            }
        }

        return false; // Return false if failed to dig anywhere
    }

    private bool CanBuild(int width, int height, Vector2Int startingPos)
    {
        for (int i = startingPos[0]; i < startingPos[0] + height; ++i)
        {
            if (i < 0 || i >= halfHeightInTiles)
            {
                continue;
            }
            for (int j = startingPos[1]; j < startingPos[1] + width; ++j)
            {
                if (j < 0 || j >= halfWidthInTiles)
                {
                    continue;
                }
                if (!Free(_halfLevelTiles, new Vector2Int(i, j)))
                {
                    return false;
                }
            }
        }

        return true;
    }
    
    private bool CanDigCorridor(ref Vector2Int startingPos, Vector2Int dir, int length)
    {
        Vector2Int currentPos = new Vector2Int(startingPos.x, startingPos.y);
        for (int i = 0; i < length; ++i)
        {
            Vector2Int nextPos = currentPos + dir;
            if (nextPos.x < 0 || nextPos.x >= halfHeightInTiles || nextPos.y < 0 || nextPos.y >= halfWidthInTiles)
            {
                // Digging out of bounds
                return false;
            }
            if (_halfLevelTiles[currentPos.x, currentPos.y] != 'W' && _halfLevelTiles[nextPos.x, nextPos.y] == 'W')
            {
                // Intersecting wall
                return false;
            }
            if (_halfLevelTiles[currentPos.x, currentPos.y] != 'C' && _halfLevelTiles[nextPos.x, nextPos.y] == 'C')
            {
                // Intersecting corridor
                return false;
            }
            if (_halfLevelTiles[nextPos.x, nextPos.y] == 'B')
            {
                // Intersecting border
                return false;
            }

            currentPos += dir;
        }

        if (_halfLevelTiles[currentPos.x, currentPos.y] != '.')
        {
            // Move ended not on the empty space
            return false;
        }

        return true;
    }
    
    private void DigCorridor(ref Vector2Int currentPos, Vector2Int dir, int length)
    {
        for (int i = 0; i < length; ++i)
        {
            if (_halfLevelTiles[currentPos.x, currentPos.y] == '.')
            {
                _halfLevelTiles[currentPos.x, currentPos.y] = 'C';
            }

            currentPos += dir;
        }
    }
    
    private void Build(int width, int height, Vector2Int startingPos, char building)
    {
        for (int i = startingPos[0]; i < startingPos[0] + height; ++i)
        {
            if (i < 0 || i >= halfHeightInTiles)
            {
                continue;
            }
            for (int j = startingPos[1]; j < startingPos[1] + width; ++j)
            {
                if (j < 0 || j >= halfWidthInTiles)
                {
                    continue;
                }
                _halfLevelTiles[i, j] = building;
            }
        }
    }

    private void RandomShuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++) {
            T temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    private void Awake()
    {
        _levelData = GetComponent<LevelData>();
    }

    private enum TileDirection
    {
        TopLeft,
        Top,
        TopRight,
        Right,
        BottomRight,
        Bottom,
        BottomLeft,
        Left,
        Other,
    }

    void Update()
    {
        if (generateLevel)
        {
            GenerateLevel();
            generateLevel = false;
        }

        if (exportLevel)
        {
            exportLevel = false;
            if (!Directory.Exists("Assets/Levels"))
            {
                Directory.CreateDirectory("Assets/Levels");
            }
            String exportPath = String.Concat("Assets/Levels/", exportName);
            System.IO.FileInfo fi = null;
            try {
                fi = new System.IO.FileInfo(exportPath);
            }
            catch (ArgumentException) { }
            catch (System.IO.PathTooLongException) { }
            catch (NotSupportedException) { }
            if (!ReferenceEquals(fi, null)) {
                using (var sw = new StreamWriter(exportPath))
                {
                    for (int i = 0; i < _levelData.levelTiles.GetLength(0); i++)
                    {
                        for (int j = 0; j < _levelData.levelTiles.GetLength(1); j++)
                        {
                            sw.Write(_levelData.levelTiles[i, j]);
                        }
                        sw.Write("\n");
                    }

                    sw.Flush();
                    sw.Close();
                }
            }
        }

        if (importLevel)
        {
            importLevel = false;
            if (!Directory.Exists("Assets/Levels"))
            {
                Debug.Log("Directory 'Assets/Levels/' does not exist, nothing to import.");
                return;
            }
            String importPath = String.Concat("Assets/Levels/", importName);
            System.IO.FileInfo fi = null;
            try {
                fi = new System.IO.FileInfo(importPath);
            }
            catch (ArgumentException) { }
            catch (System.IO.PathTooLongException) { }
            catch (NotSupportedException) { }
            if (!ReferenceEquals(fi, null)) {
                using (var sr = new StreamReader(importPath))
                {
                    for (int i = 0; i < _levelData.levelTiles.GetLength(0); i++)
                    {
                        String line = sr.ReadLine();
                        if (!ReferenceEquals(line, null))
                        {
                            for (int j = 0; j < _levelData.levelTiles.GetLength(1); j++)
                            {
                                if (j < line.Length)
                                {
                                    _levelData.levelTiles[i, j] = line[j];
                                }
                            }
                        }
                    }
                    sr.Close();
                    
                    RenderLevel();
                }
            }
        }
    }
}
