#define DEBUG_PATHFINDING

using UnityEngine;

public class JumpPointSearch : AStarSearch
{
    private static readonly int MaxJumpPointDistance = 256;

    protected override int GetNeighbors(Node currentNode, Position[] neighbors)
    {
        return grid.GetNeighbours(currentNode, neighbors);
    }

    protected override Node GetNeighborNode(Node currentNode, Position neighbor)
    {
        int xDirection = neighbor.x - currentNode.x;
        int yDirection = neighbor.y - currentNode.y;

        var jumpPosition = Jump(neighbor.x, neighbor.y, xDirection, yDirection, MaxJumpPointDistance);
        if (jumpPosition.IsValid())
            return GetNodeFromIndexUnchecked(jumpPosition.x, jumpPosition.y);

        return null;
    }

    private Position Jump(int posX, int posY, int xDirection, int yDirection, int depth)
    {
        if (!grid.IsWalkable(posX, posY))
            return Position.invalid;
        if (depth == 0 || (targetNode.x == posX && targetNode.y == posY))
            return new Position(posX, posY);

        if (xDirection != 0 && yDirection != 0)
        {
            if ((!grid.IsWalkable(posX - xDirection, posY) && grid.IsWalkable(posX - xDirection, posY + yDirection)) ||
                (!grid.IsWalkable(posX, posY - yDirection) && grid.IsWalkable(posX + xDirection, posY - yDirection)))
            {
                return new Position(posX, posY);
            }

            if (grid.IsWalkable(posX + xDirection, posY + yDirection) &&
                !grid.IsWalkable(posX + xDirection, posY) &&
                !grid.IsWalkable(posX, posY + yDirection))
            {
                return Position.invalid;
            }

            if (Jump(posX + xDirection, posY, xDirection, 0, depth - 1) != Position.invalid || Jump(posX, posY + yDirection, 0, yDirection, depth - 1) != Position.invalid)
            {
                return new Position(posX, posY);
            }
        }
        else
        {
            if (xDirection != 0)
            {
                if ((grid.IsWalkable(posX + xDirection, posY + 1) && !grid.IsWalkable(posX, posY + 1)) ||
                    (grid.IsWalkable(posX + xDirection, posY - 1) && !grid.IsWalkable(posX, posY - 1)))
                {
                    return new Position(posX, posY);
                }
            }
            else
            {
                if ((grid.IsWalkable(posX + 1, posY + yDirection) && !grid.IsWalkable(posX + 1, posY)) ||
                    (grid.IsWalkable(posX - 1, posY + yDirection) && !grid.IsWalkable(posX - 1, posY)))
                {
                    return new Position(posX, posY);
                }
            }
        }

        return Jump(posX + xDirection, posY + yDirection, xDirection, yDirection, depth - 1);
    }
}