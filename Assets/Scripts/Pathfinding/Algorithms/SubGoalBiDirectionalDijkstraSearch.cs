using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Pathfinding
{
    public class SubGoalBiDirectionalDijkstraSearch : BiDirectionalDijkstraSearch
    {
        private SubGoalGrid subGoalGrid;
        private Dictionary<long, Position[]> fakeEdges;
        public HashSet<long> removedNodes;

        public SubGoalBiDirectionalDijkstraSearch(SubGoalGrid grid) : base(grid)
        {
            fakeEdges = new Dictionary<long, Position[]>();
            removedNodes = new HashSet<long>();
            subGoalGrid = grid;
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

        public override List<Node> GetPath(Vector2Int start, Vector2Int target)
        {
            fakeEdges = new Dictionary<long, Position[]>();
            return base.GetPath(start, target);
        }

        public override Node GetStartNode(Vector2Int start)
        {
            // Insert start into graph
            if (subGoalGrid.subGoals.ContainsKey(start.x * sizeY + start.y) == false)
            {
                var reachable = subGoalGrid.GetDirectHReachable(start.x, start.y);
                if (reachable.Count == 0)
                    return null;
                foreach (var t in reachable)
                {
                    AddFakeEdge(new Position(t.x, t.y), new Position[] { new Position(start.x, start.y) });
                    AddFakeEdge(new Position(start.x, start.y), new Position[] { new Position(t.x, t.y) });
                }
            }

            return new Node(start.x, start.y);
        }

        public override Node GetEndNode(Vector2Int end)
        {
            // Insert target -> end into graph
            if (subGoalGrid.subGoals.ContainsKey(end.x * sizeY + end.y) == false)
            {
                var targetReachable = subGoalGrid.GetDirectHReachable(end.x, end.y);
                if (targetReachable.Count == 0)
                    return null;
                foreach (var t in targetReachable)
                {
                    AddFakeEdge(new Position(t.x, t.y), new Position[] { new Position(end.x, end.y) });
                    AddFakeEdge(new Position(end.x, end.y), new Position[] { new Position(t.x, t.y) });
                }
            }

            return new Node(end.x, end.y);
        }

        protected override int GetNeighbors(Node currentNode, ref Neighbor[] neighbors)
        {
            var index = currentNode.x * sizeY + currentNode.y;
            var count = 0;

            Position[] fakeEdge = null;
            if (fakeEdges.TryGetValue(index, out fakeEdge))
                count += fakeEdge.Length;

            SubGoal subGoal = null;
            if (subGoalGrid.subGoals.TryGetValue(index, out subGoal))
                count += subGoal.edges.Length;

            neighbors = new Neighbor[count];
            if (subGoal != null)
            {
                for (var i = 0; i < subGoal.edges.Length; i++)
                {
                    neighbors[i] = new Neighbor()
                    {
                        node = GetNodeAt(subGoal.edges[i].toX, subGoal.edges[i].toY),
                        cost = subGoal.edges[i].cost
                    };
                }
            }

            if (fakeEdge != null)
            {
                var offset = subGoal != null ? subGoal.edges.Length : 0;
                for (var i = 0; i < fakeEdge.Length; i++)
                {
                    neighbors[i + offset] = new Neighbor()
                    {
                        node = GetNodeAt(fakeEdge[i].x, fakeEdge[i].y),
                        cost = 1
                    };
                }
            }

            return neighbors.Length;
        }
    }
}
