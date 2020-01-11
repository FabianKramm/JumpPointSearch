#define DEBUG_PATHFINDING

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Diagnostics;

public class AStarSearch
{
    public const float DiagonalCost = 1.4142135623730950488016887242097f; // sqrt(2)
    public const float LateralCost = 1.0f;

    public bool showDebug = true;
    
    private Node startNode;
    private Node targetNode;

    private GridGraph grid;

    private MinHeap<Node, float> openSet;
    private HashSet<Node> openSetContainer;
    private HashSet<Node> closedSet;

    public List<Node> GetPath(GridGraph graph, Node startNode, Node targetNode)
    {
        this.startNode = startNode;
        this.targetNode = targetNode;
        grid = graph;

        Initialize();
        if (CalculateShortestPath())
        {
            return RetracePath();
        }

        return null;
    }

    private void Initialize()
    {
        openSet = new MinHeap<Node, float>();
        openSetContainer = new HashSet<Node>();
        closedSet = new HashSet<Node>();
    }

    private bool CalculateShortestPath()
    {
        Node currentNode;

        openSet.Add(startNode, 0);
        openSetContainer.Add(startNode);

        while (openSet.Count > 0)
        {
            currentNode = openSet.Remove();

            if (currentNode == targetNode)
                return true;
            if (closedSet.Contains(currentNode))
                continue;

            openSetContainer.Remove(currentNode);
            closedSet.Add(currentNode);

            foreach (Node neighbour in grid.GetAStarNeighbours(currentNode))
            {
                if (!grid.IsWalkable(neighbour.x, neighbour.y) || closedSet.Contains(neighbour))
                    continue;

#if DEBUG_PATHFINDING
                if (showDebug)
                {
                    DebugDrawer.Draw(new Vector2Int(currentNode.x, currentNode.y), new Vector2Int(neighbour.x, neighbour.y), Color.white);
                    DebugDrawer.DrawCube(new Vector2Int(neighbour.x, neighbour.y), Vector2Int.one, Color.white);
                }
#endif

                int newGCost = currentNode.gCost + GetDistance(currentNode, neighbour);
                if (newGCost < neighbour.gCost || !openSetContainer.Contains(neighbour))
                {
                    neighbour.gCost = newGCost;
                    neighbour.hCost = GetDistance(neighbour, targetNode);
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

    private List<Node> RetracePath()
    {
        List<Node> path = new List<Node>();
        Node currentNode = targetNode;

        while (currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }
        path.Reverse();
        return path;
    }

    private int GetDistance(Node a, Node b)
    {
        var dx = Math.Abs(a.x - b.x);
        var dy = Math.Abs(a.y - b.y);

        return (int)(LateralCost * (dx + dy) + (DiagonalCost - 2 * LateralCost) * Math.Min(dx, dy));
    }
}