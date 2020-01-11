using UnityEngine;

namespace Pathfinding
{
    public class JumpPointSearch : AStarSearch
    {
        private static readonly int MaxJumpPointDistance = 256;

        protected override int GetNeighbors(Node currentNode, Position[] neighbors)
        {
            Node parentNode = currentNode.parent;
            if (parentNode == null)
            {
                return base.GetNeighbors(currentNode, neighbors);
            }

            int count = 0;
            int xDirection = Mathf.Clamp(currentNode.x - parentNode.x, -1, 1);
            int yDirection = Mathf.Clamp(currentNode.y - parentNode.y, -1, 1);
            if (xDirection != 0 && yDirection != 0)
            {
                //assumes positive direction for variable naming
                bool neighbourUp = grid.IsWalkable(currentNode.x, currentNode.y + yDirection);
                bool neighbourRight = grid.IsWalkable(currentNode.x + xDirection, currentNode.y);
                bool neighbourLeft = grid.IsWalkable(currentNode.x - xDirection, currentNode.y);
                bool neighbourDown = grid.IsWalkable(currentNode.x, currentNode.y - yDirection);

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
                if ((neighbourUp || neighbourRight) && grid.IsWalkable(currentNode.x + xDirection, currentNode.y + yDirection))
                {
                    neighbors[count] = new Position(currentNode.x + xDirection, currentNode.y + yDirection);
                    count++;
                }
                if (!neighbourLeft && neighbourUp && grid.IsWalkable(currentNode.x - xDirection, currentNode.y + yDirection))
                {
                    neighbors[count] = new Position(currentNode.x - xDirection, currentNode.y + yDirection);
                    count++;
                }
                if (!neighbourDown && neighbourRight && grid.IsWalkable(currentNode.x + xDirection, currentNode.y - yDirection))
                {
                    neighbors[count] = new Position(currentNode.x + xDirection, currentNode.y - yDirection);
                    count++;
                }
            }
            else
            {
                if (xDirection == 0)
                {
                    if (grid.IsWalkable(currentNode.x, currentNode.y + yDirection))
                    {
                        neighbors[count] = new Position(currentNode.x, currentNode.y + yDirection);
                        count++;

                        if (!grid.IsWalkable(currentNode.x + 1, currentNode.y) && grid.IsWalkable(currentNode.x + 1, currentNode.y + yDirection))
                        {
                            neighbors[count] = new Position(currentNode.x + 1, currentNode.y + yDirection);
                            count++;
                        }
                        if (!grid.IsWalkable(currentNode.x - 1, currentNode.y) && grid.IsWalkable(currentNode.x - 1, currentNode.y + yDirection))
                        {
                            neighbors[count] = new Position(currentNode.x - 1, currentNode.y + yDirection);
                            count++;
                        }
                    }
                }
                else
                {
                    if (grid.IsWalkable(currentNode.x + xDirection, currentNode.y))
                    {
                        neighbors[count] = new Position(currentNode.x + xDirection, currentNode.y);
                        count++;

                        if (!grid.IsWalkable(currentNode.x, currentNode.y + 1) && grid.IsWalkable(currentNode.x + xDirection, currentNode.y + 1))
                        {
                            neighbors[count] = new Position(currentNode.x + xDirection, currentNode.y + 1);
                            count++;
                        }
                        if (!grid.IsWalkable(currentNode.x, currentNode.y - 1) && grid.IsWalkable(currentNode.x + xDirection, currentNode.y - 1))
                        {
                            neighbors[count] = new Position(currentNode.x + xDirection, currentNode.y - 1);
                            count++;
                        }
                    }
                }
            }

            return count;
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
}