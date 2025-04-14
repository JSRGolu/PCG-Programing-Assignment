using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Based on "Procedural Cave Generation" by Sebastian Lague
 * https://www.youtube.com/watch?v=v7yyZZjF1z4&list=PLFt_AvWsXl0eZgMK_DT5_biRkWXftAOf9
 */
public class MapGenerator : MonoBehaviour
{
    // Serialized Variables
    [Header("Map Dimensions")]
    public int width;
    public int height;
    public int mapBorderSize;

    [Header("CheckPoints")]
    public GameObject pointPrefab;
    public int startPointOffset;

    [Header("Seed Control")]
    public string seed;
    public bool useRandomSeed;

    [Header("Trees Generation")]
    [Range(0, 100)]
    public int randomFillPercent;
    public int numPatches;
    public int patchRadius;
    public int smoothning;

    [Header("Pathing")]
    public float maxDeviation;
    public int segments;

    [Header("Generation Delay")]
    public float stepDelay;

    // Private Variables
    private int[,] map;
    private System.Random pseudoRandom;
    private Vector2Int startPoint;
    private Vector2Int endPoint;
    private List<GameObject> spawnedObjects = new List<GameObject>();
    private List<Vector2Int> patchCenters = new List<Vector2Int>();
    private List<Vector2Int> path = new List<Vector2Int>();
    private List<Vector2Int> waypoints = new List<Vector2Int>();
    private List<Vector2> pathPoints = new List<Vector2>();
    private bool isGenerating = false;

