using Pathfinding;
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
            public int GridPosition;
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

        public VertexID GetVertexID(int x, int y)
        {
            var chunkID = (x / chunkSize) * chunkSizeY + (y / chunkSize);
            if (GetChunk(chunkID).gridPositionToVertex.TryGetValue(x * sizeY + y, out int vertexID))
            {
                return new VertexID
                {
                    ID = vertexID,
                    ChunkID = chunkID,
                    GridPosition = x * sizeY + y,
                };
            }

            return new VertexID
            {
                ID = -1
            };
        }

        public List<VertexID> GetDirectHReachable(int x, int y)
        {
            var reachable = new List<VertexID>();

            // Get cardinal reachable
            for (int i = 0; i < 8; i++)
            {
                var clearance = Clearance(x, y, SubGoalGrid.directions[i][0], SubGoalGrid.directions[i][1]);
                var subgoal = new Position(x + clearance * SubGoalGrid.directions[i][0], y + clearance * SubGoalGrid.directions[i][1]);
                if (grid.IsWalkable(subgoal.x, subgoal.y))
                {
                    var vertex = GetVertexID(subgoal.x, subgoal.y);
                    if (vertex.ID != -1)
                        reachable.Add(vertex);
                }
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

                    var mx = x + max * cx;
                    var my = y + max * cy; 
                    if (grid.IsWalkable(mx, my) && GetVertexID(mx, my).ID != -1)
                        max--;

                    var wx = x + diag * SubGoalGrid.directions[d][0];
                    var wy = y + diag * SubGoalGrid.directions[d][1];
                    if (grid.IsWalkable(wx, wy) && GetVertexID(wx, wy).ID != -1)
                        diag--;

                    for (int i = 1; i < diag; i++)
                    {
                        var newPosX = x + i * SubGoalGrid.directions[d][0];
                        var newPosY = y + i * SubGoalGrid.directions[d][1];
                        var j = Clearance(newPosX, newPosY, cx, cy);

                        var subGoalPosX = newPosX + j * cx;
                        var subGoalPosY = newPosY + j * cy;
                        if (j <= max && grid.IsWalkable(subGoalPosX, subGoalPosY))
                        {
                            var vertex = GetVertexID(subGoalPosX, subGoalPosY);
                            if (vertex.ID != -1)
                            {
                                reachable.Add(vertex);
                                j--;
                            }
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
                if (i >= 200 || !grid.IsWalkable(x + i * dx, y + i * dy))
                {
                    return i;
                }

                if (dx != 0 && dy != 0 && (!grid.IsWalkable(x + (i + 1) * dx, y + i * dy) || !grid.IsWalkable(x + i * dx, y + (i + 1) * dy)))
                {
                    return i;
                }

                i = i + 1;
                if (GetVertexID((x + i * dx), (y + i * dy)).ID != -1)
                {
                    return i;
                }
            }
        }

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
