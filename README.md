# Procedural Forest Generator for Unity

This Unity project generates a forest levels with dense trees, clearings, and winding paths, with imagined gameplay in which the player is an adventurer seeking the entrance to the Lost Forest Temple and Level design of a dense forest containing paths and clearings. It is built using two core scripts: MapGenerator.cs for creating the forest layout and MeshGenerator.cs for converting it into a 3D mesh for visualisation.

## Working and Output

The generator creates a forest level through a multi-stage procedural process:

1. **Stage 0: Initilisation**  
    This stage is responsible for setting up the environment and variables needed for map generation.
    * **Creates a New Map Grid:** A 2D array (`map`) is initialised with dimensions `mapWidth` by `mapHeight` to store the grid data.
    * **Clears Previous Data:** Several lists—such as `path`, `patchCenters`, `waypoints`, and `pathPoints`—are emptied to ensure no residual data affects the new map.
    * **Removes Spawned Objects:** Any previously spawned objects in the scene are destroyed to start with a clean slate.
    * **Sets Up Random Seed:** The random number generator (`pseudoRandom`) is initialised with a seed (either user-defined or randomly generated), ensuring procedural yet reproducible map generation.

2. **Stage 1: Populate the Map Grid**  
    This stage focuses on filling the map grid with initial content, creating the basic layout that will be refined in later stages. It consists of three main steps, executed sequentially:
    * **Forest Fill:** The grid is populated with trees and empty spaces. Cells along the edges are set as trees (`1`) to form a boundary, while inner cells are assigned as trees or empty spaces (`0`) based on a probability defined by `randomFillPercent`.
    * **Start and End Points:** The `startPoint` is randomly placed along one of the four edges (left, right, top, or bottom), offset from the edge by `checkPointOffset`. While, the `endPoint` is positioned in a quadrant opposite the start point to maximize the distance between them, encouraging a longer path. Visual markers are placed at these points, and small areas around them are cleared to ensure they are not blocked by trees.
    * **Create Patches:** A specified number of circular clear areas (`numPatches`) are generated within the forest. Patch locations are chosen to avoid overlapping with the start and end points, and cells within each patch’s radius are set to empty (`0`) to create open spaces.

3. **Stage 2: Smooth the Map**  
    The smoothing stage refines the initial forest layout to produce more natural and cohesive shapes, reducing the randomness of the initial fill.
    * **Smoothing:** Cellular automata rules are applied over a set number of iterations (`smoothening`):
        * For each cell, the number of neighbouring trees (including diagonals) is counted.
        * If a cell has more than 4 tree neighbours, it becomes a tree (`1`).
        * If it has fewer than 3 tree neighbours, it becomes empty (`0`).
        * Otherwise, the cell retains its original state.

4. **Stage 3: Finalise the map**  
    This stage completes the map by adding navigable paths between key points and enclosing the entire layout with a border of trees.
    * **Generate Paths:** A nearest-neighbour approach connects the start point to the nearest patch center, then to the next nearest, and so on, until reaching the end point. Each connection uses recursive midpoint displacement to create a curved, natural-looking path rather than a straight line. Cells along the path are cleared (set to `0`), and some neighbouring cells are randomly cleared to widen the path slightly for better pathing.
    * **Add Border:** The map is expanded by adding a tree border around its perimeter, with a thickness defined by `mapBorderSize`.

Together, these stages transform a blank grid into a navigable forest environment with dense tree clusters, open clearings, and winding paths.

## Usage Instructions

### Prerequisites

* Unity Hub
* Unity Editor (2023.1.13f1)

### Installation

* Open Unity Hub
* Click on `Add project from disk`
* Navigate to the project folder and select it

### How to Use

Once you’ve opened the project in Unity, here’s how to get started:

* Navigate to Assets/Main.unity and open it.
* Press Play to run the scene in the Unity Editor.
* Additionally, you can play with parameters of `MapGenerator.cs` which is attached to `MapGenerator` gameobject.

## Best Settings

The following variable values in MapGenerator.cs are used for generating  best output for a balance forest level.

| Parameter | Values |
| - |:-:|
| `mapWidth` | `126` |
| `mapHeight` | `70` |
| `mapBorderSize` |  `2` |
| `checkPointOffset` | `3` |
| `randomFillPercent` | `55` |
| `numPatches` | `25` |
| `patchRadius` | `4` |
| `smoothening` | `2` |
| `maxDeviation` | `10` |
| `segments` | `4` |

These settings are optional yet ideal to produce a dense forest with a clear path network and sufficient open areas for the adventurer’s journey to the temple.

### Credits and References
* This project is adapted from the "Procedural Cave Generation" tutorial series by Sebastian Lague: [**YouTube Playlist**](https://www.youtube.com/watch?v=v7yyZZjF1z4&list=PLFt_AvWsXl0eZgMK_DT5_biRkWXftAOf9)
* The implementation of the midpoint displacement algorithm used in `GenerateCurvedPath` was enhanced with the assistance of AI-based tool, which provided insights into improving code clarity.