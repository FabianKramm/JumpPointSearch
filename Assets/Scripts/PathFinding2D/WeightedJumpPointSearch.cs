#define DEBUG_PATHFINDING

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System;

public class WeightedJumpPointSearch
{
    public const float DiagonalCost = 1.4142135623730950488016887242097f; // sqrt(2)
    public const float LateralCost = 1.0f;
    public const int MaxJumpPointDistance = 256;

    public bool showDebug = true;

    private Node startNode;
    private Node targetNode;

    private GridGraph grid;

    private MinHeap<Node, float> openSet;
    private HashSet<Node> openSetContainer;
    private HashSet<Node> closedSet;
    private bool _forced;

    public List<Node> GetPath(GridGraph grid, Node startNode, Node targetNode)
    {
        this.startNode = startNode;
        this.targetNode = targetNode;
        this.grid = grid;

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
            if (currentNode == null || closedSet.Contains(currentNode))
                continue;

            openSetContainer.Remove(currentNode);
            closedSet.Add(currentNode);
            List<Node> Nodes = GetSuccessors(currentNode);
            foreach (Node node in Nodes)
            {
                if (closedSet.Contains(node))
                    continue;

#if DEBUG_PATHFINDING
                if (showDebug)
                {
                    DebugDrawer.Draw(new Vector2Int(currentNode.x, currentNode.y), new Vector2Int(node.x, node.y), Color.white);
                    DebugDrawer.DrawCube(new Vector2Int(node.x, node.y), Vector2Int.one, Color.white);
                }
#endif

                var newGCost = currentNode.gCost + GetDistance(currentNode, node);
                if (newGCost < node.gCost || !openSetContainer.Contains(node))
                {
                    node.gCost = newGCost;
                    node.hCost = GetDistance(node, targetNode);
                    node.parent = currentNode;

                    openSet.Add(node, node.fCost);
                    if (!openSetContainer.Contains(node))
                    {
                        openSetContainer.Add(node);
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

    private List<Node> GetSuccessors(Node currentNode)
    {
        Node jumpNode;
        List<Node> successors = new List<Node>();
        List<Node> neighbours = grid.GetNeighbours(currentNode);
        foreach (Node neighbour in neighbours)
        {
            int xDirection = neighbour.x - currentNode.x;
            int yDirection = neighbour.y - currentNode.y;

            jumpNode = Jump(neighbour.x, neighbour.y, xDirection, yDirection, MaxJumpPointDistance);
            if (jumpNode != null)
                successors.Add(jumpNode);
        }

        return successors;
    }

    private Node Jump(int posX, int posY, int xDirection, int yDirection, int depth)
    {
        if (!grid.IsWalkable(posX, posY))
            return null;
        if (depth == 0)
        {
            _forced = true;
            return grid.GetNodeFromIndexUnchecked(posX, posY);
        }
        if (targetNode.x == posX && targetNode.y == posY)
        {
            _forced = true;
            return grid.GetNodeFromIndexUnchecked(posX, posY);
        }

        _forced = false;
        if (xDirection != 0 && yDirection != 0)
        {
            if ((!grid.IsWalkable(posX - xDirection, posY) && grid.IsWalkable(posX - xDirection, posY + yDirection)) ||
                (!grid.IsWalkable(posX, posY - yDirection) && grid.IsWalkable(posX + xDirection, posY - yDirection)))
            {
                return grid.GetNodeFromIndexUnchecked(posX, posY);
            }

            if (grid.IsWalkable(posX + xDirection, posY + yDirection) &&
                !grid.IsWalkable(posX + xDirection, posY) &&
                !grid.IsWalkable(posX, posY + yDirection))
            {
                return null;
            }

            if (Jump(posX + xDirection, posY, xDirection, 0, depth - 1) != null || Jump(posX, posY + yDirection, 0, yDirection, depth - 1) != null)
            {
                return grid.GetNodeFromIndexUnchecked(posX, posY);
            }
        }
        else
        {
            if (xDirection != 0)
            {
                if ((grid.IsWalkable(posX + xDirection, posY + 1) && !grid.IsWalkable(posX, posY + 1)) ||
                    (grid.IsWalkable(posX + xDirection, posY - 1) && !grid.IsWalkable(posX, posY - 1)))
                {
                    _forced = true;
                    return grid.GetNodeFromIndexUnchecked(posX, posY);
                }
            }
            else
            {
                if ((grid.IsWalkable(posX + 1, posY + yDirection) && !grid.IsWalkable(posX + 1, posY)) ||
                    (grid.IsWalkable(posX - 1, posY + yDirection) && !grid.IsWalkable(posX - 1, posY)))
                {
                    _forced = true;
                    return grid.GetNodeFromIndexUnchecked(posX, posY);
                }
            }
        }

        return Jump(posX + xDirection, posY + yDirection, xDirection, yDirection, depth - 1);
    }

    private int GetDistance(Node a, Node b)
    {
        var dx = Math.Abs(a.x - b.x);
        var dy = Math.Abs(a.y - b.y);

        return (int)(LateralCost * (dx + dy) + (DiagonalCost - 2 * LateralCost) * Math.Min(dx, dy));
    }
}