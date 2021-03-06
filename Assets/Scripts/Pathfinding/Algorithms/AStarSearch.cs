﻿#define DEBUG_PATHFINDING

using UnityEngine;
using System.Collections.Generic;
using System;

namespace Pathfinding
{
    public class AStarSearch
    {
        public static readonly float DiagonalCost = 1.4142135623730950488016887242097f; // sqrt(2)
        public static readonly float LateralCost = 1.0f;
        
        public bool showDebug = true;

        protected Node startNode;
        protected Node targetNode;
        protected IGrid grid;
        protected long sizeY;

        protected MinHeap<Node, int> heap;
        protected Dictionary<long, Node> nodes;

        public virtual List<Node> GetPath(IGrid grid, Vector2Int start, Vector2Int target, bool greedy = true)
        {
            this.grid = grid;
            sizeY = grid.GetSize().y;
            heap = new MinHeap<Node, int>();
            nodes = new Dictionary<long, Node>();
            startNode = GetNodeFromIndexUnchecked(start.x, start.y);
            targetNode = GetNodeFromIndexUnchecked(target.x, target.y);

            if (greedy && findPathGreedy())
            {
                return RetracePath();
            }

            if (CalculateShortestPath())
            {
                return RetracePath();
            }

            return null;
        }

        private bool findPathGreedy()
        {
            var x = startNode.x;
            var y = startNode.y;

            var dx = targetNode.x - startNode.x;
            var dy = targetNode.y - startNode.y;

            int adx = Math.Abs(dx);
            int ady = Math.Abs(dy);

            Position midPos = Position.invalid;

            dx = (dx > 0 ? 1 : 0) - (dx < 0 ? 1 : 0);
            dy = (dy > 0 ? 1 : 0) - (dy < 0 ? 1 : 0);

            // go diagonally first
            if (x != targetNode.x && y != targetNode.y)
            {
                int minlen = Math.Min(adx, ady);
                int tx = x + dx * minlen;
                while (x != tx)
                {
                    if (grid.IsWalkable(x, y) && (grid.IsWalkable(x + dx, y) || grid.IsWalkable(x, y + dy))) // prevent tunneling as well
                    {
                        x += dx;
                        y += dy;
                    }
                    else
                        return false;
                }

                if (!grid.IsWalkable(x, y))
                    return false;

                midPos = new Position(x, y);
            }

            if (!(x == targetNode.x && y == targetNode.y))
            {
                while (x != targetNode.x)
                    if (!grid.IsWalkable(x += dx, y))
                        return false;

                while (y != targetNode.y)
                    if (!grid.IsWalkable(x, y += dy))
                        return false;
            }

            if (midPos != Position.invalid)
            {
                var midNode = GetNodeFromIndexUnchecked(midPos.x, midPos.y);
                if (midNode == null)
                    return false;

                midNode.parent = startNode;
                if (midNode != targetNode)
                {
                    targetNode.parent = midNode;
                }
            }
            else
            {
                targetNode.parent = startNode;
            }
            
            return true;
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
                var count = GetNeighbors(currentNode, ref neighbors);
                for (var i = 0; i < count; i++)
                {
                    var neighbor = neighbors[i];
                    var neighborNode = GetNeighborNode(currentNode, neighbor);
                    if (neighborNode == null || neighborNode.isClosed())
                        continue;

                    int newGCost = NewGCost(currentNode, neighborNode);
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
                        neighborNode.hCost = Heuristic(neighborNode, targetNode);
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

        protected virtual int GetNeighbors(Node currentNode, ref Position[] neighbors)
        {
            int count = 0;
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0)
                        continue;

                    if (grid.IsWalkable(x + currentNode.x, y + currentNode.y))
                    {
                        if (x != 0 && y != 0)
                        {
                            if (!grid.IsWalkable(x + currentNode.x, currentNode.y) && !grid.IsWalkable(currentNode.x, y + currentNode.y))
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
            var arrayPos = x * sizeY + y;
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

        protected virtual int NewGCost(Node currentNode, Node neighborNode)
        {
            return currentNode.gCost + Heuristic(currentNode, neighborNode);
        }

        protected int Diagonal(Node a, Node b)
        {
            var dx = Math.Abs(a.x - b.x);
            var dy = Math.Abs(a.y - b.y);

            return (int)(LateralCost * (dx + dy) + (DiagonalCost - 2 * LateralCost) * Math.Min(dx, dy));
        }

        protected int Manhattan(Node a, Node b)
        {
            var dx = Math.Abs(a.x - b.x);
            var dy = Math.Abs(a.y - b.y);

            return dx + dy;
        }

        public virtual int Heuristic(Node a, Node b)
        {
            var dx = Math.Abs(a.x - b.x);
            var dy = Math.Abs(a.y - b.y);

            return (int)(LateralCost * (dx + dy) + (DiagonalCost - 2 * LateralCost) * Math.Min(dx, dy));
        }
    }
}