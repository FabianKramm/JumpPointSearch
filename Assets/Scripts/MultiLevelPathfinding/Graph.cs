﻿using Pathfinding;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MultiLevelPathfinding
{
    // This is the subgoal graph 
    public class Graph
    {
        private static int[] LevelDimensions = new int[]
        {
            64,
            128,
            256
        };

        public struct Vertex
        {
            public int GridPosition;
            public int EdgeOffset;
        }

        public struct Edge
        {
            public int From;
            public int Target;
            public float Cost;
        }

        public List<Vertex> Vertices;
        public List<int> VertexEdgeMapping;
        public List<Edge> Edges;
        public List<ulong> VertexCellNumber;
        public int[] CellsPerLevel;

        public Dictionary<int, int> gridToVertexMapping;

        private IGrid grid;
        private int sizeY;
        public int[] offset;

        public Graph(IGrid grid)
        {
            this.grid = grid;
            this.sizeY = grid.GetSize().y;
            this.offset = new int[LevelDimensions.Length + 1];
            this.gridToVertexMapping = new Dictionary<int, int>();
            CellsPerLevel = new int[LevelDimensions.Length + 1];

            for (var i = 1; i < LevelDimensions.Length; i++)
            {
                var cellsPerLevelX = this.grid.GetSize().x / LevelDimensions[i - 1];
                var cellsPerLevelY = this.grid.GetSize().y / LevelDimensions[i - 1];

                CellsPerLevel[i] = cellsPerLevelX * cellsPerLevelY;
                this.offset[i] = this.offset[i - 1] + ((int)Math.Ceiling(Math.Log(cellsPerLevelX * cellsPerLevelY, 2)));
            }
        }

        public ulong getCellNumberOnLevel(int l, ulong cellNumber)
        {
            return (cellNumber & ~(ulong)(~0 << offset[l])) >> offset[l - 1];
        }

        public int getQueryLevel(ulong sCellNumber, ulong tCellNumber, ulong vCellNumber)
        {
            var l_sv = getHighestDifferingLevel(sCellNumber, vCellNumber);
            var l_tv = getHighestDifferingLevel(vCellNumber, tCellNumber);

            return l_sv < l_tv ? l_sv : l_tv;
        }

        public int getHighestDifferingLevel(ulong c1, ulong c2)
        {
            ulong diff = c1 ^ c2;
            if (diff == 0)
                return 0;

            for (int l = offset.Length - 1; l > 0; --l)
            {
                if (diff >> offset[l - 1] > 0)
                    return l;
            }

            return 0;
        }

        public ulong truncateToLevel(ulong cellNumber, int l)
        {
            return cellNumber >> offset[l - 1];
        }

        // ConstructSubgoals constructs subgoals within the specified area and tries to connect them
        public void ConstructSubgoals()
        {
            var startX = 0;
            var startY = 0;
            var endX = grid.GetSize().x;
            var endY = grid.GetSize().y;

            Vertices = new List<Vertex>();
            VertexCellNumber = new List<ulong>();
            gridToVertexMapping = new Dictionary<int, int>();

            // Construct subgoals
            for (var y = startY; y < endY; y++)
                for (var x = startX; x < endX; x++)
                {
                    if (grid.IsWalkable(x, y) == false)
                        continue;

                    for (var d = 4; d < 8; d++)
                    {
                        if (grid.IsWalkable(x + SubGoalGrid.directions[d][0], y + SubGoalGrid.directions[d][1]) == false)
                        {
                            if (grid.IsWalkable(x + SubGoalGrid.directions[d][0], y) && grid.IsWalkable(x, y + SubGoalGrid.directions[d][1]))
                            {
                                Vertices.Add(new Vertex()
                                {
                                    GridPosition = x * sizeY + y
                                });
                                VertexCellNumber.Add(GetCellNumber(x, y));
                                gridToVertexMapping[x * sizeY + y] = Vertices.Count - 1;
                            }
                        }
                    }
                }
        }

        // ConstructEdges constructs edges for subgoalds within the specified area
        public void ConstructEdges()
        {
            var startX = 0;
            var startY = 0;
            var endX = grid.GetSize().x;
            var endY = grid.GetSize().y;

            Edges = new List<Edge>();
            VertexEdgeMapping = new List<int>();
            Dictionary<long, int> createdEdges = new Dictionary<long, int>();
            for (var i = 0; i < Vertices.Count; i++)
            {
                var x = Vertices[i].GridPosition / sizeY;
                var y = Vertices[i].GridPosition % sizeY;
                if (x < startX || x >= endX || y < startY || y >= endY)
                    continue;

                var otherSubGoals = GetDirectHReachable(x, y);
                Vertices[i] = new Vertex()
                {
                    EdgeOffset = otherSubGoals.Count > 0 ? VertexEdgeMapping.Count : -1,
                    GridPosition = Vertices[i].GridPosition
                };
                
                for (int j = 0, count = otherSubGoals.Count; j < count; j++)
                {
                    var otherSubGoal = otherSubGoals[j];
                    if (otherSubGoal == i)
                        continue;

                    var ox = Vertices[otherSubGoal].GridPosition / sizeY;
                    var oy = Vertices[otherSubGoal].GridPosition % sizeY;
                    var edgeID = otherSubGoal << 32 | i;
                    if (createdEdges.TryGetValue(edgeID, out int edgeOffset) == false)
                    {
                        Edges.Add(new Edge()
                        {
                            From = i,
                            Target = otherSubGoal,
                            Cost = SubGoalGrid.Diagonal(x, y, ox, oy)
                        });

                        createdEdges[i << 32 | otherSubGoal] = Edges.Count - 1;
                        VertexEdgeMapping.Add(createdEdges[i << 32 | otherSubGoal]);
                    }
                    else
                    {
                        VertexEdgeMapping.Add(edgeOffset);
                    }
                }
            }
        }

        public List<int> GetDirectHReachable(int x, int y)
        {
            var reachable = new List<int>();

            // Get cardinal reachable
            for (int i = 0; i < 8; i++)
            {
                var clearance = Clearance(x, y, SubGoalGrid.directions[i][0], SubGoalGrid.directions[i][1]);
                var subgoal = new Position(x + clearance * SubGoalGrid.directions[i][0], y + clearance * SubGoalGrid.directions[i][1]);
                if (gridToVertexMapping.TryGetValue(subgoal.x * sizeY + subgoal.y, out int vertexRef))
                    reachable.Add(vertexRef);
            }

            // Get diagonal reachable
            for (int d = 4; d < 8; d++)
            {
                for (int c = 0; c <= 1; c++)
                {
                    var cx = c == 0 ? SubGoalGrid.directions[d][0] : 0;
                    var cy = c == 0 ? 0 : SubGoalGrid.directions[d][1];

                    var max = Clearance(x, y, cx, cy);
                    var diag = Clearance(x, y, SubGoalGrid.directions[d][0], SubGoalGrid.directions[d][1]);
                    if (IsSubGoal(x + max * cx, y + max * cy))
                        max--;
                    if (IsSubGoal(x + diag * SubGoalGrid.directions[d][0], y + diag * SubGoalGrid.directions[d][1]))
                        diag--;

                    for (int i = 1; i < diag; i++)
                    {
                        var newPosX = x + i * SubGoalGrid.directions[d][0];
                        var newPosY = y + i * SubGoalGrid.directions[d][1];
                        var j = Clearance(newPosX, newPosY, cx, cy);
                        if (j <= max && gridToVertexMapping.TryGetValue((newPosX + j * cx) * sizeY + (newPosY + j * cy), out int vertexRef))
                        {
                            reachable.Add(vertexRef);
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

        public int Clearance(int x, int y, int dx, int dy)
        {
            int i = 0;
            while (true)
            {
                if (!grid.IsWalkable(x + i * dx, y + i * dy))
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

        public bool IsSubGoal(int x, int y)
        {
            return gridToVertexMapping.ContainsKey(x * sizeY + y);
        }

        public ulong GetCellNumber(int x, int y)
        {
            ulong cellNumber = 0;
            for (var i = 0; i < LevelDimensions.Length; i++)
            {
                var cellX = x / LevelDimensions[i];
                var cellY = y / LevelDimensions[i];
                var cellRows = this.grid.GetSize().y / LevelDimensions[i];

                cellNumber = (cellNumber << offset[i + 1]) | ((ulong)cellX * (ulong)cellRows + (ulong)cellY);
            }

            return cellNumber;
        }

        public void DrawGraph()
        {
            for (var i = 0; i < Vertices.Count; i++)
            {
                var vertex = Vertices[i];
                var x = vertex.GridPosition / sizeY;
                var y = vertex.GridPosition % sizeY;

                DebugDrawer.DrawCube(new UnityEngine.Vector2Int(x, y), Vector2Int.one, Color.yellow);

                var edgeEnd = (i + 1 == Vertices.Count) ? Vertices.Count : Vertices[i + 1].EdgeOffset;
                for (var j = vertex.EdgeOffset; j < edgeEnd; j++)
                {
                    var target = Edges[j].From == i ? Edges[j].Target : Edges[j].From;
                    var tx = Vertices[target].GridPosition / sizeY;
                    var ty = Vertices[target].GridPosition % sizeY;
                    DebugDrawer.Draw(new Vector2Int(x, y), new Vector2Int(tx, ty), Color.yellow);
                }
            }
        }
    }
}
