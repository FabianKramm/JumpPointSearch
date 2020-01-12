using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pathfinding
{
    public class SubGoalGrid : IGrid
    {
        private static int[][] directions = new int[][]
        {
            // Cardinal Directions
            new int[]
            {
                0, 1
            },
            new int[]
            {
                1, 0
            },
            new int[]
            {
                0, -1
            },
            new int[]
            {
                -1, 0
            },
            // Diagonal Directions
            new int[]
            {
                -1, -1
            },
            new int[]
            {
                -1, 1
            },
            new int[]
            {
                1, 1
            },
            new int[]
            {
                1, -1
            },
        };

        private IGrid grid;
        private long sizeY;
        public Dictionary<long, SubGoal> subGoals;

        public SubGoalGrid(IGrid grid)
        {
            this.grid = grid;
            this.sizeY = grid.GetSize().y;
            this.subGoals = new Dictionary<long, SubGoal>();
        }

        // ConstructSubgoals constructs subgoals within the specified area and tries to connect them
        public void ConstructSubgoals(int startX, int startY, int endX, int endY)
        {
            // Construct subgoals
            for(var x = startX; x < endX; x++)
                for (var y = startY; y < endY; y++)
                {
                    if (grid.IsWalkable(x, y) == false)
                        continue;

                    for (var d = 4; d < 8; d++)
                    {
                        if (grid.IsWalkable(x + directions[d][0], y + directions[d][1]) == false)
                        {
                            if (grid.IsWalkable(x + directions[d][0], y) && grid.IsWalkable(x, y + directions[d][1]))
                            {
                                subGoals[x * sizeY + y] = new SubGoal(x, y);
                            }
                        }
                    }
                }
        }

        // ConstructEdges constructs edges for subgoalds within the specified area
        public void ConstructEdges(int startX, int startY, int endX, int endY)
        {
            foreach(var subgoal in subGoals.Values)
            {
                if (subgoal.x < startX || subgoal.x >= endX || subgoal.y < startY || subgoal.y >= endY)
                    continue;

                var otherSubGoals = GetDirectHReachable(subgoal.x, subgoal.y);
                subgoal.edges = new SubGoalEdge[otherSubGoals.Count];
                for (int i = 0, count = otherSubGoals.Count; i < count; i++)
                {
                    var otherSubGoal = otherSubGoals[i];
                    subgoal.edges[i] = new SubGoalEdge(otherSubGoal.x, otherSubGoal.y, Diagonal(subgoal, otherSubGoal));
                }
            }
        }

        public int Clearance(int x, int y, int dx, int dy)
        {
            int i = 0;
            while (true)
            {
                if (!grid.IsWalkable(x + i * dx, y + i * dy))
                    return i;

                i = i + 1;
                if (IsSubGoal(x + i * dx, y + i * dy))
                    return i;
            }
        }

        public List<SubGoal> GetDirectHReachable(int x, int y)
        {
            var reachable = new List<SubGoal>();

            // Get cardinal reachable
            for (int i = 0; i < 8; i++)
            {
                var clearance = Clearance(x, y, directions[i][0], directions[i][1]);
                var subgoal = new Position(x +clearance * directions[i][0], y + clearance * directions[i][1]);
                if (subGoals.TryGetValue(subgoal.x * sizeY + subgoal.y, out SubGoal subGoalRef))
                    reachable.Add(subGoalRef);
            }

            // Get diagonal reachable
            for (int d = 4; d < 8; d++)
            {
                for (int c = 0; c <= 1; c++)
                {
                    var cx = c == 0 ? directions[d][0] : 0;
                    var cy = c == 0 ? 0 : directions[d][1];

                    var max = Clearance(x, y, cx, cy);
                    var diag = Clearance(x, y, directions[d][0], directions[d][1]);
                    if (IsSubGoal(x + max * cx, y + max * cy))
                        max--;
                    if (IsSubGoal(x + diag * directions[d][0], y + diag * directions[d][1]))
                        diag--;

                    for (int i = 1; i < diag; i++)
                    {
                        var newPosX = x + i * directions[d][0];
                        var newPosY = y + i * directions[d][1];
                        var j = Clearance(newPosX, newPosY, cx, cy);
                        if (j <= max && subGoals.TryGetValue((newPosX + j * cx) * sizeY + (newPosY + j * cy), out SubGoal subGoal))
                        {
                            reachable.Add(subGoal);
                            j--;
                        }
                        if (j < max)
                        {
                            max = j;
                        }
                    }
                }
            }

            return reachable;
        }

        public bool IsSubGoal(int x, int y)
        {
            return subGoals.ContainsKey(x * sizeY + y);
        }

        private int Diagonal(SubGoal a, SubGoal b)
        {
            var dx = Math.Abs(a.x - b.x);
            var dy = Math.Abs(a.y - b.y);

            return (int)(AStarSearch.LateralCost * (dx + dy) + (AStarSearch.DiagonalCost - 2 * AStarSearch.LateralCost) * Math.Min(dx, dy));
        }

        public void DrawSubGoals()
        {
            foreach (var subgoal in subGoals.Values)
            {
                DebugDrawer.DrawCube(new UnityEngine.Vector2Int(subgoal.x, subgoal.y), Vector2Int.one, Color.yellow);
                
                foreach (var edge in subgoal.edges)
                {
                    DebugDrawer.Draw(new Vector2Int(subgoal.x, subgoal.y), new Vector2Int(edge.toX, edge.toY), Color.yellow);
                }
            }
        }

        public Position GetSize()
        {
            return grid.GetSize();
        }

        public int GetWeight(Node current, Node neighbor)
        {
            var edges = subGoals[current.x * sizeY + current.y];
            for (var i = 0; i < edges.edges.Length; i++)
            {
                if (edges.edges[i].toX == neighbor.x && edges.edges[i].toY == neighbor.y)
                    return edges.edges[i].cost;
            }

            return 0;
        }

        public int GetWeight(int x, int y)
        {
            throw new NotImplementedException();
        }

        public void SetWeight(int x, int y, int weight)
        {
            throw new NotImplementedException();
        }

        public bool IsWalkable(int x, int y)
        {
            throw new NotImplementedException();
        }
    }
}