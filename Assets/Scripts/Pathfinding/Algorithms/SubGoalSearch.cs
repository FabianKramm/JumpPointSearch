using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pathfinding
{
    public class SubGoalSearch : AStarSearch
    {
        private SubGoalGrid subGoalGrid;
        private Dictionary<long, Position[]> fakeEdges;
        public HashSet<long> removedNodes;

        public SubGoalSearch(IGrid grid)
        {
            fakeEdges = new Dictionary<long, Position[]>();
            removedNodes = new HashSet<long>();
            subGoalGrid = (SubGoalGrid)grid;
            sizeY = grid.GetSize().y;
        }

        public void AddFakeEdge(Position from, Position[] to)
        {
            if (to.Length == 0)
                return;

            var index = from.x * sizeY + from.y;
            if (fakeEdges.TryGetValue(index, out Position[] edges))
            {
                var oldLength = edges.Length;
                Array.Resize(ref edges, edges.Length + to.Length);
                for (var i = 0; i < to.Length; i++)
                    edges[oldLength + i] = to[i];

            }
            else
            {
                fakeEdges[index] = new Position[to.Length];
                Array.Copy(to, fakeEdges[index], to.Length);
            }
        }

        protected override Node GetNeighborNode(Node currentNode, Position neighbor)
        {
            if (removedNodes.Count > 0 && removedNodes.Contains(neighbor.x * sizeY + neighbor.y))
                return null;

            return base.GetNeighborNode(currentNode, neighbor);
        }

        public void FakeRemoveSubGoal(SubGoal subGoal)
        {
            removedNodes.Add(subGoal.x * sizeY + subGoal.y);
        }

        public Position[] GetReachable(int x, int y)
        {
            var subGoals = subGoalGrid.GetDirectHReachable(x, y);
            var neighbors = new Position[subGoals.Count];
            for (var i = 0; i < subGoals.Count; i++)
            {
                neighbors[i] = new Position(subGoals[i].x, subGoals[i].y);
            }

            return neighbors;
        }

        public override List<Node> GetPath(IGrid grid, Vector2Int start, Vector2Int target, bool greedy = true)
        {
            fakeEdges = new Dictionary<long, Position[]>();
            // removedNodes = new HashSet<long>();
            subGoalGrid = (SubGoalGrid)grid;
            sizeY = grid.GetSize().y;

            // Insert target -> end into graph
            if (subGoalGrid.subGoals.ContainsKey(target.x * sizeY + target.y) == false)
            {
                var targetReachable = subGoalGrid.GetDirectHReachable(target.x, target.y);
                if (targetReachable.Count == 0)
                    return null;
                foreach (var t in targetReachable)
                {
                    AddFakeEdge(new Position(t.x, t.y), new Position[] { new Position(target.x, target.y) });
                }
            }

            // Insert start into graph
            if (subGoalGrid.subGoals.ContainsKey(start.x * sizeY + start.y) == false)
            {
                AddFakeEdge(new Position(start.x, start.y), GetReachable(start.x, start.y));
            }

            return base.GetPath(grid, start, target, false);
        }

        protected override int NewGCost(Node currentNode, Node neighborNode)
        {
            // TODO: For teleporters this will change
            return currentNode.gCost + Heuristic(currentNode, neighborNode);
        }

        protected override int GetNeighbors(Node currentNode, ref Position[] neighbors)
        {
            var index = currentNode.x * sizeY + currentNode.y;
            var count = 0;

            Position[] fakeEdge = null;
            if (fakeEdges.TryGetValue(index, out fakeEdge))
                count += fakeEdge.Length;

            SubGoal subGoal = null;
            if (subGoalGrid.subGoals.TryGetValue(index, out subGoal))
                count += subGoal.edges.Length;

            neighbors = new Position[count];
            if (subGoal != null)
            {
                for (var i = 0; i < subGoal.edges.Length; i++)
                {
                    neighbors[i] = new Position(subGoal.edges[i].toX, subGoal.edges[i].toY);
                }
            }

            if (fakeEdge != null)
            {
                var offset = subGoal != null ? subGoal.edges.Length : 0;
                Array.Copy(fakeEdge, 0, neighbors, offset, fakeEdge.Length);
            }
            
            return neighbors.Length;
        }
    }
}