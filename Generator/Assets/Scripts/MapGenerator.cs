using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Based on "Procedural Cave Generation" by Sebastian Lague
 * https://www.youtube.com/watch?v=v7yyZZjF1z4&list=PLFt_AvWsXl0eZgMK_DT5_biRkWXftAOf9
 * 
 * This script generates a procedural 2D map of a forest.
 * The map is represented as a 2D grid where:
 *   - 1 indicates a trees
 *   - 0 indicates a path or empty space
 * The generation process includes:
 *   - Creating a random forest
 *   - Placing start and end points
 *   - Smoothing the forest using cellular automata
 *   - Adding clear patches for variety
 *   - Generating curved paths between points
 *   - Adding a border around the map
 */
public class MapGenerator : MonoBehaviour
{
    #region Serialized Variables
    [Header("Map Dimensions")]
    // Define the size of the map grid
    public int mapWidth;
    public int mapHeight;
    // Size of the border around the map (filled with trees)
    public int mapBorderSize;

    [Header("CheckPoints")]
    // Prefab for visualising start and end points
    public GameObject pointPrefab;
    // Offset from the map edge for placing start and end points
    public int checkPointOffset;

    [Header("Seed Control")]
    // Seed for random number generation (can be set for reproducibility)
    public string seed;
    // If true, generates a random seed based on system time
    public bool useRandomSeed;

    [Header("Trees Generation")]
    // Percentage chance for a cell to be a tree during initial population
    [Range(0, 100)]
    public int randomFillPercent;
    // Number of clear patches to create in the forest
    public int numPatches;
    // Radius of each clear patch
    public int patchRadius;
    // Number of smoothing iterations to apply cellular automata rules
    public int smoothening;

    [Header("Pathing")]
    // Maximum deviation for path curvature
    public float maxDeviation;
    // Number of segments for recursive path curving
    public int segments;

    [Header("Generation Delay")]
    // Delay between generation steps for visualization (in seconds)
    public float stepDelay;
    #endregion

    #region Private Variables
    // 2D array representing the map grid (1 = tree, 0 = path)
    private int[,] map;
    // variable for random number generator
    private System.Random pseudoRandom;
    // Start and end points for the path
    private Vector2Int startPoint;
    private Vector2Int endPoint;
    // List to keep track of instantiated GameObjects (e.g., points)
    private List<GameObject> spawnedObjects = new List<GameObject>();
    // Centers of clear patches
    private List<Vector2Int> patchCenters = new List<Vector2Int>();
    // List to hold patch centers
    private List<Vector2Int> path = new List<Vector2Int>();
    // Waypoints for path generation (patch centers and end point)
    private List<Vector2Int> waypoints = new List<Vector2Int>();
    // Points defining the curved path
    private List<Vector2> pathPoints = new List<Vector2>();
    // Flag to prevent multiple generations simultaneously
    private bool isGenerating = false;
    #endregion

