using UnityEngine;

public class MapVisualiser : MonoBehaviour
{
    private SpriteRenderer[,] gridCells;
    [SerializeField] private float scaleMultiplier = 1f; // Adjust in Inspector to increase sprite size

    public void VisualizeGrid(int[,] map, float squareSize)
    {
        // Clear any existing grid
        ClearGrid();

        int width = map.GetLength(0);
        int height = map.GetLength(1);

        // Initialize the gridCells array
        gridCells = new SpriteRenderer[width, height];

        // Calculate the starting position (center the grid in the world)
        float startX = -width * squareSize / 2f + squareSize / 2f;
        float startZ = -height * squareSize / 2f + squareSize / 2f;

        // Create sprite objects for each grid cell
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                // Calculate the position for this grid cell on the ZX plane
                Vector3 position = new Vector3(startX + x * squareSize, 0, startZ + z * squareSize);

                // Create a new GameObject with a SpriteRenderer
                GameObject cellObj = new GameObject($"Cell_{x}_{z}");
                cellObj.transform.position = position;
                cellObj.transform.parent = transform;

                // Rotate the sprite to lie on the ZX plane (facing Y-axis)
                cellObj.transform.rotation = Quaternion.Euler(90, 0, 0);

                SpriteRenderer spriteRenderer = cellObj.AddComponent<SpriteRenderer>();

                // Create a 1x1 white square sprite
                spriteRenderer.sprite = CreateSquareSprite();

                // Set the sprite's scale to match squareSize * scaleMultiplier
                cellObj.transform.localScale = new Vector3(squareSize * scaleMultiplier, squareSize * scaleMultiplier, 1);

                // Set color based on map value (0 = black, 1 = white)
                spriteRenderer.color = map[x, z] == 0 ? Color.black : Color.white;

                gridCells[x, z] = spriteRenderer;
            }
        }
    }

    private Sprite CreateSquareSprite()
    {
        // Create a 1x1 texture (smallest possible for a sprite)
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        // Create a sprite from the texture
        return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
    }

    private void ClearGrid()
    {
        // Destroy all existing cell objects
        if (gridCells != null)
        {
            for (int x = 0; x < gridCells.GetLength(0); x++)
            {
                for (int z = 0; z < gridCells.GetLength(1); z++)
                {
                    if (gridCells[x, z] != null)
                    {
                        Destroy(gridCells[x, z].gameObject);
                    }
                }
            }
        }
        gridCells = null;
    }

    // Optional: Call this to update the grid if the map changes
    public void UpdateGrid(int[,] map)
    {
        if (gridCells == null || gridCells.GetLength(0) != map.GetLength(0) || gridCells.GetLength(1) != map.GetLength(1))
        {
            VisualizeGrid(map, 1f); // Rebuild the grid if dimensions don't match
            return;
        }

        // Update cell colors
        for (int x = 0; x < map.GetLength(0); x++)
        {
            for (int z = 0; z < map.GetLength(1); z++)
            {
                if (gridCells[x, z] != null)
                {
                    gridCells[x, z].color = map[x, z] == 0 ? Color.black : Color.white;
                }
            }
        }
    }
}