﻿using Pathfinding;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChunkedPathFinding
{
    public class GraphChunk
    {
        private IGrid grid;

        public int chunkNumber;
        private int chunkSize;
        private int chunkSizeY;
        private int sizeY;

        public Vertex[] vertices;
        public int[] vertexEdgeMapping;
        public Edge[] edges;
        
        public Dictionary<int, int> gridPositionToVertex;

        public GraphChunk(IGrid grid, int chunkNumber, int chunkSize)
        {
            this.grid = grid;
            this.chunkNumber = chunkNumber;
            this.sizeY = grid.GetSize().y;
            this.chunkSize = chunkSize;
            this.chunkSizeY = sizeY / chunkSize;
        }

        public void Build()
        {
            var (nv, nvd) = ConstructVertices();

            ConstructEdges(nv, nvd);
        }

        // ConstructVertices constructs vertices within the specified area and tries to connect them
        public (List<Vertex>, Dictionary<int, int>) ConstructVertices()
        {
            var chunkX = chunkNumber / chunkSizeY;
            var chunkY = chunkNumber % chunkSizeY;

            var startX = Math.Max(0, chunkX * chunkSize - chunkSize);
            var startY = Math.Max(0, chunkY * chunkSize - chunkSize);
            var endX = Math.Min(grid.GetSize().x, chunkX * chunkSize + chunkSize * 2);
            var endY = Math.Min(grid.GetSize().y, chunkY * chunkSize + chunkSize * 2);

            var chunkStartX = chunkX * chunkSize;
            var chunkStartY = chunkY * chunkSize;
            var chunkEndX = chunkX * chunkSize + chunkSize;
            var chunkEndY = chunkY * chunkSize + chunkSize;

            var includingNeighborVertices = new List<Vertex>();
            var includingNeighborGridPositionToVertex = new Dictionary<int, int>();

            var vertices = new List<Vertex>();
            gridPositionToVertex = new Dictionary<int, int>();

            // Construct subgoals
            for (var y = startY; y < endY; y++)
                for (var x = startX; x < endX; x++)
                {
                    if (grid.IsWalkable(x, y) == false)
                        continue;

                    var gridPosition = x * sizeY + y;
                    for (var d = 4; d < 8; d++)
                    {
                        if (grid.IsWalkable(x + SubGoalGrid.directions[d][0], y + SubGoalGrid.directions[d][1]) == false)
                        {
                            if (grid.IsWalkable(x + SubGoalGrid.directions[d][0], y) && grid.IsWalkable(x, y + SubGoalGrid.directions[d][1]))
                            {
                                includingNeighborVertices.Add(new Vertex
                                {
                                    GridPosition = gridPosition,
                                });
                                includingNeighborGridPositionToVertex[gridPosition] = includingNeighborVertices.Count - 1;

                                if (!(x < chunkStartX || x >= chunkEndX || y < chunkStartY || y >= chunkEndY))
                                {
                                    vertices.Add(new Vertex()
                                    {
                                        GridPosition = gridPosition
                                    });
                                    gridPositionToVertex[gridPosition] = vertices.Count - 1;
                                }

                                break;
                            }
                        }
                    }
                }

            this.vertices = vertices.ToArray();
            return (includingNeighborVertices, includingNeighborGridPositionToVertex);
        }

        // ConstructEdges constructs edges for subgoalds within the specified area
        public void ConstructEdges(List<Vertex> includingNeighborVertices, Dictionary<int, int> includingNeighborGridPositionToVertex)
        {
            var chunkX = chunkNumber / chunkSizeY;
            var chunkY = chunkNumber % chunkSizeY;

            var startX = chunkX * chunkSize;
            var startY = chunkY * chunkSize;
            var endX = chunkX * chunkSize + chunkSize;
            var endY = chunkY * chunkSize + chunkSize;

            var edges = new List<Edge>();
            var vertexEdgeMapping = new List<int>();

            var createdEdges = new Dictionary<ulong, int>();
            for (var i = 0; i < includingNeighborVertices.Count; i++)
            {
                var x = includingNeighborVertices[i].GridPosition / sizeY;
                var y = includingNeighborVertices[i].GridPosition % sizeY;
                var gridPosition = x * sizeY + y;
                if (x < startX || x >= endX || y < startY || y >= endY)
                    continue;

                var otherSubGoals = GetDirectHReachable(x, y, includingNeighborGridPositionToVertex);

                // Get the real vertex id
                if (gridPositionToVertex.TryGetValue(gridPosition, out int vID))
                {
                    vertices[vID] = new Vertex
                    {
                        EdgeOffset = vertexEdgeMapping.Count,
                        GridPosition = gridPosition
                    };
                }
                else
                {
                    throw new Exception("This should never happen");
                }

                for (int j = 0, count = otherSubGoals.Count; j < count; j++)
                {
                    var otherSubGoal = otherSubGoals[j];
                    if (otherSubGoal == i)
                        continue;

                    var ox = includingNeighborVertices[otherSubGoal].GridPosition / sizeY;
                    var oy = includingNeighborVertices[otherSubGoal].GridPosition % sizeY;
                    var oGridPosition = includingNeighborVertices[otherSubGoal].GridPosition;
                    var isOutsideOfChunk = (ox < startX || ox >= endX || oy < startY || oy >= endY);

                    var edgeID = (ulong)otherSubGoal << 32 | (ulong)(uint)i;
                    if (createdEdges.TryGetValue(edgeID, out int edgeOffset) == false)
                    {
                        var toVertexID = -1;
                        if (isOutsideOfChunk)
                        {
                            toVertexID = -1;
                        }
                        else if (gridPositionToVertex.TryGetValue(oGridPosition, out int oID))
                        {
                            toVertexID = oID;
                        }
                        else
                        {
                            throw new Exception("This should never happen");
                        }

                        edges.Add(new Edge()
                        {
                            FromVertex = vID,
                            ToVertex = toVertexID,
                            ToVertexGridPosition = oGridPosition,
                            Cost = SubGoalGrid.Diagonal(x, y, ox, oy)
                        });

                        var newEdgeID = (ulong)i << 32 | (ulong)(uint)otherSubGoal;
                        createdEdges[newEdgeID] = edges.Count - 1;
                        vertexEdgeMapping.Add(edges.Count - 1);
                    }
                    else
                    {
                        vertexEdgeMapping.Add(edgeOffset);
                    }
                }
            }

            this.edges = edges.ToArray();
            this.vertexEdgeMapping = vertexEdgeMapping.ToArray();
        }

        public List<int> GetDirectHReachable(int x, int y, Dictionary<int, int> subGoals)
        {
            var reachable = new List<int>();

            // Get cardinal reachable
            for (int i = 0; i < 8; i++)
            {
                var clearance = Clearance(x, y, SubGoalGrid.directions[i][0], SubGoalGrid.directions[i][1], subGoals);
                var subgoal = new Position(x + clearance * SubGoalGrid.directions[i][0], y + clearance * SubGoalGrid.directions[i][1]);
                if (grid.IsWalkable(subgoal.x, subgoal.y) && subGoals.TryGetValue(subgoal.x * sizeY + subgoal.y, out int vertexRef))
                {
                    reachable.Add(vertexRef);
                }
            }

            // Get diagonal reachable
            for (int d = 4; d < 8; d++)
            {
                for (int c = 0; c <= 1; c++)
                {
                    var cx = c == 0 ? SubGoalGrid.directions[d][0] : 0;
                    var cy = c == 0 ? 0 : SubGoalGrid.directions[d][1];

                    var max = Clearance(x, y, cx, cy, subGoals);
                    var diag = Clearance(x, y, SubGoalGrid.directions[d][0], SubGoalGrid.directions[d][1], subGoals);
                    if (subGoals.ContainsKey((x + max * cx) * sizeY + (y + max * cy)))
                        max--;
                    if (subGoals.ContainsKey((x + diag * SubGoalGrid.directions[d][0]) * sizeY + (y + diag * SubGoalGrid.directions[d][1])))
                        diag--;

                    for (int i = 1; i < diag; i++)
                    {
                        var newPosX = x + i * SubGoalGrid.directions[d][0];
                        var newPosY = y + i * SubGoalGrid.directions[d][1];
                        var j = Clearance(newPosX, newPosY, cx, cy, subGoals);

                        var subGoalPosX = newPosX + j * cx;
                        var subGoalPosY = newPosY + j * cy;
                        if (j <= max && grid.IsWalkable(subGoalPosX, subGoalPosY) && subGoals.TryGetValue(subGoalPosX * sizeY + subGoalPosY, out int vertexRef))
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

        public int Clearance(int x, int y, int dx, int dy, Dictionary<int, int> subGoals)
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
                if (subGoals.ContainsKey((x + i * dx) * sizeY + (y + i * dy)))
                {
                    return i;
                }
            }
        }
        
        public void DrawGraph()
        {
            for (var i = 0; i < vertices.Length; i++)
            {
                var vertex = vertices[i];
                var x = vertex.GridPosition / sizeY;
                var y = vertex.GridPosition % sizeY;

                DebugDrawer.DrawCube(new UnityEngine.Vector2Int(x, y), Vector2Int.one, Color.yellow);
                var edgeEnd = (i + 1 == vertices.Length) ? vertexEdgeMapping.Length : vertices[i + 1].EdgeOffset;
                if (edgeEnd == -1)
                    continue;

                for (var j = vertex.EdgeOffset; j < edgeEnd; j++)
                {
                    var edge = edges[vertexEdgeMapping[j]];
                    var target = edge.FromVertex == i ? edge.ToVertex : edge.FromVertex;
                    if (target == -1)
                    {
                        var tx = edge.ToVertexGridPosition / sizeY;
                        var ty = edge.ToVertexGridPosition % sizeY;
                        DebugDrawer.Draw(new Vector2Int(x, y), new Vector2Int(tx, ty), Color.yellow);
                    }
                    else
                    {
                        var tx = vertices[target].GridPosition / sizeY;
                        var ty = vertices[target].GridPosition % sizeY;
                        DebugDrawer.Draw(new Vector2Int(x, y), new Vector2Int(tx, ty), Color.yellow);
                    }
                }
            }
        }
    }
}
