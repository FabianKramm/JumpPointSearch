using Pathfinding;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MultiLevelPathfinding
{
    public class OverlayGraph
    {
        public static int[] LevelDimensions = new int[]
        {
            16,
            32,
            // 256
        };

        private IGrid grid;
        public int[] offset;
        public int[] cellsPerLevel;

        public int chunkSize;
        public Dictionary<int, OverlayGraphChunk> graphChunks;

        public OverlayGraph(IGrid grid)
        {
            this.graphChunks = new Dictionary<int, OverlayGraphChunk>();
            this.grid = grid;
            this.chunkSize = LevelDimensions[LevelDimensions.Length - 1];
            this.offset = new int[LevelDimensions.Length + 1];
            this.cellsPerLevel = new int[LevelDimensions.Length + 1];

            for (var i = 1; i < LevelDimensions.Length + 1; i++)
            {
                var cellsPerLevelX = this.chunkSize / LevelDimensions[i - 1];
                var cellsPerLevelY = this.chunkSize / LevelDimensions[i - 1];

                cellsPerLevel[i] = cellsPerLevelX * cellsPerLevelY;
                if (cellsPerLevel[i] == 0)
                {
                    offset[i] = offset[i - 1];
                    continue;
                }

                // var offsetOnLevel = Math.Max(1, );
                this.offset[i] = this.offset[i - 1] + ((int)Math.Ceiling(Math.Log(cellsPerLevelX * cellsPerLevelY, 2)));
            }
        }

        public void BuildChunk(int position)
        {
            var chunk = new OverlayGraphChunk(this, grid, position);
            chunk.Build();
            graphChunks[position] = chunk;
        }

        public void DrawCellBorders()
        {
            for (var i = 0; i < OverlayGraph.LevelDimensions.Length; i++)
            {
                var chunkSizeX = grid.GetSize().x / OverlayGraph.LevelDimensions[i];
                var chunkSizeY = grid.GetSize().y / OverlayGraph.LevelDimensions[i];

                for (var x = 0; x < chunkSizeX; x++)
                    for (var y = 0; y < chunkSizeY; y++)
                    {
                        DebugDrawer.DrawNoOffset(new Vector2Int(x * OverlayGraph.LevelDimensions[i], y * OverlayGraph.LevelDimensions[i] + OverlayGraph.LevelDimensions[i]), new Vector2Int(x * OverlayGraph.LevelDimensions[i] + OverlayGraph.LevelDimensions[i], y * OverlayGraph.LevelDimensions[i] + OverlayGraph.LevelDimensions[i]), Color.magenta);
                        DebugDrawer.DrawNoOffset(new Vector2Int(x * OverlayGraph.LevelDimensions[i] + OverlayGraph.LevelDimensions[i], y * OverlayGraph.LevelDimensions[i] + OverlayGraph.LevelDimensions[i]), new Vector2Int(x * OverlayGraph.LevelDimensions[i] + OverlayGraph.LevelDimensions[i], y * OverlayGraph.LevelDimensions[i]), Color.magenta);
                    }
            }
        }

        public void DrawGraph()
        {
            foreach(var chunk in graphChunks)
            {
                chunk.Value.DrawGraph();
            }
        }

        public void DrawOverlayGraph(int level)
        {
            foreach (var chunk in graphChunks)
            {
                chunk.Value.DrawOverlayGraph(level);
            }
        }
    }
}
