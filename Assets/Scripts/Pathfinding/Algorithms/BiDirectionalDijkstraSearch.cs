#define DEBUG_PATHFINDING

using System.Collections.Generic;
using UnityEngine;

namespace Pathfinding
{
    public class BiDirectionalDijkstraSearch
    {
        public struct Neighbor
        {
            public Node node;
            public float cost;
        }

        public class Node
        {
            public Node parentA;
            public Node parentB;
            public int x;
            public int y;
            public uint _flags;

            public void setOpenA() { _flags |= 1; }
            public void setClosedA() { _flags |= 2; }
            public void setOpenB() { _flags |= 4; }
            public void setClosedB() { _flags |= 8; }

            public bool isOpenA() { return (_flags & 1) == 1; }
            public bool isClosedA() { return (_flags & 2) == 2; }
            public bool isOpenB() { return (_flags & 4) == 4; }
            public bool isClosedB() { return (_flags & 8) == 8; }

            public float costA;
            public float costB;

            public Node() { }

            public Node(int _x, int _y)
            {
                x = _x;
                y = _y;
            }

            public override string ToString()
            {
                return x.ToString() + " , " + y.ToString();
            }
        }

        protected bool showDebug = true;
        protected MinHeap<Node, float> openA;
        protected MinHeap<Node, float> openB;

        protected IGrid grid;
        protected int sizeY;
        protected Dictionary<long, Node> nodes;

        protected Node middleNode;
        protected float bestPathLength;

        public BiDirectionalDijkstraSearch(IGrid grid)
        {
            this.grid = grid;
            sizeY = grid.GetSize().y;
        }

        public virtual List<Node> GetPath(Vector2Int start, Vector2Int target, float upperBound = float.PositiveInfinity)
        {
            nodes = new Dictionary<long, Node>();

            var startNode = GetStartNode(start);
            var endNode = GetEndNode(target);
            var neighbors = new Neighbor[8];

            middleNode = null;
            bestPathLength = float.PositiveInfinity;

            openA = new MinHeap<Node, float>();
            openB = new MinHeap<Node, float>();

            openA.Add(startNode, 0);
            openB.Add(endNode, 0);

            var ticks = 0;
            while (openA.Count > 0 && openB.Count > 0)
            {
                ticks++;

                var mtmp = openA.Peek().costA + openB.Peek().costB;
                if (mtmp >= bestPathLength)
                {
                    Debug.Log("Found path in " + ticks + " ticks");
                    return tracebackPath(middleNode);
                }
                if (mtmp >= upperBound)
                {
                    return null;
                }

                expandForwardFrontier(openA.Remove(), ref neighbors);
                ticks++;

                mtmp = openA.Peek().costA + openB.Peek().costB;
                if (mtmp >= bestPathLength)
                {
                    return tracebackPath(middleNode);
                }
                if (mtmp >= upperBound)
                {
                    return null;
                }

                expandBackwardFrontier(openB.Remove(), ref neighbors);
            }

            return null;
        }

        private void expandBackwardFrontier(Node current, ref Neighbor[] neighbors)
        {
            current.setClosedB();

            var count = GetNeighbors(current, ref neighbors);
            for (var i = 0; i < count; i++)
            {
                var neighbor = neighbors[i].node;
                if (neighbor.isClosedB())
                    continue;

                var tentativeScore = current.costB + neighbors[i].cost;
                if (!neighbor.isOpenB())
                {
                    neighbor.setOpenB();
                    neighbor.costB = tentativeScore;
                    neighbor.parentB = current;
                    openB.Add(neighbor, tentativeScore);
                    updateBackwardFrontier(neighbor, tentativeScore);
                }
                else if (neighbor.costB > tentativeScore)
                {
                    neighbor.costB = tentativeScore;
                    neighbor.parentB = current;
                    openB.Update(neighbor, tentativeScore);
                    updateBackwardFrontier(neighbor, tentativeScore);
                }
            }
        }

        private void updateBackwardFrontier(Node node, float nodeScore)
        {
#if DEBUG_PATHFINDING
            if (showDebug)
            {
                DebugDrawer.Draw(new Vector2Int(node.parentB.x, node.parentB.y), new Vector2Int(node.x, node.y), Color.red);
                DebugDrawer.DrawCube(new Vector2Int(node.x, node.y), Vector2Int.one, Color.red);
            }
#endif

            if (node.isClosedA())
            {
                var pathLength = node.costA + nodeScore;
                if (bestPathLength > pathLength)
                {
                    bestPathLength = pathLength;
                    middleNode = node;
                }
            }
        }

        private void expandForwardFrontier(Node current, ref Neighbor[] neighbors)
        {
            current.setClosedA();

            var count = GetNeighbors(current, ref neighbors);
            for (var i = 0; i < count; i++)
            {
                var neighbor = neighbors[i].node;
                if (neighbor.isClosedA())
                    continue;

                var tentativeScore = current.costA + neighbors[i].cost;
                if (!neighbor.isOpenA())
                {
                    neighbor.setOpenA();
                    neighbor.costA = tentativeScore;
                    neighbor.parentA = current;
                    openA.Add(neighbor, tentativeScore);
                    updateForwardFrontier(neighbor, tentativeScore);
                }
                else if (neighbor.costA > tentativeScore)
                {
                    neighbor.costA = tentativeScore;
                    neighbor.parentA = current;
                    openA.Update(neighbor, tentativeScore);
                    updateForwardFrontier(neighbor, tentativeScore);
                }
            }
        }

        private void updateForwardFrontier(Node node, float nodeScore)
        {
#if DEBUG_PATHFINDING
            if (showDebug)
            {
                DebugDrawer.Draw(new Vector2Int(node.parentA.x, node.parentA.y), new Vector2Int(node.x, node.y), Color.white);
                DebugDrawer.DrawCube(new Vector2Int(node.x, node.y), Vector2Int.one, Color.white);
            }
#endif

            if (node.isClosedB())
            {
                var pathLength = node.costB + nodeScore;
                if (bestPathLength > pathLength)
                {
                    bestPathLength = pathLength;
                    middleNode = node;
                }
            }
        }

        protected virtual int GetNeighbors(Node currentNode, ref Neighbor[] neighbors)
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

                        neighbors[count] = new Neighbor()
                        {
                            node = GetNodeAt(x + currentNode.x, y + currentNode.y),
                            cost = 1
                        }; 
                        count++;
                    }
                }
            }

            return count;
        }

        protected virtual Node GetNodeAt(int x, int y)
        {
            var arrayPos = x * sizeY + y;
            if (nodes.TryGetValue(arrayPos, out Node node))
                return node;

            nodes[arrayPos] = new Node(x, y);
            // grid[arrayPos] = NodePool.NewNode(x, y);
            return nodes[arrayPos];
        }

        public virtual Node GetStartNode(Vector2Int start)
        {
            return new Node(start.x, start.y);
        }

        public virtual Node GetEndNode(Vector2Int end)
        {
            return new Node(end.x, end.y);
        }

        public List<Node> tracebackPath(Node touch)
        {
            Node current = touch;
            List<Node> path = new List<Node>();
            while (current != null)
            {
                path.Add(current);
                current = current.parentA;
            }

            path.Reverse();
            if (touch.parentB != null)
            {
                current = touch.parentB;
                while (current != null)
                {
                    path.Add(current);
                    current = current.parentB;
                }
            }
            
            return path;
        }
    }
}
