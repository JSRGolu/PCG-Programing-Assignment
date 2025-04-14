# Procedural Forest Generator for Unity

This Unity project generates a forest levels with dense trees, clearings, and winding paths, with imagined gameplay in which the player is an adventurer seeking the entrance to the Lost Forest Temple and Level design of a dense forest containing paths and clearings. It is built using two core scripts: MapGenerator.cs for creating the forest layout and MeshGenerator.cs for converting it into a 3D mesh for visualisation.

## Design and Output

The generator creates a forest level through a multi-step procedural process:
* **Forest Generation:** The map starts as a 2D grid where trees (represented as 1) are randomly placed based on a fill percentage.
* **Start and End Points:** The adventurer's starting location is placed along one edge of the map, while the temple entrance (end point) is positioned in an opposite quadrant, with small clearings around each.
* **Clearings:** Circular patches of trees are removed to create open areas (0), ensuring variety in the forest's density.
* **Smoothing:** Cellular automata rules are then applied over multiple smoothing iterations to form natural clusters of trees and open spaces
* **Paths:** Curved paths are generated between key points (start, end, and intermediate waypoints) with controlled deviation, forming natural trails that connect the adventurer's starting point to the temple.
* **Mesh Creation:** The 2D grid is transformed into a 3D mesh by MeshGenerator.cs for the visualisation of the generated map.
The output is a navigable forest environment with dense tree clusters, open clearings, and winding paths, all enclosed by a border of walls.

## Usage Instructions

### Prerequisites

* Unity Hub (recommended)
* Unity (2023.1.13f1)

### Installation

* Open Unity Hub
* Click on `Add project from disk`
* Navigate to the project folder and select it

### How to Use

Once you’ve opened the project in Unity, here’s how to get started:

* Navigate to Assets/Main.unity and open it.
* Press Play to run the scene in the Unity Editor.
* Additionally, you can play with parameters on `MapGenerator` gameobject

## Best Settings

The following variable values in MapGenerator.cs are used for generating  best output for a balance forest level.

| Parameter | Values |
| - |:-:|
| `mapBorderSize` |  `2` |
| `checkPointOffset` | `3` |
| `randomFillPercent` | `55` |
| `numPatches` | `30` |
| `patchRadius` | `4` |
| `smoothening` | `2` |
| `maxDeviation` | `8` |
| `segments` | `3` |

These settings are optional yet ideal produce a dense forest with a clear path network and sufficient open areas for the adventurer’s journey to the temple.

### Credits and References
* This project is adapted from the "Procedural Cave Generation" tutorial series by Sebastian Lague: [**YouTube Playlist**](https://www.youtube.com/watch?v=v7yyZZjF1z4&list=PLFt_AvWsXl0eZgMK_DT5_biRkWXftAOf9)
* The design was inspired by the provided brief, tailored to create a forest level for an adventurer seeking the Lost Forest Temple.