using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pathfinding
{
    public class SubGoalBiDirectionalDijkstraSearch : BiDirectionalDijkstraSearch
    {
        public struct CostPosition
        {
            public Node node;
            public float cost;
        }

        private SubGoalGrid subGoalGrid;
        private Dictionary<long, CostPosition[]> fakeEdges;
        public HashSet<long> removedNodes;

        public SubGoalBiDirectionalDijkstraSearch(SubGoalGrid grid) : base(grid)
        {
            fakeEdges = new Dictionary<long, CostPosition[]>();
            removedNodes = new HashSet<long>();
            subGoalGrid = grid;
            sizeY = grid.GetSize().y;
        }

        public void AddFakeEdge(Node from, Position[] to)
        {
            if (to.Length == 0)
                return;

            var index = from.x * sizeY + from.y;
            if (fakeEdges.TryGetValue(index, out CostPosition[] edges))
            {
                var oldLength = edges.Length;
                Array.Resize(ref edges, edges.Length + to.Length);
                for (var i = 0; i < to.Length; i++)
                    edges[oldLength + i] = new CostPosition()
                    {
                        node = GetNodeAt(to[i].x, to[i].y),
                        cost = SubGoalGrid.Diagonal(from.x, from.y, to[i].x, to[i].y)
                    };

                fakeEdges[index] = edges;
            }
            else
            {
                var newFakeEdges = new CostPosition[to.Length];
                for (var i = 0; i < to.Length; i++)
                    newFakeEdges[i] = new CostPosition()
                    {
                        node = GetNodeAt(to[i].x, to[i].y),
                        cost = SubGoalGrid.Diagonal(from.x, from.y, to[i].x, to[i].y)
                    };

                fakeEdges[index] = newFakeEdges;
            }
        }

        public void FakeRemoveSubGoal(SubGoal subGoal)
        {
            removedNodes.Add(subGoal.x * sizeY + subGoal.y);
        }

        public void Reset()
        {
            removedNodes = new HashSet<long>();
            fakeEdges = new Dictionary<long, CostPosition[]>();
        }

        public override Node GetStartNode(Vector2Int start)
        {
            // Insert start into graph
            var startNode = GetNodeAt(start.x, start.y);
            if (subGoalGrid.subGoals.ContainsKey(start.x * sizeY + start.y) == false)
            {
                var reachable = subGoalGrid.GetDirectHReachable(start.x, start.y);
                if (reachable.Count == 0)
                    return null;
                foreach (var t in reachable)
                {
                    AddFakeEdge(GetNodeAt(t.x, t.y), new Position[] { new Position(start.x, start.y) });
                    AddFakeEdge(startNode, new Position[] { new Position(t.x, t.y) });
                }
            }

            return startNode;
        }

        public override Node GetEndNode(Vector2Int end)
        {
            // Insert target -> end into graph
            var endNode = GetNodeAt(end.x, end.y);
            if (subGoalGrid.subGoals.ContainsKey(end.x * sizeY + end.y) == false)
            {
                var targetReachable = subGoalGrid.GetDirectHReachable(end.x, end.y);
                if (targetReachable.Count == 0)
                    return null;

                foreach (var t in targetReachable)
                {
                    AddFakeEdge(GetNodeAt(t.x, t.y), new Position[] { new Position(end.x, end.y) });
                    AddFakeEdge(endNode, new Position[] { new Position(t.x, t.y) });
                }
            }

            return endNode;
        }

        protected override int GetNeighbors(Node currentNode, ref Neighbor[] neighbors)
        {
            var index = currentNode.x * sizeY + currentNode.y;
            var count = 0;

            CostPosition[] fakeEdge = null;
            if (fakeEdges.TryGetValue(index, out fakeEdge))
                count += fakeEdge.Length;

            SubGoal subGoal = null;
            if (subGoalGrid.subGoals.TryGetValue(index, out subGoal))
                count += subGoal.edges.Length;

            if (neighbors.Length < count)
            {
                neighbors = new Neighbor[count];
            }

            var counter = -1;
            if (subGoal != null)
            {
                for (var i = 0; i < subGoal.edges.Length; i++)
                {
                    if (removedNodes.Contains(subGoal.edges[i].toX * sizeY + subGoal.edges[i].toY))
                        continue;

                    counter++;
                    neighbors[counter] = new Neighbor()
                    {
                        node = GetNodeAt(subGoal.edges[i].toX, subGoal.edges[i].toY),
                        cost = subGoal.edges[i].cost
                    };
                }
            }

            if (fakeEdge != null)
            {
                for (var i = 0; i < fakeEdge.Length; i++)
                {
                    counter++;
                    neighbors[counter] = new Neighbor()
                    {
                        node = fakeEdge[i].node,
                        cost = fakeEdge[i].cost
                    };
                }
            }

            return counter + 1;
        }
    }
}
