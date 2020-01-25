using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pathfinding
{
    public class SubGoalGrid : IGrid
    {
        public static int[][] directions = new int[][]
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

        private SubGoalBiDirectionalDijkstraSearch subGoalSearch;

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
                if (i >= 200 || !grid.IsWalkable(x + i * dx, y + i * dy))
                {
                    return i;
                }

                if (dx != 0 && dy != 0 && (!grid.IsWalkable(x + (i + 1) * dx, y + i * dy) || !grid.IsWalkable(x + i * dx, y + (i + 1) * dy)))
                {
                    return i;
                }

                i = i + 1;
                if (IsSubGoal(x + i * dx, y + i * dy))
                {
                    return i;
                }
            }
        }

        public static int ClearanceWithSubgoal(IGrid grid, int x, int y, int dx, int dy, int subGoalX, int subGoalY)
        {
            int i = 0;
            while (true)
            {
                if (!grid.IsWalkable(x + i * dx, y + i * dy))
                    return i;

                i = i + 1;
                if(subGoalX == (x + i * dx) && subGoalY == (y + i * dy))
                    return i;
            }
        }

        /*
        public float CostOtherPath(SubGoal from, SubGoal to, SubGoal without)
        {
            if (subGoalSearch == null)
            {
                subGoalSearch = new SubGoalBiDirectionalDijkstraSearch(this);
            }
            else
            {
                subGoalSearch.Reset();
            }

            subGoalSearch.FakeRemoveSubGoal(without);
            var path = subGoalSearch.GetPath(new Vector2Int(from.x, from.y), new Vector2Int(to.x, to.y));
            if (path == null)
            {
                return -1;
            }

            float cost = 0;
            for (var i = 1; i < path.Count; i++)
            {
                cost += ;
            }

            return cost;
        }
        /*
        public bool IsNecessaryToConnect(SubGoal from, SubGoal to, SubGoal without)
        {
            if (IsDirectHReachable(from.x, from.y, to))
                return false;

            var costOtherPath = CostOtherPath(from, to, without);
            if (costOtherPath == -1)
            {
                return true;
            }
            if (costOtherPath <= Diagonal(without, from) + Diagonal(without, to))
            {
                return false;
            }

            return true;
        }

        public SubGoal GetSubGoal(int x, int y)
        {
            return subGoals[x * sizeY + y];
        }

        public void PruneSubGoal(SubGoal subGoal)
        {
            for (var i = 0; i < subGoal.edges.Length; i++)
            {
                for (var j = 0; j < subGoal.edges.Length; j++)
                {
                    if (i == j)
                        continue;

                    var subGoal1 = GetSubGoal(subGoal.edges[i].toX, subGoal.edges[i].toY);
                    var subGoal2 = GetSubGoal(subGoal.edges[j].toX, subGoal.edges[j].toY);
                    var subGoalCostWith = Diagonal(subGoal, subGoal1) + Diagonal(subGoal, subGoal2);
                    if (Diagonal(subGoal1, subGoal2) == subGoalCostWith)
                    {
                        if (CostOtherPath(subGoal1, subGoal2, subGoal) > subGoalCostWith)
                        {
                            
                        }
                    }
                }
            }
        }

        public SubGoalGrid PruneSubGoals()
        {
            var prunedGrid = new SubGoalGrid(grid);

            // Copy graph
            foreach(var subGoalKV in subGoals)
            {
                prunedGrid.subGoals[subGoalKV.Key] = subGoalKV.Value.Clone();
            }

            // Prune Graph
            List<SubGoal> prunedSubGoals = new List<SubGoal>();
            foreach(var subGoal in prunedGrid.subGoals.Values)
            {
                var necessary = false;
                for (var i = 0; i < subGoal.edges.Length && necessary == false; i++)
                {
                    for (var j = 0; j < subGoal.edges.Length && necessary == false; j++)
                    {
                        if (i == j)
                            continue;

                        if (prunedGrid.IsNecessaryToConnect(prunedGrid.GetSubGoal(subGoal.edges[i].toX, subGoal.edges[i].toY), prunedGrid.GetSubGoal(subGoal.edges[j].toX, subGoal.edges[j].toY), subGoal))
                        {
                            necessary = true;
                            continue;
                        }
                    }
                }

                if (necessary == false)
                {
                    prunedGrid.PruneSubGoal(subGoal);
                    prunedSubGoals.Add(subGoal);
                }
            }

            // Delete SubGoals from dictionary
            foreach(var subGoal in prunedSubGoals)
            {
                prunedGrid.subGoals.Remove(subGoal.x * sizeY + subGoal.y);
            }

            return prunedGrid;
        }*/

        public static float Diagonal(int ax, int ay, int bx, int by)
        {
            var dx = Math.Abs(ax - bx);
            var dy = Math.Abs(ay - by);

            return AStarSearch.LateralCost * (float)(dx + dy) + (AStarSearch.DiagonalCost - 2f * AStarSearch.LateralCost) * (float)Math.Min(dx, dy);
            
            //return Mathf.Sqrt((ax - bx) * (ax - bx) + (ay - by) * (ay - by));
        }

        public static float Diagonal(SubGoal a, SubGoal b)
        {
            var dx = Math.Abs(a.x - b.x);
            var dy = Math.Abs(a.y - b.y);

            return AStarSearch.LateralCost * (float)(dx + dy) + (AStarSearch.DiagonalCost - 2f * AStarSearch.LateralCost) * (float)Math.Min(dx, dy);
            
            //return Mathf.Sqrt((a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y));
        }

        public static float Diagonal(Node a, Node b)
        {
            var dx = Math.Abs(a.x - b.x);
            var dy = Math.Abs(a.y - b.y);

            return AStarSearch.LateralCost * (float)(dx + dy) + (AStarSearch.DiagonalCost - 2f * AStarSearch.LateralCost) * (float)Math.Min(dx, dy);
            
            //return Mathf.Sqrt((a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y));    
        }

        public bool IsDirectHReachable(int x, int y, SubGoal subGoal)
        {
            // Get cardinal reachable
            for (int i = 0; i < 8; i++)
            {
                var clearance = ClearanceWithSubgoal(grid, x, y, directions[i][0], directions[i][1], subGoal.x, subGoal.y);
                var subgoal = new Position(x + clearance * directions[i][0], y + clearance * directions[i][1]);
                if (subGoals.TryGetValue(subgoal.x * sizeY + subgoal.y, out SubGoal subGoalRef) && subGoalRef == subGoal)
                    return true;
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
                        if (j <= max && subGoals.TryGetValue((newPosX + j * cx) * sizeY + (newPosY + j * cy), out SubGoal subGoalRef) && subGoalRef == subGoal)
                        {
                            return true;
                        }
                        if (j < max)
                        {
                            max = j;
                        }
                    }
                }
            }

            return false;
        }
        
        public List<SubGoal> GetDirectHReachable(int x, int y)
        {
            var reachable = new List<SubGoal>();

            // Get cardinal reachable
            for (int i = 0; i < 8; i++)
            {
                var clearance = Clearance(x, y, directions[i][0], directions[i][1]);
                var subgoal = new Position(x + clearance * directions[i][0], y + clearance * directions[i][1]);
                if (grid.IsWalkable(subgoal.x, subgoal.y) && subGoals.TryGetValue(subgoal.x * sizeY + subgoal.y, out SubGoal subGoalRef))
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
                        if (j <= max && grid.IsWalkable(newPosX + j * cx, newPosY + j * cy) && subGoals.TryGetValue((newPosX + j * cx) * sizeY + (newPosY + j * cy), out SubGoal subGoal))
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

        public float GetWeight(Node current, Node neighbor)
        {
            var edges = subGoals[current.x * sizeY + current.y];
            for (var i = 0; i < edges.edges.Length; i++)
            {
                if (edges.edges[i].toX == neighbor.x && edges.edges[i].toY == neighbor.y)
                    return (int)edges.edges[i].cost;
            }

            return 0;
        }

        public CellType GetWeight(int x, int y)
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