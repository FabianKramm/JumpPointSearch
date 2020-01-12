using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Pathfinding
{
    public class NavChunk
    {
        public int chunkX;
        public int chunkY;
        public int chunkSize;

        public NavChunkGrid grid;

        public Dictionary<long, NavPoint> Points;

        public NavChunk(NavChunkGrid grid, int chunkX, int chunkY, int chunkSize)
        {
            Points = new Dictionary<long, NavPoint>();

            this.chunkX = chunkX;
            this.chunkY = chunkY;
            this.chunkSize = chunkSize;
            this.grid = grid;
        }

        public void BuildNavPoints(IGrid grid)
        {
            Points = new Dictionary<long, NavPoint>();

            var sizeY = grid.GetSize().y;

            var startX = chunkX * chunkSize;
            var endX = chunkX * chunkSize + chunkSize;
            var startY = chunkY * chunkSize;
            var endY = chunkY * chunkSize + chunkSize;

            for (var x = startX; x < endX; x++)
                for (var y = startY; y < endY; y++)
                {
                    if (grid.GetWeight(x, y) == CellType.Road)
                    {
                        Points[x * sizeY + y] = new NavPoint(x, y);
                    }
                }

            prunePoints();
        }

        private void prunePoints()
        {
            var pruned = true;
            while(pruned)
            {
                pruned = false;
                long key = 0;
                foreach (var pointKV in Points)
                {
                    var point = pointKV.Value;
                    var x = point.x;
                    var y = point.y;
                    var middle = (containsPoint(x + 1, y) && containsPoint(x - 1, y))
                            || (containsPoint(x, y + 1) && containsPoint(x, y - 1));
                    if (!middle && shouldKeep(x, y) == false)
                    {
                        pruned = true;
                        key = pointKV.Key;
                    }
                }

                if (pruned)
                    Points.Remove(key);
            }
        }

        private bool containsPoint(int x, int y)
        {
            return Points.ContainsKey(x * grid.SizeY + y);
        }

        private bool shouldKeep(int x, int y)
        {
            for(var i = -1; i <= 1; i++)
                for (var j = -1; j <= 1; j++)
                {
                    if (i == 0 || j == 0)
                        continue;

                    var upRight = containsPoint(x + i, y + j);
                    var up = containsPoint(x, y + j);
                    var right = containsPoint(x + i, y);
                    var downLeft = containsPoint(x - i, y - j);
                    var left = containsPoint(x - i, y);
                    var down = containsPoint(x, y - j);
                    var downRight = containsPoint(x + i, y - j);
                    var upLeft = containsPoint(x - i, y + j);

                    if (!upRight && 
                        !up && 
                        !right &&
                        !downLeft &&
                        left &&
                        down)
                        return true;

                    if (!left && !upLeft && !up && !upRight && !right)
                        return true;

                    if (!left && !upLeft && !up && !downLeft && !down)
                        return true;
                }

            return false;
        }

        public void ShowDebug()
        {
            foreach (var navPoint in Points.Values)
            {
                DebugDrawer.DrawCube(new UnityEngine.Vector2Int(navPoint.x, navPoint.y), Vector2Int.one, Color.yellow);
                foreach (var edge in navPoint.edges)
                {
                    DebugDrawer.Draw(new Vector2Int(navPoint.x, navPoint.y), new Vector2Int(edge.toX, edge.toY), Color.yellow);
                }
            }
        }
    }
}
