using MultiLevelPathfinding;
using Pathfinding;
using System.Collections.Generic;
using System.Threading;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{   
    protected static float GetPerlinValue(int x, int y, float scale, float offset)
    {
        return Mathf.PerlinNoise((x + offset) * scale, (y + offset) * scale);
    }

    public void RandomStartEnd()
    {
        GridController.Path.ClearAllTiles();
        var sizeX = GridController.active.size.x;
        var sizeY = GridController.active.size.y;

        var r = new System.Random();
        Vector3Int start;
        Vector3Int end;
        var i = 0;
        do
        {
            start = new Vector3Int(r.Next(0, sizeX), r.Next(0, sizeY), 0);
            end = new Vector3Int(r.Next(0, sizeX), r.Next(0, sizeY), 0);
            i++;
            if (i > 100)
                return;

        } while (GridController.Ground.GetTile(start) == GridController.active.blocked || GridController.Ground.GetTile(end) == GridController.active.blocked);

        GridController.Path.SetTile(start, GridController.active.start);
        GridController.Path.SetTile(end, GridController.active.end);
    }

    public void RandomRoads()
    {
        var grid = GridController.Ground;
        var sizeX = GridController.active.size.x;
        var sizeY = GridController.active.size.y;
        var roadsSlider = GameObject.Find("RoadsSlider").GetComponent<Slider>();
        var roadsAmount = sizeX * roadsSlider.value * 0.1f;

        for (var x = 0; x < sizeX; x++)
            for (var y = 0; y < sizeY; y++)
                if (grid.GetTile(new Vector3Int(x, y, 0)) == GridController.active.road)
                {
                    grid.SetTile(new Vector3Int(x, y, 0), GridController.active.walkable);
                }

        for (var roads = 0; roads < roadsAmount; roads++)
        {
            Vector3Int roadPoint = (Random.Range(0f, 1f) > 0.5f) ? new Vector3Int(Random.Range(0, sizeX - 1), 0, 0) : new Vector3Int(0, Random.Range(0, sizeY - 1), 0);
            bool up = roadPoint.y == 0;

            while(roadPoint.x >= 0 && roadPoint.x < sizeX && roadPoint.y >= 0 && roadPoint.y < sizeY)
            {
                grid.SetTile(roadPoint, GridController.active.road);
                if (up)
                {
                    grid.SetTile(roadPoint + new Vector3Int(-1, 0, 0), GridController.active.road);
                }
                else
                {
                    grid.SetTile(roadPoint + new Vector3Int(1, 0, 0), GridController.active.road);
                }

                roadPoint += up ? new Vector3Int(Random.Range(-1, 2), Random.Range(0, 2), 0) : new Vector3Int(Random.Range(0, 2), Random.Range(-1, 2), 0);
            }
        }
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
                else if (grid.GetTile(new Vector3Int(x, y, 0)) == GridController.active.blocked)
                {
                    grid.SetTile(new Vector3Int(x, y, 0), GridController.active.walkable);
                }
            }
    }

    public void Clear()
    {
        DebugDrawer.Clear();
    }

    public void FindMultiLevelPath()
    {
        DebugDrawer.Clear();
        var pathGrid = GridController.Path;
        var memoryGrid = createGraph();

        var sizeX = GridController.active.size.x;
        var sizeY = GridController.active.size.y;

        var graph = new OverlayGraph(memoryGrid);

        graph.BuildChunk(0);
        graph.DrawCellBorders();
        graph.DrawOverlayGraph(1);

        // graph.DrawGraph();
        // overlayGraph.DrawGraph();
    }

    public void FindBiDirectionalDijkstraPath()
    {
        DebugDrawer.Clear();
        var pathGrid = GridController.Path;
        var memoryGrid = createGraph();

        var sizeX = GridController.active.size.x;
        var sizeY = GridController.active.size.y;
        // Set grid weights
        var start = new Vector2Int(-1, -1);
        var end = new Vector2Int(-1, -1);

        for (var x = 0; x < sizeX; x++)
            for (var y = 0; y < sizeY; y++)
            {
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

        // List<Node> path;
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        sw.Start();

        List<BiDirectionalDijkstraSearch.Node> path;
        var biDirectionalDijkstra = new BiDirectionalDijkstraSearch(memoryGrid);
        path = biDirectionalDijkstra.GetPath(start, end);
        UnityEngine.Debug.Log("BiDirectionalDijkstraSearch Path - Path" + (path == null ? " not " : " ") + "found in : " + sw.ElapsedMilliseconds + " ms");

        if (path != null)
        {
            foreach (var pathTile in path)
            {
                if (pathTile.x == start.x && pathTile.y == start.y)
                    continue;
                if (pathTile.x == end.x && pathTile.y == end.y)
                    continue;

                DebugDrawer.DrawCube(new Vector2Int(pathTile.x, pathTile.y), Vector2Int.one, Color.blue);
            }
        }
    }

    public void FindSubGoalDijkstraPath()
    {
        DebugDrawer.Clear();

        // Convert visible grid to memory grid
        var grid = GridController.Ground;
        var pathGrid = GridController.Path;

        var sizeX = GridController.active.size.x;
        var sizeY = GridController.active.size.y;

        var memoryGrid = createGraph();
        // Set grid weights
        var start = new Vector2Int(-1, -1);
        var end = new Vector2Int(-1, -1);

        for (var x = 0; x < sizeX; x++)
            for (var y = 0; y < sizeY; y++)
            {
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

        // List<Node> path;
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        sw.Start();

        SubGoalGrid sgg = new SubGoalGrid(memoryGrid);
        sgg.ConstructSubgoals(0, 0, sizeX, sizeY);
        sgg.ConstructEdges(0, 0, sizeX, sizeY);

        Debug.Log(sgg.subGoals.Count);

        sw.Stop();
        Debug.Log("Preprocessing took " + sw.ElapsedMilliseconds + "ms");
        // sgg.DrawSubGoals();

        List<BiDirectionalDijkstraSearch.Node> path;
        var subGoalSearch = new SubGoalBiDirectionalDijkstraSearch(sgg);
        sw.Restart();
        path = subGoalSearch.GetPath(start, end);
        UnityEngine.Debug.Log("SubGoalSearch Path - Path" + (path == null ? " not " : " ") + "found in : " + sw.ElapsedMilliseconds + " ms");

        if (path != null)
        {
            for (var i = 0; i < path.Count; i++)
            {
                var pathTile = path[i];
                if (pathTile.x == start.x && pathTile.y == start.y)
                    continue;
                if (pathTile.x == end.x && pathTile.y == end.y)
                    continue;

                if (i > 0)
                {
                   DebugDrawer.Draw(new Vector2Int(path[i - 1].x, path[i - 1].y), new Vector2Int(pathTile.x, pathTile.y), Color.blue);
                }

                DebugDrawer.DrawCube(new Vector2Int(pathTile.x, pathTile.y), Vector2Int.one, Color.blue);
            }
        }
    }


    public void FindSubGoalPath()
    {
        DebugDrawer.Clear();

        // Convert visible grid to memory grid
        var grid = GridController.Ground;
        var pathGrid = GridController.Path;

        var sizeX = GridController.active.size.x;
        var sizeY = GridController.active.size.y;

        var memoryGrid = createGraph();
        // Set grid weights
        var start = new Vector2Int(-1, -1);
        var end = new Vector2Int(-1, -1);

        for (var x = 0; x < sizeX; x++)
            for (var y = 0; y < sizeY; y++)
            {
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

        // List<Node> path;
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        sw.Start();

        SubGoalGrid sgg = new SubGoalGrid(memoryGrid);
        sgg.ConstructSubgoals(0, 0, sizeX, sizeY);
        sgg.ConstructEdges(0, 0, sizeX, sizeY);

        Debug.Log(sgg.subGoals.Count);

        sw.Stop();
        Debug.Log("Preprocessing took " + sw.ElapsedMilliseconds + "ms");
        sgg.DrawSubGoals();

        List<Node> path;
        var subGoalSearch = new SubGoalAStarSearch(sgg);
        sw.Restart();
        path = subGoalSearch.GetPath(sgg, start, end);
        UnityEngine.Debug.Log("SubGoalSearch Path - Path" + (path == null ? " not " : " ") + "found in : " + sw.ElapsedMilliseconds + " ms");

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
    }

    public void FindPath(bool astar)
    {
        DebugDrawer.Clear();

        // Convert visible grid to memory grid
        var grid = GridController.Ground;
        var pathGrid = GridController.Path;

        var sizeX = GridController.active.size.x;
        var sizeY = GridController.active.size.y;

        var memoryGrid = createGraph();
        // Set grid weights
        var start = new Vector2Int(-1, -1);
        var end = new Vector2Int(-1, -1);

        for (var x = 0; x < sizeX; x++)
            for (var y = 0; y < sizeY; y++)
            {
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
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        if (astar)
        {
            var asearch = new AStarSearch();
            sw.Start();
            path = asearch.GetPath(memoryGrid, start, end);
            UnityEngine.Debug.Log("A* Path - Path" + (path == null ? " not " : " ") + "found in : " + sw.ElapsedMilliseconds + " ms");
        }
        else
        {
            var jpsearch = new JumpPointSearch();
            sw.Start();
            path = jpsearch.GetPath(memoryGrid, start, end);
            UnityEngine.Debug.Log("JPS Path - Path" + (path == null ? " not " : " ") + "found in : " + sw.ElapsedMilliseconds + " ms");
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
    }

    internal class BenchmarkParameter
    {
        public IGrid grid;
        public int iterations;
    }

    public void Benchmark()
    {
        var t = new Thread(benchmark);
        var memoryGraph = createGraph();

        t.Start(new BenchmarkParameter()
        {
            iterations = 1000,
            grid = memoryGraph,
        });
    }

    private IGrid createGraph()
    {
        // Convert visible grid to memory grid
        var grid = GridController.Ground;
        var pathGrid = GridController.Path;

        var sizeX = GridController.active.size.x;
        var sizeY = GridController.active.size.y;

        var memoryGrid = new ArrayGrid(sizeX, sizeY);
        for (var x = 0; x < sizeX; x++)
            for (var y = 0; y < sizeY; y++)
            {
                var tile = grid.GetTile(new Vector3Int(x, y, 0));
                if (tile == GridController.active.blocked)
                {
                    memoryGrid.SetWeight(x, y, (int)CellType.Blocked);
                }
                else if (tile == GridController.active.road)
                {
                    memoryGrid.SetWeight(x, y, (int)CellType.Road);
                }
                else if (tile == GridController.active.walkable)
                {
                    memoryGrid.SetWeight(x, y, (int)CellType.Walkable);
                }
            }

        return memoryGrid;
    }

    private void benchmark(object parameter)
    {
        var benchmarkParameter = (BenchmarkParameter)parameter;
        var grid = benchmarkParameter.grid;

        float astarTime = 0;
        float jpsTime = 0;

        var stopwatch = new System.Diagnostics.Stopwatch();
        var astar = new AStarSearch();
        astar.showDebug = false;
        var jps = new JumpPointSearch();
        jps.showDebug = false;
        var r = new System.Random(100);
        var size = benchmarkParameter.grid.GetSize();

        for (var i = 0; i < benchmarkParameter.iterations; i++)
        {
            Vector3Int start;
            Vector3Int end;
            do
            {
                start = new Vector3Int(r.Next(0, size.x), r.Next(0, size.y), 0);
                end = new Vector3Int(r.Next(0, size.x), r.Next(0, size.y), 0);
            } while (!grid.IsWalkable(start.x, start.y) || !grid.IsWalkable(end.x, end.y));

            // Run A* Search
            stopwatch.Restart();
            if (astar.GetPath(grid, new Vector2Int(start.x, start.y), new Vector2Int(end.x, end.y)) == null)
                Debug.Log("Astar didn't find path");
            astarTime += stopwatch.ElapsedMilliseconds;

            // Run Jump Point Search
            stopwatch.Restart();
            if (jps.GetPath(grid, new Vector2Int(start.x, start.y), new Vector2Int(end.x, end.y)) == null)
                Debug.Log("JPS didn't find path");
            jpsTime += stopwatch.ElapsedMilliseconds;

            if (i % 100 == 0)
            {
                Debug.Log("Processed " + i + " iterations");
            }
        }

        Debug.Log("Benchmark result (" + benchmarkParameter.iterations + " iterations): AStar took " + astarTime + "ms and JPS took " + jpsTime + "ms");
    }
}