    /*
	 * Generate the map on start, on mouse click
	 */
    void Start()
    {
        StartCoroutine(GenerateMap());
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !isGenerating)
        {
            StartCoroutine(GenerateMap());
        }
    }

    IEnumerator GenerateMap()
    {
        isGenerating = true;
        Debug.Log("Generating Map: " + isGenerating);

        map = new int[width, height];

        path.Clear();
        patchCenters.Clear();
        waypoints.Clear();
        pathPoints.Clear();
        ClearSpawnObjects();
        
        StartRandomSeed();

        // Stage 1: populate the grid cells
        yield return StartCoroutine(PopulateMap());

        // Stage 2: apply cellular automata rules
        for (int i = 0; i < smoothning; i++)
        {
            yield return StartCoroutine(SmoothMap());
            GenerateMesh();
            yield return new WaitForSeconds(stepDelay);
        }

        // Stage 3: finalise the map
        yield return StartCoroutine(ProcessMap());
        yield return StartCoroutine(AddMapBorder());

        // Generate mesh
        GenerateMesh();

        isGenerating = false;
        Debug.Log("Generating Map: " + isGenerating);
    }

    void StartRandomSeed()
    {
        if (useRandomSeed)
        {
            seed = Time.time.ToString();
        }

        pseudoRandom = new System.Random(seed.GetHashCode());
    }

    void GenerateMesh()
    {
        MeshGenerator meshGen = GetComponent<MeshGenerator>();
        if (meshGen != null)
            meshGen.GenerateMesh(map, 1);
    }

    /*
	 * STAGE 1: Populate the map
	 */
    IEnumerator PopulateMap()
    {
        yield return StartCoroutine(ForestFill());
        GenerateMesh();
        yield return new WaitForSeconds(stepDelay);

        yield return StartCoroutine(StartAndEndPoint());
        GenerateMesh();
        yield return new WaitForSeconds(stepDelay);

        yield return StartCoroutine(CreatePatches());
        GenerateMesh();
        yield return new WaitForSeconds(stepDelay);
    }

    IEnumerator StartAndEndPoint()
    {
        int edge = pseudoRandom.Next(0, 4);

        switch (edge)
        {
            case 0: // Left edge
                startPoint = new Vector2Int(startPointOffset, pseudoRandom.Next(startPointOffset, height - startPointOffset));
                break;
            case 1: // Right edge
                startPoint = new Vector2Int(width - 1 - startPointOffset, pseudoRandom.Next(startPointOffset, height - startPointOffset));
                break;
            case 2: // Top edge
                startPoint = new Vector2Int(pseudoRandom.Next(startPointOffset, width - startPointOffset), height - 1 - startPointOffset);
                break;
            case 3: // Bottom edge
                startPoint = new Vector2Int(pseudoRandom.Next(startPointOffset, width - startPointOffset), startPointOffset);
                break;
        }

        bool isLeft = startPoint.x < width / 2;
        bool isBottom = startPoint.y < height / 2;

        if (isLeft && isBottom)
            endPoint = new Vector2Int(pseudoRandom.Next(width / 2, width - 2), pseudoRandom.Next(height / 2, height - 2));

        else if (!isLeft && !isBottom)
            endPoint = new Vector2Int(pseudoRandom.Next(2, width / 2), pseudoRandom.Next(2, height / 2));

        else if (isLeft && !isBottom)
            endPoint = new Vector2Int(pseudoRandom.Next(width / 2, width - 2), pseudoRandom.Next(2, height / 2));

        else if (!isLeft && isBottom)
            endPoint = new Vector2Int(pseudoRandom.Next(2, width / 2), pseudoRandom.Next(height / 2, height - 2));

        PlaceGameObject(pointPrefab, startPoint, Color.black);
        yield return new WaitForSeconds(stepDelay);
        PlaceGameObject(pointPrefab, endPoint, Color.red);
    }

    void PlaceGameObject(GameObject prefab, Vector2Int point, Color color)
    {
        Vector3 pos = new Vector3((point.x - width / 2), 0f, (point.y - height / 2));

        GameObject newObj = Instantiate(prefab, pos, Quaternion.Euler(90f, 0f, 0f));
        spawnedObjects.Add(newObj);

        Renderer renderer = newObj.GetComponent<Renderer>();
        if (renderer != null)
            renderer.material.color = color;

        ClearForest(point, 3);
    }

    IEnumerator ForestFill()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                {
                    map[x, y] = 1;
                }
                else
                {
                    map[x, y] = (pseudoRandom.Next(0, 100) < randomFillPercent) ? 1 : 0;
                }
            }
        }
        yield return null;
    }

    IEnumerator CreatePatches()
    {
        for (int i = 0; i < numPatches; i++)
        {
            int x = pseudoRandom.Next(2, width - 2);
            int y = pseudoRandom.Next(2, height - 2);

            Vector2Int center = new Vector2Int(x, y);
            
            if (Vector2Int.Distance(center, startPoint) < patchRadius + 2 || Vector2Int.Distance(center, endPoint) < patchRadius + 2)
            {
                i--;
                continue;
            }

            patchCenters.Add(center);

            ClearForest(center, patchRadius);

            GenerateMesh();
            yield return new WaitForSeconds(stepDelay / 4);
        }
        path.AddRange(patchCenters);
    }

    void ClearForest(Vector2Int pCenter, int pRadius)
    {
        for (int patchX = -pRadius; patchX <= pRadius; patchX++)
        {
            for (int patchY = -pRadius; patchY <= pRadius; patchY++)
            {
                int newX = pCenter.x + patchX;
                int newY = pCenter.y + patchY;

                if (IsInMapRange(newX, newY))
                {
                    if (patchX * patchX + patchY * patchY <= pRadius * pRadius)
                    {
                        map[newX, newY] = 0;
                    }
                }
            }
        }
    }

    void ClearSpawnObjects()
    {
        foreach (GameObject obj in spawnedObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        spawnedObjects.Clear();
    }

    /*
	 * STAGE 2: Smooth map with CA
	 */
    IEnumerator SmoothMap()
    {
        int[,] newMap = new int[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int neighbourWallTiles = GetSurroundingWallCount(x, y);

                if (neighbourWallTiles > 4)
                    newMap[x, y] = 1;
                else if (neighbourWallTiles < 3)
                    newMap[x, y] = 0;
                else
                    newMap[x, y] = map[x, y];
            }
        }

        map = newMap;
        yield return null;

    }

    int GetSurroundingWallCount(int gridX, int gridY)
    {
        int wallCount = 0;
        for (int neighbourX = gridX - 1; neighbourX <= gridX + 1; neighbourX++)
        {
            for (int neighbourY = gridY - 1; neighbourY <= gridY + 1; neighbourY++)
            {
                if (IsInMapRange(neighbourX, neighbourY))
                {
                    if (neighbourX != gridX || neighbourY != gridY)
                    {
                        wallCount += map[neighbourX, neighbourY];
                    }
                }
                else
                {
                    wallCount++;
                }
            }
        }

        return wallCount;
    }

    bool IsInMapRange(int x, int y)
    {
        return (x >= 0 && x < width && y >= 0 && y < height);
    }


    /*
	 * Stage 3: produce the finished map
	 */
    IEnumerator ProcessMap()
    {
        yield return StartCoroutine(GeneratePathing());
    }

    IEnumerator GeneratePathing()
    {
        waypoints.AddRange(path);
        waypoints.Add(endPoint);

        Vector2Int currentPoint = startPoint;
        List<Vector2Int> visited = new List<Vector2Int> { currentPoint };

        while (waypoints.Count > 0)
        {
            Vector2Int nearest = Vector2Int.zero;
            float minDistance = float.MaxValue;
            foreach (Vector2Int waypoint in waypoints)
            {
                float distance = Vector2Int.Distance(currentPoint, waypoint);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = waypoint;
                }
            }

            yield return StartCoroutine(DrawPath(currentPoint, nearest));
            GenerateMesh();
            yield return new WaitForSeconds(stepDelay / 2);

            currentPoint = nearest;
            visited.Add(currentPoint);
            waypoints.Remove(nearest);
        }
    }

    IEnumerator DrawPath(Vector2Int from, Vector2Int to)
    {
        GenerateCurvedPath(from, to, segments, maxDeviation, pathPoints);

        for (int i = 0; i < pathPoints.Count - 1; i++)
        {
            Vector2 start = pathPoints[i];
            Vector2 end = pathPoints[i + 1];

            int steps = Mathf.Max(Mathf.Abs((int)(end.x - start.x)), Mathf.Abs((int)(end.y - start.y))) + 1;
            for (int step = 0; step <= steps; step++)
            {
                float t = step / (float)steps;
                int x = Mathf.RoundToInt(Mathf.Lerp(start.x, end.x, t));
                int y = Mathf.RoundToInt(Mathf.Lerp(start.y, end.y, t));

                if (IsInMapRange(x, y))
                {
                    map[x, y] = 0;

                    Vector2Int[] neighbors = new Vector2Int[]
                    {
                        new Vector2Int(x + 1, y),
                        new Vector2Int(x - 1, y),
                        new Vector2Int(x, y + 1),
                        new Vector2Int(x, y - 1)
                    };

                    int neighborIndex = pseudoRandom.Next(0, neighbors.Length);
                    Vector2Int neighbor = neighbors[neighborIndex];

                    if (IsInMapRange(neighbor.x, neighbor.y))
                        map[neighbor.x, neighbor.y] = 0;
                }
            }
        }
        yield return null;
    }

    void GenerateCurvedPath(Vector2 from, Vector2 to, int segments, float maxDeviation, List<Vector2> pathPoints)
    {
        if (segments <= 0)
        {
            if (!pathPoints.Contains(from))
            {
                pathPoints.Add(from);
            }
            pathPoints.Add(to);
            return;
        }

        Vector2 midpoint = (from + to) / 2f;

        Vector2 direction = (to - from).normalized;
        Vector2 perpendicular = new Vector2(-direction.y, direction.x);
        float deviation = (float)(pseudoRandom.NextDouble() * 2 - 1) * maxDeviation;
        midpoint += perpendicular * deviation;

        midpoint.x = Mathf.Clamp(midpoint.x, 1, width - 2);
        midpoint.y = Mathf.Clamp(midpoint.y, 1, height - 2);

        GenerateCurvedPath(from, midpoint, segments - 1, maxDeviation * 0.5f, pathPoints);
        GenerateCurvedPath(midpoint, to, segments - 1, maxDeviation * 0.5f, pathPoints);
    }

    IEnumerator AddMapBorder()
    {
        int[,] borderedMap = new int[width + mapBorderSize * 2, height + mapBorderSize * 2];

        for (int x = 0; x < borderedMap.GetLength(0); x++)
        {
            for (int y = 0; y < borderedMap.GetLength(1); y++)
            {
                if (x >= mapBorderSize && x < width + mapBorderSize && y >= mapBorderSize && y < height + mapBorderSize)
                {
                    borderedMap[x, y] = map[x - mapBorderSize, y - mapBorderSize];
                }
                else
                {
                    borderedMap[x, y] = 1;
                }
            }
        }
        map = borderedMap;
        yield return null;
    }
}