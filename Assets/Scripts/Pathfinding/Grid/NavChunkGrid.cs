using System;
using UnityEngine;

namespace Pathfinding
{
    public class NavChunkGrid : IGrid
    {
        private object s_sync = new object();

        public int SizeX;
        public int SizeY;

        private int chunkCols;
        private int chunkRows;
        private int chunkSize;

        private IGrid grid;
        private NavChunk[] chunks;

        public NavChunkGrid(IGrid grid, int sizeX, int sizeY, int chunkSize = 64)
        {
            SizeX = sizeX;
            SizeY = sizeY;
            if (SizeX % chunkSize != 0 || SizeY % chunkSize != 0)
            {
                Debug.LogError("Size x & y needs to be divisable by chunk size");
                return;
            }
            if (grid.GetSize().x != SizeX || grid.GetSize().y != SizeY)
            {
                Debug.LogError("Nav Chunk grid has different size than the base grid");
                return;
            }

            this.grid = grid;
            this.chunkSize = chunkSize;
            this.chunkCols = SizeX / chunkSize;
            this.chunkRows = SizeY / chunkSize;
            this.chunks = new NavChunk[(SizeX / chunkSize) * (SizeY / chunkSize)];
        }

        public void BuildNavPoints()
        {
            for (var x = 0; x < chunkCols; x++)
                for (var y = 0; y < chunkRows; y++)
                {
                    var idx = x * chunkRows + y;
                    var chunk = chunks[idx];
                    if (chunk == null)
                    {
                        chunk = new NavChunk(this, x, y, chunkSize);
                        chunks[idx] = chunk;
                    }

                    chunk.BuildNavPoints(grid);
                }
        }

        public void ShowDebug()
        {
            foreach(var chunk in chunks)
            {
                if (chunk == null)
                    continue;

                chunk.ShowDebug();
            }
        }

        private NavChunk LoadChunk(int chunkX, int chunkY)
        {
            throw new NotImplementedException();
        }

        public bool IsWalkable(int x, int y)
        {
            throw new NotImplementedException();
        }

        public void SetWeight(int x, int y, int weight)
        {
            throw new NotImplementedException();
        }

        public CellType GetWeight(int x, int y)
        {
            throw new NotImplementedException();
        }

        public Position GetSize()
        {
            return new Position((int)SizeX, (int)SizeY);
        }

        public bool IsDirectHReachable(IGrid grid, int x, int y, int subGoalX, int subGoalY)
        {
            // Get cardinal reachable
            for (int i = 0; i < 8; i++)
            {
                var clearance = SubGoalGrid.ClearanceWithSubgoal(this, x, y, SubGoalGrid.directions[i][0], SubGoalGrid.directions[i][1], subGoalX, subGoalY);
                var subgoal = new Position(x + clearance * SubGoalGrid.directions[i][0], y + clearance * SubGoalGrid.directions[i][1]);
                if (subGoalX == subgoal.x && subGoalY == subgoal.y)
                    return true;
            }

            // Get diagonal reachable
            for (int d = 4; d < 8; d++)
            {
                for (int c = 0; c <= 1; c++)
                {
                    var cx = c == 0 ? SubGoalGrid.directions[d][0] : 0;
                    var cy = c == 0 ? 0 : SubGoalGrid.directions[d][1];
                    var diag = SubGoalGrid.ClearanceWithSubgoal(this, x, y, SubGoalGrid.directions[d][0], SubGoalGrid.directions[d][1], subGoalX, subGoalY);
                    var max = SubGoalGrid.ClearanceWithSubgoal(this, x, y, cx, cy, subGoalX, subGoalY);

                    for (int i = 1; i < diag; i++)
                    {
                        var newPosX = x + i * SubGoalGrid.directions[d][0];
                        var newPosY = y + i * SubGoalGrid.directions[d][1];
                        var j = SubGoalGrid.ClearanceWithSubgoal(this, newPosX, newPosY, cx, cy, subGoalX, subGoalY);
                        if (j <= max && subGoalX == (newPosX + j * cx) && subGoalY == (newPosY + j * cy))
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
    }
}