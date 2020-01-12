using System.Collections.Generic;
using UnityEngine;

namespace Pathfinding
{
    public class SubGoalSearch : AStarSearch
    {
        private SubGoalGrid subGoalGrid;
        private HashSet<long> endNodes;

        public override List<Node> GetPath(IGrid grid, Vector2Int start, Vector2Int target)
        {
            subGoalGrid = (SubGoalGrid)grid;
            sizeY = grid.GetSize().y;
            endNodes = new HashSet<long>();
            var endSubGoals = subGoalGrid.GetDirectHReachable(target.x, target.y);
            if (endSubGoals.Count == 0)
                return null;
            
            foreach (var endSubGoal in endSubGoals)
            {
                endNodes.Add(endSubGoal.x * sizeY + endSubGoal.y);
            }

            return base.GetPath(grid, start, target);
        }

        protected override int NewGCost(Node currentNode, Node neighborNode)
        {
            // TODO: For teleporters this will change
            return currentNode.gCost + Heuristic(currentNode, neighborNode);
        }

        protected override int Heuristic(Node a, Node b)
        {
            return base.Heuristic(a, b);
            // return (int)Mathf.Sqrt((a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y));
        }

        protected override int GetNeighbors(Node currentNode, ref Position[] neighbors)
        {
            var index = currentNode.x * sizeY + currentNode.y;
            var addEndNode = false;
            if (currentNode.x == startNode.x && currentNode.y == startNode.y)
            {
                var subGoals = subGoalGrid.GetDirectHReachable(startNode.x, startNode.y);
                neighbors = new Position[subGoals.Count];
                for (var i = 0; i < subGoals.Count; i++)
                {
                    neighbors[i] = new Position(subGoals[i].x, subGoals[i].y);
                }

                return subGoals.Count;
            }
            else if (endNodes.Contains(index))
            {
                addEndNode = true;
            }

            var subGoal = subGoalGrid.subGoals[index];
            neighbors = new Position[addEndNode ? subGoal.edges.Length + 1 : subGoal.edges.Length];
            for (var i = 0; i < subGoal.edges.Length; i++)
            {
                neighbors[i] = new Position(subGoal.edges[i].toX, subGoal.edges[i].toY);
            }

            if (addEndNode)
                neighbors[neighbors.Length - 1] = new Position(targetNode.x, targetNode.y);

            return neighbors.Length;
        }
    }
}