    /*
	 * Generate the map on start, and on mouse click
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

    /*
     * Main coroutine to generate the map in stages.
     * Each stage is separated for clarity and can be visualised step-by-step.
     */
    IEnumerator GenerateMap()
    {
        isGenerating = true;
        Debug.Log("Generating Map: " + isGenerating);

        // Stage 0: initialise map variables
        map = new int[mapWidth, mapHeight];
        path.Clear();
        patchCenters.Clear();
        waypoints.Clear();
        pathPoints.Clear();
        ClearSpawnObjects();
        StartRandomSeed();

        // Stage 1: populate the grid cells
        yield return StartCoroutine(PopulateMap());

        // Stage 2: apply cellular automata rules
        for (int i = 0; i < smoothening; i++)
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

    /*
     * Destroy all spawned GameObjects (e.g., point markers).
     */
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
     * Initialise the random number generator with the seed.
     * If useRandomSeed is true, a new seed is generated based on system time.
     */
    void StartRandomSeed()
    {
        if (useRandomSeed)
        {
            seed = Time.time.ToString();
        }

        pseudoRandom = new System.Random(seed.GetHashCode());
    }

    /*
     * Generate the mesh for rendering the map.
     */
    void GenerateMesh()
    {
        MeshGenerator meshGen = GetComponent<MeshGenerator>();
        if (meshGen != null)
            meshGen.GenerateMesh(map, 1);
    }

    /*
	 * STAGE 1: Populate the map with initial forest, start/end points, and clear patches.
	 */
    IEnumerator PopulateMap()
    {
        // Fill the map with a random forest based on randomFillPercent
        yield return StartCoroutine(ForestFill());
        GenerateMesh();
        yield return new WaitForSeconds(stepDelay);

        // Determine the start and end points for the path
        yield return StartCoroutine(StartAndEndPoint());
        GenerateMesh();
        yield return new WaitForSeconds(stepDelay);

        // Create clear patches in the forest for variety
        yield return StartCoroutine(CreatePatches());
        GenerateMesh();
        yield return new WaitForSeconds(stepDelay);
    }

    /*
     * Fill the map with a random forest.
     * Edge cells are always trees, inner cells are trees based on randomFillPercent.
     */
    IEnumerator ForestFill()
    {
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                if (x == 0 || x == mapWidth - 1 || y == 0 || y == mapHeight - 1)
                {
                    map[x, y] = 1; // Edge cells are always trees
                }
                else
                {
                    map[x, y] = (pseudoRandom.Next(0, 100) < randomFillPercent) ? 1 : 0;
                }
            }
        }
        yield return null;
    }

    /*
     * Determine start and end points for the path.
     * Start point is on a random edge; end point is in an opposite quadrant.
     */
    IEnumerator StartAndEndPoint()
    {
        // Randomly select an edge for the start point (0=left, 1=right, 2=top, 3=bottom)
        int edge = pseudoRandom.Next(0, 4);

        switch (edge)
        {
            case 0: // Left edge
                startPoint = new Vector2Int(checkPointOffset, pseudoRandom.Next(checkPointOffset, mapHeight - checkPointOffset));
                break;
            case 1: // Right edge
                startPoint = new Vector2Int(mapWidth - 1 - checkPointOffset, pseudoRandom.Next(checkPointOffset, mapHeight - checkPointOffset));
                break;
            case 2: // Top edge
                startPoint = new Vector2Int(pseudoRandom.Next(checkPointOffset, mapWidth - checkPointOffset), mapHeight - 1 - checkPointOffset);
                break;
            case 3: // Bottom edge
                startPoint = new Vector2Int(pseudoRandom.Next(checkPointOffset, mapWidth - checkPointOffset), checkPointOffset);
                break;
        }

        // Determine if start point is on left or bottom half
        bool isLeft = startPoint.x < mapWidth / 2;
        bool isBottom = startPoint.y < mapHeight / 2;

        // Choose end point in a quadrant opposite to start point
        if (isLeft && isBottom)
            endPoint = new Vector2Int(pseudoRandom.Next(mapWidth / 2, mapWidth - 2), pseudoRandom.Next(mapHeight / 2, mapHeight - 2));

        else if (!isLeft && !isBottom)
            endPoint = new Vector2Int(pseudoRandom.Next(2, mapWidth / 2), pseudoRandom.Next(2, mapHeight / 2));

        else if (isLeft && !isBottom)
            endPoint = new Vector2Int(pseudoRandom.Next(mapWidth / 2, mapWidth - 2), pseudoRandom.Next(2, mapHeight / 2));

        else if (!isLeft && isBottom)
            endPoint = new Vector2Int(pseudoRandom.Next(2, mapWidth / 2), pseudoRandom.Next(mapHeight / 2, mapHeight - 2));

        // Place visual markers
        PlaceGameObject(pointPrefab, startPoint, Color.black);
        yield return new WaitForSeconds(stepDelay);
        PlaceGameObject(pointPrefab, endPoint, Color.red);
    }

    /*
     * Instantiate a GameObject at a map point and clear the surrounding area.
     * Used for Start and end point
     */
    void PlaceGameObject(GameObject prefab, Vector2Int point, Color color)
    {
        Vector3 pos = new Vector3((point.x - mapWidth / 2), 0f, (point.y - mapHeight / 2));

        GameObject newObj = Instantiate(prefab, pos, Quaternion.Euler(90f, 0f, 0f));
        spawnedObjects.Add(newObj);

        Renderer renderer = newObj.GetComponent<Renderer>();
        if (renderer != null)
            renderer.material.color = color;

        ClearForest(point, 3); // Clear small area around point
    }

    /*
     * Create clear patches in the forest.
     * Patches are circular areas cleared of trees.
     */
    IEnumerator CreatePatches()
    {
        for (int i = 0; i < numPatches; i++)
        {
            int x = pseudoRandom.Next(2, mapWidth - 2);
            int y = pseudoRandom.Next(2, mapHeight - 2);

            Vector2Int center = new Vector2Int(x, y);

            // Skip patch if too close to start or end points
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

    /*
     * Clear a circular area of trees centered at pCenter with given radius.
     */
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

    /*
	 * STAGE 2: Smooth the map using cellular automata rules to create natural forest shapes.
	 */
    IEnumerator SmoothMap()
    {
        int[,] newMap = new int[mapWidth, mapHeight];

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                int neighbourTreeTiles = GetSurroundingTreeCount(x, y);
                // Rules: >4 tree neighbours = tree, <3 = path, else unchanged
                if (neighbourTreeTiles > 4)
                    newMap[x, y] = 1;
                else if (neighbourTreeTiles < 3)
                    newMap[x, y] = 0;
                else
                    newMap[x, y] = map[x, y];
            }
        }

        map = newMap;
        yield return null;

    }

    /*
     * Count tree neighbours (including diagonals) for a cell.
     * Out-of-bounds cells are considered trees.
     */
    int GetSurroundingTreeCount(int gridX, int gridY)
    {
        int treeCount = 0;
        for (int neighbourX = gridX - 1; neighbourX <= gridX + 1; neighbourX++)
        {
            for (int neighbourY = gridY - 1; neighbourY <= gridY + 1; neighbourY++)
            {
                if (IsInMapRange(neighbourX, neighbourY))
                {
                    if (neighbourX != gridX || neighbourY != gridY)
                    {
                        treeCount += map[neighbourX, neighbourY];
                    }
                }
                else
                {
                    treeCount++;
                }
            }
        }

        return treeCount;
    }

    /*
	 * Stage 3: Finalise the map by generating paths between points.
	 */
    IEnumerator ProcessMap()
    {
        // Generate paths connecting start point, patch centers, and end point
        yield return StartCoroutine(GeneratePathing());
    }

    /*
     * Generate paths by connecting waypoints (patch centers and end point) starting from startPoint.
     */
    IEnumerator GeneratePathing()
    {
        waypoints.AddRange(path);
        waypoints.Add(endPoint);

        Vector2Int currentPoint = startPoint;
        List<Vector2Int> visited = new List<Vector2Int> { currentPoint };

        // Connect to nearest unvisited waypoint until all are connected
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

    /*
     * Draw a curved path by clearing cells from 'from' to 'to'.
     * Widens path by randomly clearing one of the neighbour.
     */
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

                    Vector2Int[] neighbours = new Vector2Int[]
                    {
                        new Vector2Int(x + 1, y),
                        new Vector2Int(x - 1, y),
                        new Vector2Int(x, y + 1),
                        new Vector2Int(x, y - 1)
                    };

                    int neighbourIndex = pseudoRandom.Next(0, neighbours.Length);
                    Vector2Int neighbour = neighbours[neighbourIndex];

                    if (IsInMapRange(neighbour.x, neighbour.y))
                        map[neighbour.x, neighbour.y] = 0;
                }
            }
        }
        yield return null;
    }

    /*
     * Recursively generate a curved path using midpoint displacement for natural curves.
     * This function is enhanced with the assistance of AI-based tool
     */
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

        midpoint.x = Mathf.Clamp(midpoint.x, 1, mapWidth - 2);
        midpoint.y = Mathf.Clamp(midpoint.y, 1, mapHeight - 2);

        GenerateCurvedPath(from, midpoint, segments - 1, maxDeviation * 0.5f, pathPoints);
        GenerateCurvedPath(midpoint, to, segments - 1, maxDeviation * 0.5f, pathPoints);
    }

    /*
     * Add a border of trees around the map to enclose it.
     */
    IEnumerator AddMapBorder()
    {
        int[,] borderedMap = new int[mapWidth + mapBorderSize * 2, mapHeight + mapBorderSize * 2];

        for (int x = 0; x < borderedMap.GetLength(0); x++)
        {
            for (int y = 0; y < borderedMap.GetLength(1); y++)
            {
                if (x >= mapBorderSize && x < mapWidth + mapBorderSize && y >= mapBorderSize && y < mapHeight + mapBorderSize)
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

    /*
     * Check if coordinates are within map bounds.
     */
    bool IsInMapRange(int x, int y)
    {
        return (x >= 0 && x < mapWidth && y >= 0 && y < mapHeight);
    }

}