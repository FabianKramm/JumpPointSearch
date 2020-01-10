#define DEBUG_PATHFINDING

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public class GridGraph
{
    public bool AStar;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int gridToArrayPos(int x, int y)
    {
        return x * SizeY + y;
    }

    public int SizeX;
    public int SizeY;
    public int[] Weights;

    private FindPath _path = null;
    private Dictionary<int, Node> grid = new Dictionary<int, Node>();

    public int GridSize
    {
        get
        {
            return SizeX * SizeY;
        }
    }

    FindAPath path;
    public void InitializeGrid(int sizeX, int sizeY)
    {
        SizeX = sizeX;
        SizeY = sizeY;

        Weights = new int[GridSize];
        grid = new Dictionary<int, Node>();
        
        _path = new FindPath();
        path = new FindAPath();
    }

    //REMOVE - A STAR COMPARISON
    public List<Node> GetAStarNeighbours(Node currentNode)
    {
        List<Node> neighbours = new List<Node>();
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0)
                    continue;

                if (IsWalkable(x + currentNode.x, y + currentNode.y))
                {
                    if (x != 0 && y != 0)
                    {
                        if (!IsWalkable(x + currentNode.x, currentNode.y) && !IsWalkable(currentNode.x, y + currentNode.y))
                        {
                            continue;
                        }
                    }

                    neighbours.Add(GetNodeFromIndexUnchecked(x + currentNode.x, y + currentNode.y));
                }
            }
        }

        return neighbours;
    }

    public List<Node> GetPath(Vector2Int startPosition, Vector2Int targetPosition)
    {
        Node startNode = GetNodeFromIndex(startPosition.x, startPosition.y);
        Node targetNode = GetNodeFromIndex(targetPosition.x, targetPosition.y);

        if (AStar)
        {
            return path.GetPath(this, startNode, targetNode);
        }

        return _path.GetPath(this, startNode, targetNode);
    }
    
    public Node GetNodeFromIndexUnchecked(int x, int y)
    {
        var arrayPos = gridToArrayPos(x, y);
        if (grid.TryGetValue(arrayPos, out Node node))
            return node;

        grid[arrayPos] = new Node(x, y);
        return grid[arrayPos];
    }

    public Node GetNodeFromIndex(int x, int y)
    {
        if (!IsWalkable(x, y))
            return null;

        var arrayPos = gridToArrayPos(x, y);
        if (grid.TryGetValue(arrayPos, out Node node))
            return node;

        grid[arrayPos] = new Node(x, y);
        return grid[arrayPos];
    }

    public List<Node> GetNeighbours(Node currentNode)
    {
        Node parentNode = currentNode.parent;
        if (parentNode == null)
        {
            return GetAStarNeighbours(currentNode);
        }

        List<Node> neighbours = new List<Node>();
        int xDirection = Mathf.Clamp(currentNode.x - parentNode.x, -1, 1);
        int yDirection = Mathf.Clamp(currentNode.y - parentNode.y, -1, 1);
        if (xDirection != 0 && yDirection != 0)
        {
            //assumes positive direction for variable naming
            bool neighbourUp = IsWalkable(currentNode.x, currentNode.y + yDirection);
            bool neighbourRight = IsWalkable(currentNode.x + xDirection, currentNode.y);
            bool neighbourLeft = IsWalkable(currentNode.x - xDirection, currentNode.y);
            bool neighbourDown = IsWalkable(currentNode.x, currentNode.y - yDirection);

            if (neighbourUp)
                neighbours.Add(GetNodeFromIndexUnchecked(currentNode.x, currentNode.y + yDirection));

            if (neighbourRight)
                neighbours.Add(GetNodeFromIndexUnchecked(currentNode.x + xDirection, currentNode.y));

            if (neighbourUp || neighbourRight)
                if (IsWalkable(currentNode.x + xDirection, currentNode.y + yDirection))
                    neighbours.Add(GetNodeFromIndexUnchecked(currentNode.x + xDirection, currentNode.y + yDirection));

            if (!neighbourLeft && neighbourUp)
                if (IsWalkable(currentNode.x - xDirection, currentNode.y + yDirection))
                    neighbours.Add(GetNodeFromIndexUnchecked(currentNode.x - xDirection, currentNode.y + yDirection));

            if (!neighbourDown && neighbourRight)
                if (IsWalkable(currentNode.x + xDirection, currentNode.y - yDirection))
                    neighbours.Add(GetNodeFromIndexUnchecked(currentNode.x + xDirection, currentNode.y - yDirection));
        }
        else
        {
            if (xDirection == 0)
            {
                if (IsWalkable(currentNode.x, currentNode.y + yDirection))
                {
                    neighbours.Add(GetNodeFromIndexUnchecked(currentNode.x, currentNode.y + yDirection));
                    if (!IsWalkable(currentNode.x + 1, currentNode.y) && IsWalkable(currentNode.x + 1, currentNode.y + yDirection))
                        neighbours.Add(GetNodeFromIndexUnchecked(currentNode.x + 1, currentNode.y + yDirection));
                    if (!IsWalkable(currentNode.x - 1, currentNode.y) && IsWalkable(currentNode.x - 1, currentNode.y + yDirection))
                        neighbours.Add(GetNodeFromIndexUnchecked(currentNode.x - 1, currentNode.y + yDirection));
                }
            }
            else
            {
                if (IsWalkable(currentNode.x + xDirection, currentNode.y))
                {
                    neighbours.Add(GetNodeFromIndexUnchecked(currentNode.x + xDirection, currentNode.y));
                    if (!IsWalkable(currentNode.x, currentNode.y + 1) && IsWalkable(currentNode.x + xDirection, currentNode.y + 1))
                        neighbours.Add(GetNodeFromIndexUnchecked(currentNode.x + xDirection, currentNode.y + 1));
                    if (!IsWalkable(currentNode.x, currentNode.y - 1) && IsWalkable(currentNode.x + xDirection, currentNode.y - 1))
                        neighbours.Add(GetNodeFromIndexUnchecked(currentNode.x + xDirection, currentNode.y - 1));
                }
            }
        }

        return neighbours;
    }

    public bool IsWalkable(int x, int y)
    {
        return (x >= 0 && x < SizeX) && (y >= 0 && y < SizeY) && Weights[x * SizeY + y] != 0;
    }
}