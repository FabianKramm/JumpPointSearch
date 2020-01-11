#define DEBUG_PATHFINDING

using UnityEngine;
using System.Collections.Generic;
using System;

public class AStarSearch
{
    public const float DiagonalCost = 1.4142135623730950488016887242097f; // sqrt(2)
    public const float LateralCost = 1.0f;

    public bool showDebug = true;

    protected Node startNode;
    protected Node targetNode;

    protected GridGraph grid;

    protected MinHeap<Node, float> heap;
    protected Dictionary<int, Node> nodes;

    public List<Node> GetPath(GridGraph grid, Vector2Int start, Vector2Int target)
    {
        this.grid = grid;
        heap = new MinHeap<Node, float>();
        nodes = new Dictionary<int, Node>();
        startNode = GetNodeFromIndexUnchecked(start.x, start.y);
        targetNode = GetNodeFromIndexUnchecked(target.x, target.y);

        if (CalculateShortestPath())
        {
            return RetracePath();
        }

        return null;
    }

    private bool CalculateShortestPath()
    {
        Node currentNode;
        Position[] neighbors = new Position[8];

        heap.Add(startNode, 0);
        while (heap.Count > 0)
        {
            currentNode = heap.Remove();
            if (currentNode == targetNode)
                return true;

            currentNode.setClosed();
            var count = GetNeighbors(currentNode, neighbors);
            for (var i = 0; i < count; i++)
            {
                var neighbor = neighbors[i];
                var neighborNode = GetNeighborNode(currentNode, neighbor);
                if (neighborNode == null || neighborNode.isClosed())
                    continue;

                int newGCost = currentNode.gCost + GetDistance(currentNode, neighborNode); // * grid.Weights[grid.gridToArrayPos(neighborNode.x, neighborNode.y)];
                if (newGCost < neighborNode.gCost || !neighborNode.isOpen())
                {
#if DEBUG_PATHFINDING
                    if (showDebug)
                    {
                        DebugDrawer.Draw(new Vector2Int(currentNode.x, currentNode.y), new Vector2Int(neighborNode.x, neighborNode.y), Color.white);
                        DebugDrawer.DrawCube(new Vector2Int(neighborNode.x, neighborNode.y), Vector2Int.one, Color.white);
                    }
#endif

                    neighborNode.gCost = newGCost;
                    neighborNode.hCost = GetDistance(neighborNode, targetNode);
                    neighborNode.parent = currentNode;

                    if (!neighborNode.isOpen())
                    {
                        heap.Add(neighborNode, neighborNode.gCost + neighborNode.hCost);
                        neighborNode.setOpen();
                    }
                    else
                    {
                        heap.Update(neighborNode, neighborNode.gCost + neighborNode.hCost);
                    }
                }
            }
        }
        return false;
    }

    protected virtual Node GetNeighborNode(Node currentNode, Position neighbor)
    {
        return GetNodeFromIndexUnchecked(neighbor.x, neighbor.y);
    }

    protected virtual int GetNeighbors(Node currentNode, Position[] neighbors)
    {
        return grid.GetAStarNeighbours(currentNode, neighbors);
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

    protected Node GetNodeFromIndexUnchecked(int x, int y)
    {
        var arrayPos = grid.gridToArrayPos(x, y);
        if (nodes.TryGetValue(arrayPos, out Node node))
            return node;

        nodes[arrayPos] = new Node(x, y);
        // grid[arrayPos] = NodePool.NewNode(x, y);
        return nodes[arrayPos];
    }

    protected Node GetNodeFromIndex(int x, int y)
    {
        if (!grid.IsWalkable(x, y))
            return null;

        return GetNodeFromIndexUnchecked(x, y);
    }

    private int GetDistance(Node a, Node b)
    {
        var dx = Math.Abs(a.x - b.x);
        var dy = Math.Abs(a.y - b.y);

        return (int)(LateralCost * (dx + dy) + (DiagonalCost - 2 * LateralCost) * Math.Min(dx, dy));
    }
}