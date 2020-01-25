﻿using Pathfinding;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChunkedPathFinding
{
    public class Graph
    {
        public struct VertexID
        {
            public int ID;
            public int ChunkID;
        }

        private IGrid grid;
        public int sizeY;
        public int chunkSize;
        public int chunkSizeX;
        public int chunkSizeY;
        public GraphChunk[] graphChunks;

        public Graph(IGrid grid, int chunkSize)
        {
            this.grid = grid;
            this.sizeY = grid.GetSize().y;
            this.chunkSize = chunkSize;
            this.chunkSizeX = grid.GetSize().x / chunkSize;
            this.chunkSizeY = sizeY / chunkSize;
            this.graphChunks = new GraphChunk[chunkSizeX * chunkSizeY];
        }

        public void BuildAll()
        {
            for (var i = 0; i < graphChunks.Length; i++)
                BuildChunk(i);
        }

        public void BuildChunk(int position)
        {
            var chunk = new GraphChunk(grid, position, chunkSize);
            chunk.Build();
            graphChunks[position] = chunk;
        }

        public void DrawCellBorders()
        {
            var chunkSizeX = grid.GetSize().x / chunkSize;
            var chunkSizeY = grid.GetSize().y / chunkSize;

            for (var x = 0; x < chunkSizeX; x++)
                for (var y = 0; y < chunkSizeY; y++)
                {
                    DebugDrawer.DrawNoOffset(new Vector2Int(x * chunkSize, y * chunkSize + chunkSize), new Vector2Int(x * chunkSize + chunkSize, y * chunkSize + chunkSize), Color.magenta);
                    DebugDrawer.DrawNoOffset(new Vector2Int(x * chunkSize + chunkSize, y * chunkSize + chunkSize), new Vector2Int(x * chunkSize + chunkSize, y * chunkSize), Color.magenta);
                }
        }

        /*
        public bool IsSubgoal(int gridPosition)
        {

        }

        public List<int> GetDirectHReachable(int x, int y)
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
        }*/

        public int GetChunkID(int x, int y)
        {
            return (x / chunkSize) * chunkSizeY + (y / chunkSize);
        }

        public int GetVertexID(int x, int y, int chunkID)
        {
            return graphChunks[chunkID].gridPositionToVertex[x * sizeY + y];
        }

        public int GetChunkID(int gridPosition)
        {
            return ((gridPosition / sizeY) / chunkSize) * chunkSizeY + ((gridPosition % sizeY) / chunkSize);
        }

        public Vertex GetVertexAtGridPosition(int gridPosition)
        {
            var chunk = GetChunk(GetChunkID(gridPosition));
            return chunk.vertices[chunk.gridPositionToVertex[gridPosition]];
        }

        public GraphChunk GetChunk(int chunkID)
        {
            return graphChunks[chunkID];
        }

        public void DrawGraph()
        {
            foreach (var chunk in graphChunks)
            {
                if (chunk == null)
                    continue;

                chunk.DrawGraph();
            }
        }
    }
}