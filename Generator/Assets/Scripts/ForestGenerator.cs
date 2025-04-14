using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForestGenerator : MonoBehaviour
{
    public int width;
    public int height;

    public float noiseScale = 0.1f;
    [Range(0, 1)]
    public float treeThreshold = 0.5f;

    public string seed;
    public bool useRandomSeed;

    int[,] map;
    Vector2Int startPoint;
    Vector2Int endPoint;
    List<Vector2Int> path = new List<Vector2Int>();

    void Start()
    {
        GenerateMap();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            GenerateMap();
        }
    }

    void GenerateMap()
    {
        map = new int[width, height];

        GenerateStartAndEnd();
        GeneratePath();
        GenerateForestWithPerlin();
        ApplyCellularAutomata(1);
        ClearTreesOnPath();
        AddMapBorder();

        MeshGenerator meshGen = GetComponent<MeshGenerator>();
        meshGen.GenerateMesh(map, 1);
    }

    void GenerateStartAndEnd()
    {
        startPoint = new Vector2Int(2, 2);
        endPoint = new Vector2Int(width - 3, height - 3);

        map[startPoint.x, startPoint.y] = 2;
        map[endPoint.x, endPoint.y] = 3;
    }

    void GeneratePath()
    {
        Vector2Int current = startPoint;
        path.Add(current);

        while (Vector2Int.Distance(current, endPoint) > 1)
        {
            Vector2Int direction = new Vector2Int(
                Mathf.Clamp(endPoint.x - current.x, -1, 1),
                Mathf.Clamp(endPoint.y - current.y, -1, 1)
            );

            if (UnityEngine.Random.value > 0.5f)
                direction.x = 0;
            else
                direction.y = 0;

            Vector2Int next = current + direction;
            if (IsInMapRange(next.x, next.y))
            {
                current = next;
                path.Add(current);
                map[current.x, current.y] = 2;
            }
        }
    }

    void GenerateForestWithPerlin()
    {
        if (useRandomSeed)
            seed = Time.time.ToString();

        float offsetX = UnityEngine.Random.Range(0f, 1000f);
        float offsetY = UnityEngine.Random.Range(0f, 1000f);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (map[x, y] == 0) // only fill empty tiles
                {
                    float noise = Mathf.PerlinNoise(x * noiseScale + offsetX, y * noiseScale + offsetY);
                    if (noise > treeThreshold)
                        map[x, y] = 1;
                }
            }
        }
    }

    void ApplyCellularAutomata(int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            int[,] newMap = (int[,])map.Clone();

            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    if (map[x, y] == 1 || map[x, y] == 0)
                    {
                        int wallCount = GetSurroundingWallCount(x, y);

                        if (wallCount > 4)
                            newMap[x, y] = 1;
                        else if (wallCount < 4)
                            newMap[x, y] = 0;
                    }
                }
            }

            map = newMap;
        }
    }

    int GetSurroundingWallCount(int gridX, int gridY)
    {
        int count = 0;
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                int nx = gridX + dx;
                int ny = gridY + dy;

                if (IsInMapRange(nx, ny) && map[nx, ny] == 1)
                    count++;
            }
        }
        return count;
    }

    void ClearTreesOnPath()
    {
        foreach (Vector2Int p in path)
        {
            map[p.x, p.y] = 2; // Ensure path is empty
        }

        map[endPoint.x, endPoint.y] = 3; // Ensure temple is clear
    }

    void AddMapBorder()
    {
        int borderSize = 1;
        int[,] borderedMap = new int[width + borderSize * 2, height + borderSize * 2];

        for (int x = 0; x < borderedMap.GetLength(0); x++)
        {
            for (int y = 0; y < borderedMap.GetLength(1); y++)
            {
                if (x >= borderSize && x < width + borderSize && y >= borderSize && y < height + borderSize)
                {
                    borderedMap[x, y] = map[x - borderSize, y - borderSize];
                }
                else
                {
                    borderedMap[x, y] = 1;
                }
            }
        }
        map = borderedMap;
    }

    bool IsInMapRange(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }
}
