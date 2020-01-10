using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    protected static float GetPerlinValue(int x, int y, float scale, float offset)
    {
        return Mathf.PerlinNoise((x + offset) * scale, (y + offset) * scale);
    }

    public void RandomObstacles()
    {
        var grid = GridController.Ground;
        var sizeX = GridController.active.size.x;
        var sizeY = GridController.active.size.y;
        var obstacleSlider = GameObject.Find("ObstacleSlider").GetComponent<Slider>();

        for (var x = 0; x < sizeX; x++)
            for (var y = 0; y < sizeY; y++)
            {
                var perlin = GetPerlinValue(x, y, 0.25f, 100000f);
                if (perlin < obstacleSlider.value)
                {
                    grid.SetTile(new Vector3Int(x, y, 0), GridController.active.blocked);
                }
                else
                {
                    grid.SetTile(new Vector3Int(x, y, 0), GridController.active.walkable);
                }
            }
    }

    public void FindPath(bool astar)
    {
        DebugDrawer.Clear();

        // Convert visible grid to memory grid
        var grid = GridController.Ground;
        var pathGrid = GridController.Path;

        var sizeX = GridController.active.size.x;
        var sizeY = GridController.active.size.y;

        var memoryGrid = new GridGraph();

        memoryGrid.AStar = astar;
        memoryGrid.InitializeGrid(sizeX, sizeY);

        // Set grid weights
        var start = new Vector2Int(-1, -1);
        var end = new Vector2Int(-1, -1);

        for (var x = 0; x < sizeX; x++)
            for (var y = 0; y < sizeY; y++)
            {
                var tile = grid.GetTile(new Vector3Int(x, y, 0));
                var index = memoryGrid.gridToArrayPos(x, y);
                if (tile == GridController.active.blocked)
                {
                    memoryGrid.Weights[index] = 0;
                }
                else if (tile == GridController.active.road)
                {
                    memoryGrid.Weights[index] = 1;
                }
                else if (tile == GridController.active.walkable)
                {
                    memoryGrid.Weights[index] = 2;
                }

                var pathTile = pathGrid.GetTile(new Vector3Int(x, y, 0));
                if (pathTile != null)
                {
                    if (pathTile == GridController.active.start)
                    {
                        start = new Vector2Int(x, y);
                    }
                    else if (pathTile == GridController.active.end)
                    {
                        end = new Vector2Int(x, y);
                    }
                }
            }

        if (start.x == -1 || end.x == -1)
        {
            Debug.Log("Couldn't find any start or end position");
            return;
        }

        List<Node> path;
        Node startNode = memoryGrid.GetNodeFromIndex(start.x, start.y);
        Node targetNode = memoryGrid.GetNodeFromIndex(end.x, end.y);
        if (astar)
        {
            var asearch = new AStarSearch();
            path = asearch.GetPath(memoryGrid, startNode, targetNode);
        }
        else
        {
            var jpsearch = new JumpPointSearch();
            path = jpsearch.GetPath(memoryGrid, startNode, targetNode);
        }

        if (path != null)
        {
            foreach (var pathTile in path)
            {
                if (pathTile.x == start.x && pathTile.y == start.y)
                    continue;
                if (pathTile.x == end.x && pathTile.y == end.y)
                    continue;

                if (pathTile.parent != null)
                {
                    DebugDrawer.Draw(new Vector2Int(pathTile.parent.x, pathTile.parent.y), new Vector2Int(pathTile.x, pathTile.y), Color.blue);
                }
                DebugDrawer.DrawCube(new Vector2Int(pathTile.x, pathTile.y), Vector2Int.one, Color.blue);
            }
        }

        // Return nodes to pool
        memoryGrid.Reset();
    }
}