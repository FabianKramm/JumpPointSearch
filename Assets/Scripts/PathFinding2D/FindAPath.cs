#define DEBUG_PATHFINDING

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Diagnostics;

public class FindAPath
{
    public const float DiagonalCost = 1.4142135623730950488016887242097f; // sqrt(2)
    public const float LateralCost = 1.0f;
    
    private Node _startNode;
    private Node _targetNode;

    private GridGraph _grid;

    private MinHeap<Node, float> openSet;
    private HashSet<Node> openSetContainer;
    private HashSet<Node> closedSet;

    public List<Node> GetPath(GridGraph graph, Node startNode, Node targetNode)
    {
        _startNode = startNode;
        _targetNode = targetNode;
        _grid = graph;

        _Initialize();
        Stopwatch sw = new Stopwatch();
        sw.Start();
        if (_CalculateShortestPath())
        {
            sw.Stop();
            TimeSpan ts = sw.Elapsed;
            UnityEngine.Debug.Log("A* Path - Path found in : " + ts.Milliseconds + " ms");
            return _RetracePath();
        }
        else
        {
            sw.Stop();
            TimeSpan ts = sw.Elapsed;
            UnityEngine.Debug.Log("A* Path - No path found in : " + ts.Milliseconds + " ms");
            return null;
        }
    }

    private void _Initialize()
    {
        openSet = new MinHeap<Node, float>();
        openSetContainer = new HashSet<Node>();
        closedSet = new HashSet<Node>();
    }

    private bool _CalculateShortestPath()
    {
        Node currentNode;

        openSet.Add(_startNode, 0);
        openSetContainer.Add(_startNode);

        while (openSet.Count > 0)
        {
            currentNode = openSet.Remove();

            if (currentNode == _targetNode)
                return true;
            if (closedSet.Contains(currentNode))
                continue;

            openSetContainer.Remove(currentNode);
            closedSet.Add(currentNode);

            foreach (Node neighbour in _grid.GetAStarNeighbours(currentNode))
            {
                if (!_grid.IsWalkable(neighbour.x, neighbour.y) || closedSet.Contains(neighbour))
                    continue;

#if DEBUG_PATHFINDING
                    DebugDrawer.Draw(new Vector2Int(currentNode.x, currentNode.y), new Vector2Int(neighbour.x, neighbour.y), Color.white);
                    DebugDrawer.DrawCube(new Vector2Int(neighbour.x, neighbour.y), Vector2Int.one, Color.white);
#endif

                int newGCost = currentNode.gCost + _GetDistance(currentNode, neighbour);
                if (newGCost < neighbour.gCost || !openSetContainer.Contains(neighbour))
                {
                    neighbour.gCost = newGCost;
                    neighbour.hCost = _GetDistance(neighbour, _targetNode);
                    neighbour.parent = currentNode;

                    openSet.Add(neighbour, neighbour.fCost);
                    if (!openSetContainer.Contains(neighbour))
                    {
                        openSetContainer.Add(neighbour);
                    }
                }
            }
        }
        return false;
    }

    private List<Node> _RetracePath()
    {
        List<Node> path = new List<Node>();
        Node currentNode = _targetNode;

        while (currentNode != _startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }
        path.Reverse();
        return path;
    }

    private int _GetDistance(Node a, Node b)
    {
        var dx = Math.Abs(a.x - b.x);
        var dy = Math.Abs(a.y - b.y);

        return (int)(LateralCost * (dx + dy) + (DiagonalCost - 2 * LateralCost) * Math.Min(dx, dy));
    }
}