#define DEBUG_PATHFINDING

using UnityEngine;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public class GridGraph
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int gridToArrayPos(int x, int y)
    {
        return x * SizeY + y;
    }

    public int SizeX;
    public int SizeY;
    public float[] Weights;

    public int GridSize
    {
        get
        {
            return SizeX * SizeY;
        }
    }

    public void InitializeGrid(int sizeX, int sizeY)
    {
        SizeX = sizeX;
        SizeY = sizeY;

        Weights = new float[GridSize];
    }

    public int GetAStarNeighbours(Node currentNode, Position[] neighbors)
    {
        int count = 0;
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

                    neighbors[count] = new Position(x + currentNode.x, y + currentNode.y);
                    count++;
                }
            }
        }

        return count;
    }

    public int GetNeighbours(Node currentNode, Position[] neighbors)
    {
        Node parentNode = currentNode.parent;
        if (parentNode == null)
        {
            return GetAStarNeighbours(currentNode, neighbors);
        }

        int count = 0;
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
            {
                neighbors[count] = new Position(currentNode.x, currentNode.y + yDirection);
                count++;
            }
            if (neighbourRight)
            {
                neighbors[count] = new Position(currentNode.x + xDirection, currentNode.y);
                count++;
            }
            if ((neighbourUp || neighbourRight) && IsWalkable(currentNode.x + xDirection, currentNode.y + yDirection))
            {
                neighbors[count] = new Position(currentNode.x + xDirection, currentNode.y + yDirection);
                count++;
            }
            if (!neighbourLeft && neighbourUp && IsWalkable(currentNode.x - xDirection, currentNode.y + yDirection))
            {
                neighbors[count] = new Position(currentNode.x - xDirection, currentNode.y + yDirection);
                count++;
            }
            if (!neighbourDown && neighbourRight && IsWalkable(currentNode.x + xDirection, currentNode.y - yDirection))
            {
                neighbors[count] = new Position(currentNode.x + xDirection, currentNode.y - yDirection);
                count++;
            }
        }
        else
        {
            if (xDirection == 0)
            {
                if (IsWalkable(currentNode.x, currentNode.y + yDirection))
                {
                    neighbors[count] = new Position(currentNode.x, currentNode.y + yDirection);
                    count++;
                    
                    if (!IsWalkable(currentNode.x + 1, currentNode.y) && IsWalkable(currentNode.x + 1, currentNode.y + yDirection))
                    {
                        neighbors[count] = new Position(currentNode.x + 1, currentNode.y + yDirection);
                        count++;
                    }
                    if (!IsWalkable(currentNode.x - 1, currentNode.y) && IsWalkable(currentNode.x - 1, currentNode.y + yDirection))
                    {
                        neighbors[count] = new Position(currentNode.x - 1, currentNode.y + yDirection);
                        count++;
                    }
                }
            }
            else
            {
                if (IsWalkable(currentNode.x + xDirection, currentNode.y))
                {
                    neighbors[count] = new Position(currentNode.x + xDirection, currentNode.y);
                    count++;
                    
                    if (!IsWalkable(currentNode.x, currentNode.y + 1) && IsWalkable(currentNode.x + xDirection, currentNode.y + 1))
                    {
                        neighbors[count] = new Position(currentNode.x + xDirection, currentNode.y + 1);
                        count++;
                    }
                    if (!IsWalkable(currentNode.x, currentNode.y - 1) && IsWalkable(currentNode.x + xDirection, currentNode.y - 1))
                    {
                        neighbors[count] = new Position(currentNode.x + xDirection, currentNode.y - 1);
                        count++;
                    }
                }
            }
        }

        return count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsWalkable(int x, int y)
    {
        return (x >= 0 && x < SizeX) && (y >= 0 && y < SizeY) && Weights[x * SizeY + y] != 0;
    }